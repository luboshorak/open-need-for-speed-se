# OpenNFS1 SE Reloaded 1.10.1 — zpátečka a oprava policejního řízení

## Jak couvat (oprava zaseknutí při kolizi)

### Automatická převodovka
- **Šipka nahoru / pravá spoušť gamepadu:** akcelerace vpřed.
- **Šipka dolů / levá spoušť:** za jízdy vpřed brzda, po zastavení automat zařadí **1 → N → R** (se standardní prodlevou převodovky) a při dalším držení dolů začne couvat.
- Při couvání **šipka nahoru** nejdřív brzdí. Po zastavení postupně přepne **R → N → 1**; pokračuj držením nahoru a auto se rozjede vpřed.
- Mezi směry nelze bezpečně přeřadit v rychlosti: nejprve se musí auto zastavit.
- **R** stále zůstává existujícím nouzovým resetem vozidla, pokud se nepovede vyprostit z extrémní situace.

### Manuální převodovka
- **Z** = řazení dolů, **A** = řazení nahoru. Ze stojící 1. rychlosti může být potřeba dvakrát Z (1 → N → R).
- Se zařazenou **R** se couvá držením **šipky dolů**; nahoru v R brzdí.
- Se zařazenou **1+** se vpřed jede šipkou nahoru a dolů brzdí.
- Automatický výběr R tlačítkem dolů se záměrně nepoužije, když uživatel zapnul manuální řazení.

### Co bylo rozbité
`VehicleController.Brake` původně mapoval dolní šipku výhradně na brzdu. Převodovka sice měla R (index −1) a motor záporný převodový poměr, ale automat ji nikdy nezařadil. Kolizní resolver navíc používal `Math.Max(0, speed)` a ničil již dosaženou zápornou rychlost. Oprava má čistou/testovatelnou `ReverseDriveControl`, mapování pedálů u `PlayerDriver`, korektní zbrzdění záporné rychlosti na nulu a zachování směru při kolizi.

## Policie — co bylo prověřeno přímo v originální hře

V původním `NFS_DOS.EXE` jsme ověřili statické reference na **CopMust, CopCamaro, CopCaprice, CopVic**, na `ticket.qfs` a na animace `cop%d.tgv`/`gcop%d.tgv`. Uživatelův archiv skutečně obsahuje `SIMDATA/CARFAMS/COPMUST.CFM`, `FRONTEND/ART/TICKET.QFS`, `SIMDATA/MISC/TICKET[DR][HL].QFS` a filmy `COP1–3.TGV`, `GCOP1–3.TGV`.

To dokládá širší původní systém policejních kontaktů, ale **nikoli jeho algoritmus řízení**. Ten je v historickém DOS EXE (Watcom/4GW LE) a pro doslovný přenos by potřeboval reverzní analýzu strojového kódu, včetně identifikace AI funkcí. Tento patch tedy nepředstírá, že jeho navržené rychlosti či jízdní pruhy přesně odpovídají originálu.

## První systematická oprava policejní AI

Dosavadní `PoliceDriver` odvozoval řízení od obecné civilní `TrafficDriver`, která používá hrubé hodnoty volantu a při chybě větší než 90° rovnou přepisuje směr auta. Současně policie okamžitě nastavovala cílovou rychlost a často narážela do pomalých aut.

Nový driver:
- sleduje bod několik silničních uzlů **před autem**, ne jen jeho aktuální pozici;
- řídí volant **proporcionálně podle úhlu**, bez okamžitého otočení auta;
- používá pozvolný náběh a snižování rychlosti, výrazně zpomalí přímo za hráčem (aby ho jen netlačil);
- vyhodnocuje provoz v aktuálním a sousedních jízdních pruzích, hledá volný pruh a **2,4 s** potlačuje rychlé změny směru;
- když není kudy bezpečně předjet, zpomalí za překážkou;
- drží se části silnice určené pro provoz vpřed, aby se vyhnul riskantním skokům do protisměru.

Dřívější `PolicePursuitLogic` (detekce rychlosti, zastavení, pokuta, cooldown) zůstává zachována. Běží pouze v konfiguracích, ve kterých už existoval provoz a policejní pronásledování.

### Omezení a testování
Automatické testy kontrolují přechody automatické i manuální převodovky, obě fáze couvání, bezpečné brzdy, rychlostní odstupy pronásledování a rozhodnutí při blokovaném pruhu. Windows GitHub Actions kontroluje kompilaci a build distribučního EXE.

Automatická CI **nesimuluje živou 3D silnici**, takže finální ladění volantu, překážek, kolizí a policejní agresivity vyžaduje závod s původními daty ve Windows. Původní sirény, policejní roadblocky, různé typy pronásledování a originální koncové animace ještě nejsou kompletně rekonstruovány.
