extends "res://scripts/main.gd"

func _ready() -> void:
	super._ready()
	var profile: UniverseProfile = $World.profile
	$Environment.environment = profile.environment
	$Player/Head/Camera3D.fov = profile.field_of_view
	$Optics.apply_profile(profile)
	$Soundscape.rng.seed = int($World.visit.seed)
	AudioServer.set_bus_volume_db(0, linear_to_db(maxf(profile.sound_volume, 0.001)))
	UniverseManager.transition_failed.connect(_transition_failed)

func _process(delta: float) -> void:
	super._process(delta)
	$Interface/PassageHint.visible = $World/Passage.player != null and not switching_level and not get_tree().paused

func _switch_level(path: String) -> void:
	# Preserve the existing prototype shortcuts, but 1 stays in this authored slice.
	if path == FARM_SCENE:
		return
	super._switch_level(path)

func cross_passage(destination: UniverseProfile, passage: Area3D) -> void:
	if switching_level:
		return
	switching_level = true
	$Player.set_physics_process(false)
	loading.show()
	loading.get_node("Subtitle").text = ""
	loading.get_node("Title").text = ""
	var tween := create_tween()
	tween.tween_property(loading, "modulate:a", 1.0, 0.4)
	await tween.finished
	UniverseManager.remember("gate_crossed", true)
	var error := UniverseManager.travel(destination)
	if error != OK:
		passage.busy = false
		_transition_failed(error)

func _transition_failed(_error: Error) -> void:
	switching_level = false
	$World/Passage.busy = false
	$Player.set_physics_process(true)
	loading.hide()
