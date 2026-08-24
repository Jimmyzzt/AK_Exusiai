class_name CardArtPreview
extends Control

signal transform_changed(zoom: float, offset: Vector2)

var _texture: ImageTexture
var _zoom := 1.0
var _offset := Vector2.ZERO
var _dragging := false
var _frame_rect := Rect2()
var _output_size := Vector2i(250, 190)
var _empty_message := "从左侧选择素材，或将素材类型切换为占位图"


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


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mouse_event := event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT:
			_dragging = mouse_event.pressed and _frame_rect.has_point(mouse_event.position)
			accept_event()
			return
		if mouse_event.pressed and _frame_rect.has_point(mouse_event.position):
			if mouse_event.button_index == MOUSE_BUTTON_WHEEL_UP:
				_zoom = clampf(_zoom * 1.08, 0.2, 4.0)
				transform_changed.emit(_zoom, _offset)
				accept_event()
			elif mouse_event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_zoom = clampf(_zoom / 1.08, 0.2, 4.0)
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
