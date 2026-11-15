using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 5, bloque 3: contorno que late en lo que la guía señala (un botón, una fila o el mapa).
    /// Lo pone y lo quita OperacionUI; con «Reducir movimiento» queda fijo, sin latir.
    /// </summary>
    public class ResaltarGuia : MonoBehaviour
    {
        UnityEngine.UI.Outline contorno;
        static readonly Color Color = new Color32(0x75, 0xD6, 0xDF, 0xFF);

        void OnEnable()
        {
            contorno = GetComponent<UnityEngine.UI.Outline>();
            if (contorno == null) contorno = gameObject.AddComponent<UnityEngine.UI.Outline>();
            contorno.effectDistance = new Vector2(4, -4);
            contorno.effectColor = Color;
        }

        void Update()
        {
            if (contorno == null || Preferencias.ReducirMovimiento) return;
            var c = Color;
            c.a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
            contorno.effectColor = c;
        }

        static void Quitar(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        /// <summary>Mueve el resaltado a «destino» (o lo quita si es null).</summary>
        public static void Mover(ref ResaltarGuia actual, GameObject destino)
        {
            if (actual != null && destino != null && actual.gameObject == destino) return;
            if (actual != null)   // si la fila se redibujó, ya no existe y no hay nada que quitar
            {
                var contorno = actual.GetComponent<UnityEngine.UI.Outline>();
                Quitar(actual);
                Quitar(contorno);
            }
            actual = destino != null ? destino.AddComponent<ResaltarGuia>() : null;
        }
    }
}
