using System.Runtime.InteropServices;
using System.Text.Json;

internal static class DesktopTarget
{
    private delegate nint WndProc(nint h,uint m,nuint w,nint l);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct Class
    {
        public uint Size,Style; public WndProc Proc; public int ClassExtra,WindowExtra; public nint Instance,Icon,Cursor,Brush;
        public string? Menu; public string Name; public nint SmallIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct Message { public nint Window; public uint Id; public nuint W; public nint L; public uint Time; public int X,Y; public uint Private; }
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern ushort RegisterClassEx(ref Class c);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern nint CreateWindowEx(uint ex,string cls,string title,uint style,int x,int y,int w,int h,nint parent,nint menu,nint instance,nint param);
    [DllImport("user32.dll")] private static extern nint DefWindowProc(nint h,uint m,nuint w,nint l);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint h,out Rect r);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint h,uint a,out Rect r,int size);
    [DllImport("user32.dll")] private static extern int GetMessage(out Message m,nint h,uint min,uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message m);
    [DllImport("user32.dll")] private static extern nint DispatchMessage(ref Message m);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint hwnd,int cmd);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(nint value);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);
    private static WndProc? callback;
    public static void Run(string log)
    {
        SetProcessDpiAwarenessContext((nint)(-4));
        callback=(h,m,w,l)=>
        {
            if(m==0x47)
            {
                GetWindowRect(h,out var r); DwmGetWindowAttribute(h,9,out var v,16);
                File.WriteAllText(log,JsonSerializer.Serialize(new {Outer=new {X=r.Left,Y=r.Top,Width=r.Right-r.Left,Height=r.Bottom-r.Top}, Visible=new {X=v.Left,Y=v.Top,Width=v.Right-v.Left,Height=v.Bottom-v.Top}}));
            }
            if(m==2) PostQuitMessage(0);
            if(m==0x81) return 1;
            return DefWindowProc(h,m,w,l);
        };
        var c=new Class { Size=(uint)Marshal.SizeOf<Class>(),Proc=callback,Instance=GetModuleHandle(null),Name="WindowPositionerTestTarget",Brush=(nint)6 };
        var atom=RegisterClassEx(ref c); Console.WriteLine($"Register: {atom}, error {Marshal.GetLastWin32Error()}");
        var handle=CreateWindowEx(0,c.Name,"Ventana de prueba · Window Positioner",0x10CF0000,200,200,900,700,0,0,c.Instance,0);
        Console.WriteLine($"Window: {handle}, error {Marshal.GetLastWin32Error()}");
        if(handle==0) return;
        ShowWindow(handle,5); ShowWindow(handle,9);
        while(GetMessage(out var msg,0,0,0)>0) { TranslateMessage(ref msg); DispatchMessage(ref msg); }
    }
}
