extends Control

const MANIFEST_PATH := "res://tools/card_art_manager/card_art_manifest.json"
const UI_SETTINGS_PATH := "user://card_art_manager_ui.json"
const CARD_SOURCE_DIR := "res://AK_ExusiaiCode/Cards"
const LOCALIZATION_PATH := "res://AK_Exusiai/localization/zhs/cards.json"
const DEFAULT_OUTPUT_DIR := "res://AK_Exusiai/images/cards"
const NORMAL_SIZE := Vector2i(250, 190)
const ANCIENT_SIZE := Vector2i(250, 351)
const SUPPORTED_EXTENSIONS := ["png", "jpg", "jpeg", "webp", "svg"]
const DEFAULT_ASSET_ROOTS := [
	"references/official/art",
	"references/official/asset",
	"references/free/art",
]
const ALL_FOLDERS := "__all__"
const UI_SCALES := [1.0, 1.25, 1.5, 1.75, 2.0]
const BACKGROUNDS := {
	"laterano_sunset": {"label": "拉特兰夕照", "top": "3c2059", "bottom": "ee9558"},
	"angel_blue": {"label": "天使蓝光", "top": "10273f", "bottom": "52b8d6"},
	"penguin_night": {"label": "企鹅物流夜色", "top": "182330", "bottom": "677485"},
	"red_alert": {"label": "战斗警报", "top": "35151d", "bottom": "d35a48"},
	"holy_gold": {"label": "圣城金辉", "top": "493918", "bottom": "e3c06b"},
	"custom": {"label": "自定义", "top": "2b2340", "bottom": "bd6c59"},
}
const MOTIFS := {
	"auto": "自动",
	"crosshair": "准星",
	"halo": "光环",
	"burst": "爆发",
	"package": "包裹",
	"speedlines": "速度线",
}

var _manifest: Dictionary = {}
var _ui_settings: Dictionary = {
	"scale": 1.0,
	"asset_view": "list",
	"folder": ALL_FOLDERS,
	"outer_split_offset": 340,
	"inner_split_offset": -340,
}
var _cards: Array[Dictionary] = []
var _assets: Array[String] = []
var _filtered_assets: Array[String] = []
var _source_cache: Dictionary = {}
var _thumbnail_cache: Dictionary = {}
var _thumbnail_queue: Array[Dictionary] = []
var _current_card := ""
var _loading_ui := false
var _render_queued := false
var _is_smoke_test := false

var _asset_list: ItemList
var _asset_search: LineEdit
var _asset_view_option: OptionButton
var _folder_option: OptionButton
var _ui_scale_option: OptionButton
var _outer_split: HSplitContainer
var _inner_split: HSplitContainer
var _preview: CardArtPreview
var _card_option: OptionButton
var _mode_option: OptionButton
var _background_option: OptionButton
var _motif_option: OptionButton
var _top_color: ColorPickerButton
var _bottom_color: ColorPickerButton
var _zoom_slider: HSlider
var _zoom_value: Label
var _source_label: Label
var _status_label: Label
var _progress_label: Label
var _ancient_label: Label
var _add_files_dialog: FileDialog
var _add_folder_dialog: FileDialog
var _save_timer: Timer


func _ready() -> void:
	_is_smoke_test = "--card-art-smoke-test" in OS.get_cmdline_user_args()
	get_window().title = "AK_Exusiai 卡图管理器"
	get_window().size = Vector2i(1440, 860)
	get_window().min_size = Vector2i(1100, 680)
	_load_ui_settings()
	_apply_ui_scale(float(_ui_settings.scale), true)
	_build_ui()
	_load_manifest()
	_load_cards()
	_scan_manifest_assets()
	_rebuild_folder_options()
	_apply_asset_view(String(_ui_settings.asset_view))
	_rebuild_asset_list()
	_rebuild_card_options()
	_restore_split_offsets.call_deferred()
	if not _cards.is_empty():
		_select_card_by_index(0)
	_set_status("就绪。清单会在修改后自动保存。", false)
	set_process(true)
	if _is_smoke_test:
		_run_smoke_test.call_deferred()


func _process(_delta: float) -> void:
	if _thumbnail_queue.is_empty():
		return
	var request: Dictionary = _thumbnail_queue.pop_front()
	var index: int = request.index
	var path: String = request.path
	if index < 0 or index >= _asset_list.item_count:
		return
	if _asset_list.get_item_tooltip(index) != path:
		return
	var thumbnail := _get_thumbnail(path)
	if thumbnail != null:
		_asset_list.set_item_icon(index, thumbnail)


func _exit_tree() -> void:
	if _save_timer != null and not _save_timer.is_stopped():
		_save_manifest()
	if not _is_smoke_test:
		_save_ui_settings()


