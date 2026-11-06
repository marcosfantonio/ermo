shader_type spatial;
render_mode diffuse_lambert, specular_disabled;

uniform sampler2D grain : source_color, filter_nearest, repeat_enable;
uniform vec4 tint : source_color = vec4(1.0);
uniform float texture_scale = 1.0;
uniform float wobble : hint_range(0.0, 1.0) = 0.18;

void vertex() {
    // Subpixel, view-dependent vertex snapping; never affects collision.
    vec4 clip = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
    vec2 screen = clip.xy / clip.w;
    vec2 snapped = floor(screen * vec2(320.0, 180.0) + 0.5) / vec2(320.0, 180.0);
    clip.xy = mix(screen, snapped, wobble) * clip.w;
    POSITION = clip;
}

void fragment() {
    ALBEDO = texture(grain, UV * texture_scale).rgb * tint.rgb;
    ROUGHNESS = 1.0;
}
