extends Control

const MANIFEST := "res://tools/card_effect_manager/card_effect_manifest.json"
const CATALOG := "res://AK_Exusiai/config/card_effect_catalog.json"
const OUTPUT := "res://AK_Exusiai/config/card_effects.json"
var _bridge_dir := ""
const SETTINGS := "user://card_effect_manager_ui.json"
const PREVIEW = preload("res://tools/card_effect_manager/effect_preview.gd")
const APPEARANCES := {"0:0": "能天使 · 默认", "0:1": "能天使 · 午夜邮差", "0:2": "能天使 · 野地秘行", "0:3": "能天使 · 城市骑手", "1:0": "新约能天使 · 默认", "1:1": "新约能天使 · 寻翼之歌"}
var _catalog: Dictionary = {}
var _entries: Dictionary = {}
var _manifest: Dictionary = {}
var _cards: Array[Dictionary] = []
var _card := ""
var _selected := ""
var _kind := "preset"
var _syncing := false
var _smoke := false
var _poll := 0.0
var _last_status: Dictionary = {}
var _last_request := ""
var _trial_entry := ""
var _undo: Array[Dictionary] = []
var _redo: Array[Dictionary] = []
var _fields: Dictionary = {}
var _asset_list: ItemList
var _asset_search: LineEdit
var _card_search: LineEdit
var _card_picker: OptionButton
var _filter: OptionButton
var _origin: OptionButton
var _prepared_text := ""
var _favorites: CheckButton
var _candidate: CheckButton
var _description: RichTextLabel
var _summary: Label
var _status: Label
var _connection: Label
var _coverage: Label
var _preview: Control
var _target: OptionButton
var _appearance: OptionButton
var _appearance_override: CheckButton
var _x: SpinBox
var _y: SpinBox
var _hits: SpinBox
var _portrait: TextureRect
var _personal: OptionButton
var _personal_name: LineEdit
var _split: HSplitContainer
var _inner: HSplitContainer
var _timer: Timer
var _batch: ConfirmationDialog
var _batch_list: ItemList
var _trial: Button
var _trial_asset: Button
var _verified: Button
var _export_current: Button
var _scale_picker: OptionButton
var _load_error := false

func _ready() -> void:
	_smoke = OS.get_cmdline_user_args().has("--effect-manager-smoke")
	# The game uses a custom user-data directory, unlike this standalone Godot tool.
	_bridge_dir = OS.get_environment("APPDATA").path_join("SlayTheSpire2/AK_Exusiai/card_effect_manager")
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--bridge-dir="): _bridge_dir = argument.trim_prefix("--bridge-dir=")
	get_window().title = "能天使 · 卡牌特效与音效管理器"
	get_window().size = Vector2i(1440, 920)
	get_window().min_size = Vector2i(1040, 700)
	_catalog = _read(CATALOG)
	for entry in _catalog.get("entries", []):
		_entries[entry.id] = entry
	_manifest = _read(MANIFEST)
	_load_error = FileAccess.file_exists(MANIFEST) and (_manifest.is_empty() or int(_manifest.get("schema", 0)) not in [1, 2] or not _manifest.get("cards", {}) is Dictionary)
	if _manifest.is_empty():
		_manifest = {"schema": 1, "cards": {}, "favorites": [], "verified": {}, "presets": {}}
	for key in ["cards", "verified", "presets"]:
		if not _manifest.get(key, {}) is Dictionary: _load_error = true; _manifest[key] = {}
		if not _manifest.has(key): _manifest[key] = {}
	if not _manifest.get("favorites", []) is Array: _load_error = true; _manifest.favorites = []
	if not _manifest.has("favorites"): _manifest.favorites = []
	for id in _manifest.cards.keys():
		var binding = _manifest.cards[id]
		if not binding is Dictionary or not binding.get("base", {}) is Dictionary or (binding.has("upgrade") and not binding.upgrade is Dictionary):
			_load_error = true
			_manifest.cards.erase(id)
	if not _load_error: _migrate_manifest()
	if not _load_error:
		for ref in _manifest.presets:
			var preset = _manifest.presets[ref]
			if not preset is Dictionary or not preset.get("config") is Dictionary or not preset.get("name") is String: _load_error = true
		for binding in _manifest.cards.values():
			var ref: String = binding.get("preset_ref", "")
			if not ref.is_empty() and not _manifest.presets.has(ref) and _entries.get(ref, {}).get("kind", "") != "preset": _load_error = true
	_load_cards()
	_build_ui()
	_rebuild_assets()
	_rebuild_cards()
	_rebuild_personal()
	var settings := _read(SETTINGS)
	_split.split_offset = int(settings.get("left", 320))
	_inner.split_offset = int(settings.get("middle", 650))
	_apply_scale(float(settings.get("scale", 1.0)))
	_split.dragged.connect(func(_value: int): _save_ui())
	_inner.dragged.connect(func(_value: int): _save_ui())
	if _smoke:
		call_deferred("_smoke_test")
	elif _load_error:
		_set_status("清单格式无效，已禁止保存和导出；请修复 card_effect_manifest.json 后重启。")
	else:
		_save()

func _read(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {}
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(path))
	return parsed if parsed is Dictionary else {}

func _write(path: String, data: Dictionary) -> bool:
	# JSON.parse_string represents parsed numbers as floats; C# requires integer schema.
	if data.has("schema"): data.schema = int(data.schema)
	var absolute := ProjectSettings.globalize_path(path)
	DirAccess.make_dir_recursive_absolute(absolute.get_base_dir())
	var file := FileAccess.open(absolute + ".tmp", FileAccess.WRITE)
	if file == null:
		_set_status("无法写入：" + path)
		return false
	file.store_string(JSON.stringify(data, "\t") + "\n")
	file.close()
	var error := DirAccess.rename_absolute(absolute + ".tmp", absolute)
	if error != OK:
		_set_status("保存失败，原文件保留：" + error_string(error))
	return error == OK

