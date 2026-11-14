using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Al activarse, la vista entra con un fundido y un pequeño desplazamiento. Los hijos marcados en
    /// «escalonados» entran uno tras otro. La vista se activa al instante: la animación es solo visual.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class AparecerVista : MonoBehaviour
    {
        [SerializeField] float duracion = 0.3f;
        [SerializeField] float desplazamiento = 24f;
        [SerializeField] RectTransform[] escalonados;
        [SerializeField] float retrasoEntrePiezas = 0.07f;

        CanvasGroup grupo;
        Vector2[] posiciones;

        void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
            posiciones = new Vector2[escalonados?.Length ?? 0];
            for (int i = 0; i < posiciones.Length; i++)
                if (escalonados[i] != null) posiciones[i] = escalonados[i].anchoredPosition;
        }

        void OnEnable()
        {
            if (grupo == null) Awake();
            StopAllCoroutines();
            StartCoroutine(Entrar());
        }

        void OnDisable()
        {
            // Si se apaga a mitad de la animación, queda en su estado final para la próxima vez.
            grupo.alpha = 1f;
            for (int i = 0; i < posiciones.Length; i++)
                if (escalonados[i] != null) escalonados[i].anchoredPosition = posiciones[i];
        }

        IEnumerator Entrar()
        {
            for (int i = 0; i < posiciones.Length; i++)
                if (escalonados[i] != null) StartCoroutine(Pieza(i, i * retrasoEntrePiezas));
            yield return Animar.Durante(duracion, t => grupo.alpha = t);
        }

        IEnumerator Pieza(int i, float retraso)
        {
            var pieza = escalonados[i];
            var final = posiciones[i];
            var grupoPieza = pieza.GetComponent<CanvasGroup>();
            pieza.anchoredPosition = final + Vector2.down * desplazamiento;
            if (grupoPieza != null) grupoPieza.alpha = 0f;
            if (retraso > 0f && !Preferencias.ReducirMovimiento) yield return new WaitForSecondsRealtime(retraso);
            yield return Animar.Durante(duracion, t =>
            {
                pieza.anchoredPosition = Vector2.LerpUnclamped(final + Vector2.down * desplazamiento, final, t);
                if (grupoPieza != null) grupoPieza.alpha = t;
            });
        }
    }
}
