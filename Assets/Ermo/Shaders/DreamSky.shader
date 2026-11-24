Shader "Ermo/Dream Sky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            #include "Atmosphere.hlsl"
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(float4 vertex : POSITION)
            {
                Varyings output; output.position = UnityObjectToClipPos(vertex); output.direction = vertex.xyz; return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Sunset over the Sítios: peach horizon, dusty lavender, warm blue.
                float3 ray = normalize(input.direction);
                float height = max(ray.y,0);
                float3 horizon = float3(1.02,.56,.32);
                float3 middle = float3(.40,.33,.44);
                float3 zenith = float3(.075,.13,.32);
                float3 color = lerp(horizon,middle,smoothstep(0,.34,height));
                color = lerp(color,zenith,smoothstep(.22,.85,height));

                // Low sun disk and its warm bloom, driven by the live directional light.
                float3 sunDirection = _ErmoSunDirection.xyz;
                sunDirection = dot(sunDirection,sunDirection) > .0001 ? normalize(sunDirection) : float3(0,.3,-1);
                float sunDot = saturate(dot(ray,sunDirection));
                float glow = pow(sunDot,5)*.34 + pow(sunDot,48)*.85;
                float disk = smoothstep(.9992,.99965,sunDot);
                color += float3(1.05,.55,.24)*glow + float3(1.6,1.18,.78)*disk;

                float2 plane = ray.xz/max(ray.y,.06);
                float clouds = ErmoNoise(plane*float2(1.2,4.2))*.74 + ErmoNoise(plane*7.5)*.26;
                float mask = smoothstep(.72,.9,clouds)*smoothstep(.16,.48,height);
                color = lerp(color,float3(1.0,.72,.52),mask*.5);

                if (_ErmoFogStyle > .5) color = lerp(color,_ErmoHazeColor.rgb*1.02,_ErmoGodotFog.w);
                else color = lerp(color,_ErmoHazeColor.rgb,pow(1-saturate(height),10));
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
