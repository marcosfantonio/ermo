extends Node3D
@onready var loading: ColorRect = $Interface/Loading
@onready var hint: Label = $Interface/Hint
@onready var pause_panel: PanelContainer = $Interface/Pause
var hint_time := 11.0
var ready_to_walk := false

func _ready() -> void:
	$World.world_ready.connect(_world_ready)
	$World.rebased.connect(func(shift: Vector3) -> void: $Soundscape/Distant.position -= shift)
	$Interface/Pause/Margin/Options/Resume.pressed.connect(_resume)
	$Interface/Pause/Margin/Options/Quit.pressed.connect(func() -> void: get_tree().quit())
	$Interface/Pause/Margin/Options/Motion.toggled.connect(func(value: bool) -> void: $Player.motion_enabled = value)
	$Interface/Pause/Margin/Options/Volume.value_changed.connect(func(value: float) -> void: AudioServer.set_bus_volume_db(0, linear_to_db(maxf(value, 0.001))))

func _world_ready() -> void:
	ready_to_walk = true
	var tween := create_tween()
	tween.tween_property(loading, "modulate:a", 0.0, 1.1)
	tween.tween_callback(loading.hide)

func _process(delta: float) -> void:
	if ready_to_walk and not get_tree().paused:
		hint_time -= delta
		hint.modulate.a = clampf(hint_time, 0, 1)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_ESCAPE:
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

func _resume() -> void:
	pause_panel.hide()
	get_tree().paused = false
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
