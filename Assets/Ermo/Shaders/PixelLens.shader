Shader "Ermo/Pixel Lens"
{
    Properties { _MainTex ("Frame", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distortion, _Strength, _Grain, _Vignette, _Chromatic, _Dither;
            float _LegacyVhs, _TapeStrength, _Saturation, _Contrast;
            float _FoundFootage, _CaptureSoftness, _CaptureSaturation, _CaptureContrast, _CaptureInstability;
            float _AnalogArtifacts, _AnalogMotion;
            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.vertex = UnityObjectToClipPos(input.vertex); output.uv = input.uv; return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float2 SafeUV(float2 uv) { return clamp(uv,abs(_MainTex_TexelSize.xy)*.5,1-abs(_MainTex_TexelSize.xy)*.5); }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixel = abs(_MainTex_TexelSize.xy);
                float2 coord = floor(input.uv*_MainTex_TexelSize.zw);
                if (_LegacyVhs > .5)
                {
                    // Optional port of the legacy vhs.gdshader, not the active exploration filter.
                    float frame = floor(_Time.y*24), row = coord.y;
                    float jitter = (Hash(float2(row,frame))-.5)*.6;
                    float tracking = pow(max(0,sin(input.uv.y*8-_Time.y*.45)),48);
                    float2 uv = input.uv + float2((jitter+tracking*sin(row*.7+frame)*1.3)*pixel.x*_TapeStrength,0);
                    float3 clean = tex2D(_MainTex,SafeUV(uv)).rgb;
                    float3 bleed = float3(tex2D(_MainTex,SafeUV(uv+float2(pixel.x*1.2,0))).r,
                        clean.g,tex2D(_MainTex,SafeUV(uv-float2(pixel.x*1.2,0))).b);
                    float3 color = lerp(clean,bleed,_TapeStrength*.45);
                    float luma = dot(color,float3(.299,.587,.114));
                    color = lerp(luma.xxx,color,_Saturation);
                    color = (color-.5)*_Contrast+.5;
                    color *= 1-fmod(row,2)*.055*_TapeStrength;
                    color += (Hash(coord+float2(frame*17,frame*3))-.5)*.055*_TapeStrength;
                    float2 edge = input.uv*(1-input.uv);
                    color *= lerp(1,.91+smoothstep(0,.12,sqrt(edge.x*edge.y))*.09,_TapeStrength);
                    return half4(saturate(color),1);
                }
                // Port of memory_optics.gdshader: 640x360 before integer upscale.
                float2 p = input.uv*2-1;
                float r = dot(p,p);
                float2 uv = .5+.5*p*(1+_Distortion*r*_Strength)/(1+2*_Distortion*_Strength);
                uv.x += sin(input.uv.y*44-_Time.y*.11)*sin(_Time.y*.43)*pixel.x*_CaptureInstability*_Strength*_FoundFootage;
                float videoFrame = floor(_Time.y*24);
                float tracking = pow(saturate(sin(input.uv.y*10-_Time.y*.32)),32);
                uv.x += tracking*sin(coord.y*.71+videoFrame)*pixel.x*.7*_AnalogArtifacts*_AnalogMotion*_Strength;
                float2 shift = p*r*pixel*_Chromatic*_Strength;
                float3 color = tex2D(_MainTex,SafeUV(uv)).rgb;
                color.r = tex2D(_MainTex,SafeUV(uv+shift)).r;
                color.b = tex2D(_MainTex,SafeUV(uv-shift)).b;
                float3 soft = tex2D(_MainTex,SafeUV(uv+float2(pixel.x,0))).rgb;
                soft += tex2D(_MainTex,SafeUV(uv-float2(pixel.x,0))).rgb;
                if (_FoundFootage > .5 && _Strength > 0)
                {
                    // A softly focused camcorder image, not a thick VHS/static overlay.
                    float3 vertical = tex2D(_MainTex,SafeUV(uv+float2(0,pixel.y))).rgb;
                    vertical += tex2D(_MainTex,SafeUV(uv-float2(0,pixel.y))).rgb;
                    color = lerp(color,(color*2+soft+vertical)/6,_CaptureSoftness*_Strength);
                    color += max(soft*.5-.78,0)*float3(1,.91,.76)*.035*_Strength;
                    float3 display = LinearToGammaSpace(max(color,0));
                    float luma = saturate(dot(display,float3(.299,.587,.114)));
                    float3 grade = lerp(luma.xxx,display,_CaptureSaturation);
                    float3 bleed = LinearToGammaSpace(max(tex2D(_MainTex,SafeUV(uv-float2(pixel.x*1.4,0))).rgb,0));
                    float bleedLuma = dot(bleed,float3(.299,.587,.114));
                    grade += ((bleed-bleedLuma)-(display-luma))*.18*_AnalogArtifacts;
                    grade = (grade-.5)*_CaptureContrast+.5;
                    grade += float3(.010,.012,.014)*(1-luma)*(1-luma);
                    grade *= lerp(float3(.985,1,1.015),float3(1.01,1,.985),smoothstep(.2,.85,luma));
                    grade /= 1+max(grade-.72,0)*.15;
                    // Grain lives in display space: tiny amplitude, mostly in darker areas.
                    // Adding the old noise in linear light made it excessively visible.
                    float noise = Hash(coord+floor(_Time.y*24))-.5;
                    grade += noise*_Grain*(.35+.65*(1-luma));
                    grade += (float3(Hash(coord+videoFrame+71),Hash(coord+videoFrame+193),Hash(coord-videoFrame+137))-.5)*.12*_Grain;
                    float scan = .5+.5*cos(coord.y*3.141593);
                    grade *= 1-scan*.025*_AnalogArtifacts;
                    float exposure = 1+(sin(_Time.y*.67)*.008+sin(_Time.y*.17+.3)*.014)*_AnalogArtifacts*_AnalogMotion;
                    grade *= exposure;
                    float dropout = step(.988,Hash(float2(videoFrame,93)));
                    float bandY = Hash(float2(videoFrame,29));
                    float band = exp(-pow((input.uv.y-bandY)*_MainTex_TexelSize.w*.4,2));
                    grade += band*dropout*.035*_AnalogArtifacts*_AnalogMotion;
                    color = GammaToLinearSpace(saturate(lerp(display,grade,_Strength)));
                }
                else if (_FoundFootage < .5)
                {
                    color += max(soft*.5-.75,0)*.09*_Strength;
                    float noise = Hash(coord+floor(_Time.y*18))-.5;
                    color += noise*_Grain*_Strength;
                }
                float pattern = fmod(coord.x,2)+2*fmod(coord.y,2);
                color += (pattern/3-.5)*_Dither/31*_Strength;
                color *= 1-smoothstep(.3,1.8,r)*_Vignette*_Strength;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
