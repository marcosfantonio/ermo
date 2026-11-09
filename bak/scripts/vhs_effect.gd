extends CanvasLayer
## F3 bypasses tape artefacts without removing the richer colour grading.
@export var enabled := true
@export_range(0.0, 1.0) var strength := 0.7

func _ready() -> void:
	_apply()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_F3:
		enabled = not enabled
		_apply()

func _apply() -> void:
	$Filter.material.set_shader_parameter("tape_strength", strength if enabled else 0.0)
