// Cascada: chorros que bajan (ruido estirado y desplazándose), espuma en el borde de arriba,
// iluminada por el sol y el ambiente, y que se desvanece en la bruma del abismo.
// UV del mesh: x = metros a lo largo del borde, y = metros de caída.
Shader "Abby/Waterfall"
{
    Properties
    {
        _Color ("Agua", Color) = (0.28, 0.62, 0.72, 0.85)
        _FoamColor ("Espuma", Color) = (1, 1, 1, 1)
        _Speed ("Velocidad", Float) = 6
        _StreakWidth ("Ancho de chorros", Float) = 1.2
        _FadeStart ("Empieza a desvanecerse (m)", Float) = 40
        _FadeEnd ("Desaparece (m)", Float) = 120
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "AbbySky.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _FoamColor;
                float _Speed, _StreakWidth, _FadeStart, _FadeEnd;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                // chorros: ruido muy estirado en vertical que baja; dos capas a distinta velocidad
                float2 p = float2(i.uv.x * 2.2 / _StreakWidth, i.uv.y * 0.045 - t * 0.09);
                float n = AbbyFbm(p);
                float n2 = AbbyNoise(float2(i.uv.x * 5.0 / _StreakWidth, i.uv.y * 0.12 - t * 0.35));
                float dark = smoothstep(0.35, 0.15, n2) * 0.35;   // vetas más oscuras entre chorros

                float foam = smoothstep(0.55, 0.85, n * 0.7 + n2 * 0.5);
                foam = max(foam, smoothstep(2.5, 0.0, i.uv.y));          // labio blanco donde el agua se rompe
                float3 col = lerp(_Color.rgb * (1.0 - dark), _FoamColor.rgb, foam);

                // luz: sol + ambiente (la cascada no tiene normales útiles; se ilumina "de frente")
                Light sun = GetMainLight();
                col *= sun.color * 0.55 + SampleSH(float3(0, 1, 0)) * 0.9 + 0.15;

                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, i.uv.y);
                float alpha = _Color.a * (0.55 + 0.45 * n) * fade;
                alpha = max(alpha, foam * fade * 0.9);

                col = MixFog(col, i.fog);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
