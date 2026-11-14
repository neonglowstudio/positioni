using static Positioni.Core.UiText;
using System.Runtime.InteropServices;
using Positioni.Core;

namespace Positioni;

internal static class Native
{
    internal delegate nint KeyboardHookProc(int code, nuint message, nint data);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWindowsHookExW(int kind, KeyboardHookProc callback, nint module, uint threadId);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nuint message, nint data);
    [DllImport("kernel32.dll")] internal static extern nint GetModuleHandleW(nint name);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
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

    [DllImport("user32.dll")] internal static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] internal static extern int GetMessageTime();
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
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
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern nint LoadImage(nint instance, string name, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(nint icon);

    internal static string AppIconPath { get; } = Path.Combine(AppContext.BaseDirectory, "Assets", "Positioni.ico");

    internal static nint LoadTrayIcon(nint hwnd)
    {
        const uint imageIcon = 1;
        const uint loadFromFile = 0x10;
        int size = Math.Max(16, (int)Math.Round(16 * GetDpiForWindow(hwnd) / 96.0));
        return LoadImage(0, AppIconPath, imageIcon, size, size, loadFromFile);
    }
    [DllImport("user32.dll")] internal static extern nint CreatePopupMenu();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool AppendMenu(nint menu,uint flags,nuint id,string text);
    [DllImport("user32.dll")] internal static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] internal static extern uint TrackPopupMenu(nint menu,uint flags,int x,int y,int reserved,nint hwnd,nint rect);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int MessageBox(nint hwnd,string text,string caption,uint type);
    public static uint Modifiers(bool physical = false)
    {
        bool Down(int k) => ((physical ? GetAsyncKeyState(k) : GetKeyState(k)) & 0x8000) != 0;
        return (Down(0x11)?2u:0) | (Down(0x10)?4u:0) | (Down(0x12)?1u:0) | (Down(0x5B)||Down(0x5C)?8u:0);
    }
    public static Rect Bounds(nint hwnd)
    {
        if (!GetWindowRect(hwnd,out var r)) throw new InvalidOperationException(Get("ReadWindow"));
        if (DwmGetWindowAttribute(hwnd,9,out var visible,Marshal.SizeOf<RECT>()) == 0 && visible.Right > visible.Left) return visible.ToRect();
        return r.ToRect();
    }
    public static Rect WorkArea(nint hwnd, bool primary) => MonitorAreas(hwnd, primary).Work;

    public static (Rect Screen, Rect Work) MonitorAreas(nint hwnd, bool primary)
    {
        nint monitor = primary ? MonitorFromPoint(new POINT(),1) : MonitorFromWindow(hwnd,2);
        var info = new MONITORINFO { Size=Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor,ref info)) throw new InvalidOperationException(Get("ReadMonitor"));
        return (info.Monitor.ToRect(), info.Work.ToRect());
    }
    public static Rect Apply(nint hwnd, Preset p)
    {
        if (!IsWindow(hwnd)) throw new InvalidOperationException(Get("WindowGone"));
        var work = WorkArea(hwnd,p.PrimaryMonitor);
        if (IsZoomed(hwnd)) ShowWindow(hwnd,9);
        var visible = Bounds(hwnd);
        var target = Geometry.Resolve(p,visible,work);
        GetWindowRect(hwnd,out var raw);
        // Compensar el marco invisible para guardar medidas visuales en píxeles físicos.
        int dx = visible.X - raw.Left, dy = visible.Y - raw.Top;
        int dw = raw.Right - raw.Left - visible.Width, dh = raw.Bottom - raw.Top - visible.Height;
        if (!SetWindowPos(hwnd,0,target.X-dx,target.Y-dy,target.Width+dw,target.Height+dh,0x14))
            throw new InvalidOperationException(Get("Rejected"));
        return target;
    }
}

