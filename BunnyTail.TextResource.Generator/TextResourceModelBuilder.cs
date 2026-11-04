namespace BunnyTail.TextResource.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using BunnyTail.TextResource.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

internal sealed class TextResourceModelBuilder
{
    public const string FileExtension = ".texts.json";

    private const string CommentName = "$comment";

    private const char ReservedPrefix = '$';

    private static readonly char[] PathSeparators = ['/', '\\'];

    private static readonly char[] NamespaceSeparators = ['.'];

    private static readonly char[] LanguageSeparators = ['-'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "Keys",
        "GetText",
        "GetTextFor",
        "Equals",
        "Finalize",
        "GetHashCode",
        "GetType",
        "MemberwiseClone",
        "ReferenceEquals",
        "ToString"
    };

    private readonly List<DiagnosticInfo> diagnostics = [];

    private readonly List<EntryModel> entries = [];

    private readonly HashSet<string> keys = new(StringComparer.Ordinal);

    private readonly string path;

    private readonly string className;

    private readonly EquatableArray<string> languages;

    private readonly SourceText source;

    private TextResourceModelBuilder(string path, string className, EquatableArray<string> languages, string text)
    {
        this.path = path;
        this.className = className;
        this.languages = languages;
        source = SourceText.From(text);
    }

    // ------------------------------------------------------------
    // Options
    // ------------------------------------------------------------

    public static OptionsModel BuildOptions(AnalyzerConfigOptions options)
    {
        options.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        options.TryGetValue("build_property.ProjectDir", out var projectDirectory);
        options.TryGetValue("build_property.TextResourceAccessibility", out var accessibility);
        options.TryGetValue("build_property._TextResourceLanguages", out var languages);

        var diagnostics = new List<DiagnosticInfo>();
        return new OptionsModel(
            rootNamespace ?? string.Empty,
            projectDirectory ?? string.Empty,
            ParseAccessibility(accessibility, diagnostics),
            ParseLanguages(languages, diagnostics),
            new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    private static string ParseAccessibility(string? value, List<DiagnosticInfo> diagnostics)
    {
        if (String.Equals(value, "public", StringComparison.OrdinalIgnoreCase))
        {
            return "public";
        }

        if (!String.IsNullOrEmpty(value) && !String.Equals(value, "internal", StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidAccessibility, (Location?)null, value!));
        }

        return "internal";
    }

    private static EquatableArray<string> ParseLanguages(string? value, List<DiagnosticInfo> diagnostics)
    {
        var languages = new List<string>();
        if (String.IsNullOrWhiteSpace(value))
        {
            return new EquatableArray<string>(languages);
        }

        foreach (var element in value!.Split(','))
        {
            var language = element.Trim();
            if (!IsValidLanguage(language) || languages.Contains(NormalizeLanguage(language)))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidLanguages, (Location?)null, language));
                continue;
            }

