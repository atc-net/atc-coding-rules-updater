// ReSharper disable StringLiteralTypo
namespace Atc.CodingRules.Updater.CLI.Commands.Settings;

public class ProjectBaseCommandSettings : BaseCommandSettings
{
    [CommandOption($"{ArgumentCommandConstants.ShortProjectPath}|{ArgumentCommandConstants.LongProjectPath} <PROJECTPATH>")]
    [Description("Path to the project directory (default current diectory)")]
    public string ProjectPath { get; init; } = string.Empty;

    [CommandOption($"{ArgumentCommandConstants.ShortOptionsPath}|{ArgumentCommandConstants.LongOptionsTarget} [OPTIONSPATH]")]
    [Description("Path to an optional options json-file")]
    public FlagValue<string>? OptionsPath { get; init; }

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        return !validationResult.Successful
            ? validationResult
            : string.IsNullOrEmpty(ProjectPath)
                ? ValidationResult.Error("ProjectPath is missing.")
                : ValidationResult.Success();
    }

    internal string GetOptionsPath()
    {
        var optionsPath = string.Empty;
        if (OptionsPath is not null && OptionsPath.IsSet)
        {
            optionsPath = OptionsPath.Value;
        }

        return optionsPath;
    }
}