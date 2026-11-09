@tool
extends Node3D
## Small authored differences, never random offsets on important architecture.
@export var target: Node3D
@export var familiar_position := Vector3(2.6, 0.11, 2.8)
@export var changed_position := Vector3(-2.7, 0.11, 3.0)
@export var familiar_yaw := -0.2
@export var changed_yaw := 0.45

func apply(variant: int) -> void:
	if target != null:
		target.position = familiar_position if variant == 0 else changed_position
		target.rotation.y = familiar_yaw if variant == 0 else changed_yaw
