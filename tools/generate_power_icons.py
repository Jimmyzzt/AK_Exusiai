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
            text = re.sub(r'width="[^"]+"', f'width="{size}"', text, count=1)
            text = re.sub(r'height="[^"]+"', f'height="{size}"', text, count=1)
            destination.write_text(text, encoding="utf-8", newline="\n")
        else:
            with Image.open(source) as image:
                image.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS).save(destination)
        print(f"Generated {destination.relative_to(ROOT)}")
