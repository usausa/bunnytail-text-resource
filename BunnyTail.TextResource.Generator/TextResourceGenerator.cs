namespace BunnyTail.TextResource.Generator;

using System;
using System.Linq;

using BunnyTail.TextResource.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

[Generator]
public sealed class TextResourceGenerator : IIncrementalGenerator
{
    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => TextResourceModelBuilder.BuildOptions(provider.GlobalOptions));

        context.RegisterSourceOutput(options, static (spc, model) => spc.ReportDiagnostics(model.Diagnostics));

        var tables = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(TextResourceModelBuilder.FileExtension, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, token) => new TextFileModel(file.Path, file.GetText(token)?.ToString()))
            .Combine(options)
            .Select(static (pair, _) => TextResourceModelBuilder.BuildTable(pair.Left, pair.Right));

        var duplicates = tables
            .Collect()
            .Select(static (results, _) => TextResourceModelBuilder.FindDuplicates(results));

        context.RegisterSourceOutput(tables.Combine(duplicates), static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, Result<TableModel> result, EquatableArray<string> duplicates)
    {
        context.ReportDiagnostics(result.Diagnostics);

        if (!result.HasValue)
        {
            return;
        }

        var table = result.Value;
        if (duplicates.Contains(table.FullName, StringComparer.OrdinalIgnoreCase))
        {
            context.ReportDiagnostic(new DiagnosticInfo(Diagnostics.InvalidClassName, TextResourceModelBuilder.CreateFileLocation(table.Path), table.ClassName));
            return;
        }

        context.AddSource(HintNameBuilder.Build(table.Namespace, table.ClassName), TextResourceSourceBuilder.Build(table));
    }
}
