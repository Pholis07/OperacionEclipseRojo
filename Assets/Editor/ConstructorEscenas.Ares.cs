using SpaceMission;
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

    /// <summary>
    /// Versión 9: los lanzadores que se suman al Explorador y al Titán, todos de NASA 3D Resources sin banderas ni
    /// «USA» (ver CREDITOS.md). Cortes en fracción del alto, medidos en cada modelo:
    /// - Jupiter-C: la primera etapa termina en 0.76 y el racimo de etapas superiores en 0.90.
    /// - Ares 1 (A): mismo perfil que el Ares 1 (B).
    /// - Saturn V: el S-II termina en 0.62 (el radio pasa de 0.62 a 0.43) y el adaptador con el Apollo empieza en 0.79.
    /// El alto también está en PlataformaCohetes.Alto (la cinemática y la cámara lo usan).
    /// </summary>
    static readonly (string id, string ruta, string prefijo, float etapa, float nariz, Color? tinte, int motores, float x)[] CohetesExtra =
    {
        ("pionero", "NASA/jupiterc.fbx", "JupiterC", 0.76f, 0.90f, null, 1, -4.4f),
        ("horizonte", "NASA/ares1a.fbx", "AresA", 0.575f, 0.787f, new Color(2.1f, 2.05f, 1.9f), 1, -2.2f),   // gris oscuro → claro
        ("goliat", "NASA/saturnv.fbx", "Goliat", 0.62f, 0.79f, new Color(0.42f, 0.44f, 0.5f), 5, 1.4f),       // Saturn V en gris pizarra
        ("coloso", "NASA/saturnv.fbx", "SaturnV", 0.62f, 0.79f, null, 5, 3.6f),
    };

    /// <summary>Monta el Ares en «cohete» (base en su origen) y devuelve la etapa superior.</summary>
    static Transform ConstruirAres(Transform cohete) =>
        ConstruirPorEtapas(cohete, "NASA/ares1b.fbx", "Ares", AltoExplorador, CorteEtapaAres, CorteNarizAres);

    /// <summary>
    /// Un lanzador de la tabla: el cohete «Cohete_id» en la plataforma, con su logo y su resalte. Lo usan la pestaña
    /// Lanzador y la cinemática (que le añade las llamas con LlamasExtra).
    /// </summary>
    static (string id, Transform cohete, GameObject resalte) ConstruirCoheteExtra(Transform raiz, int i, System.Func<string, Vector3, GameObject> resalte)
    {
        var d = CohetesExtra[i];
        float alto = PlataformaCohetes.Alto(d.id);
        var cohete = new GameObject("Cohete_" + d.id).transform;
        cohete.SetParent(raiz, false);
        cohete.localPosition = new Vector3(d.x, 0, 0);
        var superior = ConstruirPorEtapas(cohete, d.ruta, d.prefijo, alto, d.etapa, d.nariz, d.tinte);
        float alturaLogo = alto * Mathf.Lerp(d.etapa, d.nariz, 0.45f);
        LogoPatrocinio(superior, "Fence", false, RadioNucleo(cohete, alturaLogo) + 0.012f, alturaLogo, alto * 0.044f, cohete);
        cohete.gameObject.AddComponent<GiroLento>().enabled = false;
        return (d.id, cohete, resalte("Elegido_" + d.id, cohete.localPosition));
    }

    /// <summary>Llamas de un lanzador de la tabla: las del pie (1 o 5 motores) y la de la etapa superior.</summary>
    static (List<Renderer> pie, List<Renderer> superior) LlamasExtra(Transform cohete, string id)
    {
        var d = System.Array.Find(CohetesExtra, c => c.id == id);
        float alto = PlataformaCohetes.Alto(id);
        var toberas = new List<(Transform, Vector3, float, float)> { (cohete, new Vector3(0f, 0.05f, 0f), alto * 0.013f, alto * 0.44f) };
        if (d.motores == 5)   // Saturn V: cinco F-1, uno central y cuatro en cruz
            for (int k = 0; k < 4; k++)
                toberas.Add((cohete, Quaternion.Euler(0, 45f + 90f * k, 0) * Vector3.forward * alto * 0.024f + Vector3.up * 0.05f, alto * 0.011f, alto * 0.4f));
        var superior = cohete.Find("EtapaSuperior");
        return (ConstruirLlamas(toberas), ConstruirLlamas(new[] { (superior, new Vector3(0f, d.etapa * alto, 0f), alto * 0.013f, alto * 0.16f) }));
    }

    /// <summary>
    /// Instancia un modelo de una pieza, lo escala a «alto», lo apoya en el origen de «cohete» y lo parte por altura:
    /// primera etapa (en el cohete), etapa superior y la nariz en dos mitades con bisagra. Devuelve la etapa superior.
    /// </summary>
    static Transform ConstruirPorEtapas(Transform cohete, string ruta, string prefijo, float alto, float fraccionEtapa, float fraccionNariz,
        Color? tinte = null)
    {
        var superior = new GameObject("EtapaSuperior").transform;
        superior.SetParent(cohete, false);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + ruta);
        if (asset == null) { Debug.LogError("Falta el modelo " + ruta); return superior; }
        var modelo = Object.Instantiate(asset, cohete, false);
        modelo.name = prefijo + "_NASA";
        var b = Limites(modelo);
        modelo.transform.localScale *= alto / b.size.y;
        b = Limites(modelo);
        modelo.transform.position += new Vector3(cohete.position.x - b.center.x, cohete.position.y - b.min.y, cohete.position.z - b.center.z);

        var mitades = new Dictionary<int, Transform>();
        foreach (var (lado, nombre) in new[] { (-1, "CofiaFrente"), (1, "CofiaDetras") })
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(superior, false);
            mitades[lado] = t;
        }
        float corteEtapa = fraccionEtapa * alto, corteNariz = fraccionNariz * alto;
        var nuevas = new List<(Transform destino, Mesh malla, Material[] mats, Transform origen)>();
        foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            var malla = mf.sharedMesh;
            var vs = malla.vertices;
            var mats = System.Array.ConvertAll(mf.GetComponent<MeshRenderer>().sharedMaterials, m => Tenir(MaterialNasa("Cohete_" + prefijo, m), tinte));
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
                var nombre = prefijo + "_" + mf.name + "_" + new[] { "Etapa1", "Etapa2", "NarizFrente", "NarizDetras" }[par.Key];
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

    /// <summary>Multiplica el color del material (recién puesto por CrearMaterial, así que no se acumula).</summary>
    static Material Tenir(Material m, Color? tinte)
    {
        if (m == null || !tinte.HasValue) return m;
        var c = m.GetColor("_BaseColor") * tinte.Value;
        m.SetColor("_BaseColor", new Color(Mathf.Min(c.r, 1f), Mathf.Min(c.g, 1f), Mathf.Min(c.b, 1f), 1f));
        EditorUtility.SetDirty(m);
        return m;
    }
}
