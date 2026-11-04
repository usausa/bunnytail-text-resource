namespace BunnyTail.TextResource.Tool.Components;

public static class OutputWriter
{
    public static void WriteSuccess(string message) =>
        Console.WriteLine($"OK {message}");

    public static void WriteError(string message) =>
        Console.WriteLine($"NG: {message}");
}
