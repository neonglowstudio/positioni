using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace Positioni;

public sealed partial class MainWindow
{
    private readonly DispatcherTimer statusTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly UISettings motionSettings = new();
    private Storyboard? toastMotion;
    private bool toastHiding;
    private bool toastInOptions, toastPersistent;

    private void Notify(string message, bool warning = false, bool persistent = false)
    {
        if (!warning && !persistent && ToastStatus.IsOpen && !toastHiding &&
            (toastPersistent || ToastStatus.Severity == InfoBarSeverity.Warning))
            return;

        statusTimer.Stop();
        bool animateIn = ToastHost.Visibility != Visibility.Visible || toastHiding;
        toastInOptions = OptionsOpen;
        toastPersistent = persistent;
        ToastStatus.Message = message;
        ToastStatus.Severity = warning ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;
        ToastStatus.IsOpen = true;
        ToastHost.IsHitTestVisible = true;
        ToastHost.Visibility = Visibility.Visible;
        if (animateIn) AnimateToast(show: true);
        if (!warning && !persistent) statusTimer.Start();
    }

    private void Toast_Closing(InfoBar sender, InfoBarClosingEventArgs args)
    {
        if (args.Reason != InfoBarCloseReason.CloseButton) return;
        args.Cancel = true;
        DispatcherQueue.TryEnqueue(DismissToast);
    }

    private void DismissToast()
    {
        statusTimer.Stop();
        if (ToastHost.Visibility != Visibility.Visible || toastHiding) return;
        ToastHost.IsHitTestVisible = false;
        AnimateToast(show: false);
    }

    private void ClearStatus()
    {
        statusTimer.Stop();
        toastMotion?.Stop();
        toastMotion = null;
        toastHiding = false;
        ToastStatus.IsOpen = false;
        ToastHost.Visibility = Visibility.Collapsed;
    }

    private void AnimateToast(bool show)
    {
        toastMotion?.Stop();
        toastMotion = null;
        toastHiding = !show;

        if (!ToastHost.IsLoaded || !motionSettings.AnimationsEnabled)
        {
            if (show)
            {
                ToastHost.Opacity = 1;
                ToastTranslation.Y = 0;
            }
            else ClearStatus();
            return;
        }

        const double slide = 18;
        double startOpacity = show ? 0 : 1;
        double endOpacity = show ? 1 : 0;
        double startY = show ? slide : 0;
        double endY = show ? 0 : slide;
        ToastHost.Opacity = startOpacity;
        ToastTranslation.Y = startY;
        var duration = new Duration(TimeSpan.FromMilliseconds(show ? 180 : 145));
        var storyboard = new Storyboard();
        var fade = new DoubleAnimation { From = startOpacity, To = endOpacity, Duration = duration };
        var move = new DoubleAnimation
        {
            From = startY,
            To = endY,
            Duration = duration,
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fade, ToastHost);
        Storyboard.SetTargetProperty(fade, "Opacity");
        Storyboard.SetTarget(move, ToastTranslation);
        Storyboard.SetTargetProperty(move, "Y");
        storyboard.Children.Add(fade);
        storyboard.Children.Add(move);
        storyboard.Completed += (_, _) =>
        {
            if (toastMotion != storyboard) return;
            if (!show)
            {
                ClearStatus();
                return;
            }
            ToastHost.Opacity = endOpacity;
            ToastTranslation.Y = endY;
            toastMotion = null;
            storyboard.Stop();
        };
        toastMotion = storyboard;
        storyboard.Begin();
    }
}
