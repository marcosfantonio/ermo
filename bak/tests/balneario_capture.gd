extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	var scene := load("res://scenes/balneario_infinito.tscn").instantiate() as Node3D
	root.add_child(scene)
	await scene.get_node("World").world_ready
	await create_timer(2).timeout
	scene.get_node("Interface").hide()
	await RenderingServer.frame_post_draw
	DirAccess.make_dir_recursive_absolute("res://captures")
	root.get_texture().get_image().save_png("res://captures/balneario_arrival.png")
	var player: CharacterBody3D = scene.get_node("Player")
	player.set_physics_process(false)
	player.position = Vector3(57, 9, 59)
	player.rotation.y = 0.65
	player.get_node("Head/Camera3D").rotation.x = -0.28
	await create_timer(1).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://captures/balneario_overview.png")
	player.position = Vector3(41, 2, 64)
	player.rotation.y = -1.05
	player.get_node("Head/Camera3D").rotation.x = -0.25
	await create_timer(1).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://captures/balneario_lower_plaza.png")
	player.position = Vector3(176, 5, 59)
	player.rotation.y = 0.45
	player.get_node("Head/Camera3D").rotation.x = -0.22
	await create_timer(1).timeout
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png("res://captures/balneario_square_pool.png")
	scene.queue_free()
	await create_timer(0.2).timeout
	quit()
