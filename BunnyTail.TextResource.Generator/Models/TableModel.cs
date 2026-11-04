namespace BunnyTail.TextResource.Generator.Models;

using SourceGenerateHelper;

internal sealed record TableModel(
    string Path,
    string Namespace,
    string ClassName,
    string Accessibility,
    EquatableArray<string> Languages,
    EquatableArray<EntryModel> Entries)
{
    public string FullName => Namespace.Length > 0 ? $"{Namespace}.{ClassName}" : ClassName;
}
