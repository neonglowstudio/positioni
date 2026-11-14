using static Positioni.Core.UiText;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Positioni.Core;

public sealed class Preset : INotifyPropertyChanged
{
    public const int UnsetPlacement = -1;
    public const int UnsetCoordinate = -100001;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Group { get; set; } = "";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WidthPercent { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HeightPercent { get; set; }
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
    [JsonIgnore] public string Dimensions => $"{FormatDimension(Width, WidthPercent)} × {FormatDimension(Height, HeightPercent)}";
    // A zero dimension means unset. A zero percentage with it preserves the selected unit.
    [JsonIgnore] public bool IsComplete => Width != 0 && Height != 0 && Placement != UnsetPlacement &&
        (Placement != 10 || (X != UnsetCoordinate && Y != UnsetCoordinate));
    [JsonIgnore] public bool CanRegisterShortcut => IsComplete && Enabled && Key != 0;
    [JsonIgnore] public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name :
        Width != 0 && Height != 0 ? Dimensions : Get("UnnamedProfile");

    private static string FormatDimension(int pixels, int? percent) =>
        percent is int value ? $"{value} %" : $"{pixels} px";
    [JsonIgnore] public string InactiveLabel => Get("Inactive");
    public Preset Copy() => JsonSerializer.Deserialize<Preset>(JsonSerializer.Serialize(this))!;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ApplyEdits(Preset edited)
    {
        string oldDisplayName = DisplayName;
        Name = edited.Name;
        Notes = edited.Notes;
        Width = edited.Width;
        Height = edited.Height;
        WidthPercent = edited.WidthPercent;
        HeightPercent = edited.HeightPercent;
        X = edited.X;
        Y = edited.Y;
        Placement = edited.Placement;
        PrimaryMonitor = edited.PrimaryMonitor;
        KeepSize = edited.KeepSize;
        AllowOutside = edited.AllowOutside;
        Enabled = edited.Enabled;
        Modifiers = edited.Modifiers;
        Key = edited.Key;

        if (DisplayName != oldDisplayName)
            RefreshDisplayName();
    }

    public void RefreshDisplayName() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));

    public string? Validate()
    {
        if (Name is null || Name.Length > 150) return Get("InvalidName");
        if (Notes is null || Notes.Length > 1000) return Get("InvalidNotes");
        if (Width != 0 && Width is < 100 or > 32768 || Height != 0 && Height is < 80 or > 32768)
            return Get("InvalidSize");
        if (!ValidPercent(Width, WidthPercent) || !ValidPercent(Height, HeightPercent)) return Get("InvalidPercent");
        static bool ValidPercent(int pixels, int? percent) =>
            !percent.HasValue || (pixels == 0 ? percent == 0 : percent is >= 1 and <= 100);
        if (!ValidCoordinate(X) || !ValidCoordinate(Y) || Placement is < UnsetPlacement or > 10)
            return Get("InvalidPosition");
        static bool ValidCoordinate(int value) => value == UnsetCoordinate || Math.Abs((long)value) <= 100000;
        if (Modifiers > 15 || Key > 254 || (Key != 0 && (Modifiers == 0 || !Hotkey.IsKey(Key)))) return Get("InvalidShortcut");
        return null;
    }
}

public static class Hotkey
{
    public static bool IsKey(uint key) => IsRecordableKey(key) || key == 0x7B; // Keep existing F12 profiles readable.

    public static bool IsRecordableKey(uint key) =>
        key is 0x08 or 0x09 or 0x0D or >= 0x20 and <= 0x28 or 0x2D or 0x2E
            or >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A
            or >= 0x60 and <= 0x6F or >= 0x70 and <= 0x87 and not 0x7B;
    public static string Format(uint mods, uint key) =>
        key == 0 ? Get("NoShortcut") : string.Join(" + ", GetKeys(mods, key));

    public static IReadOnlyList<string> GetKeys(uint mods, uint key)
    {
        if (key == 0) return [];
        var parts = new List<string>();
        if ((mods & 2) != 0) parts.Add(Get("Ctrl"));
        if ((mods & 4) != 0) parts.Add(Get("Shift"));
        if ((mods & 1) != 0) parts.Add("Alt");
        if ((mods & 8) != 0) parts.Add("Win");
        parts.Add(KeyLabel(key));
        return parts;
    }

