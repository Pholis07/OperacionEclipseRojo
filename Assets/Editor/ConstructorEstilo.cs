using System.Collections.Generic;
using System.IO;
using SpaceMission;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Crea los recursos de estilo que usa ConstructorEscenas: las fuentes TextMeshPro (con los iconos de Lucide
/// como fuente de reserva) y el asset Assets/Arte/Arte.asset con las imágenes, créditos y audios.
/// Es idempotente: si algo ya existe, lo reutiliza. Los audios se enlazan por nombre de archivo
/// (ver AUDIO-ELEVENLABS.md), así que basta con volver a construir las escenas después de añadirlos.
/// </summary>
public static class ConstructorEstilo
{
    const string CarpetaFuentes = "Assets/Arte/Fuentes";
    const string RutaArte = "Assets/Arte/Arte.asset";

    // Latín básico + Latin-1 + algunos signos tipográficos usados en los textos.
    static readonly string Caracteres = TextoLatino();

    public static TMP_FontAsset Titulos, Etiquetas, Texto, TextoFuerte, Mono, Iconos;

    [MenuItem("Herramientas/Recrear fuentes de la interfaz")]
    public static void RecrearFuentes()
    {
        foreach (var n in new[] { "Titulos", "Etiquetas", "Texto", "Texto Fuerte", "Mono", "Iconos" })
            AssetDatabase.DeleteAsset($"{CarpetaFuentes}/{n} SDF.asset");
        Asegurar();
    }

    public static void Asegurar()
    {
        Iconos = Fuente("Iconos", "Lucide.ttf", 64, 6, SpaceMission.Iconos.Todos, 512);
        TextoFuerte = Fuente("Texto Fuerte", "Barlow-SemiBold.ttf", 56, 7, Caracteres, 1024);
        Texto = Fuente("Texto", "Barlow-Regular.ttf", 56, 7, Caracteres, 1024);
        Titulos = Fuente("Titulos", "Rajdhani-Bold.ttf", 64, 8, Caracteres, 1024);
        Etiquetas = Fuente("Etiquetas", "Rajdhani-SemiBold.ttf", 56, 7, Caracteres, 1024);
        Mono = Fuente("Mono", "IBMPlexMono-Medium.ttf", 56, 7, Caracteres, 1024);

        var reserva = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        foreach (var f in new[] { Texto, TextoFuerte, Titulos, Etiquetas, Mono })
        {
            f.fallbackFontAssetTable = new List<TMP_FontAsset> { Iconos };
            if (reserva != null) f.fallbackFontAssetTable.Add(reserva);
            EditorUtility.SetDirty(f);
        }
        // <b> en los textos usa la variante seminegrita real en lugar de una negrita simulada.
        Texto.fontWeightTable[7].regularTypeface = TextoFuerte;
        Etiquetas.fontWeightTable[7].regularTypeface = Titulos;
        EditorUtility.SetDirty(Texto);
        EditorUtility.SetDirty(Etiquetas);
        AssetDatabase.SaveAssets();
    }

    static TMP_FontAsset Fuente(string nombre, string archivo, int muestreo, int relleno, string caracteres, int atlas)
    {
        var ruta = $"{CarpetaFuentes}/{nombre} SDF.asset";
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (existente != null)
        {
            // Si se añadieron caracteres (por ejemplo, iconos nuevos), se agregan al atlas existente.
            if (!existente.HasCharacters(caracteres, out var nuevos, false, false) && nuevos != null && nuevos.Length > 0)
            {
                existente.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                existente.TryAddCharacters(caracteres, out _);
                existente.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(existente);
                AssetDatabase.SaveAssets();
            }
            return existente;
        }

        var ttf = AssetDatabase.LoadAssetAtPath<Font>($"{CarpetaFuentes}/{archivo}");
        if (ttf == null) throw new FileNotFoundException("Falta la fuente " + archivo + " en " + CarpetaFuentes);
        var fa = TMP_FontAsset.CreateFontAsset(ttf, muestreo, relleno, GlyphRenderMode.SDFAA, atlas, atlas,
            AtlasPopulationMode.Dynamic, false);
        fa.name = nombre + " SDF";
        fa.TryAddCharacters(caracteres, out var faltan);
        if (!string.IsNullOrEmpty(faltan) && nombre != "Iconos")
            Debug.Log($"ConstructorEstilo: {nombre} no tiene {faltan.Length} caracteres; los pondrá la fuente de reserva.");

        AssetDatabase.CreateAsset(fa, ruta);
        fa.atlasTextures[0].name = nombre + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = nombre + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        // Atlas fijo: en WebGL no hace falta generar glifos en tiempo de ejecución.
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static string TextoLatino()
    {
        var sb = new System.Text.StringBuilder();
        for (int c = 32; c < 127; c++) sb.Append((char)c);
        for (int c = 160; c < 256; c++) sb.Append((char)c);
        sb.Append("–—‘’“”•…€™←→↑↓·");
        return sb.ToString();
    }

    // ---------- Arte.asset ----------

    public static ArteDatos Arte()
    {
        var arte = AssetDatabase.LoadAssetAtPath<ArteDatos>(RutaArte);
        if (arte == null)
        {
            arte = ScriptableObject.CreateInstance<ArteDatos>();
            AssetDatabase.CreateAsset(arte, RutaArte);
        }

        arte.evidencias = new List<ArteDatos.Evidencia>
        {
            Evidencia("termica", "Evidencia_termica", "NASA/JPL/Arizona State University (THEMIS, Mars Odyssey). Infrarrojo nocturno; colores añadidos."),
            Evidencia("radar", "Evidencia_radar", "NASA/JPL-Caltech/ASI/University of Rome/PSI/Smithsonian Institution (SHARAD, MRO)."),
            Evidencia("alta_res", "Evidencia_alta_res", "NASA/JPL-Caltech/Univ. of Arizona (HiRISE, MRO)."),
            Evidencia("espectrometro", "Evidencia_espectrometro", "NASA/JPL-Caltech/UA/JHUAPL (CRISM y HiRISE, MRO)."),
            Evidencia("atmosferico", "Evidencia_atmosferico", "NASA/JPL-Caltech/MSSS (MARCI, MRO)."),
        };
        arte.imagenMision = Imagen("Evidencias/Evidencia_mision");
        arte.creditoMision = "NASA/JPL-Caltech/MSSS (MARCI, MRO). Tormenta global de 2018.";

        // Audio por nombre de archivo en Assets/Arte/Audio (puede faltar: el juego queda en silencio).
        arte.musicaMenu = Audio("Musica/musica_menu");
        arte.musicaConfiguracion = Audio("Musica/musica_configuracion");
        arte.musicaInvestigacion = Audio("Musica/musica_investigacion");
        arte.musicaVictoria = Audio("Musica/musica_victoria");
        arte.musicaParcial = Audio("Musica/musica_parcial");
        arte.musicaFracaso = Audio("Musica/musica_fracaso");
        arte.ambienteTormenta = Audio("Efectos/ambiente_tormenta");
        arte.hover = Audio("Efectos/ui_hover");
        arte.click = Audio("Efectos/ui_click");
        arte.elegir = Audio("Efectos/ui_elegir");
        arte.quitar = Audio("Efectos/ui_quitar");
        arte.pestana = Audio("Efectos/ui_pestana");
        arte.error = Audio("Efectos/ui_error");
        arte.luzVerde = Audio("Efectos/ui_luz_verde");
        arte.luzRoja = Audio("Efectos/ui_luz_roja");
        arte.lanzamiento = Audio("Efectos/lanzamiento");
        arte.senal = Audio("Efectos/senal_entrante");
        // Cinemática de lanzamiento y escaneo orbital (paquete de especificaciones del equipo; ids del CSV).
        arte.musicaLanzamiento = Audio("Musica/MUS_Lanzamiento_Cinematica");
        arte.cuentaAtras = Audio("Efectos/SFX_Countdown_System");
        arte.despegue = Audio("Efectos/SFX_Rocket_Liftoff");
        arte.separacion = Audio("Efectos/SFX_Stage_Separation");
        arte.despliegue = Audio("Efectos/SFX_Solar_Array_Deploy");
        arte.transferencia = Audio("Efectos/SFX_Transfer_Swoosh");
        arte.propulsores = Audio("Efectos/SFX_Orbital_Thruster");
        arte.servo = Audio("Efectos/SFX_Satellite_Servo");
        arte.fijarObjetivo = Audio("Efectos/SFX_Target_Lock");
        arte.escaneoTermica = Audio("Efectos/SFX_Scan_Thermal");
        arte.escaneoRadar = Audio("Efectos/SFX_Scan_Radar");
        arte.escaneoAltaRes = Audio("Efectos/SFX_Scan_HighRes");
        arte.escaneoAtmosferico = Audio("Efectos/SFX_Scan_Atmospheric");
        arte.escaneoEspectrometro = Audio("Efectos/SFX_Scan_Spectrometer");
        arte.ambienteOrbital = Audio("Efectos/AMB_Investigacion_Orbital");
        arte.ambienteControl = Audio("Efectos/AMB_Menu_Control");
        arte.voces = new List<ArteDatos.Voz>();
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Arte/Audio/Voces" }))
        {
            var ruta = AssetDatabase.GUIDToAssetPath(guid);
            arte.voces.Add(new ArteDatos.Voz
            {
                transmisionId = Path.GetFileNameWithoutExtension(ruta).Replace("voz_", ""),
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta)
            });
        }

        EditorUtility.SetDirty(arte);
        AssetDatabase.SaveAssets();
        return arte;
    }

    static ArteDatos.Evidencia Evidencia(string id, string archivo, string credito) =>
        new ArteDatos.Evidencia { instrumentoId = id, imagen = Imagen("Evidencias/" + archivo), credito = credito };

    public static Texture2D Imagen(string rutaSinExtension)
    {
        foreach (var ext in new[] { ".jpg", ".png" })
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arte/" + rutaSinExtension + ext);
            if (t != null) return t;
        }
        throw new FileNotFoundException("Falta la imagen Assets/Arte/" + rutaSinExtension);
    }

    public static Sprite Sprite(string nombre) =>
        AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Interfaz/" + nombre + ".png");

    static AudioClip Audio(string rutaSinExtension)
    {
        foreach (var ext in new[] { ".mp3", ".ogg", ".wav" })
        {
            var a = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Arte/Audio/" + rutaSinExtension + ext);
            if (a != null) return a;
        }
        return null;
    }
}
