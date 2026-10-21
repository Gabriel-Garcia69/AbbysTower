using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Abby → Generar prefabs de enemigos: arma la apariencia de cada tipo de TowerEnemy y la guarda como prefab en
/// Assets/Resources/Enemigos/&lt;Tipo&gt;.prefab (con sus materiales en Assets/Materials/Enemigos). El juego usa esos prefabs
/// si existen: se pueden editar o reemplazar por arte, respetando los nombres "Cuerpo", "Ojo", "Aspas"/"Orbes" y "Escudo".
/// Si ya existe un prefab no se sobrescribe (para no perder cambios); bórralo para regenerarlo.
/// </summary>
public static class EnemyPrefabs
{
    const string PrefabDir = "Assets/Resources/Enemigos";
    const string MatDir = "Assets/Materials/Enemigos";
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [MenuItem("Abby/Generar prefabs de enemigos")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PrefabDir)) AssetDatabase.CreateFolder("Assets/Resources", "Enemigos");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials", "Enemigos");
        int made = 0;
        foreach (TowerEnemy.Kind k in System.Enum.GetValues(typeof(TowerEnemy.Kind)))
            if (k != TowerEnemy.Kind.Dante)   // Dante usa los sprites de Abby (sin prefab)
        {
            string path = PrefabDir + "/" + k + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
            var temp = new GameObject("Temp_" + k);
            var e = temp.AddComponent<TowerEnemy>();
            var visual = e.BuildForPrefab(k);
            visual.SetParent(null, false);
            visual.name = k.ToString();
            Bake(visual);
            PrefabUtility.SaveAsPrefabAsset(visual.gameObject, path);
            Object.DestroyImmediate(visual.gameObject);
            Object.DestroyImmediate(temp);
            made++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyPrefabs] {made} prefabs nuevos en {PrefabDir}");
    }

    /// <summary>Convierte los colores (MaterialPropertyBlock, que no se guardan) en materiales guardados.</summary>
    static void Bake(Transform root)
    {
        var cache = new Dictionary<string, Material>();
        var mpb = new MaterialPropertyBlock();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterial;
            if (src == null) continue;
            r.GetPropertyBlock(mpb);
            Color c = mpb.isEmpty ? (src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId) : Color.white) : mpb.GetColor(BaseColorId);
            bool unlit = src.shader.name.Contains("Unlit");
            string key = (unlit ? "Brillo_" : "Cuerpo_") + ColorUtility.ToHtmlStringRGB(c) + (c.maxColorComponent > 1f ? "_x" + Mathf.RoundToInt(c.maxColorComponent) : "");
            if (!cache.TryGetValue(key, out var m))
            {
                string mp = MatDir + "/Enemigo_" + key + ".mat";
                m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null)
                {
                    m = new Material(src.shader) { name = "Enemigo_" + key };
                    if (src.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));
                    m.SetColor(BaseColorId, c);
                    AssetDatabase.CreateAsset(m, mp);
                }
                cache[key] = m;
            }
            r.sharedMaterial = m;
            r.SetPropertyBlock(null);
        }
    }
}
