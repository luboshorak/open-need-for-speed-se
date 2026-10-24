# OpenNFS1 — modernized source project

OpenNFS1 is an independent C#/MonoGame reimplementation of the first Need for Speed, based on original data files. Created by Jeff H / 1am Studios, originally published in 2015.

**Modernization (2026):** This package changes the build target to .NET 8 and MonoGame DesktopGL 3.8.5.1. See [README_MODERNIZACE_CZ.md](README_MODERNIZACE_CZ.md) for build commands and limitations. It has **not** been runtime-validated against an original CD.

The game needs legally obtained game data from The Need for Speed Special Edition. None is included. Copy the game files into `CD_Data` next to the executable or configure `cdDataPath` in `gameconfig.json`.

```powershell
dotnet tool restore
dotnet restore OpenNFS1.sln
dotnet build OpenNFS1.sln -c Debug
dotnet run --project OpenNFS1/OpenNFS1.csproj
```

Original game models, textures, tracks and cars are copyright of Pioneer Productions / EA Seattle. OpenNFS1 is not affiliated with those companies.
