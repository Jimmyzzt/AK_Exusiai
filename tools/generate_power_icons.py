from pathlib import Path
import re

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
POWER_DIR = ROOT / "AK_Exusiai" / "images" / "powers"
SIZES = (64, 256)
for source in POWER_DIR.iterdir():
    if source.suffix.lower() not in {".svg", ".png"}:
        continue
    if source.stem.endswith(("_64", "_256")):
        continue

    for size in SIZES:
        destination = source.with_name(f"{source.stem}_{size}{source.suffix.lower()}")
        if source.suffix.lower() == ".svg":
            text = source.read_text(encoding="utf-8")
            svg_tag_match = re.search(r"<svg\b[^>]*>", text)
            if svg_tag_match is None:
                raise ValueError(f"Missing <svg> root in {source}")

            svg_tag = svg_tag_match.group(0)
            if re.search(r'\bwidth="[^"]+"', svg_tag):
                svg_tag = re.sub(r'\bwidth="[^"]+"', f'width="{size}"', svg_tag, count=1)
            else:
                svg_tag = svg_tag.replace("<svg", f'<svg width="{size}"', 1)
            if re.search(r'\bheight="[^"]+"', svg_tag):
                svg_tag = re.sub(r'\bheight="[^"]+"', f'height="{size}"', svg_tag, count=1)
            else:
                svg_tag = svg_tag.replace("<svg", f'<svg height="{size}"', 1)

            text = text[:svg_tag_match.start()] + svg_tag + text[svg_tag_match.end():]
            destination.write_text(text, encoding="utf-8", newline="\n")
        else:
            with Image.open(source) as image:
                image.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS).save(destination)
        print(f"Generated {destination.relative_to(ROOT)}")
