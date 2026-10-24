# OpenNFS1 SE Reloaded 1.9.0 — původní barvy, rekordy, profil hráče a detaily tratí

## Co bylo špatně

Po srovnání snímků DOS originálu a nového OpenNFS1 se ukázalo, že původní 6bitová DOS paleta je ze zdrojových QFS dekódovaná poměrně věrně. Například neutrální šedá z `VERTSEL.QFS` má v originální paletě hodnoty přibližně RGB 120/120/120 a v uživatelově původním DOS screenshotu zhruba stejně. Screenshot remake ukazoval podstatně světlejší obraz (~180). Příčinou může být uložená silnější gamma/brightness korekce; bez uživatelova `settings.json` nemůžeme přesně určit konkrétní hodnoty.

### Original Colors

- Při výchozím nastavení se originální obrázek již **neposílá přes barevný shader**. Zachovává se skutečná zdrojová paleta a korekce není ve výchozím režimu aplikovaná.
- **F8** kdykoliv vrátí originální paletu (gamma 1.0, brightness 0.0, contrast 1.0) a nastavení uloží.
- Moderní Graphics / F9 má stále volitelné přizpůsobení gammy, jasu a kontrastu. Teprve změna těchto položek zapne post-processing; Reset přepne na Original Colors.
- Opraven shader, který předtím napevno používal bilineární filtr dokonce i při zvoleném Sharp či Integer upscale. Nyní respektuje filtr zvolený v SpriteBatch.

## Opravené detaily tratí

- Byla odstraněna falešná kresba `bsg1..3` pro okruhové tratě. Jejich originální barevná mapa je už součástí `bgnd`.
- Pro původní Vertigo Ridge zobrazujeme ověřenou délku **3.0 MILES** a počet kol. Původní DOS screenshot zobrazuje **6 kol**, proto se nové instalace spouštějí s 6 koly (1–12 lze navolit klávesou L nebo kliknutím na číslo). Výběr kol se nyní ukládá v uživatelském nastavení a funguje při obnovení turnaje.
- Pro ostatní tratě zatím nepředstíráme neověřené hodnoty délky. Jejich kalibrace podle geometrií TRI zůstává další práce.
- Odstraněný žlutý pomocný text překrývající původní spodní navigaci.
- Původní tlačítka Prev/Next/Done, Midday/Morning a Best Times jsou aktivní. Opravené kliknutí na překryté varianty Midday/Morning.
- Skrytá bonusová trať Oasis Springs nemá `*SEL.QFS`; proto se nezobrazuje ve standardním výběru a neotevírá prázdnou šedou obrazovku.

## Originální Best Times v QFS

Původní `FRONTEND/ART/TRACKSEL/*BST.QFS` **nepoužívají RefPack 10FB**, ale EA **32FB Huffman s filtrem**. Starý dekodér taková data vůbec neznal. Nový `EaHuffmanDecoder` implementuje Huffman codebook, rozbalení a zpětný delta filtr. Otestován soukromě na skutečném `VERTBST.QFS` z uživatelova originálního CD: rozbalí se platný FSH (`SHPI`) se správným počtem 10 spritů. Automatický Windows test používá výhradně syntetický, veřejně vytvořený testovací obsah (nešíří EA data).

Best Times přehrává originální `*BST.QFS` pozadí a vykresluje vlastní žebříček z opravdu dokončených závodů. Navigační šipky přepínají tratě, **Description** se vrací do původních detailů. Dokud hráč nic nezajede, obrazovka ukáže „NO RECORDS YET“ — nesimulujeme neexistující původní rekordy.

## Hráčský profil a skutečné rekordy

- Hra se po prvním importu CD / prvním spuštění zeptá na **hráčské jméno**. Povoluje do 16 znaků (A–Z, 0–9, mezery, pomlčky), Backspace a Enter.
- Další spuštění automaticky načtou uložené jméno z `%LOCALAPPDATA%\OpenNFS1\player.json`.
- Při dokončení závodu se pod aktuálním jménem uloží skutečný čas, vybrané auto, režim, trať a naměřená nejvyšší rychlost do `%LOCALAPPDATA%\OpenNFS1\track-records.json`.
- Best Times zobrazuje nejlepších 10 skutečných časů a rychlostní rekord dané tratě. Neukládáme data z CD ani do instalačního adresáře; soubory ukládáme odděleně od originálních dat.
- Zatím nejde o stoprocentní klon datové struktury DOS profilu ani o import originálních historických leaderboardů. Také dosud nemáme přesný výpočet mílí pro všechny tratě.

## Ověření

Windows GitHub Actions: sestavení .NET 8, MonoGame obsah, publish Windows, existující testy (import, kolize, turnaj, race mode, HUD, škálování), nový syntetický test Huffman 32FB a normalizace jmen. Práce přímo s originálními QFS je ověřena v lokálním výzkumném rozboru, ale konečné zobrazení screenshotů ve Windows, s konkrétní GPU a nastavením obrazovky, potřebuje ruční kontrolu. Originální komerční herní data nejsou vložena do GitHub repozitáře.
