#!/usr/bin/env python3
"""Generate a real static font instance from a variable OpenType/TrueType font.
Usage: font-weight-instance.py SOURCE OUTPUT WEIGHT
Requires fontTools (Apache/MIT licensed package). Preserves Vietnamese glyphs.
"""
import os
import sys
import tempfile
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

def main():
    if len(sys.argv) != 4:
        raise SystemExit("Usage: font-weight-instance.py INPUT OUTPUT WEIGHT")
    source, dest, weight_text = sys.argv[1:]
    weight = int(weight_text)
    if not 100 <= weight <= 900:
        raise ValueError("Weight must be between 100 and 900")
    font = TTFont(source, recalcBBoxes=True, recalcTimestamp=False)
    try:
        axes = {axis.axisTag: axis for axis in font["fvar"].axes} if "fvar" in font else {}
        if "wght" not in axes:
            raise ValueError("Font này không có trục wght; hãy chọn đúng biến thể font tĩnh")
        axis = axes["wght"]
        if not axis.minValue <= weight <= axis.maxValue:
            raise ValueError(f"Weight {weight} ngoài khoảng {axis.minValue:g}–{axis.maxValue:g}")
        # Instantiate all variations at their defaults except wght. This creates a
        # deterministic, non-variable TTF/OTF suitable for the game's raw font loader.
        # Pin every secondary variation axis (opsz/wdth/etc.) to its default.
        # Otherwise multi-axis Google Fonts remain variable after wght instancing.
        pins = {tag: float(axis.defaultValue) for tag, axis in axes.items()}
        pins["wght"] = float(weight)
        instance = instantiateVariableFont(font, pins, inplace=True, optimize=True)
        if "OS/2" in instance:
            instance["OS/2"].usWeightClass = weight
        if "name" in instance:
            for record in instance["name"].names:
                if record.nameID == 2:
                    record.string = ("Regular" if weight == 400 else "Weight " + str(weight)).encode(
                        "utf-16-be" if record.isUnicode() else "ascii")
        # Vietnamese coverage must be checked before a weight is accepted.
        required = "Người Lữ Hành đã thức tỉnh sức mạnh. Phần thưởng quý giá. ắằẳẵặấầẩẫậữựỡợ"
        cmap = instance.getBestCmap() or {}
        missing = sorted({c for c in required if not c.isspace() and ord(c) not in cmap})
        if missing:
            raise ValueError("Font thiếu ký tự tiếng Việt: " + "".join(missing))
        os.makedirs(os.path.dirname(os.path.abspath(dest)), exist_ok=True)
        fd, tmp = tempfile.mkstemp(prefix="font-instance-", suffix=".ttf", dir=os.path.dirname(os.path.abspath(dest)))
        os.close(fd)
        try:
            instance.save(tmp)
            verify = TTFont(tmp)
            try:
                if "fvar" in verify:
                    raise ValueError("Kết quả chưa phải font tĩnh")
                if "OS/2" in verify and verify["OS/2"].usWeightClass != weight:
                    raise ValueError("Weight trong font đầu ra không đúng")
            finally:
                verify.close()
            os.replace(tmp, dest)
        finally:
            if os.path.exists(tmp):
                os.unlink(tmp)
        print(f"OK: {dest} (wght={weight})")
    finally:
        font.close()

if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(1)
