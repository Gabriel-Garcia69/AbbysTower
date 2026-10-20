// Cortina de energía (la barrera que se cruza con impulso): aditiva y translúcida, con bandas que suben y chispas que
// corren. Se desvanece arriba, abajo y en las orillas para que no se vea como una pared plana. Muy barata: sin texturas.
Shader "Abby/Energia"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.6, 0.3, 1.2, 1)
        _Bandas ("Bandas por metro", Float) = 1.4
        _Velocidad ("Velocidad", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Bandas, _Velocidad;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 posWS : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float hash (float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            half4 frag (Varyings i) : SV_Target
            {
                float t = _Time.y * _Velocidad;
                float y = i.posWS.y, x = i.posWS.x + i.posWS.z;
                // bandas horizontales que suben, onduladas a lo largo
                float onda = sin(x * 0.6 + t * 1.3) * 0.25;
                float b = frac(y * _Bandas - t + onda);
                float banda = smoothstep(0.0, 0.08, b) * smoothstep(0.35, 0.1, b);
                // hilos verticales que parpadean
                float col = floor(x * 2.0);
                float hilo = step(0.93, hash(float2(col, floor(t * 2.0)))) * (1.0 - abs(frac(x * 2.0) - 0.5) * 2.0);
                // se apaga arriba, abajo y en las orillas
                float borde = smoothstep(0.0, 0.25, i.uv.y) * smoothstep(1.0, 0.55, i.uv.y) * smoothstep(0.0, 0.08, i.uv.x) * smoothstep(1.0, 0.92, i.uv.x);
                float a = (0.12 + banda * 0.55 + hilo * 0.6) * borde;
                float3 c = _Color.rgb * a;
                c = MixFogColor(c, 0, i.fog);   // aditivo: la niebla lo apaga hacia negro
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
