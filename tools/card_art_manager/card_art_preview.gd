class_name CardArtPreview
extends Control

signal transform_changed(zoom: float, offset: Vector2)

var _texture: ImageTexture
var _zoom := 1.0
var _offset := Vector2.ZERO
var _dragging := false
var _frame_rect := Rect2()
var _output_size := Vector2i(500, 380)
var _empty_message := "从左侧选择素材，或启用背景 / 占位图形"
var _card_type := "skill"
var _ancient := false
var _show_frame_guide := true


func _ready() -> void:
	custom_minimum_size = Vector2(500, 420)
	mouse_default_cursor_shape = Control.CURSOR_CROSS
	mouse_filter = Control.MOUSE_FILTER_STOP
	set_process_unhandled_key_input(true)


func set_art(image: Image, output_size: Vector2i) -> void:
	_output_size = output_size
	if image == null or image.is_empty():
		_texture = null
	else:
		_texture = ImageTexture.create_from_image(image)
	queue_redraw()


func set_transform_values(zoom: float, offset: Vector2) -> void:
	_zoom = zoom
	_offset = offset


func set_empty_message(message: String) -> void:
	_empty_message = message
	queue_redraw()


func set_frame_guide(card_type: String, ancient: bool, visible: bool) -> void:
	_card_type = card_type
	_ancient = ancient
	_show_frame_guide = visible
	queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color("17191f"), true)

	var available := size - Vector2(48, 48)
	var scale := minf(available.x / float(_output_size.x), available.y / float(_output_size.y))
	var frame_size := Vector2(_output_size) * maxf(scale, 0.01)
	_frame_rect = Rect2((size - frame_size) * 0.5, frame_size)

	draw_rect(_frame_rect.grow(7.0), Color(0.02, 0.025, 0.035, 0.85), true)
	if _texture != null:
		draw_texture_rect(_texture, _frame_rect, false)
	else:
		draw_rect(_frame_rect, Color("252936"), true)
		var font := ThemeDB.fallback_font
		var font_size := 16
		var text_size := font.get_multiline_string_size(
			_empty_message,
			HORIZONTAL_ALIGNMENT_CENTER,
			_frame_rect.size.x - 40.0,
			font_size
		)
		var text_position := _frame_rect.position + Vector2(
			20.0,
			(_frame_rect.size.y - text_size.y) * 0.5
		)
		draw_multiline_string(
			font,
			text_position,
			_empty_message,
			HORIZONTAL_ALIGNMENT_CENTER,
			_frame_rect.size.x - 40.0,
			font_size,
			-1,
			Color("aab1c2")
		)

	draw_rect(_frame_rect, Color("e8ba67"), false, 2.0)
	if _show_frame_guide:
		_draw_card_frame_guide()
	var size_text := "%d × %d" % [_output_size.x, _output_size.y]
	draw_string(
		ThemeDB.fallback_font,
		_frame_rect.position + Vector2(4, -10),
		size_text,
		HORIZONTAL_ALIGNMENT_LEFT,
		-1,
		14,
		Color("d9dde8")
	)


