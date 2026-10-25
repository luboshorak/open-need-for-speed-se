# Police pursuit comparison

Independent C# implementation based on factual reverse engineering and the player's original TRI files. Sources: [ReNFS notes, section 4.17](https://github.com/ItsDanik/ReNFS/blob/main/docs/NOTES.md), [opponent.c](https://github.com/ItsDanik/ReNFS/blob/main/port/src/opponent.c), and [track.c](https://github.com/ItsDanik/ReNFS/blob/main/port/src/track.c). No original game assets are distributed. No ReNFS implementation code is copied.

| Rule | Previous remake | This repair / original evidence |
|---|---|---|
| Patrol dispatch | 3.5 seconds above 110, arbitrary spawn 26 nodes ahead | Scan once for each newly passed node, approximately 50 nodes ahead; only original marked nodes (police_come, 0x79500) |
| Spawn data | Ignored | TRI node byte +0x1a: bit 0 eligible, bit 1 left, bit 2 moving; bits 3–4 chance category |
| Probability | Unconditional | Random 0–127 <= 32/64/96/128; original executable table at 0xf7500 independently checked |
| Road limits | Constant 110 | Three speed bytes per four nodes from TRI 0x15b0c: traffic, police detection, pursuit matching |
| Detection | Before actual police encounter | Actual patrol, absolute gap <5 nodes, player strictly above local police limit (0x79728) |
| Radar | Entire spawned pursuit | Proximity <10 nodes; siren only while chasing |
| Caught | Player slow and police within four nodes for 2.5 seconds | Police >2 nodes ahead, or level/ahead with both below 3 m/s; citation requires caught and both below 1 m/s, with player not in crash recovery |
| Warning vs ticket | Five-second cutoff, no police speed check | Chase counter increases by four every fourth 30-ms step; warning below 400, ticket at/above 400 (12 seconds at the original cadence) |
| Arrest | Third ticket | Second ticket |
| Police lost | Immediate disappearance and arbitrary cooldown | One reposition behind the player, then end pursuit on the next loss; limits of reach remain an adaptation |
| Citation display | Wrong FRONTEND file, modal screen, manual dismissal | Actual SIMDATA/MISC/WARNDH and TICKETDH, scrn entry, 640x480; sixty original steps (1.8 seconds), opponents and clock continue |
| Race end | Pursuit could continue through finish drift | Chase dropped at open-road finish |
| Driving | Accumulated binary steering, low speed caps can prevent passing | Bounded yaw per second, speed matching in early chase, adjacent passing, braking at caught flag |

## Verified against original data

The actual TriFile parser was executed without graphics against CY1, CL1 and AL1. Every node's police flags and three limits match raw original bytes. CY1: 1920 nodes, eight marks at 170, 379, 682, 920, 1008, 1112, 1333, 1450. The parked mark is 1008 (0x0b). CL1: eight marks; AL1: seven marks. The actual QFS decoder confirms both warning/ticket scrn images are 640x480.

Frontend regression tests cover no-patrol speeding, exact speed/distance boundaries, both cars stopped, a patrol still behind the player, short/long chases, repeat tickets and arrest, spawn probabilities, disappearance, 30/60/144-FPS decision cadence, overtaking and convergent yaw. Previous finish/ranking regressions remain passing. Release build succeeds. Full visual/interactive pursuit testing has not been performed.

## Explicit adaptations and remaining differences

- This project uses legacy mph-valued speed fields/HUD, not original 16.16 m/s physics. Imported speed bytes are converted by 2.2369363. Distances remain remake track nodes.
- Original opponent configuration can vary by settings/driver records. This version uses the documented defaults of 400 warning ticks and sixty wait steps. It does not import every original opponent configuration record.
- Original lane scoring, object-ahead scoring, grip tables and full vehicle dynamics are not ported. Continuous lane movement, a +20 mph passing margin, curve speed bound, acceleration/deceleration and yaw limits are independent adaptations.
- Original caught-state steering towards police is approximated by automatic braking followed by placement at the right verge. Oncoming patrol turnaround uses a forward placement 18 nodes behind, rather than reproducing every intermediate original turn state.
- The original uses road-order visibility windows for replacement. Current reach thresholds are 95 nodes behind / 140 ahead; one replacement at player minus 18 nodes preserves chase time, the second drops it.
- Original police body palette/light animation, camera mode 7 and arrest film are not restored. Arrest ends with the existing acknowledgement screen, corrected to two tickets and the correct original image.
- Traffic/police availability remains this project's open-road Head to Head mode with No Mercy disabled. No linked-game police support is added.
- Warning/ticket counts are scoped to the current Race, matching the existing race lifecycle; cross-segment persistence is not added.

## Follow-up repair: alert, tickets and racing opponents
+
+The first repair conflated the global alert with the forward-chasing police flag (+0x175 & 4). A patrol already ahead could immediately catch the player. HasJoinedPursuit now requires a forward-facing, unparked patrol to have been behind the player before it can catch by passing or stopping alongside.
+
+Race.RegisterPoliceStop owns persistent per-race warning/ticket totals and latches arrest at two actual tickets. Three warnings are legal and do not increment tickets. Original images distinguish warning, speeding and game over; no custom text is drawn. Stop counts and arrest state are logged through AudioDiagnostics. A regression invokes the actual Race method from the Release assembly: three warnings followed by two tickets.
+
+Opponent AI is still an adaptation. It evaluates closing-time threats up to 650 world units, distinguishes lateral clearance, checks adjacent traffic and avoids entering oncoming lanes. Proportional wheel control replaces binary steering. Stalls are detected even when a crashed car faces sideways. Recovery reverses only with clear rear space and realigns a badly rotated car. Repeated failure selects a collision-free pose at the same node without awarding progress. Opponents always use automatic gearboxes, independently of the player's gearbox. The shadowing AtEndOfTrack field was removed.
+
+Tests cover alert without catch, oncoming alert, joined pursuit, oncoming hazards, adjacent vehicles and blocked escape lanes. Finish/ranking and frontend regressions pass. Interactive pursuit and crash/recovery runs have not been performed. Window title: Police & AI repair 2.
+
## Original English police artwork
+
+Confirmed in research/local-art-inventory/manifest.csv and visually inspected: all three assets are 640x480 at (0,0). Game over is FRONTEND/ART/TICKET.QFS, bgnd; warning is SIMDATA/MISC/WARNDH.QFS, scrn; speeding is SIMDATA/MISC/TICKETDH.QFS, scrn. No German, low-resolution or custom text overlay is used. Game-over acknowledgement returns to Control Central and releases forced braking.
+
## Stop/release physics correction

Clearing Speed did not clear DrivableVehicle._force. The first normal physics step could reapply stored drive force after the ticket picture disappeared. HoldForPolice now clears force, velocity, steering/slip/collision rotation, controls and pending gear changes; the motor is idle in neutral. ReleaseFromPolice retains zero momentum and removes the brake. The ordinary grid-idle function is no longer used during a police hold because it selects first gear.

Roadside placement now changes only the coordinate across the road, preserving the longitudinal position, as original pull_aside does. It previously placed the player at the start of the current node. A held player is an immovable collision body so traffic cannot displace or rotate it while the picture covers the view. Other cars continue to simulate; no rival position or race progress is reset by a police stop. Whether a rival passes during the wait depends on its actual gap and speed, not an invented rank penalty.

Regression tests invoke the actual Release DrivableVehicle/Motor/Gearbox methods with stored force and an in-flight shift. They verify zero force/speed/power, neutral, cancelled shift and unchanged position before and after release. Placement tests check longitudinal preservation on rotated roads. Interactive gameplay remains unverified.
