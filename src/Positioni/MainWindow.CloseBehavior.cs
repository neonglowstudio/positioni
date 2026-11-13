using Microsoft.UI.Xaml;
using static Positioni.Core.UiText;

namespace Positioni;

public sealed partial class MainWindow
{
    private static readonly string CloseBehaviorPath = Path.Combine(LanguagePreference.DataDirectory, "close-behavior.txt");

    private void InitializeCloseBehavior()
    {
        try
        {
            host.CloseToTray = File.ReadAllText(CloseBehaviorPath).Trim() != "exit";
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        (host.CloseToTray ? CloseToTrayRadio : ExitOnCloseRadio).IsChecked = true;
        CloseToTrayRadio.Checked += CloseBehavior_Checked;
        ExitOnCloseRadio.Checked += CloseBehavior_Checked;
        host.RequestExit = () => DispatcherQueue.TryEnqueue(() => RequestCloseAsync(forceExit: false));
        host.RequestQuit = () => DispatcherQueue.TryEnqueue(() => RequestCloseAsync(forceExit: true));
    }

    private bool requestingClose;
    private int closeGeneration;

    private async void RequestCloseAsync(bool forceExit)
    {
        if (requestingClose) return;
        requestingClose = true;
        try
        {
            // A hidden window must be visible before it can ask about edits.
            if (HasProfileChanges()) ShowEditor();
            var pendingDecision = unsavedProfileDecision;
            if (pendingDecision == null && activeDialog is { } dialog && activeDialogResult is { } result)
            {
                dialog.Hide();
                await result;
                // Let the dismissed dialog's caller finish before showing another.
                await Task.Yield();
            }

            if (pendingDecision != null)
            {
                if (!await pendingDecision) return;
            }
            else if (!await ConfirmProfileChangeAsync()) return;

            closeGeneration++;
            activeDialog?.Hide();
            if (!forceExit && host.CloseToTray && host.TrayAvailable)
                Native.ShowWindow(hwnd, 0);
            else
                host.Quit?.Invoke();
        }
        finally { requestingClose = false; }
    }


    private void CloseBehavior_Checked(object sender, RoutedEventArgs e)
    {
        bool closeToTray = ReferenceEquals(sender, CloseToTrayRadio);
        if (closeToTray == host.CloseToTray) return;

        try
        {
            Directory.CreateDirectory(LanguagePreference.DataDirectory);
            string temporaryPath = CloseBehaviorPath + ".tmp";
            File.WriteAllText(temporaryPath, closeToTray ? "tray" : "exit");
            File.Move(temporaryPath, CloseBehaviorPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            (host.CloseToTray ? CloseToTrayRadio : ExitOnCloseRadio).IsChecked = true;
            Notify(Get("CloseBehaviorError"), true);
            return;
        }

        host.CloseToTray = closeToTray;
    }
}
