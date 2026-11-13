using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Positioni;

public sealed partial class MainWindow
{
    private const int LogoAtlasColumns = 8;
    private const int LogoFramesPerDirection = 29;
    private const int LogoMotionDuration = 460;
    private readonly DispatcherTimer logoCadence = new() { Interval = TimeSpan.FromMilliseconds(3000) };
    private readonly Stopwatch logoElapsed = new();
    private readonly List<(double Size, TranslateTransform Transform)> logoTiles = [];
    private bool logoAtlasReady;
    private bool logoMirrored;
    private int logoFrame = -1;

    private void InitializeLogoMotion()
    {
        // Atlas order: original to mirrored, then mirrored to original.
        var atlas = new BitmapImage();
        atlas.ImageOpened += (_, _) =>
        {
            logoAtlasReady = true;
            foreach (var slot in new[] { HeaderLogo, WelcomeLogo, AboutLogo })
            {
                slot.Children[0].Visibility = Visibility.Collapsed;
                slot.Children[1].Visibility = Visibility.Visible;
            }
            UpdateLogoMotion();
        };

        foreach (var slot in new[] { HeaderLogo, WelcomeLogo, AboutLogo })
        {
            double size = slot.Width;
            var transform = new TranslateTransform();
            slot.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, size, size) };
            slot.Children.Add(new Image
            {
                Source = atlas,
                Width = size * LogoAtlasColumns,
                Height = size * LogoAtlasColumns,
                Stretch = Stretch.Fill,
                RenderTransform = transform,
                Visibility = Visibility.Collapsed
            });
            logoTiles.Add((size, transform));
        }

        logoCadence.Tick += StartLogoTransition;
        AppWindow.Changed += LogoWindowChanged;
        motionSettings.AnimationsEnabledChanged += LogoMotionPreferenceChanged;
        Closed += (_, _) =>
        {
            logoCadence.Stop();
            StopLogoTransition();
            AppWindow.Changed -= LogoWindowChanged;
            motionSettings.AnimationsEnabledChanged -= LogoMotionPreferenceChanged;
        };
        atlas.UriSource = new Uri("ms-appx:///Assets/PositioniMotion.png");
    }

    private void LogoWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidVisibilityChange || args.DidSizeChange || args.DidPresenterChange)
            UpdateLogoMotion();
    }

    private void LogoMotionPreferenceChanged(Windows.UI.ViewManagement.UISettings sender, object args) =>
        DispatcherQueue.TryEnqueue(UpdateLogoMotion);

    private void UpdateLogoMotion()
    {
        bool visible = AppWindow.IsVisible &&
            AppWindow.Presenter is not OverlappedPresenter { State: OverlappedPresenterState.Minimized };
        if (logoAtlasReady && visible && motionSettings.AnimationsEnabled)
        {
            if (!logoCadence.IsEnabled) logoCadence.Start();
            return;
        }

        logoCadence.Stop();
        StopLogoTransition();
        logoMirrored = false;
        ShowLogoFrame(0);
    }

    private void StartLogoTransition(object? sender, object args)
    {
        if (logoElapsed.IsRunning) return;
        logoElapsed.Restart();
        CompositionTarget.Rendering += RenderLogoTransition;
    }

    private void RenderLogoTransition(object? sender, object args)
    {
        int frame = (int)(logoElapsed.Elapsed.TotalMilliseconds *
            (LogoFramesPerDirection - 1) / LogoMotionDuration);
        frame = Math.Min(frame, LogoFramesPerDirection - 1);
        ShowLogoFrame((logoMirrored ? LogoFramesPerDirection : 0) + frame);
        if (frame == LogoFramesPerDirection - 1)
        {
            logoMirrored = !logoMirrored;
            StopLogoTransition();
        }
    }

    private void StopLogoTransition()
    {
        if (!logoElapsed.IsRunning) return;
        CompositionTarget.Rendering -= RenderLogoTransition;
        logoElapsed.Reset();
    }

    private void ShowLogoFrame(int frame)
    {
        if (frame == logoFrame) return;
        logoFrame = frame;
        foreach (var (size, transform) in logoTiles)
        {
            transform.X = -(frame % LogoAtlasColumns) * size;
            transform.Y = -(frame / LogoAtlasColumns) * size;
        }
    }
}
