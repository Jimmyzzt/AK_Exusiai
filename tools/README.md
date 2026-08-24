# Item asset generation

Run from the repository root after replacing a relic, potion, character icon, or its source image:

```powershell
& $GodotExe --headless --path . --script .\tools\generate_item_assets.gd
```

The manifest in `generate_item_assets.gd` maps tracked source images to runtime assets. It crops transparent margins, preserves aspect ratio, places relics on a 256×256 canvas and potions on an 80×80 canvas, then creates a white filled silhouette with a small dilation as the outline image.

Relic code uses the generated 256×256 main image for both the small icon and large inspection image. Potion code uses one 80×80 main image and one 80×80 outline image, matching RitsuLib's current two-path potion asset profile.

## Card art manager

Run the local Godot card-art GUI from the repository root:

```powershell
.\tools\card_art_manager\run_card_art_manager.ps1
```

It scans the tracked reference folders, provides pan/zoom crop previews, icon backgrounds and procedural placeholders, saves a JSON manifest, and exports normal card portraits at 250×190 or Ancient portraits at 250×351. See `tools/card_art_manager/README.md` for the workflow.
