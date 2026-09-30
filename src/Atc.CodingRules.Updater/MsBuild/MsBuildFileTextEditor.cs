namespace Atc.CodingRules.Updater.MsBuild;

/// <summary>
/// Targeted text edits on hand-written MSBuild files.
/// </summary>
/// <remarks>
/// Files such as <c>Directory.Packages.props</c> are maintained by hand, with their own
/// indentation, comments and line endings. Round-tripping them through <c>XDocument</c> would
/// rewrite all of that, so every edit here locates one element and changes only its text span.
/// Elements are found from a <see cref="PackageItem"/> (line number plus id), so attribute order
/// and several elements on one line do not matter.
/// </remarks>
public static class MsBuildFileTextEditor
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex VersionAttributeValue = new(
        """(?<=\s)Version(?<sep>\s*=\s*)(?<quote>["'])(?<value>.*?)\k<quote>""",
        RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex VersionAttributeWithLeadingSpace = new(
        """\s+Version\s*=\s*(?<quote>["']).*?\k<quote>""",
        RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex VersionAttributeName = new(
        """(?<=\s)Version(?=\s*=)""",
        RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex VersionChildElement = new(
        "<Version>(?<value>.*?)</Version>",
        RegexOptions.CultureInvariant | RegexOptions.Singleline,
        RegexTimeout);

    /// <summary>Sets the version of <paramref name="item"/>, whether it is an attribute or a child element.</summary>
    public static string SetVersion(
        string content,
        PackageItem item,
        string newVersion)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(item);

        var element = FindElement(content, item);
        var tag = content[element.Start..(element.TagEnd + 1)];

        var attribute = VersionAttributeValue.Match(tag);
        if (attribute.Success)
        {
            var valueStart = element.Start + attribute.Groups["value"].Index;
            return content[..valueStart] + newVersion + content[(valueStart + attribute.Groups["value"].Length)..];
        }

        if (element.CloseStart < 0)
        {
            return content;
        }

        var inner = content[(element.TagEnd + 1)..element.CloseStart];
        var child = VersionChildElement.Match(inner);
        if (!child.Success)
        {
            return content;
        }

        var childValueStart = element.TagEnd + 1 + child.Groups["value"].Index;
        return content[..childValueStart] + newVersion + content[(childValueStart + child.Groups["value"].Length)..];
    }

    /// <summary>
    /// Removes the version from <paramref name="item"/>. All other metadata stays. An element left
    /// with no children is collapsed to a self-closing tag.
    /// </summary>
    public static string RemoveVersion(
        string content,
        PackageItem item)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(item);

        var element = FindElement(content, item);
        var tag = content[element.Start..(element.TagEnd + 1)];

        var attribute = VersionAttributeWithLeadingSpace.Match(tag);
        if (attribute.Success)
        {
            var attributeStart = element.Start + attribute.Index;
            return content[..attributeStart] + content[(attributeStart + attribute.Length)..];
        }

        if (element.CloseStart < 0)
        {
            return content;
        }

        var innerStart = element.TagEnd + 1;
        var child = VersionChildElement.Match(content[innerStart..element.CloseStart]);
        if (!child.Success)
        {
            return content;
        }

        var (removeStart, removeEnd) = ExpandToWholeLine(content, innerStart + child.Index, innerStart + child.Index + child.Length);
        content = content[..removeStart] + content[removeEnd..];

        // Re-locate after the edit, then collapse an element that has nothing left inside.
        element = FindElement(content, item);
        var remaining = content[(element.TagEnd + 1)..element.CloseStart];
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            return content;
        }

        var closeEnd = content.IndexOf('>', element.CloseStart) + 1;
        var openTagWithoutEnd = content[element.Start..element.TagEnd].TrimEnd();
        return content[..element.Start] + openTagWithoutEnd + " />" + content[closeEnd..];
    }

    /// <summary>
    /// Turns the version of <paramref name="item"/> into a <c>VersionOverride</c>, keeping its value.
    /// </summary>
    public static string ReplaceVersionWithVersionOverride(
        string content,
        PackageItem item)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(item);

        var element = FindElement(content, item);
        var tag = content[element.Start..(element.TagEnd + 1)];

        var attribute = VersionAttributeName.Match(tag);
        if (attribute.Success)
        {
            var nameStart = element.Start + attribute.Index;
            return content[..nameStart] + "VersionOverride" + content[(nameStart + attribute.Length)..];
        }

        if (element.CloseStart < 0)
        {
            return content;
        }

        var innerStart = element.TagEnd + 1;
        var child = VersionChildElement.Match(content[innerStart..element.CloseStart]);
        if (!child.Success)
        {
            return content;
        }

        var replacement = $"<VersionOverride>{child.Groups["value"].Value}</VersionOverride>";
        return content[..(innerStart + child.Index)] + replacement + content[(innerStart + child.Index + child.Length)..];
    }

    /// <summary>
    /// Replaces the <c>ItemGroup</c> labelled <paramref name="label"/> with an XML comment made of
    /// <paramref name="commentLines"/>. Returns the content unchanged when there is no such group.
    /// </summary>
    public static string ReplaceItemGroup(
        string content,
        string label,
        IReadOnlyList<string> commentLines)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(commentLines);

        var group = FindLabelledItemGroup(content, label);
        if (group is null)
        {
            return content;
        }

        var (start, end) = group.CloseStart < 0
            ? (group.Start, group.TagEnd + 1)
            : (group.Start, content.IndexOf('>', group.CloseStart) + 1);

        var lineStart = LineStart(content, start);
        var indent = content[lineStart..start];
        if (!string.IsNullOrWhiteSpace(indent))
        {
            lineStart = start;
            indent = string.Empty;
        }

        var newLine = DetectNewLine(content);
        var unit = DetectIndentUnit(content);

        var comment = new StringBuilder();
        comment.Append(indent).Append("<!--").Append(newLine);
        foreach (var line in commentLines)
        {
            comment.Append(indent).Append(unit).Append(line).Append(newLine);
        }

        comment.Append(indent).Append("-->");

        return content[..lineStart] + comment + content[end..];
    }

    /// <summary>
    /// Adds <paramref name="itemElements"/> to the <c>ItemGroup</c> labelled
    /// <paramref name="groupLabel"/>, creating that group before <c>&lt;/Project&gt;</c> when it
    /// does not exist. The file's own newline style and indentation unit are used.
    /// </summary>
    public static string AppendItems(
        string content,
        string groupLabel,
        IReadOnlyList<string> itemElements)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(itemElements);

        if (itemElements.Count == 0)
        {
            return content;
        }

        var newLine = DetectNewLine(content);
        var unit = DetectIndentUnit(content);

        var group = FindLabelledItemGroup(content, groupLabel);
        if (group is { CloseStart: >= 0 })
        {
            var closeLineStart = LineStart(content, group.CloseStart);
            var closeIndent = content[closeLineStart..group.CloseStart];
            if (string.IsNullOrWhiteSpace(closeIndent))
            {
                var lines = new StringBuilder();
                foreach (var item in itemElements)
                {
                    lines.Append(closeIndent).Append(unit).Append(item).Append(newLine);
                }

                return content[..closeLineStart] + lines + content[closeLineStart..];
            }
        }

        var projectClose = content.LastIndexOf("</Project>", StringComparison.Ordinal);
        if (projectClose < 0)
        {
            throw new InvalidOperationException("The file has no </Project> end tag to add items before.");
        }

        var block = new StringBuilder();
        block.Append(unit).Append("<ItemGroup Label=\"").Append(groupLabel).Append("\">").Append(newLine);
        foreach (var item in itemElements)
        {
            block.Append(unit).Append(unit).Append(item).Append(newLine);
        }

        block.Append(unit).Append("</ItemGroup>").Append(newLine);

        var projectCloseLineStart = LineStart(content, projectClose);
        if (!string.IsNullOrWhiteSpace(content[projectCloseLineStart..projectClose]))
        {
            // "</Project>" shares its line with other markup: open a new line for the group.
            return content[..projectClose] + newLine + block + content[projectClose..];
        }

        var before = content[..projectCloseLineStart];
        var leadingBlankLine = before.EndsWith(newLine + newLine, StringComparison.Ordinal) ? string.Empty : newLine;
        return before + leadingBlankLine + block + newLine + content[projectCloseLineStart..];
    }

    private static ElementSpan FindElement(
        string content,
        PackageItem item)
    {
        var name = item.ItemType.ToString();
        var idPattern = new Regex(
            $"""\b(Include|Update)\s*=\s*(["']){Regex.Escape(item.Id)}\2""",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
            RegexTimeout);

        var index = LineOffset(content, item.LineNumber);
        while (true)
        {
            var start = IndexOfStartTag(content, name, index);
            if (start < 0)
            {
                throw new InvalidOperationException($"Could not locate <{name} Include=\"{item.Id}\"> from line {item.LineNumber}.");
            }

            var span = SpanAt(content, start, name);
            if (idPattern.IsMatch(content[start..(span.TagEnd + 1)]))
            {
                return span;
            }

            index = start + 1;
        }
    }

    private static ElementSpan? FindLabelledItemGroup(
        string content,
        string label)
    {
        var labelPattern = new Regex(
            $"""\bLabel\s*=\s*(["']){Regex.Escape(label)}\1""",
            RegexOptions.CultureInvariant,
            RegexTimeout);

        var index = 0;
        while (true)
        {
            var start = IndexOfStartTag(content, "ItemGroup", index);
            if (start < 0)
            {
                return null;
            }

            var span = SpanAt(content, start, "ItemGroup");
            if (labelPattern.IsMatch(content[start..(span.TagEnd + 1)]))
            {
                return span;
            }

            index = start + 1;
        }
    }

    private static int IndexOfStartTag(
        string content,
        string name,
        int fromIndex)
    {
        var index = fromIndex;
        while (true)
        {
            index = content.IndexOf("<" + name, index, StringComparison.Ordinal);
            if (index < 0)
            {
                return -1;
            }

            var next = index + 1 + name.Length;
            if (next < content.Length &&
                (char.IsWhiteSpace(content[next]) || content[next] == '/' || content[next] == '>'))
            {
                return index;
            }

            index++;
        }
    }

    private static ElementSpan SpanAt(
        string content,
        int start,
        string name)
    {
        var tagEnd = FindTagEnd(content, start);
        var selfClosing = content[tagEnd - 1] == '/';
        var closeStart = selfClosing
            ? -1
            : content.IndexOf("</" + name, tagEnd, StringComparison.Ordinal);

        return new ElementSpan(start, tagEnd, closeStart);
    }

    /// <summary>Index of the <c>&gt;</c> that ends the tag starting at <paramref name="start"/>, skipping quoted attribute values.</summary>
    private static int FindTagEnd(
        string content,
        int start)
    {
        char? quote = null;
        for (var i = start; i < content.Length; i++)
        {
            var c = content[i];
            if (quote is not null)
            {
                if (c == quote)
                {
                    quote = null;
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '>')
            {
                return i;
            }
        }

        throw new InvalidOperationException("Unterminated tag.");
    }

    private static int LineOffset(
        string content,
        int lineNumber)
    {
        var offset = 0;
        for (var line = 1; line < lineNumber; line++)
        {
            var next = content.IndexOf('\n', offset);
            if (next < 0)
            {
                return offset;
            }

            offset = next + 1;
        }

        return offset;
    }

    private static int LineStart(
        string content,
        int index)
        => index == 0 ? 0 : content.LastIndexOf('\n', index - 1) + 1;

    /// <summary>
    /// Widens [<paramref name="start"/>, <paramref name="end"/>) to the whole line, including its
    /// newline, when nothing else is on that line.
    /// </summary>
    private static (int Start, int End) ExpandToWholeLine(
        string content,
        int start,
        int end)
    {
        var lineStart = LineStart(content, start);
        var lineEnd = content.IndexOf('\n', end);
        var lineEndExclusive = lineEnd < 0 ? content.Length : lineEnd + 1;

        var before = content[lineStart..start];
        var after = content[end..(lineEnd < 0 ? content.Length : lineEnd)];

        return string.IsNullOrWhiteSpace(before) && string.IsNullOrWhiteSpace(after)
            ? (lineStart, lineEndExclusive)
            : (start, end);
    }

    private static string DetectNewLine(string content)
        => content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    /// <summary>The leading whitespace of the first indented element line, or two spaces.</summary>
    private static string DetectIndentUnit(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.TrimStart(' ', '\t');
            if (trimmed.Length < line.Length && trimmed.StartsWith('<'))
            {
                return line[..(line.Length - trimmed.Length)];
            }
        }

        return "  ";
    }

    /// <summary>Location of one element: start of its start tag, the start tag's closing <c>&gt;</c>, and the start of its end tag (or -1 when self-closing).</summary>
    private sealed record ElementSpan(
        int Start,
        int TagEnd,
        int CloseStart);
}