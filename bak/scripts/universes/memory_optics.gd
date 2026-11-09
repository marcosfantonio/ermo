extends CanvasLayer
var enabled := true

func apply_profile(profile: UniverseProfile) -> void:
	for parameter in ["distortion", "grain", "vignette", "chromatic", "dither"]:
		$Filter.material.set_shader_parameter(parameter, profile.get(parameter))

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_F3:
		enabled = not enabled
		$Filter.material.set_shader_parameter("strength", 1.0 if enabled else 0.0)
