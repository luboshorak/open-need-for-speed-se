# Original NFS SE — race starting lights

Evidence: user's DOSBox recording **2026-10-25_13h14_03.mp4** (start at approximately 46–53 seconds) and original CD bitmap inventory **manifest.json** (7,000-entry local CD scan). Do not check proprietary extracted images into Git.

## Original art — high-resolution HUD

Source: `SIMDATA/MISC/MASKHI.FSH`

| ID | Index | Size | Meaning in the five-light sequence |
|---|---:|---|---|
| `srdd` | 120 | 120×28 | unlit red panel |
| `srdl` | 121 | 120×28 | illuminated red panel |
| `syld` | 123 | 120×28 | unlit yellow panel |
| `syll` | 124 | 120×28 | illuminated yellow panel |
| `sgrn` | 115 | 120×28 | green GO panel |

The same IDs are present in `MASKHIG.FSH`, and at 60×14 in low-resolution `MASKLO.FSH` / `MASKLOG.FSH`. The inventory reports sheet/source coordinates such as `sgrn x=261 y=224`; **these are not HUD destination positions**.

## Observed composition and sequence

- Instead of the racing status strip `stat`, the original draws **five 120×28 panels horizontally** along the HUD top, each in a **128px-wide slot** (640px total). Render left edges at `x = 0, 128, 256, 384, 512` and `y = 0` in the 640×480 game coordinates.
- Slots **0–2** are red; slots **3–4** yellow. Initially they are dark; light one new lamp roughly each second, from left to right.
- Sequence stages 0–4: lit slots `0..stage`; stage 5: **all five green**.
- Green begins after five steps, when the vehicles are released. Green panels remain visible for about one second, and then the normal HUD is restored.
- Referenced video frames: ~47.0 s red1; ~47.5 s red2; ~48.5 s red3; ~49.5 s yellow1; ~50.5 s yellow2; ~51.5 s all green. Timing approximate due to capture/frame sampling; preserve original image IDs regardless of timing refinement.
- Other driving overlays (camera, speedometer, road map) continue to render while the start lights replace the upper HUD.

## Implementation

- `Race.StartCountdown()`, `Race.StartLightStage`, `Race.StartLightsVisible` determine light phase and release. Race/clock pause also freezes light progress.
- `RaceUI.RenderStartingCountdown()` loads **only the original five FSH entries**, displayed at their verified sizes; no made-up rectangles, labels or recolors.
- Sound cues are **not mapped yet**. The source video confirms sound, but neither its actual BNK/sample ID nor timing is established. Do not substitute a random sound or claim completion.

## Acceptance

1. Race starts with red1, transitions one light per second through yellow2, then five green panels and finally the original `stat` strip.
2. AI/player cannot accelerate before green.
3. No surrogate text/rectangles or HUD clock visible during starting lights.
4. ESC and P pause the light stage and preserve remaining timing.
5. Compare frames from built game against the user's DOSBox video. **Not yet executed on a Windows machine**.
