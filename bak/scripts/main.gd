extends Node3D
const FARM_SCENE := "res://scenes/main.tscn"
const POOL_SCENE := "res://scenes/balneario_infinito.tscn"
@onready var loading: ColorRect = $Interface/Loading
@onready var hint: Label = $Interface/Hint
@onready var pause_panel: PanelContainer = $Interface/Pause
var hint_time := 11.0
var ready_to_walk := false
var switching_level := false
var radio_buffer := ""
var radio_last_key_time := 0
var radio_feedback_time := 0.0
var original_hint := ""

func _ready() -> void:
	$World.world_ready.connect(_world_ready)
	if $World.has_signal("rebased"):
		$World.rebased.connect(func(shift: Vector3) -> void: $Soundscape/Distant.position -= shift)
	$Interface/Pause/Margin/Options/Resume.pressed.connect(_resume)
	$Interface/Pause/Margin/Options/Quit.pressed.connect(func() -> void: get_tree().quit())
	$Interface/Pause/Margin/Options/Motion.toggled.connect(func(value: bool) -> void: $Player.motion_enabled = value)
	$Interface/Pause/Margin/Options/Volume.value_changed.connect(func(value: float) -> void: AudioServer.set_bus_volume_db(0, linear_to_db(maxf(value, 0.001))))
	original_hint = hint.text

func _world_ready() -> void:
	ready_to_walk = true
	var tween := create_tween()
	tween.tween_property(loading, "modulate:a", 0.0, 1.1)
	tween.tween_callback(loading.hide)

func _process(delta: float) -> void:
	if radio_feedback_time > 0:
		radio_feedback_time -= delta
		if radio_feedback_time <= 0:
			hint.text = original_hint
			hint_time = 4.0
	if ready_to_walk and not get_tree().paused:
		hint_time -= delta
		hint.modulate.a = clampf(hint_time, 0, 1)

func _unhandled_input(event: InputEvent) -> void:
	if switching_level:
		return
	if event is InputEventKey and event.pressed and not event.echo:
		if _radio_command_key(event):
			return
		if event.keycode in [KEY_1, KEY_KP_1]:
			_switch_level(FARM_SCENE)
		elif event.keycode in [KEY_2, KEY_KP_2]:
			_switch_level(POOL_SCENE)
		elif event.keycode == KEY_ESCAPE:
			if get_tree().paused:
				_resume()
			else:
				get_tree().paused = true
				$Interface/Pause/Margin/Options/Motion.set_pressed_no_signal($Player.motion_enabled)
				pause_panel.show()
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		elif event.keycode == KEY_F11:
			var fullscreen := DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED if fullscreen else DisplayServer.WINDOW_MODE_FULLSCREEN)

func _radio_command_key(event: InputEventKey) -> bool:
	if not has_node("World") or not $World.has_method("spawn_radio_near_player"):
		return false
	var character: String = {
		KEY_R: "r", KEY_A: "a", KEY_D: "d", KEY_I: "i", KEY_O: "o"
	}.get(event.keycode, "")
	if character.is_empty():
		return false
	var now := Time.get_ticks_msec()
	if now - radio_last_key_time > 1800:
		radio_buffer = ""
	radio_last_key_time = now
	radio_buffer = (radio_buffer + character).right(5)
	if radio_buffer != "radio":
		return false
	radio_buffer = ""
	var spawned: bool = $World.spawn_radio_near_player()
	hint.text = "Um rádio apareceu numa mesa próxima." if spawned else "Não há mesa livre por perto."
	hint.modulate.a = 1.0
	radio_feedback_time = 3.0
	return true

func _switch_level(path: String) -> void:
	if scene_file_path == path:
		return
	switching_level = true
	get_tree().paused = false
	# Replace the old world rather than retaining two sets of streamed chunks.
	_load_level.call_deferred(path)

func _load_level(path: String) -> void:
	var error := get_tree().change_scene_to_file(path)
	if error != OK:
		switching_level = false
		push_error("Não foi possível carregar a fase: %s (%s)" % [path, error])

func _resume() -> void:
	pause_panel.hide()
	get_tree().paused = false
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
