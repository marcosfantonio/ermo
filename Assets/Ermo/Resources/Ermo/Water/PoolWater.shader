Shader "Ermo/Pool Water"
{
    Properties
    {
        _BaseColor ("Water tint", Color) = (0.014,0.085,0.10,1)
        _DepthColor ("Optical depth", Color) = (0.006,0.038,0.055,1)
        _WaveStrength ("Small surface waves", Range(0,0.3)) = 0.065
        _ReflectionFloor ("Sky reflection balance", Range(0.02,0.25)) = 0.10
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Cull Off ZWrite On
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WaterSky.hlsl"
            #include "../BalnearioFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _DepthColor;
                float _WaveStrength, _ReflectionFloor;
            CBUFFER_END
            float4 _BalnearioWorldOffset;
            TEXTURECUBE(_BalnearioReflection); SAMPLER(sampler_BalnearioReflection);
            float4 _BalnearioReflectionHDR;
            float _BalnearioReflectionReady;
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(float4 vertex : POSITION)
            {
                Varyings o; o.world = TransformObjectToWorld(vertex.xyz);
                o.position = TransformWorldToHClip(o.world); return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world.xz+_BalnearioWorldOffset.xy;
                float time = _Time.y;
                // The original Water Level optical model, not a mosaic painted on the surface.
                float2 gradient = float2(cos(p.x*1.1+time*.8)+.5*cos((p.x+p.y)*2.3-time),
                    .7*cos(p.y*1.5-time*.6)+.5*cos((p.x+p.y)*2.3-time));
                float3 n = normalize(float3(-gradient.x*_WaveStrength,1,-gradient.y*_WaveStrength));
                float3 view = normalize(_WorldSpaceCameraPos-input.world);
                if (dot(n,view) < 0) n = -n;
                float fresnel = _ReflectionFloor+(1-_ReflectionFloor)*pow(1-saturate(dot(n,view)),5);
                float deep = .65+.3*WaterNoise(p*.18);
                float3 base = lerp(_BaseColor.rgb,_DepthColor.rgb,deep);
                Light sun = GetMainLight();
                float specular = pow(saturate(dot(n,normalize(sun.direction+view))),180)*.65;
                float3 reflected = reflect(-view,n);
                float3 sky = OtherSideSky(reflected);
                if (_BalnearioReflectionReady > .5)
                {
                    half4 probe = SAMPLE_TEXTURECUBE_LOD(_BalnearioReflection,sampler_BalnearioReflection,reflected,0);
                    sky = DecodeHDREnvironment(probe,_BalnearioReflectionHDR);
                }
                float3 color = lerp(base,sky,fresnel)+sun.color*specular;
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0; i < count; i++)
                {
                    Light light = GetAdditionalLight(i,input.world);
                    color += light.color*light.distanceAttenuation*light.shadowAttenuation*
                        pow(saturate(dot(n,normalize(light.direction+view))),120)*.6;
                }
                #endif
                return half4(BalnearioFog(color,input.world,_WorldSpaceCameraPos),1);
            }
            ENDHLSL
        }
    }
}
