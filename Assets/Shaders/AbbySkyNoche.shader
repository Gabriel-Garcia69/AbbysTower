// Skybox de noche solo para el Exterior (el de siempre es Abby/Sky y lo usan Diorama y Cinematica): gradiente azul muy oscuro →
// violeta en el horizonte, estrellas estáticas (algunas más brillantes) y tres constelaciones sutiles.
// La luna, el planeta, las nubes y la aurora son mallas aparte (ExteriorBuilder.Noche.cs). La luz principal es la luna.
Shader "Abby/SkyNoche"
{
    Properties
    {
        _Zenith ("Cenit", Color) = (0.02, 0.027, 0.1, 1)
        _Mid ("Medio", Color) = (0.043, 0.063, 0.204, 1)
        _Horizon ("Horizonte", Color) = (0.165, 0.122, 0.369, 1)
        _Ground ("Bajo el horizonte", Color) = (0.04, 0.04, 0.11, 1)
        [HDR] _MoonGlow ("Resplandor de la luna", Color) = (0.25, 0.36, 0.8, 1)
        _StarDensity ("Densidad de estrellas", Range(0.8, 0.99)) = 0.93
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Zenith, _Mid, _Horizon, _Ground, _MoonGlow;
                float _StarDensity;
            CBUFFER_END

            // constelaciones (direcciones unitarias; se dibujan como nodos brillantes y líneas muy tenues)
            static const float3 CP[19] = {
                float3(-0.03166, 0.86603, -0.49900), float3(-0.21534, 0.87462, -0.43436), float3(-0.10825, 0.78801, -0.60607),
                float3(-0.15827, 0.79122, -0.59069), float3(-0.20928, 0.78801, -0.57900), float3(-0.02560, 0.68200, -0.73091),
                float3(-0.34882, 0.66913, -0.65619), float3(-0.49674, 0.74314, 0.44831), float3(-0.37846, 0.79864, 0.46792),
                float3(-0.34100, 0.73135, 0.59063), float3(-0.21600, 0.79864, 0.56172), float3(-0.13988, 0.74314, 0.65435),
                float3(0.73605, 0.65606, 0.16677), float3(0.77401, 0.62932, 0.06979), float3(0.79846, 0.60182, -0.01663),
                float3(0.82187, 0.55919, -0.10876), float3(0.79067, 0.57358, -0.21411), float3(0.72392, 0.65606, -0.21335),
                float3(0.73438, 0.66913, -0.11378)
            };
            static const int2 CE[18] = { int2(0, 2), int2(2, 5), int2(1, 4), int2(4, 6), int2(2, 3), int2(3, 4), int2(0, 1), int2(7, 8), int2(8, 9),
                                         int2(9, 10), int2(10, 11), int2(12, 13), int2(13, 14), int2(14, 15), int2(15, 16), int2(16, 17), int2(17, 18), int2(18, 15) };

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float3 Hash33(float3 p)
            {
                float3 q = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(q) * 43758.5453);
            }

            // distancia angular aproximada de d al arco A→B (0 si d no cae entre los extremos)
            float ArcLine(float3 d, float3 a, float3 b)
            {
                float3 n = normalize(cross(a, b));
                bool dentro = dot(cross(a, d), n) > 0.0 && dot(cross(d, b), n) > 0.0;
                return dentro ? abs(dot(d, n)) : 1.0;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                float3 moon = normalize(_MainLightPosition.xyz);
                float moonA = saturate(dot(d, moon));

                // degradado: cenit muy oscuro → medio azul → horizonte violeta; abajo, casi negro
                float3 col = lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0.0, 0.28, y));
                col = lerp(col, _Zenith.rgb, smoothstep(0.22, 0.9, y));
                col = lerp(col, _Ground.rgb, saturate(-y * 7.0));
                col += _MoonGlow.rgb * (pow(moonA, 10.0) * 0.35 + pow(moonA, 90.0) * 0.5);

                if (y > 0.015)
                {
                    float fade = saturate(y * 5.0) * (1.0 - 0.85 * pow(moonA, 30.0));   // se apagan en el horizonte y junto a la luna

                    // estrellas: una por celda de ~1°, la mayoría chicas, unas pocas grandes y de color
                    float3 p = d * 64.0;
                    float3 id = floor(p);
                    float3 f = frac(p) - 0.5;
                    float3 c = (Hash33(id) - 0.5) * 0.6;
                    float dist = length(f - c);
                    float sel = Hash13(id + 7.1);
                    float chica = step(_StarDensity, sel);
                    float grande = step(0.992, sel);
                    float r = lerp(0.055, 0.12, grande);
                    float brillo = lerp(0.35, 1.0, Hash13(id + 3.3)) * lerp(1.0, 2.2, grande);
                    float3 tinte = Hash13(id + 9.7) > 0.86 ? float3(1.0, 0.9, 0.72) : float3(0.82, 0.9, 1.0);
                    col += tinte * chica * smoothstep(r, r * 0.25, dist) * brillo * fade;

                    // constelaciones: nodos algo más brillantes y líneas apenas visibles
                    float lineas = 0.0, nodos = 0.0;
                    for (int k = 0; k < 18; k++)
                        lineas = max(lineas, 1.0 - smoothstep(0.0006, 0.0017, ArcLine(d, CP[CE[k].x], CP[CE[k].y])));
                    for (int j = 0; j < 19; j++)
                        nodos = max(nodos, 1.0 - smoothstep(0.0025, 0.006, length(d - CP[j])));
                    col += (float3(0.5, 0.62, 1.0) * lineas * 0.22 + float3(0.9, 0.95, 1.0) * nodos * 1.3) * fade;
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