func _load_cards() -> void:
	_cards.clear()
	var titles := _read("res://AK_Exusiai/localization/zhs/cards.json")
	var english := _read("res://AK_Exusiai/localization/eng/cards.json")
	var type_regex := RegEx.new()
	type_regex.compile("CardType\\.(Attack|Skill|Power|Status|Curse)")
	for filename in DirAccess.get_files_at("res://AK_ExusiaiCode/Cards"):
		if not filename.ends_with(".cs"):
			continue
		var code := FileAccess.get_file_as_string("res://AK_ExusiaiCode/Cards/" + filename)
		if not code.contains("[RegisterCard("):
			continue
		var class_name_value := filename.get_basename()
		var id := "AK_EXUSIAI_CARD_" + _snake(class_name_value)
		var match_type := type_regex.search(code)
		_cards.append({"id": id, "class": class_name_value, "name": titles.get(id + ".title", class_name_value), "english": english.get(id + ".title", class_name_value), "type": match_type.get_string(1) if match_type else "Skill"})
	_cards.sort_custom(func(a: Dictionary, b: Dictionary): return a.id < b.id)

func _snake(value: String) -> String:
	var r := RegEx.new()
	r.compile("([A-Z]+)([A-Z][a-z])")
	value = r.sub(value, "$1_$2", true)
	r.compile("([a-z0-9])([A-Z])")
	return r.sub(value, "$1_$2", true).to_upper()

func _label(parent: Node, text: String, font_size := 14) -> Label:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", font_size)
	parent.add_child(label)
	return label

func _button(parent: Node, text: String, callback: Callable) -> Button:
	var button := Button.new()
	button.text = text
	button.pressed.connect(callback)
	parent.add_child(button)
	return button

func _options(parent: Node, values: Dictionary, callback: Callable) -> OptionButton:
	var option := OptionButton.new()
	for key in values:
		option.add_item(str(values[key]))
		option.set_item_metadata(option.item_count - 1, key)
	option.fit_to_longest_item = false
	option.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	option.item_selected.connect(func(_index: int): callback.call())
	parent.add_child(option)
	return option

func _row(parent: Node) -> HBoxContainer:
	var box := HBoxContainer.new()
	box.add_theme_constant_override("separation", 8)
	parent.add_child(box)
	return box

func _panel(parent: Node, width: float) -> VBoxContainer:
	var panel := PanelContainer.new()
	panel.custom_minimum_size.x = width
	parent.add_child(panel)
	var margin := MarginContainer.new()
	for side in ["left", "top", "right", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 12)
	panel.add_child(margin)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 8)
	margin.add_child(box)
	return box

