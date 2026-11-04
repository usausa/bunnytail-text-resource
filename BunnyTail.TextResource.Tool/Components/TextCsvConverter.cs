namespace BunnyTail.TextResource.Tool.Components;

public sealed record TextCsvResult(string Json, int KeyCount, IReadOnlyList<string> Errors);

public sealed class TextCsvConverter
{
    private const string KeyColumn = "key";

    private const string TextColumn = "$text";

    private const string CommentColumn = "$comment";

    private const char ReservedPrefix = '$';

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);

    private readonly List<string> errors = [];

    private readonly List<TextEntry> entries = [];

    private readonly HashSet<string> keys = new(StringComparer.Ordinal);

    private readonly List<(int Index, string Language)> languageColumns = [];

    private readonly HashSet<int> columns = [];

    private readonly string filePath;

    private int textColumn = -1;

    private int commentColumn = -1;

    private TextCsvConverter(string filePath)
    {
        this.filePath = filePath;
    }

    // ------------------------------------------------------------
    // Convert
    // ------------------------------------------------------------

    public static TextCsvResult Convert(byte[] bytes, string filePath)
    {
        var converter = new TextCsvConverter(filePath);
        var text = converter.Decode(bytes);
        if (text is not null)
        {
            converter.Read(text);
        }

        return converter.errors.Count > 0
            ? new TextCsvResult(string.Empty, 0, converter.errors)
            : new TextCsvResult(TextJsonWriter.Write(Path.GetFileName(filePath), converter.entries), converter.entries.Count, converter.errors);
    }

    private string? Decode(byte[] bytes)
    {
        var preamble = Utf8.Preamble;
        var offset = bytes.AsSpan().StartsWith(preamble) ? preamble.Length : 0;
        try
        {
            return Utf8.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            errors.Add($"File is not UTF-8. Save it as CSV UTF-8. file=[{filePath}]");
            return null;
        }
    }

    private void Read(string text)
    {
        var content = CsvParser.Parse(text);
        if (content.Error is not null)
        {
            errors.Add($"{content.Error} file=[{filePath}], row=[{content.ErrorRow}]");
            return;
        }

        var rows = content.Rows.Where(static x => !x.IsEmpty && !x.IsComment).ToList();
        if (rows.Count == 0)
        {
            errors.Add($"Header is not found. file=[{filePath}]");
            return;
        }

        if (ReadHeader(rows[0]))
        {
            foreach (var row in rows.Skip(1))
            {
                ReadRow(row);
            }
        }
    }

    // ------------------------------------------------------------
    // Header
    // ------------------------------------------------------------

    private bool ReadHeader(CsvRow header)
    {
        if (!IsColumn(header.Cells[0].Trim(), KeyColumn))
        {
            errors.Add($"First column is not key. file=[{filePath}], column=[{header.Cells[0]}]");
            return false;
        }

        columns.Add(0);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { KeyColumn };
        for (var i = 1; i < header.Cells.Count; i++)
        {
            var name = header.Cells[i].Trim();
            if ((name.Length > 0) && !names.Add(name))
            {
                errors.Add($"Column is duplicated. file=[{filePath}], column=[{name}]");
                continue;
            }

            ReadColumn(i, name);
        }

        if ((languageColumns.Count == 0) && (textColumn < 0))
        {
            errors.Add($"Language column is not found. file=[{filePath}]");
        }
        else if ((languageColumns.Count > 0) && (textColumn >= 0))
        {
            errors.Add($"$text and language columns are mixed. file=[{filePath}]");
        }

        return errors.Count == 0;
    }

    private void ReadColumn(int index, string name)
    {
        if (name.Length == 0)
        {
            return;
        }

        columns.Add(index);
        if (IsColumn(name, TextColumn))
        {
            textColumn = index;
        }
        else if (IsColumn(name, CommentColumn))
        {
            commentColumn = index;
        }
        else if (name[0] != ReservedPrefix)
        {
            if (IsLanguage(name))
            {
                languageColumns.Add((index, name));
            }
            else
            {
                errors.Add($"Column is not a language code. file=[{filePath}], column=[{name}]");
            }
        }
    }

    private static bool IsColumn(string name, string column) =>
        String.Equals(name, column, StringComparison.OrdinalIgnoreCase);

    private static bool IsLanguage(string name)
    {
        var parts = name.Split('-');
        return (parts[0].Length is >= 2 and <= 8) &&
               parts[0].All(Char.IsAsciiLetter) &&
               parts.Skip(1).All(static x => (x.Length is >= 1 and <= 8) && x.All(Char.IsAsciiLetterOrDigit));
    }

    // ------------------------------------------------------------
    // Row
    // ------------------------------------------------------------

    private void ReadRow(CsvRow row)
    {
        for (var i = 0; i < row.Cells.Count; i++)
        {
            if ((row.Cells[i].Length > 0) && !columns.Contains(i))
            {
                errors.Add($"Cell has no column. file=[{filePath}], row=[{row.Number}], value=[{row.Cells[i]}]");
            }
        }

        var key = row.Cells[0].Trim();
        if (key.Length == 0)
        {
            errors.Add($"Key is empty. file=[{filePath}], row=[{row.Number}]");
            return;
        }

        if (!keys.Add(key))
        {
            errors.Add($"Key is duplicated. file=[{filePath}], row=[{row.Number}], key=[{key}]");
            return;
        }

        var text = textColumn >= 0 ? GetCell(row, textColumn) : null;
        if (text?.Length == 0)
        {
            errors.Add($"Text is empty. file=[{filePath}], row=[{row.Number}], key=[{key}]");
            return;
        }

        var values = languageColumns
            .Select(x => (x.Language, Value: GetCell(row, x.Index)))
            .Where(static x => x.Value.Length > 0)
            .ToList();
        var comment = commentColumn >= 0 ? GetCell(row, commentColumn) : string.Empty;

        entries.Add(new TextEntry(key, text, values, comment));
    }

    private static string GetCell(CsvRow row, int index) =>
        index < row.Cells.Count ? row.Cells[index] : string.Empty;
}
