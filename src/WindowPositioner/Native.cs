using System.Runtime.InteropServices;
using WindowPositioner.Core;

namespace WindowPositioner;

internal static class Native
{
    internal delegate nint SubclassProc(nint hwnd, uint msg, nuint wParam, nint lParam, nuint id, nuint data);
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left, Top, Right, Bottom; public readonly Rect ToRect() => new(Left, Top, Right-Left, Bottom-Top); }
    [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MONITORINFO { public int Size; public RECT Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] internal struct NOTIFYICONDATA
    {
        public uint Size; public nint Hwnd; public uint Id, Flags, Callback; public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)] public string Info;
        public uint Timeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)] public string InfoTitle;
        public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsZoomed(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint hwnd, int cmd);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool RegisterHotKey(nint hwnd,int id,uint mods,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("user32.dll")] internal static extern short GetKeyState(int key);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint MonitorFromWindow(nint hwnd,uint flags);
    [DllImport("user32.dll")] internal static extern nint MonitorFromPoint(POINT point,uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern bool GetMonitorInfo(nint monitor,ref MONITORINFO info);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint hwnd,uint attr,out RECT rect,int size);
    [DllImport("comctl32.dll")] internal static extern bool SetWindowSubclass(nint hwnd,SubclassProc proc,nuint id,nuint data);
    [DllImport("comctl32.dll")] internal static extern bool RemoveWindowSubclass(nint hwnd,SubclassProc proc,nuint id);
    [DllImport("comctl32.dll")] internal static extern nint DefSubclassProc(nint hwnd,uint msg,nuint wp,nint lp);
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] internal static extern bool Shell_NotifyIcon(uint message,ref NOTIFYICONDATA data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] internal static extern nint LoadIcon(nint instance,nint name);
    [DllImport("user32.dll")] internal static extern nint CreatePopupMenu();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool AppendMenu(nint menu,uint flags,nuint id,string text);
    [DllImport("user32.dll")] internal static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] internal static extern uint TrackPopupMenu(nint menu,uint flags,int x,int y,int reserved,nint hwnd,nint rect);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int MessageBox(nint hwnd,string text,string caption,uint type);
    public static uint Modifiers()
    {
        bool Down(int k) => (GetKeyState(k) & 0x8000) != 0;
        return (Down(0x11)?2u:0) | (Down(0x10)?4u:0) | (Down(0x12)?1u:0) | (Down(0x5B)||Down(0x5C)?8u:0);
    }
    public static Rect Bounds(nint hwnd)
    {
        if (!GetWindowRect(hwnd,out var r)) throw new InvalidOperationException("No se pudo leer la ventana.");
        if (DwmGetWindowAttribute(hwnd,9,out var visible,Marshal.SizeOf<RECT>()) == 0 && visible.Right > visible.Left) return visible.ToRect();
        return r.ToRect();
    }
    public static Rect WorkArea(nint hwnd,bool primary)
    {
        nint monitor = primary ? MonitorFromPoint(new POINT(),1) : MonitorFromWindow(hwnd,2);
        var info = new MONITORINFO { Size=Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor,ref info)) throw new InvalidOperationException("No se pudo leer el monitor.");
        return info.Work.ToRect();
    }
    public static Rect Apply(nint hwnd, Preset p)
    {
        if (!IsWindow(hwnd)) throw new InvalidOperationException("La ventana ya no existe.");
        var work = WorkArea(hwnd,p.PrimaryMonitor);
        if (IsZoomed(hwnd)) ShowWindow(hwnd,9);
        var visible = Bounds(hwnd);
        var target = Geometry.Resolve(p,visible,work);
        GetWindowRect(hwnd,out var raw);
        // Compensar el marco invisible para guardar medidas visuales en píxeles físicos.
        int dx = visible.X - raw.Left, dy = visible.Y - raw.Top;
        int dw = raw.Right - raw.Left - visible.Width, dh = raw.Bottom - raw.Top - visible.Height;
        if (!SetWindowPos(hwnd,0,target.X-dx,target.Y-dy,target.Width+dw,target.Height+dh,0x14))
            throw new InvalidOperationException("Windows rechazó el cambio. La aplicación puede estar elevada o protegida. No se solicitaron permisos de administrador.");
        return target;
    }
}

