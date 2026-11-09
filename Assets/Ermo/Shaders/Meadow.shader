Shader "Ermo/Meadow"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Grass ("Grass texture", Float) = 0
        _Road ("Chalk texture", Float) = 0
        _MainTex ("Godot grain", 2D) = "white" {}
        _TextureScale ("Texture scale", Float) = 1
        _Wobble ("Subpixel vertex snap", Range(0,1)) = 0
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
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
            #include "Atmosphere.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; half4 color : COLOR; float2 uv : TEXCOORD2; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _ErmoAmbientColor;
            float _ErmoFoundFootage;
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Grass;
                float _Road;
                float _TextureScale;
                float _Wobble;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world);
                float2 ndc = output.positionCS.xy / output.positionCS.w;
                float2 grid = float2(320,180);
                float2 snapped = floor(ndc*grid+.5)/grid;
                output.positionCS.xy = lerp(ndc,snapped,_Wobble) * output.positionCS.w;
                output.normal = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half diffuse = saturate(dot(normalize(input.normal), sun.direction));
                // Stylized ambient keeps shaded hills luminous rather than photorealistically dark.
                half3 ambient = lerp(half3(.2,.3,.4),_ErmoAmbientColor.rgb,_ErmoAmbientColor.a);
                half3 lighting = ambient + sun.color * diffuse * sun.shadowAttenuation;
                half3 color = _BaseColor.rgb * input.color.rgb;
                half3 detail = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv*_TextureScale).rgb;
                if (_ErmoFoundFootage > .5 && _Grass > .5)
                    detail = SAMPLE_TEXTURE2D(_MainTex,sampler_LinearRepeat,input.uv*_TextureScale).rgb;
                float2 uv = input.uv;
                float patches = sin(uv.x*.7 + sin(uv.y*.4))*sin(uv.y*.5+cos(uv.x*.6));
                float textureStrength = lerp(.4,.2,saturate(_ErmoFoundFootage));
                half3 textured = lerp(detail,lerp(half3(1,1,1),detail*1.65,textureStrength)*(1+patches*.07),_Grass);
                color *= textured;
                color *= lighting;
                return half4(ErmoAtmosphere(color, input.world, _WorldSpaceCameraPos, _Time.y), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
