using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.System;

namespace Positioni;

public sealed partial class MainWindow
{
    private DropShadow? studioGlow;
    private long firstStudioLogoClick;
    private int studioLogoClicks;

    private void StudioLogo_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!OptionsOpen || DeveloperModePanel.Visibility == Visibility.Visible ||
            e.GetCurrentPoint(StudioLogo).Properties.PointerUpdateKind != Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased)
            return;

        long now = Environment.TickCount64;
        if (studioLogoClicks == 0 || now - firstStudioLogoClick > 2000)
        {
            firstStudioLogoClick = now;
            studioLogoClicks = 0;
        }

        if (++studioLogoClicks == 5)
        {
            DeveloperModePanel.Visibility = Visibility.Visible;
            RunOnboardingButton.Focus(FocusState.Programmatic);
        }
    }

    private void RunOnboarding_Click(object sender, RoutedEventArgs e)
    {
        if (CloseOptions())
            OpenStarterProfiles(StarterPickerEntry.WelcomePreview);
    }

    private void StudioLogo_ImageOpened(object sender, RoutedEventArgs e)
    {
        var hostVisual = ElementCompositionPreview.GetElementVisual(StudioGlowHost);
        var compositor = hostVisual.Compositor;
        studioGlow = compositor.CreateDropShadow();
        studioGlow.Color = Microsoft.UI.ColorHelper.FromArgb(255, 255, 99, 23);
        studioGlow.Mask = StudioLogo.GetAlphaMask();

        var visual = compositor.CreateSpriteVisual();
        visual.Shadow = studioGlow;
        visual.RelativeSizeAdjustment = System.Numerics.Vector2.One;
        ElementCompositionPreview.SetElementChildVisual(StudioGlowHost, visual);
        UpdateStudioGlow();
    }

    private void UpdateStudioGlow()
    {
        if (studioGlow is not { } glow) return;

        glow.StopAnimation(nameof(DropShadow.Opacity));
        glow.StopAnimation(nameof(DropShadow.BlurRadius));
        glow.Opacity = 0.32f;
        glow.BlurRadius = 12;
        if (!OptionsOpen || !motionSettings.AnimationsEnabled) return;

        var compositor = glow.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(new(0.42f, 0), new(0.58f, 1));
        foreach (var (property, low, high) in new[]
        {
            (nameof(DropShadow.Opacity), 0.32f, 0.85f),
            (nameof(DropShadow.BlurRadius), 12f, 25f)
        })
        {
            var pulse = compositor.CreateScalarKeyFrameAnimation();
            pulse.InsertKeyFrame(0, low);
            pulse.InsertKeyFrame(0.5f, high, easing);
            pulse.InsertKeyFrame(1, low, easing);
            pulse.Duration = TimeSpan.FromSeconds(4.8);
            pulse.IterationBehavior = AnimationIterationBehavior.Forever;
            glow.StartAnimation(property, pulse);
        }
    }

    private async void AboutLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string destination })
            await Launcher.LaunchUriAsync(new Uri(destination));
    }
}
