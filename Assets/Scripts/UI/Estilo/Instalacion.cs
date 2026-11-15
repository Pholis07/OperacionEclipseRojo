using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Al aparecer un módulo de instrumento en la nave, se «instala»: llega desde fuera (a lo largo de la normal
    /// del casco), encaja con un pequeño rebote y suelta un destello en su zócalo. El módulo ya está activo desde
    /// el primer cuadro (NaveUI lo enciende en el mismo cuadro): solo cambian posición y escala.
    /// </summary>
    public class Instalacion : MonoBehaviour
    {
        [SerializeField] SpriteRenderer destello;
        [SerializeField] float distanciaLlegada = 0.9f;

        Vector3 escalaFinal, posicionFinal;
        bool animando;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            StopAllCoroutines();
            // La pose final es la que dejó NaveSatelite (posición, escala del satélite elegido).
            escalaFinal = transform.localScale;
            posicionFinal = transform.localPosition;
            if (Preferencias.ReducirMovimiento) return;
            StartCoroutine(Llegar());
            // Sin sonido aquí: aparecer no es una intención del jugador. Suena ConfiguracionUI al instalar.
        }

        IEnumerator Llegar()
        {
            animando = true;
            var fuera = transform.localRotation * Vector3.up;   // la normal del casco, en el espacio de la nave
            yield return Animar.Durante(0.45f, t =>
            {
                transform.localPosition = posicionFinal + fuera * distanciaLlegada * (1f - t);
                transform.localScale = escalaFinal * Mathf.Lerp(0.55f, 1f, t);
            }, Animar.SalidaCubica);
            transform.localPosition = posicionFinal;
            yield return Animar.Durante(0.18f, t => transform.localScale = escalaFinal * Mathf.Lerp(0.92f, 1f, Animar.SalidaRebote(t)));
            animando = false;
            if (destello != null) yield return Destellar();
        }

        IEnumerator Destellar()
        {
            destello.gameObject.SetActive(true);
            var escala = destello.transform.localScale;
            yield return Animar.Durante(0.4f, t =>
            {
                destello.transform.localScale = escala * Mathf.Lerp(0.6f, 2.2f, t);
                var c = destello.color; c.a = 1f - t; destello.color = c;
            });
            destello.transform.localScale = escala;
            destello.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (!animando) return;
            // Si se quita a mitad de la llegada, queda en su pose final para la próxima vez.
            transform.localPosition = posicionFinal;
            transform.localScale = escalaFinal;
            animando = false;
            if (destello != null) destello.gameObject.SetActive(false);
        }
    }
}