func _build_ui() -> void:
	_install_theme()
	var background := ColorRect.new()
	background.color = Color("20232b")
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(background)
	var viewport_scroll := ScrollContainer.new()
	viewport_scroll.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(viewport_scroll)
	var root := VBoxContainer.new()
	root.custom_minimum_size = Vector2(1050, 740)
	root.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	root.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_theme_constant_override("separation", 10)
	viewport_scroll.add_child(root)
	var top := _row(root)
	_label(top, "能天使 · 特效与音效管理器", 21).size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scale_picker = _options(top, {1.0: "100%", 1.25: "125%", 1.5: "150%", 1.75: "175%", 2.0: "200%"}, func(): _apply_scale(float(_scale_picker.get_selected_metadata())))
	_scale_picker.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN; _scale_picker.custom_minimum_size.x = 85
	_button(top, "重新扫描", _refresh)
	_button(top, "撤销", _undo_change)
	_button(top, "重做", _redo_change)
	_export_current = _button(top, "导出当前", func(): _export(false))
	_button(top, "导出全部", func(): _export(true))
	_connection = _label(root, "未连接 · 在能天使单人战斗控制台运行 exusiaifx on")
	_split = HSplitContainer.new(); _split.size_flags_vertical = Control.SIZE_EXPAND_FILL; root.add_child(_split)
	var left := _panel(_split, 245)
	_label(left, "原版效果库", 18)
	var tabs := _row(left)
	for key in {"preset": "组合预设", "vfx": "特效", "sfx": "音效"}:
		var names := {"preset": "组合预设", "vfx": "特效", "sfx": "音效"}
		_button(tabs, names[key], func(): _kind = key; _rebuild_assets())
	_asset_search = LineEdit.new(); _asset_search.placeholder_text = "中文 / 英文 / 卡名 / 资源名"; left.add_child(_asset_search)
	_asset_search.text_changed.connect(func(_text: String): _rebuild_assets())
	_origin = _options(left, {"attacks": "卡牌 / 攻击（默认）", "all": "所有来源", "card": "原版卡牌", "monster": "怪物攻击", "combat": "角色 / 战斗音效", "other": "其他来源"}, _rebuild_assets)
	var filters := _row(left)
	_favorites = CheckButton.new(); _favorites.text = "仅收藏"; filters.add_child(_favorites); _favorites.toggled.connect(func(_on: bool): _rebuild_assets())
	_candidate = CheckButton.new(); _candidate.text = "显示待适配"; filters.add_child(_candidate); _candidate.toggled.connect(func(_on: bool): _rebuild_assets())
	_asset_list = ItemList.new(); _asset_list.size_flags_vertical = Control.SIZE_EXPAND_FILL; left.add_child(_asset_list)
	_asset_list.item_selected.connect(_select_asset)
	_coverage = _label(left, "")
	_description = RichTextLabel.new(); _description.custom_minimum_size.y = 165; _description.bbcode_enabled = false; left.add_child(_description)
	var asset_buttons := _row(left)
	_button(asset_buttons, "收藏 / 取消", _favorite)
	_verified = _button(asset_buttons, "标记已验证", _mark_verified); _verified.disabled = true
	var apply_row := _row(left)
	_fields.slot = _options(apply_row, {"hit": "命中位置", "launch": "出手位置"}, func(): pass)
	_button(apply_row, "应用所选", _apply_entry)
	_inner = HSplitContainer.new(); _inner.size_flags_horizontal = Control.SIZE_EXPAND_FILL; _split.add_child(_inner)
	var middle := _panel(_inner, 310)
	_label(middle, "位置与播放阶段", 18)
	_appearance = _options(middle, APPEARANCES, _sync_form)
	_appearance_override = CheckButton.new(); _appearance_override.text = "为此外观单独保存发射偏移"; middle.add_child(_appearance_override)
	_appearance_override.toggled.connect(_toggle_appearance)
	_preview = PREVIEW.new(); _preview.size_flags_vertical = Control.SIZE_EXPAND_FILL; middle.add_child(_preview)
	_preview.offset_changed.connect(_drag_offset)
	var coords := _row(middle)
	_label(coords, "X"); _x = _spin(coords, -1500, 1500, 1, _offset_changed)
	_label(coords, "Y"); _y = _spin(coords, -1500, 1500, 1, _offset_changed)
	_label(middle, "出手  →  发射  →  命中  →  收尾", 16)
	_target = _options(middle, {-1: "全部敌人", -2: "自身（技能 / 能力）"}, _prepare_preview)
	var test_row := _row(middle)
	_label(test_row, "试播段数"); _hits = _spin(test_row, 1, 12, 1, _prepare_preview); _hits.value = 1
	_trial = _button(middle, "试播当前卡牌效果 · F5", func(): _send_play(false))
	_trial_asset = _button(middle, "只试播左侧所选素材", func(): _send_play(true))
	var bridge_row := _row(middle)
	_button(bridge_row, "停止试播", func(): _send({"action": "stop"}))
	_button(bridge_row, "检查游戏资源", func(): _send({"action": "audit"}))
	var right_panel := _panel(_inner, 360)
	var scroll := ScrollContainer.new(); scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL; right_panel.add_child(scroll)
	var right := VBoxContainer.new(); right.size_flags_horizontal = Control.SIZE_EXPAND_FILL; right.add_theme_constant_override("separation", 8); scroll.add_child(right)
	_label(right, "对应卡牌", 18)
	_card_search = LineEdit.new(); _card_search.placeholder_text = "搜索能天使卡牌"; right.add_child(_card_search)
	_card_search.text_changed.connect(func(_text: String): _rebuild_cards())
	_filter = _options(right, {"all": "全部卡牌", "unconfigured": "未配置", "configured": "已配置", "Attack": "攻击牌", "Skill": "技能牌", "Power": "能力牌"}, _rebuild_cards)
	_card_picker = OptionButton.new(); _card_picker.fit_to_longest_item = false; _card_picker.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS; right.add_child(_card_picker); _card_picker.item_selected.connect(_select_card)
	_portrait = TextureRect.new(); _portrait.custom_minimum_size = Vector2(120, 85); _portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE; _portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED; right.add_child(_portrait)
	_summary = _label(right, ""); _summary.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_label(right, "组合预设", 13)
	_personal = _options(right, {"": "独立配置"}, _load_personal)
	_personal_name = LineEdit.new(); _personal_name.placeholder_text = "预设名称"; right.add_child(_personal_name)
	var preset_actions := _row(right)
	_button(preset_actions, "修改名称", _rename_personal)
	_button(preset_actions, "另存为预设", _save_personal)
	_button(preset_actions, "解除关联", _detach_personal)
	right.move_child(_summary, right.get_child_count() - 1)
	for pair in [["launch", "launch_sound"], ["hit", "hit_sound"]]:
		var paired := _row(right)
		for field in pair:
			var captions := {"launch": "出手特效", "hit": "命中特效", "launch_sound": "出手音效", "hit_sound": "命中音效"}
			var column := _column(paired)
			_label(column, captions[field], 13)
			_fields[field] = _options(column, {"inherit": "沿用当前 / 预设", "none": "无"}, _form_changed)
			for id in _entries:
				var entry: Dictionary = _entries[id]
				if entry.status == "adapted" and entry.kind == ("sfx" if str(field).ends_with("sound") else "vfx"):
					_fields[field].add_item(entry.name); _fields[field].set_item_metadata(_fields[field].item_count - 1, id)
	_label(right, "已有角色动作 / 重复策略", 13)
	_fields.animation = _options(right, {"inherit": "沿用当前 / 预设动作", "Attack": "攻击", "Cast": "施法", "none": "无人物动作"}, _form_changed)
	_fields.repeat = _options(right, {"per_hit": "每攻击段播放出手", "once": "每次打出仅一次出手"}, _form_changed)
	_fields.phase = _options(right, {"start": "非攻击牌：出牌开始", "end": "非攻击牌：效果完成"}, _form_changed)
	var timing := _row(right)
	var delay_column := _column(timing); _label(delay_column, "延迟")
	_fields.delay = _spin(delay_column, -1, 5, 0.025, _form_changed); _fields.delay.tooltip_text = "-1 沿用预设；单位：秒"; _fields.delay.value = -1
	var volume_column := _column(timing); _label(volume_column, "独立音量")
	_fields.volume = _spin(volume_column, 0, 2, 0.05, _form_changed); _fields.volume.value = 1
	_label(right, "特效内置音效与震屏保留原版行为", 12)
	_fields.ground = CheckButton.new(); _fields.ground.text = "通用命中特效放在脚底"; right.add_child(_fields.ground); _fields.ground.toggled.connect(func(_v: bool): _form_changed())
	var copies := _row(right)
	_button(copies, "复制", func(): DisplayServer.clipboard_set(JSON.stringify(_config())))
	_button(copies, "粘贴", _paste)
	_button(copies, "批量应用", _show_batch)
	_button(right, "恢复此卡默认表现", _reset_card)
	_status = _label(root, "配置自动保存；导出后通过正常构建发布。", 13); _status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_timer = Timer.new(); _timer.one_shot = true; _timer.wait_time = 0.3; _timer.timeout.connect(_save); add_child(_timer)
	_batch = ConfirmationDialog.new(); _batch.title = "批量覆盖卡牌配置"; _batch.min_size = Vector2i(440, 500); add_child(_batch)
	_batch_list = ItemList.new(); _batch_list.select_mode = ItemList.SELECT_MULTI; _batch_list.custom_minimum_size = Vector2(420, 430); _batch.add_child(_batch_list); _batch.confirmed.connect(_apply_batch)

