@tool
class_name UniverseProfile
extends Resource
## Only register environments already defined in CONCEPT.md.
@export var id: StringName = &"sitios"
@export var title := "Sítios Infinitos"
@export_file("*.tscn") var scene_path := "res://scenes/exploration.tscn"
@export var base_seed := 27101998
@export_group("Variação por visita")
@export_range(1, 8) var configuration_count := 2
@export_enum("Rodízio determinístico", "Sorteio determinístico") var spawn_rule := 0
@export_group("Atmosfera")
@export var environment: Environment
@export_range(60.0, 90.0) var field_of_view := 73.0
@export_range(0.0, 1.0) var sound_volume := 0.65
@export_group("Óptica / memória visual")
@export_range(0.0, 0.2) var distortion := 0.045
@export_range(0.0, 1.0) var grain := 0.045
@export_range(0.0, 1.0) var vignette := 0.16
@export_range(0.0, 2.0) var chromatic := 0.35
@export_range(0.0, 1.0) var dither := 0.28