func _build_ui() -> void:
	var background := ColorRect.new()
	background.color = Color("20232b")
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(background)

	var root := VBoxContainer.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 8)
	root.offset_left = 10
	root.offset_top = 10
	root.offset_right = -10
	root.offset_bottom = -10
	add_child(root)

	var toolbar := HBoxContainer.new()
	toolbar.add_theme_constant_override("separation", 8)
	root.add_child(toolbar)

	var title := Label.new()
	title.text = "能天使卡图管理器"
	title.add_theme_font_size_override("font_size", 22)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	toolbar.add_child(title)

	var scale_label := Label.new()
	scale_label.text = "界面缩放"
	toolbar.add_child(scale_label)
	_ui_scale_option = OptionButton.new()
	for scale in UI_SCALES:
		var scale_index := _ui_scale_option.item_count
		_ui_scale_option.add_item("%d%%" % roundi(float(scale) * 100.0))
		_ui_scale_option.set_item_metadata(scale_index, scale)
	_ui_scale_option.item_selected.connect(_on_ui_scale_selected)
	toolbar.add_child(_ui_scale_option)
	_select_option_by_float_metadata(_ui_scale_option, float(_ui_settings.scale))

	var add_files := Button.new()
	add_files.text = "添加素材"
	add_files.pressed.connect(_show_add_files_dialog)
	toolbar.add_child(add_files)

	var add_folder := Button.new()
	add_folder.text = "添加文件夹"
	add_folder.pressed.connect(_show_add_folder_dialog)
	toolbar.add_child(add_folder)

	var refresh := Button.new()
	refresh.text = "重新扫描"
	refresh.pressed.connect(_refresh_assets)
	toolbar.add_child(refresh)

	var save_manifest := Button.new()
	save_manifest.text = "保存清单"
	save_manifest.pressed.connect(_save_manifest)
	toolbar.add_child(save_manifest)

	_outer_split = HSplitContainer.new()
	_outer_split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_outer_split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_outer_split.dragged.connect(_on_split_dragged)
	root.add_child(_outer_split)

	var left_panel := _make_panel_container()
	left_panel.custom_minimum_size.x = 240
	_outer_split.add_child(left_panel)
	var left := VBoxContainer.new()
	left.add_theme_constant_override("separation", 8)
	left_panel.add_child(left)
	var asset_header := HBoxContainer.new()
	asset_header.add_theme_constant_override("separation", 6)
	left.add_child(asset_header)
	var asset_title := Label.new()
	asset_title.text = "素材区"
	asset_title.add_theme_font_size_override("font_size", 18)
	asset_title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	asset_header.add_child(asset_title)
	_asset_view_option = OptionButton.new()
	_asset_view_option.add_item("列表")
	_asset_view_option.set_item_metadata(0, "list")
	_asset_view_option.add_item("紧凑")
	_asset_view_option.set_item_metadata(1, "compact")
	_asset_view_option.item_selected.connect(_on_asset_view_selected)
	asset_header.add_child(_asset_view_option)
	_select_option_by_metadata(_asset_view_option, String(_ui_settings.asset_view))
	_folder_option = OptionButton.new()
	_folder_option.tooltip_text = "按素材所在文件夹分类筛选"
	_folder_option.item_selected.connect(_on_folder_selected)
	left.add_child(_folder_option)
	_asset_search = LineEdit.new()
	_asset_search.placeholder_text = "筛选文件名……"
	_asset_search.text_changed.connect(_on_asset_search_changed)
	left.add_child(_asset_search)
	_asset_list = ItemList.new()
	_asset_list.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_asset_list.item_selected.connect(_on_asset_selected)
	_asset_list.resized.connect(_update_asset_columns)
	left.add_child(_asset_list)

	_inner_split = HSplitContainer.new()
	_inner_split.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_inner_split.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_inner_split.dragged.connect(_on_split_dragged)
	_outer_split.add_child(_inner_split)

	var middle_panel := _make_panel_container()
	middle_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	middle_panel.custom_minimum_size.x = 420
	_inner_split.add_child(middle_panel)
	var middle := VBoxContainer.new()
	middle.add_theme_constant_override("separation", 8)
	middle_panel.add_child(middle)
	var preview_header := HBoxContainer.new()
	middle.add_child(preview_header)
	var preview_title := Label.new()
	preview_title.text = "预览区 · 滚轮缩放 / 左键拖拽"
	preview_title.add_theme_font_size_override("font_size", 18)
	preview_title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	preview_header.add_child(preview_title)
	var reset_transform := Button.new()
	reset_transform.text = "复位构图"
	reset_transform.pressed.connect(_reset_transform)
	preview_header.add_child(reset_transform)
	_preview = CardArtPreview.new()
	_preview.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_preview.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_preview.transform_changed.connect(_on_preview_transform_changed)
	middle.add_child(_preview)

	var export_bar := HBoxContainer.new()
	export_bar.add_theme_constant_override("separation", 8)
	middle.add_child(export_bar)
	var previous := Button.new()
	previous.text = "上一张"
	previous.pressed.connect(func() -> void: _step_card(-1))
	export_bar.add_child(previous)
	var next := Button.new()
	next.text = "下一张"
	next.pressed.connect(func() -> void: _step_card(1))
	export_bar.add_child(next)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	export_bar.add_child(spacer)
	var export_current := Button.new()
	export_current.text = "导出当前卡图"
	export_current.pressed.connect(_export_current)
	export_bar.add_child(export_current)
	var export_all := Button.new()
	export_all.text = "批量导出已配置"
	export_all.pressed.connect(_export_all_configured)
	export_bar.add_child(export_all)

	var right_panel := _make_panel_container()
	right_panel.custom_minimum_size.x = 300
	_inner_split.add_child(right_panel)
	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	right_panel.add_child(scroll)
	var right := VBoxContainer.new()
	right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	right.add_theme_constant_override("separation", 8)
	scroll.add_child(right)
	var settings_title := Label.new()
	settings_title.text = "卡牌配置"
	settings_title.add_theme_font_size_override("font_size", 18)
	right.add_child(settings_title)

	right.add_child(_make_field_label("对应卡牌"))
	_card_option = OptionButton.new()
	_card_option.item_selected.connect(_select_card_by_index)
	right.add_child(_card_option)
	_ancient_label = Label.new()
	_ancient_label.add_theme_color_override("font_color", Color("e8ba67"))
	right.add_child(_ancient_label)

	right.add_child(_make_field_label("素材类型"))
	_mode_option = OptionButton.new()
	_mode_option.add_item("插画裁切", 0)
	_mode_option.set_item_metadata(0, "crop")
	_mode_option.add_item("透明图标", 1)
	_mode_option.set_item_metadata(1, "icon")
	_mode_option.add_item("程序化占位", 2)
	_mode_option.set_item_metadata(2, "placeholder")
	_mode_option.item_selected.connect(_on_form_changed)
	right.add_child(_mode_option)

	right.add_child(_make_field_label("当前素材"))
	_source_label = Label.new()
	_source_label.text = "未选择"
	_source_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_source_label.add_theme_color_override("font_color", Color("aeb5c4"))
	right.add_child(_source_label)
	var clear_source := Button.new()
	clear_source.text = "清除素材关联"
	clear_source.pressed.connect(_clear_source)
	right.add_child(clear_source)

	right.add_child(_make_field_label("图标背景"))
	_background_option = OptionButton.new()
	for key in BACKGROUNDS:
		var index := _background_option.item_count
		_background_option.add_item(BACKGROUNDS[key].label)
		_background_option.set_item_metadata(index, key)
	_background_option.item_selected.connect(_on_background_changed)
	right.add_child(_background_option)

	right.add_child(_make_field_label("占位图形"))
	_motif_option = OptionButton.new()
	for key in MOTIFS:
		var index := _motif_option.item_count
		_motif_option.add_item(MOTIFS[key])
		_motif_option.set_item_metadata(index, key)
	_motif_option.item_selected.connect(_on_form_changed)
	right.add_child(_motif_option)

	var color_row := HBoxContainer.new()
	color_row.add_theme_constant_override("separation", 8)
	right.add_child(color_row)
	var top_box := VBoxContainer.new()
	top_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	color_row.add_child(top_box)
	top_box.add_child(_make_field_label("顶部颜色"))
	_top_color = ColorPickerButton.new()
	_top_color.custom_minimum_size.y = 36
	_top_color.color_changed.connect(_on_custom_color_changed)
	top_box.add_child(_top_color)
	var bottom_box := VBoxContainer.new()
	bottom_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	color_row.add_child(bottom_box)
	bottom_box.add_child(_make_field_label("底部颜色"))
	_bottom_color = ColorPickerButton.new()
	_bottom_color.custom_minimum_size.y = 36
	_bottom_color.color_changed.connect(_on_custom_color_changed)
	bottom_box.add_child(_bottom_color)

	right.add_child(_make_field_label("缩放"))
	var zoom_row := HBoxContainer.new()
	right.add_child(zoom_row)
	_zoom_slider = HSlider.new()
	_zoom_slider.min_value = 0.2
	_zoom_slider.max_value = 4.0
	_zoom_slider.step = 0.01
	_zoom_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zoom_slider.value_changed.connect(_on_zoom_changed)
	zoom_row.add_child(_zoom_slider)
	_zoom_value = Label.new()
	_zoom_value.custom_minimum_size.x = 52
	_zoom_value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	zoom_row.add_child(_zoom_value)

	var hint := Label.new()
	hint.text = "插画模式：1.00 表示刚好铺满画布。\n图标模式：1.00 表示默认图标大小。"
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	hint.add_theme_color_override("font_color", Color("858d9e"))
	right.add_child(hint)

	_progress_label = Label.new()
	_progress_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	right.add_child(_progress_label)

	_status_label = Label.new()
	_status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_status_label.custom_minimum_size.y = 32
	root.add_child(_status_label)

	_add_files_dialog = FileDialog.new()
	_add_files_dialog.access = FileDialog.ACCESS_FILESYSTEM
	_add_files_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILES
	_add_files_dialog.use_native_dialog = true
	_add_files_dialog.filters = PackedStringArray([
		"*.png, *.jpg, *.jpeg, *.webp, *.svg ; 支持的图片",
	])
	_add_files_dialog.files_selected.connect(_on_files_added)
	add_child(_add_files_dialog)

	_add_folder_dialog = FileDialog.new()
	_add_folder_dialog.access = FileDialog.ACCESS_FILESYSTEM
	_add_folder_dialog.file_mode = FileDialog.FILE_MODE_OPEN_DIR
	_add_folder_dialog.use_native_dialog = true
	_add_folder_dialog.dir_selected.connect(_on_folder_added)
	add_child(_add_folder_dialog)

	_save_timer = Timer.new()
	_save_timer.one_shot = true
	_save_timer.wait_time = 0.3
	_save_timer.timeout.connect(_save_manifest)
	add_child(_save_timer)


