@tool
extends Node3D
## Place mutable objects under this node; landmarks stay outside it.
@export_flags("Familiar", "Desvio") var configurations := 3

func apply(variant: int) -> void:
	var active := (configurations & (1 << variant)) != 0
	visible = active
	process_mode = Node.PROCESS_MODE_INHERIT if active else Node.PROCESS_MODE_DISABLED
	_set_collisions(self, active)

func _set_collisions(node: Node, active: bool) -> void:
	if node is CollisionObject3D:
		if not node.has_meta("visit_collision_layer"):
			node.set_meta("visit_collision_layer", node.collision_layer)
		node.collision_layer = int(node.get_meta("visit_collision_layer")) if active else 0
	for child in node.get_children():
		_set_collisions(child, active)
