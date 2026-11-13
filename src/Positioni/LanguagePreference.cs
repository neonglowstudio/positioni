using Positioni.Core;

namespace Positioni;

internal static class LanguagePreference
{
    public static string DataDirectory { get; } =
        Environment.GetEnvironmentVariable("WINDOW_POSITIONER_DATA_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowPositioner");

    private static string LanguagePath => Path.Combine(DataDirectory, "language.txt");

    public static void Load()
    {
        try
        {
            if (File.Exists(LanguagePath)) UiText.Language = File.ReadAllText(LanguagePath).Trim();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void Save(string language)
    {
        Directory.CreateDirectory(DataDirectory);
        string temporaryPath = LanguagePath + ".tmp";
        File.WriteAllText(temporaryPath, language);
        File.Move(temporaryPath, LanguagePath, overwrite: true);
    }
}
