using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Positioni.Core.UiText;

namespace Positioni.Core;

public enum ImportFormat { Positioni, Sizer, WindowSizer, FancyZones, DisplayFusion, AutoHotkey }

public static class ExternalPresets
{
    public static (List<Preset> Presets, List<string> Warnings) Read(string path, ImportFormat source, Rect? workArea = null)
    {
        if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException(Get("FileSize"));
        string contents = File.ReadAllText(path);
        var result = new List<Preset>();
        var warnings = new List<string>();
        int skipped = 0;

        void Add(Preset preset)
        {
            if (preset.Validate() != null) { skipped++; return; }
            if (result.Count == 500) throw new InvalidDataException(Get("Limit"));
            result.Add(preset);
        }

        if (source == ImportFormat.AutoHotkey)
            ReadAutoHotkey(contents, Add, ref skipped);
        else if (source == ImportFormat.DisplayFusion && Path.GetExtension(path).Equals(".reg", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var profile in ReadDisplayFusionRegistry(contents))
            {
                try
                {
                    using var document = JsonDocument.Parse(profile);
                    ReadDisplayFusion(document.RootElement, Add, ref skipped);
                }
                catch (JsonException) { skipped++; }
            }
            warnings.Add(Get("ExternalGeometry"));
        }
        else
        {
            using var document = JsonDocument.Parse(contents);
            var root = document.RootElement;
            switch (source)
            {
                case ImportFormat.WindowSizer:
                    ReadWindowSizer(root, Add, ref skipped);
                    warnings.Add(Get("ExternalGeometry"));
                    break;
                case ImportFormat.FancyZones when workArea != null:
                    ReadFancyZones(root, workArea, Add, ref skipped);
                    warnings.Add(Get("FancyZonesImport"));
                    break;
                case ImportFormat.DisplayFusion:
                    ReadDisplayFusion(root, Add, ref skipped);
                    warnings.Add(Get("ExternalGeometry"));
                    break;
                default:
                    throw new InvalidDataException(Get("InvalidFormat"));
            }
        }
        if (skipped != 0) warnings.Add(Get("ExternalSkipped", skipped));
        return (result, warnings);
    }

