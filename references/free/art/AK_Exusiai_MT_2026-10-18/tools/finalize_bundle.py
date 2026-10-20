"""Produce contact sheets, validate the bundle, and optionally build a portable ZIP."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import runpy
import zipfile
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]


def font(size):
    candidates = [Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts/msyh.ttc",
                  Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def contact_sheet(entries, title, filename):
    width, thumb_width, thumb_height = 1920, 612, 344
    row_height, top, margin = 390, 92, 24
    height = top + ((len(entries) + 2) // 3) * row_height + margin
    output = Image.new("RGB", (width, height), "#e7ebec")
    draw = ImageDraw.Draw(output)
    draw.text((margin, 23), title, font=font(32), fill="#20292d")
    for index, (label, path) in enumerate(entries):
        x, y = margin + (index % 3) * (thumb_width + 18), top + (index // 3) * row_height
        with Image.open(path) as image:
            output.paste(ImageOps.fit(image.convert("RGB"), (thumb_width, thumb_height)), (x, y))
        draw.text((x, y + thumb_height + 9), label, font=font(22), fill="#20292d")
    output.save(ROOT / "00_index" / filename, quality=94)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--zip-output", type=Path)
    args = parser.parse_args()
    ordinary = sorted((ROOT / "02_created/backgrounds").glob("*.png"))
    names = ["圣秩", "祈祷", "礼拜", "求道", "共融", "告解", "丧礼", "朝圣", "解经"]
    contact_sheet([(f"MT-{i+1} {names[i]}", path) for i, path in enumerate(ordinary)],
                  "众生行记 · MT-1 至 MT-9 战斗背景", "created_backgrounds_contact.jpg")
    entries = []
    for family, labels in (("MT-10", ["梦中", "梦醒"]),
                           ("MT-S-5", ["梦中", "揭示①", "揭示②", "揭示③", "揭示④ · 条件阶段"])):
        directory = ROOT / "02_created" / family
        paths = [directory / "00_dream.png"] + sorted(directory.glob("0[1-4]_*.png"))
        entries.extend((f"{family} · {label}", path) for label, path in zip(labels, paths, strict=True))
    contact_sheet(entries, "众生行记 · Boss 场景递进", "created_boss_states_contact.jpg")
    try:
        runpy.run_path(str(ROOT / "tools/validate_bundle.py"), run_name="bundle_validation")
    except SystemExit as result:
        if result.code not in (None, 0):
            raise
    summary = json.loads((ROOT / "00_index/validation.json").read_bytes())
    if summary["failures"]:
        raise SystemExit("Bundle validation failed")
    manifest = ROOT / "00_index/files.sha256"
    files = sorted(path for path in ROOT.rglob("*") if path.is_file()
                   and path != manifest and "__pycache__" not in path.parts and path.suffix != ".pyc")
    records = [(hashlib.sha256(path.read_bytes()).hexdigest(), path.relative_to(ROOT).as_posix()) for path in files]
    manifest.write_text("".join(f"{digest}  {path}\n" for digest, path in records), encoding="utf-8")
    for digest, relative in records:
        if hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() != digest:
            raise SystemExit("Manifest mismatch: " + relative)
    if args.zip_output:
        archive = args.zip_output.resolve()
        if archive.is_relative_to(ROOT.resolve()):
            raise SystemExit("Write ZIP outside the source folder")
        if archive.exists():
            raise SystemExit("Refusing to replace an existing archive: " + str(archive))
        archive.parent.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as zipped:
            for path in files + [manifest]:
                zipped.write(path, (Path(ROOT.name) / path.relative_to(ROOT)).as_posix())
        with zipfile.ZipFile(archive) as zipped:
            if zipped.testzip() is not None:
                raise SystemExit("Archive CRC check failed")
        print(json.dumps({"archive": str(archive), "bytes": archive.stat().st_size,
                          "sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
                          "files": len(files) + 1}, ensure_ascii=False, indent=2))
    else:
        print("Verified", len(records), "file hashes")


if __name__ == "__main__":
    main()
