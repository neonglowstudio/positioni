using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace WindowPositioner;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(_ =>
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
                try { new App(); }
                catch(Exception ex) { Report(ex); throw; }
            });
        }
        catch(Exception ex) { Report(ex); }
    }
    internal static void Report(Exception ex)
    {
        try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"startup-error.txt"),ex.ToString()); } catch { }
        Native.MessageBox(0,"No se pudo abrir Window Positioner.\n"+ex.Message,"Window Positioner",0x10);
    }
}
