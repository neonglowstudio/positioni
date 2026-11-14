using Positioni.Core;

if(args.FirstOrDefault()=="--window") { DesktopTarget.Run(args[1]); return; }

UiText.Language = "en";
int passed=0;
void Check(bool condition,string description) { if(!condition) throw new Exception(description); Console.WriteLine("OK: "+description); passed++; }
var screen=new Rect(-1920,0,1920,1040); var current=new Rect(0,20,1000,800);
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=3 },current,screen)==new Rect(-800,0,800,600),"Ancla derecha en monitor con coordenadas negativas");
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=5 },current,screen)==new Rect(-1360,220,800,600),"Centrado sobre el área útil");
Check(Geometry.Resolve(new Preset { Width=3840,Height=2160,Placement=1 },current,screen)==screen,"Recorte al área útil sin salir de pantalla");
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=10,X=-3000,Y=2000,AllowOutside=true },current,screen)==new Rect(-3000,2000,800,600),"Posición absoluta sin recorte cuando se permite");
Check(Geometry.Resolve(new Preset { KeepSize=true,Placement=9 },current,screen)==new Rect(-1000,240,1000,800),"Conservar tamaño y anclar abajo a la derecha");
Check(Geometry.ReservedMonitorEdge(screen, new Rect(-1920, 0, 1920, 1000)) == new Rect(-1920, 1000, 1920, 40) &&
    Geometry.ReservedMonitorEdge(screen, new Rect(-1920, 40, 1920, 1000)) == new Rect(-1920, 0, 1920, 40) &&
    Geometry.ReservedMonitorEdge(screen, new Rect(-1880, 0, 1880, 1040)) == new Rect(-1920, 0, 40, 1040) &&
    Geometry.ReservedMonitorEdge(screen, new Rect(-1920, 0, 1880, 1040)) == new Rect(-40, 0, 40, 1040) &&
    Geometry.ReservedMonitorEdge(screen, screen) is null,
    "Preview uses the reserved strip on any monitor edge");
var mixed = new Preset { WidthPercent = 50, Height = 600, Placement = 3 };
Check(Geometry.Resolve(mixed, current, screen) == new Rect(-960, 0, 960, 600),
    "Mixed units resolve against the selected monitor work area");
var relative = new Preset { WidthPercent = 50, HeightPercent = 75, Placement = 1 };
Check(Geometry.Resolve(relative, current, new Rect(1920, 0, 2560, 1360)) == new Rect(1920, 0, 1280, 1020),
    "Both percentage dimensions follow the work area on another monitor");
relative.KeepSize = true;
Check(Geometry.Resolve(relative, current, screen) == new Rect(-1920, 0, 1000, 800),
    "Keep size takes precedence over percentages");
Check(new Preset { WidthPercent = 0 }.Validate() != null && new Preset { HeightPercent = 101 }.Validate() != null,
    "Reject percentages outside 1–100");
Check(new uint[] { 8, 9, 13, 32, 33, 36, 37, 40, 45, 46, 96, 111, 124, 135 }
    .All(key => Hotkey.IsRecordableKey(key) && new Preset { Modifiers = 6, Key = key }.Validate() is null) &&
    !Hotkey.IsRecordableKey(27) && !Hotkey.IsRecordableKey(123) && Hotkey.IsKey(123) &&
    Hotkey.Format(6, 32) == "Ctrl + Shift + Space" && Hotkey.Format(6, 135) == "Ctrl + Shift + F24",
    "Extended shortcut keys have readable labels and keep legacy F12 profiles readable");
Check(Hotkey.Format(6,49)=="Ctrl + Shift + 1","Compatibilidad de modificadores de Sizer");
Check(new Preset { Width = 0 }.Validate() is null && new Preset { Width = -1 }.Validate() is not null,
    "Allow unset dimensions in partial profiles and reject negative dimensions");
Check(new Preset { Modifiers=0,Key=49 }.Validate()!=null,"Rechazar atajos sin modificadores");
foreach (string language in UiText.LanguageCodes)
{
    UiText.Language = language;
    Check(new Preset { Key = 0 }.Shortcut == UiText.Get("NoShortcut")
          && !string.IsNullOrWhiteSpace(UiText.Get("ImportPreview", 2)), $"Localized preset text ({language})");
}
UiText.Language = "en";
const string releasesJson = """
    {"draft":false,"prerelease":false,"tag_name":"v0.10.0","assets":[
      {"name":"positioni-0.10.0-win-x64-setup.exe","browser_download_url":"https://github.com/neonglowstudio/positioni/releases/download/v0.10.0/positioni-0.10.0-win-x64-setup.exe","size":600,"digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"},
      {"name":"positioni-0.10.0-win-x64-portable.zip","browser_download_url":"https://github.com/neonglowstudio/positioni/releases/download/v0.10.0/positioni-0.10.0-win-x64-portable.zip","size":400}
    ]}
    """;
