using System.Globalization;
using Windows.Graphics;

namespace Positioni;

public sealed partial class MainWindow
{
    private const int InitialWidth = 905;
    private const int InitialHeight = 876;
    private static string WindowSizePath => Path.Combine(LanguagePreference.DataDirectory, "window-size.txt");

    private double WindowScale => Math.Max(1u, Native.GetDpiForWindow(hwnd)) / 96.0;

    private void RestoreWindowSize()
    {
        var size = new SizeInt32(InitialWidth, InitialHeight);
        try
        {
            var fields = File.ReadAllText(WindowSizePath).Split(' ');
            if (fields.Length == 2 &&
                int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out int width) &&
                int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out int height) &&
                width > 0 && height > 0)
            {
                size = new SizeInt32(width, height);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        ApplyWindowSize(size, center: true);
    }

    private void ResetWindowSize()
    {
        var size = new SizeInt32(InitialWidth, InitialHeight);
        ApplyWindowSize(size);
        SaveWindowSize(size);
    }

    private void ApplyWindowSize(SizeInt32 size, bool center = false)
    {
        double scale = WindowScale;
        var work = Native.WorkArea(hwnd, primary: false);
        int width = (int)Math.Clamp(size.Width * scale, 1, work.Width);
        int height = (int)Math.Clamp(size.Height * scale, 1, work.Height);
        var position = AppWindow.Position;
        int x = center ? work.X + (work.Width - width) / 2 : Math.Clamp(position.X, work.X, work.X + work.Width - width);
        int y = center ? work.Y + (work.Height - height) / 2 : Math.Clamp(position.Y, work.Y, work.Y + work.Height - height);
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void RememberWindowSize()
    {
        if (!Native.GetWindowRect(hwnd, out var bounds)) return;
        var size = bounds.ToRect();
        if (size.Width <= 0 || size.Height <= 0) return;

        double scale = WindowScale;
        SaveWindowSize(new SizeInt32((int)Math.Round(size.Width / scale), (int)Math.Round(size.Height / scale)));
    }

    private static void SaveWindowSize(SizeInt32 size)
    {
        try
        {
            Directory.CreateDirectory(LanguagePreference.DataDirectory);
            string temporaryPath = WindowSizePath + ".tmp";
            File.WriteAllText(temporaryPath, FormattableString.Invariant($"{size.Width} {size.Height}"));
            File.Move(temporaryPath, WindowSizePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}