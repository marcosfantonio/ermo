extends CharacterBody3D
signal footstep(surface: String, strength: float)
@export var settings: WorldSettings
@export var world: Node3D
@onready var head: Node3D = $Head
@onready var camera: Camera3D = $Head/Camera3D
var pitch := -0.08
var stride := 0.0
var travelled := 0.0
var landing := 0.0
var last_grounded := true
var motion_enabled := true
var last_position := Vector3.ZERO
var gait_weight := 0.0

func _ready() -> void:
	for action in {"forward": [KEY_W, KEY_UP], "backward": [KEY_S, KEY_DOWN], "left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT], "run": [KEY_SHIFT], "jump": [KEY_SPACE]}:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		if not InputMap.action_get_events(action).is_empty():
			continue
		for key in {"forward": [KEY_W, KEY_UP], "backward": [KEY_S, KEY_DOWN], "left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT], "run": [KEY_SHIFT], "jump": [KEY_SPACE]}[action]:
			var event := InputEventKey.new()
			event.physical_keycode = key
			InputMap.action_add_event(action, event)
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * settings.mouse_sensitivity)
		pitch = clampf(pitch - event.relative.y * settings.mouse_sensitivity, -1.35, 1.35)
	if event is InputEventKey and event.pressed and not event.echo and event.keycode == KEY_F2:
		motion_enabled = not motion_enabled

func _physics_process(delta: float) -> void:
	# Pausing disables this node, independently of the display's mouse mode.
	var input := Input.get_vector("left", "right", "forward", "backward")
	var direction := global_basis * Vector3(input.x, 0, input.y)
	var speed := settings.running_speed if Input.is_action_pressed("run") else settings.walking_speed
	var blend := 1.0 - exp(-delta * (9.0 if is_on_floor() else 2.0))
	velocity.x = lerpf(velocity.x, direction.x * speed, blend)
	velocity.z = lerpf(velocity.z, direction.z * speed, blend)
	if not is_on_floor():
		velocity.y -= 20.0 * delta
	elif Input.is_action_just_pressed("jump"):
		velocity.y = 4.2
	var falling_speed := velocity.y
	last_position = position
	move_and_slide()
	var distance := Vector2(position.x - last_position.x, position.z - last_position.z).length()
	if is_on_floor():
		stride += distance * PI / 1.65
		travelled += distance
		if travelled > 1.65:
			travelled = fmod(travelled, 1.65)
			footstep.emit(_surface(), 0.8 if speed > settings.walking_speed else 0.55)
		if not last_grounded:
			landing = -minf(absf(falling_speed) * 0.009, 0.08)
			footstep.emit(_surface(), minf(absf(falling_speed) / 9.0, 1.0))
	last_grounded = is_on_floor()
	var amount := settings.camera_motion if motion_enabled else 0.0
	var moving := clampf(Vector2(velocity.x, velocity.z).length() / settings.walking_speed, 0, 1.5)
	if not is_on_floor():
		moving = 0.0
	gait_weight = lerpf(gait_weight, moving, 1.0 - exp(-delta * 8.0))
	landing = lerpf(landing, 0, 1.0 - exp(-delta * 9.0))
	# Distance-driven, layered gait: no random jitter while standing still.
	var step_pulse := cos(stride * 2.0)
	var offset := Vector3(
		sin(stride) * 0.032 * gait_weight,
		-step_pulse * 0.045 * gait_weight + landing,
		sin(stride * 2.0) * 0.012 * gait_weight)
	head.position = head.position.lerp(Vector3(0, 1.62, 0) + offset * amount, 1.0 - exp(-delta * 12.0))
	var nod := (sin(stride * 2.0) * 0.009 + sin(stride * 4.0 + 0.4) * 0.002) * gait_weight * amount
	var yaw := sin(stride) * 0.005 * gait_weight * amount
	var roll := (-input.x * 0.012 + sin(stride + 0.35) * 0.009) * gait_weight * amount
	var target := Vector3(pitch + nod + landing * amount * 0.12, yaw, roll)
	camera.rotation = camera.rotation.lerp(target, 1.0 - exp(-delta * 15.0))

func _surface() -> String:
	if world != null and world.has_method("surface_at"):
		# House floors retain cement footsteps; terrain is classified by authored roads.
		var floor_query := PhysicsRayQueryParameters3D.create(global_position + Vector3.UP * 0.3, global_position - Vector3.UP * 0.3)
		floor_query.exclude = [get_rid()]
		var floor_hit := get_world_3d().direct_space_state.intersect_ray(floor_query)
		if not floor_hit.is_empty() and not floor_hit.collider.has_meta("surface"):
			return "cement"
		return world.surface_at(position)
	var query := PhysicsRayQueryParameters3D.create(global_position + Vector3.UP * 0.3, global_position - Vector3.UP * 0.3)
	query.exclude = [get_rid()]
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	if not hit.is_empty() and hit.collider.has_meta("surface"):
		return str(hit.collider.get_meta("surface"))
	if not hit.is_empty() and not hit.collider.has_meta("surface"):
		return "cement"
	var absolute: Vector3 = world.absolute_position(position)
	if world.field.road_distance(absolute.x, absolute.z) < 1.2:
		return "dirt"
	return "grass"
