namespace BunnyTail.TextResource.Tool.Components;

public sealed record TextEntry(string Key, string? Text, IReadOnlyList<(string Language, string Value)> Values, string Comment);

public static class TextJsonWriter
{
    private const string NewLine = "\r\n";

    private const string Indent = "  ";

    public static string Write(string fileName, IReadOnlyList<TextEntry> entries)
    {
        var buffer = new StringBuilder();
        buffer.Append("// Generated from ").Append(fileName).Append(NewLine);
        buffer.Append('{').Append(NewLine);
        for (var i = 0; i < entries.Count; i++)
        {
            AppendEntry(buffer, entries[i]);
            buffer.Append(i < entries.Count - 1 ? "," : string.Empty).Append(NewLine);
        }

        buffer.Append('}').Append(NewLine);
        return buffer.ToString();
    }

    private static void AppendEntry(StringBuilder buffer, TextEntry entry)
    {
        if (entry.Text is not null)
        {
            if (entry.Comment.Length > 0)
            {
                buffer.Append(Indent).Append("// ").Append(entry.Comment.Replace('\n', ' ')).Append(NewLine);
            }

            buffer.Append(Indent);
            AppendString(buffer, entry.Key);
            buffer.Append(": ");
            AppendString(buffer, entry.Text);
            return;
        }

        var members = entry.Values.AsEnumerable();
        if (entry.Comment.Length > 0)
        {
            members = members.Append(("$comment", entry.Comment));
        }

        buffer.Append(Indent);
        AppendString(buffer, entry.Key);
        buffer.Append(": { ");
        var first = true;
        foreach (var (name, value) in members)
        {
            if (!first)
            {
                buffer.Append(", ");
            }

            first = false;
            AppendString(buffer, name);
            buffer.Append(": ");
            AppendString(buffer, value);
        }

        buffer.Append(first ? "}" : " }");
    }

    private static void AppendString(StringBuilder buffer, string value)
    {
        buffer.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    buffer.Append("\\\"");
                    break;
                case '\\':
                    buffer.Append("\\\\");
                    break;
                case '\n':
                    buffer.Append("\\n");
                    break;
                case '\t':
                    buffer.Append("\\t");
                    break;
                default:
                    if (Char.IsControl(c) || (Char.GetUnicodeCategory(c) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator))
                    {
                        buffer.Append('\\').Append('u').Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        buffer.Append(c);
                    }

                    break;
            }
        }

        buffer.Append('"');
    }
}
