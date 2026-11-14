using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Va en la luz del semáforo. Cuando ConfiguracionUI le cambia el color: el halo toma ese color y late,
    /// el icono cambia y suena; si pasa a rojo, el panel se sacude un poco. El color lo sigue poniendo ConfiguracionUI.
    /// </summary>
    public class LuzAnimada : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image halo;
        [SerializeField] TMPro.TMP_Text icono;
        [SerializeField] RectTransform panelASacudir;

        UnityEngine.UI.Image luz;
        Color anterior;
        Vector2 posicionPanel;

        void Awake()
        {
            luz = GetComponent<UnityEngine.UI.Image>();
            anterior = luz.color;
            if (panelASacudir != null) posicionPanel = panelASacudir.anchoredPosition;
        }

        void LateUpdate()
        {
            if (luz.color == anterior) return;
            anterior = luz.color;
            var estado = Estado(luz.color);
            if (icono != null) icono.text = Iconos.DeEstado(estado);
            if (halo != null) halo.color = new Color(luz.color.r, luz.color.g, luz.color.b, 0.55f);
            StopAllCoroutines();
            StartCoroutine(Latido());
            if (estado == EstadoSemaforo.Rojo)
            {
                ReproductorUI.Sonar(ArteDatos.Sonido.LuzRoja);
                if (panelASacudir != null && !Preferencias.ReducirMovimiento) StartCoroutine(Sacudir());
            }
            else if (estado == EstadoSemaforo.Verde) ReproductorUI.Sonar(ArteDatos.Sonido.LuzVerde);
        }

        static EstadoSemaforo Estado(Color c)
        {
            foreach (var e in new[] { EstadoSemaforo.Rojo, EstadoSemaforo.Amarillo, EstadoSemaforo.Verde })
                if (PresentacionMision.ColorSemaforo(e) == c) return e;
            return EstadoSemaforo.Gris;
        }

        IEnumerator Latido()
        {
            if (halo == null) yield break;
            var rt = halo.rectTransform;
            yield return Animar.Durante(0.45f, t => rt.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t));
        }

        IEnumerator Sacudir()
        {
            float inicio = Time.unscaledTime;
            while (Time.unscaledTime - inicio < 0.3f)
            {
                float fuerza = 8f * (1f - (Time.unscaledTime - inicio) / 0.3f);
                panelASacudir.anchoredPosition = posicionPanel + new Vector2(Mathf.Sin(Time.unscaledTime * 90f) * fuerza, 0f);
                yield return null;
            }
            panelASacudir.anchoredPosition = posicionPanel;
        }
    }
}
