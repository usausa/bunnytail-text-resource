namespace BunnyTail.TextResource.Generator;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis.Text;

internal readonly record struct TextToken(string Value, TextSpan Span);

internal readonly record struct TextFileMember(TextToken Name, TextToken Value);

internal sealed record TextFileEntry(TextToken Key, TextToken? Text, List<TextFileMember> Members, TextSpan ValueSpan);

internal sealed record TextFileContent(List<TextFileEntry> Entries, string? Error, int ErrorPosition);

internal sealed class TextFileParser
{
    private static readonly char[] LineBreaks = ['\r', '\n'];

    private readonly List<TextFileEntry> entries = [];

    private readonly string text;

    private int position;

    private string? error;

    private TextFileParser(string text)
    {
        this.text = text;
    }

    public static TextFileContent Parse(string text)
    {
        var parser = new TextFileParser(text);
        parser.ReadDocument();
        return new TextFileContent(parser.entries, parser.error, parser.position);
    }

    // ------------------------------------------------------------
    // Structure
    // ------------------------------------------------------------

    private void ReadDocument()
    {
        SkipTrivia();
        if (!ReadObject(ReadEntry))
        {
            return;
        }

        SkipTrivia();
        if (position < text.Length)
        {
            Fail("Unexpected text after the table");
        }
    }

    private bool ReadEntry(TextToken key)
    {
        if (Peek() == '"')
        {
            if (!TryReadString(out var value))
            {
                return false;
            }

            entries.Add(new TextFileEntry(key, value, [], value.Span));
            return true;
        }

        if (Peek() != '{')
        {
            return Fail("Value must be a string or an object");
        }

        var start = position;
        var members = new List<TextFileMember>();
        var result = ReadObject(name =>
        {
            if (!TryReadString(out var value))
            {
                return false;
            }

            members.Add(new TextFileMember(name, value));
            return true;
        });
        if (!result)
        {
            return false;
        }

        entries.Add(new TextFileEntry(key, null, members, TextSpan.FromBounds(start, position)));
        return true;
    }

    private bool ReadObject(Func<TextToken, bool> readValue)
    {
        if (Peek() != '{')
        {
            return Fail("'{' expected");
        }

        position++;
        SkipTrivia();
        while (Peek() != '}')
        {
            if (!TryReadString(out var name))
            {
                return false;
            }

            SkipTrivia();
            if (Peek() != ':')
            {
                return Fail("':' expected");
            }

            position++;
            SkipTrivia();
            if (!readValue(name))
            {
                return false;
            }

            SkipTrivia();
            if (Peek() == ',')
            {
                position++;
                SkipTrivia();
            }
            else if (Peek() != '}')
            {
                return Fail("',' or '}' expected");
            }
        }

        position++;
        return true;
    }

    // ------------------------------------------------------------
    // String
    // ------------------------------------------------------------

    private bool TryReadString(out TextToken token)
    {
        token = default;

        var start = position;
        if (Peek() != '"')
        {
            return Fail("'\"' expected");
        }

        position++;
        var buffer = new StringBuilder();
        while (true)
        {
            if ((position >= text.Length) || (text[position] is '\r' or '\n'))
            {
                return Fail("Unterminated string");
            }

            var c = text[position++];
            if (c == '"')
            {
                token = new TextToken(buffer.ToString(), TextSpan.FromBounds(start, position));
                return true;
            }

            if (c != '\\')
            {
                buffer.Append(c);
            }
            else if (!TryReadEscape(buffer))
            {
                return false;
            }
        }
    }

    private bool TryReadEscape(StringBuilder buffer)
    {
        switch (Peek())
        {
            case '"':
                buffer.Append('"');
                break;
            case '/':
                buffer.Append('/');
                break;
            case '\\':
                buffer.Append('\\');
                break;
            case 'b':
                buffer.Append('\b');
                break;
            case 'f':
                buffer.Append('\f');
                break;
            case 'n':
                buffer.Append('\n');
                break;
            case 'r':
                buffer.Append('\r');
                break;
            case 't':
                buffer.Append('\t');
                break;
            case 'u':
                if (((position + 5) > text.Length) ||
                    !Int32.TryParse(text.Substring(position + 1, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
                {
                    return Fail("Invalid unicode escape");
                }

                buffer.Append((char)code);
                position += 4;
                break;
            default:
                return Fail("Invalid escape");
        }

        position++;
        return true;
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private char Peek() => position < text.Length ? text[position] : '\0';

    private bool Fail(string message)
    {
        error = message;
        return false;
    }

    private void SkipTrivia()
    {
        while (position < text.Length)
        {
            if (Char.IsWhiteSpace(text[position]))
            {
                position++;
            }
            else if (IsAt("//"))
            {
                var end = text.IndexOfAny(LineBreaks, position);
                position = end < 0 ? text.Length : end;
            }
            else if (IsAt("/*"))
            {
                var end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                position = end < 0 ? text.Length : end + 2;
            }
            else
            {
                return;
            }
        }
    }

    private bool IsAt(string value) =>
        String.CompareOrdinal(text, position, value, 0, value.Length) == 0;
}
