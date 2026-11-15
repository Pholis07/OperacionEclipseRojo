using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Retirada visible de un instrumento. El módulo de verdad se apaga en el mismo cuadro en que el jugador lo
    /// quita (la lógica no espera); esta es una copia que hace el recorrido inverso al montaje —se apaga el
    /// indicador, abren las fijaciones, se separa del soporte y se va por el lateral— y se destruye sola.
    /// No cuenta como instrumento: no tiene NaveUI ni Instalacion y su nombre empieza por «Retirada_».
    /// </summary>
    public class Retirada : MonoBehaviour
    {
        public const string Prefijo = "Retirada_";
        public const float Duracion = 0.75f;

        /// <summary>Crea la copia de «modulo» (recién apagado) y la pone a retirarse. Null si no hay animación.</summary>
        public static Retirada Empezar(Transform modulo)
        {
            if (modulo == null || !Application.isPlaying || Preferencias.ReducirMovimiento) return null;
            var previa = modulo.parent != null ? modulo.parent.Find(Prefijo + modulo.name) : null;
            if (previa != null) Destroy(previa.gameObject);
            var copia = Instantiate(modulo.gameObject, modulo.parent, false);
            copia.name = Prefijo + modulo.name;
            var montaje = copia.GetComponent<Instalacion>();
            Transform[] garras = montaje != null ? montaje.Fijaciones : new Transform[0];
            GameObject luz = montaje != null ? montaje.Indicador : null;
            if (montaje != null) DestroyImmediate(montaje);   // la copia no se «monta» al encenderse
            var r = copia.AddComponent<Retirada>();
            r.fijaciones = garras; r.indicador = luz;
            copia.SetActive(true);
            return r;
        }

        Transform[] fijaciones;
        GameObject indicador;

        IEnumerator Start()
        {
            var inicio = transform.localPosition;
            var giro = transform.localRotation;
            var fuera = giro * Vector3.up;
            var lado = Instalacion.LateralSeguro(inicio, fuera);
            if (indicador != null) indicador.SetActive(false);
            ReproductorUI.SonarEfecto(ArteDatos.Efecto.Servo, 0.3f);
            // Abren las fijaciones.
            yield return Animar.Durante(0.16f, t =>
            {
                for (int i = 0; i < fijaciones.Length; i++)
                    if (fijaciones[i] != null) fijaciones[i].localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 70f * t);
            });
            // Se separa del soporte y se va por el lateral.
            var sobre = inicio + fuera * 0.42f;
            yield return Animar.Durante(0.24f, t => transform.localPosition = Vector3.Lerp(inicio, sobre, t), x => x * x);
            var lejos = sobre + lado * 1.2f + fuera * 0.25f;
            var escala = transform.localScale;
            yield return Animar.Durante(0.35f, t =>
            {
                transform.localPosition = Vector3.Lerp(sobre, lejos, t);
                transform.localRotation = Quaternion.AngleAxis(60f * t, fuera) * giro;
                transform.localScale = escala * Mathf.Lerp(1f, 0.7f, t);
            }, x => x * x);
            Destroy(gameObject);
        }
    }
}