func _spin(parent: Node, minimum: float, maximum: float, step: float, callback: Callable) -> SpinBox:
	var spin := SpinBox.new(); spin.min_value = minimum; spin.max_value = maximum; spin.step = step; spin.custom_minimum_size.x = 90
	spin.value_changed.connect(func(_value: float): callback.call())
	parent.add_child(spin)
	return spin

func _apply_scale(scale_value: float) -> void:
	var old_scale := maxf(get_window().content_scale_factor, 0.01)
	get_window().content_scale_factor = clampf(scale_value, 1.0, 2.0)
	var usable := DisplayServer.screen_get_usable_rect()
	var new_size := Vector2i(Vector2(get_window().size) * scale_value / old_scale)
	if usable.size.x > 0: new_size = new_size.min(usable.size)
	get_window().size = new_size
	_select(_scale_picker, scale_value)
	_save_ui()

func _save_ui() -> void:
	if not _smoke: _write(SETTINGS, {"scale": get_window().content_scale_factor, "left": _split.split_offset, "middle": _inner.split_offset})

func _meta(option: OptionButton) -> String:
	return str(option.get_selected_metadata()) if option.selected >= 0 else ""

func _select(option: OptionButton, value) -> void:
	for i in range(option.item_count):
		if str(option.get_item_metadata(i)) == str(value):
			option.select(i)
			return
	option.add_item(str(value)); option.set_item_metadata(option.item_count - 1, value); option.select(option.item_count - 1)

func _rebuild_assets() -> void:
	if _asset_list == null:
		return
	_asset_list.clear()
	var needle := _asset_search.text.to_lower()
	var count := 0
	for id in _entries:
		var e: Dictionary = _entries[id]
		if e.kind != _kind or (e.status != "adapted" and not _candidate.button_pressed): continue
		if _favorites.button_pressed and not _manifest.get("favorites", []).has(id): continue
		var origin := _meta(_origin)
		var origins: Array = e.get("origins", [e.get("origin", "other")])
		if origin == "attacks" and not ("card" in origins or "monster" in origins or "combat" in origins or e.kind == "vfx"): continue
		if origin not in ["all", "attacks"] and not origin in origins: continue
		if not needle.is_empty() and not (str(e.name) + " " + str(e.get("english", "")) + " " + id + " " + str(e.get("aliases", []))).to_lower().contains(needle): continue
		var mark := "○ " if e.status == "adapted" else "△ "
		if _manifest.get("verified", {}).get(id, {}).get("game_sha256", "") == _catalog.get("game_sha256", ""): mark = "✓ "
		_asset_list.add_item(mark + str(e.name)); _asset_list.set_item_metadata(count, id)
		if id == _selected: _asset_list.select(count)
		count += 1
	_coverage.text = "%d 项可见 · ○ 待实测  ✓ 已验证  △ 待适配" % count

func _select_asset(index: int) -> void:
	_selected = _asset_list.get_item_metadata(index)
	var e: Dictionary = _entries[_selected]
	_description.text = str(e.name) + "\n" + str(e.get("english", "")) + "\n" + str(e.get("reason", "")) + "\n来源：" + ", ".join(e.get("aliases", []).slice(0, 10))
	if not e.get("embedded_audio", []).is_empty() or e.get("recipe", {}).get("embedded_audio", false):
		_description.text += "\n包含内置声音；独立音量设置不控制内置音效。"
	if e.get("shake", false): _description.text += "\n包含原版震屏。"
	_verified.disabled = true

func _rebuild_cards() -> void:
	_card_picker.clear()
	for c in _cards:
		if not _card_search.text.is_empty() and not (str(c.name) + str(c.english) + str(c.id)).to_lower().contains(_card_search.text.to_lower()): continue
		var configured: bool = _manifest.cards.has(c.id)
		var filter_value := _meta(_filter)
		if filter_value == "configured" and not configured: continue
		if filter_value == "unconfigured" and configured: continue
		if filter_value in ["Attack", "Skill", "Power"] and c.type != filter_value: continue
		_card_picker.add_item(("● " if configured else "○ ") + _card_label(c))
		_card_picker.set_item_metadata(_card_picker.item_count - 1, c.id)
		if c.id == _card: _card_picker.select(_card_picker.item_count - 1)
	if _card_picker.item_count > 0: _select_card(_card_picker.selected if _card_picker.selected >= 0 else 0)
	else: _card = ""; _summary.text = "没有匹配卡牌"; _export_current.disabled = true

func _select_card(index: int) -> void:
	_card = _card_picker.get_item_metadata(index)
	_sync_form()

func _preset_name(ref: String) -> String:
	var preset = _manifest.presets.get(ref, {})
	return str(preset.get("name", _entries.get(ref, {}).get("name", ref))) if preset is Dictionary else ref

func _reference(id := "") -> String:
	return str(_manifest.cards.get(_card if id.is_empty() else id, {}).get("preset_ref", ""))

