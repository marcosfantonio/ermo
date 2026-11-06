@tool
extends "res://scripts/world/editor_geometry.gd"
## Authored control points in world XZ. Generation only drapes the road on terrain.
@export var points: PackedVector2Array
@export var width := 2.8
@export var terrain: Node3D

func distance_to_path(p: Vector2) -> float:
	var distance := INF
	for i in range(points.size() - 1):
		distance = minf(distance, p.distance_to(Geometry2D.get_closest_point_to_segment(p, points[i], points[i + 1])))
	return distance

func _build_geometry() -> void:
	if terrain == null or points.size() < 2:
		return
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(points.size() - 1):
		var a := points[i]
		var b := points[i + 1]
		var count := ceili(a.distance_to(b))
		var tangent := (b - a).normalized()
		var normal := Vector2(-tangent.y, tangent.x) * width * 0.5
		for j in range(count):
			var p := a.lerp(b, float(j) / count)
			var q := a.lerp(b, float(j + 1) / count)
			for v: Vector2 in [p-normal, q+normal, p+normal, p-normal, q-normal, q+normal]:
				surface.set_uv(v * 0.32)
				surface.set_color(Color(0.72, 0.46, 0.25))
				surface.add_vertex(Vector3(v.x, terrain.mesh_height(v.x, v.y) + 0.045, v.y))
	surface.generate_normals()
	var road := MeshInstance3D.new()
	road.name = "DirtRoad"
	road.mesh = surface.commit()
	road.material_override = preload("res://materials/grass.tres")
	add_child(road)
