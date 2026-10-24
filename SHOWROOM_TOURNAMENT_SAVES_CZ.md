# OpenNFS1 SE 1.6 – původní showroom aut a ukládání turnaje

Tato etapa používá pouze grafické soubory **z originálního CD hráče**. Do veřejného repozitáře nejsou přidány originální QFS, filmy TGV, zvuk ani původní EXE.

## 1. Proklikávací auta / Showroom

V **Control Central** klikni do prostřední části fotografie auta, nebo vyber její panel klávesnicí a stiskni Enter. Případně stiskni **I**. Okrajové proužky fotografie dál slouží k přepínání vozidel.

Otevře se původní výstavní obrazovka auta z `FRONTEND/SHOW` (640×480). Jednotlivé bloky jsou vykreslené podle pozic bitmap vložených přímo v originálních QFS:

- **General** – původní `*GEN.QFS` s dvojicí prohlídek/fotografií, pět variant `f1s0..4`, `f2s0..4`, nativní šipky
- **History** – původní `*HIS.QFS` včetně textu/grafiky v pozadí
- **Mechanical** – původní `*MEC.QFS`, varianty `chas`, `engn` a `driv`
- **Performance** – původní `*PER.QFS`, varianty `acc1`, `bra1`, `han1` a doplňující obrázky
- **Video** – originální videa existují jako `FRONTEND/MOVIE/*.TGV`, **přehrávač TGV zatím není implementován**; nabídka toto jasně oznámí

### Originální názvy souborů

| Auto | Základní QFS | Detailní prefix |
| --- | --- | --- |
| Mazda RX-7 | `RX7` | `RX7` |
| Honda NSX | `NSX` | `NSX` |
| Toyota Supra | `SUPRA` | `SUP` |
| Porsche 911 | `911` | `911` |
| Corvette ZR-1 | `VETTE` | `ZR1` |
| Dodge Viper | `VIPER` | `VIP` |
| Ferrari F512 | `512TR` | `512` |
| Lamborghini Diablo | `DIABLO` | `DIA` |

Zkontrolována **existence všech 40 QFS souborů** přímo v dodaném `nfs.zip` (osm vozů × pět obrazovek). Warrior nemá originální showroom; při jeho výběru se zatím zobrazí RX-7.

**Ovládání:** kliknutí na originální ovládací prvky, šipky ←/→, Enter, Esc. V základním showroomu mění ←/→ auto, v detailu přepínají obrázky. Esc vrací do předchozí obrazovky. Na hlavní nabídce showroomu klávesy G/H/M/P otevřou General/History/Mechanical/Performance.

## 2. Ukládání a načítání turnaje

Menu **Options → Load/Save** otevírá původní `FRONTEND/ART/OPTION/LOADSAVE.QFS` (pozadí a levé volby `savd`, `load`, `dlgd`, `dond`). Ukládá se pouze rekonstruovaný stav našeho enginu — ne originální herní data.

- **Autosave (slot 0):** vzniká při zahájení turnaje a po každé dokončené etapě / závodě
- **Manuální sloty 1–3:** na obrazovce výsledků stiskni **S** (nebo klikni „Save Tournament“), potom vyber slot a potvrď Save
- **Obnovení:** Control Central → Options → Load/Save → vyber autosave nebo manuální slot → Load; závod se spustí od uložené etapy
- **Smazání:** vyber slot a dvakrát stiskni D, nebo dvakrát klikni Delete
- **Ovládání slotů:** ←/→ nebo ↑/↓; S pro Save, L/Enter pro Load, D pro Delete, Esc/Done pro návrat

**Cesta k uložení:** `%LOCALAPPDATA%\OpenNFS1\TournamentSaves\`. Obsahuje `autosave.json` a `slot1.json` až `slot3.json`. Jednotlivé soubory se zapisují nejprve do dočasného souboru a až potom atomicky nahradí původní uloženou hru.

Ukládá se: výchozí trať a rozpracovaná etapa, pořadí turnaje, dosavadní body, čas, jednotlivé již projeté úseky otevřené silnice, zvolený vůz a základní nastavení. Uložení z výsledkové obrazovky zachytí **příští závod**, nikoliv už dokončenou obrazovku výsledků. To zamezuje opětovnému připsání bodů po obnovení hry. Před obnovením je stav zkontrolován (podporovaná verze souboru, správné tratě, pořadí, rozsahy čísel a dostupný vůz).

### Omezení
- Ukládá se **postup v turnaji mezi závody/etapami**, nikoli pozice auta uprostřed jízdy
- Neexistuje zatím uživatelský profil ani původní tabulky soupeřů; používá se dříve zavedené provizorní bodování
- Další historické akce z `LOADSAVE.QFS` (žebříčky, záznamy jízd) zatím nejsou implementované
- Originální TGV videa v showroomu nejsou přehratelná
- CI testuje .NET 8 build a kontrolu obnovení turnaje včetně rozpracované Alpine (1. segment → uložit → načíst → 2. segment)
- **Pixelově přesný výsledek a skutečné uložení/načtení v běžící Windows hře musí být ještě ověřeny uživatelem**

## Další logický vývoj
Originální automobilová videa TGV, interaktivní technické prezentace a komentář, původní `GRAPHICS.QFS` a `CNTRLSET.QFS`, kompletní výsledkové tabulky a ukládání profilů.
