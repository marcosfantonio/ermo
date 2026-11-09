@tool
extends "res://scripts/world/editor_geometry.gd"
## Hand-shaped hills, not a noise world. The same mesh is used for collision.
@export var extent := Vector2(150, 138)
@export_range(2.0, 8.0) var grid_step := 4.0
@export var material: Material = preload("res://materials/grass.tres")

func height_at(x: float, z: float) -> float:
	var h := 0.0
	for hill in $Contours.get_children():
		var d := Vector2((x - hill.position.x) / hill.scale.x, (z - hill.position.z) / hill.scale.z).length()
		h += hill.position.y * exp(-d * d * 2.0)
	# Flatten the planned yards; no per-visit geometry changes below the houses.
	for yard in $Yards.get_children():
		var d := Vector2(x - yard.position.x, z - yard.position.z).length()
		h = lerpf(h, yard.position.y, 1.0 - smoothstep(8.0, 19.0, d))
	return h

func mesh_height(x: float, z: float) -> float:
	var p := Vector2(floorf((x + extent.x) / grid_step) * grid_step - extent.x,
		floorf((z + extent.y) / grid_step) * grid_step - extent.y)
	var f := (Vector2(x, z) - p) / grid_step
	var a := height_at(p.x, p.y)
	var b := height_at(p.x + grid_step, p.y)
	var c := height_at(p.x, p.y + grid_step)
	var d := height_at(p.x + grid_step, p.y + grid_step)
	if f.x + f.y <= 1.0:
		return a + (b - a) * f.x + (c - a) * f.y
	return d + (c - d) * (1.0 - f.x) + (b - d) * (1.0 - f.y)

func _build_geometry() -> void:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for iz in range(ceili(extent.y * 2.0 / grid_step)):
		for ix in range(ceili(extent.x * 2.0 / grid_step)):
			for corner: Vector2 in [Vector2(0,0), Vector2(1,0), Vector2(0,1), Vector2(1,0), Vector2(1,1), Vector2(0,1)]:
				var p := -extent + (Vector2(ix, iz) + corner) * grid_step
				var patch := sin(p.x * 0.07 + sin(p.y * 0.1)) * cos(p.y * 0.05)
				surface.set_color(Color(0.32 + patch * 0.035, 0.59 + patch * 0.045, 0.16 + patch * 0.02))
				surface.set_uv(p * 0.24)
				surface.add_vertex(Vector3(p.x, height_at(p.x, p.y), p.y))
	surface.generate_normals()
	var mesh_instance := MeshInstance3D.new()
	mesh_instance.name = "Hills"
	mesh_instance.mesh = surface.commit()
	mesh_instance.material_override = material
	add_child(mesh_instance)
	var ground := StaticBody3D.new()
	ground.name = "Ground"
	ground.set_meta("surface", "grass")
	var collision := CollisionShape3D.new()
	collision.shape = mesh_instance.mesh.create_trimesh_shape()
	ground.add_child(collision)
	add_child(ground)
