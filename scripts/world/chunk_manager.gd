@tool
extends Node3D
signal world_ready
signal rebased(shift: Vector3)
@export var settings: WorldSettings
@export var player: CharacterBody3D
@export_group("Prévia no editor")
@export var editor_preview_enabled := true:
	set(value):
		editor_preview_enabled = value
		_request_editor_preview()
@export_range(0, 2, 1) var editor_preview_radius := 1:
	set(value):
		editor_preview_radius = value
		_request_editor_preview()
@export var editor_preview_center := Vector2i.ZERO:
	set(value):
		editor_preview_center = value
		_request_editor_preview()
@export_tool_button("Atualizar prévia 3D", "Reload") var update_editor_preview = _request_editor_preview
var _editor_preview_pending := false
const EDITOR_GEOMETRY = preload("res://scripts/world/editor_geometry.gd")
var field: Landscape
var generator: RefCounted
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
	if Engine.is_editor_hint():
		_request_editor_preview()
		return
	_clear_editor_preview()
	field = Landscape.new(settings)
	generator = _create_generator()
	player.set_physics_process(false)
	_spawn_player()
	_refresh()

func _create_generator() -> RefCounted:
	return ChunkGenerator.new(field)

func _spawn_player() -> void:
	player.position = Vector3(field.road_x(24) - 2, field.mesh_height(field.road_x(24) - 2, 24) + 0.3, 24)
	player.rotation.y = -0.08

func _process(_delta: float) -> void:
	if Engine.is_editor_hint():
		return
	_refresh()
	# Bound the amount of main-thread mesh/physics work per frame.
	var start := Time.get_ticks_msec()
	while not pending.is_empty():
		var cell: Vector2i = pending.pop_front()
		if not active.has(cell):
			var chunk: Node3D = generator.build(cell, origin_cell)
			add_child(chunk)
			active[cell] = chunk
		if Time.get_ticks_msec() - start >= 7:
			break
	if not loaded and pending.is_empty():
		loaded = true
		player.set_physics_process(true)
		world_ready.emit()

func _request_editor_preview() -> void:
	if not Engine.is_editor_hint() or not is_inside_tree() or _editor_preview_pending:
		return
	_editor_preview_pending = true
	_rebuild_editor_preview.call_deferred()

func _clear_editor_preview() -> void:
	for child in get_children():
		if child.has_meta("editor_world_preview"):
			remove_child(child)
			child.free()

func _rebuild_editor_preview() -> void:
	_editor_preview_pending = false
	_clear_editor_preview()
	if not editor_preview_enabled or settings == null:
		return
	field = Landscape.new(settings)
	generator = _create_generator()
	# Deliberately smaller than runtime streaming; never move/capture the player.
	for z in range(-editor_preview_radius, editor_preview_radius + 1):
		for x in range(-editor_preview_radius, editor_preview_radius + 1):
			var chunk: Node3D = generator.build(editor_preview_center + Vector2i(x, z))
			chunk.set_meta("editor_world_preview", true)
			add_child(chunk)
			EDITOR_GEOMETRY.own_geometry(chunk, EDITOR_GEOMETRY.preview_owner(self))

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
