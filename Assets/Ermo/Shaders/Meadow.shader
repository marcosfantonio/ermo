Shader "Ermo/Meadow"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Grass ("Grass texture", Float) = 0
        _Road ("Chalk texture", Float) = 0
        _MainTex ("Surface texture", 2D) = "white" {}
        _TextureScale ("Texture scale", Float) = 1
        _Wobble ("Subpixel vertex snap", Range(0,1)) = 0
        _Cull ("Cull", Float) = 2
        [HideInInspector] _ErmoInteriorEnabled ("Room lighting", Float) = 0
        [HideInInspector] _ErmoInteriorRoof ("Interior roof", Float) = 0
        [HideInInspector] _ErmoInteriorMin ("Room minimum", Vector) = (0,0,0,0)
        [HideInInspector] _ErmoInteriorMax ("Room maximum", Vector) = (0,0,0,0)
        [HideInInspector] _ErmoInteriorCeiling ("Pitched room ceiling", Vector) = (0,0,0,0)
        [HideInInspector] _ErmoPortalCount ("Room openings", Integer) = 0
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
                float _ErmoInteriorEnabled;
                float _ErmoInteriorRoof;
                float4 _ErmoInteriorMin;
                float4 _ErmoInteriorMax;
                float4 _ErmoInteriorCeiling;
                float4x4 _ErmoInteriorWorldToLocal;
                int _ErmoPortalCount;
                float4 _ErmoPortalMin[8];
                float4 _ErmoPortalMax[8];
            CBUFFER_END
            #include "HouseInterior.hlsl"
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
                // Keep blue fill subtle, with deeper cast shadows and unchanged direct sunlight.
                half3 ambient = lerp(half3(.035,.065,.12),_ErmoAmbientColor.rgb,_ErmoAmbientColor.a);
                ambient *= lerp(.45, 1.0, sun.shadowAttenuation);
                float3 roomLighting = ErmoRoomLighting(input.world, normalize(input.normal), sun.direction);
                half3 lighting = ambient * roomLighting.x + sun.color * diffuse * sun.shadowAttenuation * roomLighting.y;
                half3 color = _BaseColor.rgb * input.color.rgb;
                half3 detail = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv*_TextureScale).rgb;
                if (_ErmoFoundFootage > .5 && _Grass > .5)
                    detail = SAMPLE_TEXTURE2D(_MainTex,sampler_LinearRepeat,input.uv*_TextureScale).rgb;
                float2 uv = input.uv;
                float patches = sin(uv.x*.7 + sin(uv.y*.4))*sin(uv.y*.5+cos(uv.x*.6));
                float textureStrength = lerp(.4,.2,saturate(_ErmoFoundFootage));
                half3 textured = lerp(detail,lerp(half3(1,1,1),detail*1.65,textureStrength)*(1+patches*.07),_Grass);
                color *= textured;
                // Keep a saturated meadow without pre-clipping the green channel;
                // camera color response supplies the nostalgic color, not neon paint.
                if (_Grass > .5)
                {
                    half3 greenTint = color * half3(.94, 1.1, .82);
                    color = lerp(color, greenTint, .55);
                }
                color *= lighting;
                // Outdoor distance haze must not add sky-colored light to a sealed room.
                if (roomLighting.z < .5)
                    color = ErmoAtmosphere(color, input.world, _WorldSpaceCameraPos, _Time.y);
                return half4(color, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/MotionVectors"
    }
}
