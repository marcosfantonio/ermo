shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
varying vec3 local_position;
varying vec3 local_normal;
void vertex() { local_position = VERTEX; local_normal = NORMAL; }
void fragment() {
    // Beige/cream diamond tiles from the raised planters in the references.
    float horizontal = abs(local_normal.x) > 0.5 ? local_position.z : local_position.x;
    vec2 p = vec2(horizontal, local_position.y) * 1.7;
    vec2 diamond = vec2(p.x + p.y, p.x - p.y);
    vec2 f = fract(diamond);
    float grout = step(0.035, f.x) * step(0.035, f.y);
    float checkered = mod(floor(diamond.x) + floor(diamond.y), 2.0);
    vec3 tile = mix(vec3(0.79, 0.70, 0.51), vec3(0.51, 0.35, 0.22), checkered);
    ALBEDO = mix(vec3(0.83, 0.78, 0.64), tile, grout);
    ROUGHNESS = 1.0;
}
