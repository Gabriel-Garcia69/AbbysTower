using UnityEngine;

/// <summary>
/// Casas distintas por mapa (antes todas eran la misma de entramado) y techos de verdad: dos losas con grosor,
/// alero que sobresale, tablas de borde en los hastiales y cumbrera. El frente de la casa es +Z.
/// - Pueblo y demás: entramado con tejado de teja.
/// - Puerto: casa de pescador encalada, puerta y contraventanas azules, teja de barro de poca pendiente y porche.
/// - Cascadas: casita de piedra con techo de paja grueso y empinado.
/// - Acantilado: zócalo alto de piedra, piso de tablas, techo de pizarra empinado y chimenea de piedra en el hastial.
/// Las ventanas gastan el rng del mapa igual que antes (para que no se muevan árboles ni nada más).
/// </summary>
public partial class DioramaBuilder
{
    Material mCal, mAzul, mPaja, mPizarra, mTejaBarro;

    void MaterialesCasas()
    {
        MaterialesFuego();
        if (mCal != null) return;
        mCal = Plain("Dio_Cal", new Color(0.97f, 0.95f, 0.9f), 0.05f);
        mAzul = Plain("Dio_AzulPuerto", new Color(0.16f, 0.36f, 0.6f), 0.25f);
        mPaja = Plain("Dio_Paja", new Color(0.74f, 0.6f, 0.33f), 0.02f);
        mPizarra = Plain("Dio_Pizarra", new Color(0.22f, 0.25f, 0.3f), 0.35f);
        mTejaBarro = TexMat("Dio_TejasBarro", RoofTexture(), null, new Color(1.05f, 0.78f, 0.62f), 0.1f);
    }

    void House(Transform parent, Vector3 pos, float yaw, float w, float d, float wallH, bool chimney)
    {
        MaterialesCasas();
        switch (layout)
        {
            case Layout.Puerto: CasaPescador(parent, pos, yaw, w, d, wallH, chimney); break;
            case Layout.Cascadas: CasaPiedra(parent, pos, yaw, w, d, wallH, chimney); break;
            case Layout.Acantilado: CasaPizarra(parent, pos, yaw, w, d, wallH, chimney); break;
            default: CasaEntramado(parent, pos, yaw, w, d, wallH, chimney); break;
        }
    }

    enum Cumbrera { Teja, Tabla, Paja }

