extends Node3D
signal world_ready
signal rebased(shift: Vector3)
@export var settings: WorldSettings
@export var player: CharacterBody3D
var field: Landscape
var generator: ChunkGenerator
var active: Dictionary = {}
var pending: Array[Vector2i] = []
var center := Vector2i(2147483647, 2147483647)
var loaded := false
var origin_cell := Vector2i.ZERO

func absolute_position(local_position: Vector3) -> Vector3:
	return local_position + Vector3(origin_cell.x * settings.chunk_size, 0, origin_cell.y * settings.chunk_size)

func set_player_world_position(absolute: Vector3) -> void:
	player.position = absolute - Vector3(origin_cell.x * settings.chunk_size, 0, origin_cell.y * settings.chunk_size)

func _ready() -> void:
	field = Landscape.new(settings)
	generator = ChunkGenerator.new(field)
	player.set_physics_process(false)
	player.position = Vector3(field.road_x(24) - 2, field.mesh_height(field.road_x(24) - 2, 24) + 0.3, 24)
	player.rotation.y = -0.08
	_refresh()

func _process(_delta: float) -> void:
	_refresh()
	# Bound the amount of main-thread mesh/physics work per frame.
	var start := Time.get_ticks_msec()
	while not pending.is_empty():
		var cell: Vector2i = pending.pop_front()
		if not active.has(cell):
			var chunk := generator.build(cell, origin_cell)
			add_child(chunk)
			active[cell] = chunk
		if Time.get_ticks_msec() - start >= 7:
			break
	if not loaded and pending.is_empty():
		loaded = true
		player.set_physics_process(true)
		world_ready.emit()

func _refresh() -> void:
	var local_cell := Vector2i(floori(player.position.x / settings.chunk_size), floori(player.position.z / settings.chunk_size))
	var cell := local_cell + origin_cell
	# Keep physics close to zero. Logical coordinates and seeds never change.
	if absf(player.position.x) > 512 or absf(player.position.z) > 512:
		var shift := Vector3(local_cell.x * settings.chunk_size, 0, local_cell.y * settings.chunk_size)
		origin_cell += local_cell
		player.position -= shift
		for chunk: Node3D in active.values():
			chunk.position -= shift
		rebased.emit(shift)
	if cell == center:
		return
	center = cell
	pending.clear()
	var r := settings.view_radius
	for z in range(-r, r + 1):
		for x in range(-r, r + 1):
			var target := center + Vector2i(x, z)
			if not active.has(target):
				pending.append(target)
	pending.sort_custom(func(a: Vector2i, b: Vector2i) -> bool: return a.distance_squared_to(center) < b.distance_squared_to(center))
	for key: Vector2i in active.keys():
		if absi(key.x - center.x) > r or absi(key.y - center.y) > r:
			active[key].queue_free()
			active.erase(key)
