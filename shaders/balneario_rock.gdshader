shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
varying vec3 stone_position;
varying vec3 stone_normal;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void vertex() { stone_position = VERTEX; stone_normal = NORMAL; }
void fragment() {
    // Irregular beige fieldstone with recessed dark mortar, like photo 1's left wall.
    vec2 plane = abs(stone_normal.x) > 0.5 ? stone_position.zy : stone_position.xy;
    if (abs(stone_normal.y) > 0.5) { plane = stone_position.xz; }
    vec2 p = floor(plane * 48.0) / 48.0 * vec2(2.8, 3.3);
    vec2 cell = floor(p);
    float first = 10.0;
    float second = 10.0;
    vec2 chosen = cell;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 c = cell + vec2(float(x), float(y));
            vec2 center = c + 0.15 + 0.7 * vec2(hash(c), hash(c + 37.0));
            float d = length(p - center);
            if (d < first) { second = first; first = d; chosen = c; }
            else { second = min(second, d); }
        }
    }
    float edge = second - first;
    float grain = hash(floor(plane * 45.0));
    vec3 stone = mix(vec3(0.36, 0.31, 0.23), vec3(0.78, 0.67, 0.46), hash(chosen + 19.0));
    stone *= 0.83 + grain * 0.22;
    stone *= 0.7 + 0.3 * smoothstep(0.03, 0.13, edge);
    ALBEDO = mix(vec3(0.13, 0.12, 0.095), stone, smoothstep(0.025, 0.055, edge));
    ROUGHNESS = 1.0;
}
