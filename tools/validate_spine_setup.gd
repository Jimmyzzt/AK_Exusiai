extends SceneTree

const SKELETON_DATA_PATH := \
    "res://AK_Exusiai/images/character/spine/default/exusiai_default_skeleton_data.tres"
const BUILD_SKELETON_DATA_PATH := \
    "res://AK_Exusiai/images/character/spine/default/build_exusiai_default_skeleton_data.tres"
const CHARACTER_SCENES := {
    "combat": "res://AK_Exusiai/scenes/character/exusiai_visuals.tscn",
    "merchant": "res://AK_Exusiai/scenes/character/exusiai_merchant.tscn",
    "rest_site": "res://AK_Exusiai/scenes/character/exusiai_rest_site.tscn",
}

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

    for resource_path in [SKELETON_DATA_PATH, BUILD_SKELETON_DATA_PATH]:
        var skeleton_data := ResourceLoader.load(resource_path)
        print("SPINE_RESOURCE ", resource_path, "=", skeleton_data)
        failed = failed or skeleton_data == null

    for scene_name in CHARACTER_SCENES:
        var scene_path: String = CHARACTER_SCENES[scene_name]
        var packed_scene := ResourceLoader.load(scene_path) as PackedScene
        print("SPINE_SCENE ", scene_name, " ", scene_path, "=", packed_scene)
        failed = failed or packed_scene == null

        if packed_scene != null:
            var instance := packed_scene.instantiate()
            var visuals := instance.find_child("Visuals", true, false)
            print("SPINE_VISUALS_CLASS ", scene_name, "=", \
                visuals.get_class() if visuals != null else "<missing>")
            failed = failed or visuals == null or visuals.get_class() != "SpineSprite"
            instance.free()

    quit(1 if failed else 0)
