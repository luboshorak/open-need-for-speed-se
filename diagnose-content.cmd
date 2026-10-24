@echo off
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0" || exit /b 1

echo === OpenNFS1 MonoGame content diagnostics ===
echo Project directory: %CD%
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo ERROR: dotnet.exe is not on PATH. Install the .NET 8 SDK.
  exit /b 1
)

dotnet --version
if errorlevel 1 exit /b 1
echo.

REM Extracted ZIP files may inherit Mark-of-the-Web. Diagnose it; never silently disable security.
powershell.exe -NoProfile -NonInteractive -Command "$f='.\.config\dotnet-tools.json'; if (Get-Item -LiteralPath $f -Stream Zone.Identifier -ErrorAction SilentlyContinue) { exit 42 }" >nul 2>&1
if "%ERRORLEVEL%"=="42" (
  echo ERROR: Windows has blocked .config\dotnet-tools.json (Mark of the Web).
  echo If you trust this project's source, run from the repository root:
  echo   powershell -NoProfile -Command "Unblock-File -LiteralPath '.\.config\dotnet-tools.json'"
  echo Then rerun diagnose-content.cmd. Do NOT disable Windows security globally.
  exit /b 42
)

echo === Restore MonoGame MGCB local tool ===
dotnet tool restore
if errorlevel 1 (
  echo ERROR: MGCB tool restore failed. Check the error above.
  echo If Windows reports a blocked manifest, run:
  echo   powershell -NoProfile -Command "Unblock-File -LiteralPath '.\.config\dotnet-tools.json'"
  exit /b 1
)

echo.
echo === Rebuild MonoGame content (full log in content-build.log) ===
pushd "OpenNFS1\Content" || exit /b 1
dotnet mgcb /@:"Content.mgcb" /platform:DesktopGL /outputDir:"bin/DesktopGL/Content" /intermediateDir:"obj/DesktopGL/net8.0/Content" /workingDir:"%CD%" /rebuild > "%~dp0content-build.log" 2>&1
set "MGCB_EXIT=%ERRORLEVEL%"
popd
type "content-build.log"
echo.
echo MGCB exit code: %MGCB_EXIT%
echo Log: "%~dp0content-build.log"
if not "%MGCB_EXIT%"=="0" exit /b %MGCB_EXIT%
echo SUCCESS: MGCB finished. Try dotnet build OpenNFS1.sln next.
exit /b 0
