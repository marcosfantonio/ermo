extends Area3D
## A normal rural gate. Interaction is explicit to prevent accidental visit loops.
@export var destination: UniverseProfile
var player: CharacterBody3D
var busy := false

func _ready() -> void:
	body_entered.connect(func(body: Node3D) -> void:
		if body.is_in_group("player"):
			player = body)
	body_exited.connect(func(body: Node3D) -> void:
		if body == player:
			player = null)

func _unhandled_input(event: InputEvent) -> void:
	if player == null or busy or get_tree().paused:
		return
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_E:
		busy = true
		var scene := get_tree().current_scene
		if scene != null and scene.has_method("cross_passage"):
			scene.cross_passage(destination, self)
		else:
			busy = UniverseManager.travel(destination) == OK
		get_viewport().set_input_as_handled()
