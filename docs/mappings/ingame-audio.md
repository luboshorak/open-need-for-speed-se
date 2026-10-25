# NFS SE driving audio — repair 2

Original recordings are read from the player's game files and are not distributed.
BNK offsets are indexed by original slot, including aliases; EACS identifies a format, not a sound.

| Bank | Slot | Use |
|---|---|---|
| Player car | 1 / 2 | Power / coast engine |
| Player car | 3 / 32 | Horn (H) / gear clunk |
| COLLSWWT.BNK; COLLSWMT.BNK on tr7 | 33 / 34 / 36 | Increasing car impact severity |
| Common | 35 / 37 | Wall / landing |
| Common | 38 / 41 / 42 | Offroad slip / road rolling / tire skid |
| Common | 48 / 56 / 63 | Traffic engine / skid / horn |
| Common | 62 (61 on tr7) / 80 | Track ambience / gravel |
| Opponent car-specific bank | 67 / 65 | Engine / horn |
| NFS_FMMB.BNK | 97 / 98 / 100 | Radar / siren / siren echo |
| FRONTEND/SPEECH | BESTTIME, BESTLAST, FINALLAP.EAS | Circuit lap announcements |

Playback preserves full PCM, original loop bounds, bank gain, pitch cents and bend reach. Player audio retains stereo; positional emitters are downmixed before panning. Standalone speech uses IMA ADPCM, high nibble first and zero initial state. Each voice loads independently. After stopping, recreate the instance: MonoGame/XAudio can briefly retain Playing state and ignore immediate Play on the old instance.

Road audio follows speed/contact, tunnel and rough-surface properties; track properties select directional ambience. Police pursuit drives radar/sirens. Pause, focus loss and race exit stop racing audio.

## Evidence and limits

Factual mappings/event research: [ReNFS racesnd.c](https://github.com/ItsDanik/ReNFS/blob/main/port/src/racesnd.c), [sndlib.c](https://github.com/ItsDanik/ReNFS/blob/main/port/src/sndlib.c), [notes](https://github.com/ItsDanik/ReNFS/blob/main/docs/NOTES.md). The C# implementation is independent.

Direct inspection of the user's original DOS NFS.EXE load image confirms slots 32/37 in the hit routine at 0x52c38, radar periods at 0x52560, and RET at the countdown beep routine 0x5257c. No countdown beep is invented for this executable.

Mix curves, distances, Doppler, traffic horn triggers and gravel cadence are approximations adapted to this remake. Best-lap speech compares this race's laps, not a persistent original record. Slot 39 body scrape is not wired because the remake lacks the corresponding body tumbling state. Additional original voices/aliases (43, 50, 58, 69) are represented by one skid per vehicle and up to two nearby vehicles. This is not a claim of identical mixing or complete crash-animation parity.

## Validation

OriginalAudioBankTests checks 38 original banks, 329 populated slots, read-only access, aliases, loop bounds, signed stereo PCM, driving states, pitch and event rules. Three speech outputs match independent Python audioop decoding byte for byte.

`OpenNFS1.exe --audio-audit <CD_Data> <report>` starts, stops and restarts 80 bank/speech cases through MonoGame: 80 passed, 0 failed. Output is muted; this checks backend state, not listening quality or all gameplay triggers. Release build: zero errors. Window title identifies `[Audio repair 2]`.
