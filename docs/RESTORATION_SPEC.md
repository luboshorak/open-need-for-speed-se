# Need for Speed Special Edition — Restoration contract

## Purpose
Reconstruct the **original work** faithfully, not a modern interpretation that merely functions. Fidelity takes precedence over development speed: original artwork, coordinates, animation, input behavior, timing, audio mappings, game modes, start/finish sequence, race statistics and transitions.

## Rules
1. **Evidence before implementation.** Inspect original retail assets and reproduce original game behavior before coding a replacement. An assumption is not proof.
2. **No inventions presented as restoration.** Do not draw guessed tiles, buttons, screen layouts or labels, invent sound bank indices, times, camera behavior or menu transitions. Unknown = explicitly UNMAPPED, not a license to approximate.
3. **Asset manifest first.** Identify CD-relative QFS/FSH/BNK path; entry ID/index; original position (x,y), dimensions, palette/transparency; animation frames; conditions and timing; screenshot/recording/manual source. Check original values versus output in a native 640x480 coordinate system.
4. **Original assets remain original.** Extract from the user's legitimate original game files at runtime or via a local research tool; do not commit or release retail copyrighted art/audio.
5. **Source of truth = original running game + original CD data.** Current remake code and earlier AI claims are not evidence of authenticity.
6. **Acceptance requires comparison.** Build, original-versus-remake video/screenshots, behavior tests and audio audition; resolve discrepancies before claiming completion. Mark every unverified detail.
7. **Do not merge drafts as completed restorations.** Keep one focused restoration workstream; log evidence and tests alongside implementation.
8. **Modernization is limited to nonobservable infrastructure** (for example Windows runtime or lawful FFmpeg redistribution), with explicit documentation of any unavoidable behavioral differences.

## Evidence levels
- A — verified actual original game asset and measured original behavior.
- B — original-game screenshot/video/manual observation, asset mapping still not confirmed.
- C — hypothesis or remake implementation, insufficient for authentic-restoration acceptance.
- UNKNOWN — not yet mapped. Investigate; do not fabricate.

## Required mapping files
Maintain structured mappings for: screens and transitions; original artwork entry IDs / dimensions / XY; controls by screen and race mode; audio BNK model/sample/loop/activation rules; checkpoints/finish and camera sequence; participant race-time calculation and result precision; source observation/provenance; confidence and test status.

Each entry should include: ID, retail-relative source path, asset entry/sample ID, observed output, coordinates, state/trigger, provenance, confidence, implementation status, regression result.

## Evidence register (2026-10-25)
| Area | Direct evidence | Unmapped facts / no guessing |
|---|---|---|
| Start countdown | User reports original animated graphical tiles above race view (B) | Original asset path, IDs, XY, frames, rhythm |
| Pause | Original PAUSE screenshot, six options; user reports ESC opens it (B) | QFS file/member IDs, precise hitboxes, colors; P separate pause behavior to verify |
| Finish | Original player vehicle drifts after finish before results (B) | Drift/camera path, speed, timing, conditions |
| Results | Original personal segment results followed by overall ranking with Rank, Name, Total Time and Segment Time screenshots (B) | Original artwork member mapping, real per-opponent cumulative/segment times, average speed and records |
| Opponents | User reports corresponding colors above rivals and in minimap (B) | Exact original sprites, palette assignment and projection |
| Audio | Engine effects partly correct; steering/skid incorrect, per user test (B) | Original BNK skid sample IDs, loop points, pitch/volume/when triggered |
| Release | Windows self-contained executable needs redistributable FFmpeg (implementation requirement) | Validate tracked LGPL build/license/decoders, not original asset |

## Mandatory workflow for each restoration
1. Reproduce the feature in the original game, capture before/during/after with input and timing.
2. Inventory relevant original QFS/FSH/BNK entries from actual user-owned game data; use extraction/inspection utilities if necessary.
3. Fill mappings with exact IDs, sizes, positions, timings, provenance and confidence.
4. Implement using verified assets and rules; prohibit guessed graphics/audio bindings.
5. Compile, run tests and compare game behavior and audiovisual output against reference.
6. Correct deviations until acceptance, then merge and update evidence register.

## Priority work
- Map original start tiles and implement precise countdown animation.
- Map original PAUSE screen and state/ESC/P behavior.
- Map end-of-race drift, results pages and result computation.
- Identify and audition original steering/skid sound samples and triggering thresholds.
- Map exact rival pointers/colors and minimap.
- Test Time Trial, Head to Head, Single Race, Tournament, segmented open roads and circuits.

## Explicit anti-patterns
Drawing arbitrary rectangles because the correct FSH entry isn't mapped; using a generic BaseUIScreen instead of original PAUSE artwork; inventing participant times; assuming COLL_SW.BNK sample index; publishing a PR as complete without compiling or playing.
