# Original NFS SE — sound bank forensic mapping

Source: user's original `nfs-se-original-audio.zip` with **38** original BNK files, inspected 2026-10-25. Individual samples are not distributed in this repository.

## The crucial parser issue

Each BNK begins with an array of **128 absolute file offsets indexed by the original sound slot**. Nonzero slots do NOT form a zero-based array of distinct sounds; many slots point to the **same** sound bytes. Consequently `Samples[5]` is not a durable semantic identifier. `EACS` is the constant *audio format identifier*, not a label that means "skid" or "engine".

The player car banks (`911_SW.BNK`, `SUPRA_SW.BNK`, `RX7_SW.BNK`, `TR512_SW.BNK`, `VIPERSW.BNK`, etc.) have:

| Original slot | Duration | Evidence / usage |
|---|---|---|
| 1 | 0.34–0.73 s | looping first engine sample (acceleration) |
| 2 | 0.43–0.73 s | looping second engine sample (coasting) |
| 3 | 0.19–0.37 s | short gear-shift-related sample (preserve full attack) |
| 32 | 0.32–0.74 s | separate sound, distinct from gear sample |

The `COLL_SW.BNK` common bank has:

| Original slot(s) | Sound duration | Measurable observations |
|---|---|---|
| 33–37 | 0.58–0.96 s | five distinct, nonlooping samples used by preexisting collision player |
| 38, 40, 46 | 1.93 s | all three slots point to the **same stereo sound**; legacy code uses it for offroad |
| 39, 52, 54 | 1.86 s | aliases of the same mono sound; loop length 1.55 s |
| 41, 48, 50 | 0.95 s | aliases of the same stereo sound |
| 42, 43, 45, 56, 58 | 2.11 s | **CONFIRMED tire skid sound (slot 42)** by original-game listener; aliases of one mono sound; loop length 1.56 s |
| 61 | 1.66 s | separate mono loop, not yet positively classified |
| 62 | 0.88 s | separate mono loop, not yet positively classified |
| 63 | 0.18 s | very short separate mono sample |
| 80 | 0.72 s | separate mono nonlooping sample |

**Confirmed with user audition on 2026-10-25:** `COLL_SW.BNK` **slot 42** is the original tire skid/turning sound. Runtime uses its actual 1.56-second loop. Alias slots 43/45/56/58 point to identical original data, but slot 42 is canonical. `COLL_SW` slot 33 (formerly `Samples[0]`) is instead an impact sample and must never be used for tire skid.

## Correct data use

- `BnkFile.GetBySlot(slot)` preserves the original sound slot and does not confuse aliases.
- `FullPCMData` contains the **whole** sound (for one-shot impacts or gear changes); `PCMData` contains the loop region for continuous engine/road sounds.
- Preserve original stereo when the source contains stereo, including the car engine samples.
- `VehicleAudioProvider` loads car-specific engine slots 1 and 2, gear slot 3 and the known offroad slot 38. Crossfade by real engine load; do not restart effects on every input change.
- `EnvironmentAudioProvider` uses original common-bank impact slots 33–37 (whole one-shot clips).
- `VehicleAudioProvider` uses the **verified original `COLL_SW.BNK` slot 42**, with its loop segment and continuous state-based playback, for tire slip.
- Trigger road slip from vehicle physics once per frame. Ordinary steering without actual slip must not generate screeching.
- Audio disabled / paused must remain disabled.

## Countdown/audio unknowns

`FRONT_MW.BNK` has original slots 71 (0.06s), 72 (0.13s), 73 (0.70s), and 74 (2.37s). `NFS_FMMB.BNK` has slots 97, 98/100 aliases, 105, 106. Whether any of these are the race-start beeps is **not established** from bank structure alone.

## Current acceptance status

- Structural bank-slot mapping: verified from uploaded original bytes.
- Parser fix, original stereo, full-vs-loop attack, engine/gear/impact routing: source changes made.
- Tire-screech source sample: **VERIFIED by user (slot 42)**; in-game volume, slip thresholds, and actual output still need gameplay audition.
- Countdown cues: **UNMAPPED** (not guessed).
- Windows CI validates compilation and standalone tests only; it cannot audition the game or your copyrighted CD samples.
