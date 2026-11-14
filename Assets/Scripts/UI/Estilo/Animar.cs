using System;
using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Interpolaciones pequeñas con corrutinas (sin DOTween). Usan tiempo sin escala y nunca bloquean la lógica:
    /// solo cambian alfa, escala o posición, así que los botones funcionan desde el primer cuadro.
    /// </summary>
    public static class Animar
    {
        public static float SalidaCubica(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float SalidaRebote(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>Llama a <paramref name="paso"/> con t de 0 a 1 (ya suavizado) durante <paramref name="duracion"/> segundos.</summary>
        public static IEnumerator Durante(float duracion, Action<float> paso, Func<float, float> curva = null)
        {
            curva = curva ?? SalidaCubica;
            if (duracion <= 0f || Preferencias.ReducirMovimiento)
            {
                paso(1f);
                yield break;
            }
            float inicio = Time.unscaledTime;
            while (true)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - inicio) / duracion);
                paso(curva(t));
                if (t >= 1f) yield break;
                yield return null;
            }
        }

        /// <summary>Detiene la corrutina anterior (si hay) y arranca una nueva en el mismo dueño.</summary>
        public static void Reemplazar(MonoBehaviour dueno, ref Coroutine actual, IEnumerator nueva)
        {
            if (actual != null) dueno.StopCoroutine(actual);
            actual = dueno.isActiveAndEnabled ? dueno.StartCoroutine(nueva) : null;
        }
    }
}