func _make_panel_container() -> PanelContainer:
	var panel := PanelContainer.new()
	panel.add_theme_constant_override("margin_left", 10)
	panel.add_theme_constant_override("margin_top", 10)
	panel.add_theme_constant_override("margin_right", 10)
	panel.add_theme_constant_override("margin_bottom", 10)
	return panel


func _make_field_label(text: String) -> Label:
	var label := Label.new()
	label.text = text
	label.add_theme_color_override("font_color", Color("d4d8e2"))
	return label


func _load_ui_settings() -> void:
	if not FileAccess.file_exists(UI_SETTINGS_PATH):
		return
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(UI_SETTINGS_PATH))
	if parsed is not Dictionary:
		return
	for key in parsed:
		if _ui_settings.has(key):
			_ui_settings[key] = parsed[key]
	var scale := float(_ui_settings.get("scale", 1.0))
	if scale not in UI_SCALES:
		_ui_settings.scale = 1.0
	var asset_view := String(_ui_settings.get("asset_view", "list"))
	if asset_view not in ["list", "compact"]:
		_ui_settings.asset_view = "list"


func _save_ui_settings() -> void:
	if _outer_split != null:
		_ui_settings.outer_split_offset = _outer_split.split_offset
	if _inner_split != null:
		_ui_settings.inner_split_offset = _inner_split.split_offset
	var file := FileAccess.open(UI_SETTINGS_PATH, FileAccess.WRITE)
	if file == null:
		return
	file.store_string(JSON.stringify(_ui_settings, "  ", false) + "\n")


func _apply_ui_scale(scale: float, resize_window: bool) -> void:
	scale = clampf(scale, 1.0, 2.0)
	var window := get_window()
	var old_scale := maxf(window.content_scale_factor, 0.01)
	var new_size := window.size
	if resize_window:
		new_size = Vector2i(
			roundi(window.size.x * scale / old_scale),
			roundi(window.size.y * scale / old_scale)
		)
		var screen := DisplayServer.window_get_current_screen()
		var usable := DisplayServer.screen_get_usable_rect(screen)
		new_size.x = mini(new_size.x, usable.size.x)
		new_size.y = mini(new_size.y, usable.size.y)
	window.content_scale_factor = scale
	if resize_window:
		window.size = new_size


func _on_ui_scale_selected(index: int) -> void:
	if _loading_ui or index < 0:
		return
	var scale := float(_ui_scale_option.get_item_metadata(index))
	_ui_settings.scale = scale
	_apply_ui_scale(scale, true)
	_save_ui_settings()


func _select_option_by_float_metadata(option: OptionButton, metadata: float) -> void:
	for index in option.item_count:
		if is_equal_approx(float(option.get_item_metadata(index)), metadata):
			option.select(index)
			return


func _restore_split_offsets() -> void:
	if _outer_split == null or _inner_split == null:
		return
	_outer_split.split_offset = int(_ui_settings.outer_split_offset)
	_inner_split.split_offset = int(_ui_settings.inner_split_offset)
	_outer_split.clamp_split_offset()
	_inner_split.clamp_split_offset()


func _on_split_dragged(_offset: int) -> void:
	_ui_settings.outer_split_offset = _outer_split.split_offset
	_ui_settings.inner_split_offset = _inner_split.split_offset
	_save_ui_settings()
	_update_asset_columns()


func _load_manifest() -> void:
	_manifest = {
		"version": 1,
		"output_dir": "AK_Exusiai/images/cards",
		"asset_roots": DEFAULT_ASSET_ROOTS.duplicate(),
		"asset_files": [],
		"cards": {},
	}
	if not FileAccess.file_exists(MANIFEST_PATH):
		_save_manifest()
		return
	var text := FileAccess.get_file_as_string(MANIFEST_PATH)
	var parsed = JSON.parse_string(text)
	if parsed is not Dictionary:
		_set_status("清单格式无效，已使用空清单。", true)
		return
	for key in parsed:
		_manifest[key] = parsed[key]
	if not _manifest.has("cards") or not _manifest.cards is Dictionary:
		_manifest.cards = {}
	if not _manifest.has("asset_roots") or not _manifest.asset_roots is Array:
		_manifest.asset_roots = DEFAULT_ASSET_ROOTS.duplicate()
	if not _manifest.has("asset_files") or not _manifest.asset_files is Array:
		_manifest.asset_files = []


func _save_manifest() -> void:
	var absolute := ProjectSettings.globalize_path(MANIFEST_PATH)
	var error := DirAccess.make_dir_recursive_absolute(absolute.get_base_dir())
	if error != OK:
		_set_status("无法创建清单目录：%s" % error_string(error), true)
		return
	var file := FileAccess.open(MANIFEST_PATH, FileAccess.WRITE)
	if file == null:
		_set_status("无法保存清单：%s" % FileAccess.get_open_error(), true)
		return
	file.store_string(JSON.stringify(_manifest, "  ", false) + "\n")
	_set_status("已保存 card_art_manifest.json", false)


