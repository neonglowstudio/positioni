<p align="center">
  <picture>
    <img src="docs/images/positioni-logo.png" alt="Positioni" width="420">
  </picture>
</p>

<p align="center">Positioni lets you move and resize windows with custom keyboard shortcuts.</p>

<br>

## Description

Positioni is a Windows app that lets you save window sizes and positions as profiles and apply them to the active window with custom keyboard shortcuts.

<br>

## Screenshot

<p align="center">
  <picture>
    <img src="docs/images/positioni-screenshot.png" alt="Positioni showing the starter profiles and the visual size and position editor" width="100%">
  </picture>
</p>

<br>

## Download

| Edition | How to start | Download link |
| --- | --- | --- |
| Installer | Run the setup executable. | [Download installer](https://github.com/neonglowstudio/positioni/releases/download/1.0.0/positioni-1.0.0-win-x64-setup.exe) |
| Portable | Extract the whole ZIP and open `Positioni.exe`. Keep the `Files` folder beside it. | [Download portable](https://github.com/neonglowstudio/positioni/releases/download/1.0.0/positioni-1.0.0-win-x64-portable.zip) |

Both editions are built for Windows 11 x64 and include the required runtimes. Third-party runtime components are provided under their own terms, included with each download. Find version details and older downloads under [Releases](https://github.com/neonglowstudio/positioni/releases).

<br>

## Features

- Save window sizes in pixels or as percentages of the monitor's work area.
- Draw, move, and resize a window in the visual editor, or capture its size and position from an open window.
- Use nine relative positions, exact coordinates, or resize a window in place.
- Assign global shortcuts to profiles, pause them together, or disable them individually.
- Choose from ten starter profiles or start with an empty list.
- Export profiles as JSON and select which profiles to import from Positioni, [Sizer](https://www.brianapps.net/sizer4/), [WindowSizer](https://github.com/Mxlted/WindowSizer), [FancyZones](https://learn.microsoft.com/en-us/windows/powertoys/fancyzones), [DisplayFusion](https://www.displayfusion.com/HelpGuide/WindowPositionProfiles/), or supported [AutoHotkey v2](https://github.com/AutoHotkey/AutoHotkey) bindings.
- Start in the system tray when signing in to Windows. Choose whether closing the window keeps the app running.
- Follow the system theme or choose light or dark.
- Check GitHub automatically or on demand for updates. View release notes and download the matching edition. The installed edition can apply updates in the app when Windows validates both signatures and their signing certificates match.

<br>

## Languages

English, Español, Português, Italiano, Français, and Deutsch.

<br>

## Quick start

1. Select **Add profile** to create one, or select **Choose starter profiles** to start with a ready-to-use profile.
2. Set the window size and position, assign a shortcut, and save.
3. Focus the window you want to move or resize and press the shortcut.

<br>

## Privacy

Profiles and settings are stored on your device. Positioni has no accounts, ads, or telemetry. Positioni checks the [releases in this GitHub repository](https://github.com/neonglowstudio/positioni/releases) for updates.

<br>

## Technology stack

| Component | Version | Source |
| --- | --- | --- |
| C# | 15.0 for the desktop app | [C# language repository](https://github.com/dotnet/csharplang) |
| .NET | Runtime 11.0.0, SDK 11.0.100 | [.NET runtime](https://github.com/dotnet/runtime), [.NET SDK](https://github.com/dotnet/sdk) |
| WinUI 3 and Windows App SDK | 2.6.5 (`Microsoft.WindowsAppSDK.WinUI`) | [WinUI](https://github.com/microsoft/microsoft-ui-xaml), [Windows App SDK](https://github.com/microsoft/WindowsAppSDK), [NuGet package](https://www.nuget.org/packages/Microsoft.WindowsAppSDK.WinUI/2.6.5) |
| Windows SDK Build Tools | 10.0.28000.2957 | [Microsoft's NuGet package](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools/10.0.28000.2957) |
| Inno Setup | 6.7.4 | [Inno Setup repository](https://github.com/jrsoftware/issrc) |

<br>

## Build from source

The build and packaging scripts use PowerShell. On Windows x64 with the .NET 11 SDK installed, run:

```powershell
.\scripts\build.ps1
```

To build the installer as well, run `.\scripts\package.ps1` with Inno Setup 6 available. Pass `-Iscc` if the compiler is outside your PATH.

<br>

## License and credits

Positioni's source code is licensed under the [GNU General Public License, version 3 only](LICENSE). The [trademark and branding policy](TRADEMARKS.md) covers the Positioni name, logo, and app icon.

- Designed and developed by [Neon Glow](https://neonglow.studio/).
- Positioni wordmark set in [Speltale](https://indieground.net/product/speltale-font/) by [Indieground Design](https://indieground.net/).
- Interface icons: [Segoe Fluent Icons](https://learn.microsoft.com/windows/apps/design/iconography/segoe-fluent-icons-font) by [Microsoft](https://www.microsoft.com/).
- GitHub icon: [Octicons](https://github.com/primer/octicons) by [Primer](https://primer.style/).
- Development assistance: [Codex](https://github.com/openai/codex) by [OpenAI](https://openai.com/).
