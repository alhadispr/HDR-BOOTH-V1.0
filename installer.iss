; HDR BOOTH Windows installer
#define MyAppName "HDR BOOTH"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "HDR BOOTH"
#define MyAppExeName "HDR-BOOTH.exe"

[Setup]
AppId={{D19A7C0D-8F62-4E18-9F6E-7B43F2D2A1C0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\HDR BOOTH
DefaultGroupName=HDR BOOTH
DisableProgramGroupPage=yes
OutputDir=artifacts\installer
OutputBaseFilename=HDR-BOOTH-Setup-{#MyAppVersion}-x64
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayName={#MyAppName}
Uninstallable=yes
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=1.0.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=HDR BOOTH Photobooth Management

[Languages]
Name: "indonesian"; MessagesFile: "compiler:Languages\Indonesian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut tambahan:"; Flags: unchecked

[Files]
Source: "artifacts\HDR-BOOTH-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\HDR BOOTH"; Filename: "{app}\HDR-BOOTH.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\HDR BOOTH"; Filename: "{app}\HDR-BOOTH.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\HDR-BOOTH.exe"; Description: "Jalankan HDR BOOTH"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
; Data in %LOCALAPPDATA%\HDR-BOOTH is intentionally preserved on uninstall.
