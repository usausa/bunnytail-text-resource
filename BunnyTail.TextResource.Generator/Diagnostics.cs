namespace BunnyTail.TextResource.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal static class Diagnostics
{
    // Text file

    public static DiagnosticDescriptor InvalidTextFile { get; } = new(
        id: "BTTR0001",
        title: "Invalid text file",
        messageFormat: "Text file cannot be parsed. reason=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidKey { get; } = new(
        id: "BTTR0002",
        title: "Invalid key",
        messageFormat: "Key cannot be used as a member name. key=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor DuplicateKey { get; } = new(
        id: "BTTR0003",
        title: "Duplicate key",
        messageFormat: "Key is duplicated. key=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidClassName { get; } = new(
        id: "BTTR0004",
        title: "Invalid class name",
        messageFormat: "Class name is not valid or is already used. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Language

    public static DiagnosticDescriptor InvalidLanguages { get; } = new(
        id: "BTTR0005",
        title: "Invalid language list",
        messageFormat: "Language list has an invalid element. language=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor LanguagesNotSpecified { get; } = new(
        id: "BTTR0006",
        title: "Language list not specified",
        messageFormat: "Value per language needs a language list. key=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor DefaultLanguageMissing { get; } = new(
        id: "BTTR0007",
        title: "Default language missing",
        messageFormat: "Text has no value for the default language. key=[{0}], language=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor TranslationMissing { get; } = new(
        id: "BTTR0008",
        title: "Translation missing",
        messageFormat: "Text has no value for the language. key=[{0}], language=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnknownLanguage { get; } = new(
        id: "BTTR0009",
        title: "Unknown language",
        messageFormat: "Language is not in the language list. key=[{0}], language=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Placeholder

    public static DiagnosticDescriptor InvalidPlaceholder { get; } = new(
        id: "BTTR0010",
        title: "Invalid placeholder",
        messageFormat: "Text has an invalid placeholder. key=[{0}], language=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor PlaceholderMismatch { get; } = new(
        id: "BTTR0011",
        title: "Placeholder mismatch",
        messageFormat: "Placeholders differ from the default language. key=[{0}], language=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Option

    public static DiagnosticDescriptor InvalidAccessibility { get; } = new(
        id: "BTTR0012",
        title: "Invalid accessibility",
        messageFormat: "Accessibility is not public or internal. accessibility=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);
}
