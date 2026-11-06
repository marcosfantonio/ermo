@tool
extends Node
## Persists visits, not coordinates. A seed + visit index exactly replays a layout.
signal transition_failed(error: Error)
const SAVE_PATH := "user://ermo_visits_v1.json"
var progress: Dictionary = {}
var current_visit: Dictionary = {}
var pending_replay: Dictionary = {}
var transitioning := false
var persistence_enabled := true

func _ready() -> void:
	if not Engine.is_editor_hint():
		load_progress()

static func configuration(profile: UniverseProfile, visit: int, session_seed: int) -> Dictionary:
	var rng := RandomNumberGenerator.new()
	var seed_value := int(hash("%s:%d:%d:%d" % [profile.id, profile.base_seed, session_seed, visit]))
	rng.seed = seed_value
	return {"universe": String(profile.id), "visit": visit, "session_seed": session_seed,
		"seed": seed_value, "variant": rng.randi_range(0, maxi(profile.configuration_count - 1, 0))}

func begin_visit(profile: UniverseProfile) -> Dictionary:
	var state: Dictionary = progress.get(String(profile.id), {})
	if not pending_replay.is_empty() and pending_replay.get("universe") == String(profile.id):
		current_visit = configuration(profile, int(pending_replay.visit), int(pending_replay.session_seed))
		pending_replay.clear()
	else:
		var session_seed := int(state.get("session_seed", 0))
		if state.is_empty():
			var rng := RandomNumberGenerator.new()
			rng.randomize()
			session_seed = rng.randi()
		var visit := int(state.get("visits", 0))
		current_visit = configuration(profile, visit, session_seed)
		state["session_seed"] = session_seed
		state["visits"] = visit + 1
		progress[String(profile.id)] = state
		save_progress()
	transitioning = false
	return current_visit.duplicate(true)

func replay(universe: StringName, visit: int, session_seed: int) -> void:
	pending_replay = {"universe": String(universe), "visit": visit, "session_seed": session_seed}

func remember(key: String, value: Variant) -> void:
	var id := String(current_visit.get("universe", ""))
	if id.is_empty():
		return
	var state: Dictionary = progress.get(id, {})
	var flags: Dictionary = state.get("flags", {})
	flags[key] = value
	state["flags"] = flags
	progress[id] = state
	save_progress()

func recall(key: String, fallback: Variant = false) -> Variant:
	var state: Dictionary = progress.get(String(current_visit.get("universe", "")), {})
	return state.get("flags", {}).get(key, fallback)

func travel(profile: UniverseProfile) -> Error:
	if transitioning:
		return ERR_BUSY
	if profile == null or not ResourceLoader.exists(profile.scene_path):
		return ERR_FILE_NOT_FOUND
	transitioning = true
	_change_scene.call_deferred(profile.scene_path)
	return OK

func _change_scene(path: String) -> void:
	get_tree().paused = false
	var error := get_tree().change_scene_to_file(path)
	if error != OK:
		transitioning = false
		transition_failed.emit(error)
		push_error("Passagem não carregada: %s" % path)

func save_progress(path: String = SAVE_PATH) -> Error:
	if not persistence_enabled:
		return OK
	# Write to a temporary file before replacing the last good save.
	var file := FileAccess.open(path + ".tmp", FileAccess.WRITE)
	if file == null:
		return FileAccess.get_open_error()
	file.store_string(JSON.stringify({"version": 1, "universes": progress}))
	file.close()
	return DirAccess.rename_absolute(path + ".tmp", path)

func load_progress(path: String = SAVE_PATH) -> void:
	if not persistence_enabled or not FileAccess.file_exists(path):
		return
	var data: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
	if data is Dictionary and data.get("version") == 1 and data.get("universes") is Dictionary:
		# Ignore malformed entries rather than using them as runtime configuration.
		for id: String in data.universes:
			var state: Variant = data.universes[id]
			if state is Dictionary and state.get("visits") is float and state.get("session_seed") is float:
				if state.visits >= 0 and state.visits < 1000000000:
					if not state.get("flags", {}) is Dictionary:
						state["flags"] = {}
					progress[id] = state
