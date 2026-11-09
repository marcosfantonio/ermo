@tool
extends "res://scripts/world/editor_geometry.gd"
## Batched architecture: a handful of meshes/colliders instead of hundreds of bodies.
@export var courtyard_height := 1.6:
	set(value):
		courtyard_height = value
		request_geometry_rebuild()
@export var half_chunk := 32.0:
	set(value):
		half_chunk = value
		request_geometry_rebuild()
@export var courtyard_half := 23.0:
	set(value):
		courtyard_half = value
		request_geometry_rebuild()
@export var lower_plazas: Array[Dictionary] = []:
	set(value):
		lower_plazas = value
		request_geometry_rebuild()
const G = preload("res://scripts/world/balneario_geometry.gd")
var batches: Dictionary = {}
var materials: Dictionary = {}

func _build_geometry() -> void:
	batches.clear()
	materials.clear()
	var stone := ShaderMaterial.new()
	stone.shader = preload("res://shaders/balneario_stone.gdshader")
	var wall := ShaderMaterial.new()
	wall.shader = preload("res://shaders/balneario_wall.gdshader")
	var rock := ShaderMaterial.new()
	rock.shader = preload("res://shaders/balneario_rock.gdshader")
	materials = {"Stone": stone, "Steps": stone, "Landings": stone, "Wall": wall, "Retaining": rock,
		"Coping": G.material(Color(0.83, 0.74, 0.55)),
		"Rail": G.material(Color(0.83, 0.85, 0.76)),
		"Hedge": G.material(Color(0.22, 0.38, 0.08)),
		"Lamp": G.material(Color(0.29, 0.32, 0.26))}
	for key in materials:
		var st := SurfaceTool.new()
		st.begin(Mesh.PRIMITIVE_TRIANGLES)
		batches[key] = st
	_base_and_retaining()
	_side_terraces()
	_pool_gardens()
	for key in batches:
		G.finish(self, batches[key], materials[key], key, key in ["Stone", "Landings", "Wall", "Retaining", "Coping", "Rail"])

func block(key: String, p: Vector3, size: Vector3) -> void:
	var mesh := BoxMesh.new()
	mesh.size = size
	var st: SurfaceTool = batches[key]
	st.append_from(mesh, 0, Transform3D(Basis.IDENTITY, p))

func _base_and_retaining() -> void:
	var c := courtyard_half
	var s := half_chunk
	var h := courtyard_height
	for side in [-1, 1]:
		_lower_floor(Rect2(Vector2(c if side == 1 else -s, -s), Vector2(s - c, s * 2)))
		var strip := Rect2(Vector2(-c, c if side == 1 else -s), Vector2(c * 2, s - c))
		_lower_floor(strip)
		if h > 0:
			block("Retaining", Vector3(side * c, h * 0.5, 0), Vector3(0.35, h, c * 2))
			# Open gates for the central ramp and the parallel stairway.
			for span in [Vector2(-c, -3.2), Vector2(3.2, 5.2), Vector2(9.8, c)]:
				block("Retaining", Vector3((span.x + span.y) * 0.5, h * 0.5, side * c), Vector3(span.y - span.x, h, 0.35))
		# Raised courtyard parapets, leaving both access gates open.
		for span in [Vector2(-c, -3.2), Vector2(9.8, c)]:
			var mid: float = (span.x + span.y) * 0.5
			block("Wall", Vector3(mid, h + 0.35, side * c), Vector3(span.y - span.x, 0.7, 0.45))
			block("Coping", Vector3(mid, h + 0.73, side * c), Vector3(span.y - span.x + 0.1, 0.12, 0.58))
		if h > 0:
			access(Vector3(0, 0, side * (s - 3.5)), Vector3(0, h + 0.1, side * (c + 0.3)), 5.8, false)
			access(Vector3(7.5, 0, side * (s - 0.4)), Vector3(7.5, h + 0.1, side * (c + 0.3)), 4.0, true)
			landing(Vector3(0, h + 0.1, side * (c - 0.15)), 5.8, 0.9)
			landing(Vector3(7.5, h + 0.1, side * (c - 0.15)), 4, 0.9)
		else:
			# A visible stair/ramp is not needed when this courtyard is at ground level.
			pass

