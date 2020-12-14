namespace Atc.CodingRules.Updater.CLI.Models
{
    public class OptionsMappings
    {
        public OptionsFolderMappings Sample { get; set; } = new OptionsFolderMappings();

        public OptionsFolderMappings Src { get; set; } = new OptionsFolderMappings();

        public OptionsFolderMappings Test { get; set; } = new OptionsFolderMappings();

        public override string ToString()
        {
            return $"{nameof(Sample)}: ({Sample}), {nameof(Src)}: ({Src}), {nameof(Test)}: ({Test})";
        }
    }
}