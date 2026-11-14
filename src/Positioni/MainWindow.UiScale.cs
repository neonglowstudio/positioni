using Microsoft.UI.Xaml;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private const double SmallUiScale = 0.85;
    private const double LargeUiScale = 1.15;

    private static readonly string UiScalePath = Path.Combine(LanguagePreference.DataDirectory, "ui-scale.txt");

    private void InitializeUiScale()
    {
        string setting = "";
        try { setting = File.ReadAllText(UiScalePath).Trim(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        UiViewport.ZoomFactor = setting switch
        {
            "small" or "75" => SmallUiScale,
            "large" or "125" => LargeUiScale,
            _ => 1
        };
        SmallUiRadio.Checked += UiScale_Checked;
        DefaultUiRadio.Checked += UiScale_Checked;
        LargeUiRadio.Checked += UiScale_Checked;
    }

    private void SyncUiSizeSelection()
    {
        double scale = UiViewport.ZoomFactor;
        SmallUiRadio.IsChecked = scale == SmallUiScale;
        DefaultUiRadio.IsChecked = scale == 1;
        LargeUiRadio.IsChecked = scale == LargeUiScale;
    }

    private void UiScale_Checked(object sender, RoutedEventArgs e)
    {
        double scale = ReferenceEquals(sender, SmallUiRadio) ? SmallUiScale
            : ReferenceEquals(sender, LargeUiRadio) ? LargeUiScale : 1;
        double previous = UiViewport.ZoomFactor;
        if (scale == previous) return;

        try
        {
            Directory.CreateDirectory(LanguagePreference.DataDirectory);
            string temporaryPath = UiScalePath + ".tmp";
            File.WriteAllText(temporaryPath, scale switch { SmallUiScale => "small", LargeUiScale => "large", _ => "regular" });
            File.Move(temporaryPath, UiScalePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            (previous switch
            {
                SmallUiScale => SmallUiRadio,
                LargeUiScale => LargeUiRadio,
                _ => DefaultUiRadio
            }).IsChecked = true;
            Notify(Get("UiScalePreferenceError"), true);
            return;
        }

        UiViewport.ZoomFactor = scale;
    }
}
