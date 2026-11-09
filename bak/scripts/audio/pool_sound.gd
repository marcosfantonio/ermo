extends AudioStreamPlayer3D
## Original filtered-noise water loop, shared by every pool (no external assets).
static var water_loop: AudioStreamWAV

func _ready() -> void:
	if water_loop == null:
		var rng := RandomNumberGenerator.new()
		rng.seed = 1998
		var samples := PackedByteArray()
		var count := 22050 * 3
		samples.resize(count * 2)
		var low := 0.0
		for i in range(count):
			low = lerpf(low, rng.randf_range(-1, 1), 0.13)
			var envelope := 0.7 + 0.15 * sin(TAU * i / count * 5)
			# Short seam fade avoids clicks without another audio dependency.
			var seam := minf(1, minf(float(i), float(count - 1 - i)) / 200)
			samples.encode_s16(i * 2, int(low * envelope * seam * 21000))
		water_loop = AudioStreamWAV.new()
		water_loop.format = AudioStreamWAV.FORMAT_16_BITS
		water_loop.mix_rate = 22050
		water_loop.data = samples
		water_loop.loop_mode = AudioStreamWAV.LOOP_FORWARD
		water_loop.loop_end = count
	stream = water_loop
	play()
