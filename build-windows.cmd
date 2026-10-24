@echo off
setlocal
cd /d "%~dp0"
echo === Restoring MonoGame content tools ===
dotnet tool restore || goto :failed
echo === Restoring packages ===
dotnet restore OpenNFS1.sln || goto :failed
echo === Building Release ===
dotnet build OpenNFS1.sln -c Release --no-restore || goto :failed
echo === Publishing Windows x64 ===
dotnet publish OpenNFS1\OpenNFS1.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o publish || goto :failed
echo.
echo SUCCESS: publish\OpenNFS1.exe
exit /b 0
:failed
echo.
echo FAILED - read the errors above.
exit /b 1
