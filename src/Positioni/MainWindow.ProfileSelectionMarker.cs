using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Positioni.Core;

namespace Positioni;

public sealed partial class MainWindow
{
    private readonly HashSet<Border> profileSelectionMarkers = [];

    private void ProfileSelectionMarker_Loaded(object sender, RoutedEventArgs e)
    {
        var marker = (Border)sender;
        profileSelectionMarkers.Add(marker);
        UpdateProfileSelectionMarker(marker);
    }

    private void ProfileSelectionMarker_Unloaded(object sender, RoutedEventArgs e) =>
        profileSelectionMarkers.Remove((Border)sender);

    private void ProfileSelectionMarker_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e) =>
        UpdateProfileSelectionMarker((Border)sender);

    private void UpdateProfileSelectionMarker(Border marker)
    {
        // Keep the indicator tied to the selected profile when WinUI reuses a row.
        marker.Opacity = marker.DataContext is Preset profile && ReferenceEquals(profile, selected) ? 1 : 0;
    }
}
