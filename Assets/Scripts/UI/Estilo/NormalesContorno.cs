using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Cálculo de la normal suavizada del contorno (ver ImportadorContornos).</summary>
    public static class NormalesContorno
    {
        /// <summary>Canal de la malla donde va la normal suavizada (UV 3 del Inspector, TEXCOORD2 en el shader).</summary>
        public const int Canal = 2;

        /// <summary>Calcula la normal suavizada de cada vértice y la guarda en el canal UV del contorno.</summary>
        public static void Preparar(Mesh malla)
        {
            var v = malla.vertices;
            if (v.Length == 0) return;
            var n = malla.normals;
            var t = malla.triangles;
            // Los vértices en el mismo punto (uno por cara) se agrupan por posición redondeada.
            float paso = Mathf.Max(malla.bounds.size.magnitude, 1e-6f) * 1e-5f;
            var grupos = new Dictionary<Vector3Int, Vector3>(v.Length);
            var claves = new Vector3Int[v.Length];
            for (int i = 0; i < v.Length; i++)
                claves[i] = new Vector3Int(Mathf.RoundToInt(v[i].x / paso), Mathf.RoundToInt(v[i].y / paso), Mathf.RoundToInt(v[i].z / paso));
            // Cada cara aporta su normal a sus tres esquinas, pesada por el ángulo: un cubo da la diagonal exacta.
            for (int k = 0; k + 2 < t.Length; k += 3)
            {
                var cara = Vector3.Cross(v[t[k + 1]] - v[t[k]], v[t[k + 2]] - v[t[k]]);
                if (cara.sqrMagnitude < 1e-20f) continue;
                cara.Normalize();
                for (int e = 0; e < 3; e++)
                {
                    int a = t[k + e], b = t[k + (e + 1) % 3], c = t[k + (e + 2) % 3];
                    float angulo = Vector3.Angle(v[b] - v[a], v[c] - v[a]) * Mathf.Deg2Rad;
                    grupos.TryGetValue(claves[a], out var suma);
                    grupos[claves[a]] = suma + cara * angulo;
                }
            }
            var uv = new List<Vector2>(v.Length);
            for (int i = 0; i < v.Length; i++)
            {
                grupos.TryGetValue(claves[i], out var suma);
                // Láminas de dos caras: las normales se anulan; se queda la normal propia del vértice.
                var suave = suma.sqrMagnitude > 1e-6f ? suma.normalized : (n.Length == v.Length ? n[i] : Vector3.up);
                uv.Add(Codificar(suave));
            }
            malla.SetUVs(Canal, uv);
        }

        /// <summary>
        /// Dirección → octaedro en [0.05, 0.95]². El (0, 0) queda libre: es lo que recibe el shader de una malla sin
        /// este canal, y así sabe que tiene que usar la normal de siempre.
        /// </summary>
        public static Vector2 Codificar(Vector3 d)
        {
            d /= Mathf.Abs(d.x) + Mathf.Abs(d.y) + Mathf.Abs(d.z);
            float t = Mathf.Clamp01(-d.z);
            float x = d.x + (d.x >= 0f ? t : -t), y = d.y + (d.y >= 0f ? t : -t);
            return new Vector2(0.05f + 0.9f * (x * 0.5f + 0.5f), 0.05f + 0.9f * (y * 0.5f + 0.5f));
        }

        /// <summary>Inversa de Codificar (la misma cuenta que hace el shader Tinta).</summary>
        public static Vector3 Decodificar(Vector2 uv)
        {
            float x = (uv.x - 0.05f) / 0.9f * 2f - 1f, y = (uv.y - 0.05f) / 0.9f * 2f - 1f;
            var d = new Vector3(x, y, 1f - Mathf.Abs(x) - Mathf.Abs(y));
            float t = Mathf.Clamp01(-d.z);
            d.x += d.x >= 0f ? -t : t;
            d.y += d.y >= 0f ? -t : t;
            return d.normalized;
        }
    }
}
