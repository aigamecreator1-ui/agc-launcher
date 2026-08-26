; Inno Setup script for the AGC Launcher Windows installer.
; Compile with ISCC.exe (Inno Setup 6, free from https://jrsoftware.org/isinfo.php),
; or just run scripts\publish-windows.ps1 which does the publish + compile for you.
;
; Expects a published, self-contained build already sitting in ..\publish\win-x64 —
; see scripts\publish-windows.ps1 for the exact `dotnet publish` command.

#define MyAppName "AGC Launcher"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "AGC"
#define MyAppExeName "AGC.Launcher.exe"
#define MyPublishDir "..\publish\win-x64"

[Setup]
; Fixed, stable GUID — keep this the same across versions so Windows treats future
; installers as upgrades of the same app rather than a separate install.
AppId={{6F1D2F0F-4CE0-4ECE-AB82-CE6AF4B04037}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=AGC-Launcher-Setup
SetupIconFile=..\src\AGC.Launcher\Assets\agc_logo.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible
; Unsigned installer — Windows SmartScreen may show an "Unknown publisher" prompt on
; first run. Users can click "More info" -> "Run anyway". A code-signing certificate
; removes this warning but isn't required to ship.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
