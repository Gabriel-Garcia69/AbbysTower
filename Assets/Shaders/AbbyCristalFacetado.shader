// Cristal facetado de la gruta: cada cara tiene su propio brillo (alfa del color de vértice) y un gradiente de oscuro en la base a
// claro en la punta (uv.y = altura de 0 a 1); las aristas verticales brillan fino (uv.x = ancho de la cara de 0 a 1).
// Sin luz (es emisor) pero con niebla. El color de vértice (rgb) es el color del cristal, ya poco saturado.
Shader "Abby/CristalFacetado"
{
    Properties
    {
        _Aristas ("Brillo de aristas", Range(0, 3)) = 1.4
        _Base ("Brillo de la base", Range(0, 1)) = 0.28
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Aristas, _Base;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float g = saturate(i.uv.y);
                float3 baseCol = i.color.rgb;
                float3 cara = baseCol * lerp(_Base, 1.0, g) * i.color.a;                       // gradiente oscuro→claro, por cara
                float borde = 1.0 - smoothstep(0.0, 0.07, min(i.uv.x, 1.0 - i.uv.x));          // aristas finas
                float3 emi = baseCol * 1.6 * borde * lerp(0.35, 1.0, g) * _Aristas;
                float3 col = cara + emi;
                col = MixFog(col, i.fog);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
