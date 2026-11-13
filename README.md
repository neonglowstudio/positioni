<p align="center">
  <img src="src/Positioni/Assets/Positioni.png" alt="Positioni logo" width="88">
</p>

<h1 align="center">Positioni</h1>

<p align="center">Move and resize windows with custom keyboard shortcuts.</p>

> **Documentation preview for Positioni 1.0.** The current public download is 0.1.0.

<p align="center">
  <a href="https://github.com/neonglowstudio/positioni/releases">All releases</a>
</p>

> Screenshot coming with the 1.0 release.

Positioni is a free Windows 11 app by [Neon Glow](https://neonglow.studio/). Save window sizes and positions as profiles, then apply them to the active window with global shortcuts.

## Download

| Edition | How to start |
| --- | --- |
| Installer | Run the setup executable. |
| Portable | Extract the whole ZIP and open `Positioni.exe`. Keep the `Files` folder beside it. |

Both editions are built for Windows 11 x64 and include the required runtimes. Find version details and older downloads under [Releases](https://github.com/neonglowstudio/positioni/releases).

## Features

- Save window sizes in pixels or as percentages of the monitor's work area.
- Draw, move, and resize a window in the visual editor, or capture its size and position from an open window.
- Use nine relative positions, exact coordinates, or resize a window in place.
- Assign global shortcuts to profiles, pause them together, or disable them individually.
- Choose from ten starter profiles or start with an empty list.
- Export profiles as JSON and select which profiles to import from Positioni, Sizer, WindowSizer, FancyZones, DisplayFusion, or supported AutoHotkey v2 bindings.
- Start in the system tray when signing in to Windows. Choose whether closing the window keeps the app running.
- Follow the system theme or choose light or dark.
- Check GitHub automatically or on demand for updates. View release notes and download the matching edition. The installed edition can apply updates in the app when Windows validates both signatures and their signing certificates match.

## Languages

English, Español, Português, Italiano, Français, and Deutsch. Positioni starts in the Windows display language when supported and falls back to English.

## Quick start

1. Select **Add profile** to create one, or select **Choose starter profiles** to start with a ready-to-use profile.
2. Set the window size and position, assign a shortcut, and save.
3. Focus the window you want to move or resize and press the shortcut.

## Privacy

Profiles and settings are stored on your device. Positioni has no accounts, ads, or telemetry. Update checks contact GitHub.

## Technology stack

| Component | Version | Source |
| --- | --- | --- |
| C# | 15.0 for the desktop app | [C# language repository](https://github.com/dotnet/csharplang) |
| .NET | Runtime 11.0.0, SDK 11.0.100 | [.NET runtime](https://github.com/dotnet/runtime), [.NET SDK](https://github.com/dotnet/sdk) |
| WinUI 3 and Windows App SDK | 2.6.5 (`Microsoft.WindowsAppSDK.WinUI`) | [WinUI](https://github.com/microsoft/microsoft-ui-xaml), [Windows App SDK](https://github.com/microsoft/WindowsAppSDK), [NuGet package](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.WinUI/2.6.5) |
| Windows SDK Build Tools | 10.0.28000.2957 | [Microsoft's NuGet package](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools/10.0.28000.2957) |
| Inno Setup | 6.7.4 | [Inno Setup repository](https://github.com/jrsoftware/issrc) |

## Build from source

The build and packaging scripts use PowerShell. On Windows x64 with the .NET 11 SDK installed, run:

```powershell
.\scripts\build.ps1
```

To build the installer as well, run `.\scripts\package.ps1` with Inno Setup 6 available. Pass `-Iscc` if the compiler is outside your PATH.

## License and credits

The planned license for Positioni 1.0 is the [GNU General Public License, version 3 only](https://www.gnu.org/licenses/gpl-3.0.html). A separate branding policy will cover the Positioni and Neon Glow names and logos.

- Wordmark typeface by Indieground Design. [Speltale](https://indieground.net/product/speltale-font/)
- Interface icons by Microsoft. [Segoe Fluent Icons](https://learn.microsoft.com/windows/apps/design/iconography/segoe-fluent-icons-font)
- GitHub icon from Primer. [Octicons](https://github.com/primer/octicons)
- Developed with assistance from OpenAI Codex. [Codex](https://github.com/openai/codex)
