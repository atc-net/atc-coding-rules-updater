namespace Atc.CodingRules.Updater.CentralPackageManagement.Migration;

/// <summary>
/// Runs a Central Package Management migration planned by <see cref="CpmMigrationPlanner"/>.
/// </summary>
/// <remarks>
/// <para>
/// The migration must never leave a repository worse off. It therefore refuses to start on a
/// dirty git tree (so <c>git checkout .</c> is always a complete undo), restores before writing
/// to get a baseline, and restores again afterwards.
/// </para>
/// <para>
/// Any difference after writing puts every file back: a failed restore, a new NuGet warning
/// (for example NU1507 for several package sources without source mapping, which Release turns
/// into an error), or a package that appears, disappears or changes version in any project. A
/// package appearing matters because a <c>GlobalPackageReference</c> reaches every project under
/// the root, including projects that never imported the root analyzers.
/// </para>
/// </remarks>
public static class CpmMigrationExecutor
{
    /// <summary>
    /// Plans and, unless blocked or in dry-run, performs the migration.
    /// </summary>
    public static async Task<CpmMigrationResult> ExecuteAsync(
        ILogger logger,
        DirectoryInfo projectPath,
        CpmMigrationOptions options,
        ICpmMigrationEnvironment environment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        var plan = CpmMigrationPlanner.Plan(projectPath, options.UnifyVersions);

        if (plan.AlreadyMigrated)
        {
            return Result(CpmMigrationOutcome.AlreadyMigrated, plan);
        }

        if (plan.Blockers.Count > 0)
        {
            return Result(CpmMigrationOutcome.Blocked, plan);
        }

        if (!plan.CanMigrate)
        {
            return Result(CpmMigrationOutcome.NothingToMigrate, plan);
        }

        if (options.DryRun)
        {
            return Result(CpmMigrationOutcome.DryRun, plan);
        }

        var gitBlocker = await CheckWorkingTree(projectPath, options, environment, cancellationToken);
        if (gitBlocker is not null)
        {
            return Result(CpmMigrationOutcome.Blocked, WithBlocker(plan, gitBlocker));
        }

        RestoreSnapshot? baseline = null;
        if (!options.SkipRestoreVerification)
        {
            logger.LogInformation("Restoring before the migration to record a baseline");
            baseline = await environment.RestoreAsync(projectPath, options.RestoreTarget, options.BuildConfiguration, cancellationToken);
            if (baseline.ExitCode != 0)
            {
                return Result(
                    CpmMigrationOutcome.Blocked,
                    WithBlocker(plan, new CpmMigrationBlocker("BaselineRestoreFailed", "dotnet restore fails before the migration, so there is no baseline to verify against; fix the restore first", FilePath: null)));
            }

            var coverageBlocker = CheckRestoreCoverage(projectPath, baseline);
            if (coverageBlocker is not null)
            {
                return Result(CpmMigrationOutcome.Blocked, WithBlocker(plan, coverageBlocker));
            }
        }

        return await WriteAndVerify(logger, projectPath, options, environment, plan, baseline, cancellationToken);
    }

