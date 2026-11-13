using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación de la música (Resources/Audio/Musica_*): Vorbis y en streaming, para que las pistas largas
/// (la final dura más de dos minutos) no se descompriman enteras en memoria al cargarlas.
/// </summary>
public class ImportarMusica : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        string nombre = System.IO.Path.GetFileName(assetPath);
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Audio/") || !nombre.StartsWith("Musica_")) return;
        var imp = (AudioImporter)assetImporter;
        imp.loadInBackground = true;
        var s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        s.preloadAudioData = true;
        imp.defaultSampleSettings = s;
    }
}
