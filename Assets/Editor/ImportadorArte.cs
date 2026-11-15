using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación de Assets/Arte, para que cualquier imagen nueva quede bien sin tocar el Inspector:
/// interfaz como sprites (los chaflanes con borde 9-slice), fondos y evidencias comprimidos y con tamaño máximo
/// (presupuesto de WebGL), texturas de la nave repetibles.
/// </summary>
public class ImportadorArte : AssetPostprocessor
{
    // Subir este número fuerza a Unity a reimportar las imágenes con los ajustes nuevos.
    public override uint GetVersion() => 8;

    /// <summary>
    /// Audio de Assets/Arte/Audio (ElevenLabs): música comprimida en memoria (Vorbis), efectos cortos en mono y
    /// descomprimidos al cargar (sin retraso al sonar) y voces en mono. Pensado para el peso del build WebGL.
    /// </summary>
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Arte/Audio/")) return;
        var ai = (AudioImporter)assetImporter;
        var ajuste = ai.defaultSampleSettings;
        ajuste.compressionFormat = AudioCompressionFormat.Vorbis;
        if (assetPath.Contains("/Musica/"))
        {
            ai.forceToMono = false;
            ajuste.loadType = AudioClipLoadType.CompressedInMemory;
            ajuste.quality = 0.5f;
        }
        else if (assetPath.Contains("/Voces/"))
        {
            ai.forceToMono = true;
            ajuste.loadType = AudioClipLoadType.CompressedInMemory;
            ajuste.quality = 0.55f;
        }
        else
        {
            ai.forceToMono = true;
            bool largo = assetPath.Contains("ambiente_") || assetPath.Contains("/AMB_");
            ajuste.loadType = largo ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            ajuste.quality = 0.6f;
        }
        ai.defaultSampleSettings = ajuste;
        ai.loadInBackground = assetPath.Contains("/Musica/") || assetPath.Contains("ambiente_") || assetPath.Contains("/AMB_");
    }

    /// <summary>
    /// Modelos de NASA 3D Resources (convertidos a OBJ con Herramientas/modelos): sin animación ni colisiones,
    /// materiales a partir del MTL (el constructor los cambia por materiales URP propios).
    /// </summary>
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Arte/Modelos/")) return;
        var mi = (ModelImporter)assetImporter;
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false;
        mi.importCameras = false;
        mi.importLights = false;
        mi.isReadable = false;
        mi.addCollider = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.importNormals = ModelImporterNormals.Import;
        mi.meshCompression = ModelImporterMeshCompression.Medium;
        mi.globalScale = 1f;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Arte/")) return;
        var ti = (TextureImporter)assetImporter;
        var nombre = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        ti.textureShape = TextureImporterShape.Texture2D;
        ti.textureCompression = TextureImporterCompression.Compressed;

        if (assetPath.Contains("/Interfaz/"))
        {
            bool repetible = nombre == "Segmentos" || nombre == "Barrido";
            ti.textureType = repetible ? TextureImporterType.Default : TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = repetible ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = nombre == "Barrido" ? FilterMode.Point : FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed; // son diminutas y deben verse nítidas
            if (nombre.StartsWith("Chaflan")) ti.spriteBorder = new Vector4(18, 18, 18, 18);
        }
        else if (assetPath.Contains("/Patrocinio/"))
        {
            // Logos del patrocinador: sprites con transparencia para la interfaz y calcomanía en los cohetes
            // (con mipmaps, que en 3D se ven pequeños y sin ellos parpadean).
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
        }
        else if (assetPath.Contains("/Fondos/"))
        {
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 2048;
        }
        else if (assetPath.Contains("/Evidencias/"))
        {
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 1024;
        }
        else if (assetPath.Contains("/Modelos/"))
        {
            ti.maxTextureSize = 1024;
            ti.anisoLevel = 2;
            // Las rampas de lámina dorada/plateada son tiras de pocos píxeles: sin compresión para que no se manchen.
            if (nombre.Contains("foil")) ti.textureCompression = TextureImporterCompression.Uncompressed;
        }
        else if (assetPath.Contains("/Texturas/"))
        {
            bool repetible = nombre == "Lamina_Dorada" || nombre == "Celdas_Solares" || nombre == "Marte_Detalle" ||
                             nombre == "Nubes" || nombre == "Pasto";
            bool mitad = nombre == "Marte_Superficie_Oeste" || nombre == "Marte_Superficie_Este";
            ti.wrapMode = repetible ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            // Los mapas de planeta se repiten a lo ancho (longitud) para que no se vea la costura.
            // Las dos mitades del mapa 8K no: cada una termina donde empieza la otra (shader Planeta).
            if ((nombre.StartsWith("Marte_") || nombre.StartsWith("Tierra_")) && !mitad) ti.wrapModeU = TextureWrapMode.Repeat;
            ti.mipmapEnabled = nombre != "Estrellas"; // las estrellas se ven a 1:1, sin mipmaps quedan nítidas
            ti.anisoLevel = 4;
            // La superficie de Marte se ve de cerca al elegir la órbita: más resolución. Las mitades 8K y las
            // normales a 4096 van con compresión Crunch para que la descarga en el navegador siga siendo ligera.
            ti.maxTextureSize = nombre == "Marte_Superficie" || mitad || nombre == "Marte_Normal" ? 4096
                              : nombre == "Marte_Detalle" ? 512 : 2048;
            if (mitad || nombre == "Marte_Normal")
            {
                ti.crunchedCompression = true;
                ti.compressionQuality = 70;
            }
            if (nombre == "Marte_Normal") ti.textureType = TextureImporterType.NormalMap;
            if (nombre == "Marte_Polvo" || nombre == "Nubes") ti.alphaIsTransparency = true;
        }
    }
}