func _schedule_manifest_save() -> void:
	if _save_timer == null:
		_save_manifest()
		return
	_save_timer.start()


func _load_cards() -> void:
	_cards.clear()
	var titles := _load_localized_titles()
	var directory := DirAccess.open(CARD_SOURCE_DIR)
	if directory == null:
		_set_status("无法读取卡牌源码目录。", true)
		return
	var files := directory.get_files()
	files.sort()
	for filename in files:
		if not filename.ends_with(".cs") or filename == "ExusiaiCardTemplate.cs":
			continue
		var card_class := filename.get_basename()
		var source := FileAccess.get_file_as_string(CARD_SOURCE_DIR.path_join(filename))
		var ancient := source.contains("CardRarity.Ancient")
		var title: String = titles.get(card_class, card_class)
		_cards.append({
			"class_name": card_class,
			"title": title,
			"ancient": ancient,
		})


func _load_localized_titles() -> Dictionary:
	var result := {}
	if not FileAccess.file_exists(LOCALIZATION_PATH):
		return result
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(LOCALIZATION_PATH))
	if parsed is not Dictionary:
		return result
	var title_by_id := {}
	for key in parsed:
		var key_string := String(key)
		if key_string.ends_with(".title"):
			title_by_id[key_string] = String(parsed[key])
	var directory := DirAccess.open(CARD_SOURCE_DIR)
	if directory == null:
		return result
	for filename in directory.get_files():
		if not filename.ends_with(".cs") or filename == "ExusiaiCardTemplate.cs":
			continue
		var card_class := filename.get_basename()
		var key := "AK_EXUSIAI_CARD_%s.title" % _pascal_to_upper_snake(card_class)
		if title_by_id.has(key):
			result[card_class] = title_by_id[key]
	return result


func _pascal_to_upper_snake(value: String) -> String:
	var result := ""
	for index in value.length():
		var character := value[index]
		var is_upper := character == character.to_upper() and character != character.to_lower()
		var previous_lower := false
		var next_lower := false
		if index > 0:
			var previous := value[index - 1]
			previous_lower = previous == previous.to_lower() and previous != previous.to_upper()
		if index + 1 < value.length():
			var next := value[index + 1]
			next_lower = next == next.to_lower() and next != next.to_upper()
		if is_upper and index > 0 and (previous_lower or next_lower):
			result += "_"
		result += character.to_upper()
	return result


func _scan_manifest_assets() -> void:
	_assets.clear()
	for root_path in _manifest.asset_roots:
		_scan_asset_root(String(root_path))
	for file_path in _manifest.asset_files:
		_add_asset_path(String(file_path))
	_assets.sort_custom(func(a: String, b: String) -> bool: return a.naturalnocasecmp_to(b) < 0)


func _scan_asset_root(stored_path: String) -> void:
	var absolute := _absolute_from_stored(stored_path)
	if not DirAccess.dir_exists_absolute(absolute):
		return
	_scan_directory_recursive(absolute)


func _scan_directory_recursive(absolute_dir: String) -> void:
	var directory := DirAccess.open(absolute_dir)
	if directory == null:
		return
	directory.list_dir_begin()
	while true:
		var name := directory.get_next()
		if name.is_empty():
			break
		if name.begins_with("."):
			continue
		var child := absolute_dir.path_join(name)
		if directory.current_is_dir():
			_scan_directory_recursive(child)
		else:
			_add_asset_path(_stored_from_absolute(child))
	directory.list_dir_end()


func _add_asset_path(stored_path: String) -> void:
	var extension := stored_path.get_extension().to_lower()
	if extension not in SUPPORTED_EXTENSIONS:
		return
	if stored_path not in _assets:
		_assets.append(stored_path)


func _rebuild_folder_options() -> void:
	if _folder_option == null:
		return
	var counts := {}
	for path in _assets:
		var folder := path.get_base_dir()
		counts[folder] = int(counts.get(folder, 0)) + 1
	var folders: Array = counts.keys()
	folders.sort_custom(func(a, b) -> bool: return String(a).naturalnocasecmp_to(String(b)) < 0)
	var wanted := String(_ui_settings.get("folder", ALL_FOLDERS))
	_loading_ui = true
	_folder_option.clear()
	_folder_option.add_item("全部文件夹（%d）" % _assets.size())
	_folder_option.set_item_metadata(0, ALL_FOLDERS)
	var selected_index := 0
	for folder_value in folders:
		var folder := String(folder_value)
		var index := _folder_option.item_count
		_folder_option.add_item("%s（%d）" % [_folder_display_name(folder), counts[folder]])
		_folder_option.set_item_metadata(index, folder)
		_folder_option.set_item_tooltip(index, folder)
		if folder == wanted:
			selected_index = index
	_folder_option.select(selected_index)
	if selected_index == 0:
		_ui_settings.folder = ALL_FOLDERS
	_loading_ui = false


func _folder_display_name(folder: String) -> String:
	if folder.is_absolute_path():
		return folder.get_file()
	return folder


func _on_folder_selected(index: int) -> void:
	if _loading_ui or index < 0:
		return
	_ui_settings.folder = String(_folder_option.get_item_metadata(index))
	_save_ui_settings()
	_rebuild_asset_list()


func _on_asset_view_selected(index: int) -> void:
	if _loading_ui or index < 0:
		return
	var view := String(_asset_view_option.get_item_metadata(index))
	_ui_settings.asset_view = view
	_apply_asset_view(view)
	_save_ui_settings()
	_rebuild_asset_list()


func _apply_asset_view(view: String) -> void:
	if _asset_list == null:
		return
	if view == "compact":
		_asset_list.icon_mode = ItemList.ICON_MODE_TOP
		_asset_list.fixed_icon_size = Vector2i(96, 68)
		_asset_list.fixed_column_width = 116
		_asset_list.same_column_width = true
	else:
		_asset_list.icon_mode = ItemList.ICON_MODE_LEFT
		_asset_list.fixed_icon_size = Vector2i(88, 62)
		_asset_list.fixed_column_width = 0
		_asset_list.same_column_width = false
	_update_asset_columns()


func _update_asset_columns() -> void:
	if _asset_list == null:
		return
	if String(_ui_settings.get("asset_view", "list")) == "compact":
		_asset_list.max_columns = maxi(1, floori(_asset_list.size.x / 122.0))
	else:
		_asset_list.max_columns = 1


