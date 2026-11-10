Shader "Ermo/Other Side Sky"
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
            #include "WaterSky.hlsl"
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(float4 vertex : POSITION)
            {
                Varyings output; output.position = UnityObjectToClipPos(vertex); output.direction = vertex.xyz; return output;
            }
            half4 Frag(Varyings input) : SV_Target { return half4(OtherSideSky(normalize(input.direction)),1); }
            ENDHLSL
        }
    }
}
