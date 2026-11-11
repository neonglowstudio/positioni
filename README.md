# Window Positioner

Save window sizes and positions and apply them to the active window with global keyboard shortcuts on Windows 11 (x64).

## Getting started

1. Download the Windows x64 ZIP from [Releases](https://github.com/neonglowstudio/window-positioner/releases) and extract it.
2. Open `WindowPositioner.exe` from the extracted folder. The executable is unsigned and the app interface is in Spanish.
3. Create a preset, assign a keyboard shortcut, and save it. Focus another window and press the shortcut to apply the preset.

You can capture a window's position and size, import presets from Sizer XML, and export your presets as JSON.

## Build

Install the .NET 10 SDK on Windows and run:

```powershell
.\scripts\build.ps1
```

The build output is in `outputs/WindowPositioner`. This first public preview has yet to be tested across monitors with different display scaling.
