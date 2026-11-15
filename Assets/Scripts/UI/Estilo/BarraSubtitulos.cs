using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Franja de subtítulos abajo de la pantalla, encima de todo (también de la cinemática). Aparece mientras suena
    /// una voz (Subtitulos.AlHablar) y se desvanece al terminar. No bloquea clics.
    /// </summary>
    public class BarraSubtitulos : MonoBehaviour
    {
        [SerializeField] CanvasGroup grupo;
        [SerializeField] TMP_Text texto;

        Coroutine rutina;

        void Awake() { if (grupo != null) grupo.alpha = 0f; }

        void OnEnable() => Subtitulos.AlHablar += Mostrar;

        void OnDisable() => Subtitulos.AlHablar -= Mostrar;

        void Mostrar(string linea, float duracion)
        {
            if (grupo == null || texto == null) return;
            texto.text = linea;
            if (rutina != null) StopCoroutine(rutina);
            rutina = StartCoroutine(Ver(Mathf.Max(1.5f, duracion + 0.4f)));
        }

        IEnumerator Ver(float duracion)
        {
            yield return Animar.Durante(0.2f, t => grupo.alpha = t);
            float fin = Time.unscaledTime + duracion;
            while (Time.unscaledTime < fin) yield return null;
            yield return Animar.Durante(0.35f, t => grupo.alpha = 1f - t);
            rutina = null;
        }
    }
}
