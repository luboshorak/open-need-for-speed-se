# Need for Speed SE – opakovatelný veřejný build

Stejný koncept jako `packaging/Build-CombinedRelease.ps1` ve Spellcross Reloaded. Jde o **lokální PowerShell builder**, nikoli o další Git branch ani závislost na GitHub Actions. Tato složka patří přímo do kořene repozitáře `open-need-for-speed-se`.

## Instalace – pouze jednou

Rozbal `NFSSE_Reusable_Builder.zip` **do kořene svého lokálního repozitáře** (např. `Z:\Games\Open-need-for-speed-se`), takže bude existovat:

```text
open-need-for-speed-se/
  OpenNFS1/OpenNFS1.csproj
  packaging/
    Build-Release.ps1
    README.md
    setup/
      nfsse_classic.ico
      nfsse_modern.ico
      nfsse_sfx_logo.bmp
      nfsse_sfx_logo_hd.bmp
```

Instalace .NET 8 **SDK** je potřeba jen na **tvém počítači, kde sestavuješ**. Pro WinRAR SFX je potřeba nainstalovaný **WinRAR** (stejně jako u Spellcrossu). Na počítačích hráčů není potřeba instalovat .NET ani kodek zvlášť. První sestavení potřebuje internet ke stažení knihoven NuGet a staticky zabaleného FFmpeg LGPL. Další sestavení využívají cache.

## Každý další release

Na Windows otevři PowerShell v kořeni repozitáře a spusť:

```powershell
git switch master
git pull --ff-only origin master
.\packaging\Build-Release.ps1
```

Pokud nemůžeš spouštět lokální `.ps1` skripty, jednorázově pro aktuální PowerShell okno:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\packaging\Build-Release.ps1
```

Verze se čte přímo z `OpenNFS1\OpenNFS1.csproj` (např. `<Version>2.0.2</Version>`). Při vydání 2.0.2 změníš toto číslo ve zdrojáku a skript bez další editace vytvoří:

```text
dist\Need_For_Speed_SE_v2.0.2_win-x64.zip
dist\Need_For_Speed_SE_v2.0.2_win-x64_Setup.exe
```

Pokud WinRAR není instalovaný, vznikne ZIP a skript vypíše upozornění. Archivy používají **stejnou staging složku**; `OpenNFS1.exe` je přímo v kořeni ZIPu i po instalaci SFX.

## Co automaticky přibalí a zkontroluje

- `dotnet publish` pro Windows x64 jako **self-contained .NET 8** (žádný .NET SDK/runtime pro hráče).
- `MonoGame`, nativní `SDL2.dll`/`openal.dll`, zkompilovaný herní `Content`, import ISO přes `DiscUtils`.
- `Tools\ffmpeg.exe` z LGPL Windows FFmpeg buildu; zkontroluje dostupnost `adpcm_ima_ea_eacs`. První stažení cachuje do `dist\_cache`.
- Novou `.ico` ikonu přímo v `OpenNFS1.exe` a v EXE WinRAR instalátoru.
- WinRAR SFX s logem 93×302 i 186×604 px, výběrem cílové složky a zástupcem na ploše.
- `README-FIRST.txt`, credits, informace o FFmpeg licenci, seznam souborů SHA-256.
- Kontrolu, že balík neobsahuje původní komerční CD data (`FRONTEND`, `SIMDATA`, QFS, ISO atd.).

Originální CD **musí dodat hráč**. Při prvním spuštění stiskne `Enter` nebo `I` a vybere vlastní ISO, případně nakopíruje adresáře `FRONTEND` a `SIMDATA` do `CD_Data`. Neinstaluje žádné staré .NET Framework 4.5 setupy, DOSBox ani OpenAL externě.

## Volby

```powershell
.\packaging\Build-Release.ps1 -RunTests                  # před balením spustí regresní testy
.\packaging\Build-Release.ps1 -NoSfx                     # vytvoří jen ZIP
.\packaging\Build-Release.ps1 -IconStyle Modern          # moderní ikona namísto classic
.\packaging\Build-Release.ps1 -SkipBuild -NoZip          # z hotového stage jen WinRAR EXE
.\packaging\Build-Release.ps1 -SkipBuild                 # znovu zabalí obě varianty bez kompilace
.\packaging\Build-Release.ps1 -WinRARPath 'C:\Program Files\WinRAR\WinRAR.exe'
.\packaging\Build-Release.ps1 -FFmpegExe 'D:\Tools\ffmpeg.exe' # vlastní validovaný FFmpeg
```

`-SkipBuild` používá poslední již sestavené soubory dané verze: při změně kódu jej nepoužívej. Pro nové zdrojáky vždy běžný build. Při použití `-FFmpegExe` ověř redistribuční licenci vlastního FFmpeg buildu.

Skript **sám nedělá git commit, push, branch, tag ani GitHub Release**. Pokud chceš tuto packaging složku uchovat v GitHub repozitáři pro budoucí stroje, uděláš **jen jednou**:

```powershell
git add packaging/Build-Release.ps1 packaging/README.md packaging/setup
git commit -m "Add reusable NFS SE Windows release builder"
git push origin master
```

Pak už je script trvale v `master`. Není nutné vytvářet větev pro každý release. Hotový ZIP a SFX se na GitHub Release nahrají teprve po testování.

### Poznámka ke stávající větvi

Předchozí jednorázová větev `release/windows-self-contained-2026-10` obsahuje experimentální workflow; **není potřeba pro tento postup**. Po kontrole, že v ní není nic dalšího, můžeš odstranit pouze tuto vzdálenou větev:

```powershell
git push origin --delete release/windows-self-contained-2026-10
```

Před veřejným vydáním otestuj na čistých Windows rozbalení ZIPu, první start/import originálního ISO, hudbu a alespoň celý závod. Úspěšná kompilace sama o sobě není potvrzení, že hra běží bez chyb.
