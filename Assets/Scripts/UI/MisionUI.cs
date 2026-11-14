using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Escena Mision: cambia entre las vistas (paneles) Briefing, Configuración, Revisión e Investigación,
    /// y llena el Briefing con los datos. El Briefing es de solo lectura: no cambia al avanzar.
    /// </summary>
    public class MisionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;

        [Header("Vistas")]
        [SerializeField] GameObject vistaBriefing;
        [SerializeField] GameObject vistaConfiguracion;
        [SerializeField] GameObject vistaRevision;
        [SerializeField] GameObject vistaCarga;
        [SerializeField] GameObject vistaInvestigacion;
        [SerializeField] RevisionUI revision;
        [SerializeField] CargaUI carga;
        [SerializeField] OperacionUI operacion;

        [Header("Briefing")]
        [SerializeField] TMP_Text textoObjetivo;
        [SerializeField] TMP_Text textoRequisitos;
        [SerializeField] TMP_Text textoLimites;
        [SerializeField] TMP_Text textoColores;
        [SerializeField] UnityEngine.UI.Button botonContinuar;
        [SerializeField] UnityEngine.UI.Button[] botonesNivel;   // 0, 1 y 2

        public CatalogoDatos Catalogo => catalogo;

        void Awake()
        {
            botonContinuar.onClick.AddListener(MostrarConfiguracion);
            for (int i = 0; i < botonesNivel.Length; i++)
            {
                int nivel = i;
                botonesNivel[i].onClick.AddListener(() => ElegirNivel(nivel));
            }
            LlenarBriefing();
            AbrirVistaInicial();
        }

        /// <summary>Briefing al empezar; Configuración si se viene de «Modificar mi misión».</summary>
        public void AbrirVistaInicial()
        {
            if (EstadoJuego.EntrarEnConfiguracion)
            {
                EstadoJuego.EntrarEnConfiguracion = false;
                MostrarConfiguracion();
            }
            else
                MostrarBriefing();
        }

        public bool VistaActivaEsConfiguracion => vistaConfiguracion.activeSelf && !vistaRevision.activeSelf;

        public void LlenarBriefing()
        {
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            textoObjetivo.text = nivel.objetivo;
            textoRequisitos.text = TextosBriefing.TextoRequisitos();
            textoLimites.text = TextosBriefing.TextoLimites(catalogo, nivel) +
                                "\nTiempo de navegación: " + nivel.orbitasNavegacion + " órbitas" +
                                "\nEventos de la nave: " + nivel.eventosPorMision;
            textoColores.text = TextosBriefing.ExplicacionColores;
            for (int i = 0; i < botonesNivel.Length; i++)
            {
                botonesNivel[i].GetComponent<UnityEngine.UI.Image>().color =
                    i == EstadoJuego.NivelIndice ? new Color32(0xE2, 0x64, 0x3A, 0xFF) : new Color32(0x2A, 0x2A, 0x31, 0xFF);
                botonesNivel[i].GetComponentInChildren<TMP_Text>().text = catalogo.Nivel(i).nombreNivel;
            }
        }

        /// <summary>Cambia el nivel (solo desde el Briefing) y actualiza los límites que se muestran.</summary>
        public void ElegirNivel(int indice)
        {
            EstadoJuego.NivelIndice = indice;
            LlenarBriefing();
        }

        public void MostrarBriefing() => Mostrar(vistaBriefing);

        public void MostrarConfiguracion() => Mostrar(vistaConfiguracion);

        public void MostrarRevision(Evaluacion e)
        {
            Mostrar(vistaRevision);
            revision.Mostrar(e);
        }

        /// <summary>Carga de la nave (v3), entre Revisión y Lanzar. Sin la vista (escena vieja), lanza directo.</summary>
        public void MostrarCarga(Evaluacion e)
        {
            if (vistaCarga == null || carga == null)
            {
                EstadoJuego.Carga = null;
                MostrarInvestigacion();
                return;
            }
            Mostrar(vistaCarga);
            carga.Mostrar(e);
        }

        public void MostrarInvestigacion()
        {
            Mostrar(vistaInvestigacion);
            operacion.Comenzar(EstadoJuego.Resultado, EstadoJuego.Carga);
        }

        void Mostrar(GameObject vista)
        {
            vistaBriefing.SetActive(vista == vistaBriefing);
            // La configuración sigue visible debajo de la Revisión (la Revisión es una tarjeta encima).
            vistaConfiguracion.SetActive(vista == vistaConfiguracion || vista == vistaRevision || (vista != null && vista == vistaCarga));
            vistaRevision.SetActive(vista == vistaRevision);
            if (vistaCarga != null) vistaCarga.SetActive(vista == vistaCarga);
            vistaInvestigacion.SetActive(vista == vistaInvestigacion);
        }
    }
}
