extends MeshInstance3D
## A cheap, collision-free continuation beneath the streaming terrain.
## Only the distance is coarse; it uses the same continuous height field.
@export var world: Node3D
@export var player: Node3D
var last_cell := Vector2i(999999, 999999)
var last_origin := Vector2i(999999, 999999)

func _process(_delta: float) -> void:
	if world.field == null:
		return
	var absolute: Vector3 = world.absolute_position(player.position)
	var cell := Vector2i(floori(absolute.x / 128), floori(absolute.z / 128))
	if cell == last_cell and last_origin == world.origin_cell:
		return
	last_cell = cell
	last_origin = world.origin_cell
	var origin := Vector2(cell) * 128
	var render_origin: Vector2 = Vector2(world.origin_cell) * world.settings.chunk_size
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for z in range(-32, 33):
		for x in range(-32, 33):
			if x >= -3 and x < 3 and z >= -3 and z < 3:
				continue
			for offset: Vector2 in [Vector2(0,0), Vector2(1,0), Vector2(0,1), Vector2(1,0), Vector2(1,1), Vector2(0,1)]:
				var p := origin + (Vector2(x, z) + offset) * 64
				st.set_color(Color(0.43, 0.54, 0.17))
				st.set_uv(p * 0.24)
				st.add_vertex(Vector3(p.x - render_origin.x, world.field.base_height(p.x, p.y) - 1.5, p.y - render_origin.y))
	st.generate_normals()
	mesh = st.commit()
