using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Positioni")]
[assembly: AssemblyProduct("Positioni")]
[assembly: AssemblyCompany("Neon Glow")]

internal static class Launcher
{
    [STAThread]
    private static int Main(string[] args)
    {
        string files = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files");
        string app = Path.Combine(files, "Positioni.exe");
        try
        {
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = app,
                WorkingDirectory = files,
                UseShellExecute = false,
                Arguments = string.Join(" ", args.Where(arg => arg.Equals("--startup", StringComparison.OrdinalIgnoreCase) ||
                        arg == "--demo-update=available" || arg == "--demo-update=current" || arg == "--demo-update=portable"))
            }))
            {
                return process == null ? 1 : 0;
            }
        }
        catch (Exception ex)
        {
            if (!(ex is System.ComponentModel.Win32Exception || ex is IOException || ex is UnauthorizedAccessException)) throw;
            string message;
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "es": message = "No se pudo abrir Positioni. Extrae el ZIP completo y conserva la carpeta Files junto al ejecutable."; break;
                case "pt": message = "Não foi possível abrir o Positioni. Extraia todo o ZIP e mantenha a pasta Files junto do executável."; break;
                case "it": message = "Impossibile avviare Positioni. Estrai tutto il file ZIP e tieni la cartella Files accanto all’eseguibile."; break;
                case "fr": message = "Impossible de démarrer Positioni. Extrayez le ZIP en entier et gardez le dossier Files à côté de l’exécutable."; break;
                case "de": message = "Positioni konnte nicht gestartet werden. Entpacken Sie die gesamte ZIP-Datei und lassen Sie den Ordner Files neben der EXE."; break;
                default: message = "Could not start Positioni. Extract the entire ZIP and keep the Files folder next to the executable."; break;
            }
            MessageBox.Show(message, "Positioni", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
