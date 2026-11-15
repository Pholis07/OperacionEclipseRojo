using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Explorador con el aspecto del Ares 1 (B) de NASA 3D Resources (ares1b.fbx). El FBX trae casi todo el cohete en
/// una sola malla, así que se parte por altura en las piezas que la cinemática mueve:
/// - el propio cohete (padre): primera etapa con su interestadio y los cuatro motores de separación;
/// - «EtapaSuperior»: la etapa superior, que sigue sola tras la separación (motor encendido en el vacío);
/// - «CofiaFrente» y «CofiaDetras», dentro de la etapa superior: el Orion y la torre de escape partidos en dos
///   mitades con bisagra, que se abren para liberar el satélite (el Ares no lleva cofia; es la adaptación).
/// No tiene propulsores laterales: en la separación cae la primera etapa entera.
/// Colores planos del modelo, con el estilo de tinta de los cohetes (materiales NASA_Cohete_Ares_*).
/// </summary>
public static partial class ConstructorEscenas
{
    /// <summary>Alto del Explorador en la escena (el Titán mide 11 y la torre 17). Igual en DirectorCinematica.</summary>
    public const float AltoExplorador = 7.5f;

    // Cortes en fracción del alto, medidos en el modelo (alto 9.66): el interestadio termina en 5.55 y el Orion
    // empieza en 7.6.
    const float CorteEtapaAres = 0.575f, CorteNarizAres = 0.787f;

    /// <summary>Monta el Ares en «cohete» (base en su origen) y devuelve la etapa superior.</summary>
    static Transform ConstruirAres(Transform cohete)
    {
        var superior = new GameObject("EtapaSuperior").transform;
        superior.SetParent(cohete, false);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + "NASA/ares1b.fbx");
        if (asset == null) { Debug.LogError("Falta el modelo NASA/ares1b.fbx"); return superior; }
        var modelo = Object.Instantiate(asset, cohete, false);
        modelo.name = "Ares_NASA";
        var b = Limites(modelo);
        modelo.transform.localScale *= AltoExplorador / b.size.y;
        b = Limites(modelo);
        modelo.transform.position += new Vector3(cohete.position.x - b.center.x, cohete.position.y - b.min.y, cohete.position.z - b.center.z);

        var mitades = new Dictionary<int, Transform>();
        foreach (var (lado, nombre) in new[] { (-1, "CofiaFrente"), (1, "CofiaDetras") })
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(superior, false);
            mitades[lado] = t;
        }
        float corteEtapa = CorteEtapaAres * AltoExplorador, corteNariz = CorteNarizAres * AltoExplorador;
        var nuevas = new List<(Transform destino, Mesh malla, Material[] mats, Transform origen)>();
        foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            var malla = mf.sharedMesh;
            var vs = malla.vertices;
            var mats = System.Array.ConvertAll(mf.GetComponent<MeshRenderer>().sharedMaterials, m => MaterialNasa("Cohete_Ares", m));
            // 0 = primera etapa, 1 = etapa superior, -1/1+1 = mitades de la nariz (por el lado en X).
            var partes = new Dictionary<int, List<int>[]>();
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var tri = malla.GetTriangles(s);
                for (int k = 0; k < tri.Length; k += 3)
                {
                    var c = cohete.InverseTransformPoint(mf.transform.TransformPoint((vs[tri[k]] + vs[tri[k + 1]] + vs[tri[k + 2]]) / 3f));
                    int parte = c.y < corteEtapa ? 0 : c.y < corteNariz ? 1 : c.x < 0f ? 2 : 3;
                    if (!partes.TryGetValue(parte, out var listas))
                    {
                        listas = new List<int>[malla.subMeshCount];
                        for (int q = 0; q < listas.Length; q++) listas[q] = new List<int>();
                        partes[parte] = listas;
                    }
                    listas[s].Add(tri[k]); listas[s].Add(tri[k + 1]); listas[s].Add(tri[k + 2]);
                }
            }
            foreach (var par in partes)
            {
                var nombre = "Ares_" + mf.name + "_" + new[] { "Etapa1", "Etapa2", "NarizFrente", "NarizDetras" }[par.Key];
                var guardada = GuardarMalla(Compactar(malla, par.Value), nombre);
                // Las mitades de la nariz se ven por dentro al abrirse: material de doble cara.
                var usar = par.Key < 2 ? mats : System.Array.ConvertAll(mats, x => DobleCara(x, x.name + "_DobleCara"));
                var destino = par.Key == 0 ? cohete : par.Key == 1 ? superior : mitades[par.Key == 2 ? -1 : 1];
                nuevas.Add((destino, guardada, usar, mf.transform));
            }
        }
        foreach (var (destino, malla, mats, origen) in nuevas)
        {
            var go = new GameObject(malla.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(destino, false);
            go.transform.SetPositionAndRotation(origen.position, origen.rotation);
            var l = go.transform.lossyScale; var o = origen.lossyScale;
            go.transform.localScale = new Vector3(o.x / l.x, o.y / l.y, o.z / l.z);
            go.GetComponent<MeshFilter>().sharedMesh = malla;
            go.GetComponent<MeshRenderer>().sharedMaterials = mats;
        }
        Object.DestroyImmediate(modelo);

        // Bisagra de cada mitad en su borde inferior exterior (el pivote se mueve sin mover la malla).
        foreach (var par in mitades)
        {
            var hijos = new List<Transform>();
            foreach (Transform h in par.Value) hijos.Add(h);
            if (hijos.Count == 0) continue;
            var lim = Limites(par.Value.gameObject);
            foreach (var h in hijos) h.SetParent(null, true);
            par.Value.position = new Vector3(par.Key < 0 ? lim.min.x : lim.max.x, lim.min.y, lim.center.z);
            foreach (var h in hijos) h.SetParent(par.Value, true);
        }
        return superior;
    }
}
