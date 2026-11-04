namespace BunnyTail.TextResource.Tool.Components;

public sealed record CsvRow(int Number, IReadOnlyList<string> Cells)
{
    public bool IsEmpty => Cells.All(static x => x.Length == 0);

    public bool IsComment => Cells[0].TrimStart().StartsWith('#');
}

public sealed record CsvContent(IReadOnlyList<CsvRow> Rows, string? Error, int ErrorRow);

public sealed class CsvParser
{
    private readonly List<CsvRow> rows = [];

    private readonly List<string> cells = [];

    private readonly StringBuilder buffer = new();

    private readonly string text;

    private int position;

    private string? error;

    private CsvParser(string text)
    {
        this.text = text;
    }

    public static CsvContent Parse(string text)
    {
        var parser = new CsvParser(text);
        parser.ReadRows();
        return new CsvContent(parser.rows, parser.error, parser.rows.Count + 1);
    }

    // ------------------------------------------------------------
    // Row
    // ------------------------------------------------------------

    private void ReadRows()
    {
        while ((position < text.Length) && ReadRow())
        {
        }
    }

    private bool ReadRow()
    {
        cells.Clear();
        do
        {
            if (!ReadCell())
            {
                return false;
            }
        }
        while (SkipDelimiter());

        SkipLineBreak();
        rows.Add(new CsvRow(rows.Count + 1, [.. cells]));
        return true;
    }

    private bool SkipDelimiter()
    {
        if (Peek() != ',')
        {
            return false;
        }

        position++;
        return true;
    }

    private void SkipLineBreak()
    {
        if (Peek() == '\r')
        {
            position++;
        }

        if (Peek() == '\n')
        {
            position++;
        }
    }

    // ------------------------------------------------------------
    // Cell
    // ------------------------------------------------------------

    private bool ReadCell()
    {
        if (Peek() == '"')
        {
            return ReadQuotedCell();
        }

        var start = position;
        while (!IsCellEnd())
        {
            position++;
        }

        cells.Add(text[start..position]);
        return true;
    }

    private bool ReadQuotedCell()
    {
        buffer.Clear();
        position++;
        while (position < text.Length)
        {
            var c = text[position++];
            if (c == '\r')
            {
                if (Peek() == '\n')
                {
                    position++;
                }

                buffer.Append('\n');
            }
            else if (c != '"')
            {
                buffer.Append(c);
            }
            else if (Peek() == '"')
            {
                buffer.Append('"');
                position++;
            }
            else if (IsCellEnd())
            {
                cells.Add(buffer.ToString());
                return true;
            }
            else
            {
                error = "Text follows the closing quote.";
                return false;
            }
        }

        error = "Quote is not closed.";
        return false;
    }

    private bool IsCellEnd() =>
        (position >= text.Length) || (text[position] is ',' or '\r' or '\n');

    private char Peek() => position < text.Length ? text[position] : '\0';
}
