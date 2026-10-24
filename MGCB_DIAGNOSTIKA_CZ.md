# Diagnostika sestavení OpenNFS1 ve Windows

## Windows hlásí, že dotnet-tools.json pochází z jiného počítače

Když projekt stáhneš jako ZIP, systém Windows může přidat souborům bezpečnostní označení *Mark of the Web*. `dotnet tool restore` pak zablokuje místní manifest `.config/dotnet-tools.json`. Není to chyba MonoGame, VS ani .NET SDK; samotné sestavení herního obsahu se ještě vůbec nespustilo.

Po kontrole, že soubor pochází z důvěryhodného zdroje, spusť v **PowerShellu v kořeni repozitáře**:

```powershell
Unblock-File -LiteralPath ".\.config\dotnet-tools.json"
dotnet tool restore
```

Alternativně použij `git clone https://github.com/luboshorak/open-need-for-speed-se.git`, který běžně na checkout souborů nepřenáší toto označení ze ZIP archivu. Neodblokovávej plošně cizí soubory a nevypínej Defender ani bezpečnostní politiky.

## MGCB obsah nejde sestavit

Spusť `diagnose-content.cmd` v kořeni repozitáře. Skript zkontroluje SDK, obnoví lokální nástroj MGCB a přeloží `OpenNFS1/Content/Content.mgcb` do `content-build.log` bez potlačení chyb. Pošli konkrétní chybu z logu.

Případně:

```powershell
dotnet tool restore
dotnet restore OpenNFS1.sln
dotnet build OpenNFS1.sln -c Release
```

Ve výpisu GitHub Actions z 24. 10. 2026 byl překlad fontů, textur i shaderu `ParticleEffect.fx` úspěšný. Tehdejší build skončil až na chybějícím `Ionic.Zip` (zastaralá obrazovka stahování dat) a nedefinovaném `Track` v `GameConfig.cs`; obě chyby tato opravná větev řeší.

**Úspěšný build není totéž co ověřený běh hry:** plný test vyžaduje původní CD data a Windows s podporovanou grafikou.
