@tool
class_name BalnearioGenerator
extends RefCounted
const POOL = preload("res://scenes/balneario/pool.tscn")
const PARASOL = preload("res://scenes/balneario/parasol.tscn")
const PALM = preload("res://scenes/balneario/palm.tscn")
const SLIDE = preload("res://scenes/balneario/slide.tscn")
const MUSHROOM = preload("res://scenes/balneario/mushroom.tscn")
const TREE = preload("res://scenes/props/tree.tscn")
const TERRACES = preload("res://scenes/balneario/terraces.tscn")
const RADIO = preload("res://scenes/balneario/radio.tscn")
const G = preload("res://scripts/world/balneario_geometry.gd")
var settings: WorldSettings

func _init(profile: WorldSettings) -> void:
	settings = profile

func coordinate_seed(cell: Vector2i) -> int:
	return hash("balneario:%d:%d:%d" % [settings.world_seed, cell.x, cell.y])

func build(cell: Vector2i, origin_cell: Vector2i = Vector2i.ZERO) -> Node3D:
	var root := Node3D.new()
	root.name = "Balneario_%d_%d" % [cell.x, cell.y]
	var size := float(settings.chunk_size)
	root.position = Vector3((cell.x - origin_cell.x) * size, 0, (cell.y - origin_cell.y) * size)
	var rng := RandomNumberGenerator.new()
	rng.seed = coordinate_seed(cell)
	var pool_plan := BalnearioLayout.main_pool(cell, rng)
	var radius: Vector2 = pool_plan.radius
	var lower_plazas := BalnearioLayout.lower_pools(cell, size)
	var height := BalnearioLayout.height_for(cell)
	var terraces := _instantiate(TERRACES)
	terraces.courtyard_height = height
	terraces.half_chunk = size * 0.5
	terraces.courtyard_half = BalnearioLayout.courtyard_half(size)
	terraces.lower_plazas = lower_plazas
	terraces.position = Vector3(size * 0.5, 0, size * 0.5)
	root.add_child(terraces)
	var pool := _instantiate(POOL)
	pool.pool_radius = radius
	pool.pool_shape = pool_plan.shape
	pool.half_chunk = BalnearioLayout.courtyard_half(size)
	pool.position = Vector3(size * 0.5, height, size * 0.5)
	root.add_child(pool)
	root.set_meta("layout", [radius, rng.seed, height, pool_plan.variant, lower_plazas])
	_build_lower_plazas(root, lower_plazas)
	var blue := G.material(Color(0.08, 0.26, 0.5))
	var cement := G.material(Color(0.65, 0.62, 0.47))
	var radio_tables: Array[Node3D] = []
	# A common zero-height circulation ring joins courtyards at different levels.
	for side in [-1, 1]:
		for row in range(3):
			var p := Vector3(size * 0.5 + side * 21, height + 1.6, 22 + row * 10)
			var table := _instantiate(PARASOL)
			table.position = p
			table.rotation.y = rng.randf_range(-0.15, 0.15)
			radio_tables.append(table)
			var canopy := table.get_node("Canopy") as MeshInstance3D
			var mat := G.material([Color(0.68, 0.12, 0.12), Color(0.85, 0.64, 0.12), Color(0.15, 0.40, 0.24)][rng.randi_range(0, 2)])
			mat.cull_mode = BaseMaterial3D.CULL_DISABLED
			canopy.material_override = mat
			root.add_child(table)
		for z in [13.5, size - 13.5]:
			var p := Vector3(size * 0.5 + side * 12.5, height, z)
			var palm := _instantiate(PALM)
			palm.position = p + Vector3.UP * 0.75
			palm.rotation.y = rng.randf() * TAU
			root.add_child(palm)
		# Shaded planted strips, but leave the outer chunk seam clear for walking.
		for z in [21.0, 28.0, 36.0, 43.0]:
			var tree := _instantiate(TREE)
			tree.position = Vector3(size * 0.5 + side * 25.4, height + 1.6, z)
			tree.scale = Vector3.ONE * rng.randf_range(1.0, 1.4)
			root.add_child(tree)
	# Alternate recognizable attractions rather than scattering them arbitrarily.
	var slide := _instantiate(SLIDE)
	slide.style = posmod(cell.x + cell.y, 2)
	slide.position = Vector3(size * 0.5, height, size * 0.5 - radius.y - 4)
	if slide.style == 1:
		slide.position.x -= radius.x - 1
		slide.position.z += 5
	root.add_child(slide)
	if slide.style == 1:
		var mushroom := _instantiate(MUSHROOM)
		mushroom.position = Vector3(size * 0.5 + 4, height, size * 0.5)
		root.add_child(mushroom)
	G.box(root, Vector3(size * 0.5 + radius.x + 1.7, height + 1.05, size * 0.5 + 3), Vector3(0.08, 2.1, 0.08), cement)
	G.box(root, Vector3(size * 0.5 + radius.x + 1.7, height + 1.9, size * 0.5 + 3), Vector3(1.5, 1.2, 0.08), blue)
	var sign := Label3D.new()
	sign.name = "RepeatedSign"
	sign.position = Vector3(size * 0.5 + radius.x + 1.7, height + 1.95, size * 0.5 + 3.06)
	sign.font_size = 40
	sign.pixel_size = 0.006
	# Quiet anomaly: all pools claim the same number, independent of coordinates.
	sign.text = "PISCINA 01\nPROF. 0,48 m\nABERTO ATÉ 17h"
	sign.no_depth_test = false
	root.add_child(sign)
	if BalnearioLayout.radio_roll(cell, settings.world_seed, size):
		if not radio_tables.is_empty():
			add_radio(radio_tables[rng.randi_range(0, radio_tables.size() - 1)])
	return root

