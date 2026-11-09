extends SceneTree
## godot --path . --script tests/capture.gd
## Captures the actual game viewport, not an offline mock-up.
func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	var scene := load("res://scenes/main.tscn").instantiate() as Node3D
	root.add_child(scene)
	await scene.get_node("World").world_ready
	await create_timer(2.0).timeout
	scene.get_node("Interface").hide()
	await RenderingServer.frame_post_draw
	DirAccess.make_dir_recursive_absolute("res://captures")
	root.get_texture().get_image().save_png("res://captures/01_arrival.png")
	var field: Landscape = scene.get_node("World").field
	var player: CharacterBody3D = scene.get_node("Player")
	player.set_physics_process(false)
	var site: Dictionary = field.nearby_sites(30, -12).filter(func(s: Dictionary) -> bool: return s.echo)[0]
	var p: Vector2 = site.position
	player.position = Vector3(p.x + 7, field.surface_height(p.x + 7, p.y + 18), p.y + 18)
	player.rotation.y = 0.32
	player.get_node("Head/Camera3D").rotation.x = 0.035
	await create_timer(1.0).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://captures/02_alpendre.png")
	print("Viewport captures saved in captures/.")
	scene.get_node("Soundscape/Wind").stop()
	scene.get_node("Soundscape/Distant").stop()
	scene.get_node("Player/Footsteps").stop()
	scene.queue_free()
	await create_timer(0.2).timeout
	quit()
