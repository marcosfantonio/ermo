#ifndef ERMO_RESERVOIR_SKY
#define ERMO_RESERVOIR_SKY
float WaterHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float WaterNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
    return lerp(lerp(WaterHash(i),WaterHash(i+float2(1,0)),f.x),
        lerp(WaterHash(i+float2(0,1)),WaterHash(i+1),f.x),f.y);
}
// Shared by the actual destination sky and its reflection in the reservoirs.
float3 OtherSideSky(float3 direction)
{
    float h = saturate(direction.y);
    float3 sky = lerp(float3(.14,.50,.62),float3(.025,.19,.43),pow(h,.55));
    float2 plane = direction.xz/max(direction.y,.12);
    float clouds = WaterNoise(plane*float2(.8,2.4))*.52+WaterNoise(plane*5)*.25+
        WaterNoise(plane*13)*.15+WaterNoise(plane*31)*.08;
    float cloud = smoothstep(.49,.68,clouds)*smoothstep(.04,.3,h);
    sky = lerp(sky,lerp(float3(.43,.57,.65),float3(.95,.98,.94),smoothstep(.50,.70,clouds)),cloud*.9);
    float sun = pow(saturate(dot(direction,normalize(float3(.28,.83,-.48)))),650);
    return sky+float3(1,.91,.72)*sun*2;
}
#endif
