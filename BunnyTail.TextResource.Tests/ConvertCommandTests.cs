namespace BunnyTail.TextResource.Tests;

using System.Text;

using BunnyTail.TextResource.Tool.Commands;
using BunnyTail.TextResource.Tool.Components;

using Smart.CommandLine.Hosting;

public sealed class ConvertCommandTests : IDisposable
{
    private const string ValidCsv = "key,ja\r\nA,あ\r\n";

    private readonly string directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    public void Dispose() => Directory.Delete(directory, true);

    private string Write(string relativePath, string content)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static async Task<int> RunAsync(string path)
    {
        var command = new ConvertCommand { TargetPath = path };
        var context = new CommandContext(typeof(ConvertCommand), command, new EmptyServiceProvider(), TestContext.Current.CancellationToken);
        await command.ExecuteAsync(context).ConfigureAwait(true);
        return context.ExitCode;
    }

    // ------------------------------------------------------------
    // Find
    // ------------------------------------------------------------

    [Fact]
    public void FindReturnsFile()
    {
        // Arrange
        var path = Write("Messages.texts.csv", ValidCsv);

        // Act
        var files = TextCsvFinder.Find(path);

        // Assert
        Assert.Equal([path], files);
    }

    [Fact]
    public void FindRejectsOtherFileName()
    {
        // Arrange
        var path = Write("Messages.csv", ValidCsv);

        // Act
        var files = TextCsvFinder.Find(path);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void FindReturnsEmptyWhenPathDoesNotExist()
    {
        // Arrange & Act
        var files = TextCsvFinder.Find(Path.Combine(directory, "NotFound"));

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void FindSearchesFolderRecursivelyExceptBuildOutput()
    {
        // Arrange
        var first = Write("A.texts.csv", ValidCsv);
        var second = Write(Path.Combine("Resources", "Strings", "B.texts.csv"), ValidCsv);
        var nameLikeOutput = Write(Path.Combine("binary", "obj.texts.csv"), ValidCsv);
        Write(Path.Combine("bin", "Debug", "C.texts.csv"), ValidCsv);
        Write(Path.Combine("obj", "D.texts.csv"), ValidCsv);
        Write(Path.Combine("Resources", "Other.csv"), ValidCsv);

        // Act
        var files = TextCsvFinder.Find(directory);

        // Assert
        Assert.Equal(new[] { first, second, nameLikeOutput }.Order(StringComparer.Ordinal), files);
    }

    // ------------------------------------------------------------
    // Convert
    // ------------------------------------------------------------

    [Fact]
    public async Task ConvertWritesJsonNextToCsv()
    {
        // Arrange
        var path = Write(Path.Combine("Resources", "Messages.texts.csv"), ValidCsv);

        // Act
        var exitCode = await RunAsync(path);

        // Assert
        Assert.Equal(0, exitCode);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(directory, "Resources", "Messages.texts.json"), TestContext.Current.CancellationToken);
        var expected = "// Generated from Messages.texts.csv\r\n{\r\n  \"A\": { \"ja\": \"あ\" }\r\n}\r\n";
        Assert.Equal(new UTF8Encoding(false).GetBytes(expected), bytes);
    }

    [Fact]
    public async Task ConvertFolderContinuesAfterErrorAndKeepsExistingJson()
    {
        // Arrange
        Write("Bad.texts.csv", "ja\r\n");
        var bad = Write("Bad.texts.json", "existing");
        Write("Good.texts.csv", ValidCsv);

        // Act
        var exitCode = await RunAsync(directory);

        // Assert
        Assert.Equal(-1, exitCode);
        Assert.Equal("existing", await File.ReadAllTextAsync(bad, TestContext.Current.CancellationToken));
        Assert.True(File.Exists(Path.Combine(directory, "Good.texts.json")));
    }

    [Fact]
    public async Task ConvertFailsWhenCsvIsNotFound()
    {
        // Arrange & Act
        var exitCode = await RunAsync(directory);

        // Assert
        Assert.Equal(-1, exitCode);
    }
}
