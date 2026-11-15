using SpaceMission;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renovación visual: geometría limpia de los cohetes que se parten por etapas. Los modelos de NASA 3D Resources
/// llegan en una pieza y con defectos propios (nodos repetidos en el mismo sitio, triángulos al revés), así que aquí:
/// - se descartan los nodos repetidos (parpadeaban uno sobre otro);
/// - se corrige el sentido de los triángulos de las mallas que lo traen mezclado (se veía el interior a trozos);
/// - el corte entre etapas parte los triángulos en el plano (antes iba por triángulos enteros: borde en sierra);
/// - cada boca cortada lleva una tapa interior (antes se veía el tubo hueco y el contorno lo llenaba de tinta);
/// - se mide el cohete (PerfilCohete) para los brazos de la torre, la carga de la cofia, las llamas y los logos.
/// Todo queda en el espacio del cohete: una malla por parte, con una submalla por material.
/// </summary>
public static partial class ConstructorEscenas
{
    struct VerticeCorte
    {
        public Vector3 p, n;
        public Vector2 uv;
        public long clave;
    }

    /// <summary>Una parte del cohete en construcción: vértices compartidos y una lista de triángulos por material.</summary>
    class ParteMalla
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Material> mats = new List<Material>();
        public readonly List<List<int>> sub = new List<List<int>>();
        readonly Dictionary<long, int> mapa = new Dictionary<long, int>();

        public List<int> Sub(Material m)
        {
            int i = mats.IndexOf(m);
            if (i < 0) { mats.Add(m); sub.Add(new List<int>()); i = mats.Count - 1; }
            return sub[i];
        }

        public int Vertice(VerticeCorte c)
        {
            if (mapa.TryGetValue(c.clave, out var i)) return i;
            v.Add(c.p); n.Add(c.n); uv.Add(c.uv);
            return mapa[c.clave] = v.Count - 1;
        }

        public void Poligono(List<VerticeCorte> poli, Material m)
        {
            if (poli.Count < 3) return;
            var lista = Sub(m);
            int a = Vertice(poli[0]);
            for (int k = 1; k + 1 < poli.Count; k++)
            {
                int b = Vertice(poli[k]), c = Vertice(poli[k + 1]);
                if (a == b || b == c || a == c) continue;
                lista.Add(a); lista.Add(b); lista.Add(c);
            }
        }

        public bool Vacia => sub.All(s => s.Count == 0);

        public Mesh Malla()
        {
            var m = new Mesh { indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            m.subMeshCount = mats.Count;
            for (int s = 0; s < mats.Count; s++) m.SetTriangles(sub[s], s);
            m.RecalculateBounds();
            return m;
        }
    }

    static long clavesNuevas;
    static readonly Dictionary<(long, long, int), long> clavesCorte = new Dictionary<(long, long, int), long>();

    /// <summary>
    /// Parte un polígono convexo por un plano (distancia con signo «d» de cada vértice): lo negativo a «menos» y lo
    /// positivo a «mas». Los vértices nuevos del corte se comparten entre triángulos vecinos (misma clave).
    /// </summary>
    static void Partir(List<VerticeCorte> poli, System.Func<Vector3, float> d, int plano, List<VerticeCorte> menos, List<VerticeCorte> mas)
    {
        const float eps = 1e-5f;
        menos.Clear(); mas.Clear();
        for (int i = 0; i < poli.Count; i++)
        {
            var a = poli[i]; var b = poli[(i + 1) % poli.Count];
            float da = d(a.p), db = d(b.p);
            if (da <= eps) menos.Add(a);
            if (da >= -eps) mas.Add(a);
            if ((da < -eps && db > eps) || (da > eps && db < -eps))
            {
                var (x, y) = a.clave < b.clave ? (a, b) : (b, a);
                float dx = d(x.p), dy = d(y.p);
                float t = dx / (dx - dy);
                if (!clavesCorte.TryGetValue((x.clave, y.clave, plano), out var clave))
                    clavesCorte[(x.clave, y.clave, plano)] = clave = --clavesNuevas;
                var corte = new VerticeCorte
                {
                    p = Vector3.Lerp(x.p, y.p, t), n = Vector3.Lerp(x.n, y.n, t).normalized, uv = Vector2.Lerp(x.uv, y.uv, t), clave = clave,
                };
                menos.Add(corte); mas.Add(corte);
            }
        }
        if (menos.Count < 3) menos.Clear();
        if (mas.Count < 3) mas.Clear();
    }

