namespace Atc.CodingRules.Updater.CLI.Commands
{
    public class OptionsFileValidateCommand : AsyncCommand<BaseCommandSettings>
    {
        private readonly ILogger<OptionsFileValidateCommand> logger;

        public OptionsFileValidateCommand(ILogger<OptionsFileValidateCommand> logger) => this.logger = logger;

        public override Task<int> ExecuteAsync(
            CommandContext context,
            BaseCommandSettings settings)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(settings);
            return ExecuteInternalAsync(settings);
        }

        private async Task<int> ExecuteInternalAsync(
            BaseCommandSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ConsoleHelper.WriteHeader();

            var projectPath = new DirectoryInfo(settings.ProjectPath);
            var optionsPath = settings.GetOptionsPath();

            try
            {
                (bool isSuccessful, string error) = await OptionsHelper.ValidateOptionsFile(projectPath, optionsPath);
                if (isSuccessful)
                {
                    logger.LogInformation("The options file is valid");
                }
                else
                {
                    logger.LogError(error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"{EmojisConstants.Error} {ex.GetMessage()}");
                return ConsoleExitStatusCodes.Failure;
            }

            logger.LogInformation($"{EmojisConstants.Done} Done");
            return ConsoleExitStatusCodes.Success;
        }
    }
}