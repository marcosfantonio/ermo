@tool
extends "res://scripts/world/editor_geometry.gd"
const G = preload("res://scripts/world/balneario_geometry.gd")

func _build_geometry() -> void:
	var trunk := G.material(Color(0.39, 0.32, 0.20))
	var leaves := G.material(Color(0.18, 0.34, 0.10))
	leaves.cull_mode = BaseMaterial3D.CULL_DISABLED
	G.cylinder(self, Vector3(0, 2.5, 0), 0.23, 5, trunk, 0.13)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(7):
		var angle := TAU * i / 7.0
		var d := Vector3(cos(angle), 0, sin(angle))
		var side := Vector3(-d.z, 0, d.x) * 0.55
		var base := Vector3(0, 5, 0)
		var mid := base + d * 1.5 + Vector3.UP * 0.55
		var tip := base + d * 3.4 + Vector3.DOWN * 0.9
		G.triangle(st, base, mid - side, mid + side)
		G.triangle(st, mid - side, tip, mid + side)
	G.finish(self, st, leaves, "Fronds")