func _card_label(card: Dictionary) -> String:
	var types := {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Status": "状态", "Curse": "诅咒"}
	var ref := _reference(str(card.id))
	return str(card.name) + " · " + str(types.get(card.type, card.type)) + ("（" + _preset_name(ref) + "）" if not ref.is_empty() else "")

func _resolved(id: String) -> Dictionary:
	var binding: Dictionary = _manifest.cards.get(id, {})
	var ref := _reference(id)
	if not ref.is_empty():
		if _manifest.presets.has(ref):
			var preset = _manifest.presets[ref]
			return preset.config.duplicate(true) if preset is Dictionary and preset.get("config") is Dictionary else {"preset": "invalid:" + ref}
		return {"preset": ref}
	return binding.get("base", {}).duplicate(true)

func _config() -> Dictionary:
	return _resolved(_card)

func _migrate_manifest() -> void:
	if int(_manifest.schema) == 2: return
	# Keep the original on disk before converting legacy copies to references.
	if not _smoke and not FileAccess.file_exists(MANIFEST + ".v1.bak"):
		if not _write(MANIFEST + ".v1.bak", _manifest): _load_error = true; return
	var previous: Dictionary = _manifest.presets.duplicate(true)
	_manifest.presets = {}
	for title in previous:
		_manifest.presets["custom:" + str(title).sha256_text().substr(0, 16)] = {"name": title, "config": previous[title]}
	for id in _manifest.cards:
		var binding: Dictionary = _manifest.cards[id]
		var config: Dictionary = binding.get("base", binding.get("upgrade", {}))
		binding.erase("upgrade")
		binding.base = config
		var matches: Array[String] = []
		for ref in _manifest.presets:
			if config == _manifest.presets[ref].config: matches.append(ref)
		if matches.size() == 1:
			binding.preset_ref = matches[0]; binding.erase("base")
		elif _entries.get(config.get("preset", ""), {}).get("kind", "") == "preset":
			# Preserve customized native recipes as individually named shared presets.
			var native: String = config.preset
			var ref := native if config.size() == 1 else "custom:legacy-" + JSON.stringify(config).sha256_text().substr(0, 16)
			if ref != native: _manifest.presets[ref] = {"name": str(_entries[native].name) + " · 已有配置", "config": config}
			binding.preset_ref = ref; binding.erase("base")
	_manifest.schema = 2

func _sync_form() -> void:
	if _card.is_empty() or _personal == null: return
	_syncing = true
	var config := _config()
	_rebuild_personal()
	_personal_name.text = _preset_name(_reference()) if not _reference().is_empty() else ""
	for field in ["launch", "hit", "launch_sound", "hit_sound", "animation", "repeat", "phase"]:
		var default_value := "per_hit" if field == "repeat" else "start" if field == "phase" else "inherit"
		var value: String = config.get(field, default_value)
		var option: OptionButton = _fields[field]
		_select(option, value)
	_fields.delay.value = config.get("delay", -1)
	_fields.volume.value = config.get("volume", 1)
	_fields.ground.button_pressed = config.get("ground", false)
	var appearance := _meta(_appearance)
	_appearance_override.button_pressed = config.get("appearance_offsets", {}).has(appearance)
	var offset: Array = config.get("appearance_offsets", {}).get(appearance, config.get("offset", [0, 0]))
	_x.value = offset[0]; _y.value = offset[1]
	_preview.offset = Vector2(offset[0], offset[1]); _preview.queue_redraw()
	for c in _cards:
		if c.id == _card:
			_summary.text = _card_label(c)
			if not _reference().is_empty():
				var count := 0
				for id in _manifest.cards:
					if _reference(id) == _reference(): count += 1
				_summary.text += "\n修改下方参数会同步到 %d 张关联卡牌" % count
			var image_path: String = "res://AK_Exusiai/images/cards/" + str(c["class"]) + ".png"
			_portrait.texture = load(image_path) if ResourceLoader.exists(image_path) else null
	_export_current.disabled = false
	_syncing = false
	_prepare_preview()

func _remember() -> void:
	_undo.append(_manifest.duplicate(true))
	if _undo.size() > 40: _undo.pop_front()
	_redo.clear()

func _store(config: Dictionary) -> void:
	if _card.is_empty(): return
	_remember()
	var ref := _reference()
	if ref.is_empty(): _manifest.cards[_card] = {"base": config}
	else: _manifest.presets[ref] = {"name": _preset_name(ref), "config": config}
	if _timer != null: _timer.start()
	_prepare_preview()


func _form_changed() -> void:
	if _syncing or _card.is_empty() or _personal == null: return
	var config := _config()
	for field in ["launch", "hit", "launch_sound", "hit_sound", "animation", "repeat", "phase"]: config[field] = _meta(_fields[field])
	config.delay = _fields.delay.value; config.volume = _fields.volume.value; config.ground = _fields.ground.button_pressed
	_store(config)

func _offset_changed() -> void:
	if _syncing or _personal == null: return
	var config := _config()
	var value := [_x.value, _y.value]
	if _appearance_override.button_pressed:
		if not config.has("appearance_offsets"): config.appearance_offsets = {}
		config.appearance_offsets[_meta(_appearance)] = value
	else: config.offset = value
	_store(config)
	_preview.offset = Vector2(value[0], value[1]); _preview.queue_redraw()

func _drag_offset(value: Vector2) -> void:
	_syncing = true; _x.value = value.x; _y.value = value.y; _syncing = false
	_offset_changed()

func _toggle_appearance(on: bool) -> void:
	if _syncing: return
	var config := _config()
	if not config.has("appearance_offsets"): config.appearance_offsets = {}
	if on: config.appearance_offsets[_meta(_appearance)] = [_x.value, _y.value]
	else: config.appearance_offsets.erase(_meta(_appearance))
	_store(config); _sync_form()

func _apply_entry() -> void:
	if not _entries.has(_selected) or _card.is_empty(): return
	var e: Dictionary = _entries[_selected]
	if e.status != "adapted": _set_status("此项尚待适配，可先选择关联的通用素材。"); return
	var config := _config()
	if e.kind == "preset":
		_bind_preset(_selected)
	else:
		var slot := _meta(_fields.slot)
		config[slot + ("_sound" if e.kind == "sfx" else "")] = _selected
		_store(config); _sync_form()
	_set_status("已应用：" + str(e.name) + "；请游戏内试播。")

func _favorite() -> void:
	if _selected.is_empty(): return
	_remember()
	if _manifest.favorites.has(_selected): _manifest.favorites.erase(_selected)
	else: _manifest.favorites.append(_selected)
	_timer.start(); _rebuild_assets()

func _mark_verified() -> void:
	if _trial_entry != _selected or _last_status.get("state", "") != "done" or _last_status.get("request_id", "") != _last_request or _last_status.get("game_sha256", "") != _catalog.game_sha256: return
	_remember()
	_manifest.verified[_selected] = {"game_sha256": _catalog.game_sha256, "time": Time.get_datetime_string_from_system()}
	_timer.start(); _rebuild_assets(); _set_status("已记录你确认的本机试播结果。")

func _valid(config: Dictionary) -> String:
	for key in ["preset", "launch", "hit", "launch_sound", "hit_sound"]:
		var id: String = config.get(key, "inherit")
		if id in ["inherit", "none"]: continue
		var kind := "preset" if key == "preset" else "sfx" if key.ends_with("sound") else "vfx"
		if _entries.get(id, {}).get("status", "") != "adapted" or _entries.get(id, {}).get("kind", "") != kind: return "无效或未适配的条目：" + id
	if config.get("animation", "inherit") not in ["inherit", "Attack", "Cast", "none"]: return "无效人物动作"
	if config.get("repeat", "per_hit") not in ["once", "per_hit"]: return "无效重复策略"
	if config.get("phase", "start") not in ["start", "end"]: return "无效播放阶段"
	for key in ["delay", "volume"]:
		var value = config.get(key, -1 if key == "delay" else 1)
		if not (value is float or value is int) or not is_finite(float(value)): return "无效数值：" + key
		if (key == "delay" and (value < -1 or value > 5)) or (key == "volume" and (value < 0 or value > 2)): return "数值超出范围：" + key
	var offsets: Array = [config.get("offset", [0, 0])]
	if not config.get("appearance_offsets", {}) is Dictionary: return "无效外观偏移"
	offsets.append_array(config.get("appearance_offsets", {}).values())
	for value in offsets:
		if not value is Array or value.size() != 2: return "偏移必须为两个坐标"
		for coordinate in value:
			if not (coordinate is float or coordinate is int) or not is_finite(float(coordinate)) or abs(float(coordinate)) > 1500: return "偏移超出范围"
	return ""

func _export(all_cards: bool, output_path := OUTPUT) -> void:
	if _load_error: _set_status("清单无效，禁止导出。请修复文件后重启。"); return
	var document := {"schema": 1, "cards": {}} if all_cards else _read(output_path)
	if document.is_empty(): document = {"schema": 1, "cards": {}}
	if not document.get("cards") is Dictionary: _set_status("原导出文件格式无效，请修复后重试。"); return
	var ids: Array = _manifest.cards.keys() if all_cards else [_card]
	# Refresh already exported cards too, so editing any shared preset propagates.
	for id in document.cards.keys():
		if _manifest.cards.has(id) and not ids.has(id): ids.append(id)
		# Preserve separately authored exports while removing the obsolete variant.
		document.cards[id].erase("upgrade")
	for id in ids:
		if not _manifest.cards.has(id): document.cards.erase(id); continue
		var config := _resolved(id)
		var error := _valid(config)
		if not error.is_empty(): _set_status(str(id) + "：" + error); return
		document.cards[id] = {"base": config}
	if _write(output_path, document): _set_status("已导出 %d 张配置；构建 Mod 后正式生效。" % document.cards.size())

func _save() -> void:
	if _smoke or _load_error: return
	if not _write(MANIFEST, _manifest): return
	_rebuild_cards()
	var valid := _publish_override()
	_prepare_preview()
	_set_status("清单已保存；连接游戏后在空闲时热重载。" if valid else "草稿已保存；无效条目未发送到游戏。")

func _publish_override() -> bool:
	var cards := {}
	for id in _manifest.cards:
		var config := _resolved(id)
		if not _valid(config).is_empty(): return false
		cards[id] = {"base": config}
	return _write(_bridge_dir + "/override.json", {"schema": 1, "cards": cards})

func _paste() -> void:
	var data = JSON.parse_string(DisplayServer.clipboard_get())
	if not data is Dictionary: _set_status("剪贴板不是有效配置。"); return
	var error := _valid(data)
	if not error.is_empty(): _set_status(error); return
	_store(data); _sync_form()

func _reset_card() -> void:
	_remember(); _manifest.cards.erase(_card); _timer.start(); _sync_form()

func _undo_change() -> void:
	if _undo.is_empty(): return
	_redo.append(_manifest.duplicate(true)); _manifest = _undo.pop_back(); _timer.start(); _rebuild_cards(); _rebuild_assets()

func _redo_change() -> void:
	if _redo.is_empty(): return
	_undo.append(_manifest.duplicate(true)); _manifest = _redo.pop_back(); _timer.start(); _rebuild_cards(); _rebuild_assets()

func _show_batch() -> void:
	_batch_list.clear()
	for c in _cards:
		_batch_list.add_item(_card_label(c)); _batch_list.set_item_metadata(_batch_list.item_count - 1, c.id)
	_batch.title = "选择将使用同一配置的卡牌"
	_batch.popup_centered()

func _apply_batch() -> void:
	var config := _config(); var ref := _reference(); _remember()
	for index in _batch_list.get_selected_items():
		var id: String = _batch_list.get_item_metadata(index)
		_manifest.cards[id] = {"preset_ref": ref} if not ref.is_empty() else {"base": config.duplicate(true)}
	_timer.start(); _rebuild_cards()

func _bind_preset(ref: String) -> void:
	if _card.is_empty(): return
	_remember()
	_manifest.cards[_card] = {"preset_ref": ref}
	_timer.start(); _rebuild_cards()

func _save_personal() -> void:
	var title := _personal_name.text.strip_edges()
	if title.is_empty() or _card.is_empty(): _set_status("请填写预设名称。"); return
	var config := _config(); _remember()
	var ref := "custom:" + str(Time.get_unix_time_from_system()) + "-" + str(Time.get_ticks_usec())
	_manifest.presets[ref] = {"name": title, "config": config}
	_manifest.cards[_card] = {"preset_ref": ref}
	_timer.start(); _rebuild_cards()

func _rename_personal() -> void:
	var ref := _reference(); var title := _personal_name.text.strip_edges()
	if ref.is_empty() or title.is_empty(): _set_status("请选择预设并填写名称。"); return
	var config := _config(); _remember()
	_manifest.presets[ref] = {"name": title, "config": config}
	_timer.start(); _rebuild_cards()

func _detach_personal() -> void:
	if _card.is_empty() or _reference().is_empty(): return
	var config := _config(); _remember()
	_manifest.cards[_card] = {"base": config}
	_timer.start(); _rebuild_cards()

func _rebuild_personal() -> void:
	_personal.clear(); _personal.add_item("独立配置（未关联预设）"); _personal.set_item_metadata(0, "")
	var refs: Array = _manifest.presets.keys()
	for id in _entries:
		if _entries[id].kind == "preset" and _entries[id].status == "adapted" and not refs.has(id): refs.append(id)
	for ref in refs:
		_personal.add_item(_preset_name(ref)); _personal.set_item_metadata(_personal.item_count - 1, ref)
	_select(_personal, _reference())

func _load_personal() -> void:
	if _syncing: return
	var ref := _meta(_personal)
	if ref.is_empty(): _detach_personal()
	else: _bind_preset(ref)

func _refresh() -> void:
	_catalog = _read(CATALOG); _entries.clear()
	for e in _catalog.get("entries", []): _entries[e.id] = e
	_load_cards(); _rebuild_cards(); _rebuild_assets()
	_set_status("已重新读取本机目录和卡牌。游戏更新后请运行 rebuild_catalog.ps1 重建目录。")

func _send_play(asset_only: bool) -> void:
	if _card.is_empty() or _load_error: _set_status("请先选择有效卡牌。"); return
	var config := _config()
	if asset_only:
		# An isolated asset trial must not inherit the selected card's replacement slots.
		config = {"offset": config.get("offset", [0, 0]), "appearance_offsets": config.get("appearance_offsets", {}), "volume": config.get("volume", 1), "ground": config.get("ground", false)}
	var error := _valid(config)
	if not error.is_empty(): _set_status(error); return
	if asset_only and _entries.get(_selected, {}).get("status", "") != "adapted": _set_status("请先选择已适配的素材。"); return
	_trial_entry = _selected if asset_only else ""
	_send({"action": "play", "entry": _selected if asset_only else "", "slot": _meta(_fields.slot), "config": config, "target": int(_target.get_selected_metadata()), "hits": int(_hits.value)})

func _send(request: Dictionary) -> void:
	if not _connected(): _set_status("请先在能天使单人战斗中运行 exusiaifx on。"); return
	if request.get("action", "") != "play": _trial_entry = ""
	_last_request = str(Time.get_unix_time_from_system()) + "-" + str(Time.get_ticks_usec())
	request.id = _last_request
	_write(_bridge_dir + "/request.json", request)
	_verified.disabled = true

func _connected() -> bool:
	return _last_status.get("enabled", false) and Time.get_unix_time_from_system() * 1000 - float(_last_status.get("timestamp", 0)) < 3000

func _process(delta: float) -> void:
	if _smoke or _connection == null: return
	_poll += delta
	if _poll < 0.35: return
	_poll = 0
	_prepare_preview()
	_last_status = _read(_bridge_dir + "/status.json")
	var online := _connected()
	_trial.disabled = not online; _trial_asset.disabled = not online
	_connection.text = "游戏已连接 · " + str(_last_status.get("message", "")) if online else "未连接 · 在能天使单人战斗控制台运行 exusiaifx on"
	if online:
		var old_target = _target.get_selected_metadata()
		_target.clear(); _target.add_item("全部敌人"); _target.set_item_metadata(0, -1); _target.add_item("自身（技能 / 能力）"); _target.set_item_metadata(1, -2)
		for e in _last_status.get("enemies", []): _target.add_item(str(e.name)); _target.set_item_metadata(_target.item_count - 1, e.index)
		_select(_target, old_target)
		if _last_status.get("request_id", "") == _last_request:
			_verified.disabled = _last_status.get("state", "") != "done" or _selected != _trial_entry or _last_status.get("game_sha256", "") != _catalog.game_sha256
		if _last_status.get("game_sha256", "") != _catalog.get("game_sha256", ""):
			_connection.text += " · 目录版本不同，请重建目录"

func _preview_request() -> Dictionary:
	return {"action": "play", "config": _config(), "target": int(_target.get_selected_metadata()), "hits": int(_hits.value)}

func _prepare_preview() -> void:
	if _smoke or _target == null or _hits == null: return
	var request := {"action": "invalid"} if _load_error or _card.is_empty() else _preview_request()
	if request.has("config") and not _valid(request.config).is_empty(): request = {"action": "invalid"}
	var serialized := JSON.stringify(request)
	if serialized != _prepared_text and _write(_bridge_dir + "/prepared_preview.json", request): _prepared_text = serialized

func _unhandled_key_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_F5:
		_send_play(false); get_viewport().set_input_as_handled()

func _column(parent: Node) -> VBoxContainer:
	var column := VBoxContainer.new(); column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(column)
	return column

func _install_theme() -> void:
	theme = Theme.new()
	for type in ["Button", "OptionButton", "CheckButton", "CheckBox", "MenuButton"]:
		for state in ["normal", "hover", "pressed", "hover_pressed", "disabled", "focus"]:
			var style := StyleBoxFlat.new()
			style.bg_color = Color("343c4b") if state in ["hover", "pressed", "hover_pressed"] else Color("292f3b")
			style.border_color = Color("a1c5ee") if state == "focus" else Color("7f8ea6") if state != "disabled" else Color("526079")
			style.set_border_width_all(1); style.set_corner_radius_all(3)
			style.content_margin_left = 8; style.content_margin_right = 8; style.content_margin_top = 5; style.content_margin_bottom = 5
			if state == "focus": style.bg_color = Color.TRANSPARENT
			theme.set_stylebox(state, type, style)

func _set_status(message: String) -> void:
	if _status != null: _status.text = message
	else: print(message)

func _exit_tree() -> void:
	if not _smoke and _timer != null and not _timer.is_stopped(): _save()

func _smoke_test() -> void:
	var failures: Array[String] = []
	if _cards.size() < 90: failures.append("Card scan incomplete")
	if _entries.size() < 600: failures.append("Catalog incomplete")
	for id in ["card:GrandFinale", "card:Hyperbeam"]:
		if _entries.get(id, {}).get("status", "") != "adapted": failures.append(id)
	if _valid({"hit": "factory:missing"}).is_empty(): failures.append("Invalid ID accepted")
	if _valid({"offset": [1]}).is_empty(): failures.append("Invalid offset accepted")
	if not _valid({"preset": "card:GrandFinale", "offset": [20, -10]}).is_empty(): failures.append("Valid config rejected")
	var original := _manifest.duplicate(true)
	_selected = "card:GrandFinale"; _apply_entry()
	if _config().get("preset", "") != _selected: failures.append("Binding failed")
	_undo_change()
	if _manifest != original: failures.append("Undo failed")
	_redo_change()
	if _config().get("preset", "") != "card:GrandFinale": failures.append("Redo failed")
	_manifest = {"schema": 1, "cards": {"AK_EXUSIAI_CARD_LASER_CANNON": {"base": {"preset": "card:Hyperbeam", "offset": [25, -12]}, "upgrade": {"preset": "card:GrandFinale"}}, "AK_EXUSIAI_CARD_TALENT": {"base": {"preset": "card:Hyperbeam", "offset": [25, -12]}}, "different": {"base": {"hit": "none"}}}, "presets": {"共享测试": {"preset": "card:Hyperbeam", "offset": [25, -12]}}, "favorites": [], "verified": {}}
	_migrate_manifest()
	_card = "AK_EXUSIAI_CARD_LASER_CANNON"
	var shared_ref := _reference()
	if shared_ref.is_empty() or shared_ref != _reference("AK_EXUSIAI_CARD_TALENT"): failures.append("Legacy matching copies not linked")
	if _manifest.cards[_card].has("upgrade") or not _reference("different").is_empty(): failures.append("Migration lost independent config or kept upgrade")
	var updated := _config(); updated.delay = 0.5; _store(updated)
	_personal_name.text = "改名后的预设"; _rename_personal()
	if _resolved("AK_EXUSIAI_CARD_TALENT").get("delay") != 0.5 or not _card_label({"id": "AK_EXUSIAI_CARD_TALENT", "name": "天赋", "type": "Power"}).contains("天赋 · 能力（改名后的预设）"): failures.append("Shared edit/rename did not propagate")
	var export_path := "res://tmp/effect_manager_export_contract.json"
	_write(export_path, {"schema": 1, "cards": {"AK_EXUSIAI_CARD_TALENT": {"base": {"delay": -1}, "upgrade": {}}, "external": {"base": {"hit": "none"}}}})
	_export(false, export_path)
	var exported := _read(export_path)
	if exported.cards.size() != 3 or exported.cards.AK_EXUSIAI_CARD_TALENT.base.delay != 0.5 or exported.cards.AK_EXUSIAI_CARD_TALENT.has("upgrade") or exported.cards.external.base.hit != "none": failures.append("Partial export did not refresh shared cards/preserve separate exports")
	var doc := {"schema": 1, "cards": {}}
	for id in ["AK_EXUSIAI_CARD_LASER_CANNON", "AK_EXUSIAI_CARD_TALENT"]: doc.cards[id] = {"base": _resolved(id)}
	# The runtime must ignore this legacy upgrade even when it differs from Base.
	doc.cards.AK_EXUSIAI_CARD_LASER_CANNON.upgrade = {"preset": "card:GrandFinale"}
	if not _write("res://tmp/effect_manager_contract.json", doc): failures.append("JSON contract fixture failed")
	_card = "AK_EXUSIAI_CARD_TALENT"; _select(_target, -2); _hits.value = 2
	if not _write("res://tmp/effect_manager_preview_contract.json", _preview_request()): failures.append("Prepared preview fixture failed")
	_detach_personal(); _card = "AK_EXUSIAI_CARD_LASER_CANNON"; updated.delay = 1; _store(updated)
	if _resolved("AK_EXUSIAI_CARD_TALENT").get("delay") != 0.5: failures.append("Detached card still changed")
	for id in ["generic:Buff", "generic:Block", "generic:Heal", "generic:Debuff", "generic:Power"]:
		if not _valid({"preset": id}).is_empty(): failures.append("Missing generic preset: " + id)
	_manifest = original
	print("EFFECT_MANAGER_SMOKE: ", "PASS" if failures.is_empty() else str(failures), " cards=", _cards.size(), " entries=", _entries.size())
	get_tree().quit(0 if failures.is_empty() else 1)
