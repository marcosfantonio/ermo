Shader "Ermo/Slide Water"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "WaterSky.hlsl"
            #include "../BalnearioFog.hlsl"
            TEXTURECUBE(_BalnearioReflection); SAMPLER(sampler_BalnearioReflection);
            float4 _BalnearioReflectionHDR;
            float _BalnearioReflectionReady;
            struct V { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; float3 normal : TEXCOORD2; float spill : TEXCOORD3; };
            V Vert(float4 p : POSITION, float2 uv : TEXCOORD0, float3 n : NORMAL, float4 color : COLOR)
            {
                V o; o.world = TransformObjectToWorld(p.xyz); o.position = TransformWorldToHClip(o.world);
                o.uv = uv; o.normal = TransformObjectToWorldNormal(n); o.spill = color.r; return o;
            }
            half4 Frag(V i) : SV_Target
            {
                float2 flow = float2(i.uv.x*5,i.uv.y*1.8-_Time.y*5);
                float a = WaterNoise(flow*2.3), b = WaterNoise(flow*2.3+float2(.08,0)), c = WaterNoise(flow*2.3+float2(0,.08));
                float3 baseNormal = normalize(i.normal);
                float3 dx = ddx(i.world), dy = ddy(i.world);
                float2 ux = ddx(i.uv), uy = ddy(i.uv);
                float3 tangent = normalize(dx*uy.y-dy*ux.y+float3(.00001,0,0));
                float3 bitangent = normalize(cross(baseNormal,tangent));
                float3 n = normalize(baseNormal+tangent*(b-a)*.9+bitangent*(c-a)*.6);
                float3 view = normalize(_WorldSpaceCameraPos-i.world);
                if (dot(n,view) < 0) n = -n;
                float fresnel = .0204+.9796*pow(1-saturate(dot(n,view)),5);
                float3 reflected = reflect(-view,n);
                float3 reflection = OtherSideSky(reflected);
                if (_BalnearioReflectionReady > .5)
                    reflection = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_BalnearioReflection,sampler_BalnearioReflection,reflected,0),_BalnearioReflectionHDR);
                Light sun = GetMainLight();
                float glint = pow(saturate(dot(n,normalize(sun.direction+view))),220)*.8;
                // Mostly clear film. Aeration is irregular and confined to the spill/edges.
                float edge = smoothstep(.32,.5,abs(i.uv.x-.5));
                float foam = smoothstep(.68,.87,WaterNoise(flow*5))*saturate(i.spill*.8+edge*.25);
                float alpha = saturate(.055+fresnel*.85+foam*.45+glint*.45);
                float3 color = lerp(float3(.025,.13,.15),reflection,saturate(fresnel/max(alpha,.001)));
                color = lerp(color,float3(.87,.94,.94),foam)+sun.color*glint;
                if (_CameraOpaqueTexture_TexelSize.z > 16)
                {
                    float2 screenUV = GetNormalizedScreenSpaceUV(i.position);
                    float2 distortion = mul((float3x3)UNITY_MATRIX_V,n).xy*(.0015+i.spill*.0015);
                    float3 transmitted = SampleSceneColor(saturate(screenUV+distortion));
                    transmitted *= float3(.97,.992,.995);
                    float3 reflectedFog = BalnearioFog(reflection,i.world,_WorldSpaceCameraPos);
                    color = lerp(transmitted,reflectedFog,fresnel);
                    color = lerp(color,BalnearioFog(float3(.87,.94,.94),i.world,_WorldSpaceCameraPos),foam*.6)+sun.color*glint;
                    return half4(color,1);
                }
                return half4(BalnearioFog(color,i.world,_WorldSpaceCameraPos),alpha);
            }
            ENDHLSL
        }
    }
}
