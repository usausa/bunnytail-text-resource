namespace BunnyTail.TextResource.Tests;

using System.Text;

using BunnyTail.TextResource.Tool.Components;

using Microsoft.CodeAnalysis;

public sealed class TextCsvConverterTests
{
    private const string FilePath = "C:\\App\\Resources\\Messages.texts.csv";

    private static string Lines(params string[] lines) =>
        String.Concat(lines.Select(static x => x + "\r\n"));

    private static TextCsvResult Convert(params string[] lines) =>
        TextCsvConverter.Convert(Encoding.UTF8.GetBytes(Lines(lines)), FilePath);

    private static string ConvertToJson(params string[] lines)
    {
        var result = Convert(lines);
        Assert.Empty(result.Errors);
        return result.Json;
    }

    private static string ConvertToError(params string[] lines) =>
        Assert.Single(Convert(lines).Errors);

    // ------------------------------------------------------------
    // Multi language
    // ------------------------------------------------------------

    [Fact]
    public void MultiLanguageWritesObjectPerKey()
    {
        // Arrange & Act
        var result = Convert(
            "key,ja,en,$comment",
            "# 入力画面",
            "NameRequired,名前を入力してください。,Please enter a name.,入力の検証",
            "NameMaxLength,名前は{max}文字以内で入力してください。,Name must be {max} characters or less.,",
            "Greeting,\"こんにちは、\n{name}さん\",\"Hello,\n{name}\",",
            "Symbols,\"記号 \"\"引用\"\" {{波かっこ}}\",,");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(4, result.KeyCount);
        Assert.Equal(
            Lines(
                "// Generated from Messages.texts.csv",
                "{",
                "  \"NameRequired\": { \"ja\": \"名前を入力してください。\", \"en\": \"Please enter a name.\", \"$comment\": \"入力の検証\" },",
                "  \"NameMaxLength\": { \"ja\": \"名前は{max}文字以内で入力してください。\", \"en\": \"Name must be {max} characters or less.\" },",
                "  \"Greeting\": { \"ja\": \"こんにちは、\\n{name}さん\", \"en\": \"Hello,\\n{name}\" },",
                "  \"Symbols\": { \"ja\": \"記号 \\\"引用\\\" {{波かっこ}}\" }",
                "}"),
            result.Json);
    }

