using Microsoft.UI.Xaml;
using Positioni.Core;

namespace Positioni;

public partial class App : Application
{
    private static Mutex? instance;
    private MainWindow? window;

    private const string ShowEditorEvent = "Local\\Positioni.ShowEditor";
    private readonly bool startInTray;
    private EventWaitHandle? openRequest;
    private RegisteredWaitHandle? openWait;

    public App(bool startInTray)
    {
        this.startInTray = startInTray;
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        instance = new Mutex(true, "Local\\WindowPositioner.Desktop", out bool first);
        if (!first)
        {
            if (!startInTray)
            {
                if (EventWaitHandle.TryOpenExisting(ShowEditorEvent, out var request))
                {
                    using (request) request.Set();
                }
                else Native.MessageBox(0, UiText.Get("AlreadyOpen"), "Positioni", 0x40);
            }
            Exit();
            return;
        }

        UnhandledException += (_, e) => Program.Report(e.Exception);
        try
        {
            openRequest = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEditorEvent);
            window = new MainWindow();
            var dispatcher = window.DispatcherQueue;
            openWait = ThreadPool.RegisterWaitForSingleObject(openRequest,
                (_, _) => dispatcher.TryEnqueue(window.ShowEditor), null, Timeout.Infinite, false);
            window.Closed += (_, _) =>
            {
                openWait.Unregister(null);
                openRequest.Dispose();
            };
            if (!startInTray) window.ShowEditor();
        }
        catch (Exception ex)
        {
            Program.Report(ex);
            Exit();
        }
    }
}