    private static void ReadWindowSizer(JsonElement root, Action<Preset> add, ref int skipped)
    {
        var sizes = Property(root, "sizePresets");
        var layouts = Property(root, "layoutPresets");
        if (sizes.ValueKind != JsonValueKind.Array && layouts.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException(Get("InvalidFormat"));

        foreach (var item in Items(sizes))
        {
            add(new Preset
            {
                Name = Name(Text(item, "name")),
                Notes = Clip(Text(item, "description"), 1000),
                Width = Number(item, "width"),
                Height = Number(item, "height"),
                Placement = 0
            });
        }
        bool enabled = Property(root, "hotkeysEnabled").ValueKind != JsonValueKind.False;
        foreach (var item in Items(layouts))
        {
            var preset = ReadRectangle(item, Name(Text(item, "name")));
            preset.Notes = Clip(Text(item, "targetProcessName"), 1000);
            preset.Enabled = enabled;
            string shortcut = Text(item, "hotkey");
            if (shortcut.Length != 0 && !TryWindowSizerShortcut(shortcut, preset)) skipped++;
            add(preset);
        }
    }

    private static bool TryWindowSizerShortcut(string shortcut, Preset preset)
    {
        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries);
        uint modifiers = 0;
        foreach (string part in parts.SkipLast(1))
        {
            uint value = part.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => 2, "SHIFT" => 4, "ALT" => 1, "WIN" or "WINDOWS" => 8, _ => 0
            };
            if (value == 0) return false;
            modifiers |= value;
        }
        return AssignKey(preset, modifiers, parts[^1]);
    }

    private static bool AssignKey(Preset preset, uint modifiers, string keyText)
    {
        string key = keyText.ToUpperInvariant();
        uint value = key switch
        {
            "BACKSPACE" or "BACK" or "BS" => 0x08,
            "TAB" => 0x09,
            "ENTER" or "RETURN" or "NUMPADENTER" => 0x0D,
            "SPACE" or "SPACEBAR" => 0x20,
            "PGUP" or "PAGEUP" or "PRIOR" => 0x21,
            "PGDN" or "PAGEDOWN" or "NEXT" => 0x22,
            "END" => 0x23,
            "HOME" => 0x24,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            "INS" or "INSERT" => 0x2D,
            "DEL" or "DELETE" => 0x2E,
            "NUMPADMULT" or "MULTIPLY" => 0x6A,
            "NUMPADADD" or "ADD" => 0x6B,
            "SEPARATOR" => 0x6C,
            "NUMPADSUB" or "SUBTRACT" => 0x6D,
            "NUMPADDOT" or "DECIMAL" => 0x6E,
            "NUMPADDIV" or "DIVIDE" => 0x6F,
            _ => key.Length == 1 ? key[0] : 0u
        };
        if (key.Length == 7 && key.StartsWith("NUMPAD", StringComparison.Ordinal) && char.IsAsciiDigit(key[6]))
            value = 0x60u + (uint)(key[6] - '0');
        if (key.Length == 2 && key[0] == 'D' && char.IsAsciiDigit(key[1])) value = key[1];
        if (key.Length > 1 && key[0] == 'F' && uint.TryParse(key.AsSpan(1), out uint function) && function is >= 1 and <= 24)
            value = 111 + function;
        if (modifiers == 0 || !Hotkey.IsKey(value)) return false;
        preset.Modifiers = modifiers;
        preset.Key = value;
        return true;
    }

    private static void ReadFancyZones(JsonElement root, Rect work, Action<Preset> add, ref int skipped)
    {
        var layouts = Property(root, "custom-layouts");
        if (layouts.ValueKind != JsonValueKind.Array) throw new InvalidDataException(Get("InvalidFormat"));
        foreach (var layout in Items(layouts))
        {
            string name = Name(Text(layout, "name"));
            var info = Property(layout, "info");
            IEnumerable<Rect> zones = Text(layout, "type") switch
            {
                "canvas" => CanvasZones(info, work),
                "grid" => GridZones(info, work),
                _ => []
            };
            int index = 0;
            foreach (var zone in zones)
            {
                index++;
                add(new Preset { Name = Clip($"{name} · {index}", 150), Group = name, Placement = 10, AllowOutside = true,
                    X = zone.X, Y = zone.Y, Width = zone.Width, Height = zone.Height });
            }
            if (index == 0) skipped++;
        }
    }

    private static IEnumerable<Rect> CanvasZones(JsonElement info, Rect work)
    {
        int width = Number(info, "ref-width"), height = Number(info, "ref-height");
        if (width <= 0 || height <= 0) yield break;
        foreach (var zone in Items(Property(info, "zones")))
        {
            int x = Number(zone, "X"), y = Number(zone, "Y");
            int w = Number(zone, "width"), h = Number(zone, "height");
            if (x < 0 || y < 0 || w <= 0 || h <= 0 || (long)x + w > width || (long)y + h > height) continue;
            int left = Scale(x, work.Width, width), top = Scale(y, work.Height, height);
            yield return new(work.X + left, work.Y + top, Scale(x + w, work.Width, width) - left,
                Scale(y + h, work.Height, height) - top);
        }
    }

    private static IEnumerable<Rect> GridZones(JsonElement info, Rect work)
    {
        var rows = Edges(Property(info, "rows-percentage"), work.Height);
        var columns = Edges(Property(info, "columns-percentage"), work.Width);
        int height = rows.Length - 1, width = columns.Length - 1;
        if (height != Number(info, "rows") || width != Number(info, "columns") || height < 1 || width < 1)
            yield break;
        var map = Items(Property(info, "cell-child-map")).Select(row => Items(row).Select(Integer).ToArray()).ToArray();
        if (map.Length != height || map.Any(row => row.Length != width)) yield break;
        int spacing = Property(info, "show-spacing").ValueKind == JsonValueKind.False
            ? 0 : Math.Clamp(Number(info, "spacing", 16), 0, Math.Min(work.Width, work.Height));
        var cells = new Dictionary<int, (int Left, int Top, int Right, int Bottom, int Count)>();
        for (int row = 0; row < height; row++)
        for (int col = 0; col < width; col++)
        {
            int id = map[row][col];
            if (id < 0) continue;
            cells[id] = cells.TryGetValue(id, out var bounds)
                ? (Math.Min(bounds.Left, col), Math.Min(bounds.Top, row), Math.Max(bounds.Right, col), Math.Max(bounds.Bottom, row), bounds.Count + 1)
                : (col, row, col, row, 1);
        }
        foreach (var (_, b) in cells.OrderBy(entry => entry.Key))
        {
            if ((b.Right - b.Left + 1) * (b.Bottom - b.Top + 1) != b.Count) continue;
            int left = columns[b.Left] + (b.Left == 0 ? spacing : spacing / 2);
            int top = rows[b.Top] + (b.Top == 0 ? spacing : spacing / 2);
            int right = columns[b.Right + 1] - (b.Right == width - 1 ? spacing : spacing / 2);
            int bottom = rows[b.Bottom + 1] - (b.Bottom == height - 1 ? spacing : spacing / 2);
            yield return new(work.X + left, work.Y + top, right - left, bottom - top);
        }
    }

    private static int[] Edges(JsonElement values, int pixels)
    {
        var weights = Items(values).Select(Integer).ToArray();
        if (weights.Length is < 1 or > 500 || weights.Any(n => n is < 0 or > 10000) || weights.Sum() != 10000)
            return [];
        int accumulated = 0;
        return weights.Select(n => Scale(accumulated += n, pixels, 10000)).Prepend(0).ToArray();
    }

    private static int Scale(int coordinate, int pixels, int reference) => (int)((long)coordinate * pixels / reference);

    private static void ReadDisplayFusion(JsonElement root, Action<Preset> add, ref int skipped)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in root.EnumerateArray()) ReadDisplayFusion(entry, add, ref skipped);
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            var width = Property(root, "width");
            if (width.ValueKind == JsonValueKind.Number &&
                (Property(root, "windowText").ValueKind != JsonValueKind.Undefined ||
                 Property(root, "processPath").ValueKind != JsonValueKind.Undefined ||
                 Property(root, "windowHandle").ValueKind != JsonValueKind.Undefined))
            {
                string title = Text(root, "windowText").Trim('"');
                string process = Path.GetFileNameWithoutExtension(Text(root, "processPath"));
                if (Property(root, "isMinimized").ValueKind == JsonValueKind.True ||
                    Property(root, "isMaximized").ValueKind == JsonValueKind.True) { skipped++; return; }
                add(ReadRectangle(root, Name(title.Length > 0 ? title : process)));
                return;
            }
            foreach (var child in root.EnumerateObject())
                ReadDisplayFusion(child.Value, add, ref skipped);
        }
    }

    private static IEnumerable<string> ReadDisplayFusionRegistry(string text)
    {
        const string section = @"[HKEY_CURRENT_USER\Software\Binary Fortress Software\DisplayFusion\WindowPositionProfiles";
        bool inProfiles = false;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is string raw)
        {
            string line = raw.Trim();
            if (line.StartsWith('['))
            {
                inProfiles = line.StartsWith(section + "]", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(section + @"\", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inProfiles) continue;
            var match = Regex.Match(line, """^(?:"(?:\\.|[^"\\])*"|@)="(?<json>(?:\\.|[^"\\])*)"$""", RegexOptions.NonBacktracking);
            if (match.Success)
            {
                string decoded = Regex.Replace(match.Groups["json"].Value, """\\(["\\])""", "$1");
                if (decoded.StartsWith('{') || decoded.StartsWith('[')) yield return decoded;
            }
        }
    }

    private static void ReadAutoHotkey(string contents, Action<Preset> add, ref int skipped)
    {
        const string binding = """^\s*(?<mods>[\^!+#]{1,4})(?<key>[A-Z0-9]+)::\s*WinMove\s*""";
        const string arguments = """(?<x>-?\d+)\s*,\s*(?<y>-?\d+)\s*,\s*(?<width>\d+)\s*,\s*(?<height>\d+)\s*,\s*"A"\s*""";
        var commands = new Regex(binding + "(?:\\(\\s*" + arguments + "\\)|" + arguments + @")\s*(?:;.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        using var reader = new StringReader(contents);
        bool conditional = false;
        bool inComment = false;
        while (reader.ReadLine() is string line)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("/*", StringComparison.Ordinal)) inComment = true;
            if (inComment)
            {
                if (trimmed.Contains("*/", StringComparison.Ordinal)) inComment = false;
                continue;
            }
            if (trimmed.StartsWith(';')) continue;
            if (trimmed.Equals("#HotIf", StringComparison.OrdinalIgnoreCase) ||
                (trimmed.Length > 6 && trimmed.StartsWith("#HotIf", StringComparison.OrdinalIgnoreCase) && char.IsWhiteSpace(trimmed[6])))
            {
                string condition = trimmed[6..].TrimStart();
                conditional = condition.Length > 0 && !condition.StartsWith(';');
                continue;
            }
            var match = commands.Match(line);
            if (conditional || !match.Success)
            {
                if (line.Contains("WinMove", StringComparison.OrdinalIgnoreCase)) skipped++;
                continue;
            }
            int Read(string name) => int.TryParse(match.Groups[name].Value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int value) ? value : int.MaxValue;
            uint modifiers = 0;
            foreach (char symbol in match.Groups["mods"].Value)
                modifiers |= symbol switch { '^' => 2u, '!' => 1u, '+' => 4u, '#' => 8u, _ => 0u };
            var preset = new Preset { Name = "AutoHotkey", Placement = 10, AllowOutside = true, X = Read("x"), Y = Read("y"),
                Width = Read("width"), Height = Read("height") };
            if (!AssignKey(preset, modifiers, match.Groups["key"].Value)) { skipped++; continue; }
            preset.Name = $"AutoHotkey · {preset.Shortcut}";
            add(preset);
        }
    }

    private static Preset ReadRectangle(JsonElement item, string name) => new()
    {
        Name = name, Placement = 10, AllowOutside = true, X = Number(item, "x", int.MaxValue), Y = Number(item, "y", int.MaxValue),
        Width = Number(item, "width"), Height = Number(item, "height")
    };

    private static string Name(string value) => string.IsNullOrWhiteSpace(value) ? Get("Unnamed") : Clip(value.Trim(), 150);
    private static string Clip(string value, int length) => value.Length <= length ? value : value[..length];
    private static string Text(JsonElement item, string name) =>
        Property(item, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() ?? "" : "";
    private static int Number(JsonElement item, string name, int fallback = 0) =>
        Property(item, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out int n) ? n : fallback;
    private static int Integer(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n) ? n : -1;
    private static IEnumerable<JsonElement> Items(JsonElement value) => value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
    private static JsonElement Property(JsonElement item, string name)
    {
        if (item.ValueKind == JsonValueKind.Object)
            foreach (var property in item.EnumerateObject())
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return default;
    }
}