    /// <summary>Techo a dos aguas con grosor (cumbrera a lo largo de X, a la altura top + roofH) y hastiales del material 'muro'.</summary>
    void Techo(Transform h, float w, float d, float top, float roofH, Material mat, Material muro, Material borde, float alero, float grosor, Cumbrera cumbrera)
    {
        float hz = d / 2f, a = Mathf.Atan2(roofH, hz);
        float largo = (hz + alero) / Mathf.Cos(a);
        Vector3 cima = new Vector3(0f, top + roofH + grosor * 0.5f / Mathf.Cos(a), 0f);
        foreach (float sz in new[] { -1f, 1f })
        {
            Vector3 abajo = new Vector3(0f, -Mathf.Sin(a), sz * Mathf.Cos(a));
            var losa = Box(h, "Tejado", cima + abajo * largo / 2f, new Vector3(w + alero * 1.6f, grosor, largo), mat, false);
            losa.transform.localRotation = Quaternion.Euler(sz * a * Mathf.Rad2Deg, 0f, 0f);
            // tabla de borde en el alero
            if (borde != null)
            {
                var tb = Box(h, "Alero", cima + abajo * (largo - 0.04f) + Vector3.down * grosor * 0.4f, new Vector3(w + alero * 1.6f + 0.02f, grosor + 0.1f, 0.08f), borde, false);
                tb.transform.localRotation = Quaternion.Euler(sz * a * Mathf.Rad2Deg, 0f, 0f);
            }
        }
        // hastiales (muro triangular bajo el techo) y tablas de borde inclinadas en cada hastial
        MeshObject(h, "Hastial", Gable(w, roofH, hz + 0.01f), muro, true).transform.localPosition = new Vector3(0f, top, 0f);
        if (borde != null)
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    Vector3 abajo = new Vector3(0f, -Mathf.Sin(a), sz * Mathf.Cos(a));
                    var b = Box(h, "Barbacana", cima + abajo * largo / 2f + new Vector3(sx * (w / 2f + alero * 0.8f + 0.03f), 0f, 0f), new Vector3(0.07f, grosor + 0.14f, largo), borde, false);
                    b.transform.localRotation = Quaternion.Euler(sz * a * Mathf.Rad2Deg, 0f, 0f);
                }
        // cumbrera
        switch (cumbrera)
        {
            case Cumbrera.Teja:
                var c = Prim(h, PrimitiveType.Cylinder, "Cumbrera", cima + Vector3.up * 0.04f, new Vector3(0.26f, (w + alero * 1.6f) / 2f + 0.05f, 0.26f), mat);
                c.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case Cumbrera.Paja:
                var p = Prim(h, PrimitiveType.Cylinder, "Cumbrera", cima + Vector3.down * 0.02f, new Vector3(0.6f, (w + alero * 1.6f) / 2f, 0.45f), mat);
                p.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            default:
                Box(h, "Cumbrera", cima + Vector3.up * 0.03f, new Vector3(w + alero * 1.6f + 0.05f, 0.12f, 0.3f), borde != null ? borde : mat, false);
                break;
        }
    }

    /// <summary>Gasta el rng igual que las ventanas de la casa de entramado (1 + 4 flores por ventana).</summary>
    void GastarComoVentanas(float w)
    {
        int n = 2;
        foreach (float x in new[] { -w * 0.25f, w * 0.38f }) if (Mathf.Abs(x - w * 0.15f) >= 0.9f) n++;
        for (int i = 0; i < n * 5; i++) rng.Next();
    }

    /// <summary>Ventana sin azar: vidrio, marco, alféizar y (opcional) contraventanas abiertas.</summary>
    void VentanaFija(Transform parent, Vector3 pos, float yaw, Material marco, Material contra, bool encendida, float ancho = 0.7f, float alto = 0.85f)
    {
        var t = Group("Ventana", parent);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(t, "Vidrio", Vector3.zero, new Vector3(ancho, alto, 0.04f), encendida ? mWindowLit : mWindow, false);
        Box(t, "Marco", new Vector3(0f, alto / 2f + 0.05f, 0.02f), new Vector3(ancho + 0.2f, 0.1f, 0.1f), marco, false);
        Box(t, "Alfeizar", new Vector3(0f, -alto / 2f - 0.05f, 0.06f), new Vector3(ancho + 0.25f, 0.08f, 0.18f), marco, false);
        Box(t, "Cruz", new Vector3(0f, 0f, 0.03f), new Vector3(0.05f, alto, 0.04f), marco, false);
        if (contra != null)
            foreach (float sx in new[] { -1f, 1f })
            {
                var c = Box(t, "Contraventana", new Vector3(sx * (ancho / 2f + 0.2f), 0f, 0.05f), new Vector3(ancho / 2f, alto + 0.05f, 0.05f), contra, false);
                c.transform.localRotation = Quaternion.Euler(0f, sx * -12f, 0f);
            }
    }

    void Colision(Transform h, Vector3 pos, float w, float d, float alto)
    {
        var col = h.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, alto / 2f, 0f); col.size = new Vector3(w + 0.3f, alto, d + 0.3f);
        float rad = Mathf.Max(w, d) / 2f + 1.2f;
        blocked.Add(new Rect(pos.x - rad, pos.z - rad, rad * 2f, rad * 2f));
    }

    // ------------------------------------------------------------------ puerto

    void CasaPescador(Transform parent, Vector3 pos, float yaw, float w, float d, float wallH, bool chimney)
    {
        var h = Group("Casa", parent);
        h.localPosition = pos;
        h.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var r = Azar(pos);
        float baseH = 0.35f, top = baseH + wallH, roofH = d * 0.32f;
        Box(h, "Zocalo", new Vector3(0f, baseH / 2f, 0f), new Vector3(w + 0.2f, baseH, d + 0.2f), mStone, false);
        Box(h, "Muros", new Vector3(0f, baseH + wallH / 2f, 0f), new Vector3(w, wallH, d), mCal, false);
        // franja azul abajo y esquinas de piedra
        Box(h, "Franja", new Vector3(0f, baseH + 0.2f, 0f), new Vector3(w + 0.02f, 0.4f, d + 0.02f), mAzul, false);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                for (float y = baseH + 0.5f; y < top - 0.2f; y += 0.55f)
                    Box(h, "Sillar", new Vector3(sx * (w / 2f - 0.12f), y, sz * (d / 2f - 0.12f)), new Vector3(0.3f, 0.26f, 0.3f), mStone, false);
        Techo(h, w, d, top, roofH, mTejaBarro, mCal, mAzul, 0.45f, 0.16f, Cumbrera.Teja);
        // puerta azul con porche de madera
        float px = w * 0.18f;
        Box(h, "Puerta", new Vector3(px, baseH + 0.95f, d / 2f + 0.05f), new Vector3(1f, 1.9f, 0.1f), mAzul, false);
        Box(h, "Dintel", new Vector3(px, baseH + 1.97f, d / 2f + 0.08f), new Vector3(1.3f, 0.14f, 0.12f), mStone, false);
        var porche = Box(h, "Porche", new Vector3(px, baseH + 2.35f, d / 2f + 0.65f), new Vector3(2.2f, 0.08f, 1.3f), mTejaBarro, false);
        porche.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        foreach (float sx in new[] { -1f, 1f })
            Box(h, "Poste", new Vector3(px + sx * 0.95f, baseH + 1.1f, d / 2f + 1.2f), new Vector3(0.12f, 2.2f, 0.12f), mTimber, false);
        Box(h, "Escalon", new Vector3(px, 0.1f, d / 2f + 0.5f), new Vector3(1.4f, 0.2f, 0.7f), mStone, false);
        // ventanas con contraventanas azules
        foreach (float x in new[] { -w * 0.28f, w * 0.42f })
            if (Mathf.Abs(x - px) > 1f) VentanaFija(h, new Vector3(x, baseH + wallH * 0.58f, d / 2f + 0.04f), 0f, mCal, mAzul, r.NextDouble() < 0.35);
        VentanaFija(h, new Vector3(w / 2f + 0.04f, baseH + wallH * 0.58f, 0f), 90f, mCal, mAzul, r.NextDouble() < 0.35);
        VentanaFija(h, new Vector3(-w / 2f - 0.04f, baseH + wallH * 0.58f, 0f), -90f, mCal, mAzul, r.NextDouble() < 0.35);
        GastarComoVentanas(w);
        // redes y boya colgadas del costado
        var red = Box(h, "Red", new Vector3(w / 2f + 0.05f, baseH + 1.2f, -d * 0.28f), new Vector3(0.03f, 1.1f, 1.2f), mCuerda, false);
        red.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Prim(h, PrimitiveType.Sphere, "Boya", new Vector3(w / 2f + 0.15f, baseH + 0.9f, -d * 0.1f), Vector3.one * 0.3f, mCloth);
        if (chimney)
        {
            Box(h, "Chimenea", new Vector3(-w * 0.3f, top + roofH * 0.75f, -d * 0.12f), new Vector3(0.55f, roofH * 1.1f + 0.5f, 0.55f), mCal, false);
            Box(h, "Remate", new Vector3(-w * 0.3f, top + roofH * 1.33f + 0.3f, -d * 0.12f), new Vector3(0.7f, 0.1f, 0.7f), mTejaBarro, false);
            Smoke(h, new Vector3(-w * 0.3f, top + roofH * 1.35f + 0.5f, -d * 0.12f), new Color(0.85f, 0.85f, 0.88f, 0.3f), false);
        }
        Colision(h, pos, w, d, top + roofH);
    }

    // ------------------------------------------------------------------ cascadas

    void CasaPiedra(Transform parent, Vector3 pos, float yaw, float w, float d, float wallH, bool chimney)
    {
        var h = Group("Casa", parent);
        h.localPosition = pos;
        h.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var r = Azar(pos);
        wallH *= 0.85f;   // casita baja con mucho techo
        float top = wallH, roofH = d * 0.62f;
        Box(h, "Muros", new Vector3(0f, wallH / 2f, 0f), new Vector3(w, wallH, d), mStone, false);
        // piedras de esquina más grandes
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                for (float y = 0.25f; y < wallH; y += 0.5f)
                    Box(h, "Esquina", new Vector3(sx * (w / 2f - 0.15f), y, sz * (d / 2f - 0.15f)), new Vector3(0.42f + ((int)(y * 2f) % 2) * 0.12f, 0.36f, 0.42f), mCliff, false);
        Techo(h, w, d, top, roofH, mPaja, mStone, null, 0.7f, 0.38f, Cumbrera.Paja);
        // puerta de tablas con dintel de madera
        float px = w * 0.12f;
        Box(h, "Puerta", new Vector3(px, 0.9f, d / 2f + 0.05f), new Vector3(0.95f, 1.8f, 0.1f), mDoor, false);
        Box(h, "Dintel", new Vector3(px, 1.9f, d / 2f + 0.1f), new Vector3(1.4f, 0.22f, 0.2f), mTimber, false);
        Box(h, "Escalon", new Vector3(px, 0.08f, d / 2f + 0.45f), new Vector3(1.3f, 0.16f, 0.6f), mCliff, false);
        foreach (float x in new[] { -w * 0.3f, w * 0.4f })
            if (Mathf.Abs(x - px) > 1f) VentanaFija(h, new Vector3(x, wallH * 0.6f, d / 2f + 0.04f), 0f, mTimber, null, r.NextDouble() < 0.5, 0.6f, 0.7f);
        VentanaFija(h, new Vector3(w / 2f + 0.04f, wallH * 0.6f, 0f), 90f, mTimber, null, r.NextDouble() < 0.5, 0.6f, 0.7f);
        VentanaFija(h, new Vector3(-w / 2f - 0.04f, wallH * 0.6f, 0f), -90f, mTimber, null, r.NextDouble() < 0.5, 0.6f, 0.7f);
        GastarComoVentanas(w);
        // leña apilada junto a la casa
        for (int i = 0; i < 9; i++)
        {
            var l = Prim(h, PrimitiveType.Cylinder, "Leña", new Vector3(-w / 2f - 0.45f, 0.15f + (i / 3) * 0.26f, -d * 0.2f + (i % 3) * 0.27f - ((i / 3) % 2) * 0.13f), new Vector3(0.26f, 0.45f, 0.26f), mBark);
            l.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        if (chimney)
        {
            Box(h, "Chimenea", new Vector3(w * 0.28f, top + roofH * 0.8f, -d * 0.18f), new Vector3(0.7f, roofH * 1.1f + 0.6f, 0.7f), mCliff, false);
            Smoke(h, new Vector3(w * 0.28f, top + roofH * 1.4f + 0.4f, -d * 0.18f), new Color(0.85f, 0.85f, 0.88f, 0.3f), false);
        }
        Colision(h, pos, w, d, top + roofH);
    }

    // ------------------------------------------------------------------ acantilado

    void CasaPizarra(Transform parent, Vector3 pos, float yaw, float w, float d, float wallH, bool chimney)
    {
        var h = Group("Casa", parent);
        h.localPosition = pos;
        h.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var r = Azar(pos);
        float baseH = 1.5f, top = baseH + wallH * 0.75f, roofH = d * 0.72f;
        Box(h, "Zocalo", new Vector3(0f, baseH / 2f, 0f), new Vector3(w + 0.25f, baseH, d + 0.25f), mCliff, false);
        Box(h, "Muros", new Vector3(0f, baseH + (top - baseH) / 2f, 0f), new Vector3(w, top - baseH, d), mPlank, false);
        Box(h, "Cornisa", new Vector3(0f, baseH + 0.05f, 0f), new Vector3(w + 0.4f, 0.12f, d + 0.4f), mTimber, false);
        // tablas verticales oscuras en las esquinas
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Box(h, "Esquinero", new Vector3(sx * w / 2f, baseH + (top - baseH) / 2f, sz * d / 2f), new Vector3(0.2f, top - baseH, 0.2f), mTimber, false);
        Techo(h, w, d, top, roofH, mPizarra, mPlank, mTimber, 0.35f, 0.14f, Cumbrera.Tabla);
        float px = -w * 0.15f;
        Box(h, "Puerta", new Vector3(px, 1f, d / 2f + 0.1f), new Vector3(1f, 2f, 0.12f), mDoor, false);
        Box(h, "Dintel", new Vector3(px, 2.05f, d / 2f + 0.14f), new Vector3(1.4f, 0.2f, 0.2f), mCliff, false);
        Box(h, "Escalon", new Vector3(px, 0.1f, d / 2f + 0.5f), new Vector3(1.4f, 0.2f, 0.7f), mCliff, false);
        // ventanas chicas (viento de mar) en el piso de madera
        foreach (float x in new[] { -w * 0.32f, w * 0.3f })
            VentanaFija(h, new Vector3(x, baseH + (top - baseH) * 0.55f, d / 2f + 0.04f), 0f, mTimber, mDoor, r.NextDouble() < 0.4, 0.55f, 0.65f);
        VentanaFija(h, new Vector3(w / 2f + 0.04f, baseH + (top - baseH) * 0.55f, 0f), 90f, mTimber, mDoor, r.NextDouble() < 0.4, 0.55f, 0.65f);
        GastarComoVentanas(w);
        if (chimney)
        {
            // chimenea grande de piedra pegada al hastial
            float cx = -w / 2f - 0.35f;
            Box(h, "Chimenea", new Vector3(cx, (top + roofH + 0.8f) / 2f, 0f), new Vector3(0.8f, top + roofH + 0.8f, 1.1f), mCliff, false);
            Smoke(h, new Vector3(cx, top + roofH + 1.1f, 0f), new Color(0.8f, 0.8f, 0.84f, 0.3f), false);
        }
        Colision(h, pos, w, d, top + roofH);
    }
}
