# OpenNFS1 SE Reloaded 1.10 – opravy rekordů, segmenty, barvy a první policejní honička

## 1. Pád při přepnutí Best Times

Dřívější QFS dekodér vyhazoval neodchycenou `InvalidDataException`, když originální rekordový archiv měl kompresní metodu mimo dva rozpoznané typy. Přepnutí tratě spustilo konstruktor `TrackBestTimesScreen`, který výjimku propustil do celé herní smyčky.

Nyní:
- `QfsFile` přijímá také archiv začínající nezkomprimovanou hlavičkou `SHPI`.
- `TrackBestTimesScreen` bezpečně zachytí neznámý/poškozený archiv a vypíše příčinu do diagnostiky. Pokud lze otevřít odpovídající původní `*SEL.QFS`, zobrazí tuto grafiku jako nouzový podklad, jinak zůstane použitelná černá obrazovka se skutečnými rekordy a možností návratu.
- Neznámý archiv se **nevydává za správně dekódovaný**. Abychom případně doplnili další kompresi, potřebujeme přesné první bajty vadného QFS. Hra však kvůli němu nesmí spadnout.

## 2. Skutečné segmenty otevřených tratí

Předchozí `ChangePhoto()` pouze cyklila obrázky `bsg1–3`/ `ssg1–3`. Zvolená `TrackDescription` a TRI soubor závodu se neměnily.

Nově `OpenRoadStageSelector` rozpoznává původní trojice `AL1.TRI`/`AL2.TRI`/`AL3.TRI`, `CL1–3.TRI`, `CY1–3.TRI`. Obrázky, aktuální `SelectedTrackDescription` a spuštěná etapa jsou vzájemně synchronizované.

- **A/D**, **Page Up/Page Down** nebo **1/2/3** přepínají úseky.
- V detailu lze kliknout do levé/pravé poloviny fotografie otevřené silnice.
- Původní spodní šipky dál mění **celou trať** (Alpine vs Coastal vs City), nikoli její segment.
- Control Central si po návratu zachová zvolenou etapu a tlačítko **Race** ji nepřepíše na první část.
- Opravena i stará chyba startu závodu, který dovoloval maximálně pět kol přestože nastavení a ukládání již podporovalo 1–12.

## 3. Sytost a výsledné barvy

**F7** aktivuje/deaktivuje nový volitelný profil **VGA Vivid** (gamma 0,90, contrast 1,14, saturation 1,18, brightness −0,015). Je zapnutý jako nový výchozí profil, protože současná reprodukce vypadala oproti referenčnímu DOS screenshotu bledě.

**F8** vrátí **Raw Original RGB** a vypne celý barevný shader. Pokročilé F9 Graphics Detail dál umožňuje vlastní gamma/jas/kontrast. Nové nastavení `vividColors` je uloženo vedle `originalColors` v `settings.json`.

**Důležité:** VGA Vivid je vizuální preset, **nikoli doložená pixelově přesná barevná emulace** DAC/gamma monitoru z roku 1996. Díky F7/F8 lze na stejném monitoru okamžitě porovnat zdrojovou paletu s upraveným výstupem a posoudit, zda jsou chyby případně v samotném rendereru textur.

## 4. Profil hráče

Při zadávání jména je nyní upřednostněná originální velká grafika **`FRONTEND/ART/TITLE.QFS`**, s bezpečným návratem ke `GAMEMODE.QFS` při chybějícím bitmapovém ID/kompresi. Vlastní textová oblast je menší a nechává vyniknout originální pozadí.

- Původní uložené jméno se načte pro editaci.
- **F6** na originální obrazovce Game Mode umožní upravit hráčské jméno, i když už bylo jednou nastavené.
- Jméno zůstává v `%LOCALAPPDATA%\OpenNFS1\player.json`, výsledky zůstávají přiřazené jménu v době závodu.

**Omezení:** Základní textový vstup stále používá MonoGame a nejde o přesnou rekonstrukci původního DOS dialogu. Nezískali jsme ještě jednoznačně jeho historické rozložení a originální systém aktivních polí; není fér tvrdit, že je stoprocentně zrekonstruovaný.

## 5. Policie na otevřených silnicích

`SIMDATA/CARFAMS/COPMUST.CFM` v originálním archivu skutečně existuje. Dřív se Mustang náhodně losoval mezi **běžný provoz**, takže se choval jako civilní auto.

Nově:
- Oddělený `PoliceDriver`, který sleduje hráčovu pozici na trati, zrychluje podle odstupu a reaguje na limity úseku.
- `PolicePursuitLogic` vyvolá první hlídku při jízdě rychleji než 110 km/h po dobu alespoň 3,5 sekundy.
- Policie je zatím zapojená v **Head to Head** na otevřených silnicích bez **No Mercy**, kde se podle stávající logiky vytváří provoz.
- Při zastavení pod 12 km/h v blízkosti pronásledujícího Mustangu po dobu alespoň 2,5 sekundy je vydána pokuta; přeruší se závodní obrazovka a zobrazí se původní **`FRONTEND/ART/TICKET.QFS`**, pokud jej současný QFS dekodér umí otevřít. Enter/Esc vrací hráče do závodu a obnoví zvuk motoru.
- Následuje prodleva 25 sekund před další možnou honičkou. Auta se odstraňují z `Race.Drivers`, pokud skončí úsek nebo ujedou příliš daleko.
- Policie má běžný fyzikální kontakt přes existující `VehicleCollisionResolver`.

**Omezení:** Jde o první hratelnou rekonstrukci stíhání, ne pixelově/behaviorálně identickou policejní AI originálu. Prozatím chybí ověřená siréna, přesné policejní manévry a různé typy pokut/zatčení. Není vyzkoušená v interaktivní jízdě na Windows a potřebuje nastavit rychlosti a vzdálenosti podle reálných testů.

## Ověření

Windows CI: .NET 8 MonoGame build, publikování a stávající testy plus testy skutečných rodin AL/CL/CY, detekce dlouhodobého překročení rychlosti, předání pokuty a cooldownu. Chování na skutečném CD a grafické podání zůstávají k vizuálnímu otestování uživatelem. Komerční soubory EA nejsou nahrány do GitHubu.