var installedOffer = PositioniRelease.Parse(releasesJson, new Version(0, 9, 9), portable: false);
var portableOffer = PositioniRelease.Parse(releasesJson, new Version(0, 9, 9), portable: true);
Check(installedOffer?.Asset is { Size: 600, Sha256.Length: 64 } && installedOffer.Notes.AbsolutePath.EndsWith("/tag/v0.10.0") &&
    portableOffer?.Asset is { Size: 400 }, "Update check compares numeric versions and selects the right release asset");
const string installerFile = "positioni-0.10.0-win-x64-installer.exe";
var installerJson = releasesJson.Replace("positioni-0.10.0-win-x64-setup.exe", installerFile);
Check(PositioniRelease.Parse(installerJson, new Version(0, 9, 9), portable: false)?.Asset?.Download.Segments[^1] == installerFile,
    "Update check recognizes installer assets with the new naming");
var bothInstallerNames = releasesJson.Replace(
    """{"name":"positioni-0.10.0-win-x64-portable.zip","browser_download_url":"https://github.com/neonglowstudio/positioni/releases/download/v0.10.0/positioni-0.10.0-win-x64-portable.zip","size":400}""",
    $$"""{"name":"{{installerFile}}","browser_download_url":"https://github.com/neonglowstudio/positioni/releases/download/v0.10.0/{{installerFile}}","size":600}""");
Check(PositioniRelease.Parse(bothInstallerNames, new Version(0, 9, 9), portable: false)?.Asset?.Download.Segments[^1] == installerFile,
    "Update check prefers the new installer name when both names are available");
var legacyAssetNames = releasesJson.Replace("positioni-0.10.0-win-x64-", "Positioni-0.10.0-win-x64-");
Check(PositioniRelease.Parse(legacyAssetNames, new Version(0, 9, 9), portable: true)?.Asset?.Download.AbsolutePath
    .EndsWith("/Positioni-0.10.0-win-x64-portable.zip", StringComparison.Ordinal) == true,
    "Update download links preserve the casing of legacy asset names");
var noPrefix = PositioniRelease.Parse(releasesJson.Replace("v0.10.0", "0.10.0"), new Version(0, 9, 9), portable: false);
Check(noPrefix?.Asset is not null && noPrefix.Notes.AbsolutePath.EndsWith("/tag/0.10.0"),
    "Release links use the exact numeric tag without a v prefix");
Check(PositioniRelease.Parse(releasesJson, new Version(0, 10, 0), portable: false) == null &&
    PositioniRelease.Parse(releasesJson, new Version(0, 11, 0), portable: false) == null &&
    PositioniRelease.Parse(releasesJson.Replace("\"prerelease\":false","\"prerelease\":true"), new Version(0, 9, 9), false) == null,
    "Update check rejects equal, older and prerelease versions");
Check(PositioniRelease.Parse(releasesJson.Replace("https://github.com/","https://example.com/"), new Version(0, 9, 9), false) is { Asset: null },
    "Update check rejects assets outside the Positioni repository");