            languages.Add(NormalizeLanguage(language));
        }

        return new EquatableArray<string>(languages);
    }

    private static bool IsValidLanguage(string language)
    {
        var parts = language.Split(LanguageSeparators);
        return (parts[0].Length is >= 2 and <= 8) &&
               parts[0].All(IsAsciiLetter) &&
               parts.Skip(1).All(static x => (x.Length is >= 1 and <= 8) && x.All(static c => IsAsciiLetter(c) || c is >= '0' and <= '9'));
    }

    private static bool IsAsciiLetter(char c) =>
        c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    private static string NormalizeLanguage(string language)
    {
        var parts = language.Split(LanguageSeparators);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if ((i > 0) && (part.Length == 2))
            {
                parts[i] = part.ToUpperInvariant();
            }
            else if ((i > 0) && (part.Length == 4))
            {
                parts[i] = part.Substring(0, 1).ToUpperInvariant() + ToLower(part.Substring(1));
            }
            else
            {
                parts[i] = ToLower(part);
            }
        }

        return String.Join("-", parts);
    }

    private static string ToLower(string value) =>
        new(value.Select(Char.ToLowerInvariant).ToArray());

    // ------------------------------------------------------------
    // Table
    // ------------------------------------------------------------

    public static Result<TableModel> BuildTable(TextFileModel file, OptionsModel options)
    {
        var className = GetClassName(file.Path);
        if (!IsValidMemberName(className))
        {
            return Results.Error<TableModel>(new DiagnosticInfo(Diagnostics.InvalidClassName, CreateFileLocation(file.Path), className));
        }

        var builder = new TextResourceModelBuilder(file.Path, className, options.Languages, file.Text ?? string.Empty);
        if (file.Text is null)
        {
            builder.diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidTextFile, CreateFileLocation(file.Path), "File cannot be read"));
        }
        else
        {
            builder.ReadText(file.Text);
        }

        var table = new TableModel(
            file.Path,
            GetNamespace(options, file.Path),
            className,
            options.Accessibility,
            options.Languages,
            new EquatableArray<EntryModel>(builder.entries));
        return new Result<TableModel>(table, new EquatableArray<DiagnosticInfo>(builder.diagnostics));
    }

    public static EquatableArray<string> FindDuplicates(ImmutableArray<Result<TableModel>> results) =>
        new(results
            .SelectValue()
            .GroupBy(static x => x.FullName, StringComparer.OrdinalIgnoreCase)
            .Where(static x => x.Count() > 1)
            .Select(static x => x.Key));

    public static LocationInfo CreateFileLocation(string path) =>
        new(path, default, default);

    private static string GetClassName(string path)
    {
        var name = path.Substring(path.LastIndexOfAny(PathSeparators) + 1);
        return name.Substring(0, name.Length - FileExtension.Length);
    }

    private static bool IsValidMemberName(string name) =>
        SyntaxFacts.IsValidIdentifier(name) && (SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None);

    // ------------------------------------------------------------
    // Namespace
    // ------------------------------------------------------------

    private static string GetNamespace(OptionsModel options, string path)
    {
        var parts = new List<string>();
        AddNamespaceParts(parts, options.RootNamespace);

        var project = options.ProjectDirectory.Replace('\\', '/').TrimEnd('/');
        var file = path.Replace('\\', '/');
        var directory = file.Substring(0, Math.Max(file.LastIndexOf('/'), 0));
        if ((project.Length > 0) &&
            (directory.Length > project.Length) &&
            directory.StartsWith(project, StringComparison.OrdinalIgnoreCase) &&
            (directory[project.Length] == '/'))
        {
            foreach (var folder in directory.Substring(project.Length + 1).Split('/'))
            {
                AddNamespaceParts(parts, folder);
            }
        }

        return String.Join(".", parts);
    }

    private static void AddNamespaceParts(List<string> parts, string value)
    {
        foreach (var part in value.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            parts.Add(ToIdentifier(part));
        }
    }

    private static string ToIdentifier(string name)
    {
        var buffer = new StringBuilder(name.Length + 1);
        if (!SyntaxFacts.IsIdentifierStartCharacter(name[0]) && SyntaxFacts.IsIdentifierPartCharacter(name[0]))
        {
            buffer.Append('_');
        }

        foreach (var c in name)
        {
            buffer.Append(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
        }

        return buffer.ToString();
    }

    // ------------------------------------------------------------
    // Entry
    // ------------------------------------------------------------

    private void ReadText(string text)
    {
        var content = TextFileParser.Parse(text);
        foreach (var entry in content.Entries)
        {
            AddEntry(entry);
        }

        if (content.Error is not null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidTextFile, CreateLocation(new TextSpan(content.ErrorPosition, 0)), content.Error));
        }
    }

    private void AddEntry(TextFileEntry entry)
    {
        var key = entry.Key.Value;
        if ((key.Length > 0) && (key[0] == ReservedPrefix))
        {
            return;
        }

        if (!IsValidKey(key))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidKey, CreateLocation(entry.Key.Span), key));
            return;
        }

        if (!keys.Add(key))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.DuplicateKey, CreateLocation(entry.Key.Span), key));
            return;
        }

        if (languages.Count == 0)
        {
            AddSingleLanguageEntry(entry);
        }
        else
        {
            AddMultiLanguageEntry(entry);
        }
    }

    private bool IsValidKey(string key) =>
        IsValidMemberName(key) &&
        !key.StartsWith("__", StringComparison.Ordinal) &&
        !ReservedNames.Contains(key) &&
        !String.Equals(key, className, StringComparison.Ordinal);

    private void AddSingleLanguageEntry(TextFileEntry entry)
    {
        var key = entry.Key.Value;
        if (entry.Text is not { } text)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.LanguagesNotSpecified, CreateLocation(entry.ValueSpan), key));
            return;
        }

        var template = ParseTemplate(key, string.Empty, text);
        if (template is not null)
        {
            AddEntryModel(key, null, template.Names, [new TextModel(0, template.Display, template.BuildValue(template.Names))]);
        }
    }

    private void AddMultiLanguageEntry(TextFileEntry entry)
    {
        var key = entry.Key.Value;
        var values = new TextToken?[languages.Count];
        values[0] = entry.Text;

        string? comment = null;
        foreach (var member in entry.Members)
        {
            var name = member.Name.Value;
            if ((name.Length > 0) && (name[0] == ReservedPrefix))
            {
                comment = name == CommentName ? member.Value.Value : comment;
                continue;
            }

            var index = IndexOfLanguage(NormalizeLanguage(name));
            if (index < 0)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnknownLanguage, CreateLocation(member.Name.Span), key, name));
            }
            else if (values[index] is not null)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.DuplicateKey, CreateLocation(member.Name.Span), $"{key}.{name}"));
            }
            else
            {
                values[index] = member.Value;
            }
        }

        if (values[0] is not { } defaultValue)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.DefaultLanguageMissing, CreateLocation(entry.Key.Span), key, languages[0]));
            return;
        }

        var template = ParseTemplate(key, languages[0], defaultValue);
        if (template is null)
        {
            return;
        }

        var texts = new List<TextModel> { new(0, template.Display, template.BuildValue(template.Names)) };
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] is not { } value)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.TranslationMissing, CreateLocation(entry.Key.Span), key, languages[i]));
                continue;
            }

            var other = ParseTemplate(key, languages[i], value);
            if (other is null)
            {
                continue;
            }

            if (!template.HasSameNames(other))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.PlaceholderMismatch, CreateLocation(value.Span), key, languages[i]));
                continue;
            }

            texts.Add(new TextModel(i, other.Display, other.BuildValue(template.Names)));
        }

        AddEntryModel(key, comment, template.Names, texts);
    }

    private int IndexOfLanguage(string language)
    {
        for (var i = 0; i < languages.Count; i++)
        {
            if (languages[i] == language)
            {
                return i;
            }
        }

        return -1;
    }

    private TextTemplate? ParseTemplate(string key, string language, TextToken value)
    {
        var template = TextTemplate.Parse(value.Value);
        if (template is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidPlaceholder, CreateLocation(value.Span), key, language));
        }

        return template;
    }

    private void AddEntryModel(string key, string? comment, IEnumerable<string> parameters, IEnumerable<TextModel> texts) =>
        entries.Add(new EntryModel(key, comment, new EquatableArray<string>(parameters), new EquatableArray<TextModel>(texts)));

    private LocationInfo CreateLocation(TextSpan span) =>
        new(path, span, source.Lines.GetLinePositionSpan(span));
}
