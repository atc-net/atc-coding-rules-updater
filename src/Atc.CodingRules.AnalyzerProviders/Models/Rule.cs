namespace Atc.CodingRules.AnalyzerProviders.Models
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

        public Rule(
            string code,
            string title,
            string link,
            string category)
            : this(code, title, link)
        {
            this.Category = category;
        }

        public string Code { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Link { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Description { get; set; }

        public override string ToString() => $"{nameof(Code)}: {Code}, {nameof(Title)}: {Title}, {nameof(Link)}: {Link}, {nameof(Category)}: {Category}, {nameof(Description)}: {Description}";
    }
}