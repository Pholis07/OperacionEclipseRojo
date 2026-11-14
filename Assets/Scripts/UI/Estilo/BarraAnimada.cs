using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Va en el relleno de una barra de recursos. ConfiguracionUI fija el ancho de golpe (anchorMax.x);
    /// este componente lo detecta y lo lleva hasta ahí con suavidad. Si la barra se pasa del límite
    /// (cambia a color de exceso), parpadea una vez.
    /// </summary>
    public class BarraAnimada : MonoBehaviour
    {
        [SerializeField] float duracion = 0.35f;

        RectTransform rt;
        UnityEngine.UI.Image imagen;
        float mostrado, desde, objetivo, inicio = -1f;
        Color colorAnterior;

        void Awake()
        {
            rt = (RectTransform)transform;
            imagen = GetComponent<UnityEngine.UI.Image>();
            mostrado = objetivo = rt.anchorMax.x;
            colorAnterior = imagen != null ? imagen.color : Color.white;
        }

        void LateUpdate()
        {
            float escrito = rt.anchorMax.x;
            if (!Mathf.Approximately(escrito, mostrado))
            {
                // El script escribió un valor nuevo: animar desde lo que se veía.
                desde = mostrado;
                objetivo = escrito;
                inicio = Time.unscaledTime;
            }

            if (inicio >= 0f)
            {
                float t = Preferencias.ReducirMovimiento ? 1f : Mathf.Clamp01((Time.unscaledTime - inicio) / duracion);
                mostrado = Mathf.Lerp(desde, objetivo, Animar.SalidaCubica(t));
                if (t >= 1f) inicio = -1f;
            }
            rt.anchorMax = new Vector2(mostrado, rt.anchorMax.y);

            if (imagen != null && imagen.color != colorAnterior)
            {
                bool excedida = imagen.color.r > 0.85f && imagen.color.g < 0.4f;
                colorAnterior = imagen.color;
                if (excedida) ReproductorUI.Sonar(ArteDatos.Sonido.Error, 0.6f);
            }
        }
    }
}
