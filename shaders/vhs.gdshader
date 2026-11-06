shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform float saturation : hint_range(0.0, 2.0) = 1.45;
uniform float contrast : hint_range(0.5, 1.5) = 1.06;
uniform float tape_strength : hint_range(0.0, 1.0) = 0.7;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
    vec2 pixel = SCREEN_PIXEL_SIZE;
    float frame = floor(TIME * 24.0);
    float line = floor(SCREEN_UV.y / pixel.y);
    // Low-amplitude horizontal time-base error, not a full-screen camera shake.
    float jitter = (hash(vec2(line, frame)) - 0.5) * 0.6;
    float tracking = pow(max(0.0, sin(SCREEN_UV.y * 8.0 - TIME * 0.45)), 48.0);
    vec2 uv = SCREEN_UV;
    uv.x += (jitter + tracking * sin(line * 0.7 + frame) * 1.3) * pixel.x * tape_strength;
    uv = clamp(uv, pixel * 0.5, vec2(1.0) - pixel * 0.5);
    vec3 clean = texture(screen_texture, uv).rgb;
    vec3 bleed = vec3(
        texture(screen_texture, clamp(uv + vec2(pixel.x * 1.2, 0.0), pixel * 0.5, vec2(1.0) - pixel * 0.5)).r,
        clean.g,
        texture(screen_texture, clamp(uv - vec2(pixel.x * 1.2, 0.0), pixel * 0.5, vec2(1.0) - pixel * 0.5)).b
    );
    vec3 color = mix(clean, bleed, tape_strength * 0.45);
    float luma = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(vec3(luma), color, saturation);
    color = (color - 0.5) * contrast + 0.5;
    float grain = hash(floor(SCREEN_UV / pixel) + vec2(frame * 17.0, frame * 3.0)) - 0.5;
    float scanline = mod(line, 2.0);
    color *= 1.0 - scanline * 0.055 * tape_strength;
    color += grain * 0.055 * tape_strength;
    // Gentle lens falloff preserves readable corners and the PS1 silhouette.
    vec2 edge = SCREEN_UV * (1.0 - SCREEN_UV);
    float vignette = smoothstep(0.0, 0.12, sqrt(edge.x * edge.y));
    color *= mix(1.0, 0.91 + vignette * 0.09, tape_strength);
    COLOR = vec4(clamp(color, 0.0, 1.0), 1.0);
}
