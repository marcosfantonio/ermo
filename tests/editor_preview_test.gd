@tool
extends SceneTree
## Run with: godot --headless --editor --path . --script tests/editor_preview_test.gd
var checks := 0
var failures := 0

func _initialize() -> void:
	_run.call_deferred()

func check(value: bool, message: String) -> void:
	checks += 1
	if not value:
		failures += 1
		push_error(message)

func mesh_count(node: Node) -> int:
	var count := 1 if node is MeshInstance3D else 0
	for child in node.get_children():
		count += mesh_count(child)
	return count

func _run() -> void:
	check(Engine.is_editor_hint(), "This test must run with --editor")
	if not Engine.is_editor_hint():
		quit(1)
		return
	for path in ["res://scenes/main.tscn", "res://scenes/balneario_infinito.tscn"]:
		var scene: Node = load(path).instantiate()
		var player: Node3D = scene.get_node("Player")
		var initial_position := player.position
		root.add_child(scene)
		await process_frame
		await process_frame
		var world: Node3D = scene.get_node("World")
		check(world.get_child_count() == 9, "Editor should preview nine chunks: " + path)
		check(mesh_count(world) > 9, "Editor world lacks visible meshes: " + path)
		check(player.position == initial_position, "Preview moved the player")
		check(world.active.is_empty() and world.pending.is_empty(), "Runtime streaming started in editor")
		var count := mesh_count(world)
		world._request_editor_preview()
		await process_frame
		await process_frame
		check(mesh_count(world) == count, "Updating preview duplicated geometry")
		check(world.get_child(0).owner == world, "Chunks are not inspectable in the scene tree")
		var packed_world := PackedScene.new()
		check(packed_world.pack(world) == OK, "Cannot save world preview")
		var reopened := packed_world.instantiate()
		root.add_child(reopened)
		await process_frame
		await process_frame
		check(mesh_count(reopened) == count, "Saved world preview duplicated geometry")
		reopened.free()
		world.editor_preview_enabled = false
		await process_frame
		await process_frame
		check(world.get_child_count() == 0, "Disabling preview did not remove chunks")
		scene.free()
	for path in ["pool", "palm", "slide", "terraces"]:
		var prop: Node3D = load("res://scenes/balneario/%s.tscn" % path).instantiate()
		root.add_child(prop)
		var count := mesh_count(prop)
		check(count > 0, "Standalone prop is invisible: " + path)
		prop.request_geometry_rebuild()
		await process_frame
		await process_frame
		check(mesh_count(prop) == count, "Prop rebuild duplicated meshes: " + path)
		var packed := PackedScene.new()
		check(packed.pack(prop) == OK, "Cannot save generated prop: " + path)
		var restored := packed.instantiate()
		root.add_child(restored)
		check(mesh_count(restored) == count, "Saved/reopened prop duplicated meshes: " + path)
		restored.free()
		if path == "slide":
			prop.style = 1
			await process_frame
			await process_frame
			check(prop.has_node("YellowSpiral") and not prop.has_node("Cascade"), "Inspector style did not update slide")
		if path == "pool":
			prop.pool_radius = Vector2(5, 7)
			await process_frame
			await process_frame
			check(prop.get_node("Water").mesh.get_aabb().size.x < 11, "Inspector radius did not update pool")
		prop.free()
	print("%s: %d checks, %d failures — editor world/prop previews, regeneration and save/reopen." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	quit(0 if failures == 0 else 1)
