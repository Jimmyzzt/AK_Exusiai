"""Convert downloaded CONFESS-47 Spine 3.8 assets for the game's 4.2 runtime.

Pass a local SpineSkeletonDataConverter executable as the first argument.
"""

import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "references/official/spine/char_4188_confes/defaultskin"
TARGET = ROOT / "AK_Exusiai/images/ancients/confess47"
RESOURCE = "res://AK_Exusiai/images/ancients/confess47"


def atlas_size(path: Path) -> tuple[int, int]:
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("size:"):
            return tuple(map(int, line.removeprefix("size:").strip().split(",")))
    raise ValueError(f"No atlas page size: {path}")


def main() -> None:
    if len(sys.argv) != 2:
        raise SystemExit("Usage: prepare_confess47_spine.py <SpineSkeletonDataConverter.exe>")
    converter = Path(sys.argv[1]).resolve()
    if not converter.is_file():
        raise FileNotFoundError(converter)

    for variant in ("front", "back", "build"):
        stem = "build_char_4188_confes" if variant == "build" else "char_4188_confes"
        source = SOURCE / variant
        target = TARGET / variant
        target.mkdir(parents=True, exist_ok=True)
        atlas = source / f"{stem}.atlas"
        shutil.copyfile(atlas, target / atlas.name)

        with Image.open(source / f"{stem}.png") as opened:
            image = opened.convert("RGBA")
        expected = atlas_size(atlas)
        if image.size != expected:
            print(f"{variant}: normalizing texture {image.size} -> {expected}")
            image = image.resize(expected, Image.Resampling.LANCZOS)
        image.save(target / f"{stem}.png")

        converted = target / f"{stem}_42.skel"
        subprocess.run(
            [str(converter), str(source / f"{stem}.skel"), str(converted), "-v", "4.2.43"],
            check=True,
        )
        (target / "skeleton_data.tres").write_text(
            '[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]\n\n'
            f'[ext_resource type="SpineAtlasResource" path="{RESOURCE}/{variant}/{stem}.atlas" id="1_atlas"]\n'
            f'[ext_resource type="SpineSkeletonFileResource" path="{RESOURCE}/{variant}/{stem}_42.skel" id="2_skeleton"]\n\n'
            '[resource]\natlas_res = ExtResource("1_atlas")\n'
            'skeleton_file_res = ExtResource("2_skeleton")\ndefault_mix = 0.15\n',
            encoding="utf-8",
        )
        print(f"Prepared {variant}")


if __name__ == "__main__":
    main()
