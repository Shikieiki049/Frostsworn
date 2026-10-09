extends Node2D
# Positions are sampled from the unchanged, approved Spine idle. Textures remain intact.
var data: Dictionary
var vertices: PackedFloat32Array
var elapsed := 0.0
var last_frame := -1
var meshes: Array[Polygon2D] = []

func _ready():
	data = JSON.parse_string(FileAccess.get_file_as_string("res://Frostsworn/idle/mesh.json"))
	vertices = FileAccess.get_file_as_bytes("res://Frostsworn/idle/vertices.bin").to_float32_array()
	for index in range(2):
		var polygon := Polygon2D.new()
		var topology: Dictionary = data.meshes[index]
		var uv := PackedVector2Array()
		for i in range(0, topology.uvs.size(), 2):
			uv.append(Vector2(topology.uvs[i], topology.uvs[i+1]))
		polygon.polygon = uv
		polygon.uv = uv
		var triangles: Array[PackedInt32Array] = []
		for i in range(0, topology.triangles.size(), 3):
			triangles.append(PackedInt32Array([topology.triangles[i], topology.triangles[i+1], topology.triangles[i+2]]))
		polygon.polygons = triangles
		polygon.texture = load("res://Frostsworn/idle/full.png" if index == 0 else "res://Frostsworn/idle/full-blink.png")
		polygon.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
		add_child(polygon)
		meshes.append(polygon)
	set_time(0.0)

func _process(delta: float):
	set_time(elapsed + delta)

func set_time(time: float):
	elapsed = fposmod(time, 6.0)
	var frame := int(elapsed * 30.0) % 180
	if frame == last_frame:
		return
	last_frame = frame
	var offset := frame * int(data.stride)
	for polygon in meshes:
		var points := polygon.polygon
		for i in range(points.size()):
			points[i] = Vector2(vertices[offset], vertices[offset+1])
			offset += 2
		polygon.polygon = points
	meshes[1].visible = data.blink[frame]
