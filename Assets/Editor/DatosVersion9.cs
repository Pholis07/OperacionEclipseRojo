using UnityEditor;
using UnityEngine;

using SpaceMission;

/// <summary>
/// Versión 9 (Adrian, 2026-11-15): tercer lanzador, el Coloso, con el aspecto del Saturn V de NASA 3D Resources.
/// El más capaz y el más caro; se desbloquea al ganar el nivel 2 (Progreso). Valores de juego, no del Saturn V real.
/// Se puede correr más de una vez. Batch: -executeMethod DatosVersion9.Aplicar
/// </summary>
public static class DatosVersion9
{
    const string Ruta = "Assets/Datos/Lanzador_coloso.asset";

    [MenuItem("Herramientas/Aplicar datos de la versión 9 (Coloso)")]
    public static void Aplicar()
    {
        var l = AssetDatabase.LoadAssetAtPath<LanzadorDatos>(Ruta);
        if (l == null)
        {
            l = ScriptableObject.CreateInstance<LanzadorDatos>();
            AssetDatabase.CreateAsset(l, Ruta);
        }
        l.id = "coloso";
        l.nombre = "Colossus";
        l.costoCr = 520;
        l.capacidadKg = 950;
        l.espacios = 5;
        l.masaInyectadaKg = 2600;
        l.descripcion = "Huge payload and very high cost";
        l.datoNasa = "Looks like NASA's Saturn V, which sent Apollo (about 45 t) toward the Moon. The game uses 2,600 kg toward Mars, 1.4 times the Titan.";
        l.datoNasaEs = "Aspecto del Saturn V de la NASA, que envió el Apollo (unas 45 t) hacia la Luna. El juego usa 2,600 kg hacia Marte, 1.4 veces el Titán.";
        l.fuenteNasa = "NASA";
        EditorUtility.SetDirty(l);

        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        if (!catalogo.lanzadores.Contains(l)) { catalogo.lanzadores.Add(l); EditorUtility.SetDirty(catalogo); }
        AssetDatabase.SaveAssets();
        Debug.Log("[DatosVersion9] Coloso listo.");
    }
}
