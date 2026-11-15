using UnityEngine;
using UnityEngine.UI;

namespace SpaceMission
{
    /// <summary>
    /// Barra de inspección de la nave en el constructor: un botón de foco por instrumento instalado (los demás no
    /// se ven) y «Restablecer vista». Solo mueve la cámara (CamaraConfiguracion): no instala, no cobra, no cambia
    /// la configuración. Se oculta en las pestañas donde la nave no se inspecciona (Lanzador y Órbita).
    /// </summary>
    public class InspeccionNave : MonoBehaviour
    {
        [SerializeField] CamaraConfiguracion camara;
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] string[] ids = new string[0];
        [SerializeField] Button[] botonesFoco = new Button[0];
        [SerializeField] Button botonRestablecer;
        [SerializeField] GameObject contenido;

        void Awake()
        {
            for (int i = 0; i < botonesFoco.Length && i < ids.Length; i++)
            {
                string id = ids[i];
                if (botonesFoco[i] != null) botonesFoco[i].onClick.AddListener(() => { if (camara != null) camara.Enfocar(id); });
            }
            if (botonRestablecer != null) botonRestablecer.onClick.AddListener(() => { if (camara != null) camara.Restablecer(); });
        }

        void Update() => Actualizar();

        /// <summary>Muestra los botones de los instrumentos montados. Lo llama Update; público para las pruebas.</summary>
        public void Actualizar()
        {
            var config = EstadoJuego.Configuracion;
            bool visible = configuracion != null && (configuracion.PestanaActual == ConfiguracionUI.Pestana.Instrumentos ||
                                                    configuracion.PestanaActual == ConfiguracionUI.Pestana.Satelite);
            if (contenido != null && contenido.activeSelf != visible) contenido.SetActive(visible);
            if (!visible || config == null) return;
            for (int i = 0; i < botonesFoco.Length && i < ids.Length; i++)
            {
                if (botonesFoco[i] == null) continue;
                bool montado = config.InstrumentoIds.Contains(ids[i]);
                if (botonesFoco[i].gameObject.activeSelf != montado) botonesFoco[i].gameObject.SetActive(montado);
            }
        }
    }
}
