extends SceneTree

const APPEARANCE_ROOT := "res://AK_Exusiai/images/character/spine"
const APPEARANCE_DIRECTORIES := [
    "exusiai/default",
    "exusiai/midnight_delivery",
    "exusiai/wild_operation",
    "exusiai/city_rider",
    "new_covenant/default",
    "new_covenant/wingseekers_song",
]
const CHARACTER_SCENES := {
    "combat": "res://AK_Exusiai/scenes/character/exusiai_visuals.tscn",
    "merchant": "res://AK_Exusiai/scenes/character/exusiai_merchant.tscn",
    "rest_site": "res://AK_Exusiai/scenes/character/exusiai_rest_site.tscn",
    "character_select": \
        "res://AK_Exusiai/scenes/character/exusiai_character_select_bg.tscn",
}

const EXUSIAI_COMBAT_ANIMATIONS := ["Idle", "Attack", "Die"]
const NEW_COVENANT_COMBAT_ANIMATIONS := [
    "Idle",
    "Attack_Loop",
    "Skill_3_Skill",
    "Die",
]
const DEFAULT_BUILD_ANIMATIONS := ["Relax", "Sit", "Interact"]
const SPECIAL_BUILD_ANIMATIONS := ["Special", "Sit", "Interact"]

func _initialize() -> void:
    var failed := false
    for required_class in [
        "SpineSprite",
        "SpineSkeletonDataResource",
        "SpineAtlasResource",
        "SpineSkeletonFileResource",
    ]:
        var found := ClassDB.class_exists(required_class)
        print("SPINE_CLASS ", required_class, "=", found)
        failed = failed or not found

    for directory in APPEARANCE_DIRECTORIES:
        for resource_name in ["combat_skeleton_data.tres", "build_skeleton_data.tres"]:
            var resource_path: String = APPEARANCE_ROOT.path_join(directory) \
                .path_join(resource_name)
            var skeleton_data := ResourceLoader.load(resource_path)
            print("SPINE_RESOURCE ", resource_path, "=", skeleton_data)
            failed = failed or skeleton_data == null
            if skeleton_data != null:
                var expected_animations: Array = _expected_animations(
                    directory,
                    resource_name)
                failed = _validate_animations(
                    resource_path,
                    skeleton_data,
                    expected_animations) or failed

    for scene_name in CHARACTER_SCENES:
        var scene_path: String = CHARACTER_SCENES[scene_name]
        var packed_scene := ResourceLoader.load(scene_path) as PackedScene
        print("SPINE_SCENE ", scene_name, " ", scene_path, "=", packed_scene)
        failed = failed or packed_scene == null

        if packed_scene != null:
            var instance := packed_scene.instantiate()
            var visuals := instance.find_child(
                "AppearancePreview" if scene_name == "character_select" else "Visuals",
                true,
                false)
            print("SPINE_VISUALS_CLASS ", scene_name, "=", \
                visuals.get_class() if visuals != null else "<missing>")
            failed = failed or visuals == null or visuals.get_class() != "SpineSprite"
            instance.free()

    quit(1 if failed else 0)


func _expected_animations(directory: String, resource_name: String) -> Array:
    if resource_name == "combat_skeleton_data.tres":
        return (NEW_COVENANT_COMBAT_ANIMATIONS
            if directory.begins_with("new_covenant/")
            else EXUSIAI_COMBAT_ANIMATIONS)

    return (DEFAULT_BUILD_ANIMATIONS
        if directory.ends_with("/default")
        else SPECIAL_BUILD_ANIMATIONS)


func _validate_animations(
        resource_path: String,
        skeleton_resource: Resource,
        expected_animations: Array) -> bool:
    var failed := false
    var sprite := ClassDB.instantiate("SpineSprite") as Node
    if sprite == null:
        push_error("Unable to instantiate SpineSprite for %s" % resource_path)
        return true

    sprite.set("skeleton_data_res", skeleton_resource)
    var skeleton: Variant = sprite.call("get_skeleton")
    var data: Variant = skeleton.call("get_data") if skeleton != null else null
    for animation_name in expected_animations:
        var found := data != null and data.call("find_animation", animation_name) != null
        print("SPINE_ANIMATION ", resource_path, " ", animation_name, "=", found)
        failed = failed or not found
    sprite.free()
    return failed
