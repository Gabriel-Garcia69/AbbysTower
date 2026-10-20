using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escondites detrás de muros falsos y reliquias anamórficas.
///
/// Muro falso: un tramo del muro norte se ve igual que los demás pero no tiene colisión. Con la cámara normal (mirando
/// al norte) el muro tapa el escondite; solo al girar la cámara hacia el sur (el muro queda de espaldas y se vuelve
/// invisible) se alcanza a ver el cuartito de atrás. Pista sutil: corre un poco de polvo por la base del muro.
///
/// Reliquia anamórfica: piezas flotando que parecen basura mágica… hasta que la cámara mira exactamente desde el ángulo
/// correcto, parada en el punto correcto. Ahí forman un símbolo y aparece la reliquia.
/// </summary>
public partial class FloorLayout
{
    struct Niche { public int room; public float offset, width; }

    readonly List<Niche> niches = new List<Niche>();

    /// <summary>Elige dónde va el muro falso de este piso y abre el hueco (antes de construir las salas).</summary>
    void AddSecretGaps(List<Gap>[] gaps)
    {
        niches.Clear();
        int pick = -1;
        // preferimos una sala de combate sin puerta en el muro norte; si no, la tienda
        for (int pass = 0; pass < 2 && pick < 0; pass++)
            for (int i = 1; i < rooms.Length && pick < 0; i++)
            {
                var r = rooms[i];
                if (r.feature == RoomFeature.Foso || r.feature == RoomFeature.Ascenso || r.feature == RoomFeature.Escalinata) continue;
                if (pass == 0 && r.type != RoomType.Combate) continue;
                if (pass == 1 && r.type != RoomType.Descanso) continue;
                if (r.size.x < 14f) continue;
                bool doorN = false; foreach (var g in gaps[i]) if (g.side == Side.N) doorN = true;
                if (doorN && r.type == RoomType.Combate) continue;
                pick = i;
            }
        if (pick < 0) return;
        var room = rooms[pick];
        // cerca de la esquina NE: lejos de la puerta y de las antorchas (que van a 1/4)
        float offset = room.size.x / 2f - 2.45f;
        foreach (var g in gaps[pick]) if (g.side == Side.N && Mathf.Abs(g.offset - offset) < g.width / 2f + 1.5f) offset = -offset;   // si choca con una puerta, al otro lado
        var n = new Niche { room = pick, offset = offset, width = 1.8f };
        niches.Add(n);
        gaps[pick].Add(new Gap { side = Side.N, offset = n.offset, width = n.width, elevation = 0f, secret = true });
    }

    /// <summary>Tramo de muro que se ve normal pero no tiene collider (y se oculta si Abby está detrás).</summary>
    void FakeWallSegment(Transform parent, Vector2 size, Side side, Gap g)
    {
        float hz = size.y / 2f, h = roomHeight;
        if (side != Side.N) return;   // solo norte: es el que tapa con la cámara por defecto
        var face = CreateFace(parent, "WallN_Falso", new Vector3(g.offset, h / 2f, hz), Vector3.back, g.width, h, wallMaterial, WallK, WallK, true);
        AddCap(parent, new Vector3(g.offset, h, hz), Vector3.back, g.width);
        var fake = face.AddComponent<FakeWall>();
        fake.nicheCenter = new Vector3(g.offset, 0f, hz + 1.3f);
        fake.nicheSize = new Vector2(g.width + 0.4f, 2.8f);
        // pista: polvo que se cuela por la base (como una corriente de aire)
        Motes(parent, new Vector3(g.offset, 0.15f, hz - 0.25f), new Vector3(g.width * 0.8f, 0.1f, 0.2f), new Color(0.85f, 0.85f, 0.95f), 3f, 0.08f, 3f);
    }

