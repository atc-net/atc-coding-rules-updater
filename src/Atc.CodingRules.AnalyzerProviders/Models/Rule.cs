namespace Atc.CodingRules.AnalyzerProviders.Models;

public class Rule
{
    public Rule()
    {
    }

    public Rule(
        string code,
        string title,
        string link,
        string? category = null,
        string? description = null)
    {
        this.Code = code;
        this.Title = title;
        this.Link = link;
        this.Category = category;
        this.Description = description;
    }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Description { get; set; }

    public string TitleAndLink
    {
        get
        {
            var s = string.Empty;
            if (!string.IsNullOrEmpty(Title))
            {
                s += $" - {Title}";
            }

            if (!string.IsNullOrEmpty(Link))
            {
                s += $" - {Link}";
            }

            return s;
        }
    }

    public override string ToString()
        => $"{nameof(Code)}: {Code}, {nameof(Title)}: {Title}, {nameof(Link)}: {Link}, {nameof(Category)}: {Category}, {nameof(Description)}: {Description}";
}