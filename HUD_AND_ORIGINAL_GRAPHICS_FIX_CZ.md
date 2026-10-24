# OpenNFS1 SE Reloaded 1.8.1 – závodní HUD a funkční původní grafické volby

Tato oprava vznikla na základě srovnání uživatelského screenshotu 1.8.0 a původní hry a přímého rozboru `SIMDATA/MISC/MASKHI.FSH` a `FRONTEND/ART/OPTION/GRAPHICS.QFS` z jeho originálního CD. Neobsahuje žádné proprietární textury.

## Závodní HUD

Původní `stat` má 640×32 px a už obsahuje oddělovače času, ikony převodu, vlajky, pole `LAP/MILES` a oddělovač pořadí. Starší kód kreslil duplikované oddělovače fontem, překrýval ikony a červené rychlostní sprity 28×27 násilně zmenšoval na 18×23 s příliš malým rozestupem.

Oprava:
- Počítadla času, převodu, kol a pořadí používají nativní `yel0–yel9`, `yeln`, `yelr` z MASKHI, nikoli cizí žlutý font.
- Červené `red0–red9` jsou znovu ve zdrojové velikosti 28×27 a končí **před** ikonou řazení na X=349.
- Časové dvojtečky, desetinné tečky a lomítka z originálního pozadí se již znovu nepřekreslují.
- Souřadnice upravuje čistá testovatelná třída `HudGeometry`; Windows CI kontroluje všechny počty číslic (včetně trojciferné rychlosti a dvouciferného pořadí).
- Spodní `mpbd` mapa, otáčkoměr i podklad 3D renderingu zůstávají z předchozí verze.

**Zatím nedokončené:** skutečné míle na otevřené silnici nelze získat z pouhého počtu uzlů; starý umělý výpočet `remainingNodes * 0.01` byl odstraněn. V originálním nápisu MILES proto zůstane číselné pole prázdné, dokud nepřidáme ověřenou kalibraci jednotek TRI. Rozložení čísel je teď založené na původním FSH, ale fyzický vzhled vyžaduje ještě kontrolu ve Windows.

## Originální Graphics Detail

Důsledně zachovává grafiku a hodnoty z `GRAPHICS.QFS`; jednotlivé přepínače nyní mají skutečný účinek:

| Původní volba | Co přesně dělá tento remake |
| --- | --- |
| Screen Resolution | Otevře funkční rozšířené rozlišení/upscaling; engine zůstává virtuálně na 640×480, režim 320×200 není předstírán |
| Window Size (F1) | Windowed / Fullscreen, okamžitě nastaví backbuffer |
| Image Quality (F2) | Point / Linear / Anisotropic filtrování textur vozovky i scenérie |
| View Distance (F3) | 2500 / 5000 / 7500 pro kameru při příštím startu závodu |
| Car Detail | High zobrazí stíny všech závodníků; Low vykresluje pouze stín hráče (nejde ještě o plné 3D LOD) |
| Horizon (F4) | Skutečně zapíná/vypíná skybox v renderování tratí |
| F9 | Moderní Display Options: fullscreen, velikost okna, upscale, gamma/jas/kontrast |

Nastavení `textureQuality`, `highCarDetail` a `horizonEnabled` se zapisuje do `%LOCALAPPDATA%\OpenNFS1\settings.json` stejně jako ostatní volby. Cancel obnovuje původní hodnoty.

## Omezení verifikace

Windows GitHub Actions dokáže sestavit projekt, publishnout EXE a prověřit přísné mezní hodnoty HUDu i ostatní regresní testy. **Nevykonává však interaktivní 3D hru s obrazovkou a se skutečným CD**, proto vizuální shodu ani subjektivní vzhled neoznačujeme jako kompletně otestovaný. Další mezník bude ruční srovnání HUDu pixel po pixelu a dokončení best times, highlights a dalších historických obrazovek.
