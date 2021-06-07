namespace Atc.CodingRules.AnalyzerRulesMetaData.Generator.CLI.Models
{
    public class Rule
    {
        public Rule()
        {
        }

        public Rule(
            string code,
            string title,
            string link)
        {
            this.Code = code;
            this.Title = title;
            this.Link = link;
        }

        public string Code { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Link { get; set; } = string.Empty;

        public override string ToString() => $"{nameof(Code)}: {Code}, {nameof(Title)}: {Title}, {nameof(Description)}: {Description}, {nameof(Link)}: {Link}";
    }
}