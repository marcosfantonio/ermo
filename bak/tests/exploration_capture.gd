extends SceneTree
## Actual Compatibility-renderer captures; no offline rendering or third-party assets.
func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	var manager := root.get_node("UniverseManager")
	manager.persistence_enabled = false
	manager.replay(&"sitios", 0, 1998)
	var scene: Node3D = load("res://scenes/exploration.tscn").instantiate()
	root.add_child(scene)
	await scene.get_node("World").world_ready
	await create_timer(1.5).timeout
	scene.get_node("Interface").hide()
	var player: CharacterBody3D = scene.get_node("Player")
	player.set_physics_process(false)
	var world: Node3D = scene.get_node("World")
	var viewpoints := [Vector3(-17, 0, 53), Vector3(-24, 0, 44), Vector3(24, 0, -17), Vector3(-54, 0, -33)]
	var targets := [Vector3(-32, 3.5, 32), Vector3(-32, 3.5, 32), Vector3(46, 6, -38), Vector3(-55, 5.5, -58)]
	for i in range(viewpoints.size()):
		var point: Vector3 = viewpoints[i]
		point.y = world.get_node("Terrain").mesh_height(point.x, point.z)
		player.position = point
		player.rotation = Vector3.ZERO
		var camera: Camera3D = player.get_node("Head/Camera3D")
		camera.look_at(targets[i])
		await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("res://captures/reform_%d.png" % i)
		print("View %d: %d draw calls, %d primitives" % [i,
			Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
			Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)])
	print("Capturas reais em captures/reform_0.png a reform_3.png")
	scene.free()
	quit()
