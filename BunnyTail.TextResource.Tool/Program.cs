using BunnyTail.DependencyInjection;
using BunnyTail.TextResource.Tool.Commands;
using BunnyTail.TextResource.Tool.Filters;

using Smart.CommandLine.Hosting;

var builder = CommandHost.CreateBuilder(args)
    .UseDefaults();

builder.ConfigureContainer(new GeneratedServiceProviderFactory(static options => options.TrackTransientDisposables = false));

builder.Services.AddCommandComponents();
builder.Services.AddFilterComponents();

builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Text resource tool");
    });

    commands.AddGlobalFilters();
    commands.AddCommands();
});

var host = builder.Build();
return await host.RunAsync();
