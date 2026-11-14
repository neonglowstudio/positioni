using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media.Animation;
using Positioni.Core;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class ProfileThumbnail : UserControl
{
    private Preset? lastProfile;
    private Rect? lastScreen;

    private Storyboard? inactiveBadgeMotion;
    private Guid? badgeProfileId;
    private bool badgeVisible;

    public ProfileThumbnail()
    {
        InitializeComponent();
        InactiveBadgeViewport.SizeChanged += (_, args) =>
            InactiveBadgeClip.Rect = new Windows.Foundation.Rect(0, 0, args.NewSize.Width, args.NewSize.Height);
    }

    internal void UpdateInactiveBadge(Preset profile, bool animate)
    {
        bool show = !profile.IsComplete || !profile.Enabled;
        string label = !profile.IsComplete ? Get("IncompleteProfile") : show ? profile.InactiveLabel : "";
        if (!animate && badgeProfileId == profile.Id && badgeVisible == show && InactiveBadgeText.Text == label)
            return;

        badgeProfileId = profile.Id;
        badgeVisible = show;
        inactiveBadgeMotion?.Stop();
        inactiveBadgeMotion = null;
        InactiveBadgeText.Text = label;
        InactiveBadgeViewport.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
        {
            InactiveBadgeViewport.Height = 0;
            return;
        }

        InactiveBadge.Measure(new Windows.Foundation.Size(Width, double.PositiveInfinity));
        double height = InactiveBadge.DesiredSize.Height;
        Canvas.SetLeft(InactiveBadge, Width - InactiveBadge.DesiredSize.Width);
        InactiveBadgeViewport.Height = height;
        if (!animate) return;

        var motion = new DoubleAnimation
        {
            From = 0,
            To = height,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true,
            FillBehavior = FillBehavior.Stop
        };
        Storyboard.SetTarget(motion, InactiveBadgeViewport);
        Storyboard.SetTargetProperty(motion, nameof(FrameworkElement.Height));
        inactiveBadgeMotion = new Storyboard();
        inactiveBadgeMotion.Children.Add(motion);
        inactiveBadgeMotion.Begin();
    }

    internal void ShowProfile(Preset profile, Rect screen, bool force = false)
    {
        if (!force && ReferenceEquals(profile, lastProfile) && screen == lastScreen) return;
        lastProfile = profile;
        lastScreen = screen;

        double widthLimit = ThumbnailCanvas.Width, heightLimit = ThumbnailCanvas.Height;
        double scale = Math.Min((widthLimit - 4) / screen.Width, (heightLimit - 4) / screen.Height);
        double monitorWidth = screen.Width * scale, monitorHeight = screen.Height * scale;
        double x = (widthLimit - monitorWidth) / 2, y = (heightLimit - monitorHeight) / 2;
        Place(MonitorOutline, x, y, monitorWidth, monitorHeight);
        if (!profile.IsComplete)
        {
            WindowOutline.Visibility = Visibility.Collapsed;
            return;
        }

        int width = profile.WidthPercent is int widthPercent
            ? Geometry.PercentToPixels(widthPercent, screen.Width) : profile.Width;
        int height = profile.HeightPercent is int heightPercent
            ? Geometry.PercentToPixels(heightPercent, screen.Height) : profile.Height;
        // With no position to show, center the configured size as a reference.
        var current = new Rect(screen.X + (screen.Width - width) / 2,
            screen.Y + (screen.Height - height) / 2, width, height);
        var target = Geometry.Resolve(profile, current, screen);
        int left = Math.Max(target.X, screen.X), top = Math.Max(target.Y, screen.Y);
        int right = Math.Min(target.X + target.Width, screen.X + screen.Width);
        int bottom = Math.Min(target.Y + target.Height, screen.Y + screen.Height);
        WindowOutline.Visibility = left < right && top < bottom ? Visibility.Visible : Visibility.Collapsed;
        if (WindowOutline.Visibility != Visibility.Visible) return;

        const double gap = 2;
        double scaleX = (monitorWidth - 2 * gap) / screen.Width;
        double scaleY = (monitorHeight - 2 * gap) / screen.Height;
        Place(WindowOutline, x + gap + (left - screen.X) * scaleX, y + gap + (top - screen.Y) * scaleY,
            Math.Max(1, (right - left) * scaleX), Math.Max(1, (bottom - top) * scaleY));
    }

    private static void Place(Rectangle shape, double x, double y, double width, double height)
    {
        Canvas.SetLeft(shape, x);
        Canvas.SetTop(shape, y);
        shape.Width = width;
        shape.Height = height;
    }
}
