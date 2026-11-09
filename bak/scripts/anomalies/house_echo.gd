extends Node3D
## Exact repeats are a world rule, not a transient random event.
## Audio memory survives unloading. Returning never moves or replaces a house.
static var heard: Dictionary = {}
var residence := 0.0
var player: Node3D

func _ready() -> void:
	player = get_tree().get_first_node_in_group("player")

func _process(delta: float) -> void:
	if not is_instance_valid(player):
		return
	var p: Vector2 = get_parent().get_meta("world_position")
	var key := Vector2i(roundi(p.x), roundi(p.y))
	if heard.has(key):
		return
	if player.global_position.distance_to(global_position) < 9.0:
		residence += delta
		if residence > 7.0:
			heard[key] = true
			$EmptyRoom.play()
	else:
		residence = 0.0