func _side_terraces() -> void:
	var h := courtyard_height
	for side in [-1, 1]:
		var x: float = side * 21.5
		var upper := h + 1.6
		# Keep the foundation below the paving: coincident tops showed wall tiles upstairs.
		block("Retaining", Vector3(x, (upper - 0.16) * 0.5, 0), Vector3(11, upper - 0.16, 28))
		block("Stone", Vector3(x, upper - 0.08, 0), Vector3(11, 0.16, 28))
		for edge in [16.0, 27.0]:
			block("Wall", Vector3(side * edge, upper + 0.23, 0), Vector3(0.35, 0.46, 28))
			block("Coping", Vector3(side * edge, upper + 0.49, 0), Vector3(0.5, 0.1, 28))
			rail(Vector3(side * edge, upper + 0.54, -14), Vector3(side * edge, upper + 0.54, 14))
		for end in [-1, 1]:
			access(Vector3(side * 22.5, h, end * 19.8), Vector3(side * 22.5, upper + 0.1, end * 14.3), 3.5, false)
			access(Vector3(side * 17.1, h, end * 19.8), Vector3(side * 17.1, upper + 0.1, end * 14.3), 2.0, true)
			landing(Vector3(side * 22.5, upper + 0.1, end * 13.85), 3.5, 0.9)
			landing(Vector3(side * 17.1, upper + 0.1, end * 13.85), 2.0, 0.9)
		# Dense hedges frame the seating without occupying access ramps.
		for z in [-10.0, 0.0, 10.0]:
			block("Wall", Vector3(side * 25.4, upper + 0.22, z), Vector3(2.0, 0.44, 7))
			block("Hedge", Vector3(side * 25.4, upper + 0.8, z), Vector3(1.7, 0.95, 6.7))
		for z in [-9.0, 9.0]:
			block("Lamp", Vector3(side * 26, upper + 2.7, z), Vector3(0.10, 5.4, 0.10))
			block("Coping", Vector3(side * 26, upper + 5.4, z), Vector3(0.9, 0.16, 0.4))

func _pool_gardens() -> void:
	var h := courtyard_height
	for side in [-1, 1]:
		for z in [-18.5, 18.5]:
			var x: float = side * 12.5
			block("Wall", Vector3(x, h + 0.35, z), Vector3(5.4, 0.7, 3))
			block("Coping", Vector3(x, h + 0.73, z), Vector3(5.6, 0.12, 3.2))
			block("Hedge", Vector3(x, h + 1.0, z), Vector3(4.9, 0.55, 2.5))

func rail(a: Vector3, b: Vector3) -> void:
	var length := a.distance_to(b)
	var mid := (a + b) * 0.5
	for height in [0.15, 0.85]:
		block("Rail", mid + Vector3.UP * height, Vector3(0.07, 0.07, length))
	var count := ceili(length / 0.9)
	for i in range(count + 1):
		block("Rail", a.lerp(b, float(i) / count) + Vector3.UP * 0.45, Vector3(0.06, 0.9, 0.06))

