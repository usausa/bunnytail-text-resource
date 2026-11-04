# Diagnostics

| ID | Severity | Description | How to fix |
|---|---|---|---|
| BTTR0001 | ❌ Error | Text file cannot be read, or is not a JSON object whose values are strings or objects of strings | Fix the text at the reported position |
| BTTR0002 | ❌ Error | Key is not a C# identifier, is a C# keyword, or is a reserved name (`Keys`, `GetText`, `GetTextFor`, the class name, a member of `object`, or a name starting with `__`) | Rename the key |
| BTTR0003 | ❌ Error | Key, or language in a key, appears more than once | Remove the duplicate |
| BTTR0004 | ❌ Error | Class name taken from the file name is not a valid identifier, or another text file has the same class name in the same namespace | Rename the file, or move it to another folder |
| BTTR0005 | ❌ Error | `TextResourceLanguages` has an empty element, a duplicate, or an element that is not a language code | Fix `TextResourceLanguages` |
| BTTR0006 | ❌ Error | Value is written for each language, but `TextResourceLanguages` is not specified | Set `TextResourceLanguages`, or write the value as a string |
| BTTR0007 | ❌ Error | Text has no value for the first language of `TextResourceLanguages` | Add the value of the first language |
| BTTR0008 | ⚠️ Warning | Text has no value for a language, so the first language is used for it | Add the value of the language. To stop the build, add the ID to `WarningsAsErrors` (`.editorconfig` has no effect on a location in a text file) |
| BTTR0009 | ⚠️ Warning | Language is not in `TextResourceLanguages`, so the value is not used | Fix the language, or add it to `TextResourceLanguages` |
| BTTR0010 | ❌ Error | Braces are not balanced, named and numbered placeholders are mixed, or a placeholder is not valid | Write braces as `{{` and `}}`, or fix the placeholder |
| BTTR0011 | ❌ Error | Placeholders of a language differ from those of the first language | Use the same placeholders in all languages |
| BTTR0012 | ❌ Error | `TextResourceAccessibility` is not `public` or `internal` | Fix `TextResourceAccessibility` |
