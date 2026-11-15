using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Versión 6: texturas propias para el Marte unificado (sin material de terceros), generadas por código:
/// - Texturas/Marte_Nubes.png (2048 × 1024, equirectangular como el mapa, se repite a lo ancho): nubes de hielo
///   de agua. Como en Marte, son finas: una «capucha polar» de bruma entre los 50° y 75° de latitud, nubes
///   orográficas sobre los volcanes de Tharsis y algunas vetas sueltas en latitudes medias.
/// - Interfaz/Degradado_Izquierda.png: velo oscuro que se aclara hacia la derecha (Briefing sobre el Marte 3D).
/// El mapa usa u = (longitud este + 180) / 360 y v = (latitud + 90) / 180, como el shader Planeta.
/// Batch: -executeMethod GeneradorCapasMarte.Generar. Determinista: siempre sale lo mismo.
/// </summary>
public static class GeneradorCapasMarte
{
    const int Ancho = 2048, Alto = 1024;

    [MenuItem("Herramientas/Generar capas de Marte (versión 6)")]
    public static void Generar()
    {
        GuardarNubes("Assets/Arte/Texturas/Marte_Nubes.png");
        GuardarDegradado("Assets/Arte/Interfaz/Degradado_Izquierda.png");
        AssetDatabase.Refresh();
        Debug.Log("[GeneradorCapasMarte] Nubes y degradado escritos.");
    }

    /// <summary>
    /// Ruido fractal (fBm) que se repite a lo ancho sin costura: la longitud se recorre en un círculo del plano
    /// de ruido (radio = frecuencia este-oeste) y la latitud lo desplaza (frecuencia norte-sur). Con más frecuencia
    /// norte-sur que este-oeste las nubes salen alargadas, como las estira el viento. Devuelve 0..1 aproximado.
    /// </summary>
    static float Fbm(float u, float v, int octavas, float radio, float frecuenciaV, float semilla)
    {
        float angulo = u * 2f * Mathf.PI;
        float x = Mathf.Cos(angulo), y = Mathf.Sin(angulo);
        float total = 0f, amplitud = 0.5f, escala = 1f, suma = 0f;
        for (int o = 0; o < octavas; o++)
        {
            total += amplitud * Mathf.PerlinNoise(semilla + x * radio * escala + 100f, semilla * 1.7f + (y * radio + v * frecuenciaV) * escala + 100f);
            suma += amplitud;
            amplitud *= 0.5f;
            escala *= 2.03f;
        }
        // Perlin se concentra en 0.3..0.7: se estira para tener contraste.
        return Mathf.Clamp01((total / suma - 0.5f) * 2.2f + 0.5f);
    }

    static float Gauss(float x, float centro, float ancho) => Mathf.Exp(-(x - centro) * (x - centro) / (2f * ancho * ancho));

    static void GuardarNubes(string ruta)
    {
        var tex = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
        var pixeles = new Color32[Ancho * Alto];
        // Tharsis (Olympus Mons ~134° O, Arsia/Pavonis/Ascraeus ~105-120° O), en coordenadas del mapa.
        float uOlympus = (-134f + 180f) / 360f, uArsia = (-115f + 180f) / 360f;
        for (int y = 0; y < Alto; y++)
        {
            float v = (y + 0.5f) / Alto;
            float lat = (v - 0.5f) * 180f;   // grados, sur negativo
            float aLat = Mathf.Abs(lat);
            // Capucha polar: bruma ancha entre 50° y 75°; nada en el casquete (allí ya está el hielo de la textura).
            float capucha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 58f, aLat)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(68f, 80f, aLat)));
            for (int x = 0; x < Ancho; x++)
            {
                float u = (x + 0.5f) / Ancho;
                // Vetas estiradas este-oeste (el viento las alarga): más escala en u que en v.
                float base_ = Fbm(u, v, 6, 2.2f, 26f, 13.1f);
                float vetas = Fbm(u, v, 5, 3.5f, 60f, 71.7f);
                float orografica = (Gauss(u, uOlympus, 0.02f) + Gauss(u, uArsia, 0.025f)) * Gauss(lat, 6f, 10f);
                float densidad = capucha * Mathf.SmoothStep(0.45f, 0.8f, base_) * 0.8f
                               + orografica * Mathf.SmoothStep(0.4f, 0.75f, vetas) * 0.6f
                               + Mathf.SmoothStep(0.68f, 0.9f, vetas) * 0.4f * (1f - capucha) * Gauss(lat, 0f, 38f);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(densidad) * 255f);
                pixeles[y * Ancho + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixeles);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void GuardarDegradado(string ruta)
    {
        const int w = 512, h = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var color = new Color32[w * h];
        for (int x = 0; x < w; x++)
        {
            float t = x / (w - 1f);
            // Muy oscuro donde va el texto (izquierda) y casi transparente donde se ve el planeta (derecha).
            float alfa = Mathf.Lerp(0.93f, 0.18f, Mathf.SmoothStep(0.3f, 0.85f, t));
            for (int y = 0; y < h; y++) color[y * w + x] = new Color32(5, 7, 13, (byte)Mathf.RoundToInt(alfa * 255f));
        }
        tex.SetPixels32(color);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
