#ifndef AppVersion
  #error Supply AppVersion using /DAppVersion
#endif
#ifndef PublishDir
  #error Supply PublishDir using /DPublishDir
#endif

#define AppName "Positioni"
#define Publisher "Neon Glow"
#define Website "https://neonglow.studio/"
#define Repository "https://github.com/neonglowstudio/positioni"

[Setup]
AppId=NeonGlow.WindowPositioner
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#Website}
AppSupportURL={#Repository}/issues
AppUpdatesURL={#Repository}/releases
DefaultDirName={localappdata}\Programs\Positioni
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.22000
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex=Local\WindowPositioner.Desktop
CloseApplications=no
UninstallDisplayIcon={app}\Positioni.exe
SetupIconFile=..\src\Positioni\Assets\Positioni.ico
OutputBaseFilename=positioni-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[LangOptions]
pt.LanguageName=Português

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,startup-error.txt"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Positioni"; Filename: "{app}\Positioni.exe"; WorkingDir: "{app}"

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  RunValue = 'WindowPositioner';

function InstalledCommand: String;
begin
  Result := '"' + ExpandConstant('{app}\Positioni.exe') + '" --startup';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ExistingCommand: String;
begin
  if CurStep <> ssPostInstall then
    Exit;
  if RegQueryStringValue(HKCU, RunKey, RunValue, ExistingCommand) then
    if (ExistingCommand <> '') and (CompareText(ExistingCommand, InstalledCommand) <> 0) then
      RegWriteStringValue(HKCU, RunKey, RunValue, InstalledCommand);

  if FileExists(ExpandConstant('{app}\WindowPositioner.exe')) then
  begin
    DeleteFile(ExpandConstant('{userprograms}\Window Positioner.lnk'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.exe'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.dll'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.Core.dll'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.deps.json'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.runtimeconfig.json'));
    DeleteFile(ExpandConstant('{app}\WindowPositioner.pri'));
    DeleteFile(ExpandConstant('{app}\Assets\WindowPositioner.ico'));
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ExistingCommand: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, RunKey, RunValue, ExistingCommand) then
      if CompareText(ExistingCommand, InstalledCommand) = 0 then
        RegDeleteValue(HKCU, RunKey, RunValue);
end;
