using Positioni.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Positioni;
internal static class Program
{
    internal const string StartupArgument = "--startup";

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            LanguagePreference.Load();
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(_ =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                try { new App(args.Contains(StartupArgument, StringComparer.OrdinalIgnoreCase)); }
                catch(Exception ex) { Report(ex); throw; }
            });
        }
        catch(Exception ex) { Report(ex); }
    }

    internal static void ReportTrayRegistrationFailure()
    {
        try
        {
            string directory = LanguagePreference.DataDirectory;
            Directory.CreateDirectory(directory);
            string version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            File.WriteAllText(Path.Combine(directory, "tray-error.txt"),
                $"{DateTimeOffset.Now:O} Positioni {version}, Windows {Environment.OSVersion.Version}: Windows did not register the tray icon after repeated attempts.");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static void Report(Exception ex)
    {
        try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"startup-error.txt"),ex.ToString()); } catch { }
        Native.MessageBox(0,UiText.Get("AppStartError") + "\n" + ex.Message,"Positioni",0x10);
    }
}
