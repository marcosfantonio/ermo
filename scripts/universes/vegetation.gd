@tool
extends "res://scripts/world/editor_geometry.gd"
## Only secondary foliage varies. Zones are hand placed, clear of all routes.
@export var terrain: Node3D
@export var seed_value := 1998
const TREE = preload("res://scenes/props/tree.tscn")

func _build_geometry() -> void:
	if terrain == null:
		return
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	for zone in $Zones.get_children():
		for i in range(int(zone.get_meta("count", 3))):
			var p := Vector2(zone.position.x, zone.position.z) + Vector2(rng.randf_range(-zone.scale.x, zone.scale.x), rng.randf_range(-zone.scale.z, zone.scale.z))
			var tree := TREE.instantiate() as Node3D
			tree.name = "%s_%d" % [zone.name, i]
			tree.position = Vector3(p.x, terrain.mesh_height(p.x, p.y), p.y)
			tree.rotation.y = rng.randf_range(-PI, PI)
			var size := rng.randf_range(0.7, 1.5)
			tree.scale = Vector3(size, size * rng.randf_range(0.8, 1.2), size)
			add_child(tree)
