extends Control

# NAncientBgContainer scales and shifts its children for each screen aspect ratio.
# Fit this backdrop in the container's local coordinates instead of dragging its
# TextureRect against the editor's blue bounds.
const OVERSCAN := 0.025

func _ready() -> void:
    set_anchors_preset(Control.PRESET_TOP_LEFT)
    call_deferred("_fit")

func _process(_delta: float) -> void:
    _fit()

func _fit() -> void:
    var container := get_parent() as Control
    if container == null:
        return
    var layout := container.get_parent() as Control
    if layout == null:
        return

    var visible := layout.get_global_rect()
    var to_container := container.get_global_transform().affine_inverse()
    var corners := [
        to_container * visible.position,
        to_container * Vector2(visible.end.x, visible.position.y),
        to_container * Vector2(visible.position.x, visible.end.y),
        to_container * visible.end,
    ]
    var lower: Vector2 = corners[0]
    var upper: Vector2 = corners[0]
    for corner: Vector2 in corners:
        lower = lower.min(corner)
        upper = upper.max(corner)
    var padding := (upper - lower) * OVERSCAN
    var fitted_position := lower - padding
    var fitted_size := upper - lower + padding * 2.0
    if not position.is_equal_approx(fitted_position):
        position = fitted_position
    if not size.is_equal_approx(fitted_size):
        size = fitted_size