    /// <summary>El cuartito detrás del muro falso: suelo, paredes, una vela, un cofre generoso y una nota de A.</summary>
    void BuildNiche(Transform secretsRoot, Transform roomObj, RoomSpec r, Niche n)
    {
        var root = NewChild(secretsRoot, "Escondite_" + r.name.Split(' ')[0]);
        float hz = r.size.y / 2f, depth = 2.4f, w = n.width + 0.6f, h = wallHeight;
        root.position = roomObj.TransformPoint(new Vector3(n.offset, 0f, hz + depth / 2f));
        float k = 1f / tileMeters, t = colliderThickness;
        CreateFace(root, "Suelo", Vector3.zero, Vector3.up, w, depth, floorMaterial, k, k);
        CreateCollider(root, "Collider_Suelo", new Vector3(0f, -t / 2f, 0f), new Vector3(w, t, depth));
        CreateFace(root, "ParedN", new Vector3(0f, h / 2f, depth / 2f), Vector3.back, w, h, wallMaterial, WallK, WallK, true);
        CreateFace(root, "ParedE", new Vector3(w / 2f, h / 2f, 0f), Vector3.left, depth, h, wallMaterial, WallK, WallK, true);
        CreateFace(root, "ParedO", new Vector3(-w / 2f, h / 2f, 0f), Vector3.right, depth, h, wallMaterial, WallK, WallK, true);
        CreateCollider(root, "Collider_N", new Vector3(0f, h / 2f, depth / 2f + t / 2f), new Vector3(w + 2f * t, h + 2f, t));
        CreateCollider(root, "Collider_E", new Vector3(w / 2f + t / 2f, h / 2f, 0f), new Vector3(t, h + 2f, depth));
        CreateCollider(root, "Collider_O", new Vector3(-w / 2f - t / 2f, h / 2f, 0f), new Vector3(t, h + 2f, depth));
        if (ceilings) PlainCeiling(root, w, depth, h);

        Color c = theme == FloorTheme.Base ? new Color(1f, 0.7f, 0.4f) : palette.torch;
        Candles(root, new Vector3(-w / 2f + 0.4f, 0f, depth / 2f - 0.4f), 3, c, 0.15f);
        TreasureChestAt(root, new Vector3(0.2f, 0f, 0.3f), 22 + floorNumber * 4, 180f);
        var spot = root.gameObject.AddComponent<SecretSpot>();
        spot.id = $"p{floorNumber}_muro";
        spot.size = new Vector2(w, depth);
        spot.message = "Un muro que no era muro";

        var l = new Lore { kind = LoreNote.Kind.Diario, title = "Detrás del muro", sign = "— A.", text = NicheText(floorNumber) };
        Readable(root, new Vector3(w / 2f - 0.5f, 0f, depth / 2f - 0.5f), l, $"p{floorNumber}_escondite");

        var reveal = root.gameObject.AddComponent<AreaReveal>();
        reveal.size = new Vector2(w, depth);
        reveal.yMin = -1f; reveal.yMax = h + 1f;
        reveal.margin = 3.2f;   // se "enciende" al pasar cerca, pero el muro lo tapa… a menos que gires la cámara
    }

    static string NicheText(int floor)
    {
        switch (floor)
        {
            case 1: return "Si encontraste esto es porque giraste la cámara. Bien.\n\nLa torre esconde más de lo que muestra. Los muros del norte mienten a veces.";
            case 2: return "Dejé esto aquí para la próxima vez. No sé por qué escribo \"próxima vez\". Se siente como si ya hubiera estado aquí.";
            case 3: return "Hay figuras flotando en algunos pisos. Si las miras desde el lugar correcto, se acomodan. No me preguntes cómo lo sé.";
            case 4: return "No todo lo que se come alimenta. Guardé estos fragmentos en lugar de gastarlos en comida.";
            case 5: return "Este cofre no muerde. Lo revisé tres veces.";
            case 6: return "Aquí no me encuentra La Otra. Detrás de los muros no hay reflejo.";
            case 7: return "Aquí no jala nada. Por fin un lugar donde descansar del deseo de seguir.";
            case 8: return "Me escondí aquí a respirar. El fuego no entra. El enojo sí, pero más bajito.";
            default: return "Último escondite. Lo que hay en el cofre es tuyo. Bueno, mío. Bueno, nuestro.";
        }
    }

    // ------------------------------------------------------------------ reliquias anamórficas

    struct RelicSpec { public int room; public Vector2 stand; public float yaw; public string id, name, desc; }

    RelicSpec RelicFor(int floor)
    {
        switch (floor)
        {
            case 3: return new RelicSpec { room = 1, stand = new Vector2(-5f, -3f), yaw = 135f, id = "gato", name = "Gato Dormilón", desc = "Lleva aquí siglos. Ronronea. No piensa moverse." };
            case 4: return new RelicSpec { room = 3, stand = new Vector2(4f, -3f), yaw = 225f, id = "pastel", name = "Pastel del 20 de Noviembre", desc = "Alguien lo horneó para un día especial. Huele a demo." };
            case 5: return new RelicSpec { room = 4, stand = new Vector2(-5f, 3f), yaw = 90f, id = "peso", name = "Moneda de un Peso", desc = "No alcanza para nada, pero es tuya." };
            case 6: return new RelicSpec { room = 1, stand = new Vector2(5f, 3f), yaw = 270f, id = "espejo", name = "Espejo de Mano Rajado", desc = "En el reflejo, la otra Abis sonríe. Tú no." };
            case 7: return new RelicSpec { room = 4, stand = new Vector2(-5f, -3f), yaw = 180f, id = "carta", name = "Carta sin Enviar", desc = "Dice \"para ti\". No dice quién." };
            case 8: return new RelicSpec { room = 5, stand = new Vector2(5f, -3f), yaw = 45f, id = "patito", name = "Patito de Hule Ignífugo", desc = "Sobrevivió a la lava. Respeto." };
            default: return new RelicSpec { room = 3, stand = new Vector2(-5f, 3f), yaw = 315f, id = "placa", name = "Placa del Estudio 02", desc = "Hecho por Rodrigo, Gabriel, Francisco, Ricardo y Karla — UT Cancún, IDYGS103." };
        }
    }

