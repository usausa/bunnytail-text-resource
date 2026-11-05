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

The first language is the default language.  
Omit the property for a single language.

### Text file

Each `**/*.texts.json` file becomes a class named after the file.  
The namespace is the root namespace plus the folder.

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

Braces are written as `{{` and `}}`.

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

## Properties

| Property | Default | Description |
|---|---|---|
| `TextResourceLanguages` | | Languages separated by `;` |
| `TextResourceAccessibility` | `internal` | Accessibility of the generated class (`public` or `internal`) |
| `EnableDefaultTextResourceItems` | `true` | Include `**/*.texts.json` automatically |

## CSV

`BunnyTail.TextResource.Tool` converts `*.texts.csv` to `*.texts.json`. The CSV is the source, and the JSON is overwritten.

```
dotnet tool install -g BunnyTail.TextResource.Tool
textresource convert -p Resources/Strings
```

```csv
key,ja,en,$comment
# Comment row
HelloWorld,ハローワールド,Hello World,Shown on the home page
ItemCount,{count} 件,{count} items,
```

- The first column is `key`. The other columns are languages and `$comment`. Other columns starting with `$` are ignored
- For a single language, use `key,$text`
- An empty cell means no value
- The encoding is UTF-8. In Excel, save as `CSV UTF-8`
- Excel changes values that look like numbers, dates or formulas when it opens a CSV (`1-2`, `001`, `-{0}`). Check the difference in the JSON
