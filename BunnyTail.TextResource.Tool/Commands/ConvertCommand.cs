namespace BunnyTail.TextResource.Tool.Commands;

using BunnyTail.TextResource.Tool.Components;

using Smart.CommandLine.Hosting;

[Command("convert", "Convert CSV files to text resource JSON files")]
public sealed class ConvertCommand : ICommandHandler
{
    [Option<string>("--path", "-p", Description = "CSV file or folder", DefaultValue = ".")]
    public string TargetPath { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var files = TextCsvFinder.Find(TargetPath);
        if (files.Count == 0)
        {
            OutputWriter.WriteError($"CSV file is not found. path=[{TargetPath}]");
            context.ExitCode = -1;
            return;
        }

        foreach (var file in files)
        {
            var result = TextCsvConverter.Convert(await File.ReadAllBytesAsync(file, context.CancellationToken), file);
            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    OutputWriter.WriteError(error);
                }

                context.ExitCode = -1;
                continue;
            }

            var output = Path.ChangeExtension(file, ".json");
            await File.WriteAllTextAsync(output, result.Json, context.CancellationToken);
            OutputWriter.WriteSuccess($"{output} keys=[{result.KeyCount}]");
        }
    }
}
