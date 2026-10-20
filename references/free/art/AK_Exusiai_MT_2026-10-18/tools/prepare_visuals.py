"""Normalize generated artwork and split additive, camera-stable scene layers."""
import csv
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
REPO = next((p for p in ROOT.parents if (p / "AK_Exusiai.csproj").exists()), None)
SIZE = (1920, 1080)
MASKS = {
    "MT-10": [[(0, .25, .24, .75), (.76, .25, 1, .75)]],
    "MT-S-5": [
        [(0, .16, .24, .38), (.76, .16, 1, .38), (0, .38, .18, .69), (.82, .38, 1, .69)],
        [(0, .39, .24, .75), (.76, .39, 1, .75), (.10, .68, .90, .77)],
        [(.10, .20, .90, .40)],
        [(0, .04, .25, .73), (.75, .04, 1, .73), (.25, .17, .75, .30)],
    ],
}


def normalized(path):
    image = Image.open(path)
    if "boss" in path.parts:
        image = image.convert("RGBA")
        image.thumbnail((1280, 1280), Image.Resampling.LANCZOS)
        padded = Image.new("RGBA", (1536, 1536))
        padded.alpha_composite(image, ((1536-image.width)//2, (1536-image.height)//2))
        return padded
    return ImageOps.fit(image.convert("RGB"), SIZE, Image.Resampling.LANCZOS)


def main():
    prompts = json.loads((ROOT / "00_index/generation_prompts.json").read_bytes())
    for record in prompts:
        path = ROOT / record["source"]
        if not record["source"].startswith("02_created/"):
            continue
        raw = ROOT / "02_created/source_renders" / path.relative_to(ROOT / "02_created")
        raw.parent.mkdir(parents=True, exist_ok=True)
        if not raw.exists():
            shutil.copyfile(path, raw)
        normalized(raw).save(path)
        record["raw_source"] = raw.relative_to(ROOT).as_posix()
        record["normalized_size"] = list(Image.open(path).size)
    (ROOT / "00_index/generation_prompts.json").write_text(json.dumps(prompts, ensure_ascii=False, indent=2), encoding="utf-8")

    transitions = []
    for family, phases in MASKS.items():
        directory = ROOT / "02_created" / family
        paths = [directory / "00_dream.png"] + sorted(directory.glob("0[1-4]_*.png"))
        previous = Image.open(paths[0]).convert("RGBA")
        for index, regions in enumerate(phases, 1):
            target = ImageOps.fit(Image.open(ROOT / "02_created/source_renders" / family / paths[index].name).convert("RGBA"), SIZE, Image.Resampling.LANCZOS)
            mask = Image.new("L", SIZE)
            draw = ImageDraw.Draw(mask)
            for rect in regions:
                x0,y0,x1,y1 = rect
                draw.rectangle((round(x0*SIZE[0]), round(y0*SIZE[1]), round(x1*SIZE[0]), round(y1*SIZE[1])), fill=255)
            mask = mask.filter(ImageFilter.GaussianBlur(5))
            layer = target.copy()
            layer.putalpha(mask)
            layer_path = directory / "layers" / f"reveal_{index}.png"
            layer_path.parent.mkdir(exist_ok=True)
            layer.save(layer_path)
            current = Image.alpha_composite(previous, layer)
            current.convert("RGB").save(paths[index])
            transitions.append({"family": family, "from": paths[index-1].relative_to(ROOT).as_posix(),
                                "to": paths[index].relative_to(ROOT).as_posix(),
                                "layer": layer_path.relative_to(ROOT).as_posix(), "regions_normalized_xyxy": regions,
                                "duration_seconds": 3.2, "conditional_extra_phase": family == "MT-S-5" and index == 4,
                                "timing_status": "new_mod_art_direction_not_measured_original_video",
                                "effects": ["sequential_tile_reveal", "short_vertical_settle", "cyan_edge_trace", "sparse_stone_fragments"],
                                "unchanged_regions_are_pixel_stable": True})
            previous = current
    (ROOT / "00_index/transitions.json").write_text(json.dumps(transitions, ensure_ascii=False, indent=2), encoding="utf-8")

    preview = ROOT / "05_preview"
    preview.mkdir(exist_ok=True)
    player = REPO / "references/official/art/Exusiai_default_default.png" if REPO else preview / "exusiai_scale_reference.png"
    if player.resolve() != (preview / "exusiai_scale_reference.png").resolve():
        shutil.copyfile(player, preview / "exusiai_scale_reference.png")
    source_names = ["圣秩", "祈祷", "礼拜", "求道", "共融", "告解", "丧礼", "朝圣", "解经"]
    backgrounds = [{"label": f"MT-{i+1} {source_names[i]}", "path": p.relative_to(ROOT).as_posix(),
                    "source": f"01_originals/maps/MT-{i+1}/map_preview.png"}
                   for i,p in enumerate(sorted((ROOT / "02_created/backgrounds").glob("*.png")))]
    families = {}
    for family in MASKS:
        directory = ROOT / "02_created" / family
        paths = [directory / "00_dream.png"] + sorted(directory.glob("0[1-4]_*.png"))
        labels = ["梦中", "梦醒"] if family == "MT-10" else ["梦中", "揭示①", "揭示②", "揭示③", "揭示④"]
        families[family] = [{"label": labels[i], "path": p.relative_to(ROOT).as_posix(),
                             "layer": None if i == 0 else (directory / "layers" / f"reveal_{i}.png").relative_to(ROOT).as_posix(),
                             "extra": family == "MT-S-5" and i == 4} for i,p in enumerate(paths)]
    inventory = []
    for path in sorted((ROOT / "01_originals/enemies").glob("*/actions.json")):
        data = json.loads(path.read_bytes())
        identity = data["identity"]
        inventory.append({"id": identity["id"], "name": identity["name"], "stages": identity["stages"],
                          "scope": identity["scope"], "action_count": data["animation_count"],
                          "actions": data["actions"], "folder": path.parent.relative_to(ROOT).as_posix(),
                          "source": identity["page"]})
    assets = {"size": SIZE, "backgrounds": backgrounds, "families": families,
              "boss": {"ground": "02_created/boss/saint_ground.png", "air": "02_created/boss/saint_airborne_concept.png"},
              "boss_bounds": {"ground": Image.open(ROOT / "02_created/boss/saint_ground.png").getbbox(),
                              "air": Image.open(ROOT / "02_created/boss/saint_airborne_concept.png").getbbox()},
              "player": "05_preview/exusiai_scale_reference.png", "player_bounds": Image.open(player).getbbox(), "enemies": inventory,
              "events": [{"label": "主选 · 圣堂相会", "path": "04_event_background/primary_approach_to_sanctuary.png"},
                         {"label": "备选 · 圣徒现身", "path": "04_event_background/alternate_saint_encounter.png"},
                         {"label": "备选 · 仪式深处", "path": "04_event_background/alternate_ritual_depth.png"}]}
    (preview / "assets.js").write_text("var MT_ASSETS = " + json.dumps(assets, ensure_ascii=False, indent=2) + ";\nif (typeof module !== 'undefined') module.exports = MT_ASSETS;\n", encoding="utf-8")
    print("Prepared", len(backgrounds), "backgrounds,", len(transitions), "transitions,", len(inventory), "enemy catalogs")


if __name__ == "__main__":
    main()
