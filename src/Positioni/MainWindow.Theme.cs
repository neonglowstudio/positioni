using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private static readonly string ThemePath = Path.Combine(LanguagePreference.DataDirectory, "theme.txt");

    private void InitializeTheme()
    {
        var theme = ElementTheme.Default;
        try
        {
            theme = File.ReadAllText(ThemePath).Trim() switch
            {
                "light" => ElementTheme.Light,
                "dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        ApplyTheme(theme);
        SystemThemeRadio.Checked += Theme_Checked;
        LightThemeRadio.Checked += Theme_Checked;
        DarkThemeRadio.Checked += Theme_Checked;
    }

    private void ApplyTheme(ElementTheme theme)
    {
        Root.RequestedTheme = theme;
        AppWindow.TitleBar.PreferredTheme = theme switch
        {
            ElementTheme.Light => TitleBarTheme.Light,
            ElementTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode
        };
        (theme switch
        {
            ElementTheme.Light => LightThemeRadio,
            ElementTheme.Dark => DarkThemeRadio,
            _ => SystemThemeRadio
        }).IsChecked = true;
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        var theme = ReferenceEquals(sender, LightThemeRadio) ? ElementTheme.Light
            : ReferenceEquals(sender, DarkThemeRadio) ? ElementTheme.Dark : ElementTheme.Default;
        var previous = Root.RequestedTheme;
        if (theme == previous) return;

        try
        {
            string value = theme switch
            {
                ElementTheme.Light => "light",
                ElementTheme.Dark => "dark",
                _ => "system"
            };
            Directory.CreateDirectory(LanguagePreference.DataDirectory);
            string temporaryPath = ThemePath + ".tmp";
            File.WriteAllText(temporaryPath, value);
            File.Move(temporaryPath, ThemePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ApplyTheme(previous);
            Notify(Get("ThemePreferenceError"), true);
            return;
        }

        ApplyTheme(theme);
    }
}
