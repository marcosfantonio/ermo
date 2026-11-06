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

func snapshot(node: Node) -> Array:
	var result: Array = []
	if node.name == "RadioProp":
		return result
	if node is Node3D:
		result.append(node.transform)
	if node is MeshInstance3D:
		result.append(node.mesh.get_aabb())
		if node.material_override is StandardMaterial3D:
			result.append(node.material_override.albedo_color)
	if node is Label3D:
		result.append(node.text)
	for child in node.get_children():
		result.append(snapshot(child))
	return result

func closed_mesh(mesh: Mesh) -> bool:
	var vertices := mesh.get_faces()
	var edges: Dictionary = {}
	for i in range(0, vertices.size(), 3):
		for j in range(3):
			var a := str(Vector3i((vertices[i + j] * 1000).round()))
			var b := str(Vector3i((vertices[i + (j + 1) % 3] * 1000).round()))
			var pair := [a, b]
			pair.sort()
			var key: String = pair[0] + ":" + pair[1]
			edges[key] = int(edges.get(key, 0)) + 1
	for count: int in edges.values():
		if count != 2:
			return false
	return true

func spacing_ok(cell: Vector2i, size: float) -> bool:
	var local := BalnearioLayout.access_footprints(BalnearioLayout.height_for(cell), size)
	for i in range(local.size()):
		for j in range(i + 1, local.size()):
			if local[i].grow(0.75).intersects(local[j].grow(0.75)):
				return false
		for z in range(-1, 2):
			for x in range(-1, 2):
				if x == 0 and z == 0:
					continue
				for other in BalnearioLayout.access_footprints(BalnearioLayout.height_for(cell + Vector2i(x, z)), size):
					other.position += Vector2(x, z) * size
					if local[i].grow(0.75).intersects(other.grow(0.75)):
						return false
	return true

func has_ramp_side(mesh: Mesh, x: float) -> bool:
	var faces := mesh.get_faces()
	for i in range(0, faces.size(), 3):
		var a := faces[i]
		var b := faces[i + 1]
		var c := faces[i + 2]
		if not (is_equal_approx(a.x, x) and is_equal_approx(b.x, x) and is_equal_approx(c.x, x)):
			continue
		if minf(a.z, minf(b.z, c.z)) < -28.51 or maxf(a.z, maxf(b.z, c.z)) > -23.29:
			continue
		var normal := -(b - a).cross(c - a).normalized()
		if normal.dot(Vector3(signf(x), 0, 0)) > 0.9:
			return true
	return false

func first_named(node: Node, target: String) -> Node:
	if node.name == target:
		return node
	for child in node.get_children():
		var found := first_named(child, target)
		if found != null:
			return found
	return null

func count_radios(node: Node) -> int:
	var count := 1 if node.name == "RadioProp" else 0
	for child in node.get_children():
		count += count_radios(child)
	return count