func access(low: Vector3, high: Vector3, width: float, stairs: bool) -> void:
	if stairs:
		var count := maxi(1, ceili((high.y - low.y) / 0.18))
		low.z = high.z + signf(low.z - high.z) * count * 0.28
	# Fill underneath, including both sides; keep tread geometry separate from collision.
	var core := G.ramp_prism(low + Vector3.DOWN * 0.08, high + Vector3.DOWN * 0.08, width, low.y - 0.2)
	batches.Retaining.append_from(core, 0, Transform3D.IDENTITY)
	# Smooth collision envelope over visible stair treads allows reliable walking.
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var left := Vector3.LEFT * width * 0.5
	if high.z > low.z:
		G.quad(st, low + left, high + left, high - left, low - left)
	else:
		G.quad(st, low - left, high - left, high + left, low + left)
	st.generate_normals()
	var ramp_mesh := st.commit()
	var ramp := G.mesh_node(self, ramp_mesh, materials.Stone, "StairsAccess" if stairs else "RampAccess", true)
	ramp.set_meta("access_footprint", Rect2(Vector2(low.x - width * 0.5 - 0.22, minf(low.z, high.z)), Vector2(width + 0.44, absf(high.z - low.z))))
	if stairs:
		ramp.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		ramp.visible = false
		var count := maxi(1, ceili((high.y - low.y) / 0.18))
		var run := absf(high.z - low.z) / count
		for i in range(count):
			var p := low.lerp(high, float(i + 1) / count)
			p.z = lerpf(low.z, high.z, (i + 0.5) / count)
			var thickness := p.y - low.y + 0.2
			block("Steps", Vector3(p.x, low.y - 0.2 + thickness * 0.5, p.z), Vector3(width, thickness, run + 0.02))
	# Sloping low side walls and handrails are part of the same batched mesh.
	var distance := low.distance_to(high)
	var direction := (high - low).normalized()
	var basis := Basis(Vector3.RIGHT, Vector3(0, absf(direction.z), -signf(direction.z) * direction.y), Vector3(0, direction.y * signf(direction.z), absf(direction.z)))
	for side in [-1, 1]:
		var mesh := BoxMesh.new()
		mesh.size = Vector3(0.16, 0.1, distance)
		var center := (low + high) * 0.5 + Vector3(side * (width * 0.5 + 0.15), 0.95, 0)
		batches.Rail.append_from(mesh, 0, Transform3D(basis, center))
		for i in range(5):
			var p := low.lerp(high, i / 4.0) + Vector3(side * (width * 0.5 + 0.15), 0.5, 0)
			block("Rail", p, Vector3(0.07, 1, 0.07))

func landing(p: Vector3, width: float, length: float) -> void:
	# Taper back to the deck, with no raised lip when approaching from above.
	var north_y := -0.1 if p.z > 0 else 0.0
	var south_y := 0.0 if p.z > 0 else -0.1
	var a := p + Vector3(-width * 0.5, north_y, -length * 0.5)
	var b := p + Vector3(-width * 0.5, south_y, length * 0.5)
	var c := p + Vector3(width * 0.5, south_y, length * 0.5)
	var d := p + Vector3(width * 0.5, north_y, -length * 0.5)
	G.quad(batches.Landings, a, b, c, d)

func _lower_floor(strip: Rect2) -> void:
	# Subtract plaza decks from the base before placing recessed pools, not water overlays.
	var pieces: Array[Rect2] = [strip]
	for plaza in lower_plazas:
		var center: Vector2 = plaza.center - Vector2.ONE * half_chunk
		var deck: Vector2 = plaza.deck
		var cut := Rect2(center - deck, deck * 2)
		var next: Array[Rect2] = []
		for rect in pieces:
			var hit := rect.intersection(cut)
			if not hit.has_area():
				next.append(rect)
				continue
			for part in [
				Rect2(rect.position, Vector2(rect.size.x, hit.position.y - rect.position.y)),
				Rect2(Vector2(rect.position.x, hit.end.y), Vector2(rect.size.x, rect.end.y - hit.end.y)),
				Rect2(Vector2(rect.position.x, hit.position.y), Vector2(hit.position.x - rect.position.x, hit.size.y)),
				Rect2(Vector2(hit.end.x, hit.position.y), Vector2(rect.end.x - hit.end.x, hit.size.y))]:
				if part.has_area():
					next.append(part)
		pieces = next
	for rect in pieces:
		var center := rect.get_center()
		block("Stone", Vector3(center.x, -0.2, center.y), Vector3(rect.size.x, 0.4, rect.size.y))
