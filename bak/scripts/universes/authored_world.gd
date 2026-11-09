@tool
extends Node3D
signal world_ready
@export var profile: UniverseProfile
@export var player: CharacterBody3D
@export_group("Prévia / reprodução")
@export var session_seed := 1998
@export var visit_index := 0
@export var reproduce_visit := false
@export_tool_button("Atualizar prévia 3D", "Reload") var preview = refresh_preview
var visit: Dictionary = {}
var loaded := false
var selected_spawn: Marker3D
const VISITS = preload("res://scripts/universes/universe_manager.gd")

func _ready() -> void:
	if Engine.is_editor_hint():
		refresh_preview.call_deferred()
		return
	player.set_physics_process(false)
	if reproduce_visit:
		UniverseManager.replay(profile.id, visit_index, session_seed)
	visit = UniverseManager.begin_visit(profile)
	apply_visit(visit)
	_finish_loading.call_deferred()

func refresh_preview() -> void:
	if not is_inside_tree() or profile == null:
		return
	$Terrain.request_geometry_rebuild()
	$Boundary.request_geometry_rebuild()
	for route in $Routes.get_children():
		route.request_geometry_rebuild()
	apply_visit(VISITS.configuration(profile, visit_index, session_seed))

func apply_visit(configuration: Dictionary) -> void:
	visit = configuration.duplicate(true)
	selected_spawn = null
	_snap_grounded_props(self)
	$Vegetation.seed_value = int(visit.seed)
	$Vegetation._rebuild_geometry()
	for element in $Variations.get_children():
		element.apply(int(visit.variant))
	var candidates: Array[Marker3D] = []
	for point in $Spawns.get_children():
		point.position.y = $Terrain.mesh_height(point.position.x, point.position.z) + 0.15
		if point.enabled and (point.configurations & (1 << int(visit.variant))) != 0:
			candidates.append(point)
	if candidates.is_empty():
		push_error("Universo sem entrada válida: %s" % profile.id)
		return
	# Rotating through a seeded order avoids the same entry on consecutive visits.
	var offset := int(hash("%s:%d" % [profile.id, int(visit.session_seed)])) % candidates.size()
	selected_spawn = candidates[posmod(offset + int(visit.visit), candidates.size())]
	if profile.spawn_rule == 1:
		var rng := RandomNumberGenerator.new()
		rng.seed = int(visit.seed) ^ 0x53504157
		selected_spawn = candidates[rng.randi_range(0, candidates.size() - 1)]
	visit["entry"] = String(selected_spawn.entry_id)

func _finish_loading() -> void:
	if selected_spawn == null:
		return
	# Let physics register the authored scene's shapes before releasing the player.
	await get_tree().physics_frame
	player.global_transform = selected_spawn.global_transform
	player.velocity = Vector3.ZERO
	player.last_grounded = true
	player.set_physics_process(true)
	loaded = true
	world_ready.emit()

func absolute_position(local_position: Vector3) -> Vector3:
	return to_global(local_position)

func _snap_grounded_props(node: Node) -> void:
	if node is Node3D and node.has_meta("grounded_prop"):
		var p := to_local(node.global_position)
		p.y = $Terrain.mesh_height(p.x, p.z)
		node.global_position = to_global(p)
	for child in node.get_children():
		_snap_grounded_props(child)

func surface_at(point: Vector3) -> String:
	for route in $Routes.get_children():
		if route.distance_to_path(Vector2(point.x, point.z)) < route.width * 0.5:
			return "dirt"
	return "grass"
