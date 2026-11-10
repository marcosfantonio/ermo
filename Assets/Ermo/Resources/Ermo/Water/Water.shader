Shader "Ermo/Deep Water"
{
    Properties
    {
        _BaseColor ("Water tint",Color) = (0.014,0.085,0.10,1)
        _DepthColor ("Optical depth",Color) = (0.006,0.038,0.055,1)
        _WaveStrength ("Small surface waves",Range(0,0.3)) = 0.065
        _ReflectionFloor ("Sky reflection balance",Range(0.02,0.25)) = 0.10
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WaterSky.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _DepthColor;
                float _WaveStrength, _ReflectionFloor;
            CBUFFER_END
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(float4 vertex : POSITION)
            {
                Varyings output; output.world = TransformObjectToWorld(vertex.xyz);
                output.position = TransformWorldToHClip(output.world); return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world.xz;
                float time = _Time.y;
                float2 gradient = float2(cos(p.x*1.1+time*.8)+.5*cos((p.x+p.y)*2.3-time),
                    .7*cos(p.y*1.5-time*.6)+.5*cos((p.x+p.y)*2.3-time));
                float3 n = normalize(float3(-gradient.x*_WaveStrength,1,-gradient.y*_WaveStrength));
                float3 view = normalize(_WorldSpaceCameraPos-input.world);
                if (dot(n,view) < 0) n = -n;
                float facing = saturate(dot(n,view));
                float fresnel = _ReflectionFloor+(1-_ReflectionFloor)*pow(1-facing,5);
                float3 reflection = OtherSideSky(reflect(-view,n));
                float deep = .65+.3*WaterNoise(p*.18);
                float3 water = lerp(_BaseColor.rgb,_DepthColor.rgb,deep);
                Light sun = GetMainLight();
                float specular = pow(saturate(dot(n,normalize(sun.direction+view))),180)*.65;
                float3 color = lerp(water,reflection,fresnel)+sun.color*specular;
                float haze = 1-exp(-length(_WorldSpaceCameraPos.xz-p)*.0015);
                color = lerp(color,OtherSideSky(float3(0,.025,1))*.75,haze*.65);
                return half4(color,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Unlit/MotionVectors"
    }
}
