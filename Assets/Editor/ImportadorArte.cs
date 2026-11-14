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
    public override uint GetVersion() => 2;

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
        else if (assetPath.Contains("/Texturas/"))
        {
            bool repetible = nombre == "Lamina_Dorada" || nombre == "Celdas_Solares";
            ti.wrapMode = repetible ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.mipmapEnabled = nombre != "Estrellas"; // las estrellas se ven a 1:1, sin mipmaps quedan nítidas
            ti.anisoLevel = 4;
            ti.maxTextureSize = 2048;
        }
    }
}
