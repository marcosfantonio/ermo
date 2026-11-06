@tool
extends "res://scripts/world/editor_geometry.gd"
## Polygonal pool with continuous square perimeter and a shallow, walkable basin.
@export var pool_radius := Vector2(11, 14):
	set(value):
		pool_radius = value
		request_geometry_rebuild()
@export var half_chunk := 32.0:
	set(value):
		half_chunk = value
		request_geometry_rebuild()
@export_enum("Oval", "Rectangular") var pool_shape: int = 0:
	set(value):
		pool_shape = value
		request_geometry_rebuild()
@export var deck_half_extents := Vector2.ZERO:
	set(value):
		deck_half_extents = value
		request_geometry_rebuild()
@export var parapets_enabled := true:
	set(value):
		parapets_enabled = value
		request_geometry_rebuild()
const G = preload("res://scripts/world/balneario_geometry.gd")

func _build_geometry() -> void:
	var stone := ShaderMaterial.new()
	stone.shader = preload("res://shaders/balneario_stone.gdshader")
	var tile := ShaderMaterial.new()
	tile.shader = preload("res://shaders/pool_tiles.gdshader")
	var water := ShaderMaterial.new()
	water.shader = preload("res://shaders/pool_water.gdshader")
	water.set_shader_parameter("pool_radius", pool_radius)
	water.set_shader_parameter("rectangular", pool_shape == 1)
	var deck := SurfaceTool.new()
	var walls := SurfaceTool.new()
	var surface := SurfaceTool.new()
	var rim := SurfaceTool.new()
	for st in [deck, walls, surface, rim]:
		st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var extents := deck_half_extents if deck_half_extents != Vector2.ZERO else Vector2.ONE * half_chunk
	var angles: Array[float] = []
	for i in range(32):
		angles.append(TAU * i / 32.0)
	# Include every inner/outer rectangle corner so deck seams have no clipped corners.
	for bounds: Vector2 in [extents, pool_radius]:
		for side_x in [-1, 1]:
			for side_z in [-1, 1]:
				var angle := fposmod(atan2(side_z * bounds.y, side_x * bounds.x), TAU)
				if not angles.any(func(value: float) -> bool: return absf(value - angle) < 0.0001):
					angles.append(angle)
	angles.sort()
	for i in range(angles.size()):
		var a := angles[i]
		var b := angles[(i + 1) % angles.size()]
		var da := Vector2(cos(a), sin(a))
		var db := Vector2(cos(b), sin(b))
		var pa := _outline(da)
		var pb := _outline(db)
		var oa := Vector3(da.x, 0, da.y) / maxf(absf(da.x) / extents.x, absf(da.y) / extents.y)
		var ob := Vector3(db.x, 0, db.y) / maxf(absf(db.x) / extents.x, absf(db.y) / extents.y)
		G.quad(deck, pa, pb, ob, oa)
		G.quad(walls, pa, pa + Vector3.DOWN * 0.48, pb + Vector3.DOWN * 0.48, pb)
		G.triangle(surface, Vector3(0, -0.12, 0), pb + Vector3.DOWN * 0.12, pa + Vector3.DOWN * 0.12)
		G.quad(rim, pa + Vector3.UP * 0.015, pb + Vector3.UP * 0.015, pb * 1.045 + Vector3.UP * 0.015, pa * 1.045 + Vector3.UP * 0.015)
	G.finish(self, deck, stone, "Deck", true)
	G.finish(self, walls, tile, "PoolWalls", true)
	G.finish(self, surface, water, "Water")
	G.finish(self, rim, tile, "BlueBorder")
	if parapets_enabled:
		_build_curved_parapets()
	var bottom := ShaderMaterial.new()
	bottom.shader = preload("res://shaders/pool_tiles.gdshader")
	bottom.set_shader_parameter("tile_color", Color(0.13, 0.52, 0.61))
	bottom.set_shader_parameter("underwater", true)
	G.box(self, Vector3(0, -0.58, 0), Vector3(pool_radius.x * 2, 0.2, pool_radius.y * 2), bottom, true)
	# Submerged access ramps: CharacterBody3D does not auto-climb stair risers.
	for side in [-1, 1]:
		var ramp := SurfaceTool.new()
		ramp.begin(Mesh.PRIMITIVE_TRIANGLES)
		var low_z: float = side * (pool_radius.y - minf(2.8, pool_radius.y * 0.65))
		var high_z: float = side * (pool_radius.y - 0.45)
		var end_z: float = side * (pool_radius.y + 0.6)
		var width := minf(1.5, pool_radius.x * 0.45)
		if side == 1:
			G.quad(ramp, Vector3(-width, -0.47, low_z), Vector3(-width, 0.03, high_z), Vector3(width, 0.03, high_z), Vector3(width, -0.47, low_z))
			G.quad(ramp, Vector3(-width, 0.03, high_z), Vector3(-width, 0.03, end_z), Vector3(width, 0.03, end_z), Vector3(width, 0.03, high_z))
		else:
			G.quad(ramp, Vector3(width, -0.47, low_z), Vector3(width, 0.03, high_z), Vector3(-width, 0.03, high_z), Vector3(-width, -0.47, low_z))
			G.quad(ramp, Vector3(width, 0.03, high_z), Vector3(width, 0.03, end_z), Vector3(-width, 0.03, end_z), Vector3(-width, 0.03, high_z))
		G.finish(self, ramp, tile, "PoolAccess", true)

func _outline(direction: Vector2, padding: float = 0.0) -> Vector3:
	var radius := pool_radius + Vector2.ONE * padding
	if pool_shape == 1:
		return Vector3(direction.x, 0, direction.y) / maxf(absf(direction.x) / radius.x, absf(direction.y) / radius.y)
	return Vector3(direction.x * radius.x, 0, direction.y * radius.y)

func _build_curved_parapets() -> void:
	# Low curved walls flank the water, with broad open approaches at both ends.
	var wall := ShaderMaterial.new()
	wall.shader = preload("res://shaders/balneario_wall.gdshader")
	var face := SurfaceTool.new()
	var caps := SurfaceTool.new()
	face.begin(Mesh.PRIMITIVE_TRIANGLES)
	caps.begin(Mesh.PRIMITIVE_TRIANGLES)
	for side in [0.0, PI]:
		for i in range(8):
			var a: float = side - PI * 0.23 + i * PI * 0.46 / 8
			var b: float = side - PI * 0.23 + (i + 1) * PI * 0.46 / 8
			var pa := _outline(Vector2(cos(a), sin(a)), 1.5)
			var pb := _outline(Vector2(cos(b), sin(b)), 1.5)
			var length := pa.distance_to(pb)
			var angle := -atan2((pb - pa).z, (pb - pa).x)
			var mesh := BoxMesh.new()
			mesh.size = Vector3(length + 0.035, 0.65, 0.45)
			face.append_from(mesh, 0, Transform3D(Basis(Vector3.UP, angle), (pa + pb) * 0.5 + Vector3.UP * 0.325))
			mesh = BoxMesh.new()
			mesh.size = Vector3(length + 0.07, 0.12, 0.6)
			caps.append_from(mesh, 0, Transform3D(Basis(Vector3.UP, angle), (pa + pb) * 0.5 + Vector3.UP * 0.71))
	G.finish(self, face, wall, "CurvedParapets", true)
	G.finish(self, caps, G.material(Color(0.84, 0.74, 0.55)), "ParapetCoping", true)