    void PlaceRelic(Transform secretsRoot, Transform[] roomObjects)
    {
        var spec = RelicFor(floorNumber);
        if (spec.room >= roomObjects.Length) return;
        var room = roomObjects[spec.room];
        const float pitch = 34f;   // la cámara de los pisos va de 18° a 50°: 34° es alcanzable pero no es la de siempre
        float yr = spec.yaw * Mathf.Deg2Rad, pr = pitch * Mathf.Deg2Rad;
        var view = new Vector3(Mathf.Sin(yr) * Mathf.Cos(pr), -Mathf.Sin(pr), Mathf.Cos(yr) * Mathf.Cos(pr));
        var flat = new Vector3(view.x, 0f, view.z).normalized;

        var root = NewChild(secretsRoot, "Reliquia_" + spec.id);
        root.position = room.TransformPoint(new Vector3(spec.stand.x, 0f, spec.stand.y));

        // marca del punto donde pararse: una baldosa apenas más clara (casi no se nota)
        var markMat = SavedMaterial("Marca_" + palette.key, () => { var mm = new Material(floorMaterial); mm.SetColor("_BaseColor", new Color(1.18f, 1.15f, 1.1f)); return mm; });
        var mark = CreateFace(root, "Marca", Vector3.up * 0.012f, Vector3.up, 0.9f, 0.9f, markMat, 1f / tileMeters, 1f / tileMeters);
        mark.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // piezas: puntos del símbolo proyectados en paralelo a lo largo de la vista, con profundidad al azar
        Vector3 right = Vector3.Cross(Vector3.up, view).normalized;
        Vector3 up = Vector3.Cross(view, right).normalized;
        // el símbolo queda justo encima de Abby en pantalla (la cámara la sigue y la deja al centro)
        var anchor = up * 1.8f + Vector3.up * 0.4f;
        var pieces = new List<Renderer>();
        var mat = GlowMaterial("Glow_Polvo", new Color(0.05f, 0.05f, 0.065f));
        var pts = Symbol(floorNumber);
        var piecesRoot = NewChild(root, "Piezas");
        foreach (var p in pts)
        {
            float depthAlong = DR(-2.6f, 2.6f);
            var pos = anchor + right * p.x * 0.9f + up * p.y * 0.9f + view * depthAlong;
            var g = Prim(piecesRoot, PrimitiveType.Cube, "Pieza", pos, Vector3.one * 0.11f, mat, false);
            g.transform.localRotation = Quaternion.Euler(DR(0f, 90f), DR(0f, 90f), DR(0f, 90f));
            var mr = g.GetComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pieces.Add(mr);
        }
        var bob = piecesRoot.gameObject.AddComponent<FloatingBob>(); bob.amplitude = 0.08f; bob.period = 4f;

        var reward = NewChild(root, "Recompensa");
        reward.localPosition = flat * 3.2f;
        var model = NewChild(reward, "Modelo");
        model.localPosition = Vector3.up * 0.5f;
        RelicModel(model, floorNumber);
        var lt = AddLight(reward, Vector3.up * 1.2f, new Color(1f, 0.85f, 0.5f), 4f, 4f, false);
        Motes(reward, Vector3.up * 0.4f, new Vector3(0.6f, 0.4f, 0.6f), new Color(1f, 0.9f, 0.6f), 8f, 0.5f, 2f);
        var relic = reward.gameObject.AddComponent<RelicPickup>();
        relic.id = $"p{floorNumber}_{spec.id}"; relic.relicName = spec.name; relic.description = spec.desc; relic.model = model;

        var ana = root.gameObject.AddComponent<AnamorphicSecret>();
        ana.id = $"p{floorNumber}_figura";
        ana.viewDir = view;
        ana.standPoint = Vector3.zero;
        ana.standRadius = 1.4f;
        ana.tolerance = 6f;
        ana.pieces = pieces.ToArray();
        ana.reward = reward.gameObject;
        ana.message = "Desde aquí, todo tiene sentido";
    }

