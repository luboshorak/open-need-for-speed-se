# OpenNFS1 – opravy modelů vozidel a menu (10/2026)

## Pád při vytvoření auta

V původním ORIP/CFM parseru `MeshChunk.cs` se při přeskakování nepodporovaného polygonu ztratilo jeho původní pořadové číslo. Popisky polygonů (včetně `lt_frnt`, `rt_frnt`, `lt_rear`, `rt_rear`) se pak přiřazovaly podle zkráceného indexu. Parser nyní drží seznam původních indexů a podporuje také 8znakové a mezerami doplněné názvy.

Pokud konkrétní model opravdu neobsahuje jeden nebo více těchto polygonů, `CarMesh` nejprve zkusí zrcadlit polohu protějšího kola. Když chybí celé nápravy, dopočítá je přibližně z hranic modelu. Totéž platí pro průměr kola.

**Přibližná geometrie je nouzové řešení.** Při jejím použití se vedle EXE zapíše `model-warnings.log`, který obsahuje identifikátor modelu a dostupné popisky. Tento log pomůže rozlišit odlišnou verzi CFM od další chyby parseru.

## Ovládání myší

- Control Central: klikací auto, soupeř, trať, režim a tlačítka.
- Opponent: nyní vybírá **konkrétní soupeřovo auto** a zobrazuje jeho obrázek ze skutečného QFS na vlastním CD. První AI protivník používá tento model.
- Race Options: myš může změnit převodovku, denní dobu, počet soupeřů (0–10) a spustit závod; Esc vrací do hlavního menu.
- Pause a výsledky závodu: klikací volby spolu s původní klávesnicí.
- Race Type: současný přepínač Single Race / Time Trial (není úplnou implementací všech režimů původního NFS SE).

## Zbývající originální grafika

Původní OpenNFS1 neimplementoval kompletní obrazovky Race Type a Opponent. Náš fork obsahuje jejich ovládací logiku, ale ne všechny originální grafické prvky, takže **zatím netvrdíme plnou věrnost menu původní hry**.

Po spuštění hlavního menu vznikne vedle EXE soubor `ui-assets.txt`. Jde o seznam **názvů, rozměrů a umístění bitmap v CENTRAL.QFS** plus souborů v `FRONTEND/ART/CONTROL`; žádná data CD se nekopírují na GitHub. Podle tohoto seznamu lze zjistit, které skutečné původní textury máme ještě načítat. Pro pokročilejší diagnostiku lze dodat přímo soubor `CENTRAL.QFS` z vlastního CD.

## Ověření

GitHub Actions na Windows ověřuje .NET 8 build a publish. Běh s reálným CFM modelem musí potvrdit testování ve Windows s originálními daty, která v repozitáři nejsou. Pokud se objeví další pád, pošli stack trace a případný `model-warnings.log`.
