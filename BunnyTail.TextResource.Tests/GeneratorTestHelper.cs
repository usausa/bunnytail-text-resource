namespace BunnyTail.TextResource.Tests;

using System.Collections.Immutable;
using System.Text;

using BunnyTail.TextResource.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper.Testing;

internal static class GeneratorTestHelper
{
    public const string EmptySource = "// no user code";

    public const string ProjectDirectory = "C:\\App\\";

    public const string MessagesPath = "C:\\App\\Resources\\Messages.texts.json";

    private sealed class TestAdditionalText : AdditionalText
    {
        private readonly SourceText? text;

        public TestAdditionalText(string path, string? content)
        {
            Path = path;
            text = content is null ? null : SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default) => text;
    }

    public static GeneratorTestRunner CreateRunner(string? languages = "ja,en", string? accessibility = null)
    {
        var runner = GeneratorTestRunner
            .For<TextResourceGenerator>()
            .WithDiagnosticPrefix("BTTR")
            .WithGlobalOption("build_property.RootNamespace", "App")
            .WithGlobalOption("build_property.ProjectDir", ProjectDirectory)
            .VerifyCompiles();

        if (languages is not null)
        {
            runner.WithGlobalOption("build_property._TextResourceLanguages", languages);
        }

        if (accessibility is not null)
        {
            runner.WithGlobalOption("build_property.TextResourceAccessibility", accessibility);
        }

        return runner;
    }

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string text, string? languages = "ja,en", string path = MessagesPath) =>
        CreateRunner(languages).WithAdditionalText(path, text).GetDiagnostics(EmptySource);

    public static string GetGeneratedSource(string text, string? languages = "ja,en", string? accessibility = null, string path = MessagesPath) =>
        CreateRunner(languages, accessibility).WithAdditionalText(path, text).GetGeneratedSource(EmptySource);

    public static IReadOnlyList<string> GetCompilationWarningIds(string text, string? languages = "ja,en", string? accessibility = null, LanguageVersion? languageVersion = null)
    {
        var runner = CreateRunner(languages, accessibility).WithAdditionalText(MessagesPath, text);
        if (languageVersion is not null)
        {
            runner.WithLanguageVersion(languageVersion.Value);
        }

        return
        [
            .. runner.GetDiagnosticsAll(EmptySource)
                .Where(static x => x.Severity >= DiagnosticSeverity.Warning)
                .Select(static x => x.Id)
        ];
    }

    public static IReadOnlyList<Diagnostic> GetDiagnosticsOfUnreadableFile()
    {
        var (driver, compilation) = CreateRunner().CreateDriver(EmptySource);
        return driver.AddAdditionalTexts([new TestAdditionalText(MessagesPath, null)]).RunGenerators(compilation).GetRunResult().Diagnostics;
    }

    public static IncrementalRunResult RunIncremental(string text, string addedSource) =>
        CreateRunner().WithAdditionalText(MessagesPath, text).WithTracking().RunIncremental(EmptySource, addedSource);

    public static List<IncrementalStepRunReason> RunTextEdit(string text, string editedText, params (string Path, string Text)[] others)
    {
        var (driver, compilation) = CreateRunner().WithTracking().CreateDriver(EmptySource);

        var original = new TestAdditionalText(MessagesPath, text);
        driver = driver.AddAdditionalTexts([original, .. others.Select(static x => new TestAdditionalText(x.Path, x.Text))]);
        driver = driver.RunGenerators(compilation);
        driver = driver.ReplaceAdditionalText(original, new TestAdditionalText(MessagesPath, editedText));
        driver = driver.RunGenerators(compilation);

        return
        [
            .. driver.GetRunResult().Results
                .SelectMany(static x => x.TrackedOutputSteps)
                .SelectMany(static x => x.Value)
                .SelectMany(static x => x.Outputs)
                .Select(static x => x.Reason)
        ];
    }

    public static ImmutableDictionary<string, string> GetGeneratedSources(params (string Path, string Text)[] files)
    {
        var runner = CreateRunner();
        foreach (var (path, text) in files)
        {
            runner.WithAdditionalText(path, text);
        }

        return runner.Run(EmptySource).GeneratedSources.ToImmutableDictionary();
    }
}
