// Agua "realista" para URP: olas en el vértice, normales procedurales, refracción del fondo,
// color por profundidad, reflejo del cielo (fresnel), brillo del sol y de las luces cercanas, y espuma en las orillas.
// Necesita Depth Texture y Opaque Texture activos en el URP Asset (PC_RPAsset ya los tiene).
Shader "Abby/Water"
{
    Properties
    {
        _ShallowColor ("Color poco profundo", Color) = (0.22, 0.72, 0.78, 0.35)
        _DeepColor ("Color profundo", Color) = (0.02, 0.16, 0.28, 0.9)
        _DepthDistance ("Distancia de profundidad", Float) = 1.2
        _WaveScale ("Escala de olas", Float) = 0.8
        _WaveSpeed ("Velocidad de olas", Float) = 0.5
        _WaveHeight ("Altura de olas", Float) = 0.02
        _NormalStrength ("Fuerza de normales", Float) = 0.45
        _Refraction ("Refracción", Float) = 0.035
        _FoamColor ("Espuma", Color) = (0.9, 0.97, 1, 1)
        _FoamDistance ("Ancho de espuma", Float) = 0.3
        _Reflection ("Reflejo", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardWater"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "AbbySky.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor, _DeepColor, _FoamColor;
                float _DepthDistance, _WaveScale, _WaveSpeed, _WaveHeight, _NormalStrength, _Refraction, _FoamDistance, _Reflection;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            float Waves(float2 p, float t)
            {
                return AbbyNoise(p + float2(t, t * 0.7)) * 0.5
                     + AbbyNoise(p * 2.3 - float2(t * 0.8, t * 1.3)) * 0.3
                     + AbbyNoise(p * 5.1 + float2(t * 1.7, -t)) * 0.2;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                ws.y += (Waves(ws.xz * _WaveScale, _Time.y * _WaveSpeed) - 0.5) * 2.0 * _WaveHeight;
                o.positionWS = ws;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 p = i.positionWS.xz * _WaveScale;

                // normal a partir de la altura de las olas (diferencias finitas)
                const float e = 0.05;
                float h0 = Waves(p, t), hx = Waves(p + float2(e, 0), t), hz = Waves(p + float2(0, e), t);
                // a lo lejos las olas finas serían más chicas que un píxel (se ve "granulado"): se suavizan con la distancia
                float strength = _NormalStrength / (1.0 + i.screenPos.w * 0.04);
                float3 n = normalize(float3((h0 - hx) / e * strength, 1.0, (h0 - hz) / e * strength));

                // profundidad bajo la superficie
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float surfEye = i.screenPos.w;
                float depth = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams) - surfEye;

                // refracción: deforma el fondo, sin traer cosas que están delante del agua
                float2 ruv = suv + n.xz * _Refraction * saturate(depth);
                float depthR = LinearEyeDepth(SampleSceneDepth(ruv), _ZBufferParams) - surfEye;
                if (depthR < 0.0) { ruv = suv; depthR = depth; }
                float3 below = SampleSceneColor(ruv);

                float4 water = lerp(_ShallowColor, _DeepColor, saturate(depthR / _DepthDistance));
                float3 col = lerp(below, water.rgb, water.a);

                // luz del sol (con sombras) sobre el color del agua
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float sunShadow = lerp(0.55, 1.0, sun.shadowAttenuation);
                col *= sunShadow;

                // reflejo del cielo con fresnel
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float fres = pow(1.0 - saturate(dot(n, V)), 4.0);
                float3 R = reflect(-V, n);
                R.y = abs(R.y);
                float3 sky = AbbySkyColor(normalize(R), normalize(_MainLightPosition.xyz), false);
                col = lerp(col, sky * sunShadow, saturate(fres * _Reflection + 0.06));

                // brillo especular del sol
                float3 H = normalize(sun.direction + V);
                col += sun.color * pow(saturate(dot(n, H)), 300.0) * 5.0 * sun.shadowAttenuation;

                // reflejos de faroles y luces de colores
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = suv;
                uint lightsCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light a = GetAdditionalLight(lightIndex, i.positionWS);
                    float3 Ha = normalize(a.direction + V);
                    float att = a.distanceAttenuation;
                    col += a.color * att * (pow(saturate(dot(n, Ha)), 96.0) * 2.5 + saturate(dot(n, a.direction)) * 0.12);
                LIGHT_LOOP_END

                // espuma en las orillas
                float foam = 1.0 - saturate(depth / _FoamDistance);
                foam = smoothstep(0.35, 0.9, foam + (AbbyNoise(i.positionWS.xz * 7.0 + t * 0.8) - 0.5) * 0.5);
                col = lerp(col, _FoamColor.rgb * sunShadow, foam * 0.85);

                col = MixFog(col, i.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
