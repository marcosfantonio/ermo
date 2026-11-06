@tool
extends Node3D
## Procedural props expose their real meshes in the editor as well as at runtime.
var _rebuild_pending := false

@export_tool_button("Atualizar geometria", "Reload") var rebuild_geometry = request_geometry_rebuild

func _ready() -> void:
	_rebuild_geometry()

func request_geometry_rebuild() -> void:
	if not Engine.is_editor_hint() or not is_inside_tree() or _rebuild_pending:
		return
	_rebuild_pending = true
	_rebuild_geometry.call_deferred()

func _rebuild_geometry() -> void:
	_rebuild_pending = false
	for child in get_children():
		if child.has_meta("generated_geometry"):
			remove_child(child)
			child.free()
	var authored := get_children()
	_build_geometry()
	for child in get_children():
		if not authored.has(child):
			child.set_meta("generated_geometry", true)
			if Engine.is_editor_hint():
				own_geometry(child, preview_owner(self))

static func preview_owner(fallback: Node) -> Node:
	# SceneTreeEditor exposes nodes owned by the edited root, not arbitrary parents.
	var edited_root := fallback.get_tree().edited_scene_root
	if edited_root != null and (edited_root == fallback or edited_root.is_ancestor_of(fallback)):
		return edited_root
	return fallback

static func own_geometry(node: Node, scene_root: Node) -> void:
	node.owner = scene_root
	for child in node.get_children():
		own_geometry(child, scene_root)

func _build_geometry() -> void:
	pass
