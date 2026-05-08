!define MUI_VERBOSE 1
!include "FileFunc.nsh"

!define CompanyName "harp-tech"
!define AppName "Harp.Regulator"
!define AppNameNoSpaces "Harp.Regulator"

!define ASSETS_DIR "${__FILEDIR__}\src\${AppName}\Assets"
!define ARTIFACTS_DIR "${__FILEDIR__}\artifacts"
!ifndef NSIS_INPUT_DIR
  !define NSIS_INPUT_DIR "${ARTIFACTS_DIR}\publish\${AppName}\release_win-${ARCHITECTURE}"
!endif

!ifdef PRERELEASE
  !define APP_PRERELEASE_TEXT "-${PRERELEASE}"
!else
  !define APP_PRERELEASE_TEXT ""
!endif

!define AppVersion "${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_BUILD}${APP_PRERELEASE_TEXT}"
!define APP_NUMERIC_VERSION "${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_BUILD}"
!define /date YEAR "%Y"
!define OUT_FILE_NAME "${AppNameNoSpaces}.v${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_BUILD}${APP_PRERELEASE_TEXT}-win-${ARCHITECTURE}"

Unicode true
Name "${AppName} v${AppVersion}"
Icon "${ASSETS_DIR}\logo.ico"

!include "MUI2.nsh"

OutFile "${ARTIFACTS_DIR}\${OUT_FILE_NAME}.exe"
InstallDir "$LOCALAPPDATA\${CompanyName}\${AppName}"
RequestExecutionLevel user

VIProductVersion "${APP_NUMERIC_VERSION}.0"
VIAddVersionKey "ProductName" "${AppName}"
VIAddVersionKey "CompanyName" "${CompanyName}"
VIAddVersionKey "FileDescription" "${AppName}"
VIAddVersionKey "FileVersion" "${APP_NUMERIC_VERSION}"
VIAddVersionKey "ProductVersion" "${APP_NUMERIC_VERSION}"
VIAddVersionKey "LegalCopyright" "(c) ${YEAR} ${CompanyName}"
VIAddVersionKey "LegalTrademarks" "${CompanyName}"
VIAddVersionKey "OriginalFilename" "${OUT_FILE_NAME}"

Var StartMenuFolder

!define MUI_ABORTWARNING
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "${ASSETS_DIR}\logo.png"
!define MUI_HEADERIMAGE_RIGHT

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\${CompanyName}\${AppName}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "${CompanyName}\${AppName}"

!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "${AppName} v${AppVersion}" FirstSection
  SetOutPath "$INSTDIR"

  File /r `${NSIS_INPUT_DIR}\*.*`

  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "EstimatedSize" "$0"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "DisplayName" "${CompanyName} - ${AppName}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "InstallLocation" "$\"$INSTDIR$\""
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "DisplayIcon" "$\"$INSTDIR\logo.ico$\""
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "Publisher" "${CompanyName}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "DisplayVersion" "${AppVersion}"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "VersionMajor" ${VERSION_MAJOR}
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "VersionMinor" ${VERSION_MINOR}
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}" "NoRepair" 1

  WriteRegStr HKCU "Software\${CompanyName}\${AppName}" "" $INSTDIR

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
    CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
    CreateShortcut "$SMPROGRAMS\$StartMenuFolder\${AppName}.lnk" "$INSTDIR\${AppName}.exe" "" "$INSTDIR\${AppName}.exe" 0
    CreateShortcut "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
  !insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"

  !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
  Delete "$SMPROGRAMS\$StartMenuFolder\Uninstall.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\${AppName}.lnk"
  RMDir "$SMPROGRAMS\$StartMenuFolder"

  DeleteRegKey /ifempty HKCU "Software\${CompanyName}\${AppName}"
  DeleteRegKey /ifempty HKCU "Software\${CompanyName}"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${CompanyName} ${AppName}"
SectionEnd