internal sealed class DesktopHost : IDisposable
{
    private readonly nint hwnd;
    private readonly Native.SubclassProc callback;
    private readonly Dictionary<int,Preset> keys = [];
    private Native.NOTIFYICONDATA icon;
    private readonly uint taskbarMessage = Native.RegisterWindowMessage("TaskbarCreated");
    public Action<Preset>? Pressed;
    public Action? Open, Toggle, Quit;
    public bool Paused { get; private set; }
    public bool Quitting { get; set; }
    public bool Recording { get; set; }
    public bool TrayAvailable { get; private set; }
    public DesktopHost(nint window)
    {
        hwnd=window; callback=WindowProc;
        if (!Native.SetWindowSubclass(hwnd,callback,1,0)) throw new InvalidOperationException("No se pudo inicializar la comunicación con Windows.");
        icon = new() { Size=(uint)Marshal.SizeOf<Native.NOTIFYICONDATA>(), Hwnd=hwnd, Id=1, Flags=7, Callback=0x8001,
            Icon=Native.LoadIcon(0,(nint)32512), Tip="Window Positioner · Atajos activos", Info="", InfoTitle="" };
        TrayAvailable=Native.Shell_NotifyIcon(0,ref icon);
    }
    public List<string> Register(IEnumerable<Preset> presets,bool paused=false)
    {
        foreach (var id in keys.Keys) Native.UnregisterHotKey(hwnd,id);
        keys.Clear(); Paused=paused; var errors = new List<string>(); int next=100;
        if (!paused) foreach (var p in presets.Where(p=>p.Enabled && p.Key!=0))
        {
            int id=next++;
            if (Native.RegisterHotKey(hwnd,id,p.Modifiers|0x4000,p.Key)) keys[id]=p.Copy();
            else errors.Add($"{p.Name}: {p.Shortcut} no está disponible.");
        }
        icon.Tip=paused ? "Window Positioner · Atajos en pausa" : "Window Positioner · Atajos activos";
        Native.Shell_NotifyIcon(1,ref icon);
        return errors;
    }
    private nint WindowProc(nint h,uint msg,nuint wp,nint lp,nuint id,nuint data)
    {
        if (msg==0x312 && !Recording && keys.TryGetValue((int)wp,out var p)) { Pressed?.Invoke(p); return 0; }
        if (msg==taskbarMessage) TrayAvailable=Native.Shell_NotifyIcon(0,ref icon);
        if (msg==0x10 && !Quitting && TrayAvailable) { Native.ShowWindow(hwnd,0); return 0; }
        if (msg==0x8001)
        {
            if ((int)lp==0x203) Open?.Invoke();
            if ((int)lp==0x205)
            {
                var menu=Native.CreatePopupMenu();
                Native.AppendMenu(menu,0,1,"Abrir configuración");
                Native.AppendMenu(menu,0,2,Paused?"Reactivar atajos":"Pausar atajos");
                Native.AppendMenu(menu,0,3,"Salir"); Native.GetCursorPos(out var pos); Native.SetForegroundWindow(hwnd);
                uint selected=Native.TrackPopupMenu(menu,0x102,pos.X,pos.Y,0,hwnd,0); Native.DestroyMenu(menu);
                if (selected==1) Open?.Invoke(); if(selected==2) Toggle?.Invoke(); if(selected==3) Quit?.Invoke();
            }
            return 0;
        }
        return Native.DefSubclassProc(h,msg,wp,lp);
    }
    public void Dispose()
    {
        foreach(var id in keys.Keys) Native.UnregisterHotKey(hwnd,id);
        Native.Shell_NotifyIcon(2,ref icon); Native.RemoveWindowSubclass(hwnd,callback,1);
    }
}
