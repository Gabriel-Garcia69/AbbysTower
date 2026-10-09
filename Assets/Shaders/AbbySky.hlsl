#ifndef ABBY_SKY_INCLUDED
#define ABBY_SKY_INCLUDED

// Cielo compartido por el skybox (Abby/Sky) y el agua (Abby/Water), para que el reflejo coincida con el cielo.
// Los colores los publica ExteriorSky.cs como variables globales. El sol es la luz principal de la escena.

float4 _AbbySkyZenith;
float4 _AbbySkyHorizon;
float4 _AbbySkyGround;
float4 _AbbySunColor;
float4 _AbbyCloudColor;
float  _AbbyCloudCover;    // 0..1
float  _AbbyCloudSpeed;
float  _AbbySunSize;

float AbbyHash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

float AbbyNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(AbbyHash(i), AbbyHash(i + float2(1, 0)), u.x),
                lerp(AbbyHash(i + float2(0, 1)), AbbyHash(i + float2(1, 1)), u.x), u.y);
}

float AbbyFbm(float2 p)
{
    float v = 0.0, a = 0.5;
    for (int k = 0; k < 5; k++) { v += a * AbbyNoise(p); p = p * 2.03 + 17.1; a *= 0.5; }
    return v;
}

// dir: dirección de vista normalizada (mundo). sunDir: dirección HACIA el sol.
float3 AbbySkyColor(float3 dir, float3 sunDir, bool drawSunDisk)
{
    float y = dir.y;

    // degradado: suelo → horizonte → cenit (el horizonte se ensancha cerca del sol)
    float sunAlign = saturate(dot(dir, sunDir));
    float3 sky = lerp(_AbbySkyHorizon.rgb, _AbbySkyZenith.rgb, pow(saturate(y), 0.45));
    sky = lerp(sky, _AbbySkyGround.rgb, saturate(-y * 6.0));
    sky += _AbbySunColor.rgb * pow(sunAlign, 6.0) * 0.45;            // resplandor amplio
    sky += _AbbySunColor.rgb * pow(sunAlign, 64.0) * 0.8;            // halo

    // nubes: plano proyectado sobre el horizonte, se desplazan con el tiempo
    if (y > 0.0)
    {
        float2 uv = dir.xz / (y + 0.12) * 1.4 + _Time.y * _AbbyCloudSpeed * float2(1.0, 0.35);
        float n = AbbyFbm(uv);
        float cover = smoothstep(1.0 - _AbbyCloudCover, 1.0 - _AbbyCloudCover + 0.35, n);
        cover *= smoothstep(0.0, 0.18, y);                           // se desvanecen en el horizonte
        float lit = saturate(0.55 + pow(sunAlign, 3.0) * 0.8 - (n - 0.5) * 0.6);
        float3 cloud = lerp(_AbbyCloudColor.rgb * 0.55, _AbbyCloudColor.rgb + _AbbySunColor.rgb * 0.35, lit);
        sky = lerp(sky, cloud, cover * 0.9);
    }

    if (drawSunDisk)
    {
        float disk = smoothstep(1.0 - _AbbySunSize, 1.0 - _AbbySunSize * 0.6, sunAlign);
        sky += _AbbySunColor.rgb * disk * 6.0;
    }
    return sky;
}

#endif
