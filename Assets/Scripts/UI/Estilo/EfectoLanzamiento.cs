using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Al pulsar «Lanzar» (con la misión lista), un velo con «LANZAMIENTO» y un destello cubre la pantalla y se
    /// desvanece mientras la Investigación ya está activa debajo. No bloquea clics ni retrasa nada.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class EfectoLanzamiento : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button botonLanzar;
        [SerializeField] TMP_Text texto;
        [SerializeField] MusicaEscena musica;
        [SerializeField] float duracion = 1.6f;

        CanvasGroup grupo;

        void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            grupo.alpha = 0f;
            botonLanzar.onClick.AddListener(() =>
            {
                if (!botonLanzar.interactable) return;
                ReproductorUI.Sonar(ArteDatos.Sonido.Lanzamiento);
                if (musica != null) musica.Cambiar(ArteDatos.Musica.Investigacion);
                StopAllCoroutines();
                StartCoroutine(Destello());
            });
        }

        IEnumerator Destello()
        {
            grupo.alpha = 1f;
            var rt = texto.rectTransform;
            yield return Animar.Durante(duracion, t =>
            {
                grupo.alpha = 1f - t;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, t);
                texto.characterSpacing = Mathf.Lerp(8f, 40f, t);
            });
            grupo.alpha = 0f;
        }
    }
}
