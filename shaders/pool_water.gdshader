shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_disabled;
uniform bool flowing = false;
uniform vec2 pool_radius = vec2(11.0, 14.0);
uniform bool rectangular = false;
varying vec3 tangent_view;
varying vec3 bitangent_view;

void vertex() {
    tangent_view = normalize((MODELVIEW_MATRIX * vec4(1.0, 0.0, 0.0, 0.0)).xyz);
    bitangent_view = normalize((MODELVIEW_MATRIX * vec4(0.0, 0.0, 1.0, 0.0)).xyz);
}

void fragment() {
    vec2 p = UV;
    vec2 travel = flowing ? vec2(0.04, -TIME * 2.8) : vec2(TIME * 0.12, TIME * 0.08);
    vec2 q = p + travel;
    float broad = sin(q.x * 1.7 + q.y * 0.6 + TIME * 0.65);
    float ripple = sin(q.y * 3.1 - q.x * 0.9 - TIME * 1.1);
    float detail = sin(q.x * 6.2 + sin(q.y * 4.7 + TIME) * 1.3);
    float dx = broad * 0.07 + detail * 0.025;
    float dz = ripple * 0.065 + detail * 0.02;
    NORMAL = normalize(NORMAL + tangent_view * dx + bitangent_view * dz);
    float fresnel = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 3.0);
    // Soft moving caustic-like ridges rather than the previous white pixel stipple.
    float ridge = abs(sin(q.x * 3.3 + sin(q.y * 2.6 + TIME * 0.5))
        + sin(q.y * 3.7 - sin(q.x * 2.2 - TIME * 0.4)));
    float caustic = (1.0 - smoothstep(0.02, 0.16, ridge)) * 0.18;
    vec2 boundary = abs(p / pool_radius);
    float edge = rectangular ? max(boundary.x, boundary.y) : length(boundary);
    float foam = smoothstep(0.965, 1.0, edge) * (0.4 + 0.2 * ripple);
    if (flowing) {
        float streak = sin(q.x * 9.0 + sin(q.y * 4.0 + q.x * 1.9)) * 0.6
            + sin(q.x * 15.3 + q.y * 2.7) * 0.4;
        foam = smoothstep(0.4, 0.85, streak) * 0.5;
    }
    vec3 water = mix(vec3(0.035, 0.43, 0.51), vec3(0.23, 0.54, 0.68), fresnel * 0.65);
    ALBEDO = mix(water + vec3(0.26, 0.48, 0.41) * caustic, vec3(0.72, 0.86, 0.80), foam);
    ROUGHNESS = mix(0.25, 0.42, foam);
    SPECULAR = 0.65;
    ALPHA = flowing ? 0.94 : clamp(0.66 + fresnel * 0.27 + foam * 0.15, 0.0, 0.96);
}
