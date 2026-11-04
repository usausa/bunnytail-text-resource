namespace BunnyTail.TextResource.Tool.Components;

public static class TextCsvFinder
{
    private const string Extension = ".texts.csv";

    public static IReadOnlyList<string> Find(string path)
    {
        if (File.Exists(path))
        {
            return path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? [path] : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateFiles(path, $"*{Extension}", SearchOption.AllDirectories)
                .Where(x => !IsBuildOutput(Path.GetRelativePath(path, x)))
                .Order(StringComparer.Ordinal)
        ];
    }

    private static bool IsBuildOutput(string relativePath) =>
        relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .SkipLast(1)
            .Any(static x => String.Equals(x, "bin", StringComparison.OrdinalIgnoreCase) || String.Equals(x, "obj", StringComparison.OrdinalIgnoreCase));
}
