namespace BunnyTail.TextResource.Tool.Filters;

using Smart.CommandLine.Hosting;

public sealed class ExceptionFilter : ICommandFilter
{
    private readonly ILogger<ExceptionFilter> log;

    public ExceptionFilter(ILogger<ExceptionFilter> log)
    {
        this.log = log;
    }

    public async ValueTask ExecuteAsync(CommandContext context, CommandDelegate next)
    {
#pragma warning disable CA1031
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            log.ErrorUnknownException(ex, context.CommandType.Name);

            context.ExitCode = -1;
        }
#pragma warning restore CA1031
    }
}
