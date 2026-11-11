#ifndef ERMO_BALNEARIO_FOG
#define ERMO_BALNEARIO_FOG
float4 _BalnearioHorizon; // clear and fully obscured distances, updated as sectors become ready
float3 BalnearioFog(float3 color, float3 world, float3 eye)
{
    float amount = smoothstep(_BalnearioHorizon.x,max(_BalnearioHorizon.x+1,_BalnearioHorizon.y),distance(world,eye));
    return lerp(color,float3(.14,.50,.62),amount);
}
#endif
