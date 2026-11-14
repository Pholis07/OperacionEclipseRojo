using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Escena Mision: cambia entre las vistas (paneles) y llena el Briefing con los datos.
    /// El Briefing es de solo lectura: no cambia al avanzar.
    /// </summary>
    public class MisionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;

        [Header("Vistas")]
        [SerializeField] GameObject vistaBriefing;
        [SerializeField] GameObject vistaConfiguracion;

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

        void Mostrar(GameObject vista)
        {
            vistaBriefing.SetActive(vista == vistaBriefing);
            vistaConfiguracion.SetActive(vista == vistaConfiguracion);
        }
    }
}
