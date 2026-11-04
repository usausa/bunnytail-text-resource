namespace BunnyTail.TextResource.Generator;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;

internal sealed class TextTemplate
{
    private const int MaxDigits = 6;

    private static readonly char[] Separators = [',', ':'];

    private readonly List<Segment> segments;

    public string Display { get; }

    public List<string> Names { get; }

    private TextTemplate(string display, List<Segment> segments, List<string> names)
    {
        Display = display;
        this.segments = segments;
        Names = names;
    }

    // ------------------------------------------------------------
    // Parse
    // ------------------------------------------------------------

    public static TextTemplate? Parse(string value)
    {
        var display = new StringBuilder(value.Length);
        var literal = new StringBuilder(value.Length);
        var segments = new List<Segment>();
        var names = new List<string>();
        var numbers = new SortedSet<int>();

        var i = 0;
        while (i < value.Length)
        {
            var c = value[i];
            if ((c is '{' or '}') && ((i + 1) < value.Length) && (value[i + 1] == c))
            {
                display.Append(c);
                literal.Append(c, 2);
                i += 2;
            }
            else if (c == '{')
            {
                var end = value.IndexOf('}', i + 1);
                if (end < 0)
                {
                    return null;
                }

                var body = value.Substring(i + 1, end - i - 1);
                var separator = body.IndexOfAny(Separators);
                var name = separator < 0 ? body : body.Substring(0, separator);
                var suffix = separator < 0 ? string.Empty : body.Substring(separator);
                if ((body.IndexOf('{') >= 0) || !IsValidSuffix(suffix))
                {
                    return null;
                }

                if (IsNumber(name))
                {
                    var number = Int32.Parse(name, NumberStyles.None, CultureInfo.InvariantCulture);
                    numbers.Add(number);
                    name = GetNumberedName(number);
                }
                else if (IsValidName(name))
                {
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
                else
                {
                    return null;
                }

                segments.Add(new Segment(literal.ToString(), null));
                literal.Clear();
                segments.Add(new Segment(suffix, name));

                display.Append('{').Append(body).Append('}');
                i = end + 1;
            }
            else if (c == '}')
            {
                return null;
            }
            else
            {
                display.Append(c);
                literal.Append(c);
                i++;
            }
        }

        if ((names.Count > 0) && (numbers.Count > 0))
        {
            return null;
        }

        segments.Add(new Segment(literal.ToString(), null));

        return new TextTemplate(display.ToString(), segments, names.Count > 0 ? names : numbers.Select(GetNumberedName).ToList());
    }

    private static string GetNumberedName(int number) =>
        "arg" + number.ToString(CultureInfo.InvariantCulture);

    private static bool IsNumber(string value) =>
        (value.Length > 0) && (value.Length <= MaxDigits) && value.All(static x => x is >= '0' and <= '9');

    private static bool IsValidName(string value) =>
        SyntaxFacts.IsValidIdentifier(value) && !value.StartsWith("__", StringComparison.Ordinal);

    private static bool IsValidSuffix(string suffix)
    {
        if ((suffix.Length == 0) || (suffix[0] == ':'))
        {
            return true;
        }

        var end = suffix.IndexOf(':');
        var alignment = (end < 0 ? suffix.Substring(1) : suffix.Substring(1, end - 1)).Trim();
        return IsNumber(alignment.StartsWith("-", StringComparison.Ordinal) ? alignment.Substring(1) : alignment);
    }

    // ------------------------------------------------------------
    // Build
    // ------------------------------------------------------------

    public bool HasSameNames(TextTemplate other) =>
        (Names.Count == other.Names.Count) && Names.All(other.Names.Contains);

    public string BuildValue(List<string> parameters)
    {
        if (parameters.Count == 0)
        {
            return Display;
        }

        var buffer = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.Name is null)
            {
                buffer.Append(segment.Text);
            }
            else
            {
                buffer.Append('{')
                    .Append(parameters.IndexOf(segment.Name).ToString(CultureInfo.InvariantCulture))
                    .Append(segment.Text)
                    .Append('}');
            }
        }

        return buffer.ToString();
    }

    private readonly record struct Segment(string Text, string? Name);
}
