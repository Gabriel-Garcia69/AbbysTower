using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lo que hace única la gruta del piso 20: la bóveda de roca con estalactitas (es una cueva, tiene techo), el esqueleto
/// de una criatura enorme cuyas costillas forman un arco sobre el camino, runas de energía en el suelo (lo que dejaron
/// los canalizadores), pilares con bandas de runas y cadenas que bajan al abismo, y la escalera enorme rota que se hunde
/// en la oscuridad (la misma de la torre en 2030). Las piezas iguales se funden en una sola malla por material.
/// </summary>
public partial class DioramaBuilder
{
    const float ExpTecho = 30f;

    void GrutaUnica(Transform g)
    {
        var hueso = Plain("Dio_Hueso", new Color(0.72f, 0.67f, 0.56f), 0.22f);
        var hierro = Plain("Dio_HierroViejo", new Color(0.16f, 0.15f, 0.15f), 0.45f);
        var runa = Glow("Dio_Runa", new Color(0.3f, 0.85f, 1f), 3.2f);
        var runaRoja = Glow("Dio_RunaRoja", new Color(1f, 0.22f, 0.12f), 3f);

        // ---- la bóveda: techo de roca, paredes que suben hasta él y estalactitas
        var boveda = Group("Boveda", g);
        var techo = Flat(boveda, "Techo", new Vector3(0f, ExpTecho, 40f), 90f, 260f, mCaveRock, 0.25f);   // UV en metros (antes: una sola repetición en todo el techo)
        techo.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);   // la cara mira hacia abajo: desde arriba no tapa
        // al fondo, la pared del abismo y los costados del vacío (sin ellos se veía un hueco negro)
        Wall(boveda, "ParedAbismo", new Vector3(0f, (ExpTecho - 46f) / 2f, ExpEdgeZ + 50f), Vector3.back, 90f, ExpTecho + 46f, mCaveRock, 0.25f);
        foreach (float sx in new[] { -1f, 1f })
            Wall(boveda, "CostadoAbismo", new Vector3(sx * 26f, -16f, ExpEdgeZ + 25f), new Vector3(-sx, 0f, 0f), 52f, 62f, mCaveRock, 0.25f);
        var colgantes = Group("Estalactitas", boveda);
        for (int i = 0; i < 70; i++)
        {
            float x = R(-26f, 26f), z = R(-70f, 175f), h = R(2.5f, Mathf.Abs(x) > 16f ? 11f : 7f);
            var m = MeshObject(colgantes, "Estalactita", ConoMetrico(h * 0.26f, h, 11, 0.25f), mCaveRock, false);
            m.transform.localPosition = new Vector3(x, ExpTecho + 0.3f, z);
            m.transform.localRotation = Quaternion.Euler(180f + R(-6f, 6f), R(0f, 360f), R(-6f, 6f));   // cuelga
            if (i % 12 == 0)   // solo unos pocos cristales brillan, y van pegados a la estalactita (a 55 % de su largo), no flotando
            {
                bool vio = i % 24 == 0;
                var c = MeshObject(colgantes, "Cristal", CristalFacetadoMesh(1f, 0.18f, vio ? new Color(0.58f, 0.46f, 0.86f) : new Color(0.38f, 0.72f, 0.82f)), mExpCristal, false);
                float a = R(0f, Mathf.PI * 2f), rad = h * 0.26f * 0.45f * 0.8f;   // radio de la estalactita a esa altura (poco menos, para que se hunda)
                c.transform.localPosition = new Vector3(x + Mathf.Cos(a) * rad, ExpTecho + 0.3f - h * 0.55f, z + Mathf.Sin(a) * rad);
                float hc = Mathf.Min(R(0.6f, 1.2f), h * 0.4f);   // pequeños
                c.transform.localScale = new Vector3(hc, hc, hc);
                c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Cos(a) * 0.35f, -1f, Mathf.Sin(a) * 0.35f).normalized);   // la punta mira abajo y un poco afuera
            }
        }

        // ---- el esqueleto: costillas pegadas a las paredes y la columna a lo largo de la pared este.
        // Antes las costillas hacían arcos de pared a pared sobre el sendero y la columna iba encima: con la cámara girada (±90°) quedaban
        // entre ella y Dante y tapaban la pantalla. Ahora solo quedan los tramos con |x| >= 14 (la cámara llega a ~13 m del centro) y la
        // columna y el cráneo van contra la pared, fuera de la franja del sendero (±8 m).
        var esq = Group("Esqueleto", g);
        for (int k = 0; k < 9; k++)
        {
            float z = 40f + k * 9f, s = 1f - Mathf.Abs(k - 4f) * 0.06f;   // más grandes al centro, como un tórax
            float rx = 17f * s, ry = 14f * s;
            bool rota = k == 2 || k == 6;   // un par roto, caído a medias
            int segs = 16;
            var arco = Group("Arco" + k, esq);   // una malla por arco: la oclusión suave lo atenúa entero
            for (int j = 0; j < segs; j++)
            {
                if (rota && j > 5 && j < 11) continue;
                float a0 = Mathf.Lerp(8f, 172f, j / (float)segs) * Mathf.Deg2Rad, a1 = Mathf.Lerp(8f, 172f, (j + 1) / (float)segs) * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * rx, Mathf.Sin(a0) * ry - 1.2f, z), p1 = new Vector3(Mathf.Cos(a1) * rx, Mathf.Sin(a1) * ry - 1.2f, z);
                if (Mathf.Min(Mathf.Abs(p0.x), Mathf.Abs(p1.x)) < 14f) continue;   // nada sobre el camino ni al alcance de la cámara
                float grosor = Mathf.Lerp(0.85f, 0.5f, Mathf.Sin(a0)) * s;
                var seg = Prim(arco, PrimitiveType.Capsule, "Costilla", (p0 + p1) / 2f, new Vector3(grosor, (Vector3.Distance(p0, p1) + grosor) / 2f, grosor * 0.8f), hueso);
                seg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, p1 - p0);
            }
        }
        var columna = Group("Columna", esq);   // la columna, tendida contra la pared este
        for (int k = 0; k < 9; k++)
        {
            float z = 40f + k * 9f, s = 1f - Mathf.Abs(k - 4f) * 0.06f;
            Prim(columna, PrimitiveType.Sphere, "Vertebra", new Vector3(17.2f, 0.7f * s, z), new Vector3(2.4f, 1.6f, 2.6f) * s, hueso);
            Prim(columna, PrimitiveType.Capsule, "Apofisis", new Vector3(17.2f, 1.7f * s, z), new Vector3(0.6f, 1.2f, 1f) * s, hueso);
            if (k < 8) Prim(columna, PrimitiveType.Cylinder, "Columna", new Vector3(17.2f, 0.7f * s, z + 4.5f), new Vector3(1.2f, 4.5f, 1.2f) * s, hueso)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        // el cráneo, partido, junto a la entrada de la oleada
        var craneo = Group("Craneo", esq);
        craneo.localPosition = new Vector3(19f, 0f, 112f);   // contra la pared, no a 14 m (donde pasa la cámara girada)
        craneo.localRotation = Quaternion.Euler(0f, -35f, 18f);
        Prim(craneo, PrimitiveType.Sphere, "Boveda", new Vector3(0f, 2.6f, 0f), new Vector3(6f, 4.6f, 7f), hueso);
        Prim(craneo, PrimitiveType.Sphere, "Mandibula", new Vector3(0f, 0.7f, -3.4f), new Vector3(4.4f, 1.4f, 5.4f), hueso);
        foreach (float sx in new[] { -1f, 1f })
        {
            Prim(craneo, PrimitiveType.Sphere, "Cuenca", new Vector3(sx * 1.6f, 2.8f, -3f), new Vector3(1.5f, 1.3f, 1f), Plain("Dio_Negro", Color.black, 0f));
            Prim(craneo, PrimitiveType.Capsule, "Colmillo", new Vector3(sx * 1.4f, 0.2f, -5.6f), new Vector3(0.45f, 1.2f, 0.45f), hueso).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        }

        // ---- runas en el suelo: anillos de energía bajo las dos peleas y en la barrera
        var runas = Group("Runas", g);
        RunaCirculo(runas, new Vector3(0f, 0.03f, 30f), 9f, runa);
        RunaCirculo(runas, new Vector3(0f, 0.03f, 78f), 11f, runa);
        RunaCirculo(runas, new Vector3(0f, 0.03f, ExpEdgeZ - 6f), 6f, runaRoja);   // donde la expedición se detuvo
        // la barrera: una hilera de runas pequeñas y tenues apoyadas en el suelo (no una línea): trazo largo + dos marcas, cada una distinta
        var barrera = Group("RunasBarrera", g);
        var runaTenue = Glow("Dio_RunaBarrera", new Color(0.35f, 0.8f, 1f), 1.1f);
        for (float x = -20f; x <= 20f; x += 2.4f)
        {
            var c = new Vector3(x + R(-0.25f, 0.25f), 0.025f, ExpVeilZ + R(-0.2f, 0.2f));
            float yaw = R(-12f, 12f);
            var m1 = Prim(barrera, PrimitiveType.Cube, "Trazo", c, new Vector3(0.07f, 0.012f, R(0.7f, 1.1f)), runaTenue);
            m1.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var m2 = Prim(barrera, PrimitiveType.Cube, "Marca", c + new Vector3(0.18f, 0f, R(0.05f, 0.25f)), new Vector3(0.3f, 0.012f, 0.06f), runaTenue);
            m2.transform.localRotation = Quaternion.Euler(0f, yaw + R(20f, 50f), 0f);
            if (rng.NextDouble() < 0.6)
            {
                var m3 = Prim(barrera, PrimitiveType.Cube, "Marca", c + new Vector3(-0.16f, 0f, -R(0.05f, 0.25f)), new Vector3(0.26f, 0.012f, 0.06f), runaTenue);
                m3.transform.localRotation = Quaternion.Euler(0f, yaw - R(20f, 50f), 0f);
            }
        }
        Fundir(barrera);
        barrera.gameObject.AddComponent<RunaPulso>();

        // ---- pilares con bandas de runas, del suelo al techo, y cadenas que bajan al abismo
        var pil = Group("Pilares", g);
        foreach (float z in new[] { 14f, 54f, 96f })
            foreach (float sx in new[] { -1f, 1f })
            {
                var p = new Vector3(sx * 18.5f, 0f, z);
                var unPilar = Group("PilarRoca", g);   // fuera de "Pilares": cada pilar es su propia malla para poder atenuarlo
                MeshObject(unPilar, "Pilar", CilindroMetrico(1.3f, ExpTecho, 14, 0.25f), mExpParedAlt, false).transform.localPosition = p;   // UV en metros: no se estira en 30 m de alto
                MeshObject(unPilar, "Base", CilindroMetrico(1.8f, 1.2f, 14, 0.25f), mExpParedAlt, false).transform.localPosition = p;
                Fundir(unPilar);
                foreach (var mr in unPilar.GetComponentsInChildren<MeshRenderer>()) MarcarOcluible(mr);
                foreach (float y in new[] { 3.5f, 9f, 15f })
                    Prim(pil, PrimitiveType.Cylinder, "Banda", p + Vector3.up * y, new Vector3(2.75f, 0.18f, 2.75f), runa);
                Cadena(pil, p + new Vector3(-sx * 1.4f, 20f, 0f), new Vector3(-sx * 9f, ExpTecho - 2f, z + 18f), hierro);
            }
        foreach (float x in new[] { -14f, 14f })   // las cadenas de la criatura de abajo
            Cadena(pil, new Vector3(x, ExpTecho, ExpEdgeZ + 12f), new Vector3(x * 0.8f, -34f, ExpEdgeZ + 16f), hierro);

        // ---- la escalera enorme que baja al abismo, rota
        var esc = Group("EscaleraAbismo", g);
        for (int i = 0; i < 22; i++)
        {
            if (i == 7 || i == 8 || i == 14) continue;   // escalones que ya cayeron
            float z = ExpEdgeZ + 1.5f + i * 2.4f, y = -1.2f - i * 1.6f;
            float ancho = 12f - (i % 3) * 0.7f, x = EscX(i) + (i % 2 == 0 ? 0.3f : -0.3f);
            Box(esc, "Escalon", new Vector3(x, y, z), new Vector3(ancho, 1.6f, 2.6f), mExpParedAlt, false, 0.25f)
                .transform.localRotation = Quaternion.Euler(R(-3f, 3f), R(-7f, 7f), R(-4f, 4f));   // gastados, ninguno derecho
            if (i % 2 == 1) Prim(esc, PrimitiveType.Sphere, "Escombro", new Vector3(x + R(-4.5f, 4.5f), y + 0.9f, z + R(-0.6f, 0.6f)), new Vector3(R(1f, 2.2f), R(0.7f, 1.3f), R(1f, 2f)), mExpParedAlt);
            if (i % 3 == 0 && i < 18)   // los muros de la escalera, con runas rojas que se apagan al bajar
                foreach (float sx in new[] { -1f, 1f })
                {
                    {
                    float hr = R(5f, 8f);
                    var roca = MeshObject(esc, "Roca", ConoMetrico(hr * 0.46f, hr, 11, 0.25f), mExpParedAlt, true);   // peñascos de roca a los lados de la escalera
                    roca.transform.localPosition = new Vector3(EscX(i) + sx * 7.4f, y - 0.8f, z);
                    roca.transform.localRotation = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), sx * R(4f, 12f));
                }
                    if (i < 12) Prim(esc, PrimitiveType.Cube, "Runa", new Vector3(EscX(i) + sx * 5.95f, y + 2.5f, z), new Vector3(0.05f, 0.9f, 2.4f), runaRoja);
                }
        }

        Fundir(boveda);
        foreach (Transform parte in esq)   // arcos, columna y cráneo, cada uno una malla que la oclusión suave puede atenuar
        {
            Fundir(parte);
            foreach (var mr in parte.GetComponentsInChildren<MeshRenderer>()) MarcarOcluible(mr);
        }
        Fundir(runas); Fundir(pil); Fundir(esc);
    }

    /// <summary>La escalera baja en diagonal hacia la izquierda: el centro queda libre para la criatura.</summary>
    static float EscX(int i) => -8f - i * 1.5f;

    void RunaCirculo(Transform parent, Vector3 c, float r, Material mat)
    {
        int n = Mathf.RoundToInt(r * 4f);
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            var d = Prim(parent, PrimitiveType.Cube, "Trazo", c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), new Vector3(0.18f, 0.05f, r * 6.28f / n * 0.7f), mat);
            d.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
        }
        for (int i = 0; i < 8; i++)   // los glifos de adentro
        {
            float a = (i + 0.5f) / 8f * Mathf.PI * 2f, rr = r * 0.78f;
            var p = c + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
            Prim(parent, PrimitiveType.Cube, "Glifo", p, new Vector3(0.16f, 0.05f, 1.1f), mat).transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            Prim(parent, PrimitiveType.Cube, "Glifo", p, new Vector3(0.7f, 0.05f, 0.16f), mat).transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 35f, 0f);
        }
    }

    void Cadena(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        float largo = Vector3.Distance(a, b);
        int n = Mathf.Max(2, Mathf.RoundToInt(largo / 0.95f));
        var rot = Quaternion.FromToRotation(Vector3.up, b - a);
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            var p = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * largo * 0.06f;   // cuelga un poco
            var l = Prim(parent, PrimitiveType.Cube, "Eslabon", p, new Vector3(0.5f, 1.1f, 0.14f), mat);
            l.transform.localRotation = rot * Quaternion.Euler(0f, i % 2 == 0 ? 0f : 90f, 0f);
        }
    }

    /// <summary>Junta todas las mallas hijas en una por material (menos objetos que dibujar y menos memoria).</summary>
    void Fundir(Transform grp)
    {
        var porMat = new Dictionary<Material, List<CombineInstance>>();
        var quitar = new List<GameObject>();
        var inv = grp.worldToLocalMatrix;
        foreach (var mf in grp.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mf.sharedMesh == null || mr.sharedMaterial == null || mf.GetComponent<Collider>() != null) continue;
            if (!porMat.TryGetValue(mr.sharedMaterial, out var lista)) porMat[mr.sharedMaterial] = lista = new List<CombineInstance>();
            lista.Add(new CombineInstance { mesh = mf.sharedMesh, transform = inv * mf.transform.localToWorldMatrix });
            quitar.Add(mf.gameObject);
        }
        foreach (var go in quitar) DestroyImmediate(go);
        int k = 0;
        foreach (var par in porMat)
        {
            var m = new Mesh { name = grp.name + "_" + k++, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.CombineMeshes(par.Value.ToArray(), true, true);
            m.RecalculateBounds();
            bool brilla = par.Key.IsKeywordEnabled("_EMISSION") || par.Key.shader.name.Contains("Unlit");
            MeshObject(grp, grp.name, m, par.Key, !brilla);
        }
    }
}
