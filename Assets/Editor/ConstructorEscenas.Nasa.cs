using System.Collections.Generic;
using SpaceMission;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Modelos 3D de NASA 3D Resources (convertidos a OBJ con Herramientas/modelos_nasa):
/// - Los cinco tipos de satélite que el jugador puede elegir (solo aspecto; sin reglas todavía).
/// - Los módulos de instrumento del kit del equipo (EclipseRojo_Kit_Nave_Orbita), montados sobre el casco de
///   cada satélite con un soporte; los montajes se calculan lanzando rayos contra la malla de cada modelo.
/// - El SLS (Titán), separado en etapa inferior y superior para la separación de la cinemática.
/// </summary>
public static partial class ConstructorEscenas
{
    const string RutaModelos = "Assets/Arte/Modelos/";

    /// <summary>Satélites: id, archivo, tamaño (lado mayor en unidades de la nave) y giro del modelo.</summary>
    static readonly (string id, string modelo, float tamano, Vector3 giro)[] Satelites =
    {
        ("mro", "Sat_MRO", 6.2f, new Vector3(180f, 0f, 0f)),   // la antena grande hacia arriba
        ("magellan", "Sat_Magellan", 5.2f, Vector3.zero),
        ("chandra", "Sat_Chandra", 7.0f, new Vector3(0f, 90f, 0f)),
        ("aquarius", "Sat_Aquarius", 5.0f, Vector3.zero),
        ("llcd", "Sat_LLCD", 7.2f, Vector3.zero),
        ("aztechsat", null, 0f, Vector3.zero),   // sin modelo de NASA: CubeSat 1U hecho por código (CuerpoCubeSat)
    };

    static readonly string[] IdsInstrumento = { "termica", "radar", "alta_res", "atmosferico", "espectrometro" };
    static readonly string[] ModelosInstrumento =
    {
        "01_Camara_Termica_Infrarroja", "02_Radar_de_Subsuelo", "03_Camara_Alta_Resolucion",
        "04_Sensor_Atmosferico", "05_Espectrometro_Mineralogico"
    };

    /// <summary>Lado mayor de cada módulo de instrumento, en unidades de la nave.</summary>
    const float TamanoModulo = 0.85f;

    // ---------- Datos ----------

