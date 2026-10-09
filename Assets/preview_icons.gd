extends SceneTree

func _initialize():
	var files = DirAccess.get_files_at("res://Frostsworn/icons")
	var sheet = Image.create_empty(896, 640, false, Image.FORMAT_RGBA8)
	sheet.fill(Color("0a1520"))
	var i = 0
	for file in files:
		if not file.ends_with(".svg"):
			continue
		var tile = Image.load_from_file("res://Frostsworn/icons/" + file)
		sheet.blit_rect(tile, Rect2i(0, 0, 128, 128), Vector2i((i % 7) * 128, (i / 7) * 128))
		i += 1
	assert(i == 29)
	sheet.save_png(ProjectSettings.globalize_path("res://../../outputs/霜誓者_0.5.0_状态图标预览.png"))
	quit()
