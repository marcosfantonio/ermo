shader_type sky;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1,0)), f.x), mix(hash(i + vec2(0,1)), hash(i + vec2(1,1)), f.x), f.y);
}
void sky() {
    float height = max(EYEDIR.y, 0.0);
    vec3 blue = mix(vec3(0.15, 0.50, 0.76), vec3(0.025, 0.29, 0.65), smoothstep(0.0, 0.65, height));
    vec2 plane = EYEDIR.xz / max(EYEDIR.y, 0.06);
    float clouds = noise(plane * vec2(1.4, 5.0)) * 0.8 + noise(plane * 10.0) * 0.2;
    float mask = smoothstep(0.79, 0.88, clouds) * smoothstep(0.26, 0.45, height);
    COLOR = mix(blue, vec3(0.92, 0.92, 0.80), mask * 0.85);
}
