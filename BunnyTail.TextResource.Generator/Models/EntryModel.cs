namespace BunnyTail.TextResource.Generator.Models;

using SourceGenerateHelper;

internal sealed record EntryModel(
    string Key,
    string? Comment,
    EquatableArray<string> Parameters,
    EquatableArray<TextModel> Texts);
