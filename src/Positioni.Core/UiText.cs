using System.Globalization;

namespace Positioni.Core;

public static class UiText
{
    private static readonly string[] Codes = ["en", "es", "pt", "it", "fr", "de"];
    public static IReadOnlyList<string> LanguageCodes { get; } = Array.AsReadOnly(Codes);
    private static readonly Dictionary<string, string[]> Messages = ReadMessages();
    private static string language = SystemLanguage;

    public static string SystemLanguage
    {
        get
        {
            string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return LanguageCodes.Contains(code) ? code : "en";
        }
    }

    public static string Language
    {
        get => language;
        set => language = LanguageCodes.Contains(value) ? value : SystemLanguage;
    }

    public static string Get(string key, params object[] args)
    {
        int index = Array.IndexOf(Codes, Language);
        string message = Messages[key][index];
        return args.Length == 0 ? message : string.Format(CultureInfo.GetCultureInfo(Language), message, args);
    }

    private static Dictionary<string, string[]> ReadMessages()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("Positioni.Translations.txt")
            ?? throw new InvalidOperationException("Missing Positioni translations.");
        using var reader = new StreamReader(stream);
        string expectedHeader = "Key|" + string.Join('|', LanguageCodes);
        if (reader.ReadLine() != expectedHeader)
            throw new InvalidDataException("Invalid translation table header.");

        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] columns = line.Split('|');
            if (columns.Length != LanguageCodes.Count + 1 || columns.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException($"Incomplete translation for {columns[0]}.");
            result.Add(columns[0], columns[1..]);
        }
        return result;
    }
}
