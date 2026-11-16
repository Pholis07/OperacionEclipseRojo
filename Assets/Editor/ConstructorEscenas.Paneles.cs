using SpaceMission;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renovación visual: paneles solares que se despliegan de verdad. Los satélites de NASA 3D Resources llegan como
/// mallas de una pieza; aquí se separan las «alas» de panel para darles bisagra:
/// - se buscan las piezas conexas de la malla que son sobre todo panel (por el material: shiny_panel, SolarPanel);
/// - se agrupan en dos alas, a un lado y otro del cuerpo, a lo largo del eje en el que más se extienden;
/// - cada ala pasa a un objeto propio con el pivote en su borde interior y dos poses (MecanismoInstrumento en el
///   cuerpo): plegada contra el cuerpo y desplegada (la del modelo).
/// Si un modelo no da dos alas claras, se deja rígido y se avisa en el registro: no se finge el despliegue.
/// </summary>
public static partial class ConstructorEscenas
{
    static bool EsMaterialDePanel(Material m)
    {
        if (m == null) return false;
        var n = m.name.ToLowerInvariant();
        return n.Contains("shiny_panel") || (n.Contains("solarpanel") && !n.Contains("bracket"));
    }

    /// <summary>Separa las alas de panel de «cuerpo» y les pone bisagra. Devuelve cuántas alas quedaron móviles.</summary>
    static int DividirPaneles(GameObject cuerpo, string id)
    {
        var raiz = cuerpo.transform;
        // Piezas conexas de cada malla, con su área total y de panel, en el espacio del cuerpo.
        var piezas = new List<(MeshFilter mf, int pieza, Bounds caja, float area, float areaPanel)>();
        var piezaDe = new Dictionary<MeshFilter, int[]>();   // pieza de cada triángulo, en orden de submallas
        foreach (var mf in cuerpo.GetComponentsInChildren<MeshFilter>(true))
        {
            var malla = mf.sharedMesh;
            if (malla == null) continue;
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            var vs = malla.vertices;
            var a = raiz.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var padre = new int[vs.Length];
            for (int i = 0; i < padre.Length; i++) padre[i] = i;
            int Raiz(int x) { while (padre[x] != x) x = padre[x] = padre[padre[x]]; return x; }
            float paso = Mathf.Max(malla.bounds.size.x, malla.bounds.size.y, malla.bounds.size.z) * 2e-4f;
            var porPosicion = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vs.Length; i++)
            {
                var q = Vector3Int.RoundToInt(vs[i] / paso);
                if (porPosicion.TryGetValue(q, out var otro)) padre[Raiz(i)] = Raiz(otro); else porPosicion[q] = i;
            }
            var todos = new List<(int[] t, int sub)>();
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var t = malla.GetTriangles(s);
                for (int k = 0; k < t.Length; k += 3) { padre[Raiz(t[k])] = Raiz(t[k + 1]); padre[Raiz(t[k])] = Raiz(t[k + 2]); }
                todos.Add((t, s));
            }
            var datos = new Dictionary<int, (Bounds caja, float area, float panel)>();
            var deTriangulo = new List<int>();
            foreach (var (t, s) in todos)
            {
                bool panel = EsMaterialDePanel(mats[Mathf.Min(s, mats.Length - 1)]);
                for (int k = 0; k < t.Length; k += 3)
                {
                    int r = Raiz(t[k]);
                    Vector3 p0 = a.MultiplyPoint3x4(vs[t[k]]), p1 = a.MultiplyPoint3x4(vs[t[k + 1]]), p2 = a.MultiplyPoint3x4(vs[t[k + 2]]);
                    float area = Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5f;
                    if (!datos.TryGetValue(r, out var d)) d = (new Bounds(p0, Vector3.zero), 0f, 0f);
                    d.caja.Encapsulate(p0); d.caja.Encapsulate(p1); d.caja.Encapsulate(p2);
                    d.area += area; if (panel) d.panel += area;
                    datos[r] = d;
                    deTriangulo.Add(r);
                }
            }
            piezaDe[mf] = deTriangulo.ToArray();
            foreach (var par in datos) piezas.Add((mf, par.Key, par.Value.caja, par.Value.area, par.Value.panel));
        }
        var paneles = piezas.Where(p => p.areaPanel > p.area * 0.3f && p.area > 1e-6f).ToList();
        if (paneles.Count < 2) { Debug.Log("[Paneles] " + id + ": sin piezas de panel separables; queda rígido"); return 0; }

        // Eje de las alas: el que mejor las reparte a un lado y otro del cuerpo (el lado con menos panel manda).
        // Así un satélite con los dos paneles abajo (Aquarius) no toma «abajo» como eje: ahí estarían del mismo lado.
        var centro = raiz.InverseTransformPoint(Limites(cuerpo).center);
        float total = paneles.Sum(p => p.areaPanel);
        int eje = 0; float mejor = -1f;
        for (int e = 0; e < 3; e++)
        {
            float menos = paneles.Sum(p => Mathf.Max(0f, centro[e] - p.caja.center[e]) * p.areaPanel);
            float mas = paneles.Sum(p => Mathf.Max(0f, p.caja.center[e] - centro[e]) * p.areaPanel);
            float reparto = Mathf.Min(menos, mas);
            if (reparto > mejor) { mejor = reparto; eje = e; }
        }
        float umbral = paneles.Max(p => Mathf.Abs(p.caja.center[eje] - centro[eje])) * 0.25f;
        var alas = new[] { -1, 1 }.Select(lado => paneles.Where(p => (p.caja.center[eje] - centro[eje]) * lado > umbral).ToList()).ToArray();
        if (alas.Any(a => a.Count == 0 || a.Sum(p => p.areaPanel) < total * 0.15f))
        {
            Debug.Log("[Paneles] " + id + ": no hay dos alas claras a los lados del cuerpo; queda rígido");
            return 0;
        }

        var mecanismo = cuerpo.GetComponent<MecanismoInstrumento>() ?? cuerpo.AddComponent<MecanismoInstrumento>();
        var moviles = new List<MecanismoInstrumento.Pieza>();
        var enAla = new Dictionary<(MeshFilter, int), int>();
        for (int k = 0; k < 2; k++) foreach (var p in alas[k]) enAla[(p.mf, p.pieza)] = k;
        // Con cada ala se va también lo que queda dentro de su caja: el dorso del panel, el marco y los herrajes
        // (en algunos modelos son otro material y otra pieza).
        int arrastradas = 0;
        for (int k = 0; k < 2; k++)
        {
            var cajaAla = alas[k][0].caja;
            foreach (var p in alas[k]) cajaAla.Encapsulate(p.caja);
            float margen = Mathf.Max(cajaAla.size.x, cajaAla.size.y, cajaAla.size.z) * 0.035f;
            cajaAla.Expand(margen * 2f);
            foreach (var p in piezas)
            {
                if (enAla.ContainsKey((p.mf, p.pieza))) continue;
                if (!cajaAla.Contains(p.caja.min) || !cajaAla.Contains(p.caja.max)) continue;
                enAla[(p.mf, p.pieza)] = k;
                arrastradas++;
            }
        }
        var pivotes = new Transform[2];
        for (int k = 0; k < 2; k++)
        {
            int lado = k == 0 ? -1 : 1;
            var caja = alas[k][0].caja;
            foreach (var p in alas[k]) caja.Encapsulate(p.caja);
            // Bisagra: el borde del ala más cercano al cuerpo.
            var bisagra = caja.center;
            bisagra[eje] = lado < 0 ? caja.max[eje] : caja.min[eje];
            // Normal del ala: el lado más fino de su caja (que no sea el eje).
            int normal = Enumerable.Range(0, 3).Where(e => e != eje).OrderBy(e => caja.size[e]).First();
            var d = Vector3.zero; d[eje] = lado;
            var n = Vector3.zero; n[normal] = 1f;
            var h = Vector3.Cross(d, n);                       // eje de giro, en el plano del ala
            // Se pliega hacia +normal: el ala queda de pie junto al cuerpo.
            float angulo = 84f * Mathf.Sign(Vector3.Dot(Vector3.Cross(h, d), n));
            var pivote = new GameObject("Ala_" + k).transform;
            pivote.SetParent(raiz, false);
            pivote.localPosition = bisagra;
            pivotes[k] = pivote;
            moviles.Add(PiezaMovil(pivote, bisagra, bisagra, (Quaternion.AngleAxis(angulo, h)).eulerAngles, Vector3.zero, k * 0.12f, 0.75f + k * 0.12f));
        }

        // Cada malla se reparte: lo de cada ala a su pivote y el resto se queda donde estaba.
        foreach (var par in piezaDe)
        {
            var mf = par.Key; var malla = mf.sharedMesh;
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            var listas = new[] { 0, 1, 2 }.Select(_ => Enumerable.Range(0, malla.subMeshCount).Select(__ => new List<int>()).ToArray()).ToArray();
            int indice = 0; bool hayAla = false;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var t = malla.GetTriangles(s);
                for (int k = 0; k < t.Length; k += 3)
                {
                    int destino = enAla.TryGetValue((mf, par.Value[indice++]), out var ala) ? ala : 2;
                    if (destino < 2) hayAla = true;
                    listas[destino][s].Add(t[k]); listas[destino][s].Add(t[k + 1]); listas[destino][s].Add(t[k + 2]);
                }
            }
            if (!hayAla) continue;
            for (int destino = 0; destino < 3; destino++)
            {
                if (listas[destino].All(l => l.Count == 0)) continue;
                var parte = Compactar(malla, listas[destino]);
                if (parte.uv.Length == parte.vertexCount) parte.RecalculateTangents();
                var guardada = GuardarMalla(parte, "Sat_" + id + "_" + mf.name.Replace(" ", "_") + (destino == 2 ? "_Cuerpo" : "_Ala" + destino));
                if (destino == 2) { mf.sharedMesh = guardada; continue; }
                var go = new GameObject(guardada.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(mf.transform, false);          // misma pose que la malla original…
                go.GetComponent<MeshFilter>().sharedMesh = guardada;
                go.GetComponent<MeshRenderer>().sharedMaterials = mats;
                go.transform.SetParent(pivotes[destino], true);       // …y cuelga de la bisagra de su ala
            }
        }
        var previas = new List<MecanismoInstrumento.Pieza>();
        mecanismo.Fijar(previas.Concat(moviles).ToArray());
        mecanismo.Desplegar(1f);   // los montajes de los instrumentos se calculan con la nave desplegada
        EditorUtility.SetDirty(mecanismo);
        Debug.Log("[Paneles] " + id + ": dos alas con bisagra (" + alas[0].Count + " y " + alas[1].Count + " piezas de panel, " + arrastradas + " de marco y dorso), eje " + "XYZ"[eje]);
        return 2;
    }

    /// <summary>Las cuatro antenas de cinta del CubeSat: plegadas contra la cara de atrás y desplegadas en cruz.</summary>
    static void PlegarAntenasCubeSat(GameObject cuerpo)
    {
        var moviles = new List<MecanismoInstrumento.Pieza>();
        int k = 0;
        foreach (var nombre in new[] { "Antena_D", "Antena_A", "Antena_I", "Antena_B" })
        {
            var antena = cuerpo.transform.Find(nombre);
            if (antena == null) continue;
            // Bisagra en el extremo de la antena más cercano al centro de la cara.
            var p = antena.localPosition;
            var hacia = new Vector3(p.x, p.y, 0f).normalized;
            var bisagra = new Vector3(hacia.x * 0.3f, hacia.y * 0.3f, p.z);
            var pivote = new GameObject("Bisagra_" + nombre).transform;
            pivote.SetParent(cuerpo.transform, false);
            pivote.localPosition = bisagra;
            antena.SetParent(pivote, true);
            moviles.Add(PiezaMovil(pivote, bisagra, bisagra, new Vector3(0f, 0f, 86f), Vector3.zero, k * 0.15f, 0.5f + k * 0.15f));
            k++;
        }
        var mecanismo = cuerpo.GetComponent<MecanismoInstrumento>() ?? cuerpo.AddComponent<MecanismoInstrumento>();
        mecanismo.Fijar(moviles.ToArray());
        mecanismo.Desplegar(1f);
        EditorUtility.SetDirty(mecanismo);
    }
}
