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
        [SerializeField] UnityEngine.UI.Button botonTutorial;    // versión 5: Misión 0
        [SerializeField] TutorialUI tutorial;

        public CatalogoDatos Catalogo => catalogo;

        void Awake()
        {
            botonContinuar.onClick.AddListener(MostrarConfiguracion);
            if (botonTutorial != null) botonTutorial.onClick.AddListener(IniciarTutorial);
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
            textoObjetivo.text = nivel.Objetivo;
            textoRequisitos.text = TextosBriefing.TextoRequisitos();
            textoLimites.text = TextosBriefing.TextoLimites(catalogo, nivel) +
                                Idioma.T("\nCampaign length: ") + nivel.orbitasNavegacion + (nivel.orbitasNavegacion == 1 ? Idioma.T(" month") : Idioma.T(" months")) +
                                Idioma.T("\nSpacecraft events: ") + nivel.eventosPorMision;
            textoColores.text = TextosBriefing.ExplicacionColores;
            for (int i = 0; i < botonesNivel.Length; i++)
            {
                botonesNivel[i].GetComponent<UnityEngine.UI.Image>().color =
                    i == EstadoJuego.NivelIndice ? new Color32(0xE2, 0x64, 0x3A, 0xFF) : new Color32(0x2A, 0x2A, 0x31, 0xFF);
                botonesNivel[i].GetComponentInChildren<TMP_Text>().text = catalogo.Nivel(i).NombreNivel;
            }
        }

        /// <summary>Cambia el nivel (solo desde el Briefing) y actualiza los límites que se muestran.</summary>
        public void ElegirNivel(int indice)
        {
            EstadoJuego.NivelIndice = indice;
            LlenarBriefing();
        }

        public void MostrarBriefing()
        {
            Mostrar(vistaBriefing);
            if (botonTutorial != null)
                botonTutorial.GetComponentInChildren<TMP_Text>().text = TutorialUI.Completado
                    ? Idioma.T("Replay Mission 0 (tutorial)") : Idioma.T("New? Play Mission 0 first");
        }

        /// <summary>Misión 0: nave fija, sin configuración ni cinemática, directo a la Operación guiada.</summary>
        public void IniciarTutorial()
        {
            if (tutorial == null) return;
            EstadoJuego.Tutorial = true;
            EstadoJuego.Configuracion = TutorialUI.ConfiguracionFija();
            EstadoJuego.Resultado = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice));
            EstadoJuego.Carga = new Carga(EstadoJuego.Resultado, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice));
            MostrarInvestigacion();
            tutorial.Comenzar();
        }

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
