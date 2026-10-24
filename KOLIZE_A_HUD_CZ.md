# OpenNFS1 – kolize vozidel a původní HUD (10/2026)

## Kolizní model
Ve starém enginu se řešily jen kolize auta s okraji silnice. `Race.Update()` neprováděl žádné testy střetů s ostatními auty a `RacingAIDriver` dokonce výslovně ignoroval hráče.

Nový `VehicleCollisionResolver` běží po aktualizaci všech řidičů, kontroluje páry **závodních i dopravních** vozidel:
- OBB (oriented bounding box) v rovině X/Z podle rozměrů původních CFM modelů a směru jízdy,
- vynechává auta na rozdílných výškových úrovních (nadjezdy),
- počítá nejmenší potřebné oddělení,
- jednoduchým impulsem s přibližnými hmotnostmi mění rychlost při nárazu,
- vyvolá malou rotaci u říditelných vozidel při bočním střetu.
- AI nově při rozhodování o předjíždění a zpomalování zohledňuje i hráče.

Čistá geometrie je oddělená do `Physics/CollisionFootprint.cs`, aby šla testovat bez grafického enginu. `tests/VehicleCollisionTests` ověřuje nezávislé pruhy, podélný kontakt, boční kontakt a minimální oddělení.

**Omezení:** první hratelná verze má diskrétní kontakt za snímek a arkádovou odezvu, nikoli úplnou fyzikální simulaci poškození ani CCD (continuous collision detection). Velmi vysoké rychlosti a prudké skoky přes celou délku auta mohou stále vyžadovat další úpravy. Chování je potřeba vyzkoušet se skutečnými CFM auty a provozem.

## HUD a grafika

Analýza originálního `SIMDATA/MISC/MASKHI.FSH` z dodaných uživatelských CD dat:

| Bitmapa | Původní rozměr/pozice | Použití |
| --- | --- | --- |
| `tach` | 156×156, (10,289) | skutečný původní otáčkoměr |
| `red0`–`red9` | 28×27 | původní červené číslice pro rychlost |
| `lap#` | 109×24, (433,1) | původní grafika počítadla kol |
| `0002` | 32×32 | dlaždice spodní lišty |

- Původní kód vykresloval celou spodní lištu přes 32×32 texturu a **překrýval otáčkoměr**, přestože jeho původní spodní hrana leží až na Y=445.
- Spodní lišta se nyní vykresluje první, přes vzorkování `PointWrap` a jako dlaždice, otáčkoměr a ručička až přes ni.
- Otáčkoměr má konečně původní velikost, ne natvrdo nastavených 160×160.
- Stavová lišta má zarovnané údaje a používá původní číslice rychlosti a motiv `lap#`. Čas, zařazený stupeň a pozice stále používají MonoGame font, dokud nebude hotový kompletní dobový HUD s veškerou grafikou.

Původní copyrightovaná herní data nebyla a nemají být committována na GitHub. Herní sprite bitmapy se načítají přímo z hráčova `CD_Data`.

## Testovací scénáře
1. Single Race, 10 AI, Autumn Valley: dojet za soupeřem a narazit do něj zezadu, zkusit i boční kontakt; nemá projet skrz.
2. Open road se silničním provozem: střet s dopravním autem, oddělené nadjezdy nesmí vyvolat falešnou kolizi.
3. Chase camera: otáčkoměr musí být zobrazen celý (dolní část pod Y=400) a čísla nesmí přesahovat.
4. Změnit pohled do kokpitu a zpět, případně změnit vůz; ověřit, že HUD neruší palubní desku.
5. Opakovaně spouštět a dokončovat závod, zkusit více rozlišení.

Windows CI ověřuje kompilaci, import ISO, finish gate, turnaje a kolizní matematiku. Běh na Windows s originálními daty je stále potřeba pro potvrzení vzhledu a fyzického chování.
