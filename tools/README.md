# Item asset generation

Run from the repository root after replacing a relic, potion, character icon, or its source image:

```powershell
& $GodotExe --headless --path . --script .\tools\generate_item_assets.gd
```

The manifest in `generate_item_assets.gd` maps tracked source images to runtime assets. It crops transparent margins, preserves aspect ratio, places relics on a 256×256 canvas and potions on an 80×80 canvas, then creates a white filled silhouette with a small dilation as the outline image. The runtime character icon is generated at the base game's 85×85 size so the top panel and run-history layout do not expand around the source artwork.

Relic code uses the generated 256×256 main image for both the small icon and large inspection image. Potion code uses one 80×80 main image and one 80×80 outline image, matching RitsuLib's current two-path potion asset profile.

## Spine character assets

Use the Windows Spine-Godot extension built for Godot 4.5.1 as a local editor dependency. Copy its complete directory into the repository root as `SpineGodotExtension4.5.1/`; that directory is ignored by Git and excluded from the exported PCK. The game already supplies its own Spine runtime, so the editor extension must not be shipped in the mod pack.

After copying the extension, let Godot scan and import the `.atlas`, `.png`, and converted `.skel` files:

```powershell
& $GodotExe --headless --editor --path . --import
& $GodotExe --headless --path . --script .\tools\validate_spine_setup.gd
```

The current ordinary Godot 4.5.1 Mono editor passes this validation; MegaDot is not required for the checked-in default Exusiai Spine scene. Keep `editor/export/convert_text_resources_to_binary=false` in `project.godot`, because binary conversion breaks the external custom Spine resource dependencies when they are loaded from the mod PCK.

PRTS source skeletons are Spine 3.8.99. Convert runtime copies to the game's Spine 4.2.43 format with [SpineSkeletonDataConverter](https://github.com/wang606/SpineSkeletonDataConverter), for example:

```powershell
& .\SpineSkeletonDataConverter.exe input.skel output_42.skel -v 4.2.43
```

Original downloads and their hashes remain under `references/official/spine/`; only converted runtime copies belong under `AK_Exusiai/images/character/spine/`.

PRTS `build` textures are stored at two-thirds of the pixel dimensions declared by their `.atlas` files. Copying one unchanged makes the runtime sample the wrong rectangles and displays the character as scattered fragments. Keep the downloaded PNG unchanged under `references/`, then normalize only the runtime copy to the atlas page size:

```powershell
& $GodotExe --headless --path . --script .\tools\normalize_spine_texture.gd -- `
  .\references\official\spine\char_103_angel\defaultskin\build\build_char_103_angel.atlas `
  .\references\official\spine\char_103_angel\defaultskin\build\build_char_103_angel.png `
  .\AK_Exusiai\images\character\spine\default\build_char_103_angel.png
```

The script reads the atlas page dimensions and applies Lanczos resizing only when the source dimensions differ. Front-model textures currently match their atlas dimensions and do not need this step.

## Card art manager

Run the local Godot card-art GUI from the repository root:

```powershell
.\tools\card_art_manager\run_card_art_manager.ps1
```

It scans and groups the tracked reference folders, provides scalable UI, file metadata in list view, resizable sidebars, type-aware card-frame guides, independent gradient/texture backgrounds, a unified background/material/placeholder compositor, pan/zoom/rotate/flip controls, bounded thumbnail caching with manual cleanup, and responsive batch-export progress. Export resolution is selectable from 1× to 4× while preserving the official aspect ratios, with 500×380 normal and 500×702 Ancient portraits as the default. See `tools/card_art_manager/README.md` for the workflow and cache locations.
