# OpenNFS1 – obnova originálního UI a dojezdu závodu

## Co je doložené v originálních souborech NFS SE

Podkladem je struktura vlastního CD uživatele (bez přidávání originálních dat do repozitáře). Kontrolní soubory pocházejí z `FRONTEND/ART/CONTROL`:

| Zdroj | FSH položka | Velikost a původní pozice | Účel |
| --- | --- | --- | --- |
| `CENTRAL.QFS` | `bgnd` | 640×480, (0,0) | Control Central – pozadí |
| `CENTRAL.QFS` | `Tlb1` | 313×170, (19,112) | rámeček vybraného auta |
| `CENTRAL.QFS` | `Trb3` | 288×121, (339,97) | rámeček soupeře |
| `CENTRAL.QFS` | `Blb2` | 242×70, (90,314) | rámeček typu závodu |
| `CENTRAL.QFS` | `Brb4` | 212×131, (339,253) | rámeček tratě |
| `CENTRAL.QFS` | `Coml` | 162×18, (342,219) | Computer Opponents |
| `*1.QFS` | `0000` | 309×166, (21,114) | plná fotografie hráčova auta |
| `*2.QFS` | `0000` | 284×117, (341,99) | fotografie auta soupeře |
| `SINGLE.QFS` | `0000` | 238×66, (92,316) | Single Race, původní menu |

Zdrojové soubory `RX71/RX72`, `SUP1/SUP2`, `DIA1/DIA2` atd. existují v originální podobě pro jednotlivá auta. Původně fork mylně zmenšoval fotografii hráče do bílého prostoru soupeře a nahrazoval závodní typ vlastním textem. Nyní se obrázky načítají v jejich původní velikosti a pozici.

`GAMEMODE.QFS` a `GAMEMOD2.QFS` obsahují samostatnou obrazovku Single Player / Network / Modem. `NOMERCY.QFS` a `TOURNY.QFS` obsahují další grafické varianty typu závodu. Tyto režimy ale herní engine zatím neimplementuje. Současný **Time Trial** je moderní doplněk, pro který nemáme odpovídající původní texturu; nepředstíráme, že jde o původní obrazovku.

## Dokončování závodu

- Původní logika vyžadovala `CurrentNode.Number == CheckpointNode`. Auto může během snímku překročit více uzlů a přesnou rovnost nikdy netrefit.
- Nový `RaceGateTracker` porovnává předchozí a současný uzel a testuje **překročení cílové hranice**, ne rovnost.
- Okruhová trať musí nejdříve projet přes polovinu okruhu, aby se nedalo zrychleně dokončit couváním od cílové čáry.
- Po dokončení přechází závod přímo k výsledkům, bez čekání na přesnou nulovou rychlost (která u dřívějšího modelu nemusela nastat).
- Po návratu do menu se obnoví viewport 640×480, zatímco závod používá scénu 640×400.
- V závodě **Esc = pauza**, **M = návrat do hlavní nabídky**. Na výsledkové obrazovce funguje Esc a kliknutí.
- Okruhová trať měla původně vždy 3 kola. Nově lze v Race Settings zvolit **1–5 kol**, vhodné pro rychlý test cílové čáry.

## Ověření

Na Windows běží CI: kompilace a publish, test importu ISO a čistě datový test `RaceGateTracker` (3 okruhy, přeskočené uzly, konec otevřené etapy, couvání). Plné interaktivní dojetí v MonoGame s originálními daty zatím vyžaduje test na Windows.

Originální herní soubory a grafika **nejsou součástí pull requestu** a nemají být přidány do veřejného repozitáře.
