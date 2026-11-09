@tool
extends SceneTree
var failures := 0
var checks := 0

func _initialize() -> void:
	_run.call_deferred()

func check(value: bool, message: String) -> void:
	checks += 1
	if not value:
		failures += 1
		push_error(message)

func meshes(node: Node) -> int:
	var result := int(node is MeshInstance3D)
	for child in node.get_children():
		result += meshes(child)
	return result

func _run() -> void:
	check(Engine.is_editor_hint(), "Use --editor for preview tests")
	# Let the editor finish opening its own tabs before testing an isolated scene.
	await create_timer(1.0).timeout
	var scene: Node3D = load("res://scenes/exploration.tscn").instantiate()
	var player: Node3D = scene.get_node("Player")
	var player_position := player.position
	var mouse_mode := Input.mouse_mode
	root.add_child(scene)
	edited_scene_root = scene
	for i in range(4):
		await process_frame
	edited_scene_root = scene
	var world: Node3D = scene.get_node("World")
	var count := meshes(world)
	check(count > 80, "Authored scene is invisible in editor")
	check(not world.loaded, "Preview ran gameplay")
	check(player.position == player_position, "Preview moved player")
	check(Input.mouse_mode == mouse_mode, "Preview captured mouse")
	for i in range(3):
		world.refresh_preview()
		await process_frame
		await process_frame
		check(meshes(world) == count, "Refreshing duplicated geometry")
	check(world.get_node("Terrain/Hills").owner != null, "Terrain is not inspectable in tree")
	check(world.get_node("Boundary/StoneBoundary").owner != null, "Boundary is not inspectable")
	var entry: String = world.visit.entry
	world.visit_index = 1
	world.refresh_preview()
	await process_frame
	check(world.visit.entry != entry, "Editor preview does not reproduce visit variation")
	var packed := PackedScene.new()
	check(packed.pack(scene) == OK, "Preview cannot be saved")
	var reopened := packed.instantiate()
	root.add_child(reopened)
	edited_scene_root = reopened
	for i in range(4):
		await process_frame
	check(meshes(reopened.get_node("World")) == count, "Save/reopen duplicated geometry")
	check(reopened.get_node("Player").position == player_position, "Reopen moved player")
	edited_scene_root = null
	scene.free()
	reopened.free()
	await create_timer(0.2).timeout
	print("%s: %d checks, %d failures — authored editor preview, variation, ownership, rebuild and save/reopen." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	quit(0 if failures == 0 else 1)
