#define AppName "blinkNode Light"
#define AppVersion "0.1.0"
#define AppPublisher "blinkNode"
#define AppExeName "blinkNode.App.exe"

[Setup]
AppId={{54208065-FD57-4EB5-AE97-7518ED37C401}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\blinkNode Light
DefaultGroupName=blinkNode Light
DisableProgramGroupPage=yes
OutputDir=..\publish
OutputBaseFilename=blinkNode-Light-Setup-0.1.0-win-x64
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\blinkNode.App\blinkNode.ico
UninstallDisplayIcon={app}\{#AppExeName}
; The payload is built by build-setup.ps1 before ISCC is invoked.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\publish\blinkNode-Light-0.1.0-setup-payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\blinkNode Light"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\blinkNode Light"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch blinkNode Light"; Flags: postinstall nowait skipifsilent
