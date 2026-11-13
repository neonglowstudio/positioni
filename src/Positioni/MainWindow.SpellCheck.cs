using Microsoft.UI.Xaml;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private static readonly string SpellCheckPath = Path.Combine(LanguagePreference.DataDirectory, "spell-check.txt");

    private void InitializeSpellCheck()
    {
        bool enabled = false;
        try
        {
            enabled = File.ReadAllText(SpellCheckPath).Trim() == "on";
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        ApplySpellCheck(enabled);
    }

    private void ApplySpellCheck(bool enabled)
    {
        DisableSpellCheck.IsChecked = !enabled;
        PresetName.IsSpellCheckEnabled = enabled;
        PresetNotes.IsSpellCheckEnabled = enabled;
    }

    private void DisableSpellCheck_Click(object sender, RoutedEventArgs e)
    {
        bool enabled = DisableSpellCheck.IsChecked == false;
        try
        {
            Directory.CreateDirectory(LanguagePreference.DataDirectory);
            string temporaryPath = SpellCheckPath + ".tmp";
            File.WriteAllText(temporaryPath, enabled ? "on" : "off");
            File.Move(temporaryPath, SpellCheckPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DisableSpellCheck.IsChecked = !PresetName.IsSpellCheckEnabled;
            Notify(Get("SpellCheckError"), true);
            return;
        }

        ApplySpellCheck(enabled);
    }
}
