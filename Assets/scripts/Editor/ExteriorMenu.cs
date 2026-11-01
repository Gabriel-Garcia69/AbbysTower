using UnityEditor;
using UnityEngine;

/// <summary>
/// Abby → Exterior → Generar Cala Sombría / Generar meseta (anterior): cambia la forma del ExteriorBuilder de la escena
/// abierta y lo regenera. La Cala se guarda como prefab (Assets/Prefabs/Exterior/Generado/Ext_Cala.prefab); guarda la
/// escena después (Ctrl+S) para que quede la instancia y la nueva posición de Abby.
/// </summary>
public static class ExteriorMenu
{
    [MenuItem("Abby/Exterior/Generar Cala Sombría")]
    static void Cala() => Generar(ExteriorBuilder.Forma.Cala);

    [MenuItem("Abby/Exterior/Hora: Noche")]
    static void Noche() => GenerarHora(ExteriorBuilder.Hora.Noche);

    [MenuItem("Abby/Exterior/Hora: Atardecer")]
    static void Atardecer() => GenerarHora(ExteriorBuilder.Hora.Atardecer);

    static void GenerarHora(ExteriorBuilder.Hora hora)
    {
        var b = Object.FindAnyObjectByType<ExteriorBuilder>();
        if (b != null) { b.horaDelDia = hora; EditorUtility.SetDirty(b); }
        Generar(ExteriorBuilder.Forma.Cala);
    }

    [MenuItem("Abby/Exterior/Generar meseta (anterior)")]
    static void Abismo() => Generar(ExteriorBuilder.Forma.Abismo);

    static void Generar(ExteriorBuilder.Forma forma)
    {
        if (Application.isPlaying) { Debug.LogWarning("[Exterior] Sal de Play para generar."); return; }
        var b = Object.FindAnyObjectByType<ExteriorBuilder>();
        if (b == null) { Debug.LogWarning("[Exterior] Abre la escena Exterior primero."); return; }
        b.forma = forma;
        EditorUtility.SetDirty(b);
        b.Build();
    }
}
