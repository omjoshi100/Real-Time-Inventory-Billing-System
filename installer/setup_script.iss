; ============================================================================
; INNO SETUP INSTALLER SCRIPT
; Real-Time Inventory & Billing System
; ============================================================================

#define MyAppName "Real-Time Inventory & Billing System"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Enterprise Capstone Solutions"
#define MyAppURL "https://github.com/example/real-time-inventory-billing"

[Setup]
AppId={{9F82A34C-2150-4F81-BA83-118E88E50C2A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\RealTimeInventoryBilling
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\installer_output
OutputBaseFilename=RealTimeInventoryBilling_Setup_v1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; API Backend
Source: "..\publish\API\*"; DestDir: "{app}\API"; Flags: ignoreversion recursesubdirs createallsubdirs
; WPF Client
Source: "..\publish\WPF_Cashier_POS\*"; DestDir: "{app}\WPF_Cashier_POS"; Flags: ignoreversion recursesubdirs createallsubdirs
; WinForms Admin
Source: "..\publish\WinForms_Admin\*"; DestDir: "{app}\WinForms_Admin"; Flags: ignoreversion recursesubdirs createallsubdirs
; SQL Scripts
Source: "..\database\*"; DestDir: "{app}\database"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\WPF Cashier POS Terminal"; Filename: "{app}\WPF_Cashier_POS\RealTimeInventoryBilling.WPF.exe"
Name: "{group}\WinForms Admin Console"; Filename: "{app}\WinForms_Admin\RealTimeInventoryBilling.WinForms.exe"
Name: "{group}\Start Background API Service"; Filename: "{app}\API\RealTimeInventoryBilling.API.exe"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\WPF Cashier POS Terminal"; Filename: "{app}\WPF_Cashier_POS\RealTimeInventoryBilling.WPF.exe"; Tasks: desktopicon
Name: "{autodesktop}\WinForms Admin Console"; Filename: "{app}\WinForms_Admin\RealTimeInventoryBilling.WinForms.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\API\RealTimeInventoryBilling.API.exe"; Description: "Launch ASP.NET Core API Service"; Flags: nowait postinstall skipifsilent
Filename: "{app}\WPF_Cashier_POS\RealTimeInventoryBilling.WPF.exe"; Description: "Launch WPF Cashier POS Terminal"; Flags: nowait postinstall skipifsilent
Filename: "{app}\WinForms_Admin\RealTimeInventoryBilling.WinForms.exe"; Description: "Launch WinForms Admin Console"; Flags: nowait postinstall skipifsilent
