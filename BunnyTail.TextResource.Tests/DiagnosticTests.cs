namespace BunnyTail.TextResource.Tests;

using System.Reflection;

using BunnyTail.TextResource.Generator;

using Microsoft.CodeAnalysis;

public sealed class DiagnosticTests
{
    // ------------------------------------------------------------
    // Text file
    // ------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{ \"Hello\" }")]
    [InlineData("{ \"Hello\": 1 }")]
    [InlineData("{ \"Hello\": { \"ja\": 1 } }")]
    [InlineData("{ \"Hello\": \"a\" \"World\": \"b\" }")]
    [InlineData("{ \"Hello\": \"a\"")]
    [InlineData("{ \"Hello\": \"a\n\" }")]
    [InlineData("{ \"Hello\": \"\\x\" }")]
    [InlineData("{ \"Hello\": \"\\u00\" }")]
    [InlineData("{ \"Hello\": \"\\u00G0\" }")]
    [InlineData("{ } { }")]
    public void Bttr0001InvalidJsonEmitsDiagnostic(string text)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(text);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0001");
    }

    [Fact]
    public void Bttr0001UnreadableFileEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnosticsOfUnreadableFile();

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0001");
    }

    [Fact]
    public void Bttr0001KeepsEntriesBeforeError()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": "a", "World": 1 }""", null);
        var source = GeneratorTestHelper.GetGeneratedSource("""{ "Hello": "a", "World": 1 }""", null);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0001");
        Assert.Contains("public static string Hello", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Bttr0001ReportsLocationInFile()
    {
        // Arrange
        const string text = "{\n  \"Hello\": 1\n}";

        // Act
        var location = GeneratorTestHelper.GetDiagnostics(text).Single(static x => x.Id == "BTTR0001").Location;

        // Assert
        Assert.Equal(GeneratorTestHelper.MessagesPath, location.GetLineSpan().Path);
        Assert.Equal(1, location.GetLineSpan().StartLinePosition.Line);
        Assert.Equal(11, location.GetLineSpan().StartLinePosition.Character);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Hello")]
    [InlineData("Hello-World")]
    [InlineData("class")]
    [InlineData("Keys")]
    [InlineData("GetText")]
    [InlineData("GetTextFor")]
    [InlineData("Messages")]
    [InlineData("Equals")]
    [InlineData("Finalize")]
    [InlineData("GetHashCode")]
    [InlineData("GetType")]
    [InlineData("MemberwiseClone")]
    [InlineData("ReferenceEquals")]
    [InlineData("ToString")]
    [InlineData("__Language")]
    [InlineData("__Lookup")]
    public void Bttr0002InvalidKeyEmitsDiagnostic(string key)
    {
        // Arrange
        var text = $$"""{ "{{key}}": { "ja": "a", "en": "b" } }""";

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(text);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0002");
    }

    [Theory]
    [InlineData("Index")]
    [InlineData("Find")]
    [InlineData("Current")]
    [InlineData("Resolve")]
    [InlineData("String")]
    [InlineData("System")]
    [InlineData("var")]
    [InlineData("key")]
    [InlineData("名前")]
    public void Bttr0002ValidKeyEmitsNoDiagnostic(string key)
    {
        // Arrange
        var text = $$"""{ "{{key}}": { "ja": "a {x}", "en": "b {x}" } }""";

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(text);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Bttr0002ReservedNameIsIgnored()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "$schema": "a", "$other": { "x": "y" } }""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Bttr0003DuplicateKeyEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": "a", "Hello": "b" }""", null);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0003");
    }

    [Fact]
    public void Bttr0003DuplicateLanguageEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "ja": "a", "JA": "b", "en": "c" } }""");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0003");
    }

    [Fact]
    public void Bttr0004InvalidClassNameEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("{ }", path: "C:\\App\\My-Messages.texts.json");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0004");
    }

    [Fact]
    public void Bttr0004KeywordClassNameEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("{ }", path: "C:\\App\\class.texts.json");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0004");
    }

    [Fact]
    public void Bttr0004EmptyClassNameEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("{ }", path: "C:\\App\\.texts.json");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0004");
    }

    [Fact]
    public void Bttr0004DuplicateClassNameEmitsDiagnostic()
    {
        // Arrange
        var runner = GeneratorTestHelper.CreateRunner()
            .WithAdditionalText("C:\\App\\A.B\\Messages.texts.json", "{ }")
            .WithAdditionalText("C:\\App\\A\\B\\Messages.texts.json", "{ }");

        // Act
        var diagnostics = runner.GetDiagnostics(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Equal(2, diagnostics.Count(static x => x.Id == "BTTR0004"));
    }

    [Fact]
    public void Bttr0004SameNameInOtherNamespaceEmitsNoDiagnostic()
    {
        // Arrange
        var runner = GeneratorTestHelper.CreateRunner()
            .WithAdditionalText("C:\\App\\A\\Messages.texts.json", "{ }")
            .WithAdditionalText("C:\\App\\B\\Messages.texts.json", "{ }");

        // Act
        var diagnostics = runner.GetDiagnostics(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------
    // Language
    // ------------------------------------------------------------

    [Theory]
    [InlineData("ja,,en")]
    [InlineData("ja,ja")]
    [InlineData("ja,JA")]
    [InlineData("j")]
    [InlineData("ja_JP")]
    [InlineData("ja-")]
    [InlineData("toolonglanguage")]
    public void Bttr0005InvalidLanguagesEmitsDiagnostic(string languages)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("{ }", languages);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0005");
    }

    [Theory]
    [InlineData("ja")]
    [InlineData(" ja , en ")]
    [InlineData("ja,en-US,zh-Hant,zh-Hant-TW,es-419")]
    public void Bttr0005ValidLanguagesEmitsNoDiagnostic(string languages)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("{ }", languages);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Bttr0005IsReportedWithoutTextFile()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.CreateRunner("ja,ja").GetDiagnostics(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0005");
    }

    [Fact]
    public void Bttr0006ObjectValueWithoutLanguagesEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "ja": "a" } }""", null);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0006");
    }

    [Fact]
    public void Bttr0007DefaultLanguageMissingEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "en": "a" } }""");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0007");
    }

    [Fact]
    public void Bttr0008TranslationMissingEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "ja": "a" } }""");

        // Assert
        Assert.Contains(diagnostics, static x => (x.Id == "BTTR0008") && (x.Severity == DiagnosticSeverity.Warning));
    }

    [Fact]
    public void Bttr0008StringValueEmitsDiagnosticForOtherLanguages()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": "a" }""", "ja,en,fr");

        // Assert
        Assert.Equal(2, diagnostics.Count(static x => x.Id == "BTTR0008"));
    }

    [Fact]
    public void Bttr0009UnknownLanguageEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "ja": "a", "en": "b", "jp": "c" } }""");

        // Assert
        Assert.Contains(diagnostics, static x => (x.Id == "BTTR0009") && (x.Severity == DiagnosticSeverity.Warning));
    }

    [Fact]
    public void Bttr0009LanguageCaseIsNormalized()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics("""{ "Hello": { "JA": "a", "zh-hant": "b" } }""", "ja,zh-Hant");

        // Assert
        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------
    // Placeholder
    // ------------------------------------------------------------

    [Theory]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{}")]
    [InlineData("{a")]
    [InlineData("a}")]
    [InlineData("{a}}")]
    [InlineData("{{a}")]
    [InlineData("{a{b}")]
    [InlineData("{a b}")]
    [InlineData("{ a }")]
    [InlineData("{1a}")]
    [InlineData("{a-b}")]
    [InlineData("{__a}")]
    [InlineData("{a,}")]
    [InlineData("{a,b}")]
    [InlineData("{a,+1}")]
    [InlineData("{a,1000000}")]
    [InlineData("{1000000}")]
    [InlineData("{0} {a}")]
    public void Bttr0010InvalidPlaceholderEmitsDiagnostic(string value)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics($$"""{ "Hello": "{{value}}" }""", null);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0010");
    }

    [Theory]
    [InlineData("{{")]
    [InlineData("}}")]
    [InlineData("{{a}}")]
    [InlineData("{a}")]
    [InlineData("{0}")]
    [InlineData("{1} {0}")]
    [InlineData("{a} {a}")]
    [InlineData("{a:N0}")]
    [InlineData("{a,8}")]
    [InlineData("{a,-8}")]
    [InlineData("{a, 8 :N0}")]
    [InlineData("{a:#,##0}")]
    [InlineData("{a:}")]
    [InlineData("{{{a}}}")]
    [InlineData("{class}")]
    [InlineData("{key}")]
    [InlineData("{名前}")]
    public void Bttr0010ValidPlaceholderEmitsNoDiagnostic(string value)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics($$"""{ "Hello": "{{value}}" }""", null);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Bttr0010InvalidTranslationKeepsMember()
    {
        // Arrange
        const string text = """{ "Hello": { "ja": "{a}", "en": "{" } }""";

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(text);
        var source = GeneratorTestHelper.GetGeneratedSource(text);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0010");
        Assert.Contains("public static string Hello(object? a)", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{a}", "{b}")]
    [InlineData("{a}", "b")]
    [InlineData("a", "{b}")]
    [InlineData("{a} {b}", "{a}")]
    [InlineData("{a}", "{0}")]
    [InlineData("{0}", "{1}")]
    public void Bttr0011PlaceholderMismatchEmitsDiagnostic(string first, string second)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics($$"""{ "Hello": { "ja": "{{first}}", "en": "{{second}}" } }""");

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0011");
    }

    [Theory]
    [InlineData("{a} {b}", "{b} {a}")]
    [InlineData("{a}", "{a:N0}")]
    [InlineData("{0} {1}", "{1} {0}")]
    public void Bttr0011SamePlaceholdersEmitsNoDiagnostic(string first, string second)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics($$"""{ "Hello": { "ja": "{{first}}", "en": "{{second}}" } }""");

        // Assert
        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------
    // Option
    // ------------------------------------------------------------

    [Fact]
    public void Bttr0012InvalidAccessibilityEmitsDiagnostic()
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.CreateRunner(accessibility: "protected").GetDiagnostics(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0012");
    }

    [Theory]
    [InlineData("")]
    [InlineData("public")]
    [InlineData("Public")]
    [InlineData("internal")]
    public void Bttr0012ValidAccessibilityEmitsNoDiagnostic(string accessibility)
    {
        // Arrange & Act
        var diagnostics = GeneratorTestHelper.CreateRunner(accessibility: accessibility).GetDiagnostics(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Empty(diagnostics);
    }

    // ------------------------------------------------------------
    // Descriptor
    // ------------------------------------------------------------

    [Fact]
    public void ErrorsCannotBeSuppressed()
    {
        // Arrange
        var descriptors = typeof(TextResourceGenerator).Assembly.GetType("BunnyTail.TextResource.Generator.Diagnostics")!
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(static x => x.PropertyType == typeof(DiagnosticDescriptor))
            .Select(static x => (DiagnosticDescriptor)x.GetValue(null)!)
            .ToList();

        // Act & Assert
        Assert.NotEmpty(descriptors);
        foreach (var descriptor in descriptors)
        {
            Assert.Equal(
                descriptor.DefaultSeverity == DiagnosticSeverity.Error,
                descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable));
        }
    }
}
