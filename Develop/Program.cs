using System.Globalization;

using Develop.Resources;

foreach (var culture in new[] { "ja-JP", "en-US", "fr-FR" })
{
    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

    Console.WriteLine($"[{culture}]");
    Console.WriteLine($"HelloWorld: {Messages.HelloWorld}");
    Console.WriteLine($"ItemCount: {Messages.ItemCount(3)}");
    Console.WriteLine($"GetText: {Messages.GetText(nameof(Messages.HelloWorld))}");
}

Console.ReadLine();
