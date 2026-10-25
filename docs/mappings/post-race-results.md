# Original NFS SE post-race result artwork mapping

Source: user's original CD `manifest.json` and supplied DOSBox screenshots (2026-10-25). Proprietary graphics are **not** stored in this repository.

## Personal results (first screen after crossing)
- Open road: `FRONTEND/ART/STATS/STANDSEG.QFS`
- Circuit: `FRONTEND/ART/STATS/STANDTRK.QFS`
- `bgnd`: 640×480 at (0,0). **Already contains** the labels Current Standing, Segment Time, Total Track Time, Average Speed, Top Speed and personal record headings. Do **not** print those labels a second time.
- Course images `coas`, `alpi`, `city`: 562×31 at (2,62). Other course titles require measurement of the original rendering, not invented artwork.
- `dond`/`donl`: at roughly (6,442) in the original bitmap metadata (98×30 for CD FRONTEND/ART).
- Dynamic player fields measured against original screenshots: rank, elapsed time(s), average/top speed and saved personal records. Field alignment needs side-by-side game confirmation.

## All-racers standings (second screen)
- Open road segments: `FRONTEND/ART/STATS/SGMTSTND.QFS`
- Finished circuit: `FRONTEND/ART/STATS/FINSTND.QFS`
- `bgnd`: 640×480 at (0,0), already includes the Rank/Name/Total Time/Segment Time column captions.
- For `SGMTSTND`, per-course title art includes `alpi`, `autu`, `burn`, `city`, `coas`, `mont`, `neod`, `rust`, `trop` (562×31 at (2,62)).
- `Dond`/`Donl` at about (7,442), 88×32.
- Per-driver times are recorded on crossing the real end gate (open roads and circuits). Unknown cumulative totals must remain unknown; never manufacture completion times.

## Program behavior
Finish cinematic -> personal `STANDSEG` / `STANDTRK` -> all drivers `SGMTSTND` / `FINSTND` -> `FINMENU`. Re-entering Game Standings from `FINMENU` goes straight to its actual standings screen.

## Non-goals
Do not replace native labels with app fonts; do not display a guessed race-results heading on top of `bgnd`. Audio/music and historical record tables need separately verified source mappings.