func _rebuild_asset_list() -> void:
	_asset_list.clear()
	_filtered_assets.clear()
	_thumbnail_queue.clear()
	var query := _asset_search.text.strip_edges().to_lower()
	var selected_folder := String(_ui_settings.get("folder", ALL_FOLDERS))
	var compact := String(_ui_settings.get("asset_view", "list")) == "compact"
	for path in _assets:
		if not query.is_empty() and not path.get_file().to_lower().contains(query):
			continue
		if selected_folder != ALL_FOLDERS and path.get_base_dir() != selected_folder:
			continue
		_filtered_assets.append(path)
		var item_text := path.get_file().get_basename()
		if not compact:
			item_text += "  ·  " + _folder_display_name(path.get_base_dir())
		var index := _asset_list.add_item(item_text)
		_asset_list.set_item_tooltip(index, path)
		if _thumbnail_cache.has(path):
			_asset_list.set_item_icon(index, _thumbnail_cache[path])
		else:
			_thumbnail_queue.append({"index": index, "path": path})
	_update_asset_columns()


func _get_thumbnail(path: String) -> ImageTexture:
	if _thumbnail_cache.has(path):
		return _thumbnail_cache[path]
	var image := _load_image_uncached(path)
	if image == null or image.is_empty():
		return null
	var thumbnail := image.duplicate()
	var thumbnail_scale := minf(88.0 / thumbnail.get_width(), 62.0 / thumbnail.get_height())
	thumbnail.resize(
		maxi(1, roundi(thumbnail.get_width() * thumbnail_scale)),
		maxi(1, roundi(thumbnail.get_height() * thumbnail_scale)),
		Image.INTERPOLATE_LANCZOS
	)
	var texture := ImageTexture.create_from_image(thumbnail)
	_thumbnail_cache[path] = texture
	return texture


func _rebuild_card_options() -> void:
	_loading_ui = true
	_card_option.clear()
	for card in _cards:
		var marker := "● " if _manifest.cards.has(card.class_name) else "○ "
		var ancient := "【先古】" if card.ancient else ""
		var label := "%s%s%s (%s)" % [marker, ancient, card.title, card.class_name]
		var index := _card_option.item_count
		_card_option.add_item(label)
		_card_option.set_item_metadata(index, card.class_name)
	_loading_ui = false
	_update_progress()


func _select_card_by_index(index: int) -> void:
	if _loading_ui or index < 0 or index >= _card_option.item_count:
		return
	_current_card = String(_card_option.get_item_metadata(index))
	_loading_ui = true
	var config := _get_effective_config(_current_card)
	_select_option_by_metadata(_mode_option, config.mode)
	_configure_zoom_range(config.mode)
	_select_option_by_metadata(_background_option, config.background)
	_select_option_by_metadata(_motif_option, config.motif)
	_top_color.color = Color.from_string(config.top_color, Color("2b2340"))
	_bottom_color.color = Color.from_string(config.bottom_color, Color("bd6c59"))
	_zoom_slider.value = config.zoom
	_zoom_value.text = "%.2f" % config.zoom
	_source_label.text = config.source if not String(config.source).is_empty() else "未选择"
	var card := _find_card(_current_card)
	_ancient_label.text = "先古卡 · 输出 250 × 351" if card.get("ancient", false) else "普通卡 · 输出 250 × 190"
	_preview.set_transform_values(config.zoom, _array_to_vector2(config.offset))
	_loading_ui = false
	_queue_preview_render()


func _select_option_by_metadata(option: OptionButton, metadata: String) -> void:
	for index in option.item_count:
		if String(option.get_item_metadata(index)) == metadata:
			option.select(index)
			return


func _get_effective_config(card_name: String) -> Dictionary:
	var saved: Dictionary = _manifest.cards.get(card_name, {})
	var background := String(saved.get("background", "laterano_sunset"))
	if not BACKGROUNDS.has(background):
		background = "laterano_sunset"
	var background_data: Dictionary = BACKGROUNDS[background]
	return {
		"mode": String(saved.get("mode", "crop")),
		"source": String(saved.get("source", "")),
		"zoom": float(saved.get("zoom", 1.0)),
		"offset": saved.get("offset", [0.0, 0.0]),
		"background": background,
		"top_color": String(saved.get("top_color", background_data.top)),
		"bottom_color": String(saved.get("bottom_color", background_data.bottom)),
		"motif": String(saved.get("motif", "auto")),
	}


func _store_current_form() -> void:
	if _loading_ui or _current_card.is_empty():
		return
	var previous := _get_effective_config(_current_card)
	var config := {
		"mode": String(_mode_option.get_item_metadata(_mode_option.selected)),
		"source": String(previous.source),
		"zoom": snappedf(float(_zoom_slider.value), 0.001),
		"offset": previous.offset,
		"background": String(_background_option.get_item_metadata(_background_option.selected)),
		"top_color": _top_color.color.to_html(false),
		"bottom_color": _bottom_color.color.to_html(false),
		"motif": String(_motif_option.get_item_metadata(_motif_option.selected)),
	}
	_manifest.cards[_current_card] = config
	_schedule_manifest_save()
	_rebuild_card_options_preserving_current()
	_queue_preview_render()


func _store_transform(zoom: float, offset: Vector2) -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	config.zoom = snappedf(zoom, 0.001)
	config.offset = [snappedf(offset.x, 0.0001), snappedf(offset.y, 0.0001)]
	_manifest.cards[_current_card] = config
	_schedule_manifest_save()
	_preview.set_transform_values(config.zoom, offset)
	_queue_preview_render()


func _rebuild_card_options_preserving_current() -> void:
	var selected_card := _current_card
	_rebuild_card_options()
	for index in _card_option.item_count:
		if String(_card_option.get_item_metadata(index)) == selected_card:
			_loading_ui = true
			_card_option.select(index)
			_loading_ui = false
			break


func _on_asset_selected(index: int) -> void:
	if index < 0 or index >= _filtered_assets.size() or _current_card.is_empty():
		return
	var path := _filtered_assets[index]
	var config := _get_effective_config(_current_card)
	config.source = path
	_manifest.cards[_current_card] = config
	_source_label.text = path
	_schedule_manifest_save()
	_rebuild_card_options_preserving_current()
	_queue_preview_render()


func _on_form_changed(_index: int) -> void:
	if not _loading_ui:
		var mode := String(_mode_option.get_item_metadata(_mode_option.selected))
		_configure_zoom_range(mode)
	_store_current_form()


func _configure_zoom_range(mode: String) -> void:
	var was_loading := _loading_ui
	_loading_ui = true
	_zoom_slider.min_value = 1.0 if mode == "crop" else 0.2
	if _zoom_slider.value < _zoom_slider.min_value:
		_zoom_slider.value = _zoom_slider.min_value
	_loading_ui = was_loading


func _on_background_changed(_index: int) -> void:
	if _loading_ui:
		return
	var key := String(_background_option.get_item_metadata(_background_option.selected))
	if key != "custom":
		var data: Dictionary = BACKGROUNDS[key]
		_loading_ui = true
		_top_color.color = Color.from_string(data.top, Color.WHITE)
		_bottom_color.color = Color.from_string(data.bottom, Color.WHITE)
		_loading_ui = false
	_store_current_form()


