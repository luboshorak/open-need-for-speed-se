# OpenNFS1 SE Reloaded 1.8 – návrat původního HUD a obrazovek

Tato etapa je založená **přímo na původních souborech z dodaného `nfs.zip`** a na srovnání snímků originální DOS verze s dosavadním MonoGame remakem. Originální bitmapy, QFS, FSH ani jiné herní soubory nejsou committovány do veřejného GitHubu.

## 1. Horní závodní lišta a spodní mapa

Soubor `SIMDATA/MISC/MASKHI.FSH` skutečně obsahuje tyto položky:

| ID | Rozměr a originální pozice | Účel |
| --- | --- | --- |
| `stat` | 640×32, (0,0) | originální horní stavová lišta |
| `red0`–`red9` | 28×27 | původní červené číslice rychlosti |
| `lap#` | 109×24, (433,1) | pole počtu kol |
| `mile` | 109×24, (433,1) | pole vzdálenosti na otevřené silnici |
| `mpbd` | 116×142, (510,303) | původní rámeček mapy tratě |
| `tach` | 156×156, (10,289) | otáčkoměr v pohledu za autem |

Oprava ruší naši velkou souvislou řádku „207 km/h GEAR 4 L:2/3 8/8“, která překrývala původní pole. Ponechává naměřené hodnoty, ale sází je do malých vyhrazených segmentů původního `stat`, včetně originálních červených číslic rychlosti. Vrací `mpbd` a aktuální pozice všech závodníků.

- U okruhu vykresluje obrys z pozic uzlů `Track.RoadNodes` a do něj barevné značky hráče a AI, promítnuté pomocí testované `MapProjection`.
- U otevřených silnic zatím odpovídá polohám jezdců na segmentu svislá mapa; podrobnější dobové značky, skutečné míle a policejní ikony čekají na další práci.
- Hud a mapa se nyní vykreslují **až po** spodním panelu a tachometru, takže je spodní část mapy nezakrytá.

**Známý rozdíl oproti originálu:** čas, převod, některé číselné hodnoty a žluté/červené značky na mapě jsou na první iteraci stále dynamicky vykreslované na originálním podkladu. Nejde o pixelově dokonalý reimplementation všech grafických fontů a fyzikálního měření mil.

## 2. Detaily tratí z původního CD

Kliknutím na **střed fotografie tratě** v Control Central nebo stisknutím Enter při vybraném panelu tratě se otevírá původní `FRONTEND/ART/TRACKSEL/*SEL.QFS`:

| Trať | Originální soubor |
| --- | --- |
| Alpine | `ALPSEL.QFS` |
| Coastal | `COASTSEL.QFS` |
| City | `CITYSEL.QFS` |
| Rusty Springs | `RUSTSEL.QFS` |
| Autumn Valley | `AUTSEL.QFS` |
| Vertigo Ridge | `VERTSEL.QFS` |
| Burnt Sienna | `BRUNSEL.QFS` |
| Transpolis | `TROPSEL.QFS` |
| Lost Vegas | `LOSTSEL.QFS` |

Grafické vrstvy `bgnd`, `bsg1–3` a `ssg1–3` používají souřadnice z původního QFS. Spodní originální tlačítka `pred/nexd/dond` přepínají trať a vrací se do Control Central. Klávesami A/D se přepínají původní obrázky, T mění dostupnou denní dobu a L u okruhu volí 1 až 5 kol. Historické rekordy se zatím nevymýšlejí — jejich ukládání na původní stránku Best Times dosud chybí.

## 3. Skutečné Graphics Detail

`Options → Graphics Detail` načítá **`FRONTEND/ART/OPTION/GRAPHICS.QFS`** (640×480, původní originální grafika a pozice). Zatím jsou zapojené fullscreen/okno a dohlednost; nativní volby rozlišení 320×200 a 640×480, Image Quality, Car Detail a Horizon se zobrazují podle zdroje, ale nepředstírají podporu neimplementovaných režimů.

Původní zkratky **F1–F4** zůstávají přiřazené historickým volbám. Naše nová moderní obrazovka s fullscreen/window size, smooth/sharp/integer upscale, gamma, brightness a contrast zůstává dostupná přes **F9**. F10 nadále slouží jako nouzové obnovení okna 800×600 a F11 jako okamžitý fullscreen. Stav zůstává v `%LOCALAPPDATA%\OpenNFS1\settings.json`.

## 4. Původní závodní a výsledkové obrazovky

Při stisku **Race** v Control Central se nyní rovnou připraví skutečný závod, bez naší umělé obrazovky `RaceOptionsScreen`, která byla odstraněna. Režimy 1v1 / The Pack / Tournament / Time Trial, počet kol z výběru tratě a nastavení převodovky/ABS z Options zůstávají funkční.

Po dokončení závodu se objeví **originální `FRONTEND/ART/CHECK/FINMENU.QFS`**, nikoliv improvizované výsledky na splash pozadí. Menu nabízí Control Central, Replay, Highlight Reel, Game Options, Game Standings a Chat Mode. Jsou zachovány originální rámečky a aktivní grafické varianty. Podrobný přehled času a umístění je na `STANDTRK.QFS` (okruh) nebo `STANDSEG.QFS` (otevřená silnice). Výsledek se po dojezdu „zmrazí“, takže po otevření tabulky dál nenarůstá čas.

- **N** pokračuje v další části závodu/turnaje nebo spustí nový okruh podle režimu.
- **S** otevře autosave/manuální uložení rozjetého turnaje.
- **Esc/M** vrací do Control Central.
- **Game Standings** zobrazí původní obrazovku a Done ji zavře.
- Video Highlight Reel, Chat a skutečná statistika rekordních rychlostí zatím nejsou implementované a nebudou vykazovat falešná data.

## Testy a limity

GitHub Actions na Windows ověřuje build/publish, ISO import, finish gate, závodní režimy, kolize, turnajové checkpointy, showroom, škálování vstupu a nyní i matematiku zobrazení mapy.

**Co automatické testy neověřují:** vizuální správnost vykreslování všech QFS vrstev, kvalitu dobové mapy oproti DOS originálu a chování menu při skutečné jízdě. To musí potvrdit interaktivní test hry s původními daty.

Další krok ke stoprocentní věrnosti: původní systém HUD číselných fontů a vzdáleností, track records, původní statistické žebříčky, highlight replay a další závodní režimy.
