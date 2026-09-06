class_name CardEffectPreview
extends Control

signal offset_changed(value: Vector2)
var offset := Vector2.ZERO
var effect_name := "沿用现有表现"
var _dragging := false

func _ready() -> void:
	mouse_default_cursor_shape = Control.CURSOR_MOVE
	custom_minimum_size = Vector2(260, 220)

func _draw() -> void:
	var font := ThemeDB.fallback_font
	var origin := Vector2(size.x * 0.22, size.y * 0.52)
	var target := Vector2(size.x * 0.80, size.y * 0.52)
	var muzzle := origin + offset * 0.15
	draw_style_box(get_theme_stylebox("panel", "Panel"), Rect2(Vector2.ZERO, size))
	draw_line(Vector2(15, size.y * 0.74), Vector2(size.x - 15, size.y * 0.74), Color("525e73"), 1)
	draw_rect(Rect2(origin - Vector2(25, 32), Vector2(50, 65)), Color("35465d"))
	draw_rect(Rect2(target - Vector2(25, 32), Vector2(50, 65)), Color("4e4147"))
	draw_line(muzzle, target, Color("8ebcf5"), 2)
	draw_circle(muzzle, 6, Color("a8d9fc"))
	draw_circle(target, 6, Color("deb3a8"))
	draw_string(font, origin + Vector2(-24, 60), "能天使", HORIZONTAL_ALIGNMENT_LEFT, -1, 14)
	draw_string(font, target + Vector2(-15, 60), "目标", HORIZONTAL_ALIGNMENT_LEFT, -1, 14)
	draw_string(font, Vector2(12, 24), "位置示意 · 拖动蓝点调整发射偏移", HORIZONTAL_ALIGNMENT_LEFT, size.x - 24, 13, Color("c2ccdc"))

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		_dragging = event.pressed
	if event is InputEventMouseMotion and _dragging:
		offset += event.relative / 0.15
		offset = offset.clamp(Vector2(-1500, -1500), Vector2(1500, 1500))
		offset_changed.emit(offset)
		queue_redraw()