func _on_custom_color_changed(_color: Color) -> void:
	if _loading_ui:
		return
	_select_option_by_metadata(_background_option, "custom")
	_store_current_form()


func _on_zoom_changed(value: float) -> void:
	_zoom_value.text = "%.2f" % value
	if _loading_ui or _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	_store_transform(value, _array_to_vector2(config.offset))


func _on_preview_transform_changed(zoom: float, offset: Vector2) -> void:
	var mode := String(_mode_option.get_item_metadata(_mode_option.selected))
	if mode == "crop":
		zoom = maxf(1.0, zoom)
	_loading_ui = true
	_zoom_slider.value = zoom
	_zoom_value.text = "%.2f" % zoom
	_loading_ui = false
	_store_transform(zoom, offset)


func _reset_transform() -> void:
	_store_transform(1.0, Vector2.ZERO)
	_loading_ui = true
	_zoom_slider.value = 1.0
	_zoom_value.text = "1.00"
	_loading_ui = false


func _clear_source() -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	config.source = ""
	_manifest.cards[_current_card] = config
	_source_label.text = "未选择"
	_schedule_manifest_save()
	_queue_preview_render()


func _queue_preview_render() -> void:
	if _render_queued:
		return
	_render_queued = true
	get_tree().process_frame.connect(_render_preview, CONNECT_ONE_SHOT)


func _render_preview() -> void:
	_render_queued = false
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	var card := _find_card(_current_card)
	var output_size := ANCIENT_SIZE if card.get("ancient", false) else NORMAL_SIZE
	var image := _render_card_art(config, output_size, _current_card)
	_preview.set_transform_values(config.zoom, _array_to_vector2(config.offset))
	_preview.set_art(image, output_size)
	if image == null or image.is_empty():
		_preview.set_empty_message("请选择素材；占位模式无需素材")


func _render_card_art(config: Dictionary, output_size: Vector2i, card_name: String) -> Image:
	var mode := String(config.mode)
	if mode == "placeholder":
		return _make_background(output_size, config, card_name, true)
	var source_path := String(config.source)
	if source_path.is_empty():
		return Image.new()
	var source := _load_source_image(source_path)
	if source == null or source.is_empty():
		return Image.new()
	if mode == "icon":
		var background := _make_background(output_size, config, card_name, false)
		_composite_icon(background, source, config)
		return background
	return _crop_illustration(source, output_size, float(config.zoom), _array_to_vector2(config.offset))


func _crop_illustration(source: Image, output_size: Vector2i, zoom: float, offset: Vector2) -> Image:
	var result := Image.create(output_size.x, output_size.y, false, Image.FORMAT_RGBA8)
	result.fill(Color("11131a"))
	var scale := maxf(
		float(output_size.x) / float(source.get_width()),
		float(output_size.y) / float(source.get_height())
	) * maxf(zoom, 0.01)
	var resized_width := maxi(1, roundi(source.get_width() * scale))
	var resized_height := maxi(1, roundi(source.get_height() * scale))
	var resized := source.duplicate()
	resized.resize(resized_width, resized_height, Image.INTERPOLATE_LANCZOS)

	var desired_x := roundi((output_size.x - resized_width) * 0.5 + offset.x * output_size.x)
	var desired_y := roundi((output_size.y - resized_height) * 0.5 + offset.y * output_size.y)
	var min_x := mini(0, output_size.x - resized_width)
	var min_y := mini(0, output_size.y - resized_height)
	var destination_x := clampi(desired_x, min_x, 0)
	var destination_y := clampi(desired_y, min_y, 0)
	var source_x := maxi(0, -destination_x)
	var source_y := maxi(0, -destination_y)
	var output_x := maxi(0, destination_x)
	var output_y := maxi(0, destination_y)
	var copy_width := mini(resized_width - source_x, output_size.x - output_x)
	var copy_height := mini(resized_height - source_y, output_size.y - output_y)
	if copy_width > 0 and copy_height > 0:
		result.blit_rect(
			resized,
			Rect2i(source_x, source_y, copy_width, copy_height),
			Vector2i(output_x, output_y)
		)
	return result


func _make_background(output_size: Vector2i, config: Dictionary, card_name: String, include_motif: bool) -> Image:
	var image := Image.create(output_size.x, output_size.y, false, Image.FORMAT_RGBA8)
	var top := Color.from_string(String(config.top_color), Color("2b2340"))
	var bottom := Color.from_string(String(config.bottom_color), Color("bd6c59"))
	var seed: int = absi(card_name.hash())
	var glow_center := Vector2(0.35 + float(seed % 31) / 100.0, 0.38)
	for y in output_size.y:
		var vertical := float(y) / maxf(1.0, output_size.y - 1.0)
		for x in output_size.x:
			var horizontal := float(x) / maxf(1.0, output_size.x - 1.0)
			var color := top.lerp(bottom, smoothstep(0.0, 1.0, vertical))
			var distance := Vector2(horizontal, vertical).distance_to(glow_center)
			var glow := clampf(1.0 - distance / 0.72, 0.0, 1.0)
			color = color.lightened(glow * 0.16)
			var edge := maxf(abs(horizontal - 0.5) * 2.0, abs(vertical - 0.5) * 2.0)
			color = color.darkened(pow(edge, 2.4) * 0.28)
			var streak := posmod(x + y * 2 + seed, 47)
			if streak <= 1:
				color = color.lightened(0.035)
			image.set_pixel(x, y, Color(color.r, color.g, color.b, 1.0))
	if include_motif:
		_draw_placeholder_motif(image, String(config.motif), seed)
	return image


func _draw_placeholder_motif(image: Image, motif: String, seed: int) -> void:
	if motif == "auto":
		var choices := ["crosshair", "halo", "burst", "package", "speedlines"]
		motif = choices[seed % choices.size()]
	var center := Vector2(image.get_width() * 0.5, image.get_height() * 0.48)
	var radius := minf(image.get_width(), image.get_height()) * 0.24
	var ink := Color(1.0, 0.92, 0.72, 0.72)
	if motif == "crosshair":
		_draw_ring(image, center, radius, 3.0, ink)
		_draw_line(image, center + Vector2(-radius * 1.35, 0), center + Vector2(radius * 1.35, 0), 3.0, ink)
		_draw_line(image, center + Vector2(0, -radius * 1.05), center + Vector2(0, radius * 1.05), 3.0, ink)
	elif motif == "halo":
		_draw_ring(image, center - Vector2(0, radius * 0.2), radius, 7.0, ink)
		_draw_ring(image, center - Vector2(0, radius * 0.2), radius * 0.62, 2.0, Color(1, 1, 1, 0.5))
	elif motif == "package":
		var half := Vector2(radius, radius * 0.72)
		_draw_line(image, center - half, center + Vector2(half.x, -half.y), 5.0, ink)
		_draw_line(image, center + Vector2(half.x, -half.y), center + half, 5.0, ink)
		_draw_line(image, center + half, center + Vector2(-half.x, half.y), 5.0, ink)
		_draw_line(image, center + Vector2(-half.x, half.y), center - half, 5.0, ink)
		_draw_line(image, center + Vector2(0, -half.y), center + Vector2(0, half.y), 4.0, ink)
	elif motif == "speedlines":
		for index in 7:
			var y := image.get_height() * (0.2 + index * 0.1)
			var length := radius * (1.0 + float((seed + index * 11) % 70) / 50.0)
			_draw_line(image, Vector2(center.x - length, y), Vector2(center.x + length, y - radius * 0.18), 3.0, ink)
	else:
		for index in 12:
			var angle := TAU * float(index) / 12.0
			var direction := Vector2(cos(angle), sin(angle))
			_draw_line(image, center + direction * radius * 0.45, center + direction * radius * 1.45, 4.0, ink)
		_draw_ring(image, center, radius * 0.35, 5.0, ink)


