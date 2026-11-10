Shader "Ermo/Reservoir Water"
{
    Properties
    {
        _BaseColor ("Clear water tint",Color) = (0.35,0.75,0.78,1)
        _DepthColor ("Optical depth",Color) = (0.12,0.34,0.38,1)
        _WaveStrength ("Small surface waves",Range(0,0.3)) = 0.065
        _ReflectionFloor ("Sky reflection balance",Range(0.02,0.25)) = 0.10
        _OtherWorldView ("View into the other sky",Range(0,1)) = 0
        _PortalInverted ("Invert the lower world",Float) = 0
        _WaterOpacity ("Water opacity",Range(0,1)) = 0.32
        _WellDepth ("Reservoir optical depth (zero for open sea)",Float) = 0
        _WellRadius ("Reservoir inner radius",Float) = 1.55
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "ReservoirSky.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _DepthColor;
                float _WaveStrength, _ReflectionFloor, _OtherWorldView, _PortalInverted, _WaterOpacity;
                float _WellDepth, _WellRadius;
            CBUFFER_END
            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.world = TransformObjectToWorld(input.position.xyz);
                output.uv = input.uv;
                output.position = TransformWorldToHClip(output.world); return output;
            }
            float2 RippleSlope(float2 p, float time)
            {
                // Several scales, not two broad sine waves: small broken
                // highlights remain visible in the low-resolution camera.
                float2 slope = float2(cos(p.x*3.1+time*.8),cos(p.y*4.3-time*.65))*.45;
                slope += float2(sin(p.x*13+p.y*7+time*1.3),cos(p.y*17-p.x*5-time))* .22;
                float2 q = p*23+float2(time*.34,-time*.23);
                slope += float2(WaterNoise(q+float2(.12,0))-WaterNoise(q-float2(.12,0)),
                    WaterNoise(q+float2(0,.12))-WaterNoise(q-float2(0,.12)))*3;
                return slope;
            }
            float3 ReservoirVolume(float3 world, float3 view, float3 normal)
            {
                // Trace a refracted view into a cylindrical WATER volume. This
                // keeps the impossible depth inside the existing tower bounds,
                // instead of exposing the platform/terrain beneath the tank.
                float3 origin = float3((TransformWorldToObject(world)).xz,0);
                float3 refracted = refract(-view,normal,1/1.333);
                float3 localDirection = normalize(TransformWorldToObjectDir(refracted));
                float3 ray = normalize(float3(localDirection.xz,min(localDirection.y,-.08)));
                float3 light = 0;
                float3 transmission = 1;
                [unroll] for (int bounce = 0; bounce < 4; bounce++)
                {
                    float a = max(dot(ray.xy,ray.xy),.0001);
                    float b = dot(origin.xy,ray.xy);
                    float c = dot(origin.xy,origin.xy)-_WellRadius*_WellRadius;
                    float wallDistance = max((-b+sqrt(max(b*b-a*c,0)))/a,.001);
                    float bottomDistance = max((-_WellDepth-origin.z)/min(ray.z,-.001),.001);
                    float travel = min(wallDistance,bottomDistance);
                    float3 hit = origin+ray*travel;
                    transmission *= exp(-float3(.10,.037,.022)*travel);
                    if (bottomDistance <= wallDistance)
                    {
                        // An inverted surface at the far end: sky looking UP,
                        // with cloud parallax, ripples and the same destination sky.
                        float2 ripple = RippleSlope(hit.xy,_Time.y)*.035;
                        float3 skyDirection = normalize(float3(-hit.x+ripple.x,1.15,-hit.y+ripple.y));
                        light += transmission*OtherSideSky(skyDirection);
                        return light;
                    }
                    float2 slope = RippleSlope(float2(atan2(hit.y,hit.x)*_WellRadius,-hit.z),_Time.y);
                    float3 wallNormal = normalize(float3(-normalize(hit.xy)+slope*.022,slope.y*.045));
                    float3 reflected = reflect(ray,wallNormal);
                    float3 reflectedSky = OtherSideSky(normalize(float3(reflected.x,abs(reflected.z)*.8+.12,reflected.y)));
                    light += transmission*reflectedSky*.065;
                    transmission *= .64;
                    origin = hit+wallNormal*.008;
                    ray = normalize(float3(reflected.xy,min(reflected.z,-.12)));
                }
                return light+transmission*OtherSideSky(normalize(float3(-ray.x,abs(ray.z),-ray.y)))*.3;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.world.xz;
                float time = _Time.y;
                float2 gradient = RippleSlope(p,time);
                float waveStrength = _WaveStrength*(_WellDepth > 0 ? .4 : 1);
                float3 n = normalize(float3(-gradient.x*waveStrength,1,-gradient.y*waveStrength));
                float3 view = normalize(_WorldSpaceCameraPos-input.world);
                if (dot(n,view) < 0) n = -n;
                float facing = saturate(dot(n,view));
                float fresnel = _ReflectionFloor+(1-_ReflectionFloor)*pow(1-facing,5);
                float3 reflection = OtherSideSky(reflect(-view,n));
                float deep = .65+.3*WaterNoise(p*.18);
                float3 water = lerp(_BaseColor.rgb,_DepthColor.rgb,deep*.12);
                Light sun = GetMainLight();
                float specular = pow(saturate(dot(n,normalize(sun.direction+view))),180)*.65;
                // The reservoir is a window into the other side, not merely a
                // mirror: map its circular opening as a view looking upward
                // from below the water, with the far horizon around the rim.
                float2 portalXY = (input.uv-.5)*2.2;
                portalXY *= lerp(1,-1,_PortalInverted);
                portalXY += gradient*.018;
                float3 upwardView = normalize(float3(portalXY,.66));
                float3 portalSky = OtherSideSky(upwardView);
                float edgeDepth = smoothstep(.62,1,length(input.uv-.5)*2);
                portalSky = lerp(portalSky,_DepthColor.rgb,edgeDepth*.22);
                float3 color = lerp(water,portalSky,_OtherWorldView);
                color = lerp(color,reflection,fresnel*.42)+sun.color*specular;
                if (_WellDepth > 0 && _WorldSpaceCameraPos.y > input.world.y)
                {
                    // Optical transmission is NOT alpha transparency. A full
                    // water pixel contains the view through the volume plus its
                    // surface reflection, so clear water never becomes invisible.
                    float3 volume = ReservoirVolume(input.world,view,n);
                    float surfaceReflection = .035+.965*pow(1-facing,5);
                    return half4(lerp(volume,reflection,surfaceReflection)+sun.color*specular,1);
                }
                // The open-water horizon reflects the same sky as the portal;
                // high-frequency ripples fade naturally with viewing distance.
                float haze = 1-exp(-length(_WorldSpaceCameraPos.xz-p)*.0015);
                color = lerp(color,OtherSideSky(float3(0,.025,1))*.75,haze*.65);
                // Clear from above, increasingly mirror-like at grazing angles.
                // The lower portal material overrides this to fully opaque.
                float opacity = lerp(_WaterOpacity,.32,saturate(fresnel*1.1));
                opacity = lerp(opacity,1,_OtherWorldView);
                return half4(color,opacity);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Unlit/MotionVectors"
    }
}
