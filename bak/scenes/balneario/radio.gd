extends Node3D
## A rare, repeatable landmark. AudioStreamPlayer3D performs distance attenuation.
const RADIO_BUS := "BalnearioRadio"
const SONG: AudioStreamMP3 = preload("res://balneario.mp3")
const G = preload("res://scripts/world/balneario_geometry.gd")
static var prepared_bus := false

func _ready() -> void:
	_build_radio()
	_prepare_radio_bus()
	var song := SONG.duplicate() as AudioStreamMP3
	song.loop = true
	$RadioSound.stream = song
	$RadioSound.bus = RADIO_BUS
	$RadioSound.play()

func _build_radio() -> void:
	var dark := G.material(Color(0.055, 0.06, 0.05))
	var case := G.material(Color(0.20, 0.22, 0.17))
	var brass := G.material(Color(0.61, 0.44, 0.19))
	# Relative to an existing table's origin; tabletop is at y=0.84 m.
	G.box(self, Vector3(0, 1.17, -0.03), Vector3(0.72, 0.56, 0.42), case)
	G.box(self, Vector3(-0.13, 1.18, 0.19), Vector3(0.38, 0.34, 0.025), dark)
	G.box(self, Vector3(0.25, 1.28, 0.19), Vector3(0.16, 0.13, 0.025), brass)
	G.box(self, Vector3(0.25, 1.08, 0.19), Vector3(0.09, 0.06, 0.03), dark)
	for i in range(5):
		G.box(self, Vector3(-0.28 + i * 0.075, 1.18, 0.208), Vector3(0.018, 0.28, 0.012), brass)
	G.cylinder(self, Vector3(0.22, 1.45, -0.15), 0.045, 0.06, brass)
	var antenna := G.cylinder(self, Vector3(0.31, 1.7, -0.16), 0.018, 0.5, brass)
	antenna.rotation.z = -0.2
	# A small green power indicator, deliberately subtle in the day.
	var led := G.material(Color(0.36, 0.62, 0.19))
	G.box(self, Vector3(0.31, 1.18, 0.215), Vector3(0.035, 0.025, 0.015), led)

func _prepare_radio_bus() -> void:
	if prepared_bus:
		return
	var bus_index := AudioServer.get_bus_index(RADIO_BUS)
	if bus_index < 0:
		bus_index = AudioServer.bus_count
		AudioServer.add_bus(bus_index)
		AudioServer.set_bus_name(bus_index, RADIO_BUS)
		AudioServer.set_bus_send(bus_index, "Master")
	if AudioServer.get_bus_effect_count(bus_index) == 0:
		var high_pass := AudioEffectHighPassFilter.new()
		high_pass.cutoff_hz = 320.0
		AudioServer.add_bus_effect(bus_index, high_pass)
		var low_pass := AudioEffectLowPassFilter.new()
		low_pass.cutoff_hz = 3100.0
		AudioServer.add_bus_effect(bus_index, low_pass)
		var distortion := AudioEffectDistortion.new()
		distortion.mode = AudioEffectDistortion.MODE_OVERDRIVE
		distortion.drive = 0.12
		distortion.pre_gain = -4.0
		AudioServer.add_bus_effect(bus_index, distortion)
	prepared_bus = true