func _draw_ring(image: Image, center: Vector2, radius: float, thickness: float, color: Color) -> void:
	var min_x := maxi(0, floori(center.x - radius - thickness))
	var max_x := mini(image.get_width() - 1, ceili(center.x + radius + thickness))
	var min_y := maxi(0, floori(center.y - radius - thickness))
	var max_y := mini(image.get_height() - 1, ceili(center.y + radius + thickness))
	for y in range(min_y, max_y + 1):
		for x in range(min_x, max_x + 1):
			var distance := Vector2(x, y).distance_to(center)
			if absf(distance - radius) <= thickness:
				_blend_pixel(image, x, y, color)


func _draw_line(image: Image, start: Vector2, end: Vector2, thickness: float, color: Color) -> void:
	var min_x := maxi(0, floori(minf(start.x, end.x) - thickness))
	var max_x := mini(image.get_width() - 1, ceili(maxf(start.x, end.x) + thickness))
	var min_y := maxi(0, floori(minf(start.y, end.y) - thickness))
	var max_y := mini(image.get_height() - 1, ceili(maxf(start.y, end.y) + thickness))
	var segment := end - start
	var length_squared := segment.length_squared()
	for y in range(min_y, max_y + 1):
		for x in range(min_x, max_x + 1):
			var point := Vector2(x, y)
			var t := 0.0
			if length_squared > 0.0:
				t = clampf((point - start).dot(segment) / length_squared, 0.0, 1.0)
			if point.distance_to(start + segment * t) <= thickness:
				_blend_pixel(image, x, y, color)


func _blend_pixel(image: Image, x: int, y: int, foreground: Color) -> void:
	var background := image.get_pixel(x, y)
	var alpha := foreground.a
	image.set_pixel(x, y, Color(
		foreground.r * alpha + background.r * (1.0 - alpha),
		foreground.g * alpha + background.g * (1.0 - alpha),
		foreground.b * alpha + background.b * (1.0 - alpha),
		1.0
	))


func _composite_icon(background: Image, source: Image, config: Dictionary) -> void:
	var bounds := _find_visible_bounds(source)
	if bounds.size == Vector2i.ZERO:
		return
	var icon := source.get_region(bounds)
	var base_width := background.get_width() * 0.62
	var base_height := background.get_height() * 0.68
	var scale := minf(base_width / icon.get_width(), base_height / icon.get_height()) * float(config.zoom)
	var width := maxi(1, roundi(icon.get_width() * scale))
	var height := maxi(1, roundi(icon.get_height() * scale))
	icon.resize(width, height, Image.INTERPOLATE_LANCZOS)
	var offset := _array_to_vector2(config.offset)
	var position := Vector2i(
		roundi((background.get_width() - width) * 0.5 + offset.x * background.get_width()),
		roundi((background.get_height() - height) * 0.5 + offset.y * background.get_height())
	)
	var shadow_position := position + Vector2i(4, 5)
	_blend_icon_shadow(background, icon, shadow_position)
	background.blend_rect(icon, Rect2i(Vector2i.ZERO, icon.get_size()), position)


func _blend_icon_shadow(background: Image, icon: Image, position: Vector2i) -> void:
	for y in icon.get_height():
		var target_y := position.y + y
		if target_y < 0 or target_y >= background.get_height():
			continue
		for x in icon.get_width():
			var target_x := position.x + x
			if target_x < 0 or target_x >= background.get_width():
				continue
			var alpha := icon.get_pixel(x, y).a * 0.45
			if alpha > 0.01:
				_blend_pixel(background, target_x, target_y, Color(0.02, 0.02, 0.03, alpha))


func _find_visible_bounds(image: Image) -> Rect2i:
	var min_x := image.get_width()
	var min_y := image.get_height()
	var max_x := -1
	var max_y := -1
	for y in image.get_height():
		for x in image.get_width():
			if image.get_pixel(x, y).a <= 0.02:
				continue
			min_x = mini(min_x, x)
			min_y = mini(min_y, y)
			max_x = maxi(max_x, x)
			max_y = maxi(max_y, y)
	if max_x < min_x or max_y < min_y:
		return Rect2i()
	return Rect2i(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1)


func _load_source_image(stored_path: String) -> Image:
	if _source_cache.has(stored_path):
		return _source_cache[stored_path]
	var image := _load_image_uncached(stored_path)
	if image != null:
		_source_cache[stored_path] = image
	return image


func _load_image_uncached(stored_path: String) -> Image:
	var absolute := _absolute_from_stored(stored_path)
	var image := Image.new()
	var error := ERR_FILE_UNRECOGNIZED
	if stored_path.get_extension().to_lower() == "svg":
		var svg_text := FileAccess.get_file_as_string(absolute)
		if not svg_text.is_empty():
			error = image.load_svg_from_string(svg_text, 4.0)
	else:
		error = image.load(absolute)
	if error != OK:
		_set_status("无法读取素材：%s (%s)" % [stored_path, error_string(error)], true)
		return null
	image.convert(Image.FORMAT_RGBA8)
	var longest_edge := maxi(image.get_width(), image.get_height())
	if longest_edge > 1600:
		var scale := 1600.0 / longest_edge
		image.resize(
			maxi(1, roundi(image.get_width() * scale)),
			maxi(1, roundi(image.get_height() * scale)),
			Image.INTERPOLATE_LANCZOS
		)
	return image


func _export_current() -> void:
	if _current_card.is_empty():
		return
	if _export_card(_current_card):
		_set_status("已导出 %s.png" % _current_card, false)


