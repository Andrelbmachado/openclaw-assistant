#define AppName "OpenClaw Desktop"
#define AppVersion "1.0.0"
#define AppPublisher "OpenClaw"
#define AppExeName "OpenClaw.Desktop.exe"

[Setup]
AppId={{A74C232A-D0C6-4B85-BD62-57340E6D2CD4}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\OpenClaw Desktop
DefaultGroupName=OpenClaw
OutputDir=..\artifacts
OutputBaseFilename=OpenClaw-Desktop-Setup-x64
SetupIconFile=..\..\..\ui\public\favicon.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes

[Files]
Source: "..\publish\OpenClaw.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{app}\openclaw-desktop.json"

[Icons]
Name: "{autoprograms}\OpenClaw Desktop"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\OpenClaw Desktop"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch OpenClaw Desktop"; Flags: nowait postinstall skipifsilent
