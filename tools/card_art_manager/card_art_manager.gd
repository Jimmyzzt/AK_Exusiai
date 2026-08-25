extends Control

const MANIFEST_PATH := "res://tools/card_art_manager/card_art_manifest.json"
const UI_SETTINGS_PATH := "user://card_art_manager_ui.json"
const THUMBNAIL_CACHE_DIR := "user://card_art_manager_thumbnails"
const THUMBNAIL_CACHE_MAX_BYTES := 128 * 1024 * 1024
const THUMBNAIL_MEMORY_MAX_ITEMS := 512
const SOURCE_MEMORY_MAX_ITEMS := 12
const PREVIEW_MATERIAL_MAX_BYTES := 96 * 1024 * 1024
const CARD_SOURCE_DIR := "res://AK_ExusiaiCode/Cards"
const LOCALIZATION_PATH := "res://AK_Exusiai/localization/zhs/cards.json"
const DEFAULT_OUTPUT_DIR := "res://AK_Exusiai/images/cards"
const BASE_NORMAL_SIZE := Vector2i(250, 190)
const BASE_ANCIENT_SIZE := Vector2i(250, 351)
const RESOLUTION_SCALES := [1, 2, 3, 4]
const SUPPORTED_EXTENSIONS := ["png", "jpg", "jpeg", "webp", "svg"]
const DEFAULT_ASSET_ROOTS := [
	"references/official/art",
	"references/official/asset",
	"references/free/art",
]
const ALL_FOLDERS := "__all__"
const UI_SCALES := [1.0, 1.25, 1.5, 1.75, 2.0]
const BACKGROUNDS := {
	"laterano_sunset": {"label": "夕照", "top": "3c2059", "bottom": "ee9558"},
	"angel_blue": {"label": "蓝光", "top": "10273f", "bottom": "52b8d6"},
	"penguin_night": {"label": "夜色", "top": "182330", "bottom": "677485"},
	"red_alert": {"label": "警报", "top": "35151d", "bottom": "d35a48"},
	"holy_gold": {"label": "金辉", "top": "493918", "bottom": "e3c06b"},
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
const GRADIENT_MODES := {
	"vertical": "垂直",
	"horizontal": "水平",
	"radial": "中心",
	"diagonal": "对角",
	"solid": "纯色",
}
const BACKGROUND_TEXTURES := {
	"none": "无纹理",
	"streaks": "斜纹",
	"grain": "颗粒",
	"grid": "网格",
	"rays": "放射",
}

var _manifest: Dictionary = {}
var _ui_settings: Dictionary = {
	"scale": 1.0,
	"asset_view": "list",
	"folder": ALL_FOLDERS,
	"outer_split_offset": 340,
	"inner_split_offset": -340,
	"show_frame_guide": true,
}
var _cards: Array[Dictionary] = []
var _assets: Array[String] = []
var _filtered_assets: Array[String] = []
var _source_cache: Dictionary = {}
var _asset_signatures: Dictionary = {}
var _preview_base_cache: Dictionary = {}
var _preview_material_cache: Dictionary = {}
var _thumbnail_cache: Dictionary = {}
var _thumbnail_queue: Array[Dictionary] = []
var _current_card := ""
var _loading_ui := false
var _render_queued := false
var _is_smoke_test := false
var _exporting := false
var _disk_thumbnail_cache_bytes := 0

var _asset_list: ItemList
var _asset_search: LineEdit
var _asset_view_option: OptionButton
var _folder_option: OptionButton
var _ui_scale_option: OptionButton
var _outer_split: HSplitContainer
var _inner_split: HSplitContainer
var _preview: CardArtPreview
var _card_option: OptionButton
var _resolution_option: OptionButton
var _background_option: OptionButton
var _gradient_option: OptionButton
var _texture_option: OptionButton
var _motif_option: OptionButton
var _background_toggle: CheckButton
var _placeholder_toggle: CheckButton
var _top_color: ColorPickerButton
var _bottom_color: ColorPickerButton
var _zoom_slider: HSlider
var _zoom_value: Label
var _source_label: Label
var _status_label: Label
var _progress_label: Label
var _cache_label: Label
var _motif_controls: VBoxContainer
var _export_progress_label: Label
var _export_progress: ProgressBar
var _export_progress_box: VBoxContainer
var _export_current_button: Button
var _export_all_button: Button
var _ancient_label: Label
var _frame_guide_toggle: CheckButton
var _flip_horizontal_button: Button
var _flip_vertical_button: Button
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
	_refresh_cache_stats_from_disk()
	_prune_thumbnail_disk_cache()
	_load_manifest()
	_sync_resolution_option()
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
	var deadline := Time.get_ticks_usec() + 6000
	var processed := 0
	while not _thumbnail_queue.is_empty() and processed < 4:
		var request: Dictionary = _thumbnail_queue.pop_front()
		var index: int = request.index
		var path: String = request.path
		if index < 0 or index >= _asset_list.item_count:
			continue
		if _asset_list.get_item_tooltip(index) != path:
			continue
		var thumbnail := _get_thumbnail(path)
		if thumbnail != null:
			_asset_list.set_item_icon(index, thumbnail)
		processed += 1
		if Time.get_ticks_usec() >= deadline:
			break


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

	var clear_cache := Button.new()
	clear_cache.text = "清理缓存"
	clear_cache.tooltip_text = "清理卡图管理器生成的缩略图和内存图片缓存，不会删除素材或导出卡图"
	clear_cache.pressed.connect(_clear_thumbnail_cache)
	toolbar.add_child(clear_cache)

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
	var rotate_left := Button.new()
	rotate_left.text = "左转"
	rotate_left.tooltip_text = "逆时针旋转素材 90°"
	rotate_left.pressed.connect(func() -> void: _rotate_material(-1))
	preview_header.add_child(rotate_left)
	var rotate_right := Button.new()
	rotate_right.text = "右转"
	rotate_right.tooltip_text = "顺时针旋转素材 90°"
	rotate_right.pressed.connect(func() -> void: _rotate_material(1))
	preview_header.add_child(rotate_right)
	_flip_horizontal_button = Button.new()
	_flip_horizontal_button.text = "水平翻转"
	_flip_horizontal_button.toggle_mode = true
	_flip_horizontal_button.pressed.connect(func() -> void: _flip_material(true))
	preview_header.add_child(_flip_horizontal_button)
	_flip_vertical_button = Button.new()
	_flip_vertical_button.text = "垂直翻转"
	_flip_vertical_button.toggle_mode = true
	_flip_vertical_button.pressed.connect(func() -> void: _flip_material(false))
	preview_header.add_child(_flip_vertical_button)
	var center_horizontal := Button.new()
	center_horizontal.text = "水平居中"
	center_horizontal.pressed.connect(func() -> void: _center_material(true))
	preview_header.add_child(center_horizontal)
	var center_vertical := Button.new()
	center_vertical.text = "垂直居中"
	center_vertical.pressed.connect(func() -> void: _center_material(false))
	preview_header.add_child(center_vertical)
	var reset_transform := Button.new()
	reset_transform.text = "复位"
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
	_export_current_button = Button.new()
	_export_current_button.text = "导出当前卡图"
	_export_current_button.pressed.connect(_export_current)
	export_bar.add_child(_export_current_button)
	_export_all_button = Button.new()
	_export_all_button.text = "批量导出已配置"
	_export_all_button.pressed.connect(_export_all_configured)
	export_bar.add_child(_export_all_button)

	_export_progress_box = VBoxContainer.new()
	_export_progress_box.add_theme_constant_override("separation", 3)
	middle.add_child(_export_progress_box)
	_export_progress_label = Label.new()
	_export_progress_label.text = ""
	_export_progress_label.add_theme_color_override("font_color", Color("b9c2d3"))
	_export_progress_box.add_child(_export_progress_label)
	_export_progress = ProgressBar.new()
	_export_progress.min_value = 0
	_export_progress.max_value = 1
	_export_progress.value = 0
	_export_progress.show_percentage = true
	_export_progress.custom_minimum_size.y = 18
	_export_progress_box.add_child(_export_progress)
	_export_progress_box.visible = false

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

	right.add_child(_make_field_label("输出分辨率（保持官方比例）"))
	_resolution_option = OptionButton.new()
	for scale in RESOLUTION_SCALES:
		var normal := BASE_NORMAL_SIZE * int(scale)
		var ancient := BASE_ANCIENT_SIZE * int(scale)
		var index := _resolution_option.item_count
		_resolution_option.add_item("%d× · %d×%d / 先古 %d×%d" % [
			scale,
			normal.x,
			normal.y,
			ancient.x,
			ancient.y,
		])
		_resolution_option.set_item_metadata(index, scale)
	_resolution_option.item_selected.connect(_on_resolution_selected)
	right.add_child(_resolution_option)

	_frame_guide_toggle = CheckButton.new()
	_frame_guide_toggle.text = "显示实际卡牌可视框"
	_frame_guide_toggle.button_pressed = bool(_ui_settings.show_frame_guide)
	_frame_guide_toggle.toggled.connect(_on_frame_guide_toggled)
	right.add_child(_frame_guide_toggle)

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

	_background_toggle = CheckButton.new()
	_background_toggle.text = "启用背景"
	_background_toggle.toggled.connect(_on_toggle_changed)
	right.add_child(_background_toggle)
	var background_row := HBoxContainer.new()
	background_row.add_theme_constant_override("separation", 6)
	right.add_child(background_row)
	var palette_column := VBoxContainer.new()
	palette_column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	palette_column.add_child(_make_field_label("配色"))
	background_row.add_child(palette_column)
	_background_option = OptionButton.new()
	_background_option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for key in BACKGROUNDS:
		var index := _background_option.item_count
		_background_option.add_item(BACKGROUNDS[key].label)
		_background_option.set_item_metadata(index, key)
	_background_option.item_selected.connect(_on_background_changed)
	palette_column.add_child(_background_option)

	var gradient_column := VBoxContainer.new()
	gradient_column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	gradient_column.add_child(_make_field_label("渐变"))
	background_row.add_child(gradient_column)
	_gradient_option = OptionButton.new()
	_gradient_option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for key in GRADIENT_MODES:
		var index := _gradient_option.item_count
		_gradient_option.add_item(GRADIENT_MODES[key])
		_gradient_option.set_item_metadata(index, key)
	_gradient_option.item_selected.connect(_on_form_changed)
	gradient_column.add_child(_gradient_option)

	var texture_column := VBoxContainer.new()
	texture_column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	texture_column.add_child(_make_field_label("纹理"))
	background_row.add_child(texture_column)
	_texture_option = OptionButton.new()
	_texture_option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for key in BACKGROUND_TEXTURES:
		var index := _texture_option.item_count
		_texture_option.add_item(BACKGROUND_TEXTURES[key])
		_texture_option.set_item_metadata(index, key)
	_texture_option.item_selected.connect(_on_form_changed)
	texture_column.add_child(_texture_option)

	_placeholder_toggle = CheckButton.new()
	_placeholder_toggle.text = "显示占位图形"
	_placeholder_toggle.toggled.connect(_on_toggle_changed)
	right.add_child(_placeholder_toggle)
	_motif_controls = VBoxContainer.new()
	_motif_controls.add_theme_constant_override("separation", 6)
	right.add_child(_motif_controls)
	_motif_controls.add_child(_make_field_label("占位图形"))
	_motif_option = OptionButton.new()
	for key in MOTIFS:
		var index := _motif_option.item_count
		_motif_option.add_item(MOTIFS[key])
		_motif_option.set_item_metadata(index, key)
	_motif_option.item_selected.connect(_on_form_changed)
	_motif_controls.add_child(_motif_option)

	var color_row := HBoxContainer.new()
	color_row.add_theme_constant_override("separation", 8)
	right.add_child(color_row)
	var top_box := VBoxContainer.new()
	top_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	color_row.add_child(top_box)
	top_box.add_child(_make_field_label("颜色 A / 中心"))
	_top_color = ColorPickerButton.new()
	_top_color.custom_minimum_size.y = 36
	_top_color.color_changed.connect(_on_custom_color_changed)
	top_box.add_child(_top_color)
	var bottom_box := VBoxContainer.new()
	bottom_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	color_row.add_child(bottom_box)
	bottom_box.add_child(_make_field_label("颜色 B / 外围"))
	_bottom_color = ColorPickerButton.new()
	_bottom_color.custom_minimum_size.y = 36
	_bottom_color.color_changed.connect(_on_custom_color_changed)
	bottom_box.add_child(_bottom_color)

	right.add_child(_make_field_label("缩放"))
	var zoom_row := HBoxContainer.new()
	right.add_child(zoom_row)
	_zoom_slider = HSlider.new()
	_zoom_slider.min_value = 0.05
	_zoom_slider.max_value = 4.0
	_zoom_slider.step = 0.01
	_zoom_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_zoom_slider.value_changed.connect(_on_zoom_changed)
	zoom_row.add_child(_zoom_slider)
	_zoom_value = Label.new()
	_zoom_value.custom_minimum_size.x = 52
	_zoom_value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	zoom_row.add_child(_zoom_value)

	_progress_label = Label.new()
	_progress_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	right.add_child(_progress_label)

	_cache_label = Label.new()
	_cache_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_cache_label.add_theme_color_override("font_color", Color("858d9e"))
	right.add_child(_cache_label)

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
		"version": 4,
		"output_dir": "AK_Exusiai/images/cards",
		"resolution_scale": 2,
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
	var resolution_scale := int(_manifest.get("resolution_scale", 2))
	if resolution_scale not in RESOLUTION_SCALES:
		resolution_scale = 2
	_manifest.resolution_scale = resolution_scale
	_migrate_manifest_schema()


func _migrate_manifest_schema() -> void:
	var changed := int(_manifest.get("version", 1)) < 4
	for card_name in _manifest.cards:
		var saved = _manifest.cards[card_name]
		if saved is not Dictionary:
			continue
		var old_mode := String(saved.get("mode", "crop"))
		if saved.has("mode"):
			if old_mode == "icon":
				saved.zoom = _convert_legacy_icon_zoom(saved)
			saved.erase("mode")
			changed = true
		if not saved.has("background_enabled"):
			saved.background_enabled = true
			changed = true
		if not saved.has("placeholder_enabled"):
			saved.placeholder_enabled = old_mode == "placeholder"
			changed = true
		if not saved.has("rotation"):
			saved.rotation = 0
			changed = true
		if not saved.has("flip_horizontal"):
			saved.flip_horizontal = false
			changed = true
		if not saved.has("flip_vertical"):
			saved.flip_vertical = false
			changed = true
		if not saved.has("gradient_mode"):
			saved.gradient_mode = "vertical"
			changed = true
		if not saved.has("texture"):
			saved.texture = "streaks"
			changed = true
		_manifest.cards[card_name] = saved
	_manifest.version = 4
	if changed:
		_save_manifest()


func _convert_legacy_icon_zoom(saved: Dictionary) -> float:
	var old_zoom := float(saved.get("zoom", 1.0))
	var source_path := String(saved.get("source", ""))
	if source_path.is_empty():
		return old_zoom * 0.5
	var source := _load_source_image(source_path)
	if source == null or source.is_empty():
		return old_zoom * 0.5
	var bounds := _find_visible_bounds(source)
	if bounds.size == Vector2i.ZERO:
		return old_zoom * 0.5
	var output_size := BASE_NORMAL_SIZE * int(_manifest.get("resolution_scale", 2))
	var legacy_base := minf(
		output_size.x * 0.62 / bounds.size.x,
		output_size.y * 0.68 / bounds.size.y
	)
	var unified_base := maxf(
		float(output_size.x) / bounds.size.x,
		float(output_size.y) / bounds.size.y
	)
	return snappedf(old_zoom * legacy_base / unified_base, 0.001)


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
		var card_type := "skill"
		if source.contains("CardType.Attack"):
			card_type = "attack"
		elif source.contains("CardType.Power"):
			card_type = "power"
		var title: String = titles.get(card_class, card_class)
		_cards.append({
			"class_name": card_class,
			"title": title,
			"ancient": ancient,
			"card_type": card_type,
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
	_reconcile_asset_caches()


func _reconcile_asset_caches() -> void:
	var current_signatures := {}
	var changed := false
	for path in _assets:
		var absolute := _absolute_from_stored(path)
		var signature := "%d:%d" % [
			FileAccess.get_modified_time(absolute),
			FileAccess.get_size(absolute),
		]
		current_signatures[path] = signature
		if _asset_signatures.has(path) and _asset_signatures[path] == signature:
			continue
		_source_cache.erase(path)
		_thumbnail_cache.erase(path)
		changed = true
	for old_path in _asset_signatures:
		if current_signatures.has(old_path):
			continue
		_source_cache.erase(old_path)
		_thumbnail_cache.erase(old_path)
		changed = true
	_asset_signatures = current_signatures
	if changed:
		_preview_material_cache.clear()


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
		var item_text := _asset_item_text(path, compact)
		var index := _asset_list.add_item(item_text)
		_asset_list.set_item_tooltip(index, path)
		if _thumbnail_cache.has(path):
			_asset_list.set_item_icon(index, _thumbnail_cache[path])
		else:
			_thumbnail_queue.append({"index": index, "path": path})
	_update_asset_columns()


func _asset_item_text(path: String, compact: bool) -> String:
	var item_text := path.get_file().get_basename()
	if compact:
		return item_text
	return item_text + "  ·  %s · %s  ·  %s" % [
		path.get_extension().to_upper(),
		_format_bytes(FileAccess.get_size(_absolute_from_stored(path))),
		_folder_display_name(path.get_base_dir()),
	]


func _get_thumbnail(path: String) -> ImageTexture:
	if _thumbnail_cache.has(path):
		return _thumbnail_cache[path]
	var disk_cache_path := _thumbnail_disk_path(path)
	if FileAccess.file_exists(disk_cache_path):
		var cached_image := Image.new()
		if cached_image.load(ProjectSettings.globalize_path(disk_cache_path)) == OK and not cached_image.is_empty():
			var cached_texture := ImageTexture.create_from_image(cached_image)
			_trim_thumbnail_memory_cache()
			_thumbnail_cache[path] = cached_texture
			_update_cache_label()
			return cached_texture
	var image := _load_image_uncached(path, 0, 1.0)
	if image == null or image.is_empty():
		return null
	var thumbnail := image
	var thumbnail_scale := minf(88.0 / thumbnail.get_width(), 62.0 / thumbnail.get_height())
	thumbnail.resize(
		maxi(1, roundi(thumbnail.get_width() * thumbnail_scale)),
		maxi(1, roundi(thumbnail.get_height() * thumbnail_scale)),
		Image.INTERPOLATE_BILINEAR
	)
	var texture := ImageTexture.create_from_image(thumbnail)
	_trim_thumbnail_memory_cache()
	_thumbnail_cache[path] = texture
	var disk_cache_absolute := ProjectSettings.globalize_path(disk_cache_path)
	if DirAccess.make_dir_recursive_absolute(disk_cache_absolute.get_base_dir()) == OK:
		var previous_size := FileAccess.get_size(disk_cache_path) if FileAccess.file_exists(disk_cache_path) else 0
		if thumbnail.save_png(disk_cache_absolute) == OK:
			_disk_thumbnail_cache_bytes += FileAccess.get_size(disk_cache_path) - previous_size
			_prune_thumbnail_disk_cache()
	_update_cache_label()
	return texture


func _thumbnail_disk_path(path: String) -> String:
	var signature := String(_asset_signatures.get(path, "unknown"))
	var cache_key := "%s|%s|88x62-v1" % [path, signature]
	return THUMBNAIL_CACHE_DIR.path_join(cache_key.sha256_text() + ".png")


func _trim_thumbnail_memory_cache() -> void:
	while _thumbnail_cache.size() >= THUMBNAIL_MEMORY_MAX_ITEMS:
		var keys := _thumbnail_cache.keys()
		if keys.is_empty():
			break
		_thumbnail_cache.erase(keys[0])


func _refresh_cache_stats_from_disk() -> void:
	_disk_thumbnail_cache_bytes = 0
	var directory := DirAccess.open(THUMBNAIL_CACHE_DIR)
	if directory != null:
		for filename in directory.get_files():
			if filename.get_extension().to_lower() == "png":
				_disk_thumbnail_cache_bytes += FileAccess.get_size(THUMBNAIL_CACHE_DIR.path_join(filename))
	_update_cache_label()


func _prune_thumbnail_disk_cache() -> void:
	if _disk_thumbnail_cache_bytes <= THUMBNAIL_CACHE_MAX_BYTES:
		_update_cache_label()
		return
	var directory := DirAccess.open(THUMBNAIL_CACHE_DIR)
	if directory == null:
		_disk_thumbnail_cache_bytes = 0
		_update_cache_label()
		return
	var entries: Array[Dictionary] = []
	for filename in directory.get_files():
		if filename.get_extension().to_lower() != "png":
			continue
		var stored_path := THUMBNAIL_CACHE_DIR.path_join(filename)
		entries.append({
			"path": stored_path,
			"size": FileAccess.get_size(stored_path),
			"modified": FileAccess.get_modified_time(stored_path),
		})
	entries.sort_custom(func(a: Dictionary, b: Dictionary) -> bool: return int(a.modified) < int(b.modified))
	for entry in entries:
		if _disk_thumbnail_cache_bytes <= THUMBNAIL_CACHE_MAX_BYTES:
			break
		var absolute := ProjectSettings.globalize_path(String(entry.path))
		if DirAccess.remove_absolute(absolute) == OK:
			_disk_thumbnail_cache_bytes -= int(entry.size)
	_update_cache_label()


func _clear_thumbnail_cache() -> void:
	var removed := 0
	var directory := DirAccess.open(THUMBNAIL_CACHE_DIR)
	if directory != null:
		for filename in directory.get_files():
			if filename.get_extension().to_lower() != "png":
				continue
			var absolute := ProjectSettings.globalize_path(THUMBNAIL_CACHE_DIR.path_join(filename))
			if DirAccess.remove_absolute(absolute) == OK:
				removed += 1
	_source_cache.clear()
	_thumbnail_cache.clear()
	_thumbnail_queue.clear()
	for index in _asset_list.item_count:
		_asset_list.set_item_icon(index, null)
	_preview_base_cache.clear()
	_preview_material_cache.clear()
	_disk_thumbnail_cache_bytes = 0
	_update_cache_label()
	_set_status("已清理 %d 个缩略图缓存；可点击“重新扫描”按需重建。" % removed, false)


func _update_cache_label() -> void:
	if _cache_label == null:
		return
	_cache_label.text = "磁盘缩略图：%s / %s\n内存缓存：%s（%d 个缩略图 · %d 张原图）" % [
		_format_bytes(_disk_thumbnail_cache_bytes),
		_format_bytes(THUMBNAIL_CACHE_MAX_BYTES),
		_format_bytes(_estimate_memory_cache_bytes()),
		_thumbnail_cache.size(),
		_source_cache.size(),
	]


func _estimate_memory_cache_bytes() -> int:
	var total := 0
	for value in _source_cache.values():
		if value is Image:
			total += value.get_width() * value.get_height() * 4
	for value in _preview_base_cache.values():
		if value is Image:
			total += value.get_width() * value.get_height() * 4
	for value in _preview_material_cache.values():
		if value is Image:
			total += value.get_width() * value.get_height() * 4
	for value in _thumbnail_cache.values():
		if value is Texture2D:
			total += value.get_width() * value.get_height() * 4
	return total


func _format_bytes(byte_count: int) -> String:
	if byte_count >= 1024 * 1024 * 1024:
		return "%.2f GB" % (float(byte_count) / (1024.0 * 1024.0 * 1024.0))
	if byte_count >= 1024 * 1024:
		return "%.1f MB" % (float(byte_count) / (1024.0 * 1024.0))
	if byte_count >= 1024:
		return "%.1f KB" % (float(byte_count) / 1024.0)
	return "%d B" % byte_count


func _rebuild_card_options() -> void:
	_loading_ui = true
	_card_option.clear()
	for card in _cards:
		var marker := "○ "
		if _manifest.cards.has(card.class_name):
			var saved: Dictionary = _manifest.cards[card.class_name]
			marker = "● " if not String(saved.get("source", "")).is_empty() else "◇ "
		var ancient := "【先古】" if card.ancient else ""
		var label := "%s%s%s (%s)" % [marker, ancient, card.title, card.class_name]
		var index := _card_option.item_count
		_card_option.add_item(label)
		_card_option.set_item_metadata(index, card.class_name)
	_loading_ui = false
	_update_progress()


func _sync_resolution_option() -> void:
	if _resolution_option == null:
		return
	var wanted := int(_manifest.get("resolution_scale", 2))
	_loading_ui = true
	for index in _resolution_option.item_count:
		if int(_resolution_option.get_item_metadata(index)) == wanted:
			_resolution_option.select(index)
			break
	_loading_ui = false


func _on_resolution_selected(index: int) -> void:
	if _loading_ui or index < 0:
		return
	_manifest.resolution_scale = int(_resolution_option.get_item_metadata(index))
	_save_manifest()
	_update_current_card_summary()
	_queue_preview_render()


func _on_frame_guide_toggled(enabled: bool) -> void:
	_ui_settings.show_frame_guide = enabled
	_save_ui_settings()
	_queue_preview_render()


func _get_resolution_scale() -> int:
	return int(_manifest.get("resolution_scale", 2))


func _get_output_size(card: Dictionary) -> Vector2i:
	var base_size := BASE_ANCIENT_SIZE if card.get("ancient", false) else BASE_NORMAL_SIZE
	return base_size * _get_resolution_scale()


func _card_type_label(card_type: String) -> String:
	match card_type:
		"attack":
			return "攻击牌"
		"power":
			return "能力牌"
		_:
			return "技能牌"


func _update_current_card_summary() -> void:
	if _current_card.is_empty():
		return
	var card := _find_card(_current_card)
	if card.is_empty():
		return
	var output_size := _get_output_size(card)
	var rarity_label := "先古卡" if card.get("ancient", false) else "普通卡"
	_ancient_label.text = "%s · %s · 输出 %d × %d" % [
		_card_type_label(String(card.get("card_type", "skill"))),
		rarity_label,
		output_size.x,
		output_size.y,
	]


func _select_card_by_index(index: int) -> void:
	if _loading_ui or index < 0 or index >= _card_option.item_count:
		return
	_current_card = String(_card_option.get_item_metadata(index))
	_loading_ui = true
	var config := _get_effective_config(_current_card)
	_select_option_by_metadata(_background_option, config.background)
	_select_option_by_metadata(_gradient_option, config.gradient_mode)
	_select_option_by_metadata(_texture_option, config.texture)
	_select_option_by_metadata(_motif_option, config.motif)
	_background_toggle.button_pressed = bool(config.background_enabled)
	_placeholder_toggle.button_pressed = bool(config.placeholder_enabled)
	_motif_controls.visible = bool(config.placeholder_enabled)
	_flip_horizontal_button.button_pressed = bool(config.flip_horizontal)
	_flip_vertical_button.button_pressed = bool(config.flip_vertical)
	_top_color.color = Color.from_string(config.top_color, Color("2b2340"))
	_bottom_color.color = Color.from_string(config.bottom_color, Color("bd6c59"))
	_zoom_slider.value = config.zoom
	_zoom_value.text = "%.2f" % config.zoom
	_source_label.text = config.source if not String(config.source).is_empty() else "未选择"
	_update_current_card_summary()
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
	var gradient_mode := String(saved.get("gradient_mode", "vertical"))
	if not GRADIENT_MODES.has(gradient_mode):
		gradient_mode = "vertical"
	var texture := String(saved.get("texture", "streaks"))
	if not BACKGROUND_TEXTURES.has(texture):
		texture = "streaks"
	return {
		"source": String(saved.get("source", "")),
		"zoom": float(saved.get("zoom", 1.0)),
		"offset": saved.get("offset", [0.0, 0.0]),
		"background_enabled": bool(saved.get("background_enabled", true)),
		"placeholder_enabled": bool(saved.get("placeholder_enabled", false)),
		"background": background,
		"gradient_mode": gradient_mode,
		"texture": texture,
		"top_color": String(saved.get("top_color", background_data.top)),
		"bottom_color": String(saved.get("bottom_color", background_data.bottom)),
		"motif": String(saved.get("motif", "auto")),
		"rotation": posmod(int(saved.get("rotation", 0)), 4),
		"flip_horizontal": bool(saved.get("flip_horizontal", false)),
		"flip_vertical": bool(saved.get("flip_vertical", false)),
	}


func _store_current_form() -> void:
	if _loading_ui or _current_card.is_empty():
		return
	var previous := _get_effective_config(_current_card)
	var config := {
		"source": String(previous.source),
		"zoom": snappedf(float(_zoom_slider.value), 0.001),
		"offset": previous.offset,
		"background_enabled": _background_toggle.button_pressed,
		"placeholder_enabled": _placeholder_toggle.button_pressed,
		"background": String(_background_option.get_item_metadata(_background_option.selected)),
		"gradient_mode": String(_gradient_option.get_item_metadata(_gradient_option.selected)),
		"texture": String(_texture_option.get_item_metadata(_texture_option.selected)),
		"top_color": _top_color.color.to_html(false),
		"bottom_color": _bottom_color.color.to_html(false),
		"motif": String(_motif_option.get_item_metadata(_motif_option.selected)),
		"rotation": int(previous.rotation),
		"flip_horizontal": bool(previous.flip_horizontal),
		"flip_vertical": bool(previous.flip_vertical),
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
	config.placeholder_enabled = false
	config.offset = [0.0, 0.0]
	_manifest.cards[_current_card] = config
	_source_label.text = path
	_loading_ui = true
	_placeholder_toggle.button_pressed = false
	_motif_controls.visible = false
	_loading_ui = false
	_schedule_manifest_save()
	_rebuild_card_options_preserving_current()
	_queue_preview_render()


func _on_form_changed(_index: int) -> void:
	_store_current_form()


func _on_toggle_changed(_enabled: bool) -> void:
	if _motif_controls != null:
		_motif_controls.visible = _placeholder_toggle.button_pressed
	_store_current_form()


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
	_loading_ui = true
	_zoom_slider.value = zoom
	_zoom_value.text = "%.2f" % zoom
	_loading_ui = false
	_store_transform(zoom, offset)


func _reset_transform() -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	config.zoom = 1.0
	config.offset = [0.0, 0.0]
	config.rotation = 0
	config.flip_horizontal = false
	config.flip_vertical = false
	_manifest.cards[_current_card] = config
	_schedule_manifest_save()
	_preview.set_transform_values(1.0, Vector2.ZERO)
	_queue_preview_render()
	_loading_ui = true
	_zoom_slider.value = 1.0
	_zoom_value.text = "1.00"
	_flip_horizontal_button.button_pressed = false
	_flip_vertical_button.button_pressed = false
	_loading_ui = false


func _center_material(horizontal: bool) -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	var offset := _array_to_vector2(config.offset)
	if horizontal:
		offset.x = 0.0
	else:
		offset.y = 0.0
	_store_transform(float(config.zoom), offset)


func _rotate_material(direction: int) -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	config.rotation = posmod(int(config.rotation) + direction, 4)
	_manifest.cards[_current_card] = config
	_schedule_manifest_save()
	_queue_preview_render()


func _flip_material(horizontal: bool) -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	if horizontal:
		config.flip_horizontal = not bool(config.flip_horizontal)
	else:
		config.flip_vertical = not bool(config.flip_vertical)
	_manifest.cards[_current_card] = config
	_schedule_manifest_save()
	_queue_preview_render()


func _clear_source() -> void:
	if _current_card.is_empty():
		return
	var config := _get_effective_config(_current_card)
	config.source = ""
	config.placeholder_enabled = true
	config.offset = [0.0, 0.0]
	_manifest.cards[_current_card] = config
	_source_label.text = "未选择"
	_loading_ui = true
	_placeholder_toggle.button_pressed = true
	_motif_controls.visible = true
	_loading_ui = false
	_schedule_manifest_save()
	_rebuild_card_options_preserving_current()
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
	var output_size := _get_output_size(card)
	var preview_size := output_size
	if preview_size.x > 500:
		var preview_scale := 500.0 / preview_size.x
		preview_size = Vector2i(
			500,
			maxi(1, roundi(preview_size.y * preview_scale))
		)
	var image := _render_card_art(config, preview_size, _current_card, true)
	_preview.set_transform_values(config.zoom, _array_to_vector2(config.offset))
	_preview.set_frame_guide(
		String(card.get("card_type", "skill")),
		bool(card.get("ancient", false)),
		bool(_ui_settings.show_frame_guide)
	)
	_preview.set_art(image, output_size)
	_update_cache_label()
	if image == null or image.is_empty():
		_preview.set_empty_message("请选择素材或启用背景 / 占位图形")


func _render_card_art(
	config: Dictionary,
	output_size: Vector2i,
	card_name: String,
	use_preview_cache := false
) -> Image:
	var result := _get_base_canvas(config, output_size, card_name, use_preview_cache)
	var source_path := String(config.source)
	if source_path.is_empty():
		return result
	var source := _load_source_image(source_path)
	if source == null or source.is_empty():
		return result
	var material := _get_transformed_material(source, source_path, config, output_size, use_preview_cache)
	if material == null or material.is_empty():
		return result
	var offset := _array_to_vector2(config.offset)
	var position := Vector2i(
		roundi((output_size.x - material.get_width()) * 0.5 + offset.x * output_size.x),
		roundi((output_size.y - material.get_height()) * 0.5 + offset.y * output_size.y)
	)
	position = _constrain_material_position(position, material.get_size(), output_size)
	_blend_clipped(result, material, position)
	return result


func _constrain_material_position(
	position: Vector2i,
	material_size: Vector2i,
	output_size: Vector2i
) -> Vector2i:
	# Keep a small, grabbable part of the material on the canvas. Legacy crop
	# configs could contain large offsets that were previously hidden by clamping;
	# without this guard they appeared to turn black as soon as zoom dropped below 1.
	var visible_x := mini(material_size.x, maxi(12, roundi(output_size.x * 0.05)))
	var visible_y := mini(material_size.y, maxi(12, roundi(output_size.y * 0.05)))
	return Vector2i(
		clampi(position.x, visible_x - material_size.x, output_size.x - visible_x),
		clampi(position.y, visible_y - material_size.y, output_size.y - visible_y)
	)


func _get_base_canvas(
	config: Dictionary,
	output_size: Vector2i,
	card_name: String,
	use_preview_cache: bool
) -> Image:
	var cache_key := "%dx%d|%s|%s|%s|%s|%s|%s|%s|%s" % [
		output_size.x,
		output_size.y,
		card_name,
		str(bool(config.background_enabled)),
		String(config.top_color),
		String(config.bottom_color),
		String(config.gradient_mode),
		String(config.texture),
		str(bool(config.placeholder_enabled)),
		String(config.motif),
	]
	if use_preview_cache and _preview_base_cache.has(cache_key):
		return (_preview_base_cache[cache_key] as Image).duplicate()
	var result: Image
	if bool(config.background_enabled):
		result = _make_background(output_size, config, card_name)
	else:
		result = Image.create(output_size.x, output_size.y, false, Image.FORMAT_RGBA8)
		result.fill(Color(0, 0, 0, 0))
	if bool(config.placeholder_enabled):
		_draw_placeholder_motif(result, String(config.motif), absi(card_name.hash()))
	if use_preview_cache:
		if _preview_base_cache.size() >= 32:
			_preview_base_cache.clear()
		_preview_base_cache[cache_key] = result.duplicate()
	return result


func _get_transformed_material(
	source: Image,
	source_path: String,
	config: Dictionary,
	output_size: Vector2i,
	use_preview_cache: bool
) -> Image:
	var cache_key := "%s|%dx%d|%.4f|%d|%s|%s" % [
		source_path,
		output_size.x,
		output_size.y,
		float(config.zoom),
		int(config.rotation),
		str(bool(config.flip_horizontal)),
		str(bool(config.flip_vertical)),
	]
	if use_preview_cache and _preview_material_cache.has(cache_key):
		return _preview_material_cache[cache_key]
	var bounds := _find_visible_bounds(source)
	if bounds.size == Vector2i.ZERO:
		return Image.new()
	var material := source.get_region(bounds)
	for _turn in posmod(int(config.rotation), 4):
		material.rotate_90(CLOCKWISE)
	if bool(config.flip_horizontal):
		material.flip_x()
	if bool(config.flip_vertical):
		material.flip_y()
	var scale := maxf(
		float(output_size.x) / float(material.get_width()),
		float(output_size.y) / float(material.get_height())
	) * maxf(float(config.zoom), 0.01)
	material.resize(
		maxi(1, roundi(material.get_width() * scale)),
		maxi(1, roundi(material.get_height() * scale)),
		Image.INTERPOLATE_LANCZOS
	)
	if use_preview_cache:
		_trim_image_memory_cache(
			_preview_material_cache,
			PREVIEW_MATERIAL_MAX_BYTES,
			material.get_width() * material.get_height() * 4
		)
		_preview_material_cache[cache_key] = material
	return material


func _trim_image_memory_cache(cache: Dictionary, max_bytes: int, incoming_bytes: int) -> void:
	var current_bytes := _estimate_image_dictionary_bytes(cache)
	while not cache.is_empty() and current_bytes + incoming_bytes > max_bytes:
		var keys := cache.keys()
		var oldest_key = keys[0]
		var oldest = cache[oldest_key]
		if oldest is Image:
			current_bytes -= oldest.get_width() * oldest.get_height() * 4
		cache.erase(oldest_key)


func _estimate_image_dictionary_bytes(cache: Dictionary) -> int:
	var total := 0
	for value in cache.values():
		if value is Image:
			total += value.get_width() * value.get_height() * 4
	return total


func _blend_clipped(target: Image, source: Image, position: Vector2i) -> void:
	var source_x := maxi(0, -position.x)
	var source_y := maxi(0, -position.y)
	var target_x := maxi(0, position.x)
	var target_y := maxi(0, position.y)
	var width := mini(source.get_width() - source_x, target.get_width() - target_x)
	var height := mini(source.get_height() - source_y, target.get_height() - target_y)
	if width <= 0 or height <= 0:
		return
	target.blend_rect(source, Rect2i(source_x, source_y, width, height), Vector2i(target_x, target_y))


func _make_background(output_size: Vector2i, config: Dictionary, card_name: String) -> Image:
	var image := Image.create(output_size.x, output_size.y, false, Image.FORMAT_RGBA8)
	var top := Color.from_string(String(config.top_color), Color("2b2340"))
	var bottom := Color.from_string(String(config.bottom_color), Color("bd6c59"))
	var gradient_mode := String(config.gradient_mode)
	var texture := String(config.texture)
	if gradient_mode == "solid" and texture == "none":
		image.fill(Color(top.r, top.g, top.b, 1.0))
		return image
	var seed: int = absi(card_name.hash())
	var glow_center := Vector2(0.35 + float(seed % 31) / 100.0, 0.38)
	var texture_spacing := maxi(12, roundi(minf(output_size.x, output_size.y) / 12.0))
	for y in output_size.y:
		var vertical := float(y) / maxf(1.0, output_size.y - 1.0)
		for x in output_size.x:
			var horizontal := float(x) / maxf(1.0, output_size.x - 1.0)
			var centered := Vector2(horizontal - 0.5, vertical - 0.5)
			var radial := clampf(centered.length() / 0.7071, 0.0, 1.0)
			var mix_amount := 0.0
			match gradient_mode:
				"horizontal":
					mix_amount = horizontal
				"radial":
					mix_amount = radial
				"diagonal":
					mix_amount = (horizontal + vertical) * 0.5
				"solid":
					mix_amount = 0.0
				_:
					mix_amount = vertical
			var color := top.lerp(bottom, smoothstep(0.0, 1.0, mix_amount))
			match texture:
				"streaks":
					var distance := Vector2(horizontal, vertical).distance_to(glow_center)
					var glow := clampf(1.0 - distance / 0.72, 0.0, 1.0)
					color = color.lightened(glow * 0.13)
					var edge := maxf(abs(centered.x) * 2.0, abs(centered.y) * 2.0)
					color = color.darkened(pow(edge, 2.4) * 0.24)
					if posmod(x + y * 2 + seed, 47) <= 1:
						color = color.lightened(0.04)
				"grain":
					var noise := float(posmod(x * x * 17 + y * y * 31 + x * y * 7 + seed, 101)) / 100.0
					if noise >= 0.5:
						color = color.lightened((noise - 0.5) * 0.12)
					else:
						color = color.darkened((0.5 - noise) * 0.1)
				"grid":
					var grid_x := posmod(x + seed, texture_spacing)
					var grid_y := posmod(y + seed / 7, texture_spacing)
					if grid_x <= 1 or grid_y <= 1:
						color = color.lightened(0.08)
					color = color.darkened(pow(radial, 2.0) * 0.12)
				"rays":
					var angle := atan2(centered.y, centered.x)
					var ray := (sin(angle * 14.0 + float(seed % 53)) + 1.0) * 0.5
					color = color.lightened(ray * 0.09 * (1.0 - radial * 0.35))
			image.set_pixel(x, y, Color(color.r, color.g, color.b, 1.0))
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
	var alpha := foreground.a + background.a * (1.0 - foreground.a)
	if alpha <= 0.0001:
		return
	image.set_pixel(x, y, Color(
		(foreground.r * foreground.a + background.r * background.a * (1.0 - foreground.a)) / alpha,
		(foreground.g * foreground.a + background.g * background.a * (1.0 - foreground.a)) / alpha,
		(foreground.b * foreground.a + background.b * background.a * (1.0 - foreground.a)) / alpha,
		alpha
	))


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
		while _source_cache.size() >= SOURCE_MEMORY_MAX_ITEMS:
			var keys := _source_cache.keys()
			if keys.is_empty():
				break
			_source_cache.erase(keys[0])
		_source_cache[stored_path] = image
		_update_cache_label()
	return image


func _load_image_uncached(stored_path: String, max_edge := 1600, svg_scale := 4.0) -> Image:
	var absolute := _absolute_from_stored(stored_path)
	var image := Image.new()
	var error := ERR_FILE_UNRECOGNIZED
	if stored_path.get_extension().to_lower() == "svg":
		var svg_text := FileAccess.get_file_as_string(absolute)
		if not svg_text.is_empty():
			error = image.load_svg_from_string(svg_text, svg_scale)
	else:
		error = image.load(absolute)
	if error != OK:
		_set_status("无法读取素材：%s (%s)" % [stored_path, error_string(error)], true)
		return null
	image.convert(Image.FORMAT_RGBA8)
	var longest_edge := maxi(image.get_width(), image.get_height())
	if max_edge > 0 and longest_edge > max_edge:
		var scale := float(max_edge) / longest_edge
		image.resize(
			maxi(1, roundi(image.get_width() * scale)),
			maxi(1, roundi(image.get_height() * scale)),
			Image.INTERPOLATE_LANCZOS
		)
	return image


func _export_current() -> void:
	if _current_card.is_empty() or _exporting:
		return
	if _export_card(_current_card):
		_set_status("已导出 %s.png" % _current_card, false)


func _run_smoke_test() -> void:
	var crop_config := {
		"source": "references/official/art/半身.jpg",
		"zoom": 0.5,
		"offset": [0.0, 0.0],
		"background_enabled": true,
		"placeholder_enabled": false,
		"background": "laterano_sunset",
		"gradient_mode": "vertical",
		"texture": "streaks",
		"top_color": "3c2059",
		"bottom_color": "ee9558",
		"motif": "auto",
		"rotation": 0,
		"flip_horizontal": false,
		"flip_vertical": false,
	}
	var icon_config := crop_config.duplicate(true)
	icon_config.source = "references/free/art/弹药.svg"
	icon_config.zoom = 0.25
	var placeholder_config := crop_config.duplicate(true)
	placeholder_config.source = ""
	placeholder_config.placeholder_enabled = true
	var rotated_config := crop_config.duplicate(true)
	rotated_config.rotation = 1
	rotated_config.flip_horizontal = true
	var solid_config := crop_config.duplicate(true)
	solid_config.source = ""
	solid_config.gradient_mode = "solid"
	solid_config.texture = "none"
	solid_config.top_color = "27405c"
	var radial_config := solid_config.duplicate(true)
	radial_config.gradient_mode = "radial"
	radial_config.texture = "rays"
	var expected_normal := BASE_NORMAL_SIZE * 2
	var expected_ancient := BASE_ANCIENT_SIZE * 2
	var crop := _render_card_art(crop_config, expected_normal, "SmokeCrop")
	var icon := _render_card_art(icon_config, expected_normal, "SmokeIcon")
	var ancient := _render_card_art(placeholder_config, expected_ancient, "SmokeAncient")
	var rotated := _render_card_art(rotated_config, expected_normal, "SmokeRotated")
	var solid := _render_card_art(solid_config, expected_normal, "SmokeSolid")
	var radial := _render_card_art(radial_config, expected_normal, "SmokeRadial")
	_preview_base_cache.clear()
	_preview_material_cache.clear()
	var cached_config := crop_config.duplicate(true)
	_render_card_art(cached_config, expected_normal, "SmokeCached", true)
	cached_config.offset = [0.35, -0.2]
	_render_card_art(cached_config, expected_normal, "SmokeCached", true)
	var constrained := _constrain_material_position(
		Vector2i(5000, 5000),
		Vector2i(100, 80),
		expected_normal
	)
	var cached_thumbnail_reused := false
	var thumbnail_disk_cached := false
	var list_metadata_visible := false
	if not _assets.is_empty():
		var thumbnail_path := _assets[0]
		var list_text := _asset_item_text(thumbnail_path, false)
		list_metadata_visible = (
			list_text.contains(thumbnail_path.get_extension().to_upper())
			and list_text.contains(_format_bytes(FileAccess.get_size(_absolute_from_stored(thumbnail_path))))
		)
		var thumbnail_before := _get_thumbnail(thumbnail_path)
		thumbnail_disk_cached = FileAccess.file_exists(_thumbnail_disk_path(thumbnail_path))
		_refresh_assets()
		var thumbnail_after = _thumbnail_cache.get(thumbnail_path)
		cached_thumbnail_reused = thumbnail_before != null and thumbnail_after == thumbnail_before
	var ancient_count := 0
	var card_type_counts := {"attack": 0, "skill": 0, "power": 0}
	var card_markers := {"○": false, "◇": false, "●": false}
	for option_index in _card_option.item_count:
		var option_text := _card_option.get_item_text(option_index)
		for marker in card_markers:
			if option_text.begins_with(String(marker)):
				card_markers[marker] = true
	for card in _cards:
		if card.ancient:
			ancient_count += 1
		var card_type := String(card.card_type)
		card_type_counts[card_type] = int(card_type_counts.get(card_type, 0)) + 1
	if (
		crop.get_size() != expected_normal
		or icon.get_size() != expected_normal
		or ancient.get_size() != expected_ancient
		or rotated.get_size() != expected_normal
		or solid.get_size() != expected_normal
		or radial.get_size() != expected_normal
		or not solid.get_pixel(0, 0).is_equal_approx(solid.get_pixel(expected_normal.x / 2, expected_normal.y / 2))
		or _preview_base_cache.size() != 1
		or _preview_material_cache.size() != 1
		or constrained.x >= expected_normal.x
		or constrained.y >= expected_normal.y
		or not cached_thumbnail_reused
		or not thumbnail_disk_cached
		or not list_metadata_visible
		or _asset_signatures.size() != _assets.size()
		or _export_progress == null
		or _export_progress_label == null
		or _cache_label == null
		or _disk_thumbnail_cache_bytes > THUMBNAIL_CACHE_MAX_BYTES
		or _thumbnail_cache.size() > THUMBNAIL_MEMORY_MAX_ITEMS
		or _source_cache.size() > SOURCE_MEMORY_MAX_ITEMS
		or _estimate_image_dictionary_bytes(_preview_material_cache) > PREVIEW_MATERIAL_MAX_BYTES
		or _gradient_option.item_count != GRADIENT_MODES.size()
		or _texture_option.item_count != BACKGROUND_TEXTURES.size()
		or _background_option.get_parent().get_parent() != _gradient_option.get_parent().get_parent()
		or _background_option.get_parent().get_parent() != _texture_option.get_parent().get_parent()
		or _background_option.get_item_text(0) != "夕照"
		or _gradient_option.get_item_text(0) != "垂直"
		or _texture_option.get_item_text(1) != "斜纹"
		or _progress_label.text.contains("标记：")
		or _motif_controls == null
		or not bool(card_markers["○"])
		or not bool(card_markers["◇"])
		or not bool(card_markers["●"])
		or _cards.size() != 99
		or ancient_count != 2
		or card_type_counts.attack <= 0
		or card_type_counts.skill <= 0
		or card_type_counts.power <= 0
		or _ui_scale_option.item_count != UI_SCALES.size()
		or _resolution_option.item_count != RESOLUTION_SCALES.size()
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
	rotated.save_png(smoke_dir.path_join("rotated.png"))
	solid.save_png(smoke_dir.path_join("solid.png"))
	radial.save_png(smoke_dir.path_join("radial.png"))
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
	_preview.set_frame_guide("attack", false, true)
	_preview.set_frame_guide("skill", false, true)
	_preview.set_frame_guide("power", false, true)
	_preview.set_frame_guide("attack", true, true)
	print("CARD_ART_MANAGER_SMOKE_OK cards=%d types=%d/%d/%d ancient_cards=%d folders=%d sidebars=%d/%d crop=%s icon=%s ancient=%s" % [
		_cards.size(),
		card_type_counts.attack,
		card_type_counts.skill,
		card_type_counts.power,
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
	if _exporting:
		return
	var card_names: Array[String] = []
	for card_name in _manifest.cards:
		card_names.append(String(card_name))
	if card_names.is_empty():
		_set_status("没有已配置的卡图可供导出。", true)
		return
	_exporting = true
	_export_current_button.disabled = true
	_export_all_button.disabled = true
	_export_progress_box.visible = true
	_export_progress.min_value = 0
	_export_progress.max_value = card_names.size()
	_export_progress.value = 0
	_export_progress_label.text = "准备批量导出 %d 张卡图……" % card_names.size()
	await get_tree().process_frame

	var success := 0
	var skipped := 0
	for index in card_names.size():
		var card_name := card_names[index]
		var card := _find_card(card_name)
		var display_name := card_name
		if not card.is_empty():
			display_name = "%s (%s)" % [String(card.title), card_name]
		_export_progress_label.text = "正在导出 %d / %d：%s" % [
			index + 1,
			card_names.size(),
			display_name,
		]
		# Give the UI one frame to display the next filename before the PNG encode.
		await get_tree().process_frame
		if card.is_empty():
			skipped += 1
		elif _export_card(card_name):
			success += 1
		else:
			skipped += 1
		_export_progress.value = index + 1
		await get_tree().process_frame

	_exporting = false
	_export_current_button.disabled = false
	_export_all_button.disabled = false
	_export_progress_label.text = "导出完成：%d 张成功，%d 张跳过。" % [success, skipped]
	_set_status("批量导出完成：%d 张成功，%d 张跳过。" % [success, skipped], skipped > 0)


func _export_card(card_name: String) -> bool:
	var config := _get_effective_config(card_name)
	if (
		String(config.source).is_empty()
		and not bool(config.background_enabled)
		and not bool(config.placeholder_enabled)
	):
		return false
	var card := _find_card(card_name)
	if card.is_empty():
		return false
	var output_size := _get_output_size(card)
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
	var sourced := 0
	var placeholders := 0
	for card in _cards:
		if _manifest.cards.has(card.class_name):
			configured += 1
			var saved: Dictionary = _manifest.cards[card.class_name]
			if String(saved.get("source", "")).is_empty():
				placeholders += 1
			else:
				sourced += 1
	_progress_label.text = (
		"已配置 %d / %d 张 · 已绑定 %d · 待替换 %d\n" % [
			configured,
			_cards.size(),
			sourced,
			placeholders,
		]
		+ "左侧素材 %d 个" % _assets.size()
	)


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
	_scan_manifest_assets()
	_rebuild_folder_options()
	_rebuild_asset_list()
	_update_progress()
	_update_cache_label()
	_queue_preview_render()
	_set_status(
		"素材列表已重新扫描；复用 %d / %d 个缩略图缓存。" % [
			_thumbnail_cache.size(),
			_assets.size(),
		],
		false
	)


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
