namespace BunnyTail.TextResource.Tests;

using BunnyTail.TextResource.Tool.Components;

public sealed class CsvParserTests
{
    // ------------------------------------------------------------
    // Row
    // ------------------------------------------------------------

    [Theory]
    [InlineData("a,b\r\nc,d\r\n")]
    [InlineData("a,b\nc,d\n")]
    [InlineData("a,b\rc,d\r")]
    [InlineData("a,b\r\nc,d")]
    public void RowsAreSplitByAnyLineBreak(string text)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text);

        // Assert
        Assert.Null(content.Error);
        Assert.Equal(2, content.Rows.Count);
        Assert.Equal(["a", "b"], content.Rows[0].Cells);
        Assert.Equal(["c", "d"], content.Rows[1].Cells);
    }

    [Fact]
    public void RowNumberCountsEveryRecordOnce()
    {
        // Arrange & Act
        var content = CsvParser.Parse("a\r\n\r\n,,\r\n\"b\nc\"\r\nd\r\n");

        // Assert
        Assert.Equal([1, 2, 3, 4, 5], content.Rows.Select(static x => x.Number));
    }

    [Fact]
    public void EmptyTextHasNoRow()
    {
        // Arrange & Act
        var content = CsvParser.Parse(string.Empty);

        // Assert
        Assert.Null(content.Error);
        Assert.Empty(content.Rows);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(",,", true)]
    [InlineData("\"\",\"\"", true)]
    [InlineData(" ,", false)]
    [InlineData("a,", false)]
    public void IsEmptyMeansAllCellsAreEmpty(string text, bool expected)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text + "\r\nnext");

        // Assert
        Assert.Equal(expected, content.Rows[0].IsEmpty);
    }

    [Theory]
    [InlineData("# comment,,", true)]
    [InlineData(" # comment", true)]
    [InlineData("\"# comment\"", true)]
    [InlineData("a,# comment", false)]
    [InlineData("a#", false)]
    public void IsCommentMeansFirstCellStartsWithSharp(string text, bool expected)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text);

        // Assert
        Assert.Equal(expected, content.Rows[0].IsComment);
    }

    // ------------------------------------------------------------
    // Cell
    // ------------------------------------------------------------

    [Fact]
    public void TrailingDelimiterMakesEmptyCell()
    {
        // Arrange & Act
        var content = CsvParser.Parse("a,");

        // Assert
        Assert.Equal(["a", string.Empty], content.Rows[0].Cells);
    }

    [Fact]
    public void QuotedCellKeepsDelimiterAndQuote()
    {
        // Arrange & Act
        var content = CsvParser.Parse("\"a,b\",\"c\"\"d\",\"\"");

        // Assert
        Assert.Equal(["a,b", "c\"d", string.Empty], content.Rows[0].Cells);
    }

    [Theory]
    [InlineData("\"a\nb\"")]
    [InlineData("\"a\r\nb\"")]
    [InlineData("\"a\rb\"")]
    public void LineBreakInCellIsNormalizedToLineFeed(string text)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text);

        // Assert
        Assert.Equal("a\nb", Assert.Single(Assert.Single(content.Rows).Cells));
    }

    [Theory]
    [InlineData("a\"b", "a\"b")]
    [InlineData(" \"a\"", " \"a\"")]
    [InlineData("a\"", "a\"")]
    public void QuoteInUnquotedCellIsText(string text, string expected)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text);

        // Assert
        Assert.Null(content.Error);
        Assert.Equal(expected, Assert.Single(Assert.Single(content.Rows).Cells));
    }

    [Fact]
    public void SpacesAreKept()
    {
        // Arrange & Act
        var content = CsvParser.Parse(" a , b ");

        // Assert
        Assert.Equal([" a ", " b "], content.Rows[0].Cells);
    }

    // ------------------------------------------------------------
    // Error
    // ------------------------------------------------------------

    [Fact]
    public void UnclosedQuoteIsError()
    {
        // Arrange & Act
        var content = CsvParser.Parse("a\r\n\"b\r\nc\r\nd");

        // Assert
        Assert.Equal("Quote is not closed.", content.Error);
        Assert.Equal(2, content.ErrorRow);
    }

    [Theory]
    [InlineData("a\r\n\"b\"c\r\n")]
    [InlineData("a\r\n\"b\" ,c\r\n")]
    public void TextAfterClosingQuoteIsError(string text)
    {
        // Arrange & Act
        var content = CsvParser.Parse(text);

        // Assert
        Assert.Equal("Text follows the closing quote.", content.Error);
        Assert.Equal(2, content.ErrorRow);
    }
}
