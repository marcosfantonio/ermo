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
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Distortion, _Strength, _Grain, _Vignette, _Chromatic, _Dither, _RunEdgeEffect;
            float _LegacyVhs, _TapeStrength, _Saturation, _Contrast;
            float _FoundFootage, _CaptureSoftness, _CaptureSaturation, _CaptureContrast;
            float _CaptureExposure, _CaptureSharpness, _CaptureBloom, _DigitalCompression;
            float _ChromaNoise, _WhiteClip, _EdgeSoftness, _FocusBlur, _CaptureFrame;
            float3 _WhiteBalance;
            float _WaterImmersion, _WaterWetness, _WaterEffectTime, _WaterSplash;
            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.vertex = UnityObjectToClipPos(input.vertex); output.uv = input.uv; return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float3 LensDrop(float2 uv)
            {
                float2 grid = float2(7,5);
                float2 cell = floor(uv*grid);
                float random = Hash(cell+17);
                float2 center = float2(.2+random*.6,.15+Hash(cell+73)*.65);
                float fall = _WaterEffectTime*(.035+random*.055);
                float2 q = frac(uv*grid+float2(0,fall))-center;
                q.x *= 1.4;
                float radius = .075+random*.13;
                float d = length(q);
                float mask = (1-smoothstep(radius*.65,radius,d))*step(.3,random)*_WaterWetness;
                return float3(q*mask*.10,mask);
            }
            float BubbleOverlay(float2 uv)
            {
                float bubble = 0;
                [unroll] for (int i = 0; i < 14; i++)
                {
                    float seed = i+1;
                    float random = Hash(float2(seed,31));
                    float speed = .22+random*.4;
                    float2 center = float2(Hash(float2(seed,19)),frac(_WaterEffectTime*speed+random));
                    center.x += sin(_WaterEffectTime*2+seed)*.02;
                    float2 q = uv-center; q.x *= 1.3333;
                    float radius = .005+random*.017;
                    float d = length(q);
                    float ring = (1-smoothstep(.0005,.0025,abs(d-radius)))*(.08+.13*saturate(-q.y/max(radius,.001)));
                    float highlight = (1-smoothstep(.001,.004,length(q-float2(-radius*.35,radius*.5))))*.35;
                    bubble += ring+highlight;
                }
                return saturate(bubble)*_WaterImmersion;
            }
            float2 SafeUV(float2 uv) { return clamp(uv,abs(_MainTex_TexelSize.xy)*.5,1-abs(_MainTex_TexelSize.xy)*.5); }
            float3 CaptureSample(float2 sensorUV)
            {
                float2 p = sensorUV*2-1;
                float r = dot(p,p);
                // Barrel/fisheye optics with overscan, avoiding black corners.
                float distortion = _Distortion + _RunEdgeEffect*.012;
                float2 uv = .5+.5*p*(1+distortion*r)/(1+2*distortion);
                uv += float2(sin(sensorUV.y*18+_WaterEffectTime*2),cos(sensorUV.x*16+_WaterEffectTime*1.7))*.006*_WaterImmersion;
                float3 drop = LensDrop(sensorUV);
                uv += drop.xy;
                uv += float2(sin(sensorUV.y*31+_WaterEffectTime*8),cos(sensorUV.x*27-_WaterEffectTime*7))*.012*_WaterSplash;
                float2 shift = p*r*abs(_MainTex_TexelSize.xy)*_Chromatic;
                float3 color = tex2D(_MainTex,SafeUV(uv)).rgb;
                color.r = tex2D(_MainTex,SafeUV(uv+shift)).r;
                color.b = tex2D(_MainTex,SafeUV(uv-shift)).b;
                return LinearToGammaSpace(max(color,0));
            }
            float3 DigitalGrade(float3 display)
            {
                // Exposure belongs in sensor/linear space. Multiplying gamma RGB
                // blew out green/cyan channels and looked like a neon game filter.
                display = LinearToGammaSpace(GammaToLinearSpace(max(display,0))*_WhiteBalance*exp2(_CaptureExposure));
                float green = saturate((display.g-max(display.r,display.b))*4);
                display = float3(display.r*1.035+display.g*.012,
                    display.g*.985,display.b*1.025+display.r*.006);
                float luma = dot(display,float3(.299,.587,.114));
                // Compact-camera color matrix: vivid blues/greens, not lime clipping.
                float3 grade = lerp(luma.xxx,display,_CaptureSaturation*lerp(1,.86,green));
                grade = (grade-.5)*_CaptureContrast+.5;
                grade += float3(-.008,.002,.014)*(1-smoothstep(.08,.55,luma));
                // Early digital processing has a short highlight shoulder followed
                // by hard white clipping, without a milky film-style black lift.
                grade = (grade-.018)/max(_WhiteClip,.01);
                return saturate(grade);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixel = abs(_MainTex_TexelSize.xy);
                float2 coord = floor(input.uv*_MainTex_TexelSize.zw);
                if (_Strength <= 0) return half4(tex2D(_MainTex,SafeUV(input.uv)).rgb,1);
                if (_FoundFootage > .5 && _LegacyVhs < .5)
                {
                    // Optical barrel distortion and restrained radial chromatic
                    // aberration are separate from VHS/tracking artifacts.
                    float2 p = input.uv*2-1;
                    float periphery = smoothstep(.15,1.6,dot(p,p));
                    float radius = 1.35+_FocusBlur*4;
                    float3 center = CaptureSample(input.uv);
                    // A restrained radial smear lives only in the outer image,
                    // like the soft peripheral streaking of a handheld run.
                    if (_RunEdgeEffect > .001)
                    {
                        float runEdges = smoothstep(.08,1.7,dot(p,p))*_RunEdgeEffect*.35;
                        float2 radial = normalize(p+float2(.0001,.0001));
                        float3 runBlur = CaptureSample(input.uv-radial*pixel*3);
                        runBlur += CaptureSample(input.uv-radial*pixel*6);
                        runBlur += CaptureSample(input.uv-radial*pixel*9);
                        runBlur += CaptureSample(input.uv-radial*pixel*12);
                        center = lerp(center,(center+runBlur)/5,runEdges);
                    }
                    float3 neighbors = CaptureSample(input.uv+float2(pixel.x*radius,0));
                    neighbors += CaptureSample(input.uv-float2(pixel.x*radius,0));
                    neighbors += CaptureSample(input.uv+float2(0,pixel.y*radius));
                    neighbors += CaptureSample(input.uv-float2(0,pixel.y*radius));
                    neighbors *= .25;
                    float3 display = lerp(center,neighbors,saturate(_CaptureSoftness+periphery*_EdgeSoftness+_FocusBlur));
                    float edge = dot(center-neighbors,float3(.299,.587,.114));
                    // Luma-only edge enhancement leaves the characteristic small
                    // bright/dark digital halos without introducing RGB fringes.
                    display += clamp(edge,-.075,.075)*_CaptureSharpness*(1-periphery*.6);
                    float3 glow = CaptureSample(input.uv+pixel*float2(3,3));
                    glow += CaptureSample(input.uv+pixel*float2(-3,3));
                    glow += CaptureSample(input.uv+pixel*float2(3,-3));
                    glow += CaptureSample(input.uv-pixel*3);
                    display += max(glow*.25-.8,0)*_CaptureBloom;
                    float3 grade = DigitalGrade(display);
                    float luma = dot(grade,float3(.299,.587,.114));
                    float frame = _CaptureFrame;
                    float noise = Hash(coord+float2(frame*17,frame*3))-.5;
                    float3 colorNoise = float3(Hash(floor(coord/2)+frame+19),
                        Hash(floor(coord/2)+frame+83),Hash(floor(coord/2)+frame+137))-.5;
                    float shadow = pow(1-saturate(luma),1.7);
                    float gain = exp2(max(_CaptureExposure,0)*.15);
                    grade += noise*_Grain*(.12+.88*shadow)*gain;
                    grade += colorNoise*_ChromaNoise*shadow*gain;
                    grade *= 1-smoothstep(.4,1.8,dot(p,p))*_Vignette;
                    // Preserve the scene underwater: absorption removes red,
                    // scattering reduces contrast, bubbles stay in front of it.
                    float caustic = sin(input.uv.x*12+_WaterEffectTime)*sin(input.uv.y*16-_WaterEffectTime*.8)*.018;
                    float3 submerged = grade*float3(.42,.82,.92);
                    submerged = lerp(submerged,float3(.035,.20,.25),.30)+caustic;
                    grade = lerp(grade,submerged,_WaterImmersion);
                    grade += BubbleOverlay(input.uv)*float3(.42,.65,.7);
                    float3 drop = LensDrop(input.uv);
                    grade = lerp(grade,grade*.88+float3(.075,.085,.09),drop.z*.45);
                    grade += _WaterSplash*.035;
                    return half4(GammaToLinearSpace(saturate(grade)),1);
                }
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
                float2 shift = p*r*pixel*_Chromatic*_Strength;
                float3 color = tex2D(_MainTex,SafeUV(uv)).rgb;
                color.r = tex2D(_MainTex,SafeUV(uv+shift)).r;
                color.b = tex2D(_MainTex,SafeUV(uv-shift)).b;
                float3 soft = tex2D(_MainTex,SafeUV(uv+float2(pixel.x,0))).rgb;
                soft += tex2D(_MainTex,SafeUV(uv-float2(pixel.x,0))).rgb;
                if (_FoundFootage < .5)
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
        Pass
        {
            Name "MPEG spatial and temporal detail loss"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Compress
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _HistoryTex;
            float4 _MainTex_TexelSize;
            float _DigitalCompression, _FlatQuantization, _DetailQuantization, _ChromaSubsampling;
            float _TemporalLoss, _CameraMotion, _HistoryValid;
            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.vertex = UnityObjectToClipPos(input.vertex); output.uv = input.uv; return output;
            }
            float2 SafeUV(float2 uv) { return clamp(uv,abs(_MainTex_TexelSize.xy)*.5,1-abs(_MainTex_TexelSize.xy)*.5); }
            float3 SampleDisplay(float2 uv) { return LinearToGammaSpace(max(tex2D(_MainTex,SafeUV(uv)).rgb,0)); }
            float Luma(float3 color) { return dot(color,float3(.299,.587,.114)); }
            half4 Compress(Varyings input) : SV_Target
            {
                if (_DigitalCompression <= 0) return tex2D(_MainTex,SafeUV(input.uv));
                float2 pixel = abs(_MainTex_TexelSize.xy);
                float2 coord = floor(input.uv*_MainTex_TexelSize.zw);
                float3 current = SampleDisplay(input.uv);
                float y = Luma(current);
                // Actual 2x2 average in capture space: luma retains VGA detail,
                // chroma shares one value across each 4:2:0 group.
                float2 chromaUV = (floor(coord/2)*2+.5)*pixel;
                float3 chromaBlock = (SampleDisplay(chromaUV)+SampleDisplay(chromaUV+float2(pixel.x,0))+
                    SampleDisplay(chromaUV+float2(0,pixel.y))+SampleDisplay(chromaUV+pixel))*.25;
                float3 chroma = lerp(current-y,chromaBlock-Luma(chromaBlock),_ChromaSubsampling);
                float2 dcUV = (floor(coord/8)*8+2)*pixel;
                float dc = (Luma(SampleDisplay(dcUV))+Luma(SampleDisplay(dcUV+float2(pixel.x*4,0)))+
                    Luma(SampleDisplay(dcUV+float2(0,pixel.y*4)))+Luma(SampleDisplay(dcUV+pixel*4)))*.25;
                float detail = saturate(abs(y-dc)*10);
                float3 previous = current;
                if (_HistoryValid > .5) previous = LinearToGammaSpace(max(tex2D(_HistoryTex,SafeUV(input.uv)).rgb,0));
                float difference = abs(y-Luma(previous));
                float motion = saturate(_CameraMotion+difference*8)*_HistoryValid;
                float quantum = max(.001,lerp(_FlatQuantization,_DetailQuantization,detail)*(1+motion*.65));
                // Only retain low-amplitude residuals. Reject cuts/occlusions;
                // this is codec detail loss, not a long-exposure ghost overlay.
                float historyWeight = _TemporalLoss*motion*(1-smoothstep(.025,.07,difference));
                float residual = lerp(y-dc,Luma(previous)-dc,historyWeight);
                float quantized = floor(dc/quantum+.5)*quantum+floor(residual/quantum+.5)*quantum;
                chroma = floor(chroma/(quantum*1.4)+.5)*(quantum*1.4);
                float3 compressed = quantized.xxx+chroma;
                return half4(GammaToLinearSpace(saturate(lerp(current,compressed,_DigitalCompression))),1);
            }
            ENDHLSL
        }
    }
}
