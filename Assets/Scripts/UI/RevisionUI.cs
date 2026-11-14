using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Vista Revisión: elecciones, totales y advertencias. «Lanzar» solo en Amarillo o Verde.</summary>
    public class RevisionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] MisionUI mision;
        [SerializeField] TMP_Text textoResumen;
        [SerializeField] UnityEngine.UI.Button botonCorregir;
        [SerializeField] UnityEngine.UI.Button botonLanzar;

        Evaluacion evaluacion;
        bool inicializada;

        public UnityEngine.UI.Button BotonLanzar => botonLanzar;

        void Awake() => Inicializar();

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            botonCorregir.onClick.AddListener(() => mision.MostrarConfiguracion());
            botonLanzar.onClick.AddListener(Lanzar);
        }

        public void Mostrar(Evaluacion e)
        {
            evaluacion = e;
            textoResumen.text = PresentacionMision.Resumen(e, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice));
            botonLanzar.interactable = e.SePuedeLanzar;
        }

        void Lanzar()
        {
            if (evaluacion == null || !evaluacion.SePuedeLanzar) return;
            EstadoJuego.Resultado = evaluacion;
            mision.MostrarCarga(evaluacion);
        }
    }
}