func _run() -> void:
	var profile := load("res://resources/balneario.tres") as WorldSettings
	var generator := BalnearioGenerator.new(profile)
	for z in range(-3, 4):
		for x in range(-3, 4):
			var cell := Vector2i(x, z)
			check(spacing_ok(cell, profile.chunk_size), "Access lanes too close within/across chunks: %s" % cell)
			var plans := BalnearioLayout.lower_pools(cell, profile.chunk_size)
			check(plans.size() == 3, "Missing lower leisure pools: %s" % cell)
			var center := Vector2(cell) * profile.chunk_size + Vector2.ONE * profile.chunk_size * 0.5
			if center.length() < 256.0:
				check(not BalnearioLayout.radio_roll(cell, profile.world_seed, profile.chunk_size), "Radio appeared before long-distance exploration")
	var radio_cell := Vector2i.ZERO
	var found_radio := false
	for z in range(-60, 61):
		for x in range(-60, 61):
			var candidate := Vector2i(x, z)
			if BalnearioLayout.radio_roll(candidate, profile.world_seed, profile.chunk_size):
				radio_cell = candidate
				found_radio = true
				break
		if found_radio:
			break
	check(found_radio, "No deterministic radio location found after exploring far away")
	if found_radio:
		var radio_chunk := generator.build(radio_cell)
		var radio := first_named(radio_chunk, "RadioProp") as Node3D
		check(radio != null, "Rare radio did not spawn")
		check(radio != null and radio.get_parent().scene_file_path == "res://scenes/balneario/parasol.tscn", "Rare radio did not use an existing table: %s" % (radio.get_parent().name if radio != null else "missing"))
		var radio_audio: AudioStreamPlayer3D = radio.get_node("RadioSound")
		check(radio_audio.max_distance >= 100.0 and radio_audio.unit_size >= 7.0, "Radio cannot be heard from afar")
		check(radio_audio.max_polyphony == 1, "Radio music is not a single continuous source")
		check(load("res://balneario.mp3") is AudioStreamMP3, "Balneario song asset missing")
		root.add_child(radio_chunk)
		await process_frame
		check(AudioServer.get_bus_index("BalnearioRadio") >= 0, "Radio filter bus was not created")
		check(AudioServer.get_bus_effect_count(AudioServer.get_bus_index("BalnearioRadio")) >= 3, "Radio band-limiting/distortion effects missing")
		radio_audio.stop()
		radio_chunk.free()
	for cell in [Vector2i.ZERO, Vector2i(-12, 7), Vector2i(29, -41)]:
		var a := generator.build(cell)
		var b := generator.build(cell)
		root.add_child(a)
		root.add_child(b)
		check(snapshot(a) == snapshot(b), "Regeneration changed pool/props %s" % cell)
		a.free()
		b.free()
	var scene := load("res://scenes/balneario_infinito.tscn").instantiate() as Node3D
	root.add_child(scene)
	var world: Node3D = scene.get_node("World")
	await world.world_ready
	var player: CharacterBody3D = scene.get_node("Player")
	var courtyard_height := BalnearioLayout.height_for(Vector2i.ZERO)
	for i in range(90):
		await physics_frame
	check(player.is_on_floor(), "Spawn lacks deck collision")
	check(absf(player.position.y - courtyard_height) < 0.1, "Spawn fell through raised deck")
	var levels: Array[float] = []
	for chunk: Node3D in world.active.values():
		var level: float = chunk.get_meta("layout")[2]
		if not levels.has(level):
			levels.append(level)
	check(levels.has(0.0) and levels.has(1.6) and levels.has(3.2), "Missing different courtyard levels")
	var home_chunk: Node3D = world.active[Vector2i.ZERO]
	var stairs: MeshInstance3D = home_chunk.get_node("Terraces/StairsAccess")
	check(stairs.mesh.get_aabb().size.z < 4.0, "Circulation stairs are not compact")
	var paving: ShaderMaterial = home_chunk.get_node("Terraces/Stone").material_override
	check(paving.shader == home_chunk.get_node("PoolCourtyard/Deck").material_override.shader, "Upper and lower paving shaders differ")
	var cascade: MeshInstance3D = home_chunk.get_node("Waterslide/CascadeFoundation")
	check(closed_mesh(cascade.mesh), "Cascade foundation has open sides or underside")
	var spiral_chunk: Node3D = world.active[Vector2i(1, 0)]
	var spiral: MeshInstance3D = spiral_chunk.get_node("Waterslide/YellowSpiral")
	check(closed_mesh(spiral.mesh), "Spiral flume has holes or missing end caps")
	var retaining: ShaderMaterial = home_chunk.get_node("Terraces/Retaining").material_override
	check(retaining.shader.resource_path == "res://shaders/balneario_rock.gdshader", "Tall walls do not use fieldstone")
	var retaining_mesh: Mesh = home_chunk.get_node("Terraces/Retaining").mesh
	check(has_ramp_side(retaining_mesh, -2.9) and has_ramp_side(retaining_mesh, 2.9), "Ramp cheeks missing or back-facing after mesh batching")
	var variants: Array[int] = []
	for chunk: Node3D in world.active.values():
		var variant: int = chunk.get_meta("layout")[3]
		if not variants.has(variant):
			variants.append(variant)
	check(variants.size() == 4, "Missing oval, small, large rectangular or square pool variant")
	var expected := BalnearioLayout.access_footprints(courtyard_height, profile.chunk_size)
	for access in home_chunk.get_node("Terraces").get_children():
		if not access.has_meta("access_footprint"):
			continue
		var actual: Rect2 = access.get_meta("access_footprint")
		actual.position += Vector2.ONE * profile.chunk_size * 0.5
		check(expected.any(func(rect: Rect2) -> bool: return rect.is_equal_approx(actual)), "Built ramp differs from reserved access lane")
	# A small lower basin must be recessed, not hidden underneath the old base slab.
	player.position = Vector3(14, 0.1, 4.5)
	for i in range(60):
		await physics_frame
	check(player.is_on_floor() and player.position.y < -0.35, "Lower plaza basin is blocked by base floor")
	Input.action_press("backward")
	for i in range(95):
		await physics_frame
	Input.action_release("backward")
	check(player.position.z > 8.4 and player.position.y > -0.1 and player.is_on_floor(), "Cannot leave lower pool or cross plaza/base seam")
	player.set_physics_process(false)
	var home := snapshot(world.active[Vector2i.ZERO])
	world.set_player_world_position(Vector3(1024, 0.1, -1024))
	await process_frame
	await process_frame
	while not world.pending.is_empty():
		await process_frame
	check(not world.active.has(Vector2i.ZERO), "Old pool chunk not unloaded")
	check(world.active.size() == 49, "Streaming exceeds configured radius")
	check(absf(player.position.x) < 512 and absf(player.position.z) < 512, "Floating origin failed")
	world.set_player_world_position(Vector3(32, courtyard_height + 0.1, 53))
	await process_frame
	await process_frame
	while not world.pending.is_empty():
		await process_frame
	var returned := snapshot(world.active[Vector2i.ZERO])
	check(returned == home, "Return changed courtyard")
	var radios_before := count_radios(world)
	for code in [KEY_R, KEY_A, KEY_D, KEY_I, KEY_O]:
		var typed := InputEventKey.new()
		typed.pressed = true
		typed.keycode = code
		scene._unhandled_input(typed)
	var radios_after := count_radios(world)
	check(radios_after == radios_before + 1, "Typing RADIO did not spawn one nearby radio")
	var summoned := first_named(world, "RadioProp") as Node3D
	check(summoned != null and summoned.get_parent().name == "ParasolTable", "Summoned radio is not attached to an existing table")
	if summoned != null:
		check(summoned.global_position.distance_to(player.global_position) < 100.0, "Summoned radio is not near player")
	# Walk into the basin and back out over the submerged ramps.
	player.position = Vector3(32, courtyard_height + 0.05, 49)
	player.rotation.y = 0
	player.velocity = Vector3.ZERO
	player.set_physics_process(true)
	Input.action_press("forward")
	for i in range(150):
		await physics_frame
	Input.action_release("forward")
	check(player.position.z < 43, "Cannot enter pool")
	check(player.is_on_floor() and player.position.y > courtyard_height - 0.6, "Basin has no stable floor")
	Input.action_press("backward")
	for i in range(180):
		await physics_frame
	Input.action_release("backward")
	check(player.position.z > 48 and player.position.y > courtyard_height - 0.1, "Cannot leave pool by ramp: %s" % player.position)
	# Both visible staircases and ramps must connect real, different floor heights.
	for x in [32.0, 39.5]:
		player.position = Vector3(x, 0.1, 62)
		player.velocity = Vector3.ZERO
		Input.action_press("forward")
		for i in range(180):
			await physics_frame
		Input.action_release("forward")
		check(player.position.z < 54 and player.position.y > courtyard_height - 0.1 and player.is_on_floor(), "Cannot climb courtyard access at x=%s: %s" % [x, player.position])
		Input.action_press("backward")
		for i in range(210):
			await physics_frame
		Input.action_release("backward")
		check(player.position.z > 60 and player.position.y < 0.15 and player.is_on_floor(), "Cannot descend courtyard access: %s" % player.position)
	for x in [54.5, 49.1]:
		player.position = Vector3(x, courtyard_height + 0.1, 53.3)
		player.velocity = Vector3.ZERO
		Input.action_press("forward")
		for i in range(150):
			await physics_frame
		Input.action_release("forward")
		check(player.position.z < 46 and player.position.y > courtyard_height + 1.5 and player.is_on_floor(), "Cannot reach upper seating terrace at x=%s: %s" % [x, player.position])
		Input.action_press("backward")
		for i in range(150):
			await physics_frame
		Input.action_release("backward")
		check(player.position.z > 51 and absf(player.position.y - courtyard_height) < 0.15, "Cannot descend seating terrace: %s" % player.position)
	# Cross a chunk seam on the unobstructed edge walkway.
	player.position = Vector3(2, 0.1, 3)
	player.velocity = Vector3.ZERO
	Input.action_press("forward")
	var min_head := 10.0
	var max_head := -10.0
	for i in range(150):
		await physics_frame
		min_head = minf(min_head, player.head.position.y)
		max_head = maxf(max_head, player.head.position.y)
	Input.action_release("forward")
	check(player.position.z < 0 and player.is_on_floor(), "Chunk seam blocks walking or has no floor")
	check(max_head - min_head > 0.025, "Walking has no visible camera bob")
	for i in range(120):
		await physics_frame
	check(player.head.position.distance_to(Vector3(0, 1.62, 0)) < 0.001, "Camera keeps shaking at rest")
	var toggle := InputEventKey.new()
	toggle.pressed = true
	toggle.keycode = KEY_F2
	player._unhandled_input(toggle)
	Input.action_press("forward")
	for i in range(90):
		await physics_frame
	check(player.head.position.distance_to(Vector3(0, 1.62, 0)) < 0.001, "F2 did not disable camera movement")
	check(absf(player.camera.rotation.y) < 0.001 and absf(player.camera.rotation.z) < 0.001, "F2 left rotational shake enabled")
	Input.action_release("forward")
	player._unhandled_input(toggle)
	var vhs := scene.get_node("VHS")
	var material: ShaderMaterial = vhs.get_node("Filter").material
	check(float(material.get_shader_parameter("saturation")) > 1.0, "Saturation was not increased")
	check(vhs.layer < scene.get_node("Interface").layer, "VHS filter covers the pause menu")
	toggle.keycode = KEY_F3
	vhs._unhandled_input(toggle)
	check(is_zero_approx(float(material.get_shader_parameter("tape_strength"))), "F3 did not bypass VHS")
	vhs._unhandled_input(toggle)
	check(float(material.get_shader_parameter("tape_strength")) > 0.0, "F3 did not restore VHS")
	print("%s: %d checks, %d failures — balneário determinism, collision, ramps, seams, streaming and floating origin." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	scene.queue_free()
	await create_timer(0.2).timeout
	quit(0 if failures == 0 else 1)
