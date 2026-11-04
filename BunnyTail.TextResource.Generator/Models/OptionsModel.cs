namespace BunnyTail.TextResource.Generator.Models;

using SourceGenerateHelper;

internal sealed record OptionsModel(
    string RootNamespace,
    string ProjectDirectory,
    string Accessibility,
    EquatableArray<string> Languages,
    EquatableArray<DiagnosticInfo> Diagnostics);
