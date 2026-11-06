class_name WorldSettings
extends Resource
## All distances are metres. The horizon fades before the streaming boundary.
@export var world_seed: int = 27101998
@export_range(32, 128, 16) var chunk_size: int = 64
@export_range(8, 32, 1) var subdivisions: int = 16
@export_range(3, 7, 1) var view_radius: int = 5
@export var walking_speed: float = 4.0
@export var running_speed: float = 6.5
@export_range(0.0, 1.0) var camera_motion: float = 0.65
@export_range(0.0005, 0.005) var mouse_sensitivity: float = 0.002
@export_range(0.0, 1.0) var sound_volume: float = 0.65
