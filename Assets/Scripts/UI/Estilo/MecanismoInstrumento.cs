using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Piezas móviles de un instrumento (botalones del radar, tapa y parasol de la cámara, mástil del sensor…):
    /// cada una con su pivote y dos poses, plegada y desplegada. En el hangar van plegadas; al liberarse el
    /// satélite se despliegan una tras otra (cada pieza tiene su tramo del recorrido, con una pausa de
    /// asentamiento entre tiempos). Solo presentación: no cambia consumos ni resultados.
    /// </summary>
    public class MecanismoInstrumento : MonoBehaviour
    {
        [System.Serializable]
        public class Pieza
        {
            public Transform pivote;
            public Vector3 posicionPlegada, posicionAbierta;
            public Vector3 giroPlegado, giroAbierto;
            [Range(0f, 1f)] public float desde;
            [Range(0f, 1f)] public float hasta = 1f;
        }

        [SerializeField] Pieza[] piezas = new Pieza[0];
        [SerializeField] float despliegue;

        /// <summary>0 = plegado (integración y lanzamiento); 1 = desplegado (en órbita).</summary>
        public float Despliegue => despliegue;
        public int Piezas => piezas.Length;
        /// <summary>Los pivotes de las piezas móviles (el constructor mide con ellos si chocan con la nave).</summary>
        public System.Collections.Generic.IEnumerable<Transform> Pivotes => System.Linq.Enumerable.Select(piezas, p => p.pivote);

        public void Fijar(Pieza[] lista) { piezas = lista; Desplegar(0f); }

        public void Desplegar(float k)
        {
            despliegue = Mathf.Clamp01(k);
            foreach (var p in piezas)
            {
                if (p.pivote == null) continue;
                float t = Mathf.InverseLerp(p.desde, Mathf.Max(p.hasta, p.desde + 0.001f), despliegue);
                t = t * t * (3f - 2f * t);   // arranque y frenado suaves: peso, sin rebote
                p.pivote.localPosition = Vector3.Lerp(p.posicionPlegada, p.posicionAbierta, t);
                p.pivote.localRotation = Quaternion.Slerp(Quaternion.Euler(p.giroPlegado), Quaternion.Euler(p.giroAbierto), t);
            }
        }
    }
}
