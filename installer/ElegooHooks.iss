#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\src\ElegooLink.Desktop\bin\Release\net10.0-windows\publish\win-x64"
#endif

#ifndef InstallerOutputDir
  #define InstallerOutputDir "..\artifacts\installer"
#endif

#ifndef InstallerBaseName
  #define InstallerBaseName "ElegooHooks-Setup-" + MyAppVersion
#endif

#define MyAppName "Elegoo Hooks"
#define MyAppPublisher "Robert Gelb"
#define MyAppExeName "ElegooLink.Desktop.exe"

[Setup]
AppId=ElegooHooks.Desktop
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
OutputDir={#InstallerOutputDir}
OutputBaseFilename={#InstallerBaseName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\ElegooLink.Desktop\Assets\ApplicationIcon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
