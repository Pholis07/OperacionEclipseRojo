using UnityEditor;
using UnityEngine;

using SpaceMission;

/// <summary>
/// Versión 9 (Adrian, 2026-11-15): cuatro lanzadores nuevos con modelos de NASA 3D Resources: Pionero (Jupiter-C),
/// Horizonte (Ares 1 A), Goliat (Saturn V en gris) y Coloso (Saturn V). Valores de juego; se desbloquean con Progreso.
/// Se puede correr más de una vez. Batch: -executeMethod DatosVersion9.Aplicar
/// </summary>
public static class DatosVersion9
{
    [MenuItem("Herramientas/Aplicar datos de la versión 9 (lanzadores)")]
    public static void Aplicar()
    {
        Lanzador("pionero", "Pioneer", 110, 430, 1, 1020, "Tiny and cheap: room for only one instrument",
            "Looks like NASA's Jupiter-C, which launched Explorer 1 (about 14 kg) in 1958. The game uses 1,020 kg toward Mars.",
            "Aspecto del Jupiter-C de la NASA, que lanzó el Explorer 1 (unos 14 kg) en 1958. El juego usa 1,020 kg hacia Marte.");
        Lanzador("horizonte", "Horizon", 270, 620, 3, 1500, "Middle ground: three slots at a fair price",
            "Looks like NASA's Ares I, tested in flight in 2009 (Ares I-X). The game uses 1,500 kg toward Mars.",
            "Aspecto del Ares I de la NASA, probado en vuelo en 2009 (Ares I-X). El juego usa 1,500 kg hacia Marte.");
        Lanzador("goliat", "Goliath", 440, 850, 4, 2200, "Heavy: more mass and braking than the Titan",
            "Inspired by NASA's Saturn V, painted dark gray; game values. The game uses 2,200 kg toward Mars.",
            "Inspirado en el Saturn V de la NASA, pintado de gris oscuro; valores de juego. El juego usa 2,200 kg hacia Marte.");
        Lanzador("coloso", "Colossus", 520, 950, 5, 2600, "Huge payload and very high cost",
            "Looks like NASA's Saturn V, which sent Apollo (about 45 t) toward the Moon. The game uses 2,600 kg toward Mars, 1.4 times the Titan.",
            "Aspecto del Saturn V de la NASA, que envió el Apollo (unas 45 t) hacia la Luna. El juego usa 2,600 kg hacia Marte, 1.4 veces el Titán.");

        // En la pestaña Lanzador, del más barato al más caro.
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        catalogo.lanzadores.Sort((a, b) => a.costoCr.CompareTo(b.costoCr));
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        Debug.Log("[DatosVersion9] Lanzadores listos: " + string.Join(", ", catalogo.lanzadores.ConvertAll(l => l.id)));
    }

    static void Lanzador(string id, string nombre, int costo, int capacidad, int espacios, int inyectada, string descripcion, string en, string es)
    {
        var ruta = "Assets/Datos/Lanzador_" + id + ".asset";
        var l = AssetDatabase.LoadAssetAtPath<LanzadorDatos>(ruta);
        if (l == null)
        {
            l = ScriptableObject.CreateInstance<LanzadorDatos>();
            AssetDatabase.CreateAsset(l, ruta);
        }
        l.id = id; l.nombre = nombre; l.costoCr = costo; l.capacidadKg = capacidad; l.espacios = espacios;
        l.masaInyectadaKg = inyectada; l.descripcion = descripcion;
        l.datoNasa = en; l.datoNasaEs = es; l.fuenteNasa = "NASA";
        EditorUtility.SetDirty(l);
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        if (!catalogo.lanzadores.Contains(l)) { catalogo.lanzadores.Add(l); EditorUtility.SetDirty(catalogo); }
    }
}
