@tool
class_name BalnearioLayout
extends RefCounted
## Absolute cell coordinates select terraces; all chunk edges remain at zero.
static func height_for(cell: Vector2i) -> float:
	return [1.6, 3.2, 0.0, 1.6][posmod(cell.x + cell.y * 3, 4)]

static func courtyard_half(size: float) -> float:
	return size * 0.5 - 9.0

static func main_pool(cell: Vector2i, rng: RandomNumberGenerator) -> Dictionary:
	var variant := posmod(cell.x * 3 + cell.y, 4)
	var radius := Vector2(rng.randf_range(10, 12), rng.randf_range(12, 14))
	var shape := 0
	match variant:
		1:
			shape = 1
			radius = Vector2(12.5, 14)
		2:
			shape = 1
			radius = Vector2(12, 12)
		3:
			radius = Vector2(8, 10)
	return {"radius": radius, "shape": shape, "variant": variant}

static func lower_pools(cell: Vector2i, size: float) -> Array[Dictionary]:
	# Recessed leisure pockets occupy both end plazas and a former side corridor.
	var flip := posmod(cell.x + cell.y, 2) == 1
	var candidates: Array[Dictionary] = [
		{"center": Vector2(14, size - 4.5 if flip else 4.5), "radius": Vector2(3.0, 2.6), "shape": 0, "deck": Vector2(6.5, 4.3)},
		{"center": Vector2(size - 14, 4.5 if flip else size - 4.5), "radius": Vector2(4.4, 2.5), "shape": 1, "deck": Vector2(6.5, 4.3)},
		{"center": Vector2(size - 2.5 if flip else 2.5, size * 0.5), "radius": Vector2(1.2, 4.2), "shape": 1, "deck": Vector2(2.4, 7.5), "narrow": true}
	]
	var result: Array[Dictionary] = []
	for plan in candidates:
		if plaza_is_clear(plan, cell, size):
			result.append(plan)
	return result

static func plaza_is_clear(plan: Dictionary, cell: Vector2i, size: float) -> bool:
	var center: Vector2 = plan.center
	var deck: Vector2 = plan.deck
	var footprint := Rect2(center - deck, deck * 2)
	# Include neighbour reservations before creating a basin or cutting its floor.
	for z in range(-1, 2):
		for x in range(-1, 2):
			var offset := Vector2(x, z) * size
			for access in access_footprints(height_for(cell + Vector2i(x, z)), size):
				access.position += offset
				if footprint.grow(1.5).intersects(access):
					return false
	return true

static func access_footprints(height: float, size: float) -> Array[Rect2]:
	# Rectangles include handrails. All access flights occupy exclusive lanes.
	var result: Array[Rect2] = []
	var mid := size * 0.5
	var c := courtyard_half(size)
	for end: int in [-1, 1]:
		if height > 0:
			var z := mid + end * (c + 0.3)
			var toe := mid + end * (mid - 3.5)
			result.append(Rect2(Vector2(mid - 3.12, minf(z, toe)), Vector2(6.24, absf(z - toe))))
			var stair_toe := z + end * ceili((height + 0.1) / 0.18) * 0.28
			result.append(Rect2(Vector2(mid + 5.28, minf(z, stair_toe)), Vector2(4.44, absf(z - stair_toe))))
		for side: int in [-1, 1]:
			var z := mid + end * 14.3
			var toe := mid + end * 19.8
			result.append(Rect2(Vector2(mid + side * 22.5 - 1.97, minf(z, toe)), Vector2(3.94, absf(z - toe))))
			var stair_toe := z + end * ceili(1.7 / 0.18) * 0.28
			result.append(Rect2(Vector2(mid + side * 17.1 - 1.22, minf(z, stair_toe)), Vector2(2.44, absf(z - stair_toe))))
	return result

static func radio_roll(cell: Vector2i, seed: int, chunk_size: float, min_distance: float = 256.0, denominator: int = 14) -> bool:
	var center := Vector2(cell) * chunk_size + Vector2.ONE * chunk_size * 0.5
	if center.length() < min_distance:
		return false
	var rng := RandomNumberGenerator.new()
	rng.seed = hash("radio:%d:%d:%d" % [seed, cell.x, cell.y])
	return rng.randi_range(0, denominator - 1) == 0

static func radio_cell_count(seed: int, chunk_size: float, radius: int = 80) -> int:
	var count := 0
	for z in range(-radius, radius + 1):
		for x in range(-radius, radius + 1):
			if radio_roll(Vector2i(x, z), seed, chunk_size):
				count += 1
	return count

static func radio_position(cell: Vector2i, size: float) -> Vector3:
	var side := -1.0 if posmod(cell.x + cell.y, 2) == 0 else 1.0
	# Quiet ground-level strip, clear of the main basin, recessed annexes and ramps.
	return Vector3(size * 0.5 + side * 23.0, 0, size * 0.5)
