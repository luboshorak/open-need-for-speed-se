# ISO import and rendering fixes (v1.3.0)

## First run
1. Start the game in Windows.
2. If the game does not find original NFS SE files, press **I** or **Enter** to choose your own .ISO CD image.
3. The importer reads ISO 9660/Joliet directly using DiscUtils, checks for `FRONTEND/ART/CONTROL/CENTRAL.QFS` and `SIMDATA/MISC/AL1.TRI`, extracts files to `CD_Data` beside the executable, and opens the main menu.
4. The extraction runs in the background so the game can show progress.

You do not need 7-Zip, a virtual DVD drive, admin rights or any automatic download. Original game files are **not included** in this repository.

**Supported:** standard ISO 9660/Joliet `.iso` containing NFS SE data. **Not supported yet:** raw `.bin/.cue`, multi-session/raw-sector images, archives such as `.7z`, and other editions of the game. If the file is not recognized, the importer shows an error without modifying existing installed game data.

An existing `CD_Data` directory is replaced only if it contains nothing except the project's `README.txt`. Otherwise the importer refuses to overwrite it. Files are first extracted to a temporary sibling directory and moved into place only when complete. The game executable folder must be writable; if installed under `Program Files`, run from a user-writable games folder instead.

## Rendering correction
MonoGame DesktopGL's `GameWindow.ClientBounds` can include desktop coordinates. The old code used that rectangle as a target on a local graphics backbuffer, shifting the entire game off the screen. The destination rectangle now starts inside the backbuffer, centers a 640×480 render target, and preserves 4:3 in windowed/fullscreen modes.

## Building
The game executable now targets `net8.0-windows` to use the native WinForms file picker (the engine remains `net8.0`). It depends on `DiscUtils.Iso9660` 0.16.13, and still uses MonoGame DesktopGL 3.8.5.1.

```powershell
dotnet tool restore
dotnet restore OpenNFS1.sln
dotnet build OpenNFS1.sln -c Release
```

Automated builds check compilation and publishing on a Windows runner; they do not prove the game runs with any particular CD image. Test an actual ISO and rendering on a Windows desktop to validate this feature.