var dir=Path.Combine(Path.GetTempPath(),"Positioni-Checks-"+Guid.NewGuid()); Directory.CreateDirectory(dir);
try
{
    string json=Path.Combine(dir,"presets.json"); var settings=new Settings { Presets=[new Preset { Name="Prueba á",X=-800,Placement=10,Modifiers=6,Key=49 }] };
    PresetFiles.Save(json,settings); PresetFiles.Save(json,settings);
    Check(PresetFiles.Read(json).Presets[0].X==-800 && File.Exists(json+".bak"),"Persistencia JSON y copia anterior");
    string exported = Path.Combine(dir, "exported.json");
    PresetFiles.Save(exported, PresetFiles.Read(json));
    PresetFiles.Save(json, new Settings(), keepBackup: false);
    Check(PresetFiles.Read(json).Presets.Count == 0 && !File.Exists(json + ".bak") &&
        PresetFiles.Read(exported).Presets.Single().X == -800,
        "Bulk deletion keeps the empty profile store and preserves the exported copy");
    PresetFiles.Save(json, new Settings { Presets = [new Preset { Notes = "Primera línea\nSegunda línea" }] });
    Check(PresetFiles.Read(json).Presets.Single().Copy().Notes == "Primera línea\nSegunda línea", "Notes round-trip and duplicate");
    File.WriteAllText(json, """{"Version":1,"Presets":[{"Name":"Legacy","Width":1280,"Height":720}]}""");
    Check(PresetFiles.Read(json).Presets.Single().Notes == "", "Legacy Positioni backup opens without notes");
    Check(PresetFiles.Read(json).Presets.Single() is { Width: 1280, Height: 720, WidthPercent: null, HeightPercent: null },
        "Legacy dimensions stay in pixels");
    PresetFiles.Save(json, new Settings { Presets = [mixed] });
    var restored = PresetFiles.Read(json).Presets.Single().Copy();
    Check(restored is { Name: "", WidthPercent: 50, Height: 600, HeightPercent: null }
        && restored.DisplayName == "50 % × 600 px", "An unnamed profile keeps mixed units when saved, imported or copied");
    restored.Name = "Custom profile";
    Check(restored.DisplayName == "Custom profile" && new Preset { Name = new string('a', 151) }.Validate() != null,
        "A custom name takes precedence over dimensions and has a length limit");

    File.WriteAllText(json, """
        {"sizePresets":[{"name":"HD","width":1280,"height":720,"description":"Video"}],
         "layoutPresets":[{"name":"Left","x":-1920,"y":0,"width":960,"height":1000,"hotkey":"Ctrl+Alt+F2","targetProcessName":"notepad"}]}
        """);
    var windowSizer = ExternalPresets.Read(json, ImportFormat.WindowSizer);
    Check(windowSizer.Presets is [{Placement:0, Notes:"Video"}, {Placement:10, AllowOutside:true, X:-1920, Key:113, Modifiers:3, Notes:"notepad"}],
          "WindowSizer imports size notes, coordinates and supported shortcuts");

    File.WriteAllText(json, """
        {"custom-layouts":[{"name":"Left","type":"canvas","info":{"ref-width":960,"ref-height":520,"zones":[{"X":0,"Y":0,"width":480,"height":520}]}},
          {"name":"Grid","type":"grid","info":{"rows":2,"columns":2,"rows-percentage":[5000,5000],"columns-percentage":[5000,5000],
           "cell-child-map":[[0,1],[0,2]],"show-spacing":false}}]}
        """);
    var fancyZones = ExternalPresets.Read(json, ImportFormat.FancyZones, screen);
    Check(fancyZones.Presets is [{ X:-1920, Width:960, Height:1040, Key:0 },
        { X:-1920, Width:960, Height:1040 }, { X:-960, Y:0, Height:520 }, { X:-960, Y:520, Height:520 }],
        "FancyZones scales canvas and merged grid zones on a negative-origin monitor");

    const string fusion = """{"windows":[{"windowText":"Browser","x":-1600,"y":20,"width":800,"height":600},{"windowText":"Hidden","x":10,"y":10,"width":600,"height":400,"isMinimized":true}]}""";
    File.WriteAllText(json, fusion);
    var displayFusion = ExternalPresets.Read(json, ImportFormat.DisplayFusion);
    Check(displayFusion.Presets is [{ Name:"Browser", X:-1600, Y:20 }] && displayFusion.Warnings.Count == 2,
        "DisplayFusion imports geometry and skips minimized entries");
    string registry = Path.Combine(dir, "backup.reg");
    File.WriteAllText(registry, "Windows Registry Editor Version 5.00\r\n\r\n" +
        "[HKEY_CURRENT_USER\\Software\\Binary Fortress Software\\DisplayFusion\\WindowPositionProfiles]\r\n" +
        "\"Profile\"=\"" + fusion.Replace("\"","\\\"") + "\"\r\n");
    Check(ExternalPresets.Read(registry, ImportFormat.DisplayFusion).Presets.Single().Name == "Browser",
        "DisplayFusion reads exported string data without writing to the registry");

    string ahk = Path.Combine(dir, "window.ahk");
    File.WriteAllText(ahk, """
        #Requires AutoHotkey v2.0
        ^+1::WinMove -1920, 0, 960, 1040, "A"
        !F2::WinMove(0, 0, 960, 1040, "A")
        ^+3::WinMove 0, 0, A_ScreenWidth, 1040, "A"
        ^+4::WinMove 0, 0, 960, 1040, "Notepad"
        #HotIf WinActive("Notepad")
        ^+5::WinMove 0, 0, 960, 1040, "A"
        """);
    var autoHotkey = ExternalPresets.Read(ahk, ImportFormat.AutoHotkey);
    Check(autoHotkey.Presets is [{ X:-1920, Key:49, Modifiers:6 }, { Key:113, Modifiers:1 }] &&
          autoHotkey.Warnings.Any(), "AutoHotkey imports literal v2 moves of the active window only");

    string xml=Path.Combine(dir,"sizer.xml");
    File.WriteAllText(xml,"<sizer><menu><resize description='Derecha' width='1200' height='2088' relocate='3' monitor='-2' hotKey='50' hotKeyModifiers='6'/><resize description='Macro' width='w:mon/2' height='200'/></menu></sizer>");
    var imported=PresetFiles.ReadSizer(xml);
    Check(imported.Presets.Count==1 && imported.Presets[0].Placement==3 && imported.Presets[0].Shortcut=="Ctrl + Shift + 2","Importar Sizer y omitir expresiones con aviso");
    Check(imported.Warnings.Any(w=>w.Contains("expressions")),"Avisar expresiones omitidas");
    File.WriteAllText(xml,"<!DOCTYPE sizer [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><sizer>&x;</sizer>");
    bool blocked=false; try { PresetFiles.ReadSizer(xml); } catch(System.Xml.XmlException) { blocked=true; }
    Check(blocked,"Bloquear entidades externas en XML");
    if(args.Length>0) { var real=PresetFiles.ReadSizer(args[0]); Check(real.Presets.Count==36,"Importar los 36 presets reales de Sizer"); Console.WriteLine(string.Join("\n",real.Warnings)); }
}
finally { foreach(var file in Directory.GetFiles(dir)) File.Delete(file); Directory.Delete(dir); }
Console.WriteLine($"{passed} comprobaciones superadas.");
