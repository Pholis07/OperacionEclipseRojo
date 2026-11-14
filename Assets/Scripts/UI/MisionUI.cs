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
        [SerializeField] GameObject vistaInvestigacion;
        [SerializeField] RevisionUI revision;

        [Header("Briefing")]
        [SerializeField] TMP_Text textoObjetivo;
        [SerializeField] TMP_Text textoRequisitos;
        [SerializeField] TMP_Text textoLimites;
        [SerializeField] TMP_Text textoColores;
        [SerializeField] UnityEngine.UI.Button botonContinuar;

        public CatalogoDatos Catalogo => catalogo;

        void Awake()
        {
            botonContinuar.onClick.AddListener(MostrarConfiguracion);
            LlenarBriefing();
            MostrarBriefing();
        }

        public void LlenarBriefing()
        {
            textoObjetivo.text = catalogo.mision.objetivo;
            textoRequisitos.text = TextosBriefing.TextoRequisitos();
            textoLimites.text = TextosBriefing.TextoLimites(catalogo);
            textoColores.text = TextosBriefing.ExplicacionColores;
        }

        public void MostrarBriefing() => Mostrar(vistaBriefing);

        public void MostrarConfiguracion() => Mostrar(vistaConfiguracion);

        public void MostrarRevision(Evaluacion e)
        {
            Mostrar(vistaRevision);
            revision.Mostrar(e);
        }

        public void MostrarInvestigacion() => Mostrar(vistaInvestigacion);

        void Mostrar(GameObject vista)
        {
            vistaBriefing.SetActive(vista == vistaBriefing);
            // La configuración sigue visible debajo de la Revisión (la Revisión es una tarjeta encima).
            vistaConfiguracion.SetActive(vista == vistaConfiguracion || vista == vistaRevision);
            vistaRevision.SetActive(vista == vistaRevision);
            vistaInvestigacion.SetActive(vista == vistaInvestigacion);
        }
    }
}
