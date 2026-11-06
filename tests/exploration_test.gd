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

func settle(frames: int = 40) -> void:
	for i in range(frames):
		await physics_frame

func _run() -> void:
	var manager := root.get_node("UniverseManager")
	manager.persistence_enabled = false
	manager.progress.clear()
	var profile: UniverseProfile = load("res://resources/sitios_universe.tres")
	var entries := {}
	var variants := {}
	for i in range(64):
		var a: Dictionary = manager.configuration(profile, i, 1998)
		check(a == manager.configuration(profile, i, 1998), "Visit is not deterministic")
		variants[a.variant] = true
	check(variants.size() == 2, "Both spatial configurations must be selectable")
	manager.replay(&"sitios", 0, 1998)
	var scene: Node3D = load("res://scenes/exploration.tscn").instantiate()
	root.add_child(scene)
	var world: Node3D = scene.get_node("World")
	await world.world_ready
	var player: CharacterBody3D = scene.get_node("Player")
	var terrain: Node3D = world.get_node("Terrain")
	var fixed_house: Transform3D = world.get_node("Landmarks/Casa13").transform
	var fixed_echo: Transform3D = world.get_node("Landmarks/CasaRepetida").transform
	var first_entry := ""
	var last_entry := ""
	for i in range(8):
		world.apply_visit(manager.configuration(profile, i, 1998))
		var entry: String = world.visit.entry
		entries[entry] = true
		if i == 0:
			first_entry = entry
		check(entry != last_entry, "Consecutive visits repeated an entrance")
		last_entry = entry
		player.global_transform = world.selected_spawn.global_transform
		player.velocity = Vector3.ZERO
		await settle()
		check(player.is_on_floor(), "Entrance has no stable ground: " + entry)
		check(absf(player.position.y - terrain.mesh_height(player.position.x, player.position.z)) < 0.2, "Spawn height drift: " + entry)
		var query := PhysicsShapeQueryParameters3D.new()
		query.shape = player.get_node("Collision").shape
		query.transform = player.get_node("Collision").global_transform
		query.transform.origin += Vector3.UP * 0.1
		query.exclude = [player.get_rid()]
		query.margin = 0.0
		check(player.get_world_3d().direct_space_state.intersect_shape(query).is_empty(), "Spawn overlaps solid geometry: " + entry)
		check(world.get_node("Landmarks/Casa13").transform == fixed_house, "Primary landmark moved")
		check(world.get_node("Landmarks/CasaRepetida").transform == fixed_echo, "Echo landmark moved")
	check(entries.size() == 4, "Not all four entrances were selected")
	world.apply_visit(manager.configuration(profile, 0, 1998))
	check(String(world.visit.entry) == first_entry, "Replaying did not reproduce entrance")
	var chair: Vector3 = world.get_node("Landmarks/CasaRepetida/Chair").position
	world.apply_visit(manager.configuration(profile, 0, 1998))
	check(chair == world.get_node("Landmarks/CasaRepetida/Chair").position, "Chair replay differs")
	profile.spawn_rule = 1
	for i in range(4):
		var config: Dictionary = manager.configuration(profile, i, 1998)
		world.apply_visit(config)
		var entry: String = world.visit.entry
		world.apply_visit(config)
		check(world.visit.entry == entry, "Seeded spawn draw cannot be replayed")
	profile.spawn_rule = 0
	profile.configuration_count = 1
	check(int(manager.configuration(profile, 0, 1998).variant) == 0, "Universe configuration count ignored")
	profile.configuration_count = 2
	for variant in range(2):
		var config: Dictionary = manager.configuration(profile, 0, 1998)
		config.variant = variant
		world.apply_visit(config)
		var gate: StaticBody3D = world.get_node("Variations/DesvioCercado/PorteiraDesvio")
		check((gate.collision_layer != 0) == (variant == 1), "Invisible variant retained collision")
		check(gate.is_visible_in_tree() == (variant == 1), "Variant visibility differs")
	# A traversal test walks the full outer loop, including both house approaches.
	player.position = Vector3(4, terrain.mesh_height(4, 64) + 0.1, 64)
	player.velocity = Vector3.ZERO
	await settle()
	var waypoints: Array[Vector2] = []
	var porch_points: PackedVector2Array = world.get_node("Routes/PorchRoad").points
	for i in range(2, porch_points.size()):
		waypoints.append(porch_points[i])
	var return_points: PackedVector2Array = world.get_node("Routes/RidgeReturn").points
	for i in range(1, return_points.size()):
		waypoints.append(return_points[i])
	for target in waypoints:
		var arrived := false
		Input.action_press("forward")
		for frame in range(600):
			var offset := target - Vector2(player.position.x, player.position.z)
			if offset.length() < 1.3:
				arrived = true
				break
			player.rotation.y = atan2(-offset.x, -offset.y)
			await physics_frame
		Input.action_release("forward")
		check(arrived, "Blocked route at %s (player %s)" % [target, player.position])
		check(player.position.y > -2.0, "Fell below terrain")
	# Enter the open doorway with normal movement, not a teleport into the interior.
	var house: Node3D = world.get_node("Landmarks/Casa13")
	player.position = house.to_global(Vector3(-1.35, 0.3, 6))
	player.rotation.y = house.rotation.y
	player.velocity = Vector3.ZERO
	await settle()
	Input.action_press("forward")
	await settle(120)
	Input.action_release("forward")
	var inside := house.to_local(player.global_position)
	check(inside.z < 1.5 and inside.z > -3, "House doorway is not navigable: %s" % inside)
	check(player.is_on_floor(), "Porch/interior collision missing")
	# Every boundary section has a visible wall and an actual physics hit.
	player.set_physics_process(false)
	var boundary: Node3D = world.get_node("Boundary")
	for i in range(boundary.points.size() - 1):
		var midpoint: Vector2 = (boundary.points[i] + boundary.points[i + 1]) * 0.5
		var direction: Vector2 = (boundary.points[i + 1] - boundary.points[i]).normalized()
		var normal := Vector2(-direction.y, direction.x) * 2
		var h: float = terrain.mesh_height(midpoint.x, midpoint.y) + 1.2
		var query := PhysicsRayQueryParameters3D.create(Vector3(midpoint.x + normal.x, h, midpoint.y + normal.y), Vector3(midpoint.x - normal.x, h, midpoint.y - normal.y))
		var hit := player.get_world_3d().direct_space_state.intersect_ray(query)
		check(not hit.is_empty(), "Boundary has no collision at segment %d" % i)
	# Sprint and jump at four different limits; the visible wall, not a clamp, stops us.
	player.set_physics_process(true)
	for i in [0, 3, 6, 9]:
		var midpoint: Vector2 = (boundary.points[i] + boundary.points[i + 1]) * 0.5
		var tangent: Vector2 = (boundary.points[i + 1] - boundary.points[i]).normalized()
		var inward := Vector2(-tangent.y, tangent.x)
		var start := midpoint + inward * 3.0
		player.position = Vector3(start.x, terrain.mesh_height(start.x, start.y) + 0.1, start.y)
		player.velocity = Vector3.ZERO
		player.rotation.y = atan2(inward.x, inward.y)
		await settle()
		Input.action_press("forward")
		Input.action_press("run")
		Input.action_press("jump")
		await settle(180)
		Input.action_release("forward")
		Input.action_release("run")
		Input.action_release("jump")
		check(Geometry2D.is_point_in_polygon(Vector2(player.position.x, player.position.z), boundary.points), "Sprint/jump escaped boundary %d" % i)
	# Persistent state roundtrip is isolated from the user's save.
	manager.persistence_enabled = true
	manager.progress = {"sitios": {"visits": 3, "session_seed": 1998, "flags": {"gate_crossed": true}}}
	check(manager.save_progress("user://ermo_test_visits.json") == OK, "Save failed")
	manager.progress.clear()
	manager.load_progress("user://ermo_test_visits.json")
	check(int(manager.progress.get("sitios", {}).get("visits", 0)) == 3, "Visit counter not restored")
	check(manager.progress.get("sitios", {}).get("flags", {}).get("gate_crossed", false), "Environment state not restored")
	manager.persistence_enabled = false
	DirAccess.remove_absolute("user://ermo_test_visits.json")
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	scene._unhandled_input(escape)
	check(paused, "Pause failed")
	scene._unhandled_input(escape)
	check(not paused, "Resume failed")
	# Test the real E trigger, fade, scene unloading and subsequent visit.
	current_scene = scene
	var previous: WeakRef = weakref(scene)
	manager.progress = {"sitios": {"visits": 1, "session_seed": 1998}}
	player.position = Vector3(0, 1.1, 74)
	player.velocity = Vector3.ZERO
	player.set_physics_process(true)
	await settle()
	var passage: Area3D = world.get_node("Passage")
	check(passage.player == player, "Gate cannot detect approaching player")
	var event := InputEventKey.new()
	event.keycode = KEY_E
	event.pressed = true
	passage._unhandled_input(event)
	await create_timer(1.2).timeout
	check(previous.get_ref() == null, "Old universe retained after transition")
	check(current_scene != null and current_scene.scene_file_path == "res://scenes/exploration.tscn", "Gate did not reload universe")
	if current_scene != null and previous.get_ref() == null:
		check(current_scene.get_node("World").loaded, "New visit did not become playable")
		check(int(current_scene.get_node("World").visit.visit) == 1, "Passage did not advance visit")
		check(current_scene.get_node("World").visit.entry != first_entry, "Passage repeated entrance")
		current_scene.free()
	print("%s: %d checks, %d failures — deterministic visits, four spawns, routes, house, boundaries, persistence, pause, gate transition." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	quit(0 if failures == 0 else 1)
