; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "VKDrive"
#define MyAppVersion GetFileVersion('..\VKDrive\bin\Release\VKDrive.exe')
#define MyAppPublisher "VDrive"
#define MyAppURL "http://dnap.su/"
#define MyAppExeName "VKDrive.exe"
#define UninstallRegKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\VKDrive_is1"


[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId=VKDrive
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultGroupName={#MyAppName}
LicenseFile=..\License.rtf
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes
DefaultDirName={code:GetDefaultDir}
;DefaultDirName={reg:HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VKDrive_is1,InstallLocation}
DisableDirPage=auto

[Languages]
Name: "ru"; MessagesFile: "Russian.isl"
;Name: "en"; MessagesFile: "compiler:Default.isl"
;Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: 

[Files]
Source: "..\VKDrive\bin\Release\Resurces\VKDriveSettings.exe"; DestDir: "{app}\Resurces"; Flags: ignoreversion
Source: "..\..\dokany\Dokany_0.8.0-RC2\DokanInstall_0.8.0-RC2.exe"; DestDir: "{app}\Resurces"; Flags: ignoreversion; AfterInstall: RunDokanInstaller;
Source: "..\VKDrive\bin\Release\VKDrive.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\DokanNet.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\EntityFramework.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\EntityFramework.SqlServer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\log4net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\System.Data.SQLite.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\VKDrive\bin\Release\VKDrive.application"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\VKDrive.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\VKDrive.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\x86\SQLite.Interop.dll"; DestDir: "{app}\x86"; Flags: ignoreversion
Source: "..\VKDrive\bin\Release\x64\SQLite.Interop.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
Source: "..\License.rtf"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}";

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall


[Code]
// shared code for installing the products
#include "scripts\products.iss"
// helper functions
#include "scripts\products\stringversion.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\dotnetfxversion.iss"
#include "scripts\products\msiproduct.iss"
#include "scripts\products\dotnetfx45.iss"
#include "scripts\products\vcredist2013.iss"


function IsUpgrade(): Boolean;
var
  sPrevPath: String;
begin
  sPrevPath := WizardForm.PrevAppDir;
  Result := (sPrevPath <> '');
end;


function GetStringVersion(FileName: string) : string;
var 
    MS, LS : cardinal;
    V1, V2, V3, V4 : dword;
begin
    GetVersionNumbers(FileName, MS, LS );
    V1 := MS shr 16;
    V2 := MS and $FFFF;
    V3 := LS shr 16;
    V4 := LS and $FFFF;
    Result := IntToStr(V1)+'.'+IntToStr(V2)+'.'+IntToStr(V3)+'.'+IntToStr(V4);
end;


procedure RunDokanInstaller;
var
  ResultCode: Integer;
begin
    if CompareText(GetStringVersion(ExpandConstant('{sys}\drivers\dokan.sys')), '6.3.9600.17336') <> 0 then begin
        if not Exec(ExpandConstant('{app}\Resurces\DokanInstall_0.8.0-RC2.exe'), '/S', '', SW_SHOWNORMAL,
        ewWaitUntilTerminated, ResultCode)
      then
        MsgBox(ExpandConstant('{cm:DokanInstallFail}') + #13#10 + SysErrorMessage(ResultCode), mbError, MB_OK);
    end;
end;



var
    InstallLocation: String;

function GetInstallString(): String;
begin
    initwinversion();
    dotnetfx45();
    vcredist2013();

end;

function GetDefaultDir(Param: string): string;
var
    InstPath: String;
    InstallString: String;
begin
    InstPath := ExpandConstant('{#UninstallRegKey}');
    InstallString := '';
	if RegValueExists(HKEY_LOCAL_MACHINE, InstPath, 'InstallLocation') then begin
        RegQueryStringValue(HKEY_LOCAL_MACHINE, InstPath, 'InstallLocation', InstallString);
        Result := InstallString;
        InstallLocation := InstallString;
    end else
      Result := ExpandConstant('{pf}\{#MyAppName}');
end;

function InitializeSetup: Boolean;
var
    V: Integer;
    Version: String;
begin
    if RegValueExists(HKEY_LOCAL_MACHINE, ExpandConstant('{#UninstallRegKey}'), 'UninstallString') then begin
        RegQueryStringValue(HKEY_LOCAL_MACHINE, ExpandConstant('{#UninstallRegKey}'), 'DisplayVersion', Version);
		if Version <= ExpandConstant('{#MyAppVersion}') then begin 
            Result := True;
            GetInstallString();
        end
        else begin
            MsgBox(ExpandConstant('{cm:VKDriveOld}'), mbInformation, MB_OK);
            Result := False;
        end;
    end
	else begin
		Result := True;
		GetInstallString();
	end;
end;


function GetPathInstalled(AppID: String): String;
	var
		PrevPath: String;
	begin
		PrevPath := '';
		if not RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\'+AppID+'_is1', 'Inno Setup: App Path', PrevPath) then begin
			RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\'+AppID+'_is1', 'Inno Setup: App Path', PrevPath);
		end;
		Result := PrevPath;
	end;

function ShouldSkipPage(PageID: Integer): Boolean;
  var
    PrevDir:String;
	begin
		PrevDir := GetPathInstalled('VKDrive');
		if length(Prevdir) > 0 then begin
			// skip selectdir if It's an upgrade
			if (PageID = wpSelectDir) then begin
				Result := true;
			end else if (PageID = wpSelectProgramGroup) then begin
				Result := true;
			end else if (PageID = wpSelectTasks) then begin
	 		    Result := true;
			end else if (PageID = wpLicense) then begin
	 		    Result := true;
			end else begin
				Result := false;
			end;
		end;
	end;