    /// <summary>Un nodo del modelo ya en el espacio del cohete.</summary>
    class NodoCohete
    {
        public string nombre;
        public Vector3[] v, n;
        public Vector2[] uv;
        public List<int[]> tri = new List<int[]>();   // por submalla
        public Material[] mats;
        public string[] nombresMats;   // los del modelo, sin prefijo
        public Bounds limites;
    }

    static List<NodoCohete> NodosDe(GameObject modelo, Transform cohete, System.Func<Material, Material> material)
    {
        var lista = new List<NodoCohete>();
        foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>(true))
        {
            var malla = mf.sharedMesh;
            if (malla == null) continue;
            var a = cohete.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var vs = malla.vertices; var ns = malla.normals; var us = malla.uv;
            var nodo = new NodoCohete { nombre = mf.name, v = new Vector3[vs.Length], n = new Vector3[vs.Length], uv = new Vector2[vs.Length] };
            for (int i = 0; i < vs.Length; i++)
            {
                nodo.v[i] = a.MultiplyPoint3x4(vs[i]);
                nodo.n[i] = ns.Length == vs.Length ? a.MultiplyVector(ns[i]).normalized : Vector3.up;
                if (us.Length == vs.Length) nodo.uv[i] = us[i];
            }
            for (int s = 0; s < malla.subMeshCount; s++) nodo.tri.Add(malla.GetTriangles(s));
            nodo.mats = System.Array.ConvertAll(mf.GetComponent<MeshRenderer>().sharedMaterials, m => material(m));
            nodo.nombresMats = System.Array.ConvertAll(mf.GetComponent<MeshRenderer>().sharedMaterials, m => m != null ? m.name : "");
            nodo.limites = new Bounds(nodo.v.Length > 0 ? nodo.v[0] : Vector3.zero, Vector3.zero);
            foreach (var p in nodo.v) nodo.limites.Encapsulate(p);
            // Nodo repetido: misma geometría en el mismo sitio (el Ares 1 A trae cinco conos dos veces).
            bool repetido = lista.Any(o => o.v.Length == nodo.v.Length &&
                (o.limites.center - nodo.limites.center).sqrMagnitude < 1e-8f && (o.limites.size - nodo.limites.size).sqrMagnitude < 1e-8f &&
                (o.v[0] - nodo.v[0]).sqrMagnitude < 1e-8f && (o.v[o.v.Length - 1] - nodo.v[nodo.v.Length - 1]).sqrMagnitude < 1e-8f);
            if (repetido) continue;
            lista.Add(nodo);
        }
        return lista;
    }

    /// <summary>
    /// Si la malla trae el sentido de los triángulos mezclado (aristas que dos triángulos recorren en el mismo
    /// sentido), quita los repetidos, orienta cada pieza hacia fuera y recalcula las normales. Devuelve si la tocó.
    /// Las mallas sanas no se modifican (la regla «hacia fuera» no vale para el interior de una tobera).
    /// </summary>
    static bool RepararOrientacion(NodoCohete nodo)
    {
        float paso = Mathf.Max(nodo.limites.size.x, nodo.limites.size.y, nodo.limites.size.z) * 1e-4f;
        if (paso <= 0f) return false;
        var ids = new Dictionary<Vector3Int, int>();
        var w = new int[nodo.v.Length];
        for (int i = 0; i < w.Length; i++)
        {
            var q = Vector3Int.RoundToInt(nodo.v[i] / paso);
            if (!ids.TryGetValue(q, out var id)) ids[q] = id = ids.Count;
            w[i] = id;
        }
        var aristas = new Dictionary<(int, int), int>();   // bit 0: vista a→b; bit 1: vista b→a; bit 2: repetida en un sentido
        int mal = 0, total = 0;
        foreach (var t in nodo.tri)
            for (int k = 0; k < t.Length; k += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = w[t[k + e]], b = w[t[k + (e + 1) % 3]];
                    if (a == b) continue;
                    var clave = a < b ? (a, b) : (b, a);
                    int bit = a < b ? 1 : 2;
                    aristas.TryGetValue(clave, out var visto);
                    if ((visto & bit) != 0 && (visto & 4) == 0) { mal++; visto |= 4; }
                    aristas[clave] = visto | bit;
                    total++;
                }
        if (mal < Mathf.Max(4, aristas.Count / 50)) return false;

        // Piezas conexas (por vértices soldados), cada una con su «esqueleto»: el tramo del eje vertical de su caja.
        var padre = Enumerable.Range(0, ids.Count).ToArray();
        int Raiz(int x) { while (padre[x] != x) x = padre[x] = padre[padre[x]]; return x; }
        foreach (var t in nodo.tri)
            for (int k = 0; k < t.Length; k += 3) { padre[Raiz(w[t[k]])] = Raiz(w[t[k + 1]]); padre[Raiz(w[t[k]])] = Raiz(w[t[k + 2]]); }
        var cajas = new Dictionary<int, Bounds>();
        for (int i = 0; i < w.Length; i++)
        {
            int r = Raiz(w[i]);
            if (cajas.TryGetValue(r, out var c)) { c.Encapsulate(nodo.v[i]); cajas[r] = c; }
            else cajas[r] = new Bounds(nodo.v[i], Vector3.zero);
        }
        var vistos = new HashSet<(int, int, int)>();
        for (int s = 0; s < nodo.tri.Count; s++)
        {
            var t = nodo.tri[s];
            var nuevo = new List<int>(t.Length);
            for (int k = 0; k < t.Length; k += 3)
            {
                int a = t[k], b = t[k + 1], c = t[k + 2];
                var orden = new[] { w[a], w[b], w[c] };
                System.Array.Sort(orden);
                if (orden[0] == orden[1] || orden[1] == orden[2]) continue;      // degenerado
                if (!vistos.Add((orden[0], orden[1], orden[2]))) continue;        // repetido (a veces, la misma cara al revés)
                var caja = cajas[Raiz(w[a])];
                var centro = (nodo.v[a] + nodo.v[b] + nodo.v[c]) / 3f;
                float radio = 0.5f * Mathf.Max(caja.size.x, caja.size.z);
                float y0 = caja.min.y + radio, y1 = caja.max.y - radio;
                float y = y0 <= y1 ? Mathf.Clamp(centro.y, y0, y1) : caja.center.y;
                var fuera = centro - new Vector3(caja.center.x, y, caja.center.z);
                var cara = Vector3.Cross(nodo.v[b] - nodo.v[a], nodo.v[c] - nodo.v[a]);
                if (Vector3.Dot(cara, fuera) < 0f) { nuevo.Add(a); nuevo.Add(c); nuevo.Add(b); }
                else { nuevo.Add(a); nuevo.Add(b); nuevo.Add(c); }
            }
            nodo.tri[s] = nuevo.ToArray();
        }
        // Normales nuevas: la media de las caras que comparten el vértice (las del modelo iban con el sentido viejo).
        var suma = new Vector3[nodo.v.Length];
        foreach (var t in nodo.tri)
            for (int k = 0; k < t.Length; k += 3)
            {
                var cara = Vector3.Cross(nodo.v[t[k + 1]] - nodo.v[t[k]], nodo.v[t[k + 2]] - nodo.v[t[k]]);
                suma[t[k]] += cara; suma[t[k + 1]] += cara; suma[t[k + 2]] += cara;
            }
        for (int i = 0; i < suma.Length; i++) nodo.n[i] = suma[i].sqrMagnitude > 1e-12f ? suma[i].normalized : Vector3.up;
        return true;
    }

    /// <summary>
    /// Tapa las bocas abiertas de una parte en el plano y = «altura»: cada lazo de aristas sueltas se cierra en
    /// abanico desde su centro, mirando hacia «arriba» o hacia abajo. Devuelve el radio de la boca mayor.
    /// </summary>
    static float Tapar(ParteMalla parte, float altura, bool arriba, Material interior, float eps = 2e-3f)
    {
        var ids = new Dictionary<Vector3Int, int>();
        var w = new int[parte.v.Count];
        for (int i = 0; i < w.Length; i++)
        {
            var q = Vector3Int.RoundToInt(parte.v[i] * 5000f);
            if (!ids.TryGetValue(q, out var id)) ids[q] = id = ids.Count;
            w[i] = id;
        }
        var cuenta = new Dictionary<(int, int), (int veces, int a, int b)>();
        foreach (var lista in parte.sub)
            for (int k = 0; k < lista.Count; k += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = lista[k + e], b = lista[k + (e + 1) % 3];
                    if (w[a] == w[b]) continue;
                    var clave = w[a] < w[b] ? (w[a], w[b]) : (w[b], w[a]);
                    cuenta.TryGetValue(clave, out var c);
                    cuenta[clave] = (c.veces + 1, a, b);
                }
        var sueltas = cuenta.Values.Where(c => c.veces == 1 && Mathf.Abs(parte.v[c.a].y - altura) < eps && Mathf.Abs(parte.v[c.b].y - altura) < eps).ToList();
        if (sueltas.Count < 3) return 0f;
        var padre = new Dictionary<int, int>();
        int Raiz(int x) { if (!padre.ContainsKey(x)) padre[x] = x; while (padre[x] != x) x = padre[x] = padre[padre[x]]; return x; }
        foreach (var s in sueltas) padre[Raiz(w[s.a])] = Raiz(w[s.b]);
        var tapa = parte.Sub(interior);
        var normal = arriba ? Vector3.up : Vector3.down;
        float radioMayor = 0f;
        foreach (var lazo in sueltas.GroupBy(s => Raiz(w[s.a])))
        {
            var aristas = lazo.ToList();
            if (aristas.Count < 3) continue;
            var centro = Vector3.zero;
            foreach (var s in aristas) centro += parte.v[s.a] + parte.v[s.b];
            centro /= aristas.Count * 2f;
            centro.y = altura;
            float radio = aristas.Max(s => Vector3.Distance(parte.v[s.a], centro));
            radioMayor = Mathf.Max(radioMayor, new Vector2(centro.x, centro.z).magnitude + radio);
            int ic = parte.v.Count;
            parte.v.Add(centro); parte.n.Add(normal); parte.uv.Add(Vector2.one * 0.5f);
            foreach (var s in aristas)
            {
                int ia = parte.v.Count;
                parte.v.Add(parte.v[s.a]); parte.n.Add(normal); parte.uv.Add(Vector2.zero);
                parte.v.Add(parte.v[s.b]); parte.n.Add(normal); parte.uv.Add(Vector2.zero);
                var cara = Vector3.Cross(parte.v[ia] - centro, parte.v[ia + 1] - centro);
                if (Vector3.Dot(cara, normal) >= 0f) { tapa.Add(ic); tapa.Add(ia); tapa.Add(ia + 1); }
                else { tapa.Add(ic); tapa.Add(ia + 1); tapa.Add(ia); }
            }
        }
        return radioMayor;
    }

    /// <summary>
    /// Tapa la boca de una malla ya hecha (el SLS viene en dos archivos, con las dos bocas abiertas): un objeto
    /// «Tapa» hijo de «padre», en el plano y = «altura» del espacio de «cohete».
    /// </summary>
    static void TaparObjeto(Transform cohete, Transform padre, IEnumerable<MeshFilter> mallas, float altura, bool arriba, Material interior, string nombre)
    {
        var parte = new ParteMalla();
        var piel = parte.Sub(null);
        foreach (var mf in mallas)
        {
            if (mf == null || mf.sharedMesh == null) continue;
            var a = cohete.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            int inicio = parte.v.Count;
            foreach (var vert in mf.sharedMesh.vertices) { parte.v.Add(a.MultiplyPoint3x4(vert)); parte.n.Add(Vector3.up); parte.uv.Add(Vector2.zero); }
            foreach (var t in mf.sharedMesh.triangles) piel.Add(inicio + t);
        }
        if (Tapar(parte, altura, arriba, interior, 0.03f) <= 0f) { Debug.LogWarning("[Cohetes] sin boca que tapar: " + nombre); return; }
        // Solo la tapa: sus vértices son los últimos (Tapar los añade aparte).
        var tapa = parte.Sub(interior);
        var mapa = new Dictionary<int, int>();
        var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
        foreach (var i in tapa)
        {
            if (!mapa.TryGetValue(i, out var k)) { mapa[i] = k = v.Count; v.Add(parte.v[i]); n.Add(parte.n[i]); }
            tri.Add(k);
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, v.Select(_ => Vector2.zero).ToList()); m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        var go = new GameObject(nombre, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(cohete, false);
        go.GetComponent<MeshFilter>().sharedMesh = GuardarMalla(m, nombre);
        go.GetComponent<MeshRenderer>().sharedMaterial = interior;
        go.transform.SetParent(padre, true);
    }

    /// <summary>
    /// Instancia un modelo de una pieza, lo escala a «alto», lo apoya en el origen de «cohete» y lo parte por altura:
    /// primera etapa (en el cohete), etapa superior y la nariz en dos mitades con bisagra. Devuelve la etapa superior.
    /// </summary>
    static Transform ConstruirPorEtapas(Transform cohete, string ruta, string prefijo, float alto, float fraccionEtapa, float fraccionNariz,
        Color? tinte = null, System.Func<string, Color?> color = null, System.Func<string, Vector3, string> repintar = null)
    {
        var superior = new GameObject("EtapaSuperior").transform;
        superior.SetParent(cohete, false);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + ruta);
        if (asset == null) { Debug.LogError("Falta el modelo " + ruta); return superior; }
        var modelo = Object.Instantiate(asset, cohete, false);
        var b = Limites(modelo);
        modelo.transform.localScale *= alto / b.size.y;
        b = Limites(modelo);
        modelo.transform.position += new Vector3(cohete.position.x - b.center.x, cohete.position.y - b.min.y, cohete.position.z - b.center.z);

        Material Material(Material original)
        {
            var m = Tenir(MaterialNasa("Cohete_" + prefijo, original), tinte);
            var propio = color?.Invoke(original.name);
            if (m != null && propio.HasValue) { m.SetColor("_BaseColor", propio.Value); EditorUtility.SetDirty(m); }
            return m;
        }
        var nodos = NodosDe(modelo, cohete, Material);
        Object.DestroyImmediate(modelo);
        toberasPorCohete[cohete] = nodos.Where(n => n.limites.min.y < alto * 0.015f && n.limites.size.y < alto * 0.2f)
            .Select(n => (new Vector3(n.limites.center.x, n.limites.min.y, n.limites.center.z), 0.5f * Mathf.Min(n.limites.size.x, n.limites.size.z), n.v.Length)).ToList();
        int reparados = nodos.Count(RepararOrientacion);
        if (reparados > 0) Debug.Log("[Cohetes] " + prefijo + ": " + reparados + " mallas con triángulos al revés, corregidas");

        float corteEtapa = fraccionEtapa * alto, corteNariz = fraccionNariz * alto;
        var partes = new[] { new ParteMalla(), new ParteMalla(), new ParteMalla(), new ParteMalla() };
        clavesCorte.Clear();
        var poli = new List<VerticeCorte>(); var abajo = new List<VerticeCorte>(); var arriba = new List<VerticeCorte>();
        var medio = new List<VerticeCorte>(); var punta = new List<VerticeCorte>(); var izq = new List<VerticeCorte>(); var der = new List<VerticeCorte>();
        for (int i = 0; i < nodos.Count; i++)
        {
            var nodo = nodos[i];
            for (int s = 0; s < nodo.tri.Count; s++)
            {
                var t = nodo.tri[s];
                var mat = nodo.mats[Mathf.Min(s, nodo.mats.Length - 1)];
                var doble = DobleCara(mat, mat.name + "_DobleCara");   // las mitades de la nariz se ven por dentro al abrirse
                var matBase = mat; var dobleBase = doble;
                string nombreMat = nodo.nombresMats[Mathf.Min(s, nodo.nombresMats.Length - 1)];
                for (int k = 0; k < t.Length; k += 3)
                {
                    poli.Clear();
                    for (int e = 0; e < 3; e++)
                        poli.Add(new VerticeCorte { p = nodo.v[t[k + e]], n = nodo.n[t[k + e]], uv = nodo.uv[t[k + e]], clave = ((long)(i + 1) << 32) | (uint)t[k + e] });
                    // Pintura por zonas (franjas del Jupiter-C): el triángulo puede pasar a otro material del mismo nodo.
                    mat = matBase; doble = dobleBase;
                    if (repintar != null)
                    {
                        var otro = repintar(nombreMat, (poli[0].p + poli[1].p + poli[2].p) / 3f);
                        int indice = otro != null ? System.Array.IndexOf(nodo.nombresMats, otro) : -1;
                        if (indice >= 0) { mat = nodo.mats[indice]; doble = DobleCara(mat, mat.name + "_DobleCara"); }
                    }
                    Partir(poli, p => p.y - corteEtapa, 0, abajo, arriba);
                    partes[0].Poligono(abajo, mat);
                    if (arriba.Count == 0) continue;
                    Partir(arriba, p => p.y - corteNariz, 1, medio, punta);
                    partes[1].Poligono(medio, mat);
                    if (punta.Count == 0) continue;
                    Partir(punta, p => p.x, 2, izq, der);
                    partes[2].Poligono(izq, doble);
                    partes[3].Poligono(der, doble);
                }
            }
        }

        // Tapas: la boca de la primera etapa, y la base y la cubierta de la etapa superior (donde va la carga).
        var interior = CrearMaterial("NASA_Cohete_" + prefijo + "_Interior", Hex("#23272E"), 0.35f, 0.3f);
        float radioCorte = Tapar(partes[0], corteEtapa, true, interior);
        radioCorte = Mathf.Max(radioCorte, Tapar(partes[1], corteEtapa, false, interior));
        Tapar(partes[1], corteNariz, true, interior);

        var mitades = new Dictionary<int, Transform>();
        foreach (var (lado, nombre) in new[] { (-1, "CofiaFrente"), (1, "CofiaDetras") })
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(superior, false);
            mitades[lado] = t;
        }
        var nombres = new[] { "Etapa1", "Etapa2", "NarizFrente", "NarizDetras" };
        var destinos = new[] { cohete, superior, mitades[-1], mitades[1] };
        for (int k = 0; k < 4; k++)
        {
            if (partes[k].Vacia) continue;
            var malla = GuardarMalla(partes[k].Malla(), prefijo + "_" + nombres[k]);
            var go = new GameObject(malla.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(destinos[k], false);
            go.GetComponent<MeshFilter>().sharedMesh = malla;
            go.GetComponent<MeshRenderer>().sharedMaterials = partes[k].mats.ToArray();
        }

        // Tobera de la etapa superior: va dentro del interestadio y se ve al soltar la primera etapa.
        if (radioCorte > 0.01f)
        {
            float r = radioCorte * 0.5f, largo = radioCorte * 1.1f;
            var perfil = new[] { P(r * 0.28f, 0f), P(r * 0.34f, -largo * 0.2f), P(r * 0.62f, -largo * 0.55f), P(r, -largo), P(r * 0.9f, -largo), P(r * 0.5f, -largo * 0.5f), P(r * 0.2f, -largo * 0.15f) };
            System.Array.Reverse(perfil);
            var motor = Malla(GuardarMalla(Torno(perfil, 24), prefijo + "_MotorSuperior"), "MotorSuperior", superior,
                new Vector3(0f, corteEtapa, 0f), Vector3.zero, Vector3.one,
                DobleCara(CrearMaterial("Cohete_Motor", Hex("#3A3F47"), 0.7f, 0.5f), "Cohete_Motor_DobleCara"));
            motor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

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
        MedirCohete(cohete, alto, new[] { mitades[-1], mitades[1] }, radioCorte);
        return superior;
    }

    /// <summary>Candidatas a tobera de cada cohete: nodos bajos y cortos del modelo (posición de la boca, radio, vértices).</summary>
    static readonly Dictionary<Transform, List<(Vector3 pos, float radio, int vertices)>> toberasPorCohete =
        new Dictionary<Transform, List<(Vector3, float, int)>>();

    /// <summary>
    /// Las «motores» toberas del pie, medidas en el modelo: los nodos bajos con más detalle (las campanas). Con un
    /// solo motor tiene que estar en el eje. Lista vacía si el modelo no las trae como piezas aparte.
    /// </summary>
    static List<(Vector3 pos, float radio)> ToberasMedidas(Transform cohete, float alto, int motores)
    {
        var lista = new List<(Vector3, float)>();
        if (!toberasPorCohete.TryGetValue(cohete, out var candidatas)) return lista;
        var elegidas = candidatas.Where(c => motores > 1 || new Vector2(c.pos.x, c.pos.z).magnitude < alto * 0.01f)
            .OrderByDescending(c => c.vertices).Take(motores).ToList();
        if (elegidas.Count != motores) return lista;
        foreach (var c in elegidas) lista.Add((c.pos + Vector3.up * 0.02f, c.radio * 0.62f));
        Debug.Log("[Cohetes] " + cohete.name + ": toberas " + string.Join(" ", lista.Select(t => t.Item1.ToString("0.00") + " r" + t.Item2.ToString("0.000"))));
        return lista;
    }

    /// <summary>¿Este renderer es piel del cohete? (No lo son las llamas, los brillos, los logos ni los resaltes.)</summary>
    static bool EsPielDeCohete(Renderer r)
    {
        if (!(r is MeshRenderer) || r.sharedMaterial == null) return false;
        var n = r.sharedMaterial.name;
        return n.StartsWith("NASA_Cohete_") || n.StartsWith("Cohete_");
    }

    /// <summary>
    /// Mide el cohete y lo guarda en su PerfilCohete: hasta dónde llega hacia la torre en cada banda de altura (los
    /// brazos se apoyan ahí) y el radio libre dentro de la cofia (la carga se escala a eso).
    /// </summary>
    static PerfilCohete MedirCohete(Transform cohete, float alto, Transform[] cofia, float radioCorte)
    {
        const int Bandas = 48;
        var haciaTorre = new float[Bandas];
        var hayDato = new bool[Bandas];
        var direccion = DireccionBrazo;
        var lateral = new Vector3(direccion.z, 0f, -direccion.x);
        var puntosCofia = new List<Vector3>();
        float radioBase = 0f;
        foreach (var mf in cohete.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null || !EsPielDeCohete(mf.GetComponent<Renderer>())) continue;
            bool esCofia = cofia.Any(c => c != null && mf.transform.IsChildOf(c));
            foreach (var vert in mf.sharedMesh.vertices)
            {
                var p = cohete.InverseTransformPoint(mf.transform.TransformPoint(vert));
                if (esCofia) puntosCofia.Add(p);
                if (p.y < alto * 0.05f) radioBase = Mathf.Max(radioBase, new Vector2(p.x, p.z).magnitude);
                if (Mathf.Abs(Vector3.Dot(p, lateral)) > AnchoBrazo * 0.5f + 0.06f) continue;
                int k = Mathf.Clamp(Mathf.FloorToInt(p.y / alto * Bandas), 0, Bandas - 1);
                haciaTorre[k] = Mathf.Max(haciaTorre[k], Vector3.Dot(p, direccion));
                hayDato[k] = true;
            }
        }
        // Un tramo recto no tiene vértices en medio: sus bandas toman la mayor de las vecinas con dato.
        for (int k = 0; k < Bandas; k++)
        {
            if (hayDato[k]) continue;
            int a = k, c = k;
            while (a > 0 && !hayDato[a]) a--;
            while (c < Bandas - 1 && !hayDato[c]) c++;
            haciaTorre[k] = Mathf.Max(hayDato[a] ? haciaTorre[a] : 0f, hayDato[c] ? haciaTorre[c] : 0f);
        }
        // Carga: el radio de la cofia a un tercio de su altura, menos la pared.
        float radioCarga = alto * 0.02f, altoCofia = alto * 0.1f;
        if (puntosCofia.Count > 0)
        {
            altoCofia = puntosCofia.Max(p => p.y) - puntosCofia.Min(p => p.y);
            float y0 = puntosCofia.Min(p => p.y), y1 = puntosCofia.Max(p => p.y);
            float eje = puntosCofia.Average(p => p.x), ejeZ = puntosCofia.Average(p => p.z);
            float corte = Mathf.Lerp(y0, y1, 0.3f);
            var abajo = puntosCofia.Where(p => p.y <= corte).ToList();
            var arriba = puntosCofia.Where(p => p.y >= corte).ToList();
            float R(List<Vector3> l) => l.Count == 0 ? 0f : l.Max(p => new Vector2(p.x - eje, p.z - ejeZ).magnitude);
            // Lo que cabe a esa altura: no más ancho que la cofia por encima ni por debajo del corte.
            radioCarga = Mathf.Max(alto * 0.006f, Mathf.Min(R(abajo), R(arriba)) * 0.78f);
        }
        var perfil = cohete.GetComponent<PerfilCohete>() ?? cohete.gameObject.AddComponent<PerfilCohete>();
        perfil.Fijar(alto, haciaTorre, radioCarga, radioCorte, altoCofia, radioBase);
        Debug.Log("[Cohetes] " + cohete.name + ": radio de carga " + radioCarga.ToString("0.000") + ", corte " + radioCorte.ToString("0.000") + ", cofia " + altoCofia.ToString("0.00"));
        return perfil;
    }

    // ---------- Brazos de la torre ----------

    /// <summary>Pivote de los brazos (esquina de la torre) respecto al eje del cohete, en el suelo.</summary>
    static readonly Vector3 PivoteBrazo = new Vector3(-0.9f, 0f, 2.5f);
    static Vector3 DireccionBrazo => PivoteBrazo.normalized;
    const float AnchoBrazo = 0.34f;

    /// <summary>
    /// Brazo telescópico de la torre: celosía fija que apunta al eje del cohete, pasarela que sale hasta su costado
    /// y cabezal con dos umbilicales. Local −Z va del pivote al cohete.
    /// </summary>
    static BrazoTorre ConstruirBrazo(Transform torre, string nombre, Vector3 pivoteLocal, Material estructura, Material acento, Material cable)
    {
        var brazo = new GameObject(nombre).transform;
        brazo.SetParent(torre, false);
        brazo.localPosition = pivoteLocal;
        var tramo = new GameObject("Tramo").transform;
        tramo.SetParent(brazo, false);
        // −Z local hacia el eje: del pivote al origen.
        tramo.localRotation = Quaternion.LookRotation(PivoteBrazo, Vector3.up);
        float distancia = PivoteBrazo.magnitude;
        const float fijo = 1.55f, largoPasarela = 1.25f, alto = 0.3f;
        // Celosía fija: dos largueros abajo, dos arriba y diagonales.
        foreach (int lado in new[] { -1, 1 })
        {
            Pieza(PrimitiveType.Cube, "Larguero", tramo, new Vector3(lado * AnchoBrazo * 0.5f, -alto * 0.5f, -fijo * 0.5f), Vector3.zero, new Vector3(0.06f, 0.06f, fijo), estructura);
            Pieza(PrimitiveType.Cube, "Larguero", tramo, new Vector3(lado * AnchoBrazo * 0.5f, alto * 0.5f, -fijo * 0.5f), Vector3.zero, new Vector3(0.05f, 0.05f, fijo), estructura);
            const int vanos = 4;
            float vano = fijo / vanos, ang = Mathf.Atan2(alto, vano) * Mathf.Rad2Deg, diag = Mathf.Sqrt(alto * alto + vano * vano);
            for (int i = 0; i < vanos; i++)
            {
                Pieza(PrimitiveType.Cube, "Diagonal", tramo, new Vector3(lado * AnchoBrazo * 0.5f, 0f, -(i + 0.5f) * vano), new Vector3(i % 2 == 0 ? ang : -ang, 0f, 0f), new Vector3(0.035f, 0.035f, diag), estructura);
                Pieza(PrimitiveType.Cube, "Montante", tramo, new Vector3(lado * AnchoBrazo * 0.5f, 0f, -(i + 1) * vano), Vector3.zero, new Vector3(0.04f, alto, 0.04f), estructura);
            }
        }
        Pieza(PrimitiveType.Cube, "Bisagra", tramo, new Vector3(0f, 0f, -0.04f), Vector3.zero, new Vector3(AnchoBrazo + 0.12f, alto + 0.14f, 0.16f), acento);
        // Pasarela: sale de la celosía hasta el cohete (BrazoTorre.Ajustar la coloca).
        var pasarela = new GameObject("Pasarela").transform;
        pasarela.SetParent(tramo, false);
        pasarela.localPosition = new Vector3(0f, -alto * 0.5f + 0.04f, -(fijo - largoPasarela * 0.5f));
        Pieza(PrimitiveType.Cube, "Piso", pasarela, Vector3.zero, Vector3.zero, new Vector3(AnchoBrazo - 0.1f, 0.05f, largoPasarela), acento);
        foreach (int lado in new[] { -1, 1 })
            Pieza(PrimitiveType.Cube, "Baranda", pasarela, new Vector3(lado * (AnchoBrazo * 0.5f - 0.06f), 0.16f, 0f), Vector3.zero, new Vector3(0.025f, 0.025f, largoPasarela), estructura);
        // Cabezal: la placa que se apoya en el cohete y los dos umbilicales que cuelgan hacia él.
        var cabezal = new GameObject("Cabezal").transform;
        cabezal.SetParent(tramo, false);
        cabezal.localPosition = new Vector3(0f, 0f, -fijo);
        Pieza(PrimitiveType.Cube, "Placa", cabezal, new Vector3(0f, -0.02f, 0.035f), Vector3.zero, new Vector3(AnchoBrazo, alto + 0.1f, 0.07f), acento);
        var cables = new List<Transform>();
        foreach (int lado in new[] { -1, 1 })
        {
            var u = new GameObject("Umbilical").transform;
            u.SetParent(cabezal, false);
            u.localPosition = new Vector3(lado * 0.09f, 0.06f, 0.4f);
            Pieza(PrimitiveType.Cylinder, "Manguera", u, new Vector3(0f, 0f, -0.2f), new Vector3(90f, 0f, 0f), new Vector3(0.045f, 0.2f, 0.045f), cable);
            cables.Add(u);
        }
        var c = brazo.gameObject.AddComponent<BrazoTorre>();
        c.Fijar(pasarela, cabezal, cables.ToArray(), distancia, largoPasarela, fijo - 0.25f);
        c.Ajustar(0.3f);
        return c;
    }
}
