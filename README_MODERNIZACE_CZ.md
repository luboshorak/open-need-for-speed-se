# OpenNFS1 1.2 – modernizace pro současné Visual Studio

Toto je pracovní modernizace zdrojového projektu OpenNFS1. Cílem je zachovat původní logiku hry a nahradit zastaralé build závislosti. **Není to otestované hotové vydání hry:** v prostředí, kde byl balíček připraven, nebyly dostupné .NET SDK, Windows ani originální herní datové soubory. Skutečný build a hraní je nutné ověřit ve Windows.

## Požadavky

- Windows 10/11 x64, aktuální Visual Studio 2022/2026 se podporou vývoje v .NET (nebo .NET 8 SDK a příkazová řádka).
- Připojení k internetu při prvním sestavení: obnovení NuGet balíčků a lokálního MGCB nástroje.
- .NET 8 SDK, nikoli pouze runtime. Pro spuštění publikované framework-dependent verze i .NET 8 Desktop Runtime podle požadavků aplikace.
- Grafický ovladač podporující MonoGame DesktopGL / OpenGL.
- Vlastní legálně pořízená data ze hry *The Need for Speed Special Edition* (1996). **Nejsou součástí tohoto ZIPu.**

## Nejjednodušší sestavení

1. Rozbal archiv do cesty bez diakritiky, například `C:\Dev\OpenNFS1`.
2. Spusť `build-windows.cmd` ze složky projektu (potřebuje `dotnet` na PATH).
3. Při úspěchu bude aplikace v `publish\OpenNFS1.exe`.
4. Pro ladění otevři `OpenNFS1.sln` ve Visual Studiu, nastav `OpenNFS1` jako **Startup Project** a zvol Debug / Any CPU.

Ručně přes terminál v kořeni repozitáře:

```powershell
dotnet --info
dotnet tool restore
dotnet restore OpenNFS1.sln
dotnet build OpenNFS1.sln -c Debug
dotnet run --project OpenNFS1/OpenNFS1.csproj
```

Pokud používáš F5 ve Visual Studiu, jednou před prvním sestavením proveď `dotnet tool restore` v terminálu otevřeném v kořeni projektu.

## Originální herní data

Data CD zkopíruj do `CD_Data` **vedle výsledného OpenNFS1.exe** (tedy například `publish\CD_Data`). Podadresáře musí být přímo v `CD_Data`, například:

```text
publish/
  OpenNFS1.exe
  gameconfig.json
  Content/
    ArialBlack.xnb
    ArialBlack-Italic.xnb
    ParticleEffect.xnb
    smoke.xnb
    splash-screen.xnb
  CD_Data/
    FRONTEND/ART/CONTROL/CENTRAL.QFS
    SIMDATA/MISC/AL1.TRI
    SIMDATA/CARFAMS/MRX7.CFM
    SIMDATA/SOUNDBNK/RX7_SW.BNK
```

Na Windows nezáleží na velikosti písmen souboru. Pokud máš data jinde, uprav `cdDataPath` v `gameconfig.json` vedle EXE, třeba:

```json
"cdDataPath": "D:\\Hry\\NFSSE"
```

Nebo použij spouštěcí parametr:

```powershell
.\OpenNFS1.exe --data "D:\Hry\NFSSE" --windowed
```

Další parametr `--fullscreen` spouští fullscreen. Pokud data chybějí, hra by měla zobrazit příslušnou obrazovku a umožnit zopakování kontroly klávesou Enter.

## Co je upravené

- Nové jednoduché SDK-style `.csproj`, jednotné konfigurace Debug/Release a Any CPU, `.NET 8`.
- MonoGame DesktopGL `3.8.5.1` ze správce NuGet namísto dll z roku 2015, bez pevné vazby na OpenTK, SDL.dll a starý import MSBuild.
- Nová konfigurace MGCB pro překládání zdrojových fontů, obrázků a částicového shaderu. Staré `.xnb` se nepoužívají.
- Jednotné načítání zdrojů vzhledem k adresáři `Content`, fullscreen přes standardní API MonoGame.
- Bez `Newtonsoft.Json` a `Ionic.Zip`: konfiguraci čte `System.Text.Json`. Zastaralé automatické stahování proprietárních dat přes nezabezpečené HTTP je nahrazeno pokynem ke vložení vlastních CD souborů.
- Zvukový provider používá původní soubory BNK namísto neexistujících `ConvertedAudio/*.xnb`. Převod podepsaného 8bitového PCM na 16bitové PCM; chybějící/nekompatibilní zvuky se zaznamenají do `audio.log` a nemají ukončit závod.
- Stabilní cesta ke konfiguraci a originálním CD souborům, základní parametry `--data`, `--windowed`, `--fullscreen`.
- GitHub Actions workflow pro Windows, .NET restore, build a publikaci x64 (spustí se po umístění projektu do repozitáře).

## Otevřené oblasti k ověření

- **Kompilace a běh nejsou v tomto prostředí ověřeny.** Součástí práce byly statické kontroly XML/JSON a návaznosti souborů, nikoli úspěšný kompilátor.
- Zejména nový `ParticleEffect.fx` musí projít MGCB a být otestován při renderování kouře.
- Čtení originálních tratí/aut, menu, fyzika, zvuky a runtime GPU bylo možné zkontrolovat pouze zdrojově. Výsledek může potřebovat další úpravy podle chyb z VS nebo runtime logu.
- Podle konkrétního vydání původního CD může být potřeba upravit cestu k datům nebo konverzi BNK zvuků.
- Starý NSIS instalátor ve složce `Installer` **není upravený na nové publikování**; používej `build-windows.cmd`/`dotnet publish`.

Po prvním pokusu o build a spuštění lze na základě chybového výpisu doladit chybějící kompatibilitu.