    /// <summary>Crea (o actualiza) los datos de los cinco satélites y los añade al catálogo.</summary>
    [MenuItem("Herramientas/Crear datos de satélites")]
    public static void CrearDatosSatelites()
    {
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);
        if (catalogo == null) return;
        var textos = new Dictionary<string, (string nombre, string inspirado, string descripcion)>
        {
            ["mro"] = ("Orbitador MRO", "Mars Reconnaissance Orbiter (2005)",
                "Estudia Marte desde 2006 con cámaras, radar y espectrómetro. Antena grande y dos paneles solares amplios."),
            ["magellan"] = ("Cartógrafo Magellan", "Magellan (1989)",
                "Cartografió por radar casi toda la superficie de Venus. Su gran antena servía también de radar."),
            ["chandra"] = ("Observatorio Chandra", "Chandra (1999)",
                "Telescopio espacial de rayos X: un tubo óptico muy largo con dos alas de paneles solares."),
            ["aquarius"] = ("Plataforma Aquarius", "Aquarius / SAC-D (2011)",
                "Midió la salinidad de los océanos desde la órbita de la Tierra, con un gran reflector de antena."),
            ["llcd"] = ("Relé láser LLCD", "LLCD (2013)",
                "Demostró comunicaciones por láser desde la órbita de la Luna. Plataforma con grandes paneles solares."),
            ["aztechsat"] = ("Nanosatélite AztechSat-1", "AztechSat-1 (UPAEP, 2019)",
                "CubeSat 1U de la UPAEP (Puebla). Liberado desde la Estación Espacial en 2020, probó a comunicarse a través de la constelación Globalstar."),
        };
        var lista = new List<SateliteDatos>();
        foreach (var s in Satelites)
        {
            var ruta = "Assets/Datos/Satelite_" + s.id + ".asset";
            var datos = AssetDatabase.LoadAssetAtPath<SateliteDatos>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<SateliteDatos>();
                AssetDatabase.CreateAsset(datos, ruta);
            }
            var t = textos[s.id];
            datos.id = s.id;
            datos.nombre = t.nombre;
            datos.inspiradoEn = t.inspirado;
            datos.descripcion = t.descripcion;
            datos.credito = s.modelo != null ? "Modelo 3D: NASA 3D Resources" : "Modelo 3D: propio del equipo (hecho por código)";
            EditorUtility.SetDirty(datos);
            lista.Add(datos);
        }
        catalogo.satelites = lista;
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
    }

    // ---------- Nave ----------

    /// <summary>
    /// Nave con los cinco cuerpos de satélite (solo el elegido encendido) y un módulo por instrumento,
    /// en el orden del catálogo. Los módulos aparecen al elegir el instrumento (NaveUI).
    /// </summary>
    static NaveUI ConstruirNave(string sateliteFijo = null)
    {
        var raiz = new GameObject("Nave");
        var matSoporte = CrearMaterial("Nave_Plata", Hex("#C9CED6"), 0.85f, 0.7f);

        // Cuerpos de los satélites, centrados en la raíz.
        var cuerpos = new GameObject[Satelites.Length];
        for (int s = 0; s < Satelites.Length; s++)
        {
            cuerpos[s] = Satelites[s].modelo == null
                ? CuerpoCubeSat(raiz.transform, "Cuerpo_" + Satelites[s].id)
                : InstanciarModelo("NASA/" + Satelites[s].modelo, "Cuerpo_" + Satelites[s].id, raiz.transform,
                    Satelites[s].tamano, Satelites[s].giro, true);
            cuerpos[s].SetActive(s == 0);
        }

        // Módulos de instrumento (modelos del kit del equipo) con un soporte que los une al casco.
        var modulos = new Transform[IdsInstrumento.Length];
        for (int i = 0; i < IdsInstrumento.Length; i++)
        {
            var m = new GameObject("Modulo_" + IdsInstrumento[i]).transform;
            m.SetParent(raiz.transform, false);
            var equipo = InstanciarModelo("Instrumentos/" + ModelosInstrumento[i], "Equipo", m, TamanoModulo, Vector3.zero, false);
            // La base del modelo del kit está en y = 0: así queda apoyada en el origen del módulo.
            var b = Limites(equipo);
            equipo.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            var soporte = Pieza(PrimitiveType.Cylinder, "Soporte", m, Vector3.zero, Vector3.zero, new Vector3(0.12f, 0.05f, 0.12f), matSoporte);
            soporte.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            m.gameObject.AddComponent<Instalacion>();
            modulos[i] = m;
            m.gameObject.SetActive(false);
        }

        // Montajes de cada satélite: debajo del casco (la cara que mira a Marte), con el soporte hasta la superficie.
        var posiciones = new List<Vector3>();
        var rotaciones = new List<Quaternion>();
        var largos = new List<float>();
        for (int s = 0; s < Satelites.Length; s++)
        {
            foreach (var montaje in CalcularMontajes(cuerpos[s], IdsInstrumento.Length))
            {
                posiciones.Add(montaje.posicion);
                rotaciones.Add(montaje.rotacion);
                largos.Add(montaje.largo);
            }
        }

        var nave = raiz.AddComponent<NaveUI>();
        var so = new SerializedObject(nave);
        var pIds = so.FindProperty("idsInstrumento");
        var pModulos = so.FindProperty("modulos");
        pIds.arraySize = IdsInstrumento.Length;
        pModulos.arraySize = IdsInstrumento.Length;
        for (int i = 0; i < IdsInstrumento.Length; i++)
        {
            pIds.GetArrayElementAtIndex(i).stringValue = IdsInstrumento[i];
            pModulos.GetArrayElementAtIndex(i).objectReferenceValue = modulos[i].gameObject;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        var aspecto = raiz.AddComponent<NaveSatelite>();
        var sa = new SerializedObject(aspecto);
        Lista(sa, "idsSatelite", Satelites.Length, (p, k) => p.stringValue = Satelites[k].id);
        Lista(sa, "cuerpos", cuerpos.Length, (p, k) => p.objectReferenceValue = cuerpos[k]);
        Lista(sa, "modulos", modulos.Length, (p, k) => p.objectReferenceValue = modulos[k]);
        Lista(sa, "posiciones", posiciones.Count, (p, k) => p.vector3Value = posiciones[k]);
        Lista(sa, "rotaciones", rotaciones.Count, (p, k) => p.quaternionValue = rotaciones[k]);
        Lista(sa, "largosSoporte", largos.Count, (p, k) => p.floatValue = largos[k]);
        sa.FindProperty("sateliteFijo").stringValue = sateliteFijo ?? "";
        sa.ApplyModifiedPropertiesWithoutUndo();
        aspecto.Aplicar(string.IsNullOrEmpty(sateliteFijo) ? Satelites[0].id : sateliteFijo);

        raiz.transform.rotation = Quaternion.Euler(12, -28, 0);
        return nave;
    }

    /// <summary>
    /// AztechSat-1: CubeSat 1U hecho con piezas simples (no hay modelo de NASA y solo se usan assets propios o CC0).
    /// Cubo de aluminio con celdas solares en cuatro caras, rieles en las aristas y antenas de alambre detrás.
    /// La cara de delante (−Z) queda libre para montar los instrumentos.
    /// </summary>
    static GameObject CuerpoCubeSat(Transform padre, string nombre)
    {
        const float lado = 2.2f;
        var cuerpo = new GameObject(nombre).transform;
        cuerpo.SetParent(padre, false);
        var aluminio = CrearMaterial("CubeSat_Aluminio", Hex("#A7ADB5"), 0.8f, 0.55f);
        var celdas = CrearMaterial("CubeSat_Celdas", Hex("#1B2A4A"), 0.55f, 0.8f);
        var riel = CrearMaterial("CubeSat_Riel", Hex("#D9DDE2"), 0.85f, 0.6f);
        var antena = CrearMaterial("CubeSat_Antena", Hex("#C8A24A"), 0.85f, 0.6f);

        Pieza(PrimitiveType.Cube, "Casco", cuerpo, Vector3.zero, Vector3.zero, Vector3.one * lado, aluminio);
        float h = lado / 2f + 0.01f, celda = lado * 0.82f;
        Pieza(PrimitiveType.Cube, "Celdas_D", cuerpo, new Vector3(h, 0, 0), Vector3.zero, new Vector3(0.02f, celda, celda), celdas);
        Pieza(PrimitiveType.Cube, "Celdas_I", cuerpo, new Vector3(-h, 0, 0), Vector3.zero, new Vector3(0.02f, celda, celda), celdas);
        Pieza(PrimitiveType.Cube, "Celdas_A", cuerpo, new Vector3(0, h, 0), Vector3.zero, new Vector3(celda, 0.02f, celda), celdas);
        Pieza(PrimitiveType.Cube, "Celdas_B", cuerpo, new Vector3(0, -h, 0), Vector3.zero, new Vector3(celda, 0.02f, celda), celdas);
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? 1 : -1) * lado / 2f, y = (i < 2 ? 1 : -1) * lado / 2f;
            Pieza(PrimitiveType.Cube, "Riel_" + i, cuerpo, new Vector3(x, y, 0), Vector3.zero, new Vector3(0.14f, 0.14f, lado * 1.08f), riel);
        }
        // Antenas de cinta en cruz, en la cara de atrás.
        float largo = 1.4f, z = lado / 2f + 0.03f;
        Pieza(PrimitiveType.Cylinder, "Antena_D", cuerpo, new Vector3(largo / 2f + 0.3f, 0, z), new Vector3(0, 0, 90), new Vector3(0.03f, largo / 2f, 0.03f), antena);
        Pieza(PrimitiveType.Cylinder, "Antena_I", cuerpo, new Vector3(-largo / 2f - 0.3f, 0, z), new Vector3(0, 0, 90), new Vector3(0.03f, largo / 2f, 0.03f), antena);
        Pieza(PrimitiveType.Cylinder, "Antena_A", cuerpo, new Vector3(0, largo / 2f + 0.3f, z), Vector3.zero, new Vector3(0.03f, largo / 2f, 0.03f), antena);
        Pieza(PrimitiveType.Cylinder, "Antena_B", cuerpo, new Vector3(0, -largo / 2f - 0.3f, z), Vector3.zero, new Vector3(0.03f, largo / 2f, 0.03f), antena);
        return cuerpo.gameObject;
    }

    static void Lista(SerializedObject so, string campo, int n, System.Action<SerializedProperty, int> poner)
    {
        var p = so.FindProperty(campo);
        p.arraySize = n;
        for (int k = 0; k < n; k++) poner(p.GetArrayElementAtIndex(k), k);
    }

    struct Montaje
    {
        public Vector3 posicion;
        public Quaternion rotacion;
        public float largo;
    }

    /// <summary>
    /// Busca dónde atornillar cada módulo: rayos desde delante del satélite (−Z, la cara que mira a la cámara, al
    /// Sol y, en órbita, a Marte), en un anillo alrededor del centro del cuerpo. El módulo queda sobre el casco
    /// con su soporte, apuntando hacia fuera.
    /// </summary>
    static List<Montaje> CalcularMontajes(GameObject cuerpo, int cantidad)
    {
        var tris = new List<(Vector3 a, Vector3 b, Vector3 c)>();
        var raiz = cuerpo.transform.parent;
        foreach (var mf in cuerpo.GetComponentsInChildren<MeshFilter>(true))
        {
            var malla = mf.sharedMesh;
            var v = malla.vertices;
            var t = malla.triangles;
            for (int k = 0; k < t.Length; k += 3)
                tris.Add((raiz.InverseTransformPoint(mf.transform.TransformPoint(v[t[k]])),
                          raiz.InverseTransformPoint(mf.transform.TransformPoint(v[t[k + 1]])),
                          raiz.InverseTransformPoint(mf.transform.TransformPoint(v[t[k + 2]]))));
        }

        var caja = Limites(cuerpo);
        caja.center = raiz.InverseTransformPoint(caja.center);
        // Tamaño del «cuerpo»: el lado menor de la caja en XY (los paneles ensanchan el otro lado).
        float radioCuerpo = Mathf.Min(caja.extents.x, caja.extents.y) * 0.5f;
        float desdeDelante = caja.min.z - 1f;
        var resultado = new List<Montaje>();
        for (int i = 0; i < cantidad; i++)
        {
            // El radar (i = 1) va al centro; los demás en anillo.
            Vector2 offset = Vector2.zero;
            if (i != 1)
            {
                int j = i < 1 ? i : i - 1;
                float ang = (j * 90f + 45f) * Mathf.Deg2Rad;
                offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radioCuerpo;
            }
            var origen = new Vector3(caja.center.x + offset.x, caja.center.y + offset.y, desdeDelante);
            Vector3 punto, normal;
            if (!Rayo(tris, origen, Vector3.forward, out punto, out normal))
            {
                // Sin casco debajo de ese punto: se acerca al centro hasta encontrarlo.
                bool hallado = false;
                for (float f = 0.75f; f > 0.05f && !hallado; f -= 0.2f)
                    hallado = Rayo(tris, new Vector3(caja.center.x + offset.x * f, caja.center.y + offset.y * f, desdeDelante),
                        Vector3.forward, out punto, out normal);
                if (!hallado) { punto = new Vector3(origen.x, origen.y, caja.min.z); normal = Vector3.back; }
            }
            if (normal.z > -0.3f) normal = Vector3.back; // caras de canto: el módulo sale recto hacia delante
            const float hueco = 0.07f;
            var arriba = normal.normalized;
            var frente = Vector3.ProjectOnPlane(Vector3.down, arriba);
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.ProjectOnPlane(Vector3.right, arriba);
            resultado.Add(new Montaje
            {
                posicion = punto + arriba * hueco,
                rotacion = Quaternion.LookRotation(frente.normalized, arriba),
                largo = hueco / 2f,
            });
        }
        return resultado;
    }

    /// <summary>Rayo contra triángulos (Möller–Trumbore), el impacto más cercano y su normal hacia el rayo.</summary>
    static bool Rayo(List<(Vector3 a, Vector3 b, Vector3 c)> tris, Vector3 origen, Vector3 dir, out Vector3 punto, out Vector3 normal)
    {
        float mejor = float.MaxValue;
        punto = normal = Vector3.zero;
        foreach (var (a, b, c) in tris)
        {
            var e1 = b - a; var e2 = c - a;
            var p = Vector3.Cross(dir, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-9f) continue;
            float inv = 1f / det;
            var s = origen - a;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0f || u > 1f) continue;
            var q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(dir, q) * inv;
            if (v < 0f || u + v > 1f) continue;
            float t = Vector3.Dot(e2, q) * inv;
            if (t <= 0f || t >= mejor) continue;
            mejor = t;
            punto = origen + dir * t;
            normal = Vector3.Cross(e1, e2).normalized;
            if (Vector3.Dot(normal, dir) > 0f) normal = -normal;
        }
        return mejor < float.MaxValue;
    }

    /// <summary>
    /// Copia un modelo importado (sin enlace de prefab), con el lado mayor de «tamano» y centrado en el padre.
    /// Con «materialesPropios», sus materiales se cambian por materiales URP con brillo según la pieza
    /// (láminas doradas y plateadas metálicas, paneles solares satinados).
    /// </summary>
    static GameObject InstanciarModelo(string ruta, string nombre, Transform padre, float tamano, Vector3 giro, bool materialesPropios)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + ruta + ".obj");
        if (asset == null) { Debug.LogError("Falta el modelo " + ruta); return new GameObject(nombre); }
        var go = Object.Instantiate(asset, padre, false);
        go.name = nombre;
        go.transform.localRotation = Quaternion.Euler(giro);
        var b = Limites(go);
        float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        go.transform.localScale = Vector3.one * (tamano / Mathf.Max(0.0001f, mayor));
        b = Limites(go);
        go.transform.position -= b.center - (padre != null ? padre.position : Vector3.zero);
        if (materialesPropios)
        {
            var modelo = System.IO.Path.GetFileName(ruta);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int k = 0; k < mats.Length; k++) mats[k] = MaterialNasa(modelo, mats[k]);
                r.sharedMaterials = mats;
            }
        }
        return go;
    }

    static Material MaterialNasa(string modelo, Material original)
    {
        if (original == null) return null;
        var n = original.name.ToLowerInvariant();
        // La textura se busca por nombre (<modelo>_<material>.png, como en el .mtl): el importador de OBJ de Unity
        // a veces asigna a cada material la textura del anterior, según el orden en que se importaron los archivos.
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaModelos + "NASA/" + modelo + "_" + original.name.Replace(" ", "_") + ".png")
                      ?? (original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") as Texture2D : original.mainTexture as Texture2D);
        var color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
        float metal = 0.15f, suave = 0.4f;
        if (n.Contains("foil") || n.Contains("gold")) { metal = 0.85f; suave = 0.62f; }
        else if (n.Contains("silver") || n.Contains("alumin")) { metal = 0.8f; suave = 0.55f; }
        else if (n.Contains("panel")) { metal = 0.55f; suave = 0.78f; }
        else if (n.Contains("black") || n.Contains("krinkle")) { metal = 0.3f; suave = 0.35f; }
        // SLS: naranja de la espuma aislante (mate), blanco de los propulsores, oscuro de toberas y faldones.
        if (n == "naranja") { color = Hex("#C8651E"); metal = 0f; suave = 0.25f; }
        else if (n == "blanco") { color = Hex("#ECEEF0"); metal = 0.05f; suave = 0.5f; }
        else if (n == "oscuro") { color = Hex("#2B2F36"); metal = 0.6f; suave = 0.45f; }
        var nombre = "NASA_" + modelo.Replace("_Inferior", "").Replace("_Superior", "") + "_" + original.name.Replace(" ", "_");
        return CrearMaterial(nombre, textura != null ? Color.white : color, metal, suave, textura, Vector2.one);
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // ---------- SLS ----------

    /// <summary>
    /// El SLS (Titán) de NASA 3D Resources, de «alto» unidades, con la base en el origen del padre. Devuelve las dos
    /// partes: la inferior (etapa central y propulsores) y la superior (etapa superior y Orion).
    /// </summary>
    static (GameObject inferior, GameObject superior) ConstruirSls(Transform padre, float alto)
    {
        var raiz = new GameObject("SLS_NASA").transform;
        raiz.SetParent(padre, false);
        // Las dos partes comparten escala y origen: juntas forman el cohete completo (lado mayor = 2 en el OBJ).
        var inferior = InstanciarParte("NASA/Cohete_SLS_Inferior", "EtapaInferior", raiz, alto / 2f);
        var superior = InstanciarParte("NASA/Cohete_SLS_Superior", "EtapaSuperior", raiz, alto / 2f);
        raiz.localPosition = new Vector3(0, alto / 2f, 0);   // el OBJ está centrado: se sube para apoyarlo en el suelo
        return (inferior, superior);
    }

    static GameObject InstanciarParte(string ruta, string nombre, Transform padre, float escala)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + ruta + ".obj");
        if (asset == null) { Debug.LogError("Falta el modelo " + ruta); return new GameObject(nombre); }
        var go = Object.Instantiate(asset, padre, false);
        go.name = nombre;
        go.transform.localScale = Vector3.one * escala;
        var modelo = System.IO.Path.GetFileName(ruta);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int k = 0; k < mats.Length; k++) mats[k] = MaterialNasa(modelo, mats[k]);
            r.sharedMaterials = mats;
        }
        return go;
    }
}
