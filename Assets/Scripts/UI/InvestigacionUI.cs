using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Vista Investigación: muestra las transmisiones de una en una, en el orden que da el Evaluador,
    /// con «Continuar investigación» entre cada una. Al terminar pasa a la escena Resultado.
    /// </summary>
    public class InvestigacionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] TMP_Text textoEncabezado;
        [SerializeField] TMP_Text textoTransmision;
        [SerializeField] UnityEngine.UI.Button botonContinuar;

        Evaluacion evaluacion;
        int indice;
        bool inicializada;

        public int Indice => indice;
        public string TextoActual => textoTransmision.text;
        public TMP_Text EtiquetaBoton => botonContinuar.GetComponentInChildren<TMP_Text>();

        void Awake() => Inicializar();

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            botonContinuar.onClick.AddListener(() => Avanzar());
        }

        public void Comenzar(Evaluacion e)
        {
            Inicializar();
            if (e == null || !e.SePuedeLanzar) return; // una configuración Gris o Roja nunca llega aquí
            evaluacion = e;
            indice = 0;
            Pintar();
        }

        /// <summary>Pasa a la siguiente transmisión. Devuelve false cuando ya no quedan y se va al informe.</summary>
        public bool Avanzar()
        {
            if (evaluacion == null) return false;
            if (indice < evaluacion.Transmisiones.Count - 1)
            {
                indice++;
                Pintar();
                return true;
            }
            if (Application.isPlaying) SceneManager.LoadScene("Resultado");
            return false;
        }

        void Pintar()
        {
            var id = evaluacion.Transmisiones[indice];
            textoEncabezado.text = InformeMision.Encabezado(id, indice, evaluacion.Transmisiones.Count, catalogo);
            textoTransmision.text = catalogo.Transmision(id).texto;
            bool ultima = indice == evaluacion.Transmisiones.Count - 1;
            EtiquetaBoton.text = ultima ? "Ver informe" : "Continuar investigación";
        }
    }
}
