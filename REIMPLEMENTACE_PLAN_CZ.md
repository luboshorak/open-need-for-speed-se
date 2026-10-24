# OpenNFS1 SE – plán dokončení hry

Tento fork staví na MonoGame DesktopGL (.NET 8) a používá **originální CD data dodaná hráčem**. Binární data, bitmapy, hudba ani původní EXE nejsou a nebudou přidávány do veřejného repozitáře.

## Dokončeno v této etapě

### Originální Game Mode

Nahrávají se sprite bitmapy z `FRONTEND/ART/CONTROL/GAMEMODE.QFS`, v originálních rozměrech a souřadnicích:

| ID | Rozlišení | Pozice | Funkce |
| --- | --- | --- | --- |
| `bgnd` | 640 × 480 | (0,0) | Pozadí menu |
| `sind / sinl` | 272 × 29 | (178,133) | Single Player → Control Central |
| `modd / modl` | 253 × 32 | (189,207) | Modem/Serial – zatím nedostupné |
| `netd / netl` | 170 × 30 | (232,281) | Network – zatím nedostupné |
| `exid / exil` | 83 × 30 | (274,355) | Exit |

Zdroj: rozbor indexu FSH z originálního `GAMEMODE.QFS` uživatele. Existuje také `GAMEMOD2.QFS` s mírně odlišným rozložením a bez tlačítka Network; pro první fázi používáme plnou verzi `GAMEMODE.QFS`.

Ovládání: myš (kliknutí na původní aktivní oblast), Up/Down/Tab, Enter/Space, Esc. Všechny funkční obrazovky jsou přístupné bez nutnosti spuštění závodu. Základní průchod: **Splash → Game Mode → Control Central → Race Options → Race → Results**. Po importu ISO přejde hráč rovněž do Game Mode.

### Tournament – první funkční implementace

Panel Race Type nyní přepíná **Single Race / Tournament / Time Trial**. První dva používají skutečné `SINGLE.QFS` a `TOURNY.QFS` (238 × 66, pozice 92,316). Time Trial zůstává jasně označenou novou volbou.

Turnaj:
- Skládá se z existujících podporovaných okruhů, začíná zvolenou tratí a pokračuje přes ostatní okruhy v pořadí zdrojového seznamu (s otočením pořadí podle startovní trati).
- Nevkládá skryté tratě ani otevřené silniční etapy.
- Po výsledcích nabídne **Next tournament race** nebo **Finish tournament**; automaticky nastaví další trať a načte ji.
- Pamatuje si absolvované etapy, jednoduché body za umístění (10/9/8/…/1) a součet jízdních časů.
- Vytvoření nové série se děje až po potvrzení Race Options.

**Pozor:** Jedná se o první skutečně hratelnou turnajovou smyčku, nikoli přesnou kopii bodovacích pravidel původního NFS SE. Nejsou zatím implementováni ostatní závodníci v souhrnné tabulce mistrovství, ukládání rozehrané série ani ceny.

## Další vývojové bloky

1. **Celá původní UI navigace:** podrobný průzkum `FRONTEND/ART/CHECK/*`, obnovení původních nastavení, výsledků, obrazovek vozidel, výběru závodů, ověřený layout a hitboxy.
2. **Pravidla závodních režimů:** `NOMERCY.QFS`, `HEADHEAD.QFS`, turnajové varianty `TOURNY1/2/3.QFS`, pořadí protivníků, progres, odměny, autentické skórování.
3. **Hratelnost:** AI, průjezd checkpoints a finish gate pro všechny dodané tratě, kolize, přehledná HUD/minimapa, ovládání myš/klávesnice/gamepad.
4. **Audio/video a nastavení:** původní hudba a efekty načítané z lokálního CD, ovládání hlasitostí, zvukové formáty, časy a animace.
5. **Uložení stavu:** lokální profil, nastavení, rekordy, průběh turnaje a save/load.
6. **Multiplayer:** samostatný návrh moderního síťového režimu. Historický Modem/Serial se zatím nesmí tvářit jako funkční.

Všechny etapy se mají doprovázet testy ve Windows GitHub Actions a praktickým testem s originálními daty. Automatický build sám neprokazuje grafickou věrnost ani funkčnost závodu.
