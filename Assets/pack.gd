extends SceneTree

func _initialize():
	var packer = PCKPacker.new()
	var output = ProjectSettings.globalize_path("res://../dist/Frostsworn/Frostsworn.pck")
	var result = packer.pck_start(output)
	if result != OK:
		push_error("pck_start failed: " + str(result))
		quit(1)
		return
	if not add_tree(packer, "res://Frostsworn") or not add_tree(packer, "res://.godot/imported"):
		quit(1)
		return
	result = packer.flush()
	print("FROSTSWORN_PACK_RESULT=" + str(result))
	quit(0 if result == OK else 1)

func add_tree(packer: PCKPacker, path: String) -> bool:
	var dir = DirAccess.open(path)
	if dir == null:
		push_error("Cannot open pack directory: " + path)
		return false
	for file in dir.get_files():
		var full = path.path_join(file)
		var result = packer.add_file(full, full)
		if result != OK:
			push_error("Cannot pack " + full)
			return false
	for child in dir.get_directories():
		if not add_tree(packer, path.path_join(child)):
			return false
	return true
