#!/usr/bin/env python3
r"""Inspect original NFS SE QFS/FSH artwork without a game/GPU dependency.

py -3 tools/nfs_asset_map.py "I:\"
py -3 tools/nfs_asset_map.py "C:\Path\To\CD_Data" --out "C:\Temp\nfs-art" --all
Only local outputs contain copyrighted original-game images. Never commit them.
"""
import argparse
import csv
import html
import json
import struct
import zlib
from pathlib import Path


def refpack(src):
    if len(src) < 8 or src[1] != 0xfb or src[0] not in (0x10, 0x11):
        raise ValueError("Unsupported RefPack header")
    size = int.from_bytes(src[2:5], "big")
    if not 0 < size <= 64 * 1024 * 1024:
        raise ValueError("Invalid RefPack output length")
    pos, out = (8 if src[0] & 1 else 5), bytearray()

    def literal(count):
        nonlocal pos
        if pos + count > len(src): raise ValueError("Truncated literal")
        out.extend(src[pos:pos + count])
        pos += count

    def back(offset, count):
        if offset <= 0 or offset > len(out) or len(out) + count > size:
            raise ValueError("Invalid RefPack back reference")
        for _ in range(count): out.append(out[-offset])

    while pos < len(src) and src[pos] < 0xfc:
        token = src[pos]
        if token < 0x80:
            if pos + 2 > len(src): raise ValueError("Truncated token")
            a = src[pos + 1]; pos += 2
            literal(token & 3)
            back(((token >> 5) << 8) + a + 1, ((token & 0x1c) >> 2) + 3)
        elif token < 0xc0:
            if pos + 3 > len(src): raise ValueError("Truncated token")
            a, b = src[pos + 1:pos + 3]; pos += 3
            literal((a >> 6) & 3)
            back(((a & 0x3f) << 8) + b + 1, (token & 0x3f) + 4)
        elif token < 0xe0:
            if pos + 4 > len(src): raise ValueError("Truncated token")
            a, b, c = src[pos + 1:pos + 4]; pos += 4
            literal(token & 3)
            back(((token & 0x10) << 12) + (a << 8) + b + 1,
                 ((token >> 2) & 3) * 256 + c + 5)
        else:
            pos += 1
            literal((token & 0x1f) * 4 + 4)
    if pos < len(src):
        n = src[pos] & 3
        pos += 1
        literal(n)
    if len(out) != size:
        raise ValueError(f"RefPack decoded {len(out)} / expected {size}")
    return bytes(out)


