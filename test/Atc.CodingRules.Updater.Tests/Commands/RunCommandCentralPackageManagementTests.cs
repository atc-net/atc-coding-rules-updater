namespace Atc.CodingRules.Updater.Tests.Commands;

public sealed class RunCommandCentralPackageManagementTests
{
    [Fact]
    public void OptionsFile_DefaultsToAutoDetection()
    {
        var actual = new OptionsFile();

        actual.CentralPackageManagement.Should().Be(CentralPackageManagementMode.Auto);
    }

    [Fact]
    public async Task CreateDefault_ReadsCentralPackageManagementFromOptionsFile()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteFile(
            "atc-coding-rules-updater.json",
            """{ "projectTarget": "DotNet10", "centralPackageManagement": "Disabled" }""");

        var actual = await OptionsHelper.CreateDefault(workspace.Root, settingsOptionsPath: null, CancellationToken.None);

        actual.CentralPackageManagement.Should().Be(CentralPackageManagementMode.Disabled);
    }

    [Fact]
    public async Task GetOptionsFromFileAndUserArguments_AppliesCentralPackageManagementFlag()
    {
        using var workspace = new TestWorkspace();
        var settings = new RunCommandSettings
        {
            ProjectPath = workspace.Root.FullName,
            CentralPackageManagement = new FlagValue<CentralPackageManagementMode>
            {
                IsSet = true,
                Value = CentralPackageManagementMode.Disabled,
            },
        };

        var actual = await RunCommand.GetOptionsFromFileAndUserArguments(settings, workspace.Root, CancellationToken.None);

        actual.CentralPackageManagement.Should().Be(CentralPackageManagementMode.Disabled);
    }

    [Fact]
    public void RunSummary_OmitsCentralPackageManagementFromJson_WhenNotSet()
    {
        // Repositories that do not use CPM must get exactly the JSON they got before.
        var actual = RunCommand.SerializeJson(new RunSummary());

        actual.Should().NotContain("CentralPackageManagement");
    }

    [Fact]
    public void RunSummary_IncludesCentralPackageManagementInJson_WhenEnabled()
    {
        var actual = RunCommand.SerializeJson(new RunSummary { CentralPackageManagement = true });

        actual.Should().Contain("\"CentralPackageManagement\": true");
    }
}