@tool
extends "res://scripts/world/editor_geometry.gd"
@export_enum("Cascata", "Espiral") var style: int = 0:
	set(value):
		style = value
		request_geometry_rebuild()
const G = preload("res://scripts/world/balneario_geometry.gd")

func _build_geometry() -> void:
	var blue := G.material(Color(0.04, 0.18, 0.52))
	var yellow := G.material(Color(0.88, 0.64, 0.08))
	yellow.cull_mode = BaseMaterial3D.CULL_DISABLED
	var steel := G.material(Color(0.38, 0.44, 0.40))
	if style == 1:
		_spiral(yellow, steel)
		return
	var water := ShaderMaterial.new()
	water.shader = preload("res://shaders/pool_water.gdshader")
	water.set_shader_parameter("flowing", true)
	G.mesh_node(self, G.ramp_prism(Vector3(0, 0.01, 7), Vector3(0, 4.77, -3), 5.3, -0.5), blue, "CascadeFoundation", true)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	G.quad(st, Vector3(-2.4, 0.05, 7), Vector3(2.4, 0.05, 7), Vector3(2.4, 4.8, -3), Vector3(-2.4, 4.8, -3))
	G.finish(self, st, water, "Cascade", true)
	for side in [-1, 1]:
		var wall := G.box(self, Vector3(side * 2.65, 2.5, 2), Vector3(0.35, 0.65, 11.2), blue, true)
		wall.rotation.x = 0.443
	G.box(self, Vector3(0, 2.4, -3.5), Vector3(5.8, 4.8, 1), blue, true)
	for i in range(7):
		G.box(self, Vector3(-2.6 + i * 0.86, 5.1, -3.5), Vector3(0.45, 0.6, 1), blue)
	_stair_flight("CascadeStairs", Vector3(-3.7, 0, 3), Vector3(-3.7, 4.8, -3), 1.6, yellow, steel)

func _spiral(yellow: Material, steel: Material) -> void:
	G.cylinder(self, Vector3(0, 3.5, 0), 0.16, 7, steel)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	# Closed U-section: real underside, outer cheeks, thick rims and end caps.
	for i in range(64):
		var a := _section(float(i) / 64.0)
		var b := _section(float(i + 1) / 64.0)
		for j in range(8):
			var next := (j + 1) % 8
			G.quad(st, a[j], b[j], b[next], a[next])
		if i == 0:
			_cap(st, a)
		if i == 63:
			_cap(st, b)
	G.finish(self, st, yellow, "YellowSpiral")
	for i in range(0, 64, 8):
		var section := _section(float(i) / 64.0)
		var p := (section[4] + section[5]) * 0.5
		G.cylinder(self, Vector3(p.x, p.y * 0.5, p.z), 0.085, maxf(p.y, 0.1), steel)
	G.box(self, Vector3(0, 6.7, -2.7), Vector3(3, 0.2, 2), steel)
	for x in [-1.3, 1.3]:
		G.cylinder(self, Vector3(x, 3.4, -2.7), 0.07, 6.8, steel)
		G.cylinder(self, Vector3(x, 7.1, -2.7), 0.04, 0.8, steel)
	_stair_flight("SpiralStairs", Vector3(0, 0, -11.2), Vector3(0, 6.7, -2.7), 1.4, steel, steel)

func _section(t: float) -> Array[Vector3]:
	var angle := t * TAU * 2 - PI / 2
	var d := Vector3(cos(angle), 0, sin(angle))
	var center := d * 2.7 + Vector3.UP * (6.7 - t * 6.4)
	return [center - d * 0.65, center + d * 0.65,
		center + d * 0.65 + Vector3.UP * 0.5, center + d * 0.79 + Vector3.UP * 0.5,
		center + d * 0.79 + Vector3.DOWN * 0.14, center - d * 0.79 + Vector3.DOWN * 0.14,
		center - d * 0.79 + Vector3.UP * 0.5, center - d * 0.65 + Vector3.UP * 0.5]

func _cap(st: SurfaceTool, p: Array[Vector3]) -> void:
	G.quad(st, p[0], p[1], p[4], p[5])
	G.quad(st, p[1], p[2], p[3], p[4])
	G.quad(st, p[5], p[6], p[7], p[0])

func _stair_flight(label: String, low: Vector3, high: Vector3, width: float, mat: Material, rail_mat: Material) -> void:
	var count := ceili((high.y - low.y) / 0.20)
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(count):
		var t := float(i + 1) / count
		var p := low.lerp(high, t)
		var height := p.y - low.y + 0.2
		var box := BoxMesh.new()
		box.size = Vector3(width, height, absf(high.z - low.z) / count + 0.015)
		p.y = low.y - 0.2 + height * 0.5
		p.z = lerpf(low.z, high.z, (i + 0.5) / count)
		st.append_from(box, 0, Transform3D(Basis.IDENTITY, p))
	G.finish(self, st, mat, label)
	var envelope := G.mesh_node(self, G.ramp_prism(low, high, width, low.y - 0.2), mat, label + "Collision", true)
	envelope.visible = false
	for side in [-1, 1]:
		var start := low + Vector3(side * (width * 0.5 + 0.06), 0.9, 0)
		var end := high + Vector3(side * (width * 0.5 + 0.06), 0.9, 0)
		var rail := G.box(self, (start + end) * 0.5, Vector3(0.07, 0.07, start.distance_to(end)), rail_mat)
		rail.rotation.x = -atan2(end.y - start.y, end.z - start.z)
		for i in range(5):
			G.cylinder(self, low.lerp(high, i / 4.0) + Vector3(side * (width * 0.5 + 0.06), 0.45, 0), 0.035, 0.9, rail_mat)