internal sealed class DesktopHost : IDisposable
{
    private readonly nint hwnd;
    private readonly Native.SubclassProc callback;
    private readonly Dictionary<int,Preset> keys = [];
    private Native.NOTIFYICONDATA icon;
    private readonly nint trayIcon;
    private readonly uint taskbarMessage = Native.RegisterWindowMessage("TaskbarCreated");
    public Action<Preset>? Pressed;
    public Action? Open, Toggle, Quit, ResetSize, RequestExit, RequestQuit;
    public Action? MonitorLayoutChanged;
    public Action? Resized;
    public bool CloseToTray { get; set; } = true;
    private const uint BorderClick = 0x00A1;
    private const uint BorderDoubleClick = 0x00A3;
    private const uint WindowSizing = 0x0214;
    private const uint EnterSizeMove = 0x0231;
    private const uint ExitSizeMove = 0x0232;
    private Rect? sizeBeforeDrag;
    private bool sizingWindow;
    private const nuint BottomRightCorner = 17;
    private bool trackingCornerClick, cornerDragged;
    private (int X, int Y, uint Time)? previousCornerClick;
    public bool Paused { get; private set; }
    public bool Quitting { get; set; }
    public bool Recording { get; private set; }
    private nint recordingHook;
    private Native.KeyboardHookProc? recordingCallback;
    private Action<uint, uint>? recordKey;

    public void BeginRecording(Action<uint, uint> callback)
    {
        Recording = true;
        recordKey = callback;
        recordingCallback ??= OnRecordingKey;
        const int lowLevelKeyboard = 13;
        recordingHook = Native.SetWindowsHookExW(lowLevelKeyboard, recordingCallback, Native.GetModuleHandleW(0), 0);
    }

    public void EndRecording()
    {
        Recording = false;
        recordKey = null;
        if (recordingHook == 0) return;
        Native.UnhookWindowsHookEx(recordingHook);
        recordingHook = 0;
    }

    private nint OnRecordingKey(int code, nuint message, nint data)
    {
        if (code == 0 && Recording && message is 0x100 or 0x104 && Native.GetForegroundWindow() == hwnd)
        {
            uint key = unchecked((uint)Marshal.ReadInt32(data));
            uint modifiers = Native.Modifiers(physical: true);
            if (key == 0x1B || (modifiers != 0 && Hotkey.IsRecordableKey(key)))
            {
                recordKey?.Invoke(key, modifiers);
                return 1;
            }
        }
        return Native.CallNextHookEx(0, code, message, data);
    }
    public bool TrayAvailable { get; private set; }
    public DesktopHost(nint window)
    {
        hwnd=window; callback=WindowProc;
        if (!Native.SetWindowSubclass(hwnd,callback,1,0)) throw new InvalidOperationException(Get("NativeInit"));
        trayIcon = Native.LoadTrayIcon(hwnd);
        icon = new() { Size=(uint)Marshal.SizeOf<Native.NOTIFYICONDATA>(), Hwnd=hwnd, Id=1, Flags=7, Callback=0x8001,
            Icon=trayIcon != 0 ? trayIcon : Native.LoadIcon(0,(nint)32512), Tip="Positioni · " + Get("ShortcutsActive"), Info="", InfoTitle="" };
        TrayAvailable=Native.Shell_NotifyIcon(0,ref icon);
    }
    public int CheckShortcut(uint modifiers, uint key)
    {
        if (keys.Values.Any(p => p.Modifiers == modifiers && p.Key == key)) return 0;

        const int probeId = 99;
        if (!Native.RegisterHotKey(hwnd, probeId, modifiers | 0x4000, key))
            return Marshal.GetLastWin32Error();

        Native.UnregisterHotKey(hwnd, probeId);
        return 0;
    }

    public List<string> Register(IEnumerable<Preset> presets,bool paused=false)
    {
        foreach (var id in keys.Keys) Native.UnregisterHotKey(hwnd,id);
        keys.Clear(); Paused=paused; var errors = new List<string>(); int next=100;
        if (!paused) foreach (var p in presets.Where(p => p.CanRegisterShortcut))
        {
            int id=next++;
            if (Native.RegisterHotKey(hwnd,id,p.Modifiers|0x4000,p.Key)) keys[id]=p.Copy();
            else errors.Add(Get("Unavailable", p.DisplayName, p.Shortcut));
        }
        UpdateTrayText();
        return errors;
    }

