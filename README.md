# BunnyTail.TextResource

[![NuGet](https://img.shields.io/nuget/v/BunnyTail.TextResource.svg)](https://www.nuget.org/packages/BunnyTail.TextResource)

## What is this?

Generate static classes from JSON text files for MAUI, Blazor and other C# 8.0 or later projects.

Texts are embedded in the code. The language is selected by `CultureInfo.CurrentUICulture`.

## Usage

### Project

```xml
  <PropertyGroup>
    <TextResourceLanguages>ja;en</TextResourceLanguages>
  </PropertyGroup>
```

The first language is the default language. Omit the property for a single language.

### Text file

Each `**/*.texts.json` file becomes a class named after the file. The namespace is the root namespace plus the folder.

```jsonc
// Resources/Messages.texts.json
{
  "HelloWorld": { "ja": "ハローワールド", "en": "Hello World" },
  "ItemCount": { "ja": "{count} 件", "en": "{count} items", "$comment": "Shown in the list header" },
  "Total": { "ja": "合計 {value:N0} 円", "en": "Total {value:N0} yen" }
}
```

For a single language, values are strings.

```jsonc
{
  "HelloWorld": "ハローワールド",
  "ItemCount": "{count} 件"
}
```

Braces are written as `{{` and `}}`. `$comment` is added to the documentation comment.

### Source

```cs
var hello = Messages.HelloWorld;
var count = Messages.ItemCount(3);

var keys = Messages.Keys;
var text = Messages.GetText("HelloWorld");
var english = Messages.GetTextFor("HelloWorld", CultureInfo.GetCultureInfo("en"));
```

`GetText` and `GetTextFor` return `null` if the key does not exist, and the format string if the text has placeholders.

### XAML

```xml
<Label Text="{x:Static strings:Messages.HelloWorld}" />
```

The generated class cannot be used as `x:DataType` with the XAML source generator.

## Properties

| Property | Default | Description |
|---|---|---|
| `TextResourceLanguages` | | Languages separated by `;` |
| `TextResourceAccessibility` | `internal` | Accessibility of the generated class (`public` or `internal`) |
| `EnableDefaultTextResourceItems` | `true` | Include `**/*.texts.json` automatically |
