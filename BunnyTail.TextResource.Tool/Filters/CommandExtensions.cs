namespace BunnyTail.TextResource.Tool.Filters;

using BunnyTail.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using Smart.CommandLine.Hosting;

public static partial class CommandExtensions
{
    public static void AddGlobalFilters(this ICommandBuilder command)
    {
        command.AddGlobalFilter<ExceptionFilter>(Int32.MaxValue);
    }

    [ComponentRegistration(Lifetime.Transient, "Filter$")]
    public static partial IServiceCollection AddFilterComponents(this IServiceCollection services);
}
