# OpenNFS1 SE Reloaded

**Modern redevelopment and project maintenance: Luboš Horák**  
A C# / MonoGame reconstruction of *Road & Track Presents: The Need for Speed Special Edition* for current Windows PCs.

This actively developed fork has grown beyond a .NET compatibility update. It now adds original-disc ISO import, renewed game menus, the car showroom, local tournaments and saves, audio playback, vehicle-to-vehicle collisions, gamepad/keyboard/mouse navigation and ongoing restoration of original NFS SE behavior.

The game is a work in progress. Graphics and race logic are being restored from the original game's CD data, and some systems still need gameplay testing and refinement.

## Run and build

- Windows 10/11, .NET 8 SDK, Visual Studio or the .NET CLI.
- Your **own legally obtained NFS Special Edition CD/ISO**. Proprietary game assets are **not distributed** in this repository.
- Optional FFmpeg on PATH (or `Tools/ffmpeg.exe` next to the game) for the first conversion of original EACS music. Menu click effects and car sounds use the built-in BNK decoder.

```powershell
dotnet tool restore
dotnet restore OpenNFS1.sln
dotnet build OpenNFS1.sln -c Debug
dotnet run --project OpenNFS1/OpenNFS1.csproj
```

When the game starts without CD data, choose your original NFS SE `.iso` in the installer UI. You may also supply already-extracted CD files under `CD_Data` beside `OpenNFS1.exe`.

User preferences and championship save slots are stored under `%LOCALAPPDATA%\OpenNFS1`.

## What's implemented

- Four single-player modes: Head to Head, Single Race, Tournament, Time Trial
- Original menus and vehicle photo/details/showroom
- Original CD music and front-end effects (with optional FFmpeg for EACS music)
- Original vehicles and tracks, initial contact physics for traffic and AI
- Tournament progress, autosave, manual saves and restore
- Modern .NET 8 builds with Windows CI and regression tests

For technical notes, see [modernization](README_MODERNIZACE_CZ.md), [race modes and sound](ORIGINAL_MODES_OPTIONS_MUSIC_CZ.md), and [showroom and saves](SHOWROOM_TOURNAMENT_SAVES_CZ.md).

## Origins and acknowledgements

OpenNFS1 SE Reloaded is built upon the original [OpenNFS1 project by Jeff Harris / 1am Studios](https://github.com/jeff-1amstudios/OpenNFS1). The engine and historical format-parsing foundations were developed before this fork, with further reverse-engineering work by Ian Brown and Denis Auroux. We preserve that provenance while distinguishing the ongoing redevelopment, design and maintenance of this fork.

*The Need for Speed* and its original models, artwork, music and game data belong to their respective rights holders, including Electronic Arts / Pioneer Productions. This is an unofficial fan project and is not affiliated with or endorsed by EA.

See [CREDITS.md](CREDITS.md) for detailed credits and provenance. This README does not grant additional rights to any third-party code or game assets.