class Bits:
    def __init__(self, data):
        self.data, self.pos = data, 0

    def read(self, n):
        if n < 0 or self.pos + n > len(self.data) * 8:
            raise ValueError("Truncated Huffman stream")
        v = 0
        for _ in range(n):
            v = (v << 1) | ((self.data[self.pos // 8] >> (7 - self.pos % 8)) & 1)
            self.pos += 1
        return v

    def variable(self):
        width = 2
        while self.read(1) == 0:
            width += 1
            if width > 16: raise ValueError("Invalid variable-length value")
        return self.read(width) + (1 << width) - 4


def huffman(src):
    bits = Bits(src)
    method = bits.read(16)
    if method & 0x100: bits.read(24)
    size = bits.read(24)
    if not 0 < size <= 64 * 1024 * 1024:
        raise ValueError("Invalid Huffman output length")
    escape = bits.read(8)
    counts, sum_codes = [], 0
    while True:
        if len(counts) >= 16: raise ValueError("Invalid Huffman code lengths")
        count = bits.variable()
        counts.append(count)
        sum_codes = sum_codes * 2 + count
        if sum_codes > (1 << len(counts)):
            raise ValueError("Oversubscribed Huffman codes")
        if sum_codes == (1 << len(counts)): break
    if not 1 <= sum(counts) <= 256:
        raise ValueError("Invalid Huffman symbol count")
    symbols, used, cursor = [], set(), 255
    for _ in range(sum(counts)):
        remaining = bits.variable() + 1
        while remaining:
            cursor = (cursor + 1) & 255
            if cursor not in used: remaining -= 1
        used.add(cursor)
        symbols.append(cursor)
    codes, code, idx = {}, 0, 0
    for width, count in enumerate(counts, 1):
        for _ in range(count):
            codes[(width, code >> (16 - width))] = symbols[idx]
            idx += 1
            code += 1 << (16 - width)
    result = bytearray()
    while len(result) < size:
        prefix = 0
        for width in range(1, len(counts) + 1):
            prefix = (prefix << 1) | bits.read(1)
            if (width, prefix) in codes:
                value = codes[(width, prefix)]
                break
        else:
            raise ValueError("Unknown Huffman code")
        if value != escape:
            result.append(value)
        else:
            count = bits.variable()
            if not count:
                if bits.read(1): raise ValueError("Unexpected Huffman end marker")
                result.append(bits.read(8))
            else:
                if not result or len(result) + count > size:
                    raise ValueError("Invalid Huffman run")
                result.extend([result[-1]] * count)
    if (method & 0xfeff) == 0x32fb:
        x = 0
        for i, v in enumerate(result):
            x = (x + v) & 255
            result[i] = x
    elif (method & 0xfeff) == 0x34fb:
        x = y = 0
        for i, v in enumerate(result):
            x = (x + v) & 255
            y = (y + x) & 255
            result[i] = y
    return bytes(result)


def decompress(data):
    if data[:4] == b"SHPI": return data
    if len(data) < 5 or data[1] != 0xfb:
        raise ValueError("Unknown FSH/QFS compression")
    if data[0] in (0x10, 0x11): return refpack(data)
    if data[0] in (0x30, 0x31, 0x32, 0x33, 0x34, 0x35):
        return huffman(data)
    raise ValueError("Unsupported QFS method")


def read_palette(data, off, kind):
    if off + 16 + 768 > len(data): return None
    pal = data[off + 16:off + 16 + 768]
    if kind == 0x22:
        pal = bytes((c << 2) & 255 for c in pal)
    return pal


def write_png(path, width, height, pixels, palette):
    rgba = bytearray()
    for pix in pixels:
        rgba.extend(palette[3 * pix:3 * pix + 3])
        rgba.append(0 if pix == 255 else 255)
    scanlines = b"".join(
        b"\0" + rgba[y * width * 4:(y + 1) * width * 4]
        for y in range(height)
    )
    def chunk(name, body):
        return (struct.pack(">I", len(body)) + name + body +
                struct.pack(">I", zlib.crc32(name + body)))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n" +
        chunk(b"IHDR", struct.pack(">2I5B", width, height, 8, 6, 0, 0, 0)) +
        chunk(b"IDAT", zlib.compress(scanlines, 9)) + chunk(b"IEND", b"")
    )


def inspect(data, name, output):
    if data[:4] != b"SHPI" or len(data) < 16:
        raise ValueError("Decoded data is not SHPI")
    count = struct.unpack_from("<I", data, 8)[0]
    if count > 8192 or 16 + count * 8 > len(data):
        raise ValueError("Invalid SHPI index")
    entries = []
    for i in range(count):
        ident = data[16 + i * 8:20 + i * 8].decode("latin-1")
        off = struct.unpack_from("<I", data, 20 + i * 8)[0]
        if off + 16 > len(data):
            entries.append(dict(file=name, index=i, id=ident,
                                error="Entry offset outside file"))
            continue
        raw = struct.unpack_from("<I", data, off)[0]
        kind = raw & 0x7f
        w, h, m0, m1, x, y = struct.unpack_from("<HHhhhh", data, off + 4)
        entries.append(dict(file=name, index=i, id=ident, offset=off,
                            type=f"0x{kind:02x}", width=w, height=h,
                            x=x, y=y, misc0=m0, misc1=m1,
                            palette="", png=""))
    palette = None
    for entry in entries:
        if entry.get("id", "").upper() == "!PAL" and entry.get("type") in ("0x22", "0x24"):
            palette = read_palette(data, entry["offset"], int(entry["type"], 16))
            break
    for entry in entries:
        if entry.get("type") != "0x7b": continue
        w, h, off = entry["width"], entry["height"], entry["offset"]
        if w <= 0 or h <= 0 or w * h > 16_000_000 or off + 16 + w * h > len(data):
            entry["error"] = "Invalid bitmap dimensions or truncated data"
            continue
        indexed_pixels = data[off + 16:off + 16 + w * h]
        active_palette = palette
        child_offset = off + 16 + w * h
        if child_offset + 16 <= len(data):
            child_type = struct.unpack_from("<I", data, child_offset)[0] & 0x7f
            if child_type in (0x22, 0x24):
                attached = read_palette(data, child_offset, child_type)
                if attached:
                    active_palette = attached
                    entry["palette"] = "attached"
        if active_palette is None:
            entry["error"] = "Palette unavailable: no guessed colors exported"
            continue
        if not entry["palette"]: entry["palette"] = "global !PAL"
        safe_id = "".join(c if c.isascii() and c.isalnum() else "_" for c in entry["id"])
        rel = Path("images") / Path(name).with_suffix("") / f"{entry['index']:03}_{safe_id}.png"
        write_png(output / rel, w, h, indexed_pixels, active_palette)
        entry["png"] = rel.as_posix()
    return entries


def find_files(root, all_files):
    """Find artwork under any CD layout, including I:/NFSSE/... or mixed case.

    The original CD may nest FRONTEND/SIMDATA under another directory.
    Path.glob("FRONTEND/ART") at the drive root silently skipped such discs.
    """
    if root.is_file():
        return [root]
    candidates = sorted(
        (p for p in root.rglob("*")
         if p.is_file() and p.suffix.casefold() in (".qfs", ".fsh")),
        key=lambda p: str(p).casefold()
    )
    if all_files:
        return candidates

    def game_art(path):
        parts = [part.casefold() for part in path.relative_to(root).parts]
        return any(
            (parts[i] == "frontend" and parts[i + 1] == "art") or
            (parts[i] == "simdata" and parts[i + 1] == "misc")
            for i in range(len(parts) - 1)
        )

    return [p for p in candidates if game_art(p)]


def gallery(entries, output):
    page = ['<!doctype html><html><head><meta charset="utf-8">',
            '<title>NFS SE artwork mapping</title>',
            '<style>body{background:#17191c;color:#eee;font:14px Arial}'
            'h2{border-top:1px solid #555;padding-top:12px}'
            'section{display:inline-block;vertical-align:top;width:205px;'
            'min-height:100px;margin:5px;background:#30343a;padding:8px}'
            'img{image-rendering:pixelated;max-width:200px;max-height:160px}'
            'small{display:block;color:#bbb}</style></head><body>',
            '<h1>Original NFS SE bitmap inventory — local research only</h1>']
    last = None
    for e in entries:
        if e["file"] != last:
            last = e["file"]
            page.append("<h2>" + html.escape(last) + "</h2>")
        location = f"{e.get('width','?')} × {e.get('height','?')} — XY {e.get('x','?')}, {e.get('y','?')}"
        page.append("<section><b>" + html.escape(e["id"]) +
                    "</b><small>" + html.escape(location) + "</small>")
        if e.get("png"):
            page.append('<img loading="lazy" src="' +
                        html.escape(e["png"], quote=True) + '">')
        if e.get("error"):
            page.append("<small>" + html.escape(e["error"]) + "</small>")
        page.append("</section>")
    page.append("</body></html>")
    (output / "index.html").write_text("\n".join(page), encoding="utf-8")


def main():
    arg = argparse.ArgumentParser(description=__doc__)
    arg.add_argument("cd_data", type=Path, help="Extracted original CD root or single FSH/QFS file")
    arg.add_argument("--out", type=Path, default=Path("research/local-art-inventory"))
    arg.add_argument("--all", "-all", action="store_true", help="Include all QFS/FSH from the disc, not just frontend art")
    a = arg.parse_args()
    source, output = a.cd_data.resolve(), a.out.resolve()
    if not source.exists(): arg.error(f"Not found: {source}")
    paths = find_files(source, a.all)
    if not paths: arg.error(f"No UI QFS/FSH found under {source}. Use your mounted original CD root, e.g. I:\\, with --all to scan every archive.")
    output.mkdir(parents=True, exist_ok=True)
    entries, errors = [], []
    print(f"Found {len(paths)} original archives under {source}")
    for number, path in enumerate(paths, 1):
        if number == 1 or number % 25 == 0:
            print(f"Scanning {number}/{len(paths)}: {path.name}", flush=True)
        relative = path.relative_to(source).as_posix() if source.is_dir() else path.name
        try:
            entries.extend(inspect(decompress(path.read_bytes()), relative, output))
        except (OSError, ValueError, IndexError, struct.error) as err:
            errors.append(dict(file=relative, error=str(err)))
    (output / "manifest.json").write_text(
        json.dumps(dict(entries=entries, errors=errors), ensure_ascii=False, indent=2),
        encoding="utf-8")
    fields = ["file", "index", "id", "offset", "type", "width", "height",
              "x", "y", "misc0", "misc1", "palette", "png", "error"]
    with (output / "manifest.csv").open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(entries)
    gallery(entries, output)
    print(f"Inspected {len(paths)} files, {len(entries)} entries, "
          f"{sum(bool(e.get('png')) for e in entries)} PNG, {len(errors)} failed archives.")
    print(f"Open: {output / 'index.html'}")
    for err in errors[:12]:
        print(f"WARN {err['file']}: {err['error']}")


if __name__ == "__main__":
    main()
