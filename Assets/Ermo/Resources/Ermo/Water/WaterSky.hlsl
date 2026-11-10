#ifndef ERMO_WATER_SKY
#define ERMO_WATER_SKY
float WaterHash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float WaterNoise(float2 p)
{
    float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
    return lerp(lerp(WaterHash(i),WaterHash(i+float2(1,0)),f.x),
        lerp(WaterHash(i+float2(0,1)),WaterHash(i+1),f.x),f.y);
}
float3 OtherSideSky(float3 direction)
{
    float h = saturate(direction.y);
    float3 sky = lerp(float3(.14,.50,.62),float3(.025,.19,.43),pow(h,.55));
    float2 plane = direction.xz/max(direction.y,.12);
    float clouds = WaterNoise(plane*float2(.8,2.4))*.75+WaterNoise(plane*5)*.25;
    float cloud = smoothstep(.57,.75,clouds)*smoothstep(.04,.3,h);
    return lerp(sky,float3(.95,.98,.94),cloud*.85);
}
#endif
