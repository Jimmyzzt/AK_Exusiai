# 制作工具 / Authoring tools

项目构建与本机路径配置见 [开发说明](../docs/archive/DEVELOPMENT.md)。下列命令均从仓库根目录执行。需要 `$GodotExe` 的脚本可先从本机配置读取路径：

```powershell
[xml]$exusiaiLocalProps = Get-Content -LiteralPath .\local.props -Raw
$GodotExe = [string]$exusiaiLocalProps.Project.PropertyGroup.GodotExe
```

常用入口：[卡图管理器](card_art_manager/README.md)、[卡牌特效与音效管理器](card_effect_manager/README.md)。工具与原始参考素材不随 PCK 发布，工具导出的正式资源才进入游戏。

## Item asset generation

Generate exact 64×64 and 256×256 runtime variants for every custom power icon:

Requires Python 3 and Pillow. SVG dimensions are rewritten directly; PNG variants are resized by Pillow.

```powershell
python .\tools\generate_power_icons.py
```

Run from the repository root after replacing a relic, potion, character icon, or its source image:

```powershell
& $GodotExe --headless --path . --script .\tools\generate_item_assets.gd
```

The manifest in `generate_item_assets.gd` maps tracked source images to runtime assets. It crops transparent margins, preserves aspect ratio, places relics on a 256×256 canvas and potions on an 80×80 canvas, then creates a white filled silhouette with a small dilation as the outline image. The runtime character icon is generated at the base game's 85×85 size so the top panel and run-history layout do not expand around the source artwork.

Relic code uses the generated 256×256 main image for both the small icon and large inspection image. Potion code uses one 80×80 main image and one 80×80 outline image, matching RitsuLib's current two-path potion asset profile.
The two new Ancient map icons are generated from the selected logos at 85×85 with matching white outlines. Their map and run-history asset paths can reuse each pair when the events are registered.

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

CONFESS-47's front, back, and build sources can be fetched from its PRTS index and prepared with a local converter executable. The build texture is normalized to its atlas-declared size; the original stays unchanged under `references/official/spine/char_4188_confes/`.

```powershell
python .\tools\fetch_confess47_spine.py
python .\tools\prepare_confess47_spine.py .\tmp\SpineSkeletonDataConverter.exe
```

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

## Card effect and audio manager

```powershell
.\tools\card_effect_manager\run_card_effect_manager.ps1
```

Edit shared visual/audio presets in the standalone Godot window and preview them in a single-player test battle with `exusiai fx on`. Export the approved configuration to `AK_Exusiai/config/card_effects.json`, then build the PCK. Full instructions, dependencies and validation commands are in [the manager README](card_effect_manager/README.md).

## Ancient dialogue validation

Use PowerShell 7 to validate both dialogue tables, including contiguous indices, speakers, buttons, markup, matching locale keys, and Architect ending metadata:

```powershell
.\tools\validate_ancient_dialogue.ps1
.\tools\validate_ancient_dialogue.ps1 -PckPath '<path to deployed AK_Exusiai.pck>'
```

The optional PCK check compares both packed dialogue tables with the source bytes. It does not launch the game or edit saves. Dialogue timing, sources and the in-game checklist are documented in [ANCIENT_DIALOGUE.md](../docs/archive/ANCIENT_DIALOGUE.md).
