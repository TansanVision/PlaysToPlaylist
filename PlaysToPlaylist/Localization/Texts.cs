using System.Globalization;
using System.Text.Json;

namespace PlaysToPlaylist.Localization;

public static class Texts
{
    private static readonly Dictionary<string, string> English = Load("en");
    private static readonly Dictionary<string, string> Japanese = Load("ja");
    public static string Language { get; private set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ? "ja" : "en";

    public static void SetLanguage(string language)
    {
        if (language is not ("ja" or "en"))
            throw new ArgumentException(Get("InvalidLanguage"));
        Language = language;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(language == "ja" ? "ja-JP" : "en-US");
    }

    public static string Get(string key, params object?[] args)
    {
        var resources = Language == "ja" ? Japanese : English;
        var value = resources.GetValueOrDefault(key) ?? English[key];
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }

    private static Dictionary<string, string> Load(string language)
    {
        using var stream = typeof(Texts).Assembly.GetManifestResourceStream($"PlaysToPlaylist.Localization.{language}.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
