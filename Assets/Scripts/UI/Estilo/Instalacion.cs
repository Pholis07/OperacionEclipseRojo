using UnityEngine;

namespace SpaceMission
{
    /// <summary>Al aparecer un módulo de instrumento en la nave, se «instala» creciendo con un pequeño rebote.</summary>
    public class Instalacion : MonoBehaviour
    {
        Vector3 escalaFinal;
        bool lista;

        void Awake()
        {
            escalaFinal = transform.localScale;
            lista = true;
        }

        void OnEnable()
        {
            if (!lista || !Application.isPlaying) return;
            StopAllCoroutines();
            StartCoroutine(Animar.Durante(0.35f, t => transform.localScale = escalaFinal * Mathf.Max(0.01f, t),
                Animar.SalidaRebote));
            ReproductorUI.Sonar(ArteDatos.Sonido.Elegir, 0.8f);
        }

        void OnDisable() => transform.localScale = escalaFinal;
    }
}
