shader_type spatial;
render_mode diffuse_lambert, specular_disabled;
uniform sampler2D grass : source_color, filter_nearest, repeat_enable;
varying vec3 ground_color;

void vertex() {
    ground_color = COLOR.rgb;
}
void fragment() {
    vec3 detail = texture(grass, UV).rgb;
    float patches = sin(UV.x * 0.7 + sin(UV.y * 0.4)) * sin(UV.y * 0.5 + cos(UV.x * 0.6));
    ALBEDO = ground_color * mix(vec3(1.0), detail * 1.65, 0.4) * (1.0 + patches * 0.07);
    ROUGHNESS = 1.0;
}
