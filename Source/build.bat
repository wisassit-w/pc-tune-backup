@echo off
chcp 65001 >nul
title PC Tune Backup - Build
for /F %%a in ('echo prompt $E ^| cmd') do set "ESC=%%a"
set "R=%ESC%[0m"
set "CY=%ESC%[96m"
set "GR=%ESC%[92m"
set "RD=%ESC%[91m"
set "MG=%ESC%[95m"
set "YL=%ESC%[93m"
cls
echo.
echo %MG%  ============================================================%R%
echo %MG%    PC TUNE BACKUP%R%  - rebuild from source (optional)
echo %MG%  ============================================================%R%
echo.
echo   You do NOT need this file to use the program.
echo   PcTuneBackup.exe is already built. This only rebuilds it.
echo.
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo %RD%  [X] Built-in Windows compiler not found.%R%
  pause
  exit /b 1
)
echo %CY%  [1/1]%R% Compiling...
"%CSC%" /nologo /target:winexe /platform:anycpu /out:"%~dp0..\PcTuneBackup.exe" /win32icon:"%~dp0app.ico" /win32manifest:"%~dp0app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "%~dp0*.cs"
if errorlevel 1 (
  echo.
  echo %RD%  [X] Build failed. See messages above.%R%
  pause
  exit /b 1
)
echo.
echo %GR%  [OK]%R% Built: %YL%%~dp0..\PcTuneBackup.exe%R%
echo.
pause
