#ifndef ERMO_ATMOSPHERE_INCLUDED
#define ERMO_ATMOSPHERE_INCLUDED

float4 _ErmoHazeColor;
float4 _ErmoHazeParams; // start, opaque distance, optical depth, cloudiness
float _ErmoViewRange;
float _ErmoHazeHeight;
float _ErmoFogStyle;
float4 _ErmoGodotFog; // density, aerial perspective, light energy, sky affect

float ErmoHash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
float ErmoNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f*f*(3-2*f);
    return lerp(lerp(ErmoHash(i), ErmoHash(i+float2(1,0)), f.x),
        lerp(ErmoHash(i+float2(0,1)), ErmoHash(i+1), f.x), f.y);
}

float3 ErmoAtmosphere(float3 color, float3 world, float3 eye, float time)
{
    float distanceToEye = distance(world, eye);
    if (_ErmoViewRange <= 0) return color;
    if (_ErmoFogStyle > .5)
    {
        float height = saturate(normalize(world-eye).y);
        float3 sky = lerp(float3(.15,.50,.76),float3(.025,.29,.65),smoothstep(0,.65,height));
        float3 tint = lerp(_ErmoHazeColor.rgb*_ErmoGodotFog.z,sky,_ErmoGodotFog.y);
        float amount = 1-exp(-distanceToEye*_ErmoGodotFog.x);
        // Godot's distance fog, not the former broad moving height/noise layer.
        return lerp(color,tint,saturate(amount));
    }
    // No height layer, wisps or edge seal may leak into the clear viewing range.
    if (distanceToEye <= _ErmoHazeParams.x) return color;
    float t = saturate((distanceToEye - _ErmoHazeParams.x) / max(1, _ErmoHazeParams.y - _ErmoHazeParams.x));
    float3 midpoint = lerp(eye, world, .55);
    float lowLayer = exp(-max(0, midpoint.y - _ErmoHazeHeight) * .055);
    // Broad continuous wisps, not pixel noise. Very slow drift keeps distant hills calm.
    float2 p = midpoint.xz / 150 + float2(time * .002, -time * .001);
    float cloud = ErmoNoise(p) * .7 + ErmoNoise(p * 2.03 + 31) * .3;
    float opticalDepth = pow(t, 1.55) * _ErmoHazeParams.z * (.72 + lowLayer * .40);
    opticalDepth *= 1 + (cloud - .5) * 2 * _ErmoHazeParams.w;
    float haze = 1 - exp(-opticalDepth);
    // Seal the streaming horizon while leaving the foreground untouched.
    float edge = smoothstep(max(_ErmoHazeParams.x, _ErmoViewRange * .94), _ErmoViewRange * .999, distanceToEye);
    haze = 1 - (1 - haze) * (1 - edge * .98);
    float3 tint = _ErmoHazeColor.rgb + (cloud - .5) * .035 * t;
    return lerp(color, tint, saturate(haze));
}
#endif
