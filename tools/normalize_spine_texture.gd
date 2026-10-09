extends SceneTree


func _initialize() -> void:
    var args := OS.get_cmdline_user_args()
    if args.size() != 3:
        push_error(
            "Usage: normalize_spine_texture.gd -- <atlas> <source.png> <output.png>"
        )
        quit(1)
        return

    var atlas_size := _read_atlas_size(args[0])
    if atlas_size == Vector2i.ZERO:
        push_error("Could not read atlas page size: " + args[0])
        quit(1)
        return

    var image := Image.load_from_file(args[1])
    if image == null or image.is_empty():
        push_error("Could not load texture: " + args[1])
        quit(1)
        return

    var source_size := image.get_size()
    if source_size != atlas_size:
        image.resize(atlas_size.x, atlas_size.y, Image.INTERPOLATE_LANCZOS)

    var result := image.save_png(args[2])
    print(
        "SPINE_TEXTURE=",
        source_size,
        " -> ",
        atlas_size,
        " output=",
        args[2],
        " result=",
        result
    )
    quit(0 if result == OK else 1)


func _read_atlas_size(path: String) -> Vector2i:
    var file := FileAccess.open(path, FileAccess.READ)
    if file == null:
        return Vector2i.ZERO

    while not file.eof_reached():
        var line := file.get_line().strip_edges()
        if not line.begins_with("size:"):
            continue

        var values := line.trim_prefix("size:").split(",", false)
        if values.size() != 2:
            return Vector2i.ZERO
        return Vector2i(int(values[0].strip_edges()), int(values[1].strip_edges()))

    return Vector2i.ZERO
