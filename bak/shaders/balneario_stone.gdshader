shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
varying vec3 world_position;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
// Pool-local coordinates keep the pattern unchanged during floating-origin shifts.
void vertex() { world_position = VERTEX; }
void fragment() {
    vec2 p = floor(world_position.xz * 24.0) / 24.0 * 1.7;
    vec2 cell = floor(p);
    float first = 10.0;
    float second = 10.0;
    vec2 chosen = cell;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 c = cell + vec2(float(x), float(y));
            vec2 center = c + vec2(hash(c), hash(c + 37.0));
            float d = length(p - center);
            if (d < first) { second = first; first = d; chosen = c; }
            else { second = min(second, d); }
        }
    }
    vec3 stone = mix(vec3(0.42, 0.32, 0.24), vec3(0.77, 0.48, 0.27), hash(chosen + 13.0));
    ALBEDO = mix(vec3(0.58, 0.50, 0.39), stone, step(0.028, second - first));
    ROUGHNESS = 1.0;
}