    [Fact]
    public void ColumnOrderIsKeptAndCommentIsLast()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,$comment,en,ja",
            "A,c,e,j");

        // Assert
        Assert.Contains("\"A\": { \"en\": \"e\", \"ja\": \"j\", \"$comment\": \"c\" }", json, StringComparison.Ordinal);
    }

    [Fact]
    public void RowWithoutValuesWritesEmptyObject()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja,en",
            "A,,");

        // Assert
        Assert.Contains("\"A\": { }", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortRowTreatsMissingCellsAsEmpty()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja,en",
            "A,j");

        // Assert
        Assert.Contains("\"A\": { \"ja\": \"j\" }", json, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Single language
    // ------------------------------------------------------------

    [Fact]
    public void TextColumnWritesStringAndCommentLine()
    {
        // Arrange & Act
        var result = Convert(
            "key,$text,$comment",
            "NameRequired,名前を入力してください。,\"入力の\n検証\"",
            "ErrorOccurred,エラーが発生しました。,");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(
            Lines(
                "// Generated from Messages.texts.csv",
                "{",
                "  // 入力の 検証",
                "  \"NameRequired\": \"名前を入力してください。\",",
                "  \"ErrorOccurred\": \"エラーが発生しました。\"",
                "}"),
            result.Json);
    }

    // ------------------------------------------------------------
    // Rows and columns
    // ------------------------------------------------------------

    [Fact]
    public void HeaderOnlyWritesEmptyTable()
    {
        // Arrange & Act
        var result = Convert("key,ja");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.KeyCount);
        Assert.Equal(Lines("// Generated from Messages.texts.csv", "{", "}"), result.Json);
    }

    [Fact]
    public void EmptyAndCommentRowsAreSkippedBeforeAndAfterHeader()
    {
        // Arrange & Act
        var result = Convert(
            "# first",
            ",,",
            "key,ja,en",
            string.Empty,
            "# second,x,y",
            ",,",
            "A,j,e");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(1, result.KeyCount);
    }

    [Fact]
    public void HeaderIsCaseInsensitiveAndTrimmed()
    {
        // Arrange & Act
        var json = ConvertToJson(
            " KEY , ja ,$COMMENT",
            "A,j,c");

        // Assert
        Assert.Contains("\"A\": { \"ja\": \"j\", \"$comment\": \"c\" }", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservedColumnIsIgnored()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja,$memo",
            "A,j,memo");

        // Assert
        Assert.DoesNotContain("memo", json, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyCellsRightOfHeaderAreIgnored()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja,,",
            "A,j,,,,");

        // Assert
        Assert.Contains("\"A\": { \"ja\": \"j\" }", json, StringComparison.Ordinal);
    }

    [Fact]
    public void KeyIsTrimmedAndTextIsNot()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja",
            " A , j ");

        // Assert
        Assert.Contains("\"A\": { \"ja\": \" j \" }", json, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Text
    // ------------------------------------------------------------

    [Fact]
    public void BackslashIsNotEscapeInCsv()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja",
            "A,a\\nb");

        // Assert
        Assert.Contains("\"ja\": \"a\\\\nb\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void NonAsciiIsNotEscaped()
    {
        // Arrange & Act
        var json = ConvertToJson(
            "key,ja",
            "A,😀𠮷ﾊﾝｶｸ＆<>");

        // Assert
        Assert.Contains("\"ja\": \"😀𠮷ﾊﾝｶｸ＆<>\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ControlCharactersAreEscaped()
    {
        // Arrange
        var text = "a\tb" + (char)1 + (char)0x85 + (char)0x2028 + (char)0x2029;

        // Act
        var json = ConvertToJson(
            "key,ja",
            "A," + text);

        // Assert
        Assert.Contains("\"ja\": \"a\\tb" + "\\" + "u0001" + "\\" + "u0085" + "\\" + "u2028" + "\\" + "u2029\"", json, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Encoding
    // ------------------------------------------------------------

    [Fact]
    public void BomIsAccepted()
    {
        // Arrange
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(Lines("key,ja", "A,あ"))).ToArray();

        // Act
        var result = TextCsvConverter.Convert(bytes, FilePath);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Contains("\"ja\": \"あ\"", result.Json, StringComparison.Ordinal);
    }

    [Fact]
    public void Cp932IsError()
    {
        // Arrange
        var bytes = new byte[] { 0x6B, 0x65, 0x79, 0x2C, 0x6A, 0x61, 0x0D, 0x0A, 0x41, 0x2C, 0x82, 0xA0 };

        // Act
        var result = TextCsvConverter.Convert(bytes, FilePath);

        // Assert
        Assert.Contains("File is not UTF-8.", Assert.Single(result.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void SameCsvWritesSameJson()
    {
        // Arrange
        string[] lines = ["key,ja,en", "A,j,e", "B,j,"];

        // Act & Assert
        Assert.Equal(ConvertToJson(lines), ConvertToJson(lines));
    }

    // ------------------------------------------------------------
    // Error
    // ------------------------------------------------------------

    [Fact]
    public void EmptyFileIsError()
    {
        // Arrange & Act
        var error = ConvertToError(string.Empty);

        // Assert
        Assert.Equal($"Header is not found. file=[{FilePath}]", error);
    }

    [Fact]
    public void UnclosedQuoteIsErrorWithRow()
    {
        // Arrange & Act
        var error = ConvertToError("key,ja", "A,\"j");

        // Assert
        Assert.Equal($"Quote is not closed. file=[{FilePath}], row=[2]", error);
    }

    [Fact]
    public void FirstColumnMustBeKey()
    {
        // Arrange & Act
        var error = ConvertToError("ja,key", "j,A");

        // Assert
        Assert.Equal($"First column is not key. file=[{FilePath}], column=[ja]", error);
    }

    [Theory]
    [InlineData("key,ja,JA", "JA")]
    [InlineData("key,zh-hant,zh-Hant", "zh-Hant")]
    [InlineData("key,ja,$comment,$COMMENT", "$COMMENT")]
    [InlineData("key,ja,key", "key")]
    public void DuplicateColumnIsError(string header, string column)
    {
        // Arrange & Act
        var error = ConvertToError(header, "A,j");

        // Assert
        Assert.Equal($"Column is duplicated. file=[{FilePath}], column=[{column}]", error);
    }

    [Theory]
    [InlineData("ja_JP")]
    [InlineData("備考")]
    [InlineData("j")]
    [InlineData("ja-")]
    [InlineData("toolonglanguage")]
    public void InvalidLanguageColumnIsError(string column)
    {
        // Arrange & Act
        var error = ConvertToError($"key,ja,{column}", "A,j");

        // Assert
        Assert.Equal($"Column is not a language code. file=[{FilePath}], column=[{column}]", error);
    }

    [Theory]
    [InlineData("key")]
    [InlineData("key,$comment")]
    [InlineData("key,$memo,")]
    public void MissingLanguageColumnIsError(string header)
    {
        // Arrange & Act
        var error = ConvertToError(header, "A");

        // Assert
        Assert.Equal($"Language column is not found. file=[{FilePath}]", error);
    }

    [Fact]
    public void TextAndLanguageColumnsAreError()
    {
        // Arrange & Act
        var error = ConvertToError("key,$text,ja", "A,t,j");

        // Assert
        Assert.Equal($"$text and language columns are mixed. file=[{FilePath}]", error);
    }

    [Fact]
    public void EmptyKeyIsErrorWithExcelRow()
    {
        // Arrange & Act
        var error = ConvertToError(
            "key,ja",
            "A,\"j\nj\"",
            string.Empty,
            ",value");

        // Assert
        Assert.Equal($"Key is empty. file=[{FilePath}], row=[4]", error);
    }

    [Fact]
    public void DuplicateKeyIsError()
    {
        // Arrange & Act
        var error = ConvertToError(
            "key,ja",
            "A,j",
            "A,j");

        // Assert
        Assert.Equal($"Key is duplicated. file=[{FilePath}], row=[3], key=[A]", error);
    }

    [Theory]
    [InlineData("key,ja", "A,j,x")]
    [InlineData("key,,ja", "A,x,j")]
    public void CellWithoutColumnIsError(string header, string row)
    {
        // Arrange & Act
        var error = ConvertToError(header, row);

        // Assert
        Assert.Equal($"Cell has no column. file=[{FilePath}], row=[2], value=[x]", error);
    }

    [Fact]
    public void EmptyTextColumnIsError()
    {
        // Arrange & Act
        var error = ConvertToError("key,$text,$comment", "A,,c");

        // Assert
        Assert.Equal($"Text is empty. file=[{FilePath}], row=[2], key=[A]", error);
    }

    [Fact]
    public void AllErrorsAreReported()
    {
        // Arrange & Act
        var result = Convert(
            "key,ja",
            "A,j",
            "A,j",
            ",j",
            "B,j,x");

        // Assert
        Assert.Equal(3, result.Errors.Count);
        Assert.Empty(result.Json);
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    [Fact]
    public void GeneratorReadsMultiLanguageJson()
    {
        // Arrange
        var json = ConvertToJson(
            "key,en,ja,$comment",
            "# 見本",
            "Hello,Hello,こんにちは,Greeting",
            "Count,{count} items,{count} 件,",
            "Symbols,\"<&\"\"\\>\n\",\"{{波}}😀\",");

        // Act
        var result = GeneratorTestHelper.CreateRunner("en,ja")
            .WithAdditionalText(GeneratorTestHelper.MessagesPath, json)
            .Run(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("public static string Hello =>", result.FirstGeneratedSource, StringComparison.Ordinal);
        Assert.Contains("public static string Count(object? count) =>", result.FirstGeneratedSource, StringComparison.Ordinal);
        Assert.Contains("/// <para>Greeting</para>", result.FirstGeneratedSource, StringComparison.Ordinal);
        Assert.Contains("_ => \"<&\\\"\\\\>\\n\"", result.FirstGeneratedSource, StringComparison.Ordinal);
        Assert.Contains("1 => \"{波}😀\"", result.FirstGeneratedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorReadsSingleLanguageJson()
    {
        // Arrange
        var json = ConvertToJson(
            "key,$text,$comment",
            "Hello,こんにちは,Greeting",
            "Count,{count} 件,");

        // Act
        var result = GeneratorTestHelper.CreateRunner(null)
            .WithAdditionalText(GeneratorTestHelper.MessagesPath, json)
            .Run(GeneratorTestHelper.EmptySource);

        // Assert
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("/// <summary>こんにちは</summary>", result.FirstGeneratedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorReportsEmptyCellAsMissingTranslation()
    {
        // Arrange
        var json = ConvertToJson(
            "key,ja,en",
            "A,j,",
            "B,,e");

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(json);

        // Assert
        Assert.Contains(diagnostics, static x => (x.Id == "BTTR0008") && (x.Severity == DiagnosticSeverity.Warning));
        Assert.Contains(diagnostics, static x => x.Id == "BTTR0007");
    }
}
