using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Abby → Sprites → Crear clips PixelLab: arma un AnimationClip por animación y dirección con los frames importados en
/// Assets/sprites/Abby/PixelLab (curva de Sprite sobre el SpriteRenderer). Los guarda en Assets/Animations/PixelLab
/// y NO toca ningún Animator, prefab ni escena. Si un clip ya existe se reemplaza su contenido (misma ruta, mismo GUID).
/// </summary>
public static class PixelLabClips
{
    const string Origen = "Assets/sprites/Abby/PixelLab/Idle/animations";
    const string Destino = "Assets/Animations/PixelLab";
    static readonly string[] Dirs = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    struct Def
    {
        public string clip, carpeta;   // carpeta: prefijo del nombre bajo animations/
        public string origen;          // otra raíz de animaciones (null = Origen)
        public float fps; public bool loop;
        public string[] dirs;          // null = las 8
    }

    static readonly Def[] Defs =
    {
        new Def { clip = "Idle",           carpeta = "idle_breathing",          fps = 6f,  loop = true  },
        new Def { clip = "Walking",        carpeta = "Walking",                 fps = 12f, loop = true  },
        new Def { clip = "Running",        carpeta = "Running",                 fps = 14f, loop = true,  origen = "Assets/sprites/Abby/PixelLab/Idle_copy_2/animations" },   // las 8 direcciones
        new Def { clip = "SwordSlash",     carpeta = "quick_horizontal_sword",  fps = 20f, loop = false },
        new Def { clip = "DefensiveBlock", carpeta = "defensive_block",         fps = 20f, loop = false },
        new Def { clip = "TakingHit",      carpeta = "taking_a_hit",            fps = 20f, loop = false },
        new Def { clip = "CrossPunch",     carpeta = "Cross_Punch",             fps = 14f, loop = false, dirs = new[] { "south" } },
    };

    [MenuItem("Abby/Sprites/Crear clips PixelLab")]
    static void Crear()
    {
        if (!AssetDatabase.IsValidFolder(Origen)) { Debug.LogWarning("[Abby] Falta " + Origen + " (importa primero los sprites)."); return; }
        if (!AssetDatabase.IsValidFolder("Assets/Animations")) AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(Destino)) AssetDatabase.CreateFolder("Assets/Animations", "PixelLab");

        var resumen = new List<string>();
        foreach (var d in Defs)
        {
            string carpeta = null;
            foreach (var sub in AssetDatabase.GetSubFolders(d.origen ?? Origen))
                if (Path.GetFileName(sub).StartsWith(d.carpeta)) { carpeta = sub; break; }
            if (carpeta == null) { resumen.Add($"{d.clip}: SIN carpeta '{d.carpeta}*'"); continue; }

            int hechos = 0, frames = 0;
            foreach (var dir in d.dirs ?? Dirs)
            {
                var sprites = new List<Sprite>();
                for (int i = 0; i < 64; i++)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{carpeta}/{dir}/frame_{i:000}.png");
                    if (s == null) break;
                    sprites.Add(s);
                }
                if (sprites.Count == 0) continue;

                string ruta = $"{Destino}/Abby_{d.clip}_{dir}.anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
                if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, ruta); }
                else clip.ClearCurves();
                clip.frameRate = d.fps;
                clip.wrapMode = d.loop ? WrapMode.Loop : WrapMode.ClampForever;

                var keys = new ObjectReferenceKeyframe[sprites.Count];
                for (int i = 0; i < keys.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / d.fps, value = sprites[i] };
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);

                var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
                ajustes.loopTime = d.loop;
                ajustes.stopTime = sprites.Count / d.fps;   // la última pose dura un cuadro entero
                AnimationUtility.SetAnimationClipSettings(clip, ajustes);
                EditorUtility.SetDirty(clip);
                hechos++; frames = sprites.Count;
            }
            resumen.Add($"{d.clip}: {hechos} clips de {frames} cuadros a {d.fps} fps ({(d.loop ? "loop" : "una vez")})");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Abby] Clips PixelLab en " + Destino + ":\n  " + string.Join("\n  ", resumen));
    }
}
