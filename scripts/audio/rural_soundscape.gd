extends Node3D
@export var player: CharacterBody3D
@export var settings: WorldSettings
const STEPS = {
	"grass": preload("res://assets/audio/step_grass.wav"),
	"dirt": preload("res://assets/audio/step_dirt.wav"),
	"cement": preload("res://assets/audio/step_cement.wav")}
const CALLS = [preload("res://assets/audio/bird.wav"), preload("res://assets/audio/dog.wav"), preload("res://assets/audio/insects.wav")]
var rng := RandomNumberGenerator.new()
var next_call := 17.0
var next_wind := 2.0

func _ready() -> void:
	rng.randomize()
	player.footstep.connect(_step)
	AudioServer.set_bus_volume_db(0, linear_to_db(maxf(settings.sound_volume, 0.001)))

func _process(delta: float) -> void:
	next_call -= delta
	next_wind -= delta
	if next_call <= 0:
		next_call = rng.randf_range(19, 48)
		$Distant.position = player.position + Vector3(rng.randf_range(-45, 45), 6, rng.randf_range(-60, 40))
		$Distant.stream = CALLS[rng.randi_range(0, CALLS.size() - 1)]
		$Distant.pitch_scale = rng.randf_range(0.88, 1.08)
		$Distant.play()
	if next_wind <= 0:
		next_wind = rng.randf_range(25, 44)
		$Wind.play()

func _step(surface: String, strength: float) -> void:
	var sound: AudioStreamPlayer = player.get_node("Footsteps")
	sound.stream = STEPS[surface]
	sound.pitch_scale = rng.randf_range(0.88, 1.12)
	sound.volume_db = lerpf(-22, -14, strength)
	sound.play()
