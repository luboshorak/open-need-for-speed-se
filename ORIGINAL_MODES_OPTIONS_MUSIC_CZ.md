# OpenNFS1 SE 1.5 – původní režimy, menu Options a hudba

Tato etapa je založena na **originálních souborech z nfs.zip od uživatele**, včetně grafických QFS a skladby ROCK1.AS4. Pravidla jsme porovnali s originálním manuálem Road & Track Presents: The Need for Speed SE (strany 8–9, 14–15).

## Control Central – přesné grafické podklady

Všechny uvedené grafiky se načítají z `CD_Data/FRONTEND/ART/CONTROL` a používají souřadnice uložené v FSH/QFS. Žádná původní grafika se necommituje.

| Režim / volba | Původní grafika | Rozměr/pozice |
| --- | --- | --- |
| Head to Head | `HEADHEAD.QFS` | 238×66; (92,316) |
| Single Race | `SINGLE.QFS` | 238×66; (92,316) |
| Tournament | `TOURNY.QFS` | 238×66; (92,316) |
| **Time Trial** | **`WATCH1.QFS`** | 238×66; (92,316) |
| **Time Trial – panel soupeře** | **`WATCH2.QFS`** | 284×117; (341,99) |
| **The Pack** | **`SLEW.QFS`** | 284×117; (341,99) |
| Tournament Class A/B/C | `TOURNY1/2/3.QFS` | 284×117; (341,99) |
| Konkrétní soupeř | `*2.QFS` | 284×117; (341,99) |
| Skrytý No Mercy | `NOMERCY.QFS` | 238×66; (92,316) |

Původní zdroje mají barevnou paletu, již parser přečte z `CENTRAL.QFS`. **Předchozí nepůvodní nápisy TIME TRIAL, THE PACK a CLASS A/B/C jsou odstraněny.** Obrazovka Options se načítá z `FRONTEND/ART/OPTION/OPTIONS.QFS` a její audio podnabídka z `AUDIO.QFS`.

## Čtyři původní režimy

- **Head to Head:** právě jeden vybraný soupeř. Na otevřených tratích provoz, pokud není aktivován No Mercy. *Policejní pronásledování zatím neimplementováno.*
- **Single Race:** sedm soupeřů. Volba **The Pack** = sedm různých běžných aut; konkrétní soupeřovo auto = sedm aut stejného modelu. Open road bez veřejného provozu.
- **Time Trial:** bez AI soupeřů, původní grafika stopek a měření času.
- **Tournament:** sedm soupeřů dle původní výkonnostní třídy (A = Diablo/F512, B = 911/Viper/ZR1, C = RX7/NSX/Supra). Turnaj nyní zahrnuje původních šest tratí: City/Rusty Springs, Coastal/Autumn Valley, Alpine/Vertigo Ridge. Otevřené tratě musí absolvovat **všechny tři segmenty** před připsáním výsledku.

**No Mercy:** podrž klávesu **End** při výběru Head to Head. Použije původní grafiku NOMERCY a vypne běžný provoz. Policie, závodní asistence AI a další historické odlišnosti ještě vyžadují implementaci.

Současný turnaj používá prozatímní jednoduché body za umístění (10/9/8/…/1), ne zcela věrné vyhodnocování vítězství, soupeřské tabulky a odemykání bonusových položek originálu. Ty budou následovat.

## Options (původní bitmapy)

Stiskni **Options** v Control Central (ne Race). Místo generické nabídky uvidíš skutečné `OPTIONS.QFS` s původními tlačítky na původních pozicích: Transmission, ABS, Traction Control, Audio, Graphics Detail, Control Setup, Load/Save, Credits, Done a Cancel.

- **Transmission:** automatická / manuální převodovka
- **ABS / Traction Control:** zapnutí/vypnutí prvních jednoduchých arkádových asistentů (nikoli ještě přesná původní fyzika)
- **Graphics Detail:** omezení dohlednosti; plné původní grafické volby dosud nejsou implementovány
- **Audio:** otevře `AUDIO.QFS` s volbou přehrávání hudby, menu hudby, žánru ROCK/TECHNO, efektů a hlasitosti
- **Controls, Credits, Load/Save:** zatím informativní; neslibují neexistující funkce
- **Done / Cancel:** potvrzení nebo vrácení změn

Uložené preference: `%LOCALAPPDATA%\OpenNFS1\settings.json`. Nezasahují do původních CD dat ani do repozitáře.

## Hudba

Z CD byly identifikovány `FRONTEND/MUSIC/ROCK1–6.AS4`, `TECHNO1–6.AS4` (EACS/EA IMA ADPCM), `SHOWCASE.ASF` a `PAUSE.ASF`. **Na dodaném ROCK1.AS4 byl úspěšně ověřen převod pomocí FFmpeg** do stereo PCM WAV 22 050 Hz.

- V menu běží `SHOWCASE.ASF`.
- Při závodě se střídají originální `ROCK1–6` nebo `TECHNO1–6` podle nastavení.
- První převod se spustí automaticky mimo hlavní herní vlákno. Převedená data se ukládají do `%LOCALAPPDATA%\OpenNFS1\MusicCache`.
- Při dalších spuštěních už FFmpeg není potřeba pro jednou převedenou skladbu. Hudba se opakuje, hlasitost lze měnit v Audio.
- Není nutné rozbalovat nebo převádět CD ručně a žádné licencované audio není v GitHubu.

**Instalace FFmpeg (jednorázově ve Windows):**

```powershell
winget install -e --id Gyan.FFmpeg
ffmpeg -version
```

Po instalaci případně restartuj Visual Studio, aby se obnovila proměnná PATH. Alternativně lze umístit `ffmpeg.exe` do `Tools` vedle herního EXE. Když FFmpeg chybí, hra běží bez hudby a v Audio se zobrazuje konkrétní diagnostika.

## Omezení a ověření

Windows CI kompiluje a publikuje hru a spouští testy importu ISO, ukončení závodů, turnaje, kolizí a nově pravidel původních závodních režimů.

**Samotnou hudbu ve Windows a pixelově přesný vzhled nových obrazovek je ještě potřeba ověřit interaktivně na PC uživatele.** Další práce: policie, původní pravidla AI, originální nastavení grafických detailů, ovládání, ukládání, výsledkové tabulky a hudební přechody.
