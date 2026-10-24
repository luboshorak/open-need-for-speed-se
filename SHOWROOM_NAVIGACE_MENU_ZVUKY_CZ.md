# OpenNFS1 SE Reloaded 1.6.1 – opravy showroomu, zvuků a prezentace projektu

## Ovládání showroomu

Původní obrazovky používají **dva různé typy šipek**, které předchozí implementace zaměňovala:

| Prvek | Původní bitmapa | Funkce |
| --- | --- | --- |
| Spodní levá šipka | `pred / prel` (221, 437) | Předchozí **auto** |
| Spodní pravá šipka | `nexd / nexl` (375, 438) | Další **auto** |
| General: velký obrázek | `f1lf / f1rt` (199/593, 319) | Předchozí/další **velká fotografie** |
| General: malý obrázek | `f2lf / f2rt` (376/582, 160) | Předchozí/další **malá fotografie** |
| Mechanical | `left / righ` (18/600, 236) | Podvozek / motor / pohon |
| Performance | `left / righ` (40/580, 207) | Akcelerace / brzdění / ovladatelnost |

Spodní šipky tedy **vždy přepínají vůz**, i pokud je zobrazena History, Mechanical, General nebo Performance. Detailní stránka zůstane otevřená a její varianta se při změně auta vrátí na první obrázek.

Dvojice fotografií General je nezávislá: jedna šipka nemá měnit současně oba snímky.

Klávesnice: **←/→** vždy předchozí/další auto; **A/D** mění spodní velký obrázek nebo právě vybraný diagram, **W/S** mění malý horní obrázek. **Esc** přejde z detailu na přehled auta, z přehledu do Control Central.

Automatický test `tests/ShowroomNavigationTests` kontroluje výběr auta, nezávislé varianty obou fotografií, přepínání nákresů a návrat. Přístup ke všem souborům QFS zůstává přímo z originálního CD.

## Zvuky menu (nikoli hudba)

Hudební skladby `ROCK/TECHNO` a `SHOWCASE` byly funkční a jejich přehrávač se nemění.

V originálním `SIMDATA/SOUNDBNK/FRONT_MW.BNK` jsou **čtyři** zvukové vzorky. Dva nejkratší (16 kHz, délky cca 64 ms a 128 ms) nyní slouží jako:
- jemné kliknutí při navigaci šipkami a tabulátorem,
- potvrzovací kliknutí při stisku Enter, Esc, mezerníku nebo uvolnění levého tlačítka myši.

Zvuky přehrává MonoGame přímo ze stávajícího BNK parseru. **Žádný FFmpeg není potřeba** a žádný původní zvuk se neukládá na GitHub. Audio je volitelné a řídí ho položka **Audio → Sound Effects**.

## Kredity

Branding README, metadata Windows EXE a text v Options uvádějí na prvním místě současnou větev **OpenNFS1 SE Reloaded** a její vývoj. Podrobná historie výchozího kódu a poděkování původnímu OpenNFS1 autorovi Jeffu Harrisovi / 1am Studios jsou v `CREDITS.md`, kde zůstává i právní upozornění na původní EA data.

Neodstraňujeme původní Git historii, informace o zdrojovém kódu ani nepřepisujeme neznámá licenční oprávnění.

## Kontrola

Windows GitHub Actions provádí standardní sestavení, publish, stávající testy a nový test showroom navigace. Teprve praktický test s původními CD daty ověří, zda jsou všechny původní galerie správně poskládané a zda hlasitost klikání odpovídá původnímu menu.
