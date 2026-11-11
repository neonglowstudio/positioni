using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace WindowPositioner.Core;

public sealed class Preset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nuevo preset";
    public string Group { get; set; } = "";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public int X { get; set; }
    public int Y { get; set; }
    // 0: conservar posición; 1..9: anclas en cuadrícula; 10: coordenadas absolutas.
    public int Placement { get; set; } = 5;
    public bool PrimaryMonitor { get; set; }
    public bool KeepSize { get; set; }
    public bool AllowOutside { get; set; }
    public bool Enabled { get; set; } = true;
    public uint Modifiers { get; set; }
    public uint Key { get; set; }
    [JsonIgnore] public string Shortcut => Hotkey.Format(Modifiers, Key);
    [JsonIgnore] public string Summary => $"{Width} × {Height}  ·  {Shortcut}{(!Enabled ? " · Inactivo" : "")}";
    public Preset Copy() => JsonSerializer.Deserialize<Preset>(JsonSerializer.Serialize(this))!;
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 150) return "El nombre debe tener entre 1 y 150 caracteres.";
        if (Width < 100 || Height < 80 || Width > 32768 || Height > 32768) return "Tamaño admitido: de 100 × 80 a 32768 × 32768 píxeles.";
        if (Math.Abs((long)X) > 100000 || Math.Abs((long)Y) > 100000 || Placement < 0 || Placement > 10) return "Posición fuera del rango permitido.";
        if (Modifiers > 15 || Key > 254 || (Key != 0 && (Modifiers == 0 || !Hotkey.IsKey(Key)))) return "El atajo debe incluir un modificador y una letra, número o tecla de función.";
        return null;
    }
}

public static class Hotkey
{
    public static bool IsKey(uint k) => k is >= 48 and <= 57 or >= 65 and <= 90 or >= 112 and <= 123;
    public static string Format(uint mods, uint key)
    {
        if (key == 0) return "Sin atajo";
        var parts = new List<string>();
        if ((mods & 2) != 0) parts.Add("Ctrl");
        if ((mods & 4) != 0) parts.Add("Shift");
        if ((mods & 1) != 0) parts.Add("Alt");
        if ((mods & 8) != 0) parts.Add("Win");
        parts.Add(key is >= 112 and <= 123 ? $"F{key - 111}" : ((char)key).ToString());
        return string.Join(" + ", parts);
    }
}

public record Rect(int X, int Y, int Width, int Height);
public static class Geometry
{
    public static Rect Resolve(Preset p, Rect current, Rect work)
    {
        int w = p.KeepSize ? current.Width : p.Width, h = p.KeepSize ? current.Height : p.Height;
        if (!p.AllowOutside) { w = Math.Min(w, work.Width); h = Math.Min(h, work.Height); }
        int x = current.X, y = current.Y;
        if (p.Placement == 10) { x = p.X; y = p.Y; }
        else if (p.Placement is >= 1 and <= 9)
        {
            int col = (p.Placement - 1) % 3, row = (p.Placement - 1) / 3;
            x = work.X + (work.Width - w) * col / 2;
            y = work.Y + (work.Height - h) * row / 2;
        }
        if (!p.AllowOutside) { x = Math.Clamp(x, work.X, work.X + work.Width - w); y = Math.Clamp(y, work.Y, work.Y + work.Height - h); }
        return new(x, y, w, h);
    }
}

public sealed class Settings
{
    public int Version { get; set; } = 1;
    public List<Preset> Presets { get; set; } = [];
}

public static class PresetFiles
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static Settings Read(string path)
    {
        if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException("El archivo supera los 2 MB.");
        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Archivo vacío.");
        if (settings.Version != 1 || settings.Presets == null || settings.Presets.Count > 500) throw new InvalidDataException("Formato o cantidad de presets no admitidos.");
        foreach (var p in settings.Presets) if (p == null || p.Validate() is string error) throw new InvalidDataException(p?.Validate() ?? "Preset inválido.");
        return settings;
    }
    public static void Save(string path, Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Options));
        if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp, path);
    }
    public static (List<Preset> Presets, List<string> Warnings) ReadSizer(string path)
    {
        if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException("El XML supera los 2 MB.");
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var doc = XDocument.Load(reader);
        if (doc.Root?.Name != "sizer") throw new InvalidDataException("No es un archivo de Sizer.");
        var result = new List<Preset>(); var warnings = new List<string>();
        foreach (var e in doc.Descendants("resize"))
        {
            string name = (string?)e.Attribute("description") ?? "Sin nombre";
            int Number(string a, int fallback = 0) => int.TryParse((string?)e.Attribute(a), out int n) ? n : fallback;
            if (!int.TryParse((string?)e.Attribute("width"), out int w) || !int.TryParse((string?)e.Attribute("height"), out int h))
            { warnings.Add($"{name}: se omitió porque usa expresiones o dimensiones no numéricas."); continue; }
            int placement = Number("relocate", -1), monitor = Number("monitor", -99);
            if (placement is < 0 or > 9 || monitor is not (-2 or -1))
            { warnings.Add($"{name}: se omitió por posición o monitor no compatible."); continue; }
            var p = new Preset { Name = name, Group = (string?)e.Parent?.Attribute("description") ?? "", Width = w, Height = h,
                Placement = placement, PrimaryMonitor = monitor == -1, KeepSize = Number("noresize") == 1,
                AllowOutside = Number("allowOutsideBounds") == 1, Modifiers = (uint)Number("hotKeyModifiers"), Key = (uint)Number("hotKey") };
            if (p.Key == 0) p.Modifiers = 0;
            if (p.Validate() is string error) { warnings.Add($"{name}: {error} Se omitió."); continue; }
            result.Add(p);
        }
        if (doc.Descendants("macro").Any()) warnings.Add("Las macros de Sizer no se importan.");
        if (result.Count > 500) throw new InvalidDataException("Máximo: 500 presets.");
        warnings.Add("Sizer: monitor -2 se interpreta como el de la ventana activa; -1 como el principal. Las anclas se aplican al área útil, sin la barra de tareas. Revisá la vista previa antes de importar.");
        return (result, warnings);
    }
}
