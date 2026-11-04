namespace BunnyTail.TextResource.Tool.Filters;

internal static partial class Log
{
    // Error

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown exception. command=[{commandName}]")]
    public static partial void ErrorUnknownException(this ILogger logger, Exception ex, string commandName);
}
