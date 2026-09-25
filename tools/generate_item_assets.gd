extends SceneTree

const ALPHA_THRESHOLD := 0.02

const ITEMS := [
    {"source": "references/official/art/模组_证章.png", "output": "AK_Exusiai/images/relics/ExusiaiBadge.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/official/art/模组_能天使的杰作.png", "output": "AK_Exusiai/images/relics/ExusiaiSurprise.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/蚀刻子弹.png", "output": "AK_Exusiai/images/relics/EtchedRound.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/苹果派物流.png", "output": "AK_Exusiai/images/relics/ApplePieLogistics.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/老板奖章.png", "output": "AK_Exusiai/images/relics/BossMedal.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/主-主机.png", "output": "AK_Exusiai/images/relics/LordServer.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/official/art/uniequip_002_angel2.png", "output": "AK_Exusiai/images/relics/PocketSlotMachine.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/日光灯.png", "output": "AK_Exusiai/images/relics/FluorescentLight.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/火力电台.png", "output": "AK_Exusiai/images/relics/FirepowerFm.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/PhotoWithTheLord.png", "output": "AK_Exusiai/images/relics/PhotoWithTheLord.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/EntryPermit.png", "output": "AK_Exusiai/images/relics/EntryPermit.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/StudyTourCertificate.png", "output": "AK_Exusiai/images/relics/StudyTourCertificate.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/LordDrone.png", "output": "AK_Exusiai/images/relics/LordDrone.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/SprayCan.png", "output": "AK_Exusiai/images/relics/SprayCan.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/CactusTart.png", "output": "AK_Exusiai/images/relics/CactusTart.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/PrismaticWings.png", "output": "AK_Exusiai/images/relics/PrismaticWings.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/Confess47.png", "output": "AK_Exusiai/images/relics/Confess47.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/BeaconOfNations.png", "output": "AK_Exusiai/images/relics/BeaconOfNations.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/AK_Exusiai_Ancients素材_2026-09-25/01_拉特兰/10_律法/光环_萨卡兹肉鸽_指定图标.png", "output": "AK_Exusiai/images/relics/TheLaw.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/PenguinLogisticsId.png", "output": "AK_Exusiai/images/relics/PenguinLogisticsId.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/AFewFineVintages.png", "output": "AK_Exusiai/images/relics/AFewFineVintages.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/BlackCard.png", "output": "AK_Exusiai/images/relics/BlackCard.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/MasterTape.png", "output": "AK_Exusiai/images/relics/MasterTape.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/IllGottenGains.png", "output": "AK_Exusiai/images/relics/IllGottenGains.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/CompanyVan.png", "output": "AK_Exusiai/images/relics/CompanyVan.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/ReturnToSender.png", "output": "AK_Exusiai/images/relics/ReturnToSender.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/DjDeck.png", "output": "AK_Exusiai/images/relics/DjDeck.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/AK_Exusiai_Ancients素材_2026-09-25/02_大帝/19_珍藏的唱片/黑夜呢喃_傀影肉鸽_指定图标.png", "output": "AK_Exusiai/images/relics/PrizedRecord.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/ancients_generated/BossBusinessCard.png", "output": "AK_Exusiai/images/relics/BossBusinessCard.png", "size": 256, "padding": 16, "radius": 4},
    {"source": "references/free/art/药水形状的弹夹.png", "output": "AK_Exusiai/images/potions/PotionShapedMag.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/free/art/大帝的珍藏.png", "output": "AK_Exusiai/images/potions/EmperorsStash.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/free/art/瓶装光环.png", "output": "AK_Exusiai/images/potions/BottledHalo.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/free/art/ancients_generated/UrsusBeluga.png", "output": "AK_Exusiai/images/potions/UrsusBeluga.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/free/art/ancients_generated/GaulChardonnay.png", "output": "AK_Exusiai/images/potions/GaulChardonnay.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/free/art/ancients_generated/YanFenjiu.png", "output": "AK_Exusiai/images/potions/YanFenjiu.png", "size": 80, "padding": 4, "radius": 2},
    {"source": "references/official/art/Logo_拉特兰.png", "output": "AK_Exusiai/images/ancients/laterano/map_icon.png", "size": 85, "padding": 5, "radius": 2},
    {"source": "references/official/art/企鹅物流logo.png", "output": "AK_Exusiai/images/ancients/emperor/map_icon.png", "size": 85, "padding": 5, "radius": 2},
]

