Shader "Ermo/Balneario Surface"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Surface detail", 2D) = "white" {}
        _Cull ("Cull", Float) = 2
        _DepthBias ("Overlay depth bias", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull [_Cull]
        Offset 0, [_DepthBias]
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "BalnearioFog.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cull;
            CBUFFER_END
            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings o; o.world = TransformObjectToWorld(input.position.xyz);
                o.position = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(input.normal); o.uv = input.uv; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                float diffuse = dot(normalize(input.normal),sun.direction);
                diffuse = _Cull < .5 ? abs(diffuse) : saturate(diffuse);
                // Outdoor hemispheric fill belongs to this destination, not the rural room globals.
                half3 ambient = lerp(half3(.09,.10,.07),half3(.18,.22,.25),normalize(input.normal).y*.5+.5);
                half3 color = _BaseColor.rgb*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).rgb;
                color *= ambient+sun.color*diffuse*sun.shadowAttenuation;
                return half4(BalnearioFog(color,input.world,_WorldSpaceCameraPos),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/MotionVectors"
    }
}
