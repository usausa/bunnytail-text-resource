namespace BunnyTail.TextResource.Tests;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTests
{
    private const string Text =
        """
        {
          "Hello": { "ja": "こんにちは", "en": "Hello" }
        }
        """;

    private const string EditedText =
        """
        {
          "Hello": { "ja": "こんにちは", "en": "Hello!" }
        }
        """;

    private const string OtherPath = "C:\\App\\Resources\\Other.texts.json";

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    // ------------------------------------------------------------
    // Cache
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Text, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var reasons = GeneratorTestHelper.RunTextEdit(Text, EditedText);

        // Assert
        Assert.Contains(reasons, static x => x.IsChanged());
    }

    [Fact]
    public void SameTextKeepsModelCached()
    {
        // Arrange & Act
        var reasons = GeneratorTestHelper.RunTextEdit(Text, Text);

        // Assert
        Assert.NotEmpty(reasons);
        Assert.DoesNotContain(reasons, static x => x.IsChanged());
    }

    [Fact]
    public void TargetEditKeepsOtherFileCached()
    {
        // Arrange & Act
        var reasons = GeneratorTestHelper.RunTextEdit(Text, EditedText, (OtherPath, Text));

        // Assert
        Assert.Single(reasons, static x => x.IsChanged());
        Assert.Contains(reasons, static x => !x.IsChanged());
    }
}
