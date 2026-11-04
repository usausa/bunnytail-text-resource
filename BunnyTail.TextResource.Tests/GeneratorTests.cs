namespace BunnyTail.TextResource.Tests;

using Microsoft.CodeAnalysis.CSharp;

public sealed class GeneratorTests
{
    private const string MultiLanguageText =
        """
        {
          "Hello": { "ja": "こんにちは", "en": "Hello", "$comment": "Greeting" },
          "Count": { "ja": "{count} 件", "en": "{count} items" }
        }
        """;

    private const string SingleLanguageText =
        """
        {
          "Hello": "こんにちは",
          "Count": "{count} 件"
        }
        """;

    // ------------------------------------------------------------
    // Class
    // ------------------------------------------------------------

    [Fact]
    public void GeneratesStaticClassWithoutPartial()
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(MultiLanguageText);

        // Assert
        Assert.Contains("internal static class Messages", source, StringComparison.Ordinal);
        Assert.DoesNotContain("partial", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessibilityOptionGeneratesPublicClass()
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(MultiLanguageText, accessibility: "public");

        // Assert
        Assert.Contains("public static class Messages", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("C:\\App\\Messages.texts.json", "App")]
    [InlineData("C:\\App\\Resources\\Strings\\Messages.texts.json", "App.Resources.Strings")]
    [InlineData("C:/App/Resources/Messages.texts.json", "App.Resources")]
    [InlineData("C:\\APP\\Resources\\Messages.texts.json", "App.Resources")]
    [InlineData("C:\\App\\A.B\\Messages.texts.json", "App.A.B")]
    [InlineData("C:\\App\\My Folder\\1st\\a-b\\Messages.texts.json", "App.My_Folder._1st.a_b")]
    [InlineData("C:\\App\\class\\Messages.texts.json", "App.@class")]
    [InlineData("C:\\Other\\Messages.texts.json", "App")]
    [InlineData("C:\\App2\\Resources\\Messages.texts.json", "App")]
    public void NamespaceIsRootNamespaceAndFolder(string path, string expected)
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(MultiLanguageText, path: path);

        // Assert
        Assert.Contains($"namespace {expected}\n", source.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyNamespaceGeneratesGlobalClass()
    {
        // Arrange
        var runner = GeneratorTestHelper.CreateRunner()
            .WithGlobalOption("build_property.RootNamespace", string.Empty)
            .WithAdditionalText("C:\\App\\Messages.texts.json", MultiLanguageText);

        // Act
        var source = runner.GetGeneratedSource(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.DoesNotContain("namespace", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FilesGenerateOwnClasses()
    {
        // Arrange & Act
        var sources = GeneratorTestHelper.GetGeneratedSources(
            ("C:\\App\\A\\Messages.texts.json", MultiLanguageText),
            ("C:\\App\\B\\Messages.texts.json", MultiLanguageText),
            ("C:\\App\\B\\Names.texts.json", MultiLanguageText));

        // Assert
        Assert.Equal(3, sources.Count);
        Assert.Contains("App_A_Messages.g.cs", sources.Keys);
        Assert.Contains("App_B_Messages.g.cs", sources.Keys);
        Assert.Contains("App_B_Names.g.cs", sources.Keys);
    }

    [Fact]
    public void OtherAdditionalFilesAreIgnored()
    {
        // Arrange
        var runner = GeneratorTestHelper.CreateRunner()
            .WithAdditionalText("C:\\App\\appsettings.json", "not json");

        // Act
        var result = runner.Run(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Empty(result.GeneratedSources);
        Assert.Empty(result.GeneratorDiagnostics);
    }

    // ------------------------------------------------------------
    // Multi language
    // ------------------------------------------------------------

    [Fact]
    public void MultiLanguageGeneratesSwitchOnLanguage()
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(MultiLanguageText);

        // Assert
        Assert.Contains("public static string Hello =>", source, StringComparison.Ordinal);
        Assert.Contains("1 => \"Hello\",", source, StringComparison.Ordinal);
        Assert.Contains("_ => \"こんにちは\"", source, StringComparison.Ordinal);
        Assert.Contains("public static string Count(object? count) =>", source, StringComparison.Ordinal);
        Assert.Contains("1 => \"{0} items\",", source, StringComparison.Ordinal);
        Assert.Contains("private static class __Language", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentContainsTextsAndComment()
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(MultiLanguageText);

        // Assert
        Assert.Contains("/// <para>ja: こんにちは</para>", source, StringComparison.Ordinal);
        Assert.Contains("/// <para>en: Hello</para>", source, StringComparison.Ordinal);
        Assert.Contains("/// <para>Greeting</para>", source, StringComparison.Ordinal);
        Assert.Contains("/// <para>ja: {count} 件</para>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentEscapesTextForXml()
    {
        // Arrange
        const string text = """{ "Hello": "<&>\r\na\u2028b\u0001c\td" }""";

        // Act
        var source = GeneratorTestHelper.GetGeneratedSource(text, null);

        // Assert
        Assert.Contains("/// <summary>&lt;&amp;&gt;<br/>a<br/>bc\td</summary>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingTranslationHasNoBranchAndFallsBackInLookup()
    {
        // Arrange
        const string text = """{ "Hello": { "ja": "こんにちは" } }""";

        // Act
        var source = GeneratorTestHelper.GetGeneratedSource(text).ReplaceLineEndings("\n");

        // Assert
        Assert.Contains("public static string Hello =>\n            \"こんにちは\";", source, StringComparison.Ordinal);
        Assert.Equal(3, source.Split("\"Hello\" => \"こんにちは\",").Length);
    }

    [Fact]
    public void LanguagesAreNormalizedInGeneratedCode()
    {
        // Arrange
        const string text = """{ "Hello": { "EN-us": "a", "zh-hant": "b" } }""";

        // Act
        var source = GeneratorTestHelper.GetGeneratedSource(text, "EN-us,zh-hant");

        // Assert
        Assert.Contains("\"en-US\" => 0,", source, StringComparison.Ordinal);
        Assert.Contains("\"zh-Hant\" => 1,", source, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Single language
    // ------------------------------------------------------------

    [Fact]
    public void SingleLanguageGeneratesLiteralWithoutLanguageClass()
    {
        // Arrange & Act
        var source = GeneratorTestHelper.GetGeneratedSource(SingleLanguageText, null).ReplaceLineEndings("\n");

        // Assert
        Assert.Contains("/// <summary>こんにちは</summary>", source, StringComparison.Ordinal);
        Assert.Contains("public static string Hello =>\n            \"こんにちは\";", source, StringComparison.Ordinal);
        Assert.Contains("public static string Count(object? count) =>", source, StringComparison.Ordinal);
        Assert.Contains("\"{0} 件\",", source, StringComparison.Ordinal);
        Assert.Contains("GetTextFor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("__Language", source, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Compilation
    // ------------------------------------------------------------

    [Theory]
    [InlineData("ja,en")]
    [InlineData("ja")]
    [InlineData(null)]
    public void EmptyTableCompilesWithoutWarning(string? languages)
    {
        // Arrange & Act
        var ids = GeneratorTestHelper.GetCompilationWarningIds("{ }", languages);

        // Assert
        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("ja,en", MultiLanguageText)]
    [InlineData(null, SingleLanguageText)]
    public void GeneratedCodeCompilesWithoutWarningInPublicClass(string? languages, string text)
    {
        // Arrange & Act
        var ids = GeneratorTestHelper.GetCompilationWarningIds(text, languages, "public");

        // Assert
        Assert.Empty(ids);
    }

    [Theory]
    [InlineData("ja,en", MultiLanguageText)]
    [InlineData(null, SingleLanguageText)]
    [InlineData("ja,en", "{ }")]
    [InlineData(null, "{ }")]
    public void GeneratedCodeCompilesWithCSharp8(string? languages, string text)
    {
        // Arrange & Act
        var ids = GeneratorTestHelper.GetCompilationWarningIds(text, languages, languageVersion: LanguageVersion.CSharp8);

        // Assert
        Assert.Empty(ids);
    }

    [Fact]
    public void KeysAndParametersDoNotConflictWithGeneratedMembers()
    {
        // Arrange
        const string text =
            """
            {
              "Index": "a",
              "Find": "b",
              "Current": "c",
              "Resolve": "d",
              "String": "e",
              "System": "f",
              "global": "g",
              "key": "h",
              "Parameters": "{key} {language} {culture} {string} {class} {System} {global}"
            }
            """;

        // Act
        var multiLanguage = GeneratorTestHelper.GetCompilationWarningIds(text, "ja");
        var singleLanguage = GeneratorTestHelper.GetCompilationWarningIds(text, null);

        // Assert
        Assert.Empty(multiLanguage);
        Assert.Empty(singleLanguage);
    }
}