const COPIES := [
    {"source": "references/official/art/Exusiai_icon.png", "output": "AK_Exusiai/images/character/exusiai_icon.png", "size": 85},
]

func _initialize() -> void:
    var failed := false
    for item in ITEMS:
        if not _generate_item(item):
            failed = true
    for item in COPIES:
        if not _copy_png(item.source, item.output, item.get("size", 0)):
            failed = true

    if failed:
        push_error("One or more item assets failed to generate.")
        quit(1)
        return

    print("Generated %d item image pairs and %d copied assets." % [ITEMS.size(), COPIES.size()])
    quit()

func _generate_item(item: Dictionary) -> bool:
    var source_path := _absolute(item.source)
    var image := Image.new()
    var error := image.load(source_path)
    if error != OK:
        push_error("Cannot load source image: %s (%s)" % [source_path, error_string(error)])
        return false

    var bounds := _find_alpha_bounds(image)
    if bounds.size == Vector2i.ZERO:
        push_error("Source image has no visible pixels: %s" % source_path)
        return false

    var cropped := image.get_region(bounds)
    var size: int = item.size
    var padding: int = item.padding
    var available: int = size - padding * 2
    var scale: float = min(float(available) / cropped.get_width(), float(available) / cropped.get_height())
    var width: int = max(1, roundi(cropped.get_width() * scale))
    var height: int = max(1, roundi(cropped.get_height() * scale))
    cropped.resize(width, height, Image.INTERPOLATE_LANCZOS)

    var main := Image.create(size, size, false, Image.FORMAT_RGBA8)
    var position := Vector2i((size - width) / 2, (size - height) / 2)
    main.blit_rect(cropped, Rect2i(Vector2i.ZERO, cropped.get_size()), position)

    var output_path: String = item.output
    var outline_path := output_path.trim_suffix(".png") + "Outline.png"
    if not _save_png(main, output_path):
        return false

    var outline := _make_white_silhouette(main, item.radius)
    if not _save_png(outline, outline_path):
        return false

    print("Generated %s and %s" % [output_path, outline_path])
    return true

func _copy_png(source_relative: String, output_relative: String, size: int = 0) -> bool:
    var image := Image.new()
    var error := image.load(_absolute(source_relative))
    if error != OK:
        push_error("Cannot load source image: %s (%s)" % [source_relative, error_string(error)])
        return false
    if size > 0 and image.get_size() != Vector2i(size, size):
        image.resize(size, size, Image.INTERPOLATE_LANCZOS)
    return _save_png(image, output_relative)

func _find_alpha_bounds(image: Image) -> Rect2i:
    var min_x := image.get_width()
    var min_y := image.get_height()
    var max_x := -1
    var max_y := -1
    for y in image.get_height():
        for x in image.get_width():
            if image.get_pixel(x, y).a <= ALPHA_THRESHOLD:
                continue
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x = max(max_x, x)
            max_y = max(max_y, y)
    if max_x < min_x or max_y < min_y:
        return Rect2i()
    return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)

func _make_white_silhouette(main: Image, radius: int) -> Image:
    var outline := Image.create(main.get_width(), main.get_height(), false, Image.FORMAT_RGBA8)
    for y in main.get_height():
        for x in main.get_width():
            var alpha := main.get_pixel(x, y).a
            if alpha <= ALPHA_THRESHOLD:
                continue
            for offset_y in range(-radius, radius + 1):
                for offset_x in range(-radius, radius + 1):
                    if offset_x * offset_x + offset_y * offset_y > radius * radius:
                        continue
                    var target_x := x + offset_x
                    var target_y := y + offset_y
                    if target_x < 0 or target_y < 0 or target_x >= outline.get_width() or target_y >= outline.get_height():
                        continue
                    var old_alpha := outline.get_pixel(target_x, target_y).a
                    outline.set_pixel(target_x, target_y, Color(1.0, 1.0, 1.0, max(old_alpha, alpha)))
    return outline

func _save_png(image: Image, relative_path: String) -> bool:
    var absolute_path := _absolute(relative_path)
    var error := DirAccess.make_dir_recursive_absolute(absolute_path.get_base_dir())
    if error != OK:
        push_error("Cannot create output directory: %s (%s)" % [absolute_path.get_base_dir(), error_string(error)])
        return false
    error = image.save_png(absolute_path)
    if error != OK:
        push_error("Cannot save image: %s (%s)" % [absolute_path, error_string(error)])
        return false
    return true

func _absolute(relative_path: String) -> String:
    return ProjectSettings.globalize_path("res://" + relative_path)
