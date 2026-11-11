using WindowPositioner.Core;

if(args.FirstOrDefault()=="--window") { DesktopTarget.Run(args[1]); return; }

int passed=0;
void Check(bool condition,string description) { if(!condition) throw new Exception(description); Console.WriteLine("OK: "+description); passed++; }
var screen=new Rect(-1920,0,1920,1040); var current=new Rect(0,20,1000,800);
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=3 },current,screen)==new Rect(-800,0,800,600),"Ancla derecha en monitor con coordenadas negativas");
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=5 },current,screen)==new Rect(-1360,220,800,600),"Centrado sobre el área útil");
Check(Geometry.Resolve(new Preset { Width=3840,Height=2160,Placement=1 },current,screen)==screen,"Recorte al área útil sin salir de pantalla");
Check(Geometry.Resolve(new Preset { Width=800,Height=600,Placement=10,X=-3000,Y=2000,AllowOutside=true },current,screen)==new Rect(-3000,2000,800,600),"Posición absoluta sin recorte cuando se permite");
Check(Geometry.Resolve(new Preset { KeepSize=true,Placement=9 },current,screen)==new Rect(-1000,240,1000,800),"Conservar tamaño y anclar abajo a la derecha");
Check(Hotkey.Format(6,49)=="Ctrl + Shift + 1","Compatibilidad de modificadores de Sizer");
Check(new Preset { Width=0 }.Validate()!=null,"Rechazar dimensiones inválidas");
Check(new Preset { Modifiers=0,Key=49 }.Validate()!=null,"Rechazar atajos sin modificadores");
var dir=Path.Combine(Path.GetTempPath(),"WindowPositioner-Checks-"+Guid.NewGuid()); Directory.CreateDirectory(dir);
try
{
    string json=Path.Combine(dir,"presets.json"); var settings=new Settings { Presets=[new Preset { Name="Prueba á",X=-800,Placement=10,Modifiers=6,Key=49 }] };
    PresetFiles.Save(json,settings); PresetFiles.Save(json,settings);
    Check(PresetFiles.Read(json).Presets[0].X==-800 && File.Exists(json+".bak"),"Persistencia JSON y copia anterior");
    string xml=Path.Combine(dir,"sizer.xml");
    File.WriteAllText(xml,"<sizer><menu><resize description='Derecha' width='1200' height='2088' relocate='3' monitor='-2' hotKey='50' hotKeyModifiers='6'/><resize description='Macro' width='w:mon/2' height='200'/></menu></sizer>");
    var imported=PresetFiles.ReadSizer(xml);
    Check(imported.Presets.Count==1 && imported.Presets[0].Placement==3 && imported.Presets[0].Shortcut=="Ctrl + Shift + 2","Importar Sizer y omitir expresiones con aviso");
    Check(imported.Warnings.Any(w=>w.Contains("expresiones")),"Avisar expresiones omitidas");
    File.WriteAllText(xml,"<!DOCTYPE sizer [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><sizer>&x;</sizer>");
    bool blocked=false; try { PresetFiles.ReadSizer(xml); } catch(System.Xml.XmlException) { blocked=true; }
    Check(blocked,"Bloquear entidades externas en XML");
    if(args.Length>0) { var real=PresetFiles.ReadSizer(args[0]); Check(real.Presets.Count==36,"Importar los 36 presets reales de Sizer"); Console.WriteLine(string.Join("\n",real.Warnings)); }
}
finally { foreach(var file in Directory.GetFiles(dir)) File.Delete(file); Directory.Delete(dir); }
Console.WriteLine($"{passed} comprobaciones superadas.");