    /// <summary>Puntos (x, y en -1..1) del símbolo de cada pecado.</summary>
    static List<Vector2> Symbol(int floor)
    {
        var pts = new List<Vector2>();
        void Arc(Vector2 c, float r, float a0, float a1, int n) { for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); } }
        void Line(Vector2 a, Vector2 b, int n) { for (int i = 0; i <= n; i++) pts.Add(Vector2.Lerp(a, b, i / (float)n)); }
        switch (floor)
        {
            case 3:  // luna creciente (Pereza: el sueño)
                Arc(Vector2.zero, 1f, 60f, 300f, 26); Arc(new Vector2(0.45f, 0f), 0.8f, 95f, 265f, 18); break;
            case 4:  // manzana (Gula)
                Arc(new Vector2(0f, -0.1f), 0.85f, 100f, 440f, 30); Line(new Vector2(0f, 0.7f), new Vector2(0.1f, 1.05f), 4); Arc(new Vector2(0.35f, 0.95f), 0.22f, 180f, 330f, 6); break;
            case 5:  // moneda con un rombo (Avaricia)
                Arc(Vector2.zero, 1f, 0f, 360f, 32); Line(new Vector2(0f, 0.55f), new Vector2(0.45f, 0f), 5); Line(new Vector2(0.45f, 0f), new Vector2(0f, -0.55f), 5); Line(new Vector2(0f, -0.55f), new Vector2(-0.45f, 0f), 5); Line(new Vector2(-0.45f, 0f), new Vector2(0f, 0.55f), 5); break;
            case 6:  // ojo (Envidia)
                Arc(new Vector2(0f, -0.9f), 1.35f, 48f, 132f, 14); Arc(new Vector2(0f, 0.9f), 1.35f, 228f, 312f, 14); Arc(Vector2.zero, 0.32f, 0f, 360f, 12); break;
            case 7:  // corazón (Lujuria)
                for (int i = 0; i <= 40; i++) { float t = i / 40f * Mathf.PI * 2f; pts.Add(new Vector2(16f * Mathf.Pow(Mathf.Sin(t), 3f), 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) / 17f); }
                break;
            case 8:  // llama (Ira)
                Arc(new Vector2(0f, -0.35f), 0.6f, 180f, 360f, 10); Line(new Vector2(0.6f, -0.35f), new Vector2(0.15f, 1f), 9); Line(new Vector2(-0.6f, -0.35f), new Vector2(-0.1f, 0.6f), 8); Line(new Vector2(-0.1f, 0.6f), new Vector2(0.15f, 1f), 3); Arc(new Vector2(0f, -0.4f), 0.25f, 0f, 360f, 8); break;
            default: // corona (Soberbia)
                Line(new Vector2(-1f, -0.5f), new Vector2(1f, -0.5f), 12); Line(new Vector2(-1f, -0.5f), new Vector2(-1f, 0.6f), 6); Line(new Vector2(1f, -0.5f), new Vector2(1f, 0.6f), 6);
                Line(new Vector2(-1f, 0.6f), new Vector2(-0.5f, 0f), 5); Line(new Vector2(-0.5f, 0f), new Vector2(0f, 0.8f), 6); Line(new Vector2(0f, 0.8f), new Vector2(0.5f, 0f), 6); Line(new Vector2(0.5f, 0f), new Vector2(1f, 0.6f), 5); break;
        }
        return pts;
    }

    /// <summary>La reliquia de cada piso, hecha con formas simples.</summary>
    void RelicModel(Transform m, int floor)
    {
        switch (floor)
        {
            case 3:   // gato dormido hecho bolita
            {
                var fur = LitMaterial("Pelaje", new Color(0.85f, 0.55f, 0.3f));
                Prim(m, PrimitiveType.Sphere, "Cuerpo", Vector3.zero, new Vector3(0.6f, 0.35f, 0.5f), fur, false);
                Prim(m, PrimitiveType.Sphere, "Cabeza", new Vector3(0.25f, 0.05f, 0.12f), new Vector3(0.28f, 0.24f, 0.26f), fur, false);
                foreach (float z in new[] { 0.04f, 0.2f })
                    Prim(m, PrimitiveType.Cube, "Oreja", new Vector3(0.3f, 0.2f, z), new Vector3(0.07f, 0.1f, 0.06f), fur, false).transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
                var tail = Prim(m, PrimitiveType.Cylinder, "Cola", new Vector3(-0.2f, -0.08f, -0.2f), new Vector3(0.08f, 0.22f, 0.08f), fur, false);
                tail.transform.localRotation = Quaternion.Euler(90f, 40f, 0f);
                break;
            }
            case 4:   // pastel con vela
            {
                Prim(m, PrimitiveType.Cylinder, "Pan", Vector3.zero, new Vector3(0.6f, 0.15f, 0.6f), LitMaterial("Pan", new Color(0.8f, 0.55f, 0.3f)), false);
                Prim(m, PrimitiveType.Cylinder, "Betun", new Vector3(0f, 0.16f, 0f), new Vector3(0.62f, 0.03f, 0.62f), LitMaterial("Betun", new Color(1f, 0.85f, 0.9f)), false);
                for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2f; Prim(m, PrimitiveType.Sphere, "Fresa", new Vector3(Mathf.Cos(a) * 0.22f, 0.2f, Mathf.Sin(a) * 0.22f), Vector3.one * 0.07f, LitMaterial("Fresa", new Color(0.85f, 0.1f, 0.15f)), false); }
                Prim(m, PrimitiveType.Cylinder, "Vela", new Vector3(0f, 0.3f, 0f), new Vector3(0.03f, 0.1f, 0.03f), LitMaterial("Cera", new Color(0.92f, 0.86f, 0.72f)), false);
                Flame(m, new Vector3(0f, 0.43f, 0f), 0.3f, new Color(1f, 0.75f, 0.4f), false, true);
                break;
            }
            case 5:   // moneda grande
            {
                var coin = Prim(m, PrimitiveType.Cylinder, "Moneda", Vector3.zero, new Vector3(0.5f, 0.03f, 0.5f), BrassMaterial(), false);
                coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prim(m, PrimitiveType.Cube, "Uno", new Vector3(0f, 0f, -0.035f), new Vector3(0.05f, 0.25f, 0.02f), GlowMaterial("Glow_Oro", new Color(1f, 0.75f, 0.35f)), false);
                break;
            }
            case 6:   // espejo de mano
            {
                Prim(m, PrimitiveType.Cylinder, "Marco", Vector3.zero, new Vector3(0.42f, 0.03f, 0.42f), BrassMaterial(), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prim(m, PrimitiveType.Cylinder, "Luna", new Vector3(0f, 0f, -0.02f), new Vector3(0.36f, 0.02f, 0.36f), GlassMaterial(new Color(0.4f, 1f, 0.55f)), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prim(m, PrimitiveType.Cube, "Mango", new Vector3(0f, -0.35f, 0f), new Vector3(0.07f, 0.3f, 0.05f), BrassMaterial(), false);
                break;
            }
            case 7:   // carta con sello
            {
                Prim(m, PrimitiveType.Cube, "Sobre", Vector3.zero, new Vector3(0.5f, 0.32f, 0.02f), LitMaterial("Papel", new Color(0.9f, 0.84f, 0.7f)), false);
                Prim(m, PrimitiveType.Cylinder, "Sello", new Vector3(0f, 0f, -0.02f), new Vector3(0.1f, 0.01f, 0.1f), LitMaterial("Lacre", new Color(0.7f, 0.05f, 0.15f)), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
            }
            case 8:   // patito de hule
            {
                var yellow = LitMaterial("Hule", new Color(1f, 0.85f, 0.15f));
                Prim(m, PrimitiveType.Sphere, "Cuerpo", Vector3.zero, new Vector3(0.42f, 0.3f, 0.32f), yellow, false);
                Prim(m, PrimitiveType.Sphere, "Cabeza", new Vector3(0.12f, 0.22f, 0f), Vector3.one * 0.22f, yellow, false);
                Prim(m, PrimitiveType.Cube, "Pico", new Vector3(0.25f, 0.2f, 0f), new Vector3(0.1f, 0.04f, 0.08f), LitMaterial("Pico", new Color(1f, 0.45f, 0.1f)), false);
                break;
            }
            default:  // placa con letras
            {
                Prim(m, PrimitiveType.Cube, "Placa", Vector3.zero, new Vector3(0.7f, 0.45f, 0.05f), BrassMaterial(), false);
                for (int i = 0; i < 4; i++)
                    Prim(m, PrimitiveType.Cube, "Linea", new Vector3(0f, 0.13f - i * 0.09f, -0.03f), new Vector3(0.5f - (i % 2) * 0.12f, 0.03f, 0.01f), GlowMaterial("Glow_Oro", new Color(1f, 0.75f, 0.35f)), false);
                break;
            }
        }
    }
}
