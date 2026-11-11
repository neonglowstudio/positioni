using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Windows.Storage.Pickers;
using Windows.System;
using WindowPositioner.Core;

namespace WindowPositioner;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<Preset> presets = [];
    private readonly string dataPath;
    private readonly nint hwnd;
    private readonly DesktopHost host;
    private Preset? selected;
    private uint editModifiers, editKey;
    private bool loading, recording, pausedBeforeRecording, busy;
    private bool paused;
    private static readonly string[] Placements = ["Conservar posición actual", "Arriba a la izquierda", "Arriba al centro", "Arriba a la derecha", "Centro a la izquierda", "Centrar", "Centro a la derecha", "Abajo a la izquierda", "Abajo al centro", "Abajo a la derecha", "Coordenadas exactas (X, Y)"];

    public MainWindow()
    {
        InitializeComponent();
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        double scale=Native.GetDpiForWindow(hwnd)/96.0;
        AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(1100*scale), (int)(900*scale)));
        PlacementInput.ItemsSource = Placements;
        PresetList.ItemsSource = presets;
        dataPath = Path.Combine(Environment.GetEnvironmentVariable("WINDOW_POSITIONER_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowPositioner"),"presets.json");
        string? loadError = null;
        try
        {
            if (File.Exists(dataPath)) foreach (var p in PresetFiles.Read(dataPath).Presets) presets.Add(p);
            else
            {
                presets.Add(new Preset { Name="Izquierda", Width=1280, Height=960, Placement=1, Modifiers=6, Key=49 });
                presets.Add(new Preset { Name="Derecha", Width=1280, Height=960, Placement=3, Modifiers=6, Key=50 });
            }
        }
        catch(Exception e) { loadError=$"No se pudo cargar tu configuración: {e.Message} El archivo original se conserva. Usá Importar o creá un preset."; }
        host = new DesktopHost(hwnd);
        host.Pressed = p => { if(!busy) ApplyToActive(p); };
        host.Open = ShowEditor;
        host.Toggle = TogglePause;
        host.Quit = () => { host.Quitting=true; Close(); };
        Closed += (_,_) => host.Dispose();
        try { using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"); Startup.IsChecked=key?.GetValue("WindowPositioner") != null; }
        catch { Startup.IsEnabled=false; }
        RefreshCount();
        PresetList.SelectedIndex = presets.Count>0 ? 0 : -1;
        Editor.IsEnabled = selected != null;
        var errors=RefreshHotkeys();
        Notify(loadError ?? (errors.Count>0 ? string.Join("\n",errors) : "Preparado. Podés importar el respaldo XML de Sizer."),loadError!=null||errors.Count>0);
        if(!host.TrayAvailable) Notify("No se pudo crear el icono junto al reloj. Cerrar esta ventana saldrá de la aplicación.",true);
    }
    private void Notify(string message,bool warning=false) { Status.Message=message; Status.Severity=warning?InfoBarSeverity.Warning:InfoBarSeverity.Informational; Status.IsOpen=true; }
    private void ShowEditor() { Native.ShowWindow(hwnd,9); Activate(); Native.SetForegroundWindow(hwnd); }
    private void RefreshCount() => CountLabel.Text=$"PRESETS · {presets.Count}";
    private List<string> RefreshHotkeys() => host.Register(presets,paused);
    private void Persist()
    {
        PresetFiles.Save(dataPath,new Settings { Presets=presets.ToList() });
        RefreshCount();
    }
    private void Selection_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading) return;
        selected=PresetList.SelectedItem as Preset; Editor.IsEnabled=selected!=null;
        if(selected==null) return;
        loading=true;
        PresetName.Text=selected.Name; WidthInput.Value=selected.Width; HeightInput.Value=selected.Height;
        XInput.Value=selected.X; YInput.Value=selected.Y; PlacementInput.SelectedIndex=selected.Placement;
        PrimaryInput.IsChecked=selected.PrimaryMonitor; KeepSizeInput.IsChecked=selected.KeepSize; OutsideInput.IsChecked=selected.AllowOutside;
        EnabledInput.IsChecked=selected.Enabled; editModifiers=selected.Modifiers; editKey=selected.Key;
        RecordButton.Content=selected.Shortcut; Coordinates.Visibility=selected.Placement==10?Visibility.Visible:Visibility.Collapsed;
        loading=false;
    }
    private void Placement_Changed(object sender,SelectionChangedEventArgs e)
    { if(Coordinates!=null) Coordinates.Visibility=PlacementInput.SelectedIndex==10?Visibility.Visible:Visibility.Collapsed; }
    private Preset ReadEditor()
    {
        if(selected==null) throw new InvalidOperationException("Elegí un preset.");
        int Number(NumberBox input) => double.IsFinite(input.Value)?checked((int)Math.Round(input.Value)):throw new InvalidDataException("Completá los campos numéricos.");
        var p=selected.Copy(); p.Name=PresetName.Text.Trim(); p.Width=Number(WidthInput); p.Height=Number(HeightInput);
        p.X=Number(XInput); p.Y=Number(YInput); p.Placement=PlacementInput.SelectedIndex; p.PrimaryMonitor=PrimaryInput.IsChecked==true;
        p.KeepSize=KeepSizeInput.IsChecked==true; p.AllowOutside=OutsideInput.IsChecked==true; p.Enabled=EnabledInput.IsChecked==true;
        p.Modifiers=editModifiers; p.Key=editKey;
        if(p.Validate() is string error) throw new InvalidDataException(error);
        return p;
    }
    private void Save_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var p=ReadEditor();
            if(p.Enabled && p.Key!=0 && presets.Any(q=>q.Id!=p.Id && q.Enabled && q.Key==p.Key && q.Modifiers==p.Modifiers))
                throw new InvalidDataException("Otro preset activo ya usa ese atajo.");
            int i=presets.IndexOf(selected!); presets[i]=p; PresetList.SelectedItem=p;
            Persist(); var errors=RefreshHotkeys(); Notify(errors.Count>0?"Guardado, con atajos no disponibles:\n"+string.Join("\n",errors):"Preset guardado.",errors.Count>0);
        }
        catch(Exception ex) { Notify(ex.Message,true); }
    }
    private void Add_Click(object sender,RoutedEventArgs e)
    {
        if(presets.Count>=500) { Notify("Máximo: 500 presets.",true); return; }
        var p=new Preset(); presets.Add(p); PresetList.SelectedItem=p; SaveCollection();
    }
    private void Duplicate_Click(object sender,RoutedEventArgs e)
    {
        if(selected==null || presets.Count>=500) return;
        var p=selected.Copy(); p.Id=Guid.NewGuid(); p.Name+=" (copia)"; p.Modifiers=0; p.Key=0;
        presets.Insert(presets.IndexOf(selected)+1,p); PresetList.SelectedItem=p; SaveCollection();
    }
    private async void Delete_Click(object sender,RoutedEventArgs e)
    {
        if(selected==null) return;
        var victim=selected;
        if(await Dialog("Borrar preset",$"¿Borrar «{victim.Name}»?","Borrar")!=ContentDialogResult.Primary) return;
        int i=presets.IndexOf(victim); presets.Remove(victim); PresetList.SelectedIndex=Math.Min(i,presets.Count-1); SaveCollection(); RefreshHotkeys();
    }
    private void Up_Click(object sender,RoutedEventArgs e) => Move(-1);
    private void Down_Click(object sender,RoutedEventArgs e) => Move(1);
    private void Move(int delta)
    {
        if(selected==null) return; int i=presets.IndexOf(selected), j=i+delta;
        if(j<0||j>=presets.Count) return; var p=selected; presets.Move(i,j); PresetList.SelectedItem=p; SaveCollection();
    }
    private void SaveCollection() { try { Persist(); } catch(Exception ex) { Notify("No se pudo guardar: "+ex.Message,true); } }
    private void Pause_Click(object sender,RoutedEventArgs e) => TogglePause();
    private void TogglePause()
    {
        paused=!paused; var errors=RefreshHotkeys(); PauseButton.Content=paused?"Reactivar atajos":"Pausar atajos";
        Notify(errors.Count>0?string.Join("\n",errors):paused?"Atajos en pausa.":"Atajos activos.",errors.Count>0);
    }
    private void Record_Click(object sender,RoutedEventArgs e)
    {
        if(recording) { StopRecording(); return; }
        recording=true; pausedBeforeRecording=paused; host.Register(presets,true); host.Recording=true;
        RecordButton.Content="Pulsá la combinación…"; RecordButton.Focus(FocusState.Programmatic);
    }
    private void StopRecording()
    {
        if(!recording) return; recording=false; host.Recording=false; host.Register(presets,pausedBeforeRecording);
        RecordButton.Content=Hotkey.Format(editModifiers,editKey);
    }
    private void Record_LostFocus(object sender,RoutedEventArgs e) => StopRecording();
    private void Record_KeyDown(object sender,KeyRoutedEventArgs e)
    {
        if(!recording) return; e.Handled=true;
        if(e.Key==VirtualKey.Escape) { StopRecording(); return; }
        uint key=(uint)e.Key, mods=Native.Modifiers();
        if(!Hotkey.IsKey(key)||mods==0) return;
        editKey=key; editModifiers=mods; StopRecording(); Notify("Atajo capturado. Guardá el preset para activarlo.");
    }
    private void ClearShortcut_Click(object sender,RoutedEventArgs e) { StopRecording(); editKey=editModifiers=0; RecordButton.Content="Sin atajo"; }
    private async void Capture_Click(object sender,RoutedEventArgs e)
    {
        if(busy||selected==null) return; busy=true;
        try
        {
            Native.ShowWindow(hwnd,6); await Task.Delay(4000);
            var target=Native.GetForegroundWindow(); if(target==0||target==hwnd) throw new InvalidOperationException("Seleccioná otra ventana para capturarla.");
            var r=Native.Bounds(target); XInput.Value=r.X; YInput.Value=r.Y; WidthInput.Value=r.Width; HeightInput.Value=r.Height;
            PlacementInput.SelectedIndex=10; KeepSizeInput.IsChecked=false;
            Notify("Posición y tamaño capturados. Guardá el preset para conservarlos.");
        }
        catch(Exception ex) { Notify(ex.Message,true); }
        finally { ShowEditor(); busy=false; }
    }
    private async void Apply_Click(object sender,RoutedEventArgs e)
    {
        if(busy) return;
        try { var p=ReadEditor(); busy=true; Native.ShowWindow(hwnd,6); await Task.Delay(3000); ApplyToActive(p); }
        catch(Exception ex) { Notify(ex.Message,true); ShowEditor(); }
        finally { busy=false; }
    }
    private async void ApplyToActive(Preset p)
    {
        try
        {
            nint target=Native.GetForegroundWindow();
            if(target==0||target==hwnd) throw new InvalidOperationException("Elegí otra ventana y pulsá su atajo.");
            var expected=Native.Apply(target,p); await Task.Delay(150); var actual=Native.Bounds(target);
            bool limited=Math.Abs(actual.Width-expected.Width)>3||Math.Abs(actual.Height-expected.Height)>3||Math.Abs(actual.X-expected.X)>3||Math.Abs(actual.Y-expected.Y)>3;
            Notify(limited?$"«{p.Name}»: la aplicación ajustó las medidas o rechazó parte del cambio. Revisá sus límites de tamaño.":$"Aplicado: {p.Name}.",limited);
        }
        catch(Exception ex) { Notify(ex.Message,true); ShowEditor(); }
    }
    private async Task<ContentDialogResult> Dialog(string title,string message,string primary)
    {
        var dialog=new ContentDialog { XamlRoot=Root.XamlRoot, Title=title, Content=new ScrollViewer { MaxHeight=400, Content=new TextBlock { Text=message, TextWrapping=TextWrapping.Wrap } }, PrimaryButtonText=primary, CloseButtonText="Cancelar", DefaultButton=ContentDialogButton.Close };
        return await dialog.ShowAsync();
    }
    private async void Import_Click(object sender,RoutedEventArgs e)
    {
        if(busy) return; busy=true;
        try
        {
            var picker=new FileOpenPicker(); WinRT.Interop.InitializeWithWindow.Initialize(picker,hwnd); picker.FileTypeFilter.Add(".xml"); picker.FileTypeFilter.Add(".json");
            var file=await picker.PickSingleFileAsync(); if(file==null) return;
            List<Preset> incoming; var warnings=new List<string>();
            if(Path.GetExtension(file.Path).Equals(".xml",StringComparison.OrdinalIgnoreCase)) (incoming,warnings)=PresetFiles.ReadSizer(file.Path);
            else incoming=PresetFiles.Read(file.Path).Presets;
            if(incoming.Count==0) { Notify("No hay presets compatibles en el archivo.",true); return; }
            if(presets.Count+incoming.Count>500) throw new InvalidDataException("La importación superaría los 500 presets.");
            int conflicts=0;
            var used=new HashSet<(uint,uint)>(presets.Where(p=>p.Enabled&&p.Key!=0).Select(p=>(p.Modifiers,p.Key)));
            foreach(var p in incoming) { p.Id=Guid.NewGuid(); if(p.Enabled&&p.Key!=0&&!used.Add((p.Modifiers,p.Key))) { p.Enabled=false; conflicts++; } }
            string preview=$"Se agregarán {incoming.Count} presets. Los existentes se conservan.\n\n"+string.Join("\n",incoming.Select(p=>$"{p.Group}{(p.Group.Length>0?" / ":"")}{p.Name} · {p.Width} × {p.Height} · {Placements[p.Placement]} · {p.Shortcut}{(!p.Enabled?" (inactivo)":"")}"));
            if(conflicts>0) preview+=$"\n\n{conflicts} presets se importarán inactivos porque sus atajos ya están asignados. Podés editar sus atajos y activarlos después.";
            if(warnings.Count>0) preview+="\n\nAvisos:\n"+string.Join("\n",warnings);
            if(await Dialog("Revisar importación",preview,"Agregar presets")!=ContentDialogResult.Primary) return;
            foreach(var p in incoming) presets.Add(p); Persist(); PresetList.SelectedItem=incoming[0];
            var errors=RefreshHotkeys(); Notify(errors.Count>0?"Importado.\n"+string.Join("\n",errors):$"Se importaron {incoming.Count} presets.",errors.Count>0);
        }
        catch(Exception ex) { Notify("No se pudo importar: "+ex.Message,true); }
        finally { busy=false; }
    }
    private async void Export_Click(object sender,RoutedEventArgs e)
    {
        if(busy) return; busy=true;
        try
        {
            var picker=new FileSavePicker { SuggestedFileName="Window-Positioner-presets" }; picker.FileTypeChoices.Add("Presets de Window Positioner",new List<string>{".json"});
            WinRT.Interop.InitializeWithWindow.Initialize(picker,hwnd); var file=await picker.PickSaveFileAsync(); if(file==null) return;
            PresetFiles.Save(file.Path,new Settings { Presets=presets.ToList() }); Notify("Respaldo exportado.");
        }
        catch(Exception ex) { Notify("No se pudo exportar: "+ex.Message,true); }
        finally { busy=false; }
    }
    private void Startup_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if(Startup.IsChecked==true) key.SetValue("WindowPositioner",$"\"{Environment.ProcessPath}\""); else key.DeleteValue("WindowPositioner",false);
            Notify(Startup.IsChecked==true?"Se abrirá al iniciar sesión. Conservá la carpeta de la aplicación en su ubicación actual.":"Inicio con Windows desactivado.");
        }
        catch(Exception ex) { Startup.IsChecked=Startup.IsChecked!=true; Notify("No se pudo cambiar el inicio automático: "+ex.Message,true); }
    }
}
