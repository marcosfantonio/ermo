@tool
extends "res://scripts/world/editor_geometry.gd"
## Visible, solid rural property walls follow an irregular authored contour.
@export var points: PackedVector2Array
@export var terrain: Node3D
@export var height := 2.4

func _build_geometry() -> void:
	if terrain == null:
		return
	var stone := StandardMaterial3D.new()
	stone.albedo_color = Color(0.39, 0.41, 0.29)
	stone.roughness = 1.0
	stone.albedo_texture = preload("res://assets/textures/plaster.png")
	stone.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(points.size() - 1):
		var a := points[i]
		var b := points[i + 1]
		var count := ceili(a.distance_to(b) / 3.0)
		for j in range(count):
			var p := a.lerp(b, (float(j) + 0.5) / count)
			var box := BoxMesh.new()
			box.size = Vector3(a.distance_to(b) / count + 0.1, height + 0.18 * sin(i + j), 0.75)
			var transform := Transform3D(Basis(Vector3.UP, -atan2(b.y - a.y, b.x - a.x)),
				Vector3(p.x, terrain.mesh_height(p.x, p.y) + box.size.y * 0.5 - 0.15, p.y))
			surface.append_from(box, 0, transform)
	var wall := MeshInstance3D.new()
	wall.name = "StoneBoundary"
	wall.mesh = surface.commit()
	wall.material_override = stone
	add_child(wall)
	var body := StaticBody3D.new()
	var shape := CollisionShape3D.new()
	shape.shape = wall.mesh.create_trimesh_shape()
	body.add_child(shape)
	wall.add_child(body)
