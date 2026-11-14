using Microsoft.UI.Xaml;
using Positioni.Core;

namespace Positioni;

public sealed partial class MainWindow
{
    private readonly HashSet<ProfileThumbnail> profileThumbnails = [];
    private Guid? animateInactiveProfile;

    private void ProfileThumbnail_Loaded(object sender, RoutedEventArgs e)
    {
        var thumbnail = (ProfileThumbnail)sender;
        profileThumbnails.Add(thumbnail);
        UpdateProfileThumbnail(thumbnail, force: true);
    }

    private void ProfileThumbnail_Unloaded(object sender, RoutedEventArgs e) =>
        profileThumbnails.Remove((ProfileThumbnail)sender);

    private void ProfileThumbnail_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
    {
        var thumbnail = (ProfileThumbnail)sender;
        if (profileThumbnails.Contains(thumbnail)) UpdateProfileThumbnail(thumbnail);
    }

    private void UpdateProfileThumbnail(ProfileThumbnail thumbnail, bool force = false)
    {
        if (thumbnail.DataContext is not Preset profile) return;
        bool animate = !profile.Enabled && animateInactiveProfile == profile.Id;
        if (animate) animateInactiveProfile = null;
        thumbnail.UpdateInactiveBadge(profile, animate && motionSettings.AnimationsEnabled);
        try
        {
            thumbnail.ShowProfile(profile, Native.MonitorAreas(hwnd, profile.PrimaryMonitor).Screen, force);
        }
        catch (InvalidOperationException)
        {
            // The monitor may be temporarily unavailable while the display layout changes.
        }
    }

    private void UpdateMonitorPreviews()
    {
        UpdatePositionPreview();
        foreach (var thumbnail in profileThumbnails) UpdateProfileThumbnail(thumbnail);
    }
}
