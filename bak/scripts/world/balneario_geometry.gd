@tool
class_name BalnearioGeometry
extends RefCounted
## Small shared mesh helpers; architecture remains composed of reusable scenes.

static func material(color: Color) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = 0.95
	return mat

static func mesh_node(parent: Node3D, mesh: Mesh, mat: Material, label: String, solid: bool = false) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	node.name = label
	node.mesh = mesh
	node.material_override = mat
	parent.add_child(node)
	if solid:
		var body := StaticBody3D.new()
		body.set_meta("surface", "cement")
		var shape := CollisionShape3D.new()
		shape.shape = mesh.create_trimesh_shape()
		body.add_child(shape)
		node.add_child(body)
	return node

static func box(parent: Node3D, pos: Vector3, size: Vector3, mat: Material, solid: bool = false) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var node := mesh_node(parent, mesh, mat, "Block", solid)
	node.position = pos
	return node

static func cylinder(parent: Node3D, pos: Vector3, radius: float, height: float, mat: Material, top: float = -1.0) -> MeshInstance3D:
	var mesh := CylinderMesh.new()
	mesh.bottom_radius = radius
	mesh.top_radius = radius if top < 0 else top
	mesh.height = height
	mesh.radial_segments = 8
	mesh.rings = 1
	var node := mesh_node(parent, mesh, mat, "Cylinder")
	node.position = pos
	return node

static func triangle(st: SurfaceTool, a: Vector3, b: Vector3, c: Vector3) -> void:
	# Godot treats clockwise triangles as front faces.
	for v in [a, c, b]:
		st.set_uv(Vector2(v.x, v.z))
		st.add_vertex(v)

static func quad(st: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, d: Vector3) -> void:
	triangle(st, a, b, c)
	triangle(st, a, c, d)

static func finish(parent: Node3D, st: SurfaceTool, mat: Material, label: String, solid: bool = false) -> void:
	st.generate_normals()
	mesh_node(parent, st.commit(), mat, label, solid)

static func ramp_prism(low: Vector3, high: Vector3, width: float, bottom_y: float) -> ArrayMesh:
	# Closed volume: top, underside, side cheeks and both end caps.
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var a := low + Vector3.LEFT * width * 0.5
	var b := high + Vector3.LEFT * width * 0.5
	var c := high + Vector3.RIGHT * width * 0.5
	var d := low + Vector3.RIGHT * width * 0.5
	if high.z < low.z:
		var swap := a
		a = d
		d = swap
		swap = b
		b = c
		c = swap
	var aa := Vector3(a.x, bottom_y, a.z)
	var bb := Vector3(b.x, bottom_y, b.z)
	var cc := Vector3(c.x, bottom_y, c.z)
	var dd := Vector3(d.x, bottom_y, d.z)
	quad(st, a, b, c, d)
	quad(st, dd, cc, bb, aa)
	quad(st, aa, bb, b, a)
	quad(st, bb, cc, c, b)
	quad(st, cc, dd, d, c)
	quad(st, dd, aa, a, d)
	st.generate_normals()
	# append_from() must receive indexed faces when joining an indexed box batch.
	# Otherwise the added vertices can lose their faces, exposing the ramp sides.
	st.index()
	return st.commit()