    public void UpdateTrayText()
    {
        icon.Tip = "Positioni · " + Get(Paused ? "ShortcutsPaused" : "ShortcutsActive");
        Native.Shell_NotifyIcon(1, ref icon);
    }
    private nint HandleCornerClick(nint window, nint coordinates)
    {
        int x = (short)((long)coordinates & 0xFFFF);
        int y = (short)(((long)coordinates >> 16) & 0xFFFF);
        uint time = unchecked((uint)Native.GetMessageTime());
        const int doubleClickWidth = 36, doubleClickHeight = 37;
        if (previousCornerClick is { } last &&
            unchecked(time - last.Time) <= Native.GetDoubleClickTime() &&
            Math.Abs(x - last.X) <= Native.GetSystemMetrics(doubleClickWidth) / 2 &&
            Math.Abs(y - last.Y) <= Native.GetSystemMetrics(doubleClickHeight) / 2)
        {
            previousCornerClick = null;
            ResetSize?.Invoke();
            return 0;
        }

        // Let Windows run its normal sizing loop. Only a click without dragging can start a reset gesture.
        previousCornerClick = null;
        trackingCornerClick = true;
        cornerDragged = false;
        try
        {
            var result = Native.DefSubclassProc(window, BorderClick, BottomRightCorner, coordinates);
            if (!cornerDragged) previousCornerClick = (x, y, time);
            return result;
        }
        finally { trackingCornerClick = false; }
    }

    private nint WindowProc(nint h,uint msg,nuint wp,nint lp,nuint id,nuint data)
    {
        const uint settingChanged = 0x001A, displayChanged = 0x007E;
        const nuint setWorkArea = 0x002F;
        if (msg == displayChanged || (msg == settingChanged && wp == setWorkArea))
            MonitorLayoutChanged?.Invoke();

        if (msg == EnterSizeMove)
        {
            sizingWindow = false;
            sizeBeforeDrag = Native.GetWindowRect(h, out var bounds) ? bounds.ToRect() : null;
        }
        if (msg == WindowSizing)
        {
            sizingWindow = true;
            if (trackingCornerClick) cornerDragged = true;
        }
        if (msg == ExitSizeMove)
        {
            if (sizingWindow && !Native.IsZoomed(h) && Native.GetWindowRect(h, out var bounds))
            {
                var size = bounds.ToRect();
                if (sizeBeforeDrag is not { } start || size.Width != start.Width || size.Height != start.Height)
                    Resized?.Invoke();
            }
            sizingWindow = false;
            sizeBeforeDrag = null;
        }
        if (msg == BorderDoubleClick && wp == BottomRightCorner && !Native.IsZoomed(h))
        {
            previousCornerClick = null;
            ResetSize?.Invoke();
            return 0;
        }
        if (msg == BorderClick && wp == BottomRightCorner && !Native.IsZoomed(h))
            return HandleCornerClick(h, lp);
        if (msg == BorderClick || msg == 0x0201) previousCornerClick = null;
        if (msg==0x312 && !Recording && keys.TryGetValue((int)wp,out var p)) { Pressed?.Invoke(p); return 0; }
        if (msg==taskbarMessage) TrayAvailable=Native.Shell_NotifyIcon(0,ref icon);
        if (msg == 0x10 && !Quitting && RequestExit is { } requestClose)
        {
            requestClose();
            return 0;
        }
        if (msg==0x8001)
        {
            if ((int)lp==0x203) Open?.Invoke();
            if ((int)lp==0x205)
            {
                var menu=Native.CreatePopupMenu();
                Native.AppendMenu(menu,0,1,Get("OpenEditor"));
                Native.AppendMenu(menu,0,2,Paused?Get("Resume"):Get("Pause"));
                Native.AppendMenu(menu,0,3,Get("Quit")); Native.GetCursorPos(out var pos); Native.SetForegroundWindow(hwnd);
                uint selected=Native.TrackPopupMenu(menu,0x102,pos.X,pos.Y,0,hwnd,0); Native.DestroyMenu(menu);
                if (selected==1) Open?.Invoke(); if(selected==2) Toggle?.Invoke(); if(selected==3) RequestQuit?.Invoke();
            }
            return 0;
        }
        return Native.DefSubclassProc(h,msg,wp,lp);
    }
    public void Dispose()
    {
        EndRecording();
        foreach(var id in keys.Keys) Native.UnregisterHotKey(hwnd,id);
        Native.Shell_NotifyIcon(2,ref icon); Native.RemoveWindowSubclass(hwnd,callback,1);
        if (trayIcon != 0) Native.DestroyIcon(trayIcon);
    }
}
