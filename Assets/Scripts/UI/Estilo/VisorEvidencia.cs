using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Vista Investigación: muestra la imagen real (NASA) del instrumento que envía la transmisión actual,
    /// con su crédito, una entrada de «señal recibida» y la voz si existe. Lee la transmisión de
    /// EstadoJuego.Resultado y del índice de InvestigacionUI, sin cambiar su lógica.
    /// </summary>
    public class VisorEvidencia : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] ArteDatos arte;
        [SerializeField] InvestigacionUI investigacion;
        [SerializeField] UnityEngine.UI.RawImage imagen;
        [SerializeField] TMP_Text textoFuente;
        [SerializeField] TMP_Text textoCredito;
        [SerializeField] CanvasGroup senal; // franja «SEÑAL RECIBIDA» que destella al llegar cada transmisión

        string mostrada;

        void OnEnable() => mostrada = null;

        void Update()
        {
            var e = EstadoJuego.Resultado;
            if (e == null || e.Transmisiones.Count == 0 || investigacion == null) return;
            int i = Mathf.Clamp(investigacion.Indice, 0, e.Transmisiones.Count - 1);
            var id = e.Transmisiones[i];
            if (id == mostrada) return;
            mostrada = id;
            Mostrar(catalogo.Transmision(id));
        }

        void Mostrar(TransmisionDatos t)
        {
            var instrumento = string.IsNullOrEmpty(t.instrumentoId) ? null : catalogo.Instrumento(t.instrumentoId);
            var evidencia = instrumento != null ? arte.DeInstrumento(instrumento.id) : null;
            imagen.texture = evidencia != null ? evidencia.imagen : arte.imagenMision;
            textoCredito.text = "Imagen: " + (evidencia != null ? evidencia.credito : arte.creditoMision);
            textoFuente.text = instrumento != null
                ? Iconos.DeInstrumento(instrumento.id) + "  " + instrumento.nombre.ToUpperInvariant()
                : Iconos.Senal + "  CONTROL DE MISIÓN";

            ReproductorUI.Sonar(ArteDatos.Sonido.Senal, 0.7f);
            ReproductorUI.Hablar(arte.VozDe(t.id));
            StopAllCoroutines();
            StartCoroutine(Entrada());
        }

        IEnumerator Entrada()
        {
            var rt = imagen.rectTransform;
            if (senal != null) senal.alpha = 1f;
            yield return Animar.Durante(0.6f, t =>
            {
                imagen.color = Color.Lerp(new Color(0.4f, 0.8f, 1f, 0f), Color.white, t);
                rt.localScale = Vector3.one * Mathf.Lerp(1.04f, 1f, t);
                if (senal != null) senal.alpha = 1f - t;
            });
        }
    }
}