    /// <summary>
    /// Lists everything that differs between the two restores. Package differences are skipped
    /// when versions were unified on purpose, since those are expected.
    /// </summary>
    internal static List<string> Compare(
        RestoreSnapshot baseline,
        RestoreSnapshot after,
        bool comparePackages)
    {
        var differences = new List<string>();

        if (after.ExitCode != 0)
        {
            differences.Add($"dotnet restore failed after the migration (exit code {after.ExitCode})");
        }

        differences.AddRange(after.Warnings
            .Where(x => !baseline.Warnings.Contains(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .Select(x => $"new restore warning: {x}"));

        if (!comparePackages)
        {
            return differences;
        }

        var projects = baseline.Packages.Keys
            .Union(after.Packages.Keys, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal);

        foreach (var project in projects)
        {
            var before = baseline.Packages.GetValueOrDefault(project) ?? new HashSet<string>(StringComparer.Ordinal);
            var now = after.Packages.GetValueOrDefault(project) ?? new HashSet<string>(StringComparer.Ordinal);

            differences.AddRange(now.Except(before, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(x => $"{project}: gained {x}"));
            differences.AddRange(before.Except(now, StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(x => $"{project}: lost {x}"));
        }

        return differences;
    }

    private static async Task<CpmMigrationResult> WriteAndVerify(
        ILogger logger,
        DirectoryInfo projectPath,
        CpmMigrationOptions options,
        ICpmMigrationEnvironment environment,
        CpmMigrationPlan plan,
        RestoreSnapshot? baseline,
        CancellationToken cancellationToken)
    {
        var backup = WriteFiles(plan);

        if (baseline is null)
        {
            return Result(CpmMigrationOutcome.Migrated, plan);
        }

        logger.LogInformation("Restoring after the migration to verify it");
        var after = await environment.RestoreAsync(projectPath, options.RestoreTarget, options.BuildConfiguration, cancellationToken);
        var differences = Compare(baseline, after, comparePackages: !options.UnifyVersions);
        if (differences.Count == 0)
        {
            return Result(CpmMigrationOutcome.Migrated, plan);
        }

        RollBack(backup);

        // The rollback restores every file the migration wrote, but restore itself may have
        // rewritten tracked files (such as packages.lock.json). Verify instead of assuming.
        if (!options.AllowDirty &&
            await environment.IsWorkingTreeCleanAsync(projectPath, cancellationToken) != true)
        {
            differences.Add("The git working tree is not clean after the rollback - check 'git status'; restore may have rewritten tracked files such as packages.lock.json");
        }

        return new CpmMigrationResult(CpmMigrationOutcome.RolledBack, plan, differences);
    }

    /// <summary>
    /// <c>Directory.Packages.props</c> and <c>GlobalPackageReference</c> items reach every project
    /// under the root, so every project must be part of the verifying restore. A project it never
    /// saw could change what it restores without anyone noticing.
    /// </summary>
    private static CpmMigrationBlocker? CheckRestoreCoverage(
        DirectoryInfo projectPath,
        RestoreSnapshot baseline)
    {
        var missing = FileHelper.EnumerateSourceFiles(projectPath, "*.csproj")
            .Select(x => Path.GetRelativePath(projectPath.FullName, x.FullName).Replace('\\', '/'))
            .Where(x => !baseline.Packages.ContainsKey($"{Path.GetDirectoryName(x)!.Replace('\\', '/')}/obj/project.assets.json".TrimStart('/')))
            .Order(StringComparer.Ordinal)
            .ToList();

        return missing.Count == 0
            ? null
            : new CpmMigrationBlocker(
                "ProjectNotCoveredByRestore",
                $"The restore did not cover {string.Join(", ", missing)}, which the migration would also affect; pass a --build-file that includes every project, or use --skip-restore-verification",
                FilePath: null);
    }

    private static async Task<CpmMigrationBlocker?> CheckWorkingTree(
        DirectoryInfo projectPath,
        CpmMigrationOptions options,
        ICpmMigrationEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (options.AllowDirty)
        {
            return null;
        }

        return await environment.IsWorkingTreeCleanAsync(projectPath, cancellationToken) switch
        {
            null => new CpmMigrationBlocker("NotAGitRepository", "The repository is not under git, so there is no simple way to undo the migration; use --allow-dirty to proceed anyway", FilePath: null),
            false => new CpmMigrationBlocker("DirtyWorkingTree", "The git working tree has uncommitted changes; commit or stash them first so the migration can be undone with git, or use --allow-dirty", FilePath: null),
            true => null,
        };
    }

    /// <summary>
    /// Writes every planned file and returns the original bytes (<c>null</c> for files that did
    /// not exist), so a rollback is exact down to the byte-order mark.
    /// </summary>
    private static Dictionary<string, byte[]?> WriteFiles(CpmMigrationPlan plan)
    {
        var backup = new Dictionary<string, byte[]?>(StringComparer.Ordinal);
        foreach (var path in plan.FileChanges.Keys)
        {
            backup[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        foreach (var (path, content) in plan.FileChanges)
        {
            FileHelper.WriteAllTextPreservingByteOrderMark(path, content);
        }

        return backup;
    }

    private static void RollBack(Dictionary<string, byte[]?> backup)
    {
        foreach (var (path, original) in backup)
        {
            if (original is null)
            {
                File.Delete(path);
            }
            else
            {
                File.WriteAllBytes(path, original);
            }
        }
    }

    private static CpmMigrationPlan WithBlocker(
        CpmMigrationPlan plan,
        CpmMigrationBlocker blocker)
        => new()
        {
            Blockers = [.. plan.Blockers, blocker],
            Packages = plan.Packages,
            Overrides = plan.Overrides,
            FileChanges = plan.FileChanges,
        };

    private static CpmMigrationResult Result(
        CpmMigrationOutcome outcome,
        CpmMigrationPlan plan)
        => new(outcome, plan, []);
}