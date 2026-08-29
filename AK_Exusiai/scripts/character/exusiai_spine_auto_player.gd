extends Node

const APPEARANCE_CONFIG_PATH := "user://AK_Exusiai/appearance.cfg"
const BUILD_SKELETON_PATHS := [
	[
		"res://AK_Exusiai/images/character/spine/exusiai/default/build_skeleton_data.tres",
		"res://AK_Exusiai/images/character/spine/exusiai/midnight_delivery/build_skeleton_data.tres",
		"res://AK_Exusiai/images/character/spine/exusiai/wild_operation/build_skeleton_data.tres",
		"res://AK_Exusiai/images/character/spine/exusiai/city_rider/build_skeleton_data.tres",
	],
	[
		"res://AK_Exusiai/images/character/spine/new_covenant/default/build_skeleton_data.tres",
		"res://AK_Exusiai/images/character/spine/new_covenant/wingseekers_song/build_skeleton_data.tres",
	],
]

@export var animation_name := "Relax"
@export var use_special_for_non_default := false


func _ready() -> void:
	_initialize_spine.call_deferred()


func _initialize_spine() -> void:
	var selection := _load_selection()
	var character_index: int = selection.x
	var skin_index: int = selection.y
	var skeleton_path: String = BUILD_SKELETON_PATHS[character_index][skin_index]
	var skeleton_data := load(skeleton_path)
	if skeleton_data == null:
		push_error("Unable to load Exusiai merchant appearance: %s" % skeleton_path)
		return

	get_parent().set("skeleton_data_res", skeleton_data)
	var selected_animation := (
		"Special" if use_special_for_non_default and skin_index > 0
		else animation_name
	)
	await _start_animation(selected_animation)


func _load_selection() -> Vector2i:
	var config := ConfigFile.new()
	var character_index := 0
	var skin_index := 0
	var loaded := config.load(APPEARANCE_CONFIG_PATH) == OK
	if loaded:
		character_index = int(config.get_value("character", "family", 0))
	character_index = clampi(character_index, 0, BUILD_SKELETON_PATHS.size() - 1)
	if loaded:
		skin_index = int(config.get_value("character", "skin_%d" % character_index, 0))
	skin_index = clampi(skin_index, 0, BUILD_SKELETON_PATHS[character_index].size() - 1)
	return Vector2i(character_index, skin_index)


func _start_animation(selected_animation: String) -> void:
	var sprite := get_parent()
	while is_instance_valid(sprite) and sprite.is_inside_tree():
		var animation_state: Variant = sprite.get_animation_state()
		if animation_state != null:
			animation_state.set_animation(selected_animation, true, 0)
			return
		await get_tree().process_frame
