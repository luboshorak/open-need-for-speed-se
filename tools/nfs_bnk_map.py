#!/usr/bin/env python3
"""Extract *real* Need for Speed SE BNK sound samples for accurate event mapping.

    py -3 tools/nfs_bnk_map.py "I:\SIMDATA\SOUNDBNK"

Creates local research/audio-bank-inventory/manifest.csv and numbered WAV files.
The original sound data are user-owned and must NOT be committed or distributed.
No sample labels are inferred from numerical indices.
"""
import argparse
import csv
import struct
import wave
from pathlib import Path


def sample_bank(path, output, root):
    data = path.read_bytes()
    if len(data) < 512:
        raise ValueError("Truncated original BNK header")
    offsets = [struct.unpack_from("<I", data, i * 4)[0] for i in range(128)]
    samples = [(i, offset) for i, offset in enumerate(offsets) if offset != 0]
    rows = []
    rel = path.relative_to(root).with_suffix("")
    for index, (slot, offset) in enumerate(samples):
        header = offset + 0x28
        if header + 32 > len(data):
            raise ValueError(f"Invalid BNK sample offset {offset:#x}")
        sid = data[header:header+4].decode("latin-1", "replace")
        rate, bps, channels, compression, stype, length, loop_start, loop_len, data_offset, unk = (
            struct.unpack_from("<iBBBBiiiii", data, header+4))
        if not 1000 <= rate <= 192000 or bps not in (1, 2) or channels not in (1, 2):
            raise ValueError(f"Invalid sample format at index {index}")
        if length <= 0 or length > 60_000_000 or data_offset < 0:
            raise ValueError(f"Invalid sample data bounds for index {index}")
        end = data_offset + length * channels * bps
        if end > len(data):
            raise ValueError(f"Truncated BNK sample {index}")
        pcm = data[data_offset:end]
        if bps == 1:
            signed16 = bytearray(len(pcm) * 2)
            for i, v in enumerate(pcm):
                value = (v - 256 if v >= 128 else v) << 8
                struct.pack_into("<h", signed16, i * 2, value)
            pcm = signed16
        folder = output / rel
        folder.mkdir(parents=True, exist_ok=True)
        wav = folder / f"{index:03d}_slot{slot:03d}.wav"
        with wave.open(str(wav), "wb") as dst:
            dst.setnchannels(channels)
            dst.setsampwidth(2)
            dst.setframerate(rate)
            dst.writeframes(pcm)
        rows.append(dict(bank=path.relative_to(root).as_posix(), index=index,
            slot=slot, source_id=sid, sample_rate=rate, channels=channels,
            bytes_per_sample=bps, compression=compression, type=stype,
            frames=length, loop_start=loop_start, loop_length=loop_len,
            duration_seconds=f"{length/rate:.3f}",
            wav=wav.relative_to(output).as_posix()))
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Mounted original CD SOUNDBNK directory, e.g. I:\\SIMDATA\\SOUNDBNK")
    parser.add_argument("--out", type=Path, default=Path("research/audio-bank-inventory"))
    a = parser.parse_args()
    src = a.source.resolve()
    if not src.exists():
        parser.error(f"Cannot find {src}")
    root = src if src.is_dir() else src.parent
    paths = sorted(src.rglob("*.BNK")) if src.is_dir() else [src]
    # Windows file extensions can use mixed case.
    if src.is_dir():
        paths = sorted(p for p in src.rglob("*") if p.is_file() and p.suffix.lower() == ".bnk")
    if not paths:
        parser.error("No .BNK files found under source directory")
    out = a.out.resolve()
    out.mkdir(parents=True, exist_ok=True)
    fields = ["bank", "index", "slot", "source_id", "sample_rate", "channels",
              "bytes_per_sample", "compression", "type", "frames",
              "loop_start", "loop_length", "duration_seconds", "wav"]
    entries, errors = [], []
    for path in paths:
        try:
            rows = sample_bank(path, out, root)
            entries.extend(rows)
            print(f"{path.name}: {len(rows)} samples")
        except (OSError, ValueError, struct.error) as exc:
            errors.append(f"{path}: {exc}")
            print(f"WARNING {errors[-1]}")
    with (out / "manifest.csv").open("w", newline="", encoding="utf-8-sig") as fh:
        writer = csv.DictWriter(fh, fieldnames=fields)
        writer.writeheader()
        writer.writerows(entries)
    print(f"Done: {len(entries)} original WAV samples, {len(errors)} failed banks.")
    print(out / "manifest.csv")


if __name__ == "__main__":
    main()
