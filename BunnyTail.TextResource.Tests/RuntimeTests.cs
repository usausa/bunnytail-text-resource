namespace BunnyTail.TextResource.Tests;

using System.Globalization;

using BunnyTail.TextResource.Tests.Texts;

public sealed class RuntimeTests
{
    private sealed class NoParentCulture : CultureInfo
    {
        public NoParentCulture(string name)
            : base(name)
        {
        }

        public override CultureInfo Parent => InvariantCulture;
    }

    private static void RunWith(string culture, Action action)
    {
        var uiCulture = CultureInfo.CurrentUICulture;
        var formatCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = uiCulture;
            CultureInfo.CurrentCulture = formatCulture;
        }
    }

    // ------------------------------------------------------------
    // Language
    // ------------------------------------------------------------

    [Theory]
    [InlineData("ja", "ハローワールド")]
    [InlineData("ja-JP", "ハローワールド")]
    [InlineData("en", "Hello World")]
    [InlineData("en-US", "Hello World")]
    [InlineData("zh-Hant", "哈囉世界")]
    [InlineData("zh-TW", "哈囉世界")]
    [InlineData("zh-Hant-HK", "哈囉世界")]
    [InlineData("fr-FR", "ハローワールド")]
    [InlineData("", "ハローワールド")]
    public void PropertyReturnsTextOfCurrentUiCulture(string culture, string expected)
    {
        // Arrange & Act & Assert
        RunWith(culture, () => Assert.Equal(expected, Messages.HelloWorld));
    }

    [Fact]
    public void LanguageFollowsCultureChange()
    {
        // Arrange & Act & Assert
        RunWith("en-US", () =>
        {
            Assert.Equal("Index", Messages.Index);
            RunWith("ja-JP", () => Assert.Equal("索引", Messages.Index));
            Assert.Equal("Find", Messages.Find);
        });
    }

    // ------------------------------------------------------------
    // Format
    // ------------------------------------------------------------

    [Fact]
    public void MethodFormatsTextOfCurrentUiCulture()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("3 件", Messages.ItemCount(3)));
        RunWith("en-US", () => Assert.Equal("3 items", Messages.ItemCount(3)));
    }

    [Fact]
    public void ArgumentsFollowDefaultLanguageWhenWordOrderDiffers()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("1〜2", Messages.Range(1, 2)));
        RunWith("en-US", () => Assert.Equal("2 to 1", Messages.Range(1, 2)));
    }

    [Fact]
    public void NumberedArgumentsAreOrderedByNumber()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("b a", Messages.Numbered("a", "b")));
        RunWith("en-US", () => Assert.Equal("a b", Messages.Numbered(arg0: "a", arg1: "b")));
    }

    [Fact]
    public void FormatAndAlignmentAreApplied()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("1,235 円", Messages.Price(1234.6)));
        RunWith("en-US", () => Assert.Equal("1,234.50 yen", Messages.Price(1234.5)));
        RunWith("en-US", () => Assert.Equal("    1.50 yen", Messages.Price(1.5)));
    }

    [Fact]
    public void FormatUsesCurrentCulture()
    {
        // Arrange
        var culture = CultureInfo.CurrentCulture;

        // Act & Assert
        RunWith("en-US", () =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            try
            {
                Assert.Equal("1.234,50 yen", Messages.Price(1234.5).TrimStart());
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        });
    }

    [Fact]
    public void NullArgumentIsFormattedAsEmpty()
    {
        // Arrange & Act & Assert
        RunWith("en-US", () => Assert.Equal(" items", Messages.ItemCount(null)));
    }

    // ------------------------------------------------------------
    // Literal
    // ------------------------------------------------------------

    [Fact]
    public void BracesAreUnescaped()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () =>
        {
            Assert.Equal("{}", Messages.Brace);
            Assert.Equal("{1}", Messages.Escape(1));
        });
    }

    [Fact]
    public void SymbolsAndEscapesAreKept()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("<&\"\\>\n", Messages.Symbol));
        RunWith("zh-Hant", () => Assert.Equal("A", Messages.Symbol));
    }

    // ------------------------------------------------------------
    // Key
    // ------------------------------------------------------------

    [Fact]
    public void KeysContainsAllKeysInFileOrder()
    {
        // Arrange & Act & Assert
        Assert.Equal(
            ["HelloWorld", "ItemCount", "Range", "Numbered", "Price", "Escape", "Brace", "Symbol", "Index", "Find"],
            Messages.Keys);
    }

    [Fact]
    public void GetTextReturnsTextOfCurrentUiCulture()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("ハローワールド", Messages.GetText(nameof(Messages.HelloWorld))));
        RunWith("en-US", () => Assert.Equal("Hello World", Messages.GetText(nameof(Messages.HelloWorld))));
    }

    [Fact]
    public void GetTextReturnsTemplateOfFormattedText()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("{0} 件", Messages.GetText(nameof(Messages.ItemCount))));
    }

    [Fact]
    public void GetTextReturnsNullWhenKeyDoesNotExist()
    {
        // Arrange & Act & Assert
        Assert.Null(Messages.GetText("Unknown"));
        Assert.Null(Messages.GetTextFor("Unknown", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void GetTextForIgnoresCurrentUiCulture()
    {
        // Arrange & Act & Assert
        RunWith("ja-JP", () => Assert.Equal("Hello World", Messages.GetTextFor(nameof(Messages.HelloWorld), CultureInfo.GetCultureInfo("en-US"))));
    }

    [Fact]
    public void GetTextForFindsLanguageWithoutParentCulture()
    {
        // Arrange & Act & Assert
        Assert.Equal("Hello World", Messages.GetTextFor(nameof(Messages.HelloWorld), new NoParentCulture("en-US")));
        Assert.Equal("ハローワールド", Messages.GetTextFor(nameof(Messages.HelloWorld), new NoParentCulture("fr-FR")));
    }
}
