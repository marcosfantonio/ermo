shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
uniform vec4 tile_color : source_color = vec4(0.035, 0.19, 0.46, 1.0);
uniform bool underwater = false;
varying vec3 tile_position;
void vertex() { tile_position = VERTEX; }
void fragment() {
    vec2 coordinates = underwater ? tile_position.xz : UV;
    vec2 tile = fract(coordinates * 6.0);
    float grout = step(0.06, tile.x) * step(0.06, tile.y);
    ALBEDO = mix(vec3(0.57, 0.66, 0.65), tile_color.rgb, grout);
    if (underwater) {
        vec2 p = coordinates + vec2(TIME * 0.12, TIME * 0.08);
        float ridge = abs(sin(p.x * 3.3 + sin(p.y * 2.6 + TIME * 0.5))
            + sin(p.y * 3.7 - sin(p.x * 2.2 - TIME * 0.4)));
        ALBEDO += vec3(0.22, 0.35, 0.29) * (1.0 - smoothstep(0.03, 0.20, ridge));
    }
    ROUGHNESS = 0.9;
}
