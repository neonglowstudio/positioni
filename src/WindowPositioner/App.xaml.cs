using Microsoft.UI.Xaml;
namespace WindowPositioner;
public partial class App : Application
{
    private static Mutex? instance;
    private MainWindow? window;
    public App() { InitializeComponent(); }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        instance = new Mutex(true, "Local\\WindowPositioner.Desktop", out bool first);
        if (!first) { Native.MessageBox(0, "Window Positioner ya está abierto. Buscá su icono junto al reloj.", "Window Positioner", 0x40); Exit(); return; }
        UnhandledException += (_, e) => { Program.Report(e.Exception); };
        try { window = new MainWindow(); window.Activate(); }
        catch(Exception ex) { Program.Report(ex); Exit(); }
    }
}
