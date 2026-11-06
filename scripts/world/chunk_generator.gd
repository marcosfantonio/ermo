class_name ChunkGenerator
extends RefCounted

const GRASS = preload("res://materials/grass.tres")
const HOUSE = preload("res://scenes/props/house.tscn")
const TREE = preload("res://scenes/props/tree.tscn")
const FENCE = preload("res://scenes/props/fence.tscn")
const POLE = preload("res://scenes/props/pole.tscn")
const TANK = preload("res://scenes/props/water_tank.tscn")
const ECHO = preload("res://scenes/anomalies/house_echo.tscn")
var field: Landscape

func _init(landscape: Landscape) -> void:
	field = landscape

func build(cell: Vector2i, origin_cell: Vector2i = Vector2i.ZERO) -> Node3D:
	var root := Node3D.new()
	root.name = "Chunk_%d_%d" % [cell.x, cell.y]
	var size := field.settings.chunk_size
	var origin := Vector2(cell) * size
	var local_origin := Vector2(cell - origin_cell) * size
	root.position = Vector3(local_origin.x, 0, local_origin.y)
	var sites: Array[Dictionary] = []
	for z in range(floori((origin.y - 32) / 96), floori((origin.y + size + 32) / 96) + 1):
		for x in range(floori((origin.x - 32) / 128), floori((origin.x + size + 32) / 128) + 1):
			sites.append_array(field.sites_in_cell(Vector2i(x, z)))
	_build_ground(root, origin, sites)
	_build_road(root, origin, sites)
	for site in sites:
		var p: Vector2 = site.position
		if _owns(p, origin, size):
			var house := _place(HOUSE, root, p, origin)
			house.rotation.y = site.rotation
			house.position.y += 0.05
			if site.echo:
				var echo := ECHO.instantiate()
				house.add_child(echo)
			else:
				house.get_node("Number").text = str(15 + int(site.variant) * 2)
			if int(site.variant) == 1:
				_place(TANK, root, p + Vector2(9, -5), origin)
			if int(site.variant) == 2:
				for i in range(3):
					_place(FENCE, root, p + Vector2(8 + i * 4, 9), origin)
	# At most one isolated tree per chunk; most chunks have none.
	var rng := RandomNumberGenerator.new()
	rng.seed = field.coordinate_seed(cell, 71)
	if rng.randf() < 0.13:
		var p := origin + Vector2(rng.randf_range(8, size - 8), rng.randf_range(8, size - 8))
		var clear := field.road_distance(p.x, p.y) > 12.0
		for site in sites:
			if p.distance_to(site.position) < 18.0:
				clear = false
		if clear:
			_place(TREE, root, p, origin)
	var pole_z := float(cell.y * size + 20)
	var pole_p := Vector2(field.road_x(pole_z) + 4, pole_z)
	if posmod(cell.y, 3) == 1 and _owns(pole_p, origin, size):
		_place(POLE, root, pole_p, origin)
	return root

func _owns(p: Vector2, origin: Vector2, size: int) -> bool:
	return p.x >= origin.x and p.x < origin.x + size and p.y >= origin.y and p.y < origin.y + size

func _place(scene: PackedScene, root: Node3D, p: Vector2, origin: Vector2) -> Node3D:
	var prop := scene.instantiate() as Node3D
	prop.set_meta("world_position", p)
	prop.position = Vector3(p.x - origin.x, field.surface_height(p.x, p.y), p.y - origin.y)
	root.add_child(prop)
	return prop

func _build_ground(root: Node3D, origin: Vector2, sites: Array[Dictionary]) -> void:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var count := field.settings.subdivisions
	var step := float(field.settings.chunk_size) / count
	for z in range(count):
		for x in range(count):
			for offset: Vector2 in [Vector2(0,0), Vector2(1,0), Vector2(0,1), Vector2(1,0), Vector2(1,1), Vector2(0,1)]:
				var local := (Vector2(x, z) + offset) * step
				var p := origin + local
				var h := field.height_at(p.x, p.y, sites)
				var patch := sin(p.x * 0.18 + sin(p.y * 0.13)) * sin(p.y * 0.2) * 0.025
				var tint := Color(0.43 + patch, 0.54 + patch, 0.17 + patch * 0.5)
				st.set_color(tint)
				st.set_uv(p * 0.24)
				st.add_vertex(Vector3(local.x, h, local.y))
	st.generate_normals()
	var mesh := st.commit()
	var terrain := MeshInstance3D.new()
	terrain.name = "Terrain"
	terrain.mesh = mesh
	terrain.material_override = GRASS
	root.add_child(terrain)
	var body := StaticBody3D.new()
	body.set_meta("surface", "grass")
	var collision := CollisionShape3D.new()
	collision.shape = mesh.create_trimesh_shape()
	body.add_child(collision)
	root.add_child(body)

func _build_road(root: Node3D, origin: Vector2, sites: Array[Dictionary]) -> void:
	# The path is independent of terrain tessellation, so it never disappears
	# when narrower than one terrain quad. Identical boundary samples.
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var size := field.settings.chunk_size
	var added := false
	for iz in range(0, size, 2):
		var z := origin.y + iz
		var x := field.road_x(z)
		if x < origin.x - 3 or x > origin.x + size + 3:
			continue
		added = true
		for offset: Vector2 in [Vector2(-1,0), Vector2(1,0), Vector2(-1,2), Vector2(1,0), Vector2(1,2), Vector2(-1,2)]:
			var wz := z + offset.y
			var wx := field.road_x(wz) + offset.x * (0.95 + 0.12 * sin(wz * 0.3))
			# Clip at x borders, avoiding double-drawn road fragments.
			wx = clampf(wx, origin.x, origin.x + size)
			st.set_color(Color(0.64, 0.56, 0.37))
			st.set_uv(Vector2(wx, wz) * 0.4)
			st.add_vertex(Vector3(wx - origin.x, field.mesh_height(wx, wz, sites) + 0.06, wz - origin.y))
	if not added:
		return
	st.generate_normals()
	var road := MeshInstance3D.new()
	road.name = "DirtPath"
	road.mesh = st.commit()
	road.material_override = GRASS
	root.add_child(road)
