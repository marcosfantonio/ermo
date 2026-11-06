@tool
extends "res://scripts/world/chunk_manager.gd"

func _create_generator() -> RefCounted:
	return BalnearioGenerator.new(settings)

func _spawn_player() -> void:
	player.position = Vector3(32, BalnearioLayout.height_for(Vector2i.ZERO) + 0.15, 53)
	player.rotation.y = 0.0

func spawn_radio_near_player() -> bool:
	var tables: Array[Node3D] = []
	for chunk: Node3D in active.values():
		_collect_tables(chunk, tables)
	var available: Array[Node3D] = []
	for table in tables:
		if table.get_node_or_null("RadioProp") == null:
			available.append(table)
	if available.is_empty():
		return false
	available.sort_custom(func(a: Node3D, b: Node3D) -> bool:
		return a.global_position.distance_squared_to(player.global_position) < b.global_position.distance_squared_to(player.global_position))
	if available[0].global_position.distance_to(player.global_position) > 100.0:
		return false
	generator.add_radio(available[0])
	return true

func _collect_tables(node: Node, result: Array[Node3D]) -> void:
	if node is Node3D and node.name == "ParasolTable":
		result.append(node)
	for child in node.get_children():
		_collect_tables(child, result)
