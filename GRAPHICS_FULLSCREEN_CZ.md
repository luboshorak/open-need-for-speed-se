# OpenNFS1 SE Reloaded 1.7.0 – fullscreen, obraz, škálování a kredity

## Co je implementováno

**Options → Graphics Detail** otevírá novou obrazovku pro moderní zobrazovací nastavení. Původní QFS menu ostatních obrazovek zůstává nezměněné.

- **Screen:** Windowed / Fullscreen. Fullscreen používá nativní rozlišení monitoru a přepíná se jako borderless režim, bez změny fyzického rozlišení desktopu.
- **Window Size:** 800×600, 1024×768, 1280×720, 1280×960, 1600×900, 1920×1080, 2560×1440. V režimu fullscreen se používá nativní rozlišení monitoru a tato položka určuje pouze budoucí velikost okna.
- **Upscaling:** Smooth (bilineární), Sharp (nearest-neighbor), Integer Pixels (celistvý násobek). Všechny módy zachovávají původní poměr stran 4:3 s černými pruhy. Integer Pixels v rozlišení 1920×1080 použije 2× → 1280×960 uprostřed obrazovky.
- **Gamma:** 0.60–1.60; nižší hodnoty ztmavují střední tóny.
- **Brightness:** −0.30 až +0.30.
- **Contrast:** 0.60–1.60; vyšší hodnoty mohou pomoci s vybledlým obrazem.
- **Draw Distance:** přibližně 2500 / 5000 / 7500, původní nastavení dohlednosti.
- **Reset:** pouze výchozí hodnoty korekce barev.
- **Apply & Save:** nastavení grafického výstupu se aplikuje za běhu hry a uloží se do `%LOCALAPPDATA%\OpenNFS1\settings.json`.
- **Cancel / Esc:** zahodí neuložený náhled korekcí a vrátí se do Options.

Klávesa **F11** kdykoliv přepne fullscreen / okno a tuto volbu automaticky uloží. Ostatní herní a zvuková nastavení fungují nezávisle.

### Jak funguje obraz

1. Originální menu, HUD i 3D scéna se stále skládají ve virtuálním rozlišení **640×480** (závodní 3D viewport má 640×400).
2. Výsledný render target se geometricky správně zvětší na požadovaný výstup. Umístění i hitboxy myši používají **stejnou funkci** `PresentationLayout.Calculate`, takže se neztratí soulad klikání při změně poměru stran nebo při integer upscale.
3. GPU pixel shader `ColorCorrection.fx` provede jas, kontrast a gamma v jediném závěrečném průchodu. Originální QFS textury zůstávají netknuté.

**DŮLEŽITÉ:** Tato fáze znamená upscale obrazového výstupu (včetně 4K fullscreen), ne skutečný 3D rendering v 4K. Samostatný vyšší interní 3D render target při současném zachování původního 640×480 UI bude další etapa. Nedeklarujeme tedy rozlišení 4K jako nativní grafiku hry.

### Doporučené nastavení na moderní monitor
Začni: Fullscreen ON, Scaling Sharp (nebo Integer pro opravdu ostré pixely), Gamma 0.85–0.95, Brightness 0.00 až −0.025, Contrast 1.10–1.20. Hodnoty jsou orientační — záleží na konkrétním monitoru a jeho barevném profilu.

## Kredity přímo ve hře

Starý úvodní text `By Jeff H - www.1amstudios.com` a dobový popis původního forku jsou nahrazeny prezentací současného projektu **OpenNFS1 SE Reloaded**. Nejde o odstranění zásluh:

- aktuální modernizace a navazující vývoj: **Luboš Horák**,
- původní základ `OpenNFS1`: **Jeff Harris / 1am Studios**,
- originální hra a zdrojová data: **Electronic Arts a příslušní autoři**.

V **Options → Credits** je nová plnohodnotná obrazovka. Detailní původ projektu zůstává i v `CREDITS.md` a Git historii.

## Validace a známá omezení

Windows GitHub Actions provádí build, publish, test ISO importu, cílové čáry, turnaje, závodních režimů, kolizí, showroomu a nový test matematického přepočtu fullscreen/upscaling/myši. Vizuální kvalitu gamma shaderu a fullscreen na konkrétních ovladačích zatím musí potvrdit interaktivní test ve Windows.

Z důvodu kompatibility není zapnuté žádné agresivní vyhlazování obrazu, HDR, DLSS/FSR ani vlastní GPU renderování tratí v rozlišení monitoru. Tyto funkce by vyžadovaly další renderovací pipeline, nikoliv pouhé zvětšení obrázku.
