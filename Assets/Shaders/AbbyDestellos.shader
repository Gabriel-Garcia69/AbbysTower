// Reflejo falso de la luna sobre el agua: una franja aditiva (uv.x a lo ancho, uv.y a lo largo) con destellos que titilan.
// Se dibuja un poco por encima del agua; sin niebla.
Shader "Abby/Destellos"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.6, 0.72, 1.2, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
                float4 _Color;
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

            half4 frag(Varyings i) : SV_Target
            {
                float cx = abs(i.uv.x * 2.0 - 1.0);
                float columna = pow(saturate(1.0 - cx), 2.0) * smoothstep(0.0, 0.12, i.uv.y) * smoothstep(1.0, 0.7, i.uv.y);

                float2 g = float2(i.uv.x * 30.0, i.uv.y * 95.0);
                float2 id = floor(g);
                float h = Hash(id);
                float titileo = pow(saturate(sin(_Time.y * (1.5 + h * 2.5) + h * 40.0) * 0.5 + 0.5), 6.0);
                float forma = smoothstep(0.36, 0.0, length((frac(g) - 0.5) * float2(1.0, 0.45)));
                float chispa = step(0.55, h) * titileo * forma;

                float3 col = _Color.rgb * columna * (0.22 + 2.6 * chispa);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
