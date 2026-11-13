using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

internal static class UpdateLauncher
{
    [STAThread]
    private static int Main(string[] args)
    {
        int pid;
        if (args.Length != 2 || !int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out pid) ||
            !Path.IsPathRooted(args[1]) || !Path.GetFileName(args[1]).Equals("Positioni.exe", StringComparison.OrdinalIgnoreCase))
            return 1;

        string app = args[1];
        string installDirectory = Path.GetDirectoryName(app);
        string setup = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Positioni-setup.exe");
        bool appExited = false;
        try
        {
            Process running = null;
            try { running = Process.GetProcessById(pid); }
            catch (ArgumentException) { }
            using (running)
            {
                if (running != null && !running.WaitForExit(30000)) throw new InvalidOperationException();
            }
            appExited = true;

            var options = new ProcessStartInfo(setup)
            {
                UseShellExecute = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"" + installDirectory.TrimEnd('\\') + "\""
            };
            using (var installer = Process.Start(options))
            {
                if (installer == null) throw new InvalidOperationException();
                installer.WaitForExit();
                if (installer.ExitCode != 0) throw new InvalidOperationException();
            }
            Restart(app);
            return 0;
        }
        catch (Exception)
        {
            string text;
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "es": text = "No se pudo instalar la actualización de Positioni."; break;
                case "pt": text = "Não foi possível instalar a atualização do Positioni."; break;
                case "it": text = "Impossibile installare l’aggiornamento di Positioni."; break;
                case "fr": text = "Impossible d’installer la mise à jour de Positioni."; break;
                case "de": text = "Das Positioni-Update konnte nicht installiert werden."; break;
                default: text = "The Positioni update could not be installed."; break;
            }
            MessageBox.Show(text, "Positioni", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (appExited)
            {
                try { Restart(app); } catch (Exception) { }
            }
            return 1;
        }
        finally
        {
            try { File.Delete(setup); } catch (Exception) { }
        }
    }

    private static void Restart(string app)
    {
        using (Process.Start(new ProcessStartInfo(app)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(app)
        })) { }
    }
}
