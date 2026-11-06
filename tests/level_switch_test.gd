extends SceneTree
var failures := 0
var checks := 0
const FARM := "res://scenes/main.tscn"
const POOL := "res://scenes/balneario_infinito.tscn"

func _initialize() -> void:
	_run.call_deferred()

func check(value: bool, message: String) -> void:
	checks += 1
	if not value:
		failures += 1
		push_error(message)

func key(code: Key, pressed: bool = true, echo: bool = false) -> void:
	var event := InputEventKey.new()
	event.keycode = code
	event.pressed = pressed
	event.echo = echo
	current_scene._unhandled_input(event)

func wait_for_level(path: String) -> void:
	for i in range(1000):
		await process_frame
		if current_scene != null and current_scene.scene_file_path == path:
			var world := current_scene.get_node("World")
			if not world.loaded:
				await world.world_ready
			return
	check(false, "Timed out changing level to %s" % path)

func _run() -> void:
	change_scene_to_file(POOL)
	await wait_for_level(POOL)
	var original := current_scene.get_instance_id()
	key(KEY_2)
	key(KEY_1, false)
	key(KEY_1, true, true)
	await process_frame
	check(current_scene.get_instance_id() == original, "Same-level, released or repeated key restarted scene")
	var old_scene: WeakRef = weakref(current_scene)
	key(KEY_1)
	key(KEY_2) # A second input during replacement must not enqueue another change.
	await wait_for_level(FARM)
	check(current_scene.scene_file_path == FARM, "1 did not load farms")
	check(old_scene.get_ref() == null, "Old balneário was retained")
	check(current_scene.get_node_or_null("VHS") == null, "Balneário effects leaked into farms")
	key(KEY_ESCAPE)
	check(paused, "Pause setup failed")
	key(KEY_2)
	await wait_for_level(POOL)
	check(not paused, "Switch from pause left new level frozen")
	check(current_scene.get_node("Player").is_physics_processing(), "New player did not resume after loading")
	check(current_scene.get_node_or_null("VHS") != null, "Balneário VHS missing after return")
	key(KEY_KP_1)
	await wait_for_level(FARM)
	check(current_scene.scene_file_path == FARM, "Numpad 1 did not load farms")
	key(KEY_KP_2)
	await wait_for_level(POOL)
	check(current_scene.scene_file_path == POOL, "Numpad 2 did not load balneário")
	print("%s: %d checks, %d failures — level shortcuts, pause, repeated input and scene cleanup." % ["PASS" if failures == 0 else "FAIL", checks, failures])
	current_scene.queue_free()
	await create_timer(0.2).timeout
	quit(0 if failures == 0 else 1)
