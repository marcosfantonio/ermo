@tool
class_name Landscape
extends RefCounted
## One shared world-space field for mesh, props, paths and collision.
## No per-chunk noise offsets: borders sample the exact same positions.
var settings: WorldSettings

func _init(profile: WorldSettings) -> void:
	settings = profile

func coordinate_seed(cell: Vector2i, salt: int = 0) -> int:
	return hash("%d:%d:%d:%d" % [settings.world_seed, cell.x, cell.y, salt])

func road_x(z: float) -> float:
	return 10.0 * sin(z / 37.0) + 14.0 * sin(z / 93.0)

func road_distance(x: float, z: float) -> float:
	var slope := 10.0 / 37.0 * cos(z / 37.0) + 14.0 / 93.0 * cos(z / 93.0)
	return absf(x - road_x(z)) / sqrt(1.0 + slope * slope)

func base_height(x: float, z: float) -> float:
	var phase := float(settings.world_seed % 997 - 225) * 0.001
	return (4.5 * sin((z + 4.0) / 21.0 + phase)
		+ 4.0 * sin(x / 31.0 + z / 68.0)
		+ 2.8 * cos(x / 61.0 - z / 88.0)
		+ 1.6 * cos(x / 19.0 - z / 27.0))

func sites_in_cell(cell: Vector2i) -> Array[Dictionary]:
	var rng := RandomNumberGenerator.new()
	rng.seed = coordinate_seed(Vector2i(0, cell.y))
	var sites: Array[Dictionary] = []
	# A repeated line of sparse homesteads, not uniform white noise.
	var z := float(cell.y * 96) - 12.0
	var side := 1.0 if posmod(cell.y, 2) == 0 else -1.0
	var main_x := road_x(z) + side * (19.0 + rng.randf_range(0.0, 8.0))
	if cell.x == floori(main_x / 128.0):
		sites.append({"position": Vector2(main_x, z), "rotation": -side * 0.25,
			"variant": 0 if posmod(cell.y, 4) == 0 else posmod(cell.y, 3), "echo": posmod(cell.y, 4) == 0})
	# Most off-road cells stay empty. Rare pairs sit on a shared contour.
	rng.seed = coordinate_seed(cell, 12)
	if rng.randf() < 0.24 and abs(cell.x) > 0:
		var x := float(cell.x * 128) + rng.randf_range(28.0, 100.0)
		var far_z := float(cell.y * 96) + rng.randf_range(20.0, 64.0)
		if road_distance(x, far_z) > 30.0:
			sites.append({"position": Vector2(x, far_z), "rotation": rng.randf_range(-0.6, 0.6), "variant": 1, "echo": false})
			if rng.randf() < 0.25:
				sites.append({"position": Vector2(x + 21.0, far_z + 8.0), "rotation": 0.1, "variant": 2, "echo": false})
	return sites

func nearby_sites(x: float, z: float) -> Array[Dictionary]:
	var cell := Vector2i(floori(x / 128.0), floori(z / 96.0))
	var result: Array[Dictionary] = []
	for dz in range(-1, 2):
		for dx in range(-1, 2):
			result.append_array(sites_in_cell(cell + Vector2i(dx, dz)))
	return result

func height_at(x: float, z: float, sites: Array[Dictionary] = []) -> float:
	var h := base_height(x, z)
	for site in sites:
		var p: Vector2 = site.position
		var distance := Vector2(x, z).distance_to(p)
		if distance < 28.0:
			var blend := 1.0 - smoothstep(10.0, 28.0, distance)
			h = lerpf(h, base_height(p.x, p.y) + 1.0, blend)
	return h

func surface_height(x: float, z: float) -> float:
	return height_at(x, z, nearby_sites(x, z))

func mesh_height(x: float, z: float, sites: Array[Dictionary] = []) -> float:
	var step := float(settings.chunk_size) / settings.subdivisions
	var gx := floorf(x / step) * step
	var gz := floorf(z / step) * step
	var fx := (x - gx) / step
	var fz := (z - gz) / step
	if sites.is_empty():
		sites = nearby_sites(x, z)
	var a := height_at(gx, gz, sites)
	var b := height_at(gx + step, gz, sites)
	var c := height_at(gx, gz + step, sites)
	var d := height_at(gx + step, gz + step, sites)
	if fx + fz <= 1.0:
		return a + (b - a) * fx + (c - a) * fz
	return d + (c - d) * (1.0 - fx) + (b - d) * (1.0 - fz)