func _draw_card_frame_guide() -> void:
	var guide_color := Color(0.47, 0.94, 1.0, 0.95)
	var shadow_color := Color(0.0, 0.0, 0.0, 0.5)
	if _ancient:
		var inset := maxf(4.0, _frame_rect.size.x * 0.025)
		var ancient_rect := _frame_rect.grow(-inset)
		var style := StyleBoxFlat.new()
		style.bg_color = Color.TRANSPARENT
		style.border_color = guide_color
		style.set_border_width_all(3)
		var radius := roundi(_frame_rect.size.x * 0.07)
		style.set_corner_radius_all(radius)
		draw_style_box(style, ancient_rect)
		_draw_guide_label("先古卡可视框", guide_color)
		return

	var normalized_points: Array[Vector2]
	var label := "技能牌可视框"
	# Calibrated against the inner edge of the three official portrait-border
	# textures, mapped from the native 250x190 portrait to this normalized area.
	if _card_type == "attack":
		normalized_points = [
			Vector2(0.022, 0.02),
			Vector2(0.022, 0.758),
			Vector2(0.066, 0.800),
			Vector2(0.164, 0.842),
			Vector2(0.262, 0.884),
			Vector2(0.336, 0.916),
			Vector2(0.386, 0.937),
			Vector2(0.614, 0.937),
			Vector2(0.664, 0.916),
			Vector2(0.738, 0.884),
			Vector2(0.836, 0.842),
			Vector2(0.934, 0.800),
			Vector2(0.978, 0.758),
			Vector2(0.978, 0.02),
		]
		label = "攻击牌可视框"
	elif _card_type == "power":
		normalized_points = [
			Vector2(0.022, 0.02),
			Vector2(0.022, 0.589),
			Vector2(0.024, 0.632),
			Vector2(0.030, 0.674),
			Vector2(0.042, 0.716),
			Vector2(0.056, 0.758),
			Vector2(0.078, 0.800),
			Vector2(0.104, 0.842),
			Vector2(0.140, 0.884),
			Vector2(0.168, 0.905),
			Vector2(0.188, 0.916),
			Vector2(0.210, 0.926),
			Vector2(0.242, 0.937),
			Vector2(0.758, 0.937),
			Vector2(0.790, 0.926),
			Vector2(0.812, 0.916),
			Vector2(0.832, 0.905),
			Vector2(0.860, 0.884),
			Vector2(0.896, 0.842),
			Vector2(0.922, 0.800),
			Vector2(0.944, 0.758),
			Vector2(0.958, 0.716),
			Vector2(0.970, 0.674),
			Vector2(0.976, 0.632),
			Vector2(0.978, 0.589),
			Vector2(0.978, 0.02),
		]
		label = "能力牌可视框"
	else:
		normalized_points = [
			Vector2(0.022, 0.02),
			Vector2(0.022, 0.937),
			Vector2(0.978, 0.937),
			Vector2(0.978, 0.02),
		]

	var points := PackedVector2Array()
	for point in normalized_points:
		points.append(_frame_rect.position + point * _frame_rect.size)
	draw_polyline(points, shadow_color, 8.0, true)
	draw_polyline(points, guide_color, 2.5, true)
	_draw_guide_label(label, guide_color)


func _draw_guide_label(label: String, color: Color) -> void:
	var font := ThemeDB.fallback_font
	var font_size := 13
	var width := font.get_string_size(label, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
	var position := _frame_rect.position + Vector2(_frame_rect.size.x - width - 6.0, 18.0)
	draw_string(font, position + Vector2(1, 1), label, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color(0, 0, 0, 0.8))
	draw_string(font, position, label, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, color)


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event := event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_dragging = mouse_event.pressed and _frame_rect.has_point(mouse_event.position)
			accept_event()
			return
		if mouse_event.pressed and _frame_rect.has_point(mouse_event.position):
			if mouse_event.button_index == MOUSE_BUTTON_WHEEL_UP:
				_zoom = clampf(_zoom * 1.08, 0.05, 4.0)
				transform_changed.emit(_zoom, _offset)
				accept_event()
			elif mouse_event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_zoom = clampf(_zoom / 1.08, 0.05, 4.0)
				transform_changed.emit(_zoom, _offset)
				accept_event()
	elif event is InputEventMouseMotion and _dragging:
		var motion := event as InputEventMouseMotion
		if _frame_rect.size.x > 0.0 and _frame_rect.size.y > 0.0:
			_offset += Vector2(
				motion.relative.x / _frame_rect.size.x,
				motion.relative.y / _frame_rect.size.y
			)
			_offset.x = clampf(_offset.x, -2.0, 2.0)
			_offset.y = clampf(_offset.y, -2.0, 2.0)
			transform_changed.emit(_zoom, _offset)
			accept_event()


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		queue_redraw()