func _build_lower_plazas(root: Node3D, plazas: Array[Dictionary]) -> void:
	var plaster := G.material(Color(0.79, 0.76, 0.61))
	var leaves := G.material(Color(0.20, 0.35, 0.09))
	for i in range(plazas.size()):
		var plan := plazas[i]
		var center: Vector2 = plan.center
		var pool := _instantiate(POOL)
		pool.name = "LowerPool%d" % i
		pool.pool_radius = plan.radius
		pool.pool_shape = plan.shape
		pool.deck_half_extents = plan.deck
		pool.parapets_enabled = false
		pool.position = Vector3(center.x, 0, center.y)
		# Keep the shared water loop only on main pools, not every small annex.
		pool.get_node("WaterSound").free()
		root.add_child(pool)
		if bool(plan.get("narrow", false)):
			for end in [-1, 1]:
				G.box(root, Vector3(center.x, 0.25, center.y + end * 6), Vector3(1.6, 0.5, 0.5), plaster, true)
			continue
		# Side seating and planted corners leave the seam and access lanes open.
		var bench_x := center.x - 5.5
		G.box(root, Vector3(bench_x, 0.25, center.y), Vector3(0.55, 0.5, 2.2), plaster, true)
		G.box(root, Vector3(bench_x - 0.2, 0.75, center.y), Vector3(0.12, 0.65, 2.2), plaster)
		var table := _instantiate(PARASOL)
		table.position = Vector3(center.x + 5.1, 0, center.y)
		table.scale = Vector3.ONE * 0.75
		table.set_meta("radio_table", true)
		root.add_child(table)
		for end in [-1, 1]:
			var p := Vector3(bench_x, 0.2, center.y + end * 2.9)
			G.box(root, p, Vector3(0.8, 0.4, 0.8), plaster, true)
			G.box(root, p + Vector3.UP * 0.45, Vector3(0.7, 0.6, 0.7), leaves)

func add_radio(table: Node3D) -> Node3D:
	var radio := _instantiate(RADIO) as Node3D
	radio.name = "RadioProp"
	table.add_child(radio)
	return radio

func _instantiate(scene: PackedScene) -> Node:
	return scene.instantiate(PackedScene.GEN_EDIT_STATE_INSTANCE if Engine.is_editor_hint() else PackedScene.GEN_EDIT_STATE_DISABLED)
