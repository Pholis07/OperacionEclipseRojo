using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Vista Operación: muestra la imagen real (NASA) del instrumento que envió la última transmisión recibida,
    /// con su crédito, un destello de «señal recibida» y la voz si existe. Solo lee el estado de OperacionUI;
    /// no cambia su lógica.
    /// </summary>
    public class VisorEvidencia : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] ArteDatos arte;
        [SerializeField] OperacionUI operacion;
        [SerializeField] UnityEngine.UI.RawImage imagen;
        [SerializeField] TMP_Text textoFuente;
        [SerializeField] TMP_Text textoCredito;
        [SerializeField] CanvasGroup senal; // franja «SEÑAL RECIBIDA» que destella al llegar cada transmisión

        string mostrada;
        int recibidas = -1;

        void OnEnable()
        {
            mostrada = null;
            recibidas = -1;
        }

        void Update()
        {
            var op = operacion != null ? operacion.Op : null;
            if (op == null || op.Recibidas.Count == 0) return;
            if (op.Recibidas.Count == recibidas) return;
            // Si llegan varias en el mismo mes, se oyen todas en fila (ReproductorUI.Hablar no corta la anterior);
            // la imagen muestra la última. Al abrir la vista, solo suena la última (como antes: TX-00 al empezar).
            int desde = recibidas < 0 ? op.Recibidas.Count - 1 : recibidas;
            recibidas = op.Recibidas.Count;
            for (int i = desde; i < op.Recibidas.Count; i++) ReproductorUI.Hablar(arte.VozDe(op.Recibidas[i]));
            var id = op.Recibidas.Last();
            if (id == mostrada) return;
            mostrada = id;
            Mostrar(catalogo.Transmision(id));
        }

        void Mostrar(TransmisionDatos t)
        {
            var instrumento = string.IsNullOrEmpty(t.instrumentoId) ? null : catalogo.Instrumento(t.instrumentoId);
            var evidencia = instrumento != null ? arte.DeInstrumento(instrumento.id) : null;
            imagen.texture = evidencia != null && evidencia.imagen != null ? evidencia.imagen : arte.imagenMision;
            textoCredito.text = evidencia != null ? evidencia.Credito : arte.creditoMision;
            textoFuente.text = instrumento != null
                ? Iconos.DeInstrumento(instrumento.id) + "  " + instrumento.Nombre.ToUpperInvariant()
                : Iconos.Senal + Idioma.T("  MISSION CONTROL");

            ReproductorUI.Sonar(ArteDatos.Sonido.Senal, 0.7f);
            StopAllCoroutines();
            StartCoroutine(Entrada());
        }

        IEnumerator Entrada()
        {
            var rt = imagen.rectTransform;
            if (senal != null) senal.alpha = 1f;
            yield return Animar.Durante(0.8f, t =>
            {
                imagen.color = Color.Lerp(new Color(0.4f, 0.8f, 1f, 0f), Color.white, t);
                rt.localScale = Vector3.one * Mathf.Lerp(1.04f, 1f, t);
                if (senal != null) senal.alpha = 1f - t;
            });
        }
    }
}
