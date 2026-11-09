extends SceneTree
var checks := 0
var failures := 0

func _initialize() -> void:
	_run.call_deferred()

func check(value: bool, message: String) -> void:
	checks += 1
	if not value:
		failures += 1
		push_error(message)

func _run() -> void:
	var profile := load("res://resources/sitios.tres") as WorldSettings
	var field := Landscape.new(profile)
	var other := Landscape.new(profile)
	for z in range(-6, 7):
		for x in range(-6, 7):
			var cell := Vector2i(x, z)
			check(field.sites_in_cell(cell) == other.sites_in_cell(cell), "Site determinism %s" % cell)
			check(field.surface_height(x * 19.7, z * 27.1) == other.surface_height(x * 19.7, z * 27.1), "Height determinism")
	var generator := ChunkGenerator.new(field)
	var a := generator.build(Vector2i.ZERO)
	var b := generator.build(Vector2i(1, 0))
	var av: PackedVector3Array = a.get_node("Terrain").mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	var bv: PackedVector3Array = b.get_node("Terrain").mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	for v in av:
		if is_equal_approx(v.x, 64):
			var matched := false
			for w in bv:
				if is_zero_approx(w.x) and is_equal_approx(v.z, w.z):
					check(is_equal_approx(v.y, w.y), "Crack at chunk edge")
					matched = true
					break
			check(matched, "Missing edge sample")
	var repeat := generator.build(Vector2i.ZERO)
	check(av == repeat.get_node("Terrain").mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX], "Regeneration differs")
	check(a.get_child_count() == repeat.get_child_count(), "Regenerated prop count differs")
	a.free()
	b.free()
	repeat.free()
	var scene := load("res://scenes/main.tscn").instantiate() as Node3D
	root.add_child(scene)
	var world: Node3D = scene.get_node("World")
	await world.world_ready
	var player: CharacterBody3D = scene.get_node("Player")
	for i in range(90):
		await physics_frame
	check(player.is_on_floor(), "Spawn has no ground collision")
	check(player.position.y > -20, "Player fell through terrain")
	player.set_physics_process(false)
	var home: Vector3 = world.absolute_position(player.position)
	var original_vertices: PackedVector3Array = world.active[Vector2i.ZERO].get_node("Terrain").mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	player.position += Vector3(1024, 0, -1024)
	await process_frame
	await process_frame
	while not world.pending.is_empty():
		await process_frame
	check(not world.active.has(Vector2i.ZERO), "Old chunks not unloaded")
	check(world.active.size() <= 121, "Chunk memory bound exceeded")
	check(absf(player.position.x) < 512 and absf(player.position.z) < 512, "Floating origin did not rebase")
	world.set_player_world_position(home)
	await process_frame
	await process_frame
	while not world.pending.is_empty():
		await process_frame
	check(original_vertices == world.active[Vector2i.ZERO].get_node("Terrain").mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX], "Return changed terrain")
	player.set_physics_process(true)
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	Input.action_press("forward")
	for i in range(240):
		await physics_frame
	Input.action_release("forward")
	check(world.absolute_position(player.position).distance_to(home) > 10, "Walking input did not move player")
	check(player.position.y > -20, "Walking fell through ground")
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	scene._unhandled_input(escape)
	check(paused, "Pause did not pause the tree")
	scene._unhandled_input(escape)
	check(not paused, "Escape did not resume")
	var nearby_house: Node3D
	for chunk: Node3D in world.active.values():
		for prop in chunk.get_children():
			if prop.scene_file_path == "res://scenes/props/house.tscn" and prop.get_node("Number").text == "13":
				if nearby_house == null or prop.global_position.distance_to(player.global_position) < nearby_house.global_position.distance_to(player.global_position):
					nearby_house = prop
	check(nearby_house != null, "Missing echo house near spawn")
	if nearby_house != null:
		player.position = nearby_house.to_global(Vector3(-1.35, 0.3, 6))
		player.rotation.y = nearby_house.rotation.y
		player.velocity = Vector3.ZERO
		Input.action_press("forward")
		for i in range(120):
			await physics_frame
		Input.action_release("forward")
		var inside: Vector3 = nearby_house.to_local(player.global_position)
		check(inside.z < 1.5 and inside.z > -3, "Cannot enter empty house through open doorway: %s" % inside)
		check(player.is_on_floor(), "House floor has no stable collision")
	print("%s: %d checks, %d failures — determinism, borders, collision, streaming, floating origin, walking, pause and house entry." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	scene.get_node("Soundscape/Wind").stop()
	scene.get_node("Soundscape/Distant").stop()
	scene.get_node("Player/Footsteps").stop()
	scene.queue_free()
	await create_timer(0.2).timeout
	quit(0 if failures == 0 else 1)
