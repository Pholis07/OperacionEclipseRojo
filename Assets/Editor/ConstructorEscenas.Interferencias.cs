using SpaceMission;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Renovación visual: que los mecanismos de los instrumentos no atraviesen la nave. Para cada satélite y cada
/// instrumento se prueban ocho giros del módulo sobre su soporte (y, si hace falta, un soporte más largo) y se
/// elige el que menos choca, con el mecanismo
/// desplegado (órbita) y plegado (hangar), contra el cuerpo del satélite en la misma pose —con sus paneles— y
/// contra las piezas de los instrumentos ya colocados. Se mide con puntos a lo largo de cada pieza móvil.
/// Lo que no se pueda evitar se guarda en NaveSatelite (interferencias) y sale en el registro: no se esconde.
/// </summary>
public static partial class ConstructorEscenas
{
    static float DistanciaAlTriangulo2(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // Punto más cercano de un triángulo (Ericson, Real-Time Collision Detection).
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return ap.sqrMagnitude;
        var bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return bp.sqrMagnitude;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return (p - (a + ab * (d1 / (d1 - d3)))).sqrMagnitude;
        var cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return cp.sqrMagnitude;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return (p - (a + ac * (d2 / (d2 - d6)))).sqrMagnitude;
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return (p - (b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6))))).sqrMagnitude;
        float den = 1f / (va + vb + vc);
        return (p - (a + ab * (vb * den) + ac * (vc * den))).sqrMagnitude;
    }

    static List<(Vector3 a, Vector3 b, Vector3 c)> TriangulosMundo(GameObject cuerpo)
    {
        var lista = new List<(Vector3, Vector3, Vector3)>();
        foreach (var mf in cuerpo.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var v = mf.sharedMesh.vertices; var t = mf.sharedMesh.triangles;
            var m = mf.transform.localToWorldMatrix;
            for (int k = 0; k < t.Length; k += 3)
                lista.Add((m.MultiplyPoint3x4(v[t[k]]), m.MultiplyPoint3x4(v[t[k + 1]]), m.MultiplyPoint3x4(v[t[k + 2]])));
        }
        return lista;
    }

    /// <summary>Puntos a lo largo de cada pieza móvil del mecanismo, en el mundo.</summary>
    static List<Vector3> PuntosDelMecanismo(MecanismoInstrumento mecanismo)
    {
        var puntos = new List<Vector3>();
        var vistos = new HashSet<MeshFilter>();
        foreach (var pivote in mecanismo.Pivotes)
        {
            if (pivote == null) continue;
            foreach (var mf in pivote.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || !vistos.Add(mf)) continue;
                var b = mf.sharedMesh.bounds;
                var tamano = Vector3.Scale(b.size, mf.transform.lossyScale);
                int eje = tamano.x >= tamano.y && tamano.x >= tamano.z ? 0 : tamano.y >= tamano.z ? 1 : 2;
                foreach (float t in new[] { -0.9f, -0.45f, 0f, 0.45f, 0.9f })
                {
                    var local = b.center; local[eje] += b.extents[eje] * t;
                    puntos.Add(mf.transform.TransformPoint(local));
                }
            }
        }
        return puntos;
    }

    /// <summary>
    /// Elige el giro de cada módulo sobre su soporte en cada satélite y lo deja en «rotaciones». Devuelve, por
    /// satélite y módulo, cuántos puntos del mecanismo siguen tocando algo con el mejor giro (0 = libre).
    /// </summary>
    static int[] OrientarMecanismos(GameObject[] cuerpos, Transform[] modulos, List<Vector3> posiciones, List<Quaternion> rotaciones, List<float> largos, List<float> escalas)
    {
        const float holgura = 0.03f;
        var restantes = new int[cuerpos.Length * modulos.Length];
        int radar = System.Array.IndexOf(IdsInstrumento, "radar");
        var orden = Enumerable.Range(0, modulos.Length).OrderBy(m => m == radar ? 0 : 1).ToArray();   // el más largo, primero
        for (int s = 0; s < cuerpos.Length; s++)
        {
            var alas = cuerpos[s].GetComponent<MecanismoInstrumento>();
            var cuerpo = new List<(Vector3 a, Vector3 b, Vector3 c)>[2];
            for (int pose = 0; pose < 2; pose++)
            {
                if (alas != null) alas.Desplegar(pose);
                cuerpo[pose] = TriangulosMundo(cuerpos[s]);
            }
            if (alas != null) alas.Desplegar(1f);
            var giros = new List<string>();
            foreach (int m in orden)
            {
                int k = s * modulos.Length + m;
                var modulo = modulos[m];
                var mecanismo = modulo.GetComponent<MecanismoInstrumento>();
                modulo.localPosition = posiciones[k];
                modulo.localScale = Vector3.one * escalas[s];
                var baseGiro = rotaciones[k];
                // Solo los triángulos al alcance del módulo.
                float alcance = 2.4f * escalas[s];
                var zona = new Bounds(modulo.position, Vector3.one * alcance * 2f);
                var cerca = new List<(Vector3 a, Vector3 b, Vector3 c)>[2];
                for (int pose = 0; pose < 2; pose++)
                    cerca[pose] = cuerpo[pose].Where(t => zona.Contains(t.a) || zona.Contains(t.b) || zona.Contains(t.c)).ToList();
                // Primero se prueba a girar el módulo; si con ningún giro queda libre, se alarga su soporte (el
                // módulo se separa del casco) y se vuelve a probar.
                int mejor = int.MaxValue; float mejorGiro = 0f, mejorAlza = 0f;
                var arriba = baseGiro * Vector3.up;
                var basePosicion = posiciones[k];
                foreach (float alza in new[] { 0f, 0.14f, 0.28f, 0.45f })
                {
                    modulo.localPosition = basePosicion + arriba * alza * escalas[s];
                    for (int g = 0; g < 8 && mejor > 0; g++)
                    {
                        float giro = g * 45f;
                        modulo.localRotation = baseGiro * Quaternion.Euler(0f, giro, 0f);
                        int choques = 0;
                        for (int pose = 0; pose < 2; pose++)
                        {
                            mecanismo.Desplegar(pose);
                            foreach (var p in PuntosDelMecanismo(mecanismo))
                                foreach (var t in cerca[pose])
                                    if (DistanciaAlTriangulo2(p, t.a, t.b, t.c) < holgura * holgura) { choques++; break; }
                        }
                        if (choques < mejor) { mejor = choques; mejorGiro = giro; mejorAlza = alza; }
                    }
                    if (mejor == 0) break;
                }
                rotaciones[k] = baseGiro * Quaternion.Euler(0f, mejorGiro, 0f);
                posiciones[k] = basePosicion + arriba * mejorAlza * escalas[s];
                largos[k] += mejorAlza * escalas[s] * 0.5f;   // el soporte (un cilindro: su alto es el doble de la escala) llega hasta el casco
                modulo.localRotation = rotaciones[k];
                modulo.localPosition = posiciones[k];
                // Ya colocado, este instrumento es un obstáculo para los siguientes (sus piezas reales, en cada pose).
                for (int pose = 0; pose < 2; pose++)
                {
                    mecanismo.Desplegar(pose);
                    cuerpo[pose].AddRange(TriangulosMundo(modulo.gameObject));
                }
                restantes[k] = mejor;
                giros.Add(IdsInstrumento[m] + " " + mejorGiro.ToString("0") + "°" + (mejorAlza > 0f ? " +" + mejorAlza.ToString("0.00") : "") + (mejor > 0 ? " (quedan " + mejor + ")" : ""));
            }
            Debug.Log("[Interferencias] " + Satelites[s].id + ": " + string.Join(", ", giros));
        }
        foreach (var modulo in modulos) modulo.GetComponent<MecanismoInstrumento>().Desplegar(0f);
        return restantes;
    }
}