func _run_smoke_test() -> void:
	var crop_config := {
		"mode": "crop",
		"source": "references/official/art/半身.jpg",
		"zoom": 1.0,
		"offset": [0.0, 0.0],
		"background": "laterano_sunset",
		"top_color": "3c2059",
		"bottom_color": "ee9558",
		"motif": "auto",
	}
	var icon_config := crop_config.duplicate(true)
	icon_config.mode = "icon"
	icon_config.source = "references/free/art/弹药.svg"
	var placeholder_config := crop_config.duplicate(true)
	placeholder_config.mode = "placeholder"
	placeholder_config.source = ""
	var crop := _render_card_art(crop_config, NORMAL_SIZE, "SmokeCrop")
	var icon := _render_card_art(icon_config, NORMAL_SIZE, "SmokeIcon")
	var ancient := _render_card_art(placeholder_config, ANCIENT_SIZE, "SmokeAncient")
	var ancient_count := 0
	for card in _cards:
		if card.ancient:
			ancient_count += 1
	if (
		crop.get_size() != NORMAL_SIZE
		or icon.get_size() != NORMAL_SIZE
		or ancient.get_size() != ANCIENT_SIZE
		or _cards.size() != 99
		or ancient_count != 2
		or _ui_scale_option.item_count != UI_SCALES.size()
		or _folder_option.item_count < 2
	):
		push_error("Card art manager smoke test failed.")
		get_tree().quit(1)
		return
	var smoke_dir := ProjectSettings.globalize_path("res://tmp/card_art_manager_smoke")
	DirAccess.make_dir_recursive_absolute(smoke_dir)
	crop.save_png(smoke_dir.path_join("crop.png"))
	icon.save_png(smoke_dir.path_join("icon.png"))
	ancient.save_png(smoke_dir.path_join("ancient.png"))
	await get_tree().process_frame
	await get_tree().process_frame
	var original_ui_scale := get_window().content_scale_factor
	_apply_ui_scale(1.25, false)
	if not is_equal_approx(get_window().content_scale_factor, 1.25):
		push_error("Card art manager UI scale smoke test failed.")
		get_tree().quit(1)
		return
	_apply_ui_scale(original_ui_scale, false)
	_apply_asset_view("compact")
	_update_asset_columns()
	if _asset_list.max_columns < 1 or _outer_split.get_child_count() != 2 or _inner_split.get_child_count() != 2:
		push_error("Card art manager layout smoke test failed.")
		get_tree().quit(1)
		return
	_apply_asset_view(String(_ui_settings.asset_view))
	print("CARD_ART_MANAGER_SMOKE_OK cards=%d ancient_cards=%d folders=%d sidebars=%d/%d crop=%s icon=%s ancient=%s" % [
		_cards.size(),
		ancient_count,
		_folder_option.item_count - 1,
		roundi((_outer_split.get_child(0) as Control).size.x),
		roundi((_inner_split.get_child(1) as Control).size.x),
		crop.get_size(),
		icon.get_size(),
		ancient.get_size(),
	])
	get_tree().quit()


func _export_all_configured() -> void:
	var success := 0
	var skipped := 0
	for card_name in _manifest.cards:
		if _find_card(String(card_name)).is_empty():
			skipped += 1
			continue
		if _export_card(String(card_name)):
			success += 1
		else:
			skipped += 1
	_set_status("批量导出完成：%d 张成功，%d 张跳过。" % [success, skipped], skipped > 0)


func _export_card(card_name: String) -> bool:
	var config := _get_effective_config(card_name)
	if config.mode != "placeholder" and String(config.source).is_empty():
		return false
	var card := _find_card(card_name)
	if card.is_empty():
		return false
	var output_size := ANCIENT_SIZE if card.ancient else NORMAL_SIZE
	var image := _render_card_art(config, output_size, card_name)
	if image == null or image.is_empty():
		return false
	var output_dir := String(_manifest.get("output_dir", "AK_Exusiai/images/cards"))
	var output_path := _absolute_from_stored(output_dir).path_join(card_name + ".png")
	var error := DirAccess.make_dir_recursive_absolute(output_path.get_base_dir())
	if error != OK:
		_set_status("无法创建输出目录：%s" % error_string(error), true)
		return false
	error = image.save_png(output_path)
	if error != OK:
		_set_status("无法保存 %s：%s" % [output_path, error_string(error)], true)
		return false
	if image.get_size() != output_size:
		_set_status("尺寸校验失败：%s" % card_name, true)
		return false
	return true


func _find_card(card_name: String) -> Dictionary:
	for card in _cards:
		if card.class_name == card_name:
			return card
	return {}


func _step_card(direction: int) -> void:
	if _card_option.item_count == 0:
		return
	var index := wrapi(_card_option.selected + direction, 0, _card_option.item_count)
	_card_option.select(index)
	_select_card_by_index(index)


func _update_progress() -> void:
	var configured := 0
	for card in _cards:
		if _manifest.cards.has(card.class_name):
			configured += 1
	_progress_label.text = "已配置 %d / %d 张\n左侧素材 %d 个" % [configured, _cards.size(), _assets.size()]


func _show_add_files_dialog() -> void:
	_add_files_dialog.popup_centered_ratio(0.75)


func _show_add_folder_dialog() -> void:
	_add_folder_dialog.popup_centered_ratio(0.75)


func _on_files_added(paths: PackedStringArray) -> void:
	for path in paths:
		var stored := _stored_from_absolute(path)
		if stored not in _manifest.asset_files:
			_manifest.asset_files.append(stored)
	_save_manifest()
	_refresh_assets()


func _on_folder_added(path: String) -> void:
	var stored := _stored_from_absolute(path)
	if stored not in _manifest.asset_roots:
		_manifest.asset_roots.append(stored)
	_save_manifest()
	_refresh_assets()


func _refresh_assets() -> void:
	_source_cache.clear()
	_thumbnail_cache.clear()
	_scan_manifest_assets()
	_rebuild_folder_options()
	_rebuild_asset_list()
	_update_progress()
	_queue_preview_render()
	_set_status("素材列表已重新扫描。", false)


func _on_asset_search_changed(_text: String) -> void:
	_rebuild_asset_list()


func _absolute_from_stored(path: String) -> String:
	if path.is_absolute_path():
		return path.simplify_path()
	return ProjectSettings.globalize_path("res://" + path).simplify_path()


func _stored_from_absolute(path: String) -> String:
	var normalized := path.simplify_path()
	var project_root := ProjectSettings.globalize_path("res://").simplify_path().trim_suffix("/")
	if normalized.to_lower().begins_with(project_root.to_lower() + "/"):
		return normalized.substr(project_root.length() + 1)
	return normalized


func _array_to_vector2(value) -> Vector2:
	if value is Array and value.size() >= 2:
		return Vector2(float(value[0]), float(value[1]))
	return Vector2.ZERO


func _set_status(message: String, error: bool) -> void:
	if _status_label == null:
		return
	_status_label.text = message
	_status_label.add_theme_color_override("font_color", Color("ef767a") if error else Color("9bd8a5"))