    private static string KeyLabel(uint key) => key switch
    {
        0x08 => Get("KeyBackspace"),
        0x09 => "Tab",
        0x0D => "Enter",
        0x20 => Get("KeySpace"),
        0x21 => Get("KeyPageUp"),
        0x22 => Get("KeyPageDown"),
        0x23 => Get("KeyEnd"),
        0x24 => Get("KeyHome"),
        0x25 => Get("KeyLeft"),
        0x26 => Get("KeyUp"),
        0x27 => Get("KeyRight"),
        0x28 => Get("KeyDown"),
        0x2D => Get("KeyInsert"),
        0x2E => Get("KeyDelete"),
        >= 0x60 and <= 0x69 => $"Num {key - 0x60}",
        0x6A => "Num ×",
        0x6B => "Num +",
        0x6C => Get("KeySeparator"),
        0x6D => "Num −",
        0x6E => Get("KeyDecimal"),
        0x6F => "Num ÷",
        >= 0x70 and <= 0x87 => $"F{key - 0x6F}",
        _ => ((char)key).ToString()
    };
}

public record Rect(int X, int Y, int Width, int Height);
public static class Geometry
{
    public static int PercentToPixels(int percent, int extent) =>
        Math.Max(1, (int)Math.Round(extent * (percent / 100.0), MidpointRounding.AwayFromZero));

    public static Rect? ReservedMonitorEdge(Rect screen, Rect work)
    {
        int left = work.X - screen.X;
        int top = work.Y - screen.Y;
        int right = screen.X + screen.Width - work.X - work.Width;
        int bottom = screen.Y + screen.Height - work.Y - work.Height;
        int thickness = Math.Max(Math.Max(left, right), Math.Max(top, bottom));
        if (thickness <= 0) return null;

        if (thickness == bottom)
            return new(screen.X, screen.Y + screen.Height - bottom, screen.Width, bottom);
        if (thickness == top)
            return new(screen.X, screen.Y, screen.Width, top);
        if (thickness == left)
            return new(screen.X, screen.Y, left, screen.Height);

        return new(screen.X + screen.Width - right, screen.Y, right, screen.Height);
    }

    public static Rect Resolve(Preset p, Rect current, Rect work)
    {
        int w = p.WidthPercent is int widthPercent ? PercentToPixels(widthPercent, work.Width) : p.Width;
        int h = p.HeightPercent is int heightPercent ? PercentToPixels(heightPercent, work.Height) : p.Height;
        if (p.KeepSize) { w = current.Width; h = current.Height; }
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
        if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException(Get("FileSize"));
        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options) ?? throw new InvalidDataException(Get("EmptyFile"));
        if (settings.Version != 1 || settings.Presets == null || settings.Presets.Count > 500) throw new InvalidDataException(Get("InvalidFormat"));
        foreach (var p in settings.Presets) if (p == null || p.Validate() is string error) throw new InvalidDataException(p?.Validate() ?? Get("InvalidPreset"));
        return settings;
    }
    public static void Save(string path, Settings settings, bool keepBackup = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Options));
        string backup = path + ".bak";
        if (!keepBackup) File.Delete(backup);
        if (File.Exists(path)) File.Replace(tmp, path, keepBackup ? backup : null);
        else File.Move(tmp, path);
    }
    public static (List<Preset> Presets, List<string> Warnings) ReadSizer(string path)
    {
        if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException(Get("FileSize"));
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var doc = XDocument.Load(reader);
        if (doc.Root?.Name != "sizer") throw new InvalidDataException(Get("NotSizer"));
        var result = new List<Preset>(); var warnings = new List<string>();
        foreach (var e in doc.Descendants("resize"))
        {
            string name = (string?)e.Attribute("description") ?? Get("Unnamed");
            int Number(string a, int fallback = 0) => int.TryParse((string?)e.Attribute(a), out int n) ? n : fallback;
            if (!int.TryParse((string?)e.Attribute("width"), out int w) || !int.TryParse((string?)e.Attribute("height"), out int h))
            { warnings.Add(Get("SkipDimensions", name)); continue; }
            int placement = Number("relocate", -1), monitor = Number("monitor", -99);
            if (placement is < 0 or > 9 || monitor is not (-2 or -1))
            { warnings.Add(Get("SkipPlacement", name)); continue; }
            var p = new Preset { Name = name, Group = (string?)e.Parent?.Attribute("description") ?? "", Width = w, Height = h,
                Placement = placement, PrimaryMonitor = monitor == -1, KeepSize = Number("noresize") == 1,
                AllowOutside = Number("allowOutsideBounds") == 1, Modifiers = (uint)Number("hotKeyModifiers"), Key = (uint)Number("hotKey") };
            if (p.Key == 0) p.Modifiers = 0;
            if (p.Validate() is string error) { warnings.Add(Get("SkipInvalid", name, error)); continue; }
            result.Add(p);
        }
        if (doc.Descendants("macro").Any()) warnings.Add(Get("SkipMacros"));
        if (result.Count > 500) throw new InvalidDataException(Get("Limit"));
        warnings.Add(Get("SizerMonitors"));
        return (result, warnings);
    }
}
