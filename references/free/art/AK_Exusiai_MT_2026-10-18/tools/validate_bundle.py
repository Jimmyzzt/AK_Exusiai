"""Check the delivered source links, original animation triplets and stage invariants."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

ROOT=Path(__file__).resolve().parents[1]
failures=[]
checks=[]


def require(condition,message):
    checks.append(message)
    if not condition: failures.append(message)


stages=json.loads((ROOT/"00_index/stages.json").read_bytes())
regular={e["id"] for s in stages if s["code"]!="MT-S-5" for e in s["enemies"] if "hlslp" not in e["id"]}
require(len(regular)==16,"MT-1 through MT-10 have 16 unique non-mechanism enemies")
catalog=json.loads((ROOT/"03_animation_catalog/all_actions.json").read_bytes())
require(len(catalog)==150,"150 complete original animation entries")
require(len({a["enemy_id"] for a in catalog})==23,"23 enemy animation sets including MT-S-5 supplements")
require(len([a for a in catalog if a["enemy_id"]=="enemy_1567_pope"])==31,"Saint has 31 original actions")
for directory in (ROOT/"01_originals/enemies").iterdir():
    identity=json.loads((directory/"identity.json").read_bytes())
    if identity["category"]=="mechanism": continue
    key=identity["id"]
    for extension in ("skel","atlas","png"):
        require((directory/(key+"."+extension)).is_file(),key+" has "+extension)
    parsed=json.loads((ROOT/"03_animation_catalog/parsed"/(key+".json")).read_bytes())
    actions=json.loads((directory/"actions.json").read_bytes())
    require(set(parsed["animations"])=={a["animation"] for a in actions["actions"]},key+" complete action-name correspondence")
    for line in (directory/(key+".atlas")).read_text(encoding="utf-8-sig").splitlines():
        if line and line==line.strip() and line.lower().endswith(".png"):
            require((directory/line).is_file(),key+" atlas page exists: "+line)
for directory in ("backgrounds","MT-10","MT-S-5"):
    for path in (ROOT/"02_created"/directory).rglob("*.png"):
        require(Image.open(path).size==(1920,1080),str(path.relative_to(ROOT))+" is 1920x1080")
for name in ("saint_ground.png","saint_airborne_concept.png"):
    image=Image.open(ROOT/"02_created/boss"/name)
    require(image.mode=="RGBA" and image.getchannel("A").getextrema()==(0,255),name+" has real alpha")
    box=image.getbbox()
    require(box[0]>=100 and box[1]>=100 and box[2]<=1436 and box[3]<=1436,name+" has safe transparent margins")
for family in ("MT-10","MT-S-5"):
    directory=ROOT/"02_created"/family
    paths=[directory/"00_dream.png"]+sorted(directory.glob("0[1-4]_*.png"))
    previous=Image.open(paths[0]).convert("RGBA")
    core=(int(1920*.30),int(1080*.45),int(1920*.70),int(1080*.63))
    initial_core=previous.crop(core)
    for i,path in enumerate(paths[1:],1):
        image=Image.open(path).convert("RGBA")
        layer=Image.open(directory/"layers"/f"reveal_{i}.png").convert("RGBA")
        composed=Image.alpha_composite(previous,layer)
        require(ImageChops.difference(composed.convert("RGB"),image.convert("RGB")).getbbox() is None,family+f" transition {i} endpoint matches additive layer")
        require(ImageChops.difference(initial_core.convert("RGB"),image.crop(core).convert("RGB")).getbbox() is None,family+f" transition {i} preserves core standing platform pixels")
        previous=image
downloads=json.loads((ROOT/"00_index/downloads.json").read_bytes())
for record in downloads:
    path=ROOT/record["path"]
    if record["kind"] in ("selected_stage_inventory","download_manifest"): continue
    if record["kind"]=="download_audit":continue
    require(path.exists(),"download exists: "+record["path"])
    if path.exists(): require(hashlib.sha256(path.read_bytes()).hexdigest()==record["sha256"],"download bytes unchanged: "+record["path"])
for path in ROOT.rglob("*.png"):
    with Image.open(path) as image: image.verify()
    checks.append("PNG decodes: "+path.relative_to(ROOT).as_posix())
for family,count in (("MT-10",1),("MT-S-5",4)):
    for i in range(1,count+1):
        suffix="_conditional" if i==4 else ""
        path=ROOT/"05_preview/exports"/f"{family}_transition_{i}{suffix}.mp4"
        require(path.exists() and path.stat().st_size>10000,path.name+" encoded")
summary={"checks":len(checks),"failures":failures,"regular_enemy_count":len(regular),"all_enemy_sets":23,
         "actions":len(catalog),"saint_actions":31,"standard_backgrounds":16,"transition_clips":5,
         "cg_count":len(list((ROOT/"01_originals/event_cg").glob("*.png"))),
         "note":"Local art validation only, not game or original-video playback validation"}
(ROOT/"00_index/validation.json").write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding="utf-8")
print(json.dumps(summary,ensure_ascii=False,indent=2))
raise SystemExit(1 if failures else 0)
