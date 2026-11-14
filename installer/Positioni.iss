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
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\Positioni.exe
SetupIconFile=..\src\Positioni\Assets\Positioni.ico
OutputBaseFilename=positioni-{#AppVersion}-win-x64-installer
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dynamic
WizardSizePercent=110,100
WizardSmallImageFile=

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[LangOptions]
pt.LanguageName=Português

[Messages]
en.SetupWindowTitle=Install Positioni
en.SelectLanguageTitle=Positioni Setup
en.SelectLanguageLabel=Choose the installation language.
en.ExitSetupTitle=Exit installation
en.WizardReady=Ready to install
en.WizardPreparing=Preparing to install
en.SetupAppRunningError=Close %1 and select Continue to start the installation.
en.UninstallAppRunningError=Close %1 and select Continue to start the uninstallation.
en.YesRadio=Restart now
en.NoRadio=Restart later
en.FinishedHeadingLabel=Installation complete
en.FinishedLabelNoIcons=[name] was installed successfully.
en.FinishedLabel=[name] was installed successfully.
en.ReadyLabel1=Everything is ready to install [name] on your computer.
en.ReadyLabel2b=Click Install to continue.
es.SetupWindowTitle=Instalar Positioni
es.UninstallAppFullTitle=Desinstalar %1
es.SelectLanguageTitle=Instalación de Positioni
es.SelectLanguageLabel=Selecciona el idioma de la instalación.
es.ExitSetupTitle=Salir de la instalación
es.WizardReady=Listo para instalar
es.WizardPreparing=Preparando la instalación
es.SetupAppRunningError=Cierra %1 y selecciona Continuar para iniciar la instalación.
es.UninstallAppRunningError=Cierra %1 y selecciona Continuar para iniciar la desinstalación.
es.YesRadio=Reiniciar ahora
es.NoRadio=Reiniciar más tarde
es.FinishedHeadingLabel=Instalación completada
es.FinishedLabelNoIcons=[name] se instaló correctamente.
es.FinishedLabel=[name] se instaló correctamente.
es.ReadyLabel1=Todo está listo para instalar [name] en tu equipo.
es.ReadyLabel2b=Haz clic en Instalar para continuar.
pt.SetupWindowTitle=Instalar o Positioni
pt.UninstallAppFullTitle=Desinstalar o %1
pt.SelectLanguageTitle=Instalação do Positioni
pt.SelectLanguageLabel=Selecione o idioma da instalação.
pt.ExitSetupTitle=Sair da instalação
pt.WizardReady=Pronto para instalar
pt.WizardPreparing=Preparando a instalação
pt.SetupAppRunningError=Feche o %1 e selecione Continuar para iniciar a instalação.
pt.UninstallAppRunningError=Feche o %1 e selecione Continuar para iniciar a desinstalação.
pt.YesRadio=Reiniciar agora
pt.NoRadio=Reiniciar mais tarde
pt.FinishedHeadingLabel=Instalação concluída
pt.FinishedLabelNoIcons=[name] foi instalado com sucesso.
pt.FinishedLabel=[name] foi instalado com sucesso.
pt.ReadyLabel1=Está tudo pronto para instalar [name] no seu computador.
pt.ReadyLabel2b=Clique em Instalar para continuar.
it.SetupWindowTitle=Installa Positioni
it.SelectLanguageTitle=Installazione di Positioni
it.SelectLanguageLabel=Seleziona la lingua di installazione.
it.ExitSetupTitle=Esci dall’installazione
it.WizardReady=Pronto per l’installazione
it.WizardPreparing=Preparazione dell’installazione
it.SetupAppRunningError=Chiudi %1 e seleziona Continua per avviare l’installazione.
it.UninstallAppRunningError=Chiudi %1 e seleziona Continua per avviare la disinstallazione.
it.YesRadio=Riavvia ora
it.NoRadio=Riavvia più tardi
it.FinishedHeadingLabel=Installazione completata
it.FinishedLabelNoIcons=[name] è stato installato correttamente.
it.FinishedLabel=[name] è stato installato correttamente.
it.ReadyLabel1=È tutto pronto per installare [name] sul computer.
it.ReadyLabel2b=Fai clic su Installa per continuare.
fr.SetupWindowTitle=Installer Positioni
fr.UninstallAppFullTitle=Désinstaller %1
fr.SelectLanguageTitle=Installation de Positioni
fr.SelectLanguageLabel=Sélectionnez la langue d’installation.
fr.ExitSetupTitle=Quitter l’installation
fr.WizardReady=Prêt à installer
fr.WizardPreparing=Préparation de l’installation
fr.SetupAppRunningError=Fermez %1 et sélectionnez Continuer pour lancer l’installation.
fr.UninstallAppRunningError=Fermez %1 et sélectionnez Continuer pour lancer la désinstallation.
fr.YesRadio=Redémarrer maintenant
fr.NoRadio=Redémarrer plus tard
fr.FinishedHeadingLabel=Installation terminée
fr.FinishedLabelNoIcons=[name] a été installé avec succès.
fr.FinishedLabel=[name] a été installé avec succès.
fr.ReadyLabel1=Tout est prêt pour installer [name] sur votre ordinateur.
fr.ReadyLabel2b=Cliquez sur Installer pour continuer.
de.SetupWindowTitle=Positioni installieren
de.SelectLanguageTitle=Positioni-Installation
de.SelectLanguageLabel=Wähle die Installationssprache.
de.ExitSetupTitle=Installation beenden
de.WizardReady=Bereit zur Installation
de.WizardPreparing=Installation wird vorbereitet
de.SetupAppRunningError=Schließe %1 und wähle Fortfahren, um die Installation zu starten.
de.UninstallAppRunningError=Schließe %1 und wähle Fortfahren, um die Deinstallation zu starten.
de.YesRadio=Jetzt neu starten
de.NoRadio=Später neu starten
de.FinishedHeadingLabel=Installation abgeschlossen
de.FinishedLabelNoIcons=[name] wurde erfolgreich installiert.
de.FinishedLabel=[name] wurde erfolgreich installiert.
de.ReadyLabel1=Alles ist bereit, um [name] auf deinem Computer zu installieren.
de.ReadyLabel2b=Klicke auf Installieren, um fortzufahren.

en.WizardSelectTasks=Installation options
en.SelectTasksDesc=Choose the options you want to enable.
en.ReadyMemoTasks=Selected options:
es.WizardSelectTasks=Opciones de instalación
es.SelectTasksDesc=Elige las opciones que quieras activar.
es.ReadyMemoTasks=Opciones seleccionadas:
pt.WizardSelectTasks=Opções de instalação
pt.SelectTasksDesc=Escolha as opções que pretende ativar.
pt.ReadyMemoTasks=Opções selecionadas:
it.WizardSelectTasks=Opzioni di installazione
it.SelectTasksDesc=Scegli le opzioni da attivare.
it.ReadyMemoTasks=Opzioni selezionate:
fr.WizardSelectTasks=Options d’installation
fr.SelectTasksDesc=Choisissez les options à activer.
fr.ReadyMemoTasks=Options sélectionnées :
de.WizardSelectTasks=Installationsoptionen
de.SelectTasksDesc=Wähle die Optionen aus, die du aktivieren möchtest.
de.ReadyMemoTasks=Ausgewählte Optionen:

[CustomMessages]
en.ContinueButton=Continue
en.InstallInProgress=Installation is still in progress.
en.UninstallCloseButton=Close
en.UninstallButton=Uninstall
en.ExitButton=Exit
en.ConfirmExit=Do you want to exit the installation?
en.ExitExplanation=The installation is still incomplete. If you exit now, Positioni won’t be installed.
en.OpenAfterInstall=Open Positioni after installation
en.StartMenuShortcut=Create a Start menu shortcut
en.StartWithWindows=Start Positioni in the system tray at sign-in
es.CreateDesktopIcon=Crear un acceso directo en el escritorio
es.ContinueButton=Continuar
es.InstallInProgress=La instalación todavía está en curso.
es.UninstallCloseButton=Cerrar
es.UninstallButton=Desinstalar
es.ExitButton=Salir
es.ConfirmExit=¿Quieres salir de la instalación?
es.ExitExplanation=La instalación todavía no se completó. Si sales ahora, Positioni no se instalará.
es.OpenAfterInstall=Ejecutar Positioni al finalizar la instalación
es.StartMenuShortcut=Crear un acceso directo en el menú Inicio
es.StartWithWindows=Iniciar Positioni en la bandeja del sistema al iniciar sesión
pt.ContinueButton=Continuar
pt.InstallInProgress=A instalação ainda está em andamento.
pt.UninstallCloseButton=Fechar
pt.UninstallButton=Desinstalar
pt.ExitButton=Sair
pt.ConfirmExit=Quer sair da instalação?
pt.ExitExplanation=A instalação ainda não foi concluída. Se sair agora, o Positioni não será instalado.
pt.OpenAfterInstall=Abrir o Positioni após a instalação
pt.StartMenuShortcut=Criar um atalho no menu Iniciar
pt.StartWithWindows=Iniciar o Positioni na área de notificação ao entrar no Windows
it.ContinueButton=Continua
it.InstallInProgress=L’installazione è ancora in corso.
it.UninstallCloseButton=Chiudi
it.UninstallButton=Disinstalla
it.ExitButton=Esci
it.ConfirmExit=Vuoi uscire dall’installazione?
it.ExitExplanation=L’installazione non è ancora completata. Se esci ora, Positioni non verrà installato.
it.OpenAfterInstall=Apri Positioni al termine dell’installazione
it.StartMenuShortcut=Crea un collegamento nel menu Start
it.StartWithWindows=Avvia Positioni nell’area di notifica all’accesso a Windows
fr.ContinueButton=Continuer
fr.InstallInProgress=L’installation est toujours en cours.
fr.UninstallCloseButton=Fermer
fr.UninstallButton=Désinstaller
fr.ExitButton=Quitter
fr.ConfirmExit=Voulez-vous quitter l’installation ?
fr.ExitExplanation=L’installation n’est pas encore terminée. Si vous quittez maintenant, Positioni ne sera pas installé.
fr.OpenAfterInstall=Ouvrir Positioni après l’installation
fr.StartMenuShortcut=Créer un raccourci dans le menu Démarrer
fr.StartWithWindows=Lancer Positioni dans la zone de notification à l’ouverture de session
de.ContinueButton=Fortfahren
de.InstallInProgress=Die Installation läuft noch.
de.UninstallCloseButton=Schließen
de.UninstallButton=Deinstallieren
de.ExitButton=Beenden
de.ConfirmExit=Möchtest du die Installation beenden?
de.ExitExplanation=Die Installation ist noch nicht abgeschlossen. Wenn du jetzt beendest, wird Positioni nicht installiert.
de.OpenAfterInstall=Positioni nach der Installation öffnen
de.StartMenuShortcut=Eine Verknüpfung im Startmenü erstellen
de.StartWithWindows=Positioni bei der Anmeldung im Infobereich starten

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "startmenu"; Description: "{cm:StartMenuShortcut}"
Name: "startup"; Description: "{cm:StartWithWindows}"
Name: "openapp"; Description: "{cm:OpenAfterInstall}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,startup-error.txt"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Positioni"; Filename: "{app}\Positioni.exe"; WorkingDir: "{app}"; Tasks: startmenu
Name: "{userdesktop}\Positioni"; Filename: "{app}\Positioni.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  RunValue = 'WindowPositioner';
  WhCbt = 5;
  HcbtActivate = 5;
  GwChild = 5;
  GwNext = 2;
  SwpKeepOrderFocusAndRedraw = $001C;
  WmGetFont = $0031;

type
  TDialogRect = record
    Left, Top, Right, Bottom: Longint;
  end;
  TButtonTextSize = record
    Width, Height: Longint;
  end;
  TDialogPoint = record
    X, Y: Longint;
  end;

function GetCurrentThreadId: DWORD;
  external 'GetCurrentThreadId@kernel32.dll stdcall';
function SetWindowsHookEx(IdHook: Integer; Callback: UINT_PTR; Module: THandle; ThreadId: DWORD): THandle;
  external 'SetWindowsHookExW@user32.dll stdcall';
function CallNextHookEx(Hook: THandle; Code: Integer; WParam: UINT_PTR; LParam: Longint): Longint;
  external 'CallNextHookEx@user32.dll stdcall';
function UnhookWindowsHookEx(Hook: THandle): BOOL;
  external 'UnhookWindowsHookEx@user32.dll stdcall';
function EnumChildWindows(Window: HWND; Callback: UINT_PTR; Param: Longint): BOOL;
  external 'EnumChildWindows@user32.dll stdcall';
function GetWindowText(Window: HWND; Text: String; MaxCount: Integer): Integer;
  external 'GetWindowTextW@user32.dll stdcall';
function SetWindowText(Window: HWND; Text: String): BOOL;
  external 'SetWindowTextW@user32.dll stdcall';
function DialogClassName(Window: HWND; Text: String; MaxCount: Integer): Integer;
  external 'GetClassNameW@user32.dll stdcall';
function GetParent(Window: HWND): HWND;
  external 'GetParent@user32.dll stdcall';
function SetParent(Window, NewParent: HWND): HWND;
  external 'SetParent@user32.dll stdcall';
function GetWindow(Window: HWND; Command: UINT): HWND;
  external 'GetWindow@user32.dll stdcall';
function ReadDialogRect(Window: HWND; var Bounds: TDialogRect): Integer;
  external 'GetWindowRect@user32.dll stdcall';
function MapDialogPoint(Window: HWND; var Point: TDialogPoint): Integer;
  external 'ScreenToClient@user32.dll stdcall';
function ShowWindow(Window: HWND; Command: Integer): BOOL;
  external 'ShowWindow@user32.dll stdcall';
function SetWindowPos(Window, InsertAfter: HWND; X, Y, Width, Height: Integer; Flags: UINT): BOOL;
  external 'SetWindowPos@user32.dll stdcall';

function GetDC(Window: HWND): THandle;
  external 'GetDC@user32.dll stdcall';
function ReleaseDC(Window: HWND; DC: THandle): Integer;
  external 'ReleaseDC@user32.dll stdcall';
function SelectObject(DC, Obj: THandle): THandle;
  external 'SelectObject@gdi32.dll stdcall';
function MeasureText(DC: THandle; Text: String; Count: Integer; var Size: TButtonTextSize): Integer;
  external 'GetTextExtentPoint32W@gdi32.dll stdcall';

var
  SelectedOptionsLabel: TNewStaticText;
  ReadyHeaderExtension: Integer;
  DialogHook: THandle;
  UninstallConfirmText, UninstallSuccessText, UninstallRestartText, UninstallPartialText: String;
  AppRunningText, InstallWindowTitle: String;
  UninstallYesText, UninstallNoText, UninstallCancelText, UninstallOKText: String;
  NativeDialogMessage, UninstallYesButton, UninstallNoButton, UninstallOKButton: HWND;
  NativeDialogKind: Integer;

function WindowClass(Window: HWND): String;
begin
  SetLength(Result, 64);
  SetLength(Result, DialogClassName(Window, Result, Length(Result)));
end;

function FindNativeDialogControls(Window: HWND; Param: Longint): BOOL;
var
  Text, ClassName: String;
begin
  Result := True;
  ClassName := WindowClass(Window);
  if (ClassName <> 'TNewStaticText') and (ClassName <> 'TNewButton') then
    Exit;
  SetLength(Text, 1024);
  SetLength(Text, GetWindowText(Window, Text, Length(Text)));
  if ClassName = 'TNewStaticText' then
  begin
    if Text = '' then
      Exit;
    if NativeDialogMessage = 0 then
      NativeDialogMessage := Window;
    if Text = UninstallConfirmText then
      NativeDialogKind := 1
    else if Text = UninstallSuccessText then
      NativeDialogKind := 2
    else if Text = UninstallPartialText then
      NativeDialogKind := 2
    else if Text = UninstallRestartText then
      NativeDialogKind := 3
    else if Text = AppRunningText then
      NativeDialogKind := 4
    else
      Exit;
    NativeDialogMessage := Window;
    Exit;
  end;

  StringChangeEx(Text, '&', '', True);
  if Text = UninstallYesText then
    UninstallYesButton := Window
  else if Text = UninstallNoText then
    UninstallNoButton := Window
  else if Text = UninstallOKText then
    UninstallOKButton := Window;
end;

procedure RemoveNativeDialogIcon;
var
  TextPanel, ContentPanel, Sibling: HWND;
  TextBounds, IconBounds, MessageBounds: TDialogRect;
  Position: TDialogPoint;
begin
  TextPanel := GetParent(NativeDialogMessage);
  ContentPanel := GetParent(TextPanel);
  if (WindowClass(TextPanel) <> 'TStackPanel') or (ReadDialogRect(TextPanel, TextBounds) = 0) then
    Exit;

  { Inno places the icon in a panel immediately left of the message stack. }
  Sibling := GetWindow(ContentPanel, GwChild);
  while Sibling <> 0 do
  begin
    if (WindowClass(Sibling) = 'TPanel') and
      (ReadDialogRect(Sibling, IconBounds) <> 0) and (IconBounds.Right = TextBounds.Left) and
      (IconBounds.Top = TextBounds.Top) then
    begin
      if ReadDialogRect(NativeDialogMessage, MessageBounds) <> 0 then
      begin
        Position.X := IconBounds.Left + MessageBounds.Left - TextBounds.Left;
        Position.Y := MessageBounds.Top;
        { The stack clips its children. Give the message the full content panel. }
        if (MapDialogPoint(ContentPanel, Position) <> 0) and
          (SetParent(NativeDialogMessage, ContentPanel) <> 0) then
        begin
          ShowWindow(Sibling, SW_HIDE);
          SetWindowPos(NativeDialogMessage, 0, Position.X, Position.Y,
            MessageBounds.Right - MessageBounds.Left + TextBounds.Left - IconBounds.Left,
            MessageBounds.Bottom - MessageBounds.Top, SwpKeepOrderFocusAndRedraw);
        end;
      end;
      Exit;
    end;
    Sibling := GetWindow(Sibling, GwNext);
  end;
end;

procedure SetRestartButtonLabels;
var
  NowBounds, LaterBounds, ParentBounds: TDialogRect;
  NowLabel, LaterLabel: String;
  DC, Font, OriginalFont: THandle;
  NowSize, LaterSize: TButtonTextSize;
  Width, Gap, Height: Integer;
  Parent: HWND;
  Point: TDialogPoint;
begin
  NowLabel := SetupMessage(msgYesRadio);
  LaterLabel := SetupMessage(msgNoRadio);
  if (ReadDialogRect(UninstallYesButton, NowBounds) <> 0) and
    (ReadDialogRect(UninstallNoButton, LaterBounds) <> 0) then
  begin
    DC := GetDC(UninstallYesButton);
    if DC <> 0 then
    begin
      try
        Font := SendMessage(UninstallYesButton, WmGetFont, 0, 0);
        OriginalFont := 0;
        if Font <> 0 then
          OriginalFont := SelectObject(DC, Font);
        if (MeasureText(DC, NowLabel, Length(NowLabel), NowSize) <> 0) and
          (MeasureText(DC, LaterLabel, Length(LaterLabel), LaterSize) <> 0) then
        begin
          Width := LaterBounds.Right - LaterBounds.Left;
          Height := LaterBounds.Bottom - LaterBounds.Top;
          if NowSize.Width > LaterSize.Width then
            LaterSize.Width := NowSize.Width;
          if LaterSize.Width + Height > Width then
            Width := LaterSize.Width + Height;
          Gap := LaterBounds.Left - NowBounds.Right;
          Parent := GetParent(UninstallNoButton);
          if ReadDialogRect(Parent, ParentBounds) <> 0 then
          begin
            Point.X := LaterBounds.Right - 2 * Width - Gap;
            Point.Y := ParentBounds.Top;
            if MapDialogPoint(GetParent(Parent), Point) <> 0 then
            begin
              SetWindowPos(Parent, 0, Point.X, Point.Y,
                ParentBounds.Right - LaterBounds.Right + 2 * Width + Gap,
                ParentBounds.Bottom - ParentBounds.Top, SwpKeepOrderFocusAndRedraw);
              SetWindowPos(UninstallNoButton, 0, Width + Gap, LaterBounds.Top - ParentBounds.Top,
                Width, Height, SwpKeepOrderFocusAndRedraw);
              SetWindowPos(UninstallYesButton, 0, 0, NowBounds.Top - ParentBounds.Top,
                Width, Height, SwpKeepOrderFocusAndRedraw);
            end;
          end;
        end;
        if OriginalFont <> 0 then
          SelectObject(DC, OriginalFont);
      finally
        ReleaseDC(UninstallYesButton, DC);
      end;
    end;
  end;
  SetWindowText(UninstallYesButton, NowLabel);
  SetWindowText(UninstallNoButton, LaterLabel);
end;

function CustomizeNativeDialog(Code: Integer; WParam: UINT_PTR; LParam: Longint): Longint;
begin
  if (Code = HcbtActivate) and (WindowClass(HWND(WParam)) = 'TTaskDialogForm') then
  begin
    NativeDialogKind := 0;
    NativeDialogMessage := 0;
    UninstallYesButton := 0;
    UninstallNoButton := 0;
    UninstallOKButton := 0;
    EnumChildWindows(HWND(WParam), CreateCallback(@FindNativeDialogControls), 0);
    if NativeDialogMessage <> 0 then
      RemoveNativeDialogIcon;
    if NativeDialogKind <> 0 then
    begin
      if (NativeDialogKind = 1) and (UninstallYesButton <> 0) and (UninstallNoButton <> 0) then
      begin
        SetWindowText(UninstallYesButton, CustomMessage('UninstallButton'));
        SetWindowText(UninstallNoButton, UninstallCancelText);
      end;
      if (NativeDialogKind = 2) and (UninstallOKButton <> 0) then
        SetWindowText(UninstallOKButton, CustomMessage('UninstallCloseButton'));
      if (NativeDialogKind = 3) and (UninstallYesButton <> 0) and (UninstallNoButton <> 0) then
        SetRestartButtonLabels;
      if (NativeDialogKind = 4) and (UninstallOKButton <> 0) then
      begin
        SetWindowText(UninstallOKButton, CustomMessage('ContinueButton'));
        if InstallWindowTitle <> '' then
          SetWindowText(HWND(WParam), InstallWindowTitle);
      end;
    end;
  end;
  Result := CallNextHookEx(0, Code, WParam, LParam);
end;

procedure StartDialogHook;
begin
  UninstallYesText := SetupMessage(msgButtonYes);
  UninstallNoText := SetupMessage(msgButtonNo);
  UninstallCancelText := SetupMessage(msgButtonCancel);
  UninstallOKText := SetupMessage(msgButtonOK);
  StringChangeEx(UninstallYesText, '&', '', True);
  StringChangeEx(UninstallNoText, '&', '', True);
  StringChangeEx(UninstallCancelText, '&', '', True);
  StringChangeEx(UninstallOKText, '&', '', True);
  DialogHook := SetWindowsHookEx(WhCbt, CreateCallback(@CustomizeNativeDialog),
    0, GetCurrentThreadId);
end;

procedure StopDialogHook;
begin
  if DialogHook <> 0 then
    UnhookWindowsHookEx(DialogHook);
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if WizardSilent then
    Exit;
  InstallWindowTitle := SetupMessage(msgSetupWindowTitle);
  AppRunningText := FmtMessage(SetupMessage(msgSetupAppRunningError), ['{#AppName}']);
  StartDialogHook;
end;

procedure DeinitializeSetup;
begin
  StopDialogHook;
end;

function InitializeUninstall: Boolean;
begin
  Result := True;
  if UninstallSilent then
    Exit;

  UninstallConfirmText := FmtMessage(SetupMessage(msgConfirmUninstall), ['{#AppName}']);
  UninstallSuccessText := FmtMessage(SetupMessage(msgUninstalledAll), ['{#AppName}']);
  UninstallPartialText := FmtMessage(SetupMessage(msgUninstalledMost), ['{#AppName}']);
  UninstallRestartText := FmtMessage(SetupMessage(msgUninstalledAndNeedsRestart), ['{#AppName}']);
  AppRunningText := FmtMessage(SetupMessage(msgUninstallAppRunningError), ['{#AppName}']);
  StartDialogHook;
end;

procedure DeinitializeUninstall;
begin
  StopDialogHook;
end;

function InstalledCommand: String;
begin
  Result := '"' + ExpandConstant('{app}\Positioni.exe') + '" --startup';
end;

function WizardRelativeLeft(Control: TControl): Integer;
begin
  Result := 0;
  while Control <> WizardForm do
  begin
    Result := Result + Control.Left;
    Control := Control.Parent;
  end;
end;

procedure InitializeWizard;
var
  ContentOffset, LeftMargin: Integer;
begin
  LeftMargin := ScaleX(16);
  ContentOffset := WizardRelativeLeft(WizardForm.InnerNotebook) - LeftMargin;
  WizardForm.InnerNotebook.Left := WizardForm.InnerNotebook.Left - ContentOffset;
  WizardForm.InnerNotebook.Width := WizardForm.InnerNotebook.Width + ContentOffset;

  WizardForm.PageNameLabel.Left := LeftMargin - WizardRelativeLeft(WizardForm.MainPanel);
  WizardForm.PageNameLabel.Top := ScaleY(16);
  WizardForm.PageDescriptionLabel.Left := WizardForm.PageNameLabel.Left;
  WizardForm.PageDescriptionLabel.Top :=
    WizardForm.PageNameLabel.Top + WizardForm.PageNameLabel.Height + ScaleY(2);
  WizardForm.PageDescriptionLabel.Width :=
    WizardForm.MainPanel.ClientWidth - 2 * WizardForm.PageNameLabel.Left;
  { Give the completion text the same margins as the installation pages. }
  WizardForm.WizardBitmapImage2.Visible := False;
  WizardForm.FinishedHeadingLabel.Left := LeftMargin;
  WizardForm.FinishedHeadingLabel.Top := ScaleY(16);
  WizardForm.FinishedHeadingLabel.Width := WizardForm.FinishedPage.ClientWidth - 2 * LeftMargin;
  WizardForm.FinishedHeadingLabel.AdjustHeight;
  WizardForm.FinishedLabel.Left := LeftMargin;
  WizardForm.FinishedLabel.Top :=
    WizardForm.FinishedHeadingLabel.Top + WizardForm.FinishedHeadingLabel.Height + ScaleY(4);
  WizardForm.FinishedLabel.Width := WizardForm.FinishedPage.ClientWidth - 2 * LeftMargin;
  WizardForm.WizardSmallBitmapImage.Visible := False;
  WizardForm.SelectTasksLabel.Visible := False;
  WizardForm.TasksList.Height := WizardForm.TasksList.Height + WizardForm.TasksList.Top;
  WizardForm.TasksList.Top := 0;
  WizardForm.TasksList.Offset := WizardForm.TasksList.Offset + ScaleX(3);

  SelectedOptionsLabel := TNewStaticText.Create(WizardForm);
  SelectedOptionsLabel.Parent := WizardForm.ReadyPage;
  SelectedOptionsLabel.AutoSize := False;
  SelectedOptionsLabel.WordWrap := True;
  SelectedOptionsLabel.ShowAccelChar := False;
  SelectedOptionsLabel.Anchors := [akLeft, akTop, akRight, akBottom];
  SelectedOptionsLabel.SetBounds(0, ScaleY(6), WizardForm.ReadyPage.ClientWidth,
    WizardForm.ReadyPage.ClientHeight - ScaleY(6));
end;

procedure CurPageChanged(CurPageID: Integer);
var
  HeaderDelta, HeaderExtension: Integer;
  DesktopLabel: String;
begin
  HeaderExtension := 0;
  if CurPageID = wpReady then
    HeaderExtension := ScaleY(10);
  HeaderDelta := HeaderExtension - ReadyHeaderExtension;
  if HeaderDelta <> 0 then
  begin
    WizardForm.MainPanel.Height := WizardForm.MainPanel.Height + HeaderDelta;
    WizardForm.InnerNotebook.Top := WizardForm.InnerNotebook.Top + HeaderDelta;
    WizardForm.InnerNotebook.Height := WizardForm.InnerNotebook.Height - HeaderDelta;
    ReadyHeaderExtension := HeaderExtension;
  end;

  WizardForm.PageDescriptionLabel.Height :=
    WizardForm.MainPanel.ClientHeight - WizardForm.PageDescriptionLabel.Top;

  if CurPageID = wpFinished then
    WizardForm.NextButton.Left := WizardForm.CancelButton.Left +
      WizardForm.CancelButton.Width - WizardForm.NextButton.Width;

  if CurPageID = wpReady then
  begin
    WizardForm.PageDescriptionLabel.Caption :=
      WizardForm.PageDescriptionLabel.Caption + #13#10 + SetupMessage(msgReadyLabel2b);
    WizardForm.ReadyLabel.Visible := False;
    WizardForm.ReadyMemo.Visible := False;

    SelectedOptionsLabel.Caption := SetupMessage(msgReadyMemoTasks);
    if WizardIsTaskSelected('desktopicon') then
    begin
      DesktopLabel := CustomMessage('CreateDesktopIcon');
      StringChange(DesktopLabel, '&', '');
      SelectedOptionsLabel.Caption := SelectedOptionsLabel.Caption + #13#10 + '- ' + DesktopLabel;
    end;
    if WizardIsTaskSelected('startmenu') then
      SelectedOptionsLabel.Caption := SelectedOptionsLabel.Caption + #13#10 + '- ' + CustomMessage('StartMenuShortcut');
    if WizardIsTaskSelected('startup') then
      SelectedOptionsLabel.Caption := SelectedOptionsLabel.Caption + #13#10 + '- ' + CustomMessage('StartWithWindows');
    if WizardIsTaskSelected('openapp') then
      SelectedOptionsLabel.Caption := SelectedOptionsLabel.Caption + #13#10 + '- ' + CustomMessage('OpenAfterInstall');
    SelectedOptionsLabel.Visible := WizardSelectedTasks(False) <> '';
  end;
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
var
  Dialog: TSetupForm;
  MessageLabel: TNewStaticText;
  ExitButton, KeepButton: TNewButton;
  ButtonWidth, ButtonTop: Integer;
begin
  if not Cancel or not Confirm or WizardSilent then
    Exit;

  Dialog := CreateCustomForm(ScaleX(300), ScaleY(127), True, True);
  try
    Dialog.Caption := SetupMessage(msgExitSetupTitle);
    MessageLabel := TNewStaticText.Create(Dialog);
    MessageLabel.Parent := Dialog;
    MessageLabel.AutoSize := False;
    MessageLabel.WordWrap := True;
    MessageLabel.Caption := CustomMessage('ConfirmExit') + #13#10#13#10;
    if CurPageID = wpInstalling then
      MessageLabel.Caption := MessageLabel.Caption + CustomMessage('InstallInProgress')
    else
      MessageLabel.Caption := MessageLabel.Caption + CustomMessage('ExitExplanation');
    MessageLabel.SetBounds(ScaleX(16), ScaleY(14), Dialog.ClientWidth - ScaleX(32), ScaleY(72));

    ExitButton := TNewButton.Create(Dialog);
    ExitButton.Parent := Dialog;
    ExitButton.Caption := CustomMessage('ExitButton');
    ExitButton.ModalResult := mrOk;

    KeepButton := TNewButton.Create(Dialog);
    KeepButton.Parent := Dialog;
    KeepButton.Caption := SetupMessage(msgButtonCancel);
    KeepButton.ModalResult := mrCancel;
    KeepButton.Cancel := True;
    KeepButton.Default := True;

    ButtonWidth := Dialog.CalculateButtonWidth([ExitButton.Caption, KeepButton.Caption]);
    ButtonTop := Dialog.ClientHeight - ScaleY(34);
    KeepButton.SetBounds(Dialog.ClientWidth - ScaleX(16) - ButtonWidth, ButtonTop, ButtonWidth, ScaleY(23));
    ExitButton.SetBounds(KeepButton.Left - ScaleX(8) - ButtonWidth, ButtonTop, ButtonWidth, ScaleY(23));
    Dialog.ActiveControl := KeepButton;
    Dialog.FlipAndCenterIfNeeded(True, WizardForm, False);
    Cancel := Dialog.ShowModal = mrOk;
    Confirm := False;
  finally
    Dialog.Free;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ExistingCommand: String;
  ResultCode: Integer;
begin
  if CurStep = ssDone then
  begin
    if not WizardSilent and WizardIsTaskSelected('openapp') then
      if not Exec(ExpandConstant('{app}\Positioni.exe'), '', ExpandConstant('{app}'),
        SW_SHOWNORMAL, ewNoWait, ResultCode) then
        Log('Could not open Positioni: ' + SysErrorMessage(ResultCode));
    Exit;
  end;

  if CurStep <> ssPostInstall then
    Exit;
  if not WizardSilent then
  begin
    if WizardIsTaskSelected('startup') then
      RegWriteStringValue(HKCU, RunKey, RunValue, InstalledCommand)
    else if RegQueryStringValue(HKCU, RunKey, RunValue, ExistingCommand) then
      if CompareText(ExistingCommand, InstalledCommand) = 0 then
        RegDeleteValue(HKCU, RunKey, RunValue);

    if not WizardIsTaskSelected('startmenu') then
      DeleteFile(ExpandConstant('{userprograms}\Positioni.lnk'));
    if not WizardIsTaskSelected('desktopicon') then
      DeleteFile(ExpandConstant('{userdesktop}\Positioni.lnk'));
  end;

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
