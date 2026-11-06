shader_type canvas_item;
render_mode unshaded;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float distortion : hint_range(0.0, 0.2) = 0.045;
uniform float grain : hint_range(0.0, 1.0) = 0.045;
uniform float vignette : hint_range(0.0, 1.0) = 0.16;
uniform float chromatic : hint_range(0.0, 2.0) = 0.35;
uniform float dither : hint_range(0.0, 1.0) = 0.28;
uniform float strength = 1.0;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}
void fragment() {
    vec2 p = SCREEN_UV * 2.0 - 1.0;
    float r = dot(p, p);
    // Barrel distortion with overscan: never exposes black or clamped borders.
    vec2 uv = 0.5 + 0.5 * p * (1.0 + distortion * r * strength) / (1.0 + 2.0 * distortion * strength);
    vec2 shift = p * r * SCREEN_PIXEL_SIZE * chromatic * strength;
    vec3 color = texture(screen_texture, uv).rgb;
    color.r = texture(screen_texture, uv + shift).r;
    color.b = texture(screen_texture, uv - shift).b;
    // Analogue highlight spread without an expensive viewport bloom chain.
    vec3 soft = texture(screen_texture, uv + vec2(SCREEN_PIXEL_SIZE.x, 0)).rgb;
    soft += texture(screen_texture, uv - vec2(SCREEN_PIXEL_SIZE.x, 0)).rgb;
    color += max(soft * 0.5 - vec3(0.75), vec3(0.0)) * 0.09 * strength;
    float noise = hash(floor(FRAGCOORD.xy) + floor(TIME * 18.0)) - 0.5;
    float pattern = mod(FRAGCOORD.x, 2.0) + 2.0 * mod(FRAGCOORD.y, 2.0);
    color += (noise * grain + (pattern / 3.0 - 0.5) * dither / 31.0) * strength;
    color *= 1.0 - smoothstep(0.3, 1.8, r) * vignette * strength;
    COLOR = vec4(color, 1.0);
}
