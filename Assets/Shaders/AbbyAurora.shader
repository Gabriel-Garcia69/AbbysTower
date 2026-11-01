// Aurora: cintas curvas (malla en arco, uv.x a lo largo y uv.y de abajo arriba) con ruido animado, aditivas y tenues,
// de cian a violeta. Sin niebla: es parte del cielo.
Shader "Abby/Aurora"
{
    Properties
    {
        [HDR] _ColorA ("Cian", Color) = (0.25, 0.88, 0.82, 1)
        [HDR] _ColorB ("Violeta", Color) = (0.6, 0.36, 1, 1)
        _Intensity ("Intensidad", Range(0, 2)) = 0.3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB;
                float _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = i.uv.x, v = i.uv.y, time = _Time.y;
                float ondas = Noise(float2(t * 6.0 + time * 0.03, v * 1.6 + sin(t * 5.0 + time * 0.2) * 0.35));
                float rayos = Noise(float2(t * 42.0 + time * 0.05, 0.5));
                float cuerpo = smoothstep(0.0, 0.14, v) * pow(1.0 - v, 1.5);
                float extremos = smoothstep(0.0, 0.1, t) * smoothstep(1.0, 0.9, t);
                float a = cuerpo * extremos * (0.3 + 0.7 * ondas) * (0.55 + 0.45 * rayos);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, saturate(v * 1.3 + ondas * 0.35 - 0.1));
                return half4(col * a * _Intensity, 1.0);
            }
            ENDHLSL
        }
    }
}
