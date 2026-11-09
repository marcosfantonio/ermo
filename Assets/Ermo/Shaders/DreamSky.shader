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
                // Direct port of godot/ermo/shaders/sky.gdshader.
                float3 ray = normalize(input.direction);
                float height = max(ray.y,0);
                float3 blue = lerp(float3(.15,.50,.76),float3(.025,.29,.65),smoothstep(0,.65,height));
                float2 plane = ray.xz/max(ray.y,.06);
                float clouds = ErmoNoise(plane*float2(1.4,5))* .8 + ErmoNoise(plane*10)*.2;
                float mask = smoothstep(.79,.88,clouds)*smoothstep(.26,.45,height);
                float3 color = lerp(blue,float3(.92,.92,.80),mask*.85);
                if (_ErmoFogStyle > .5) color = lerp(color,_ErmoHazeColor.rgb*_ErmoGodotFog.z,_ErmoGodotFog.w);
                else color = lerp(color,_ErmoHazeColor.rgb,pow(1-saturate(height),45));
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
