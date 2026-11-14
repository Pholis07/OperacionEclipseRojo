using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>
    /// Navegación de la vista Configuración, encima de ConfiguracionUI (sin cambiar su lógica):
    /// - Pestañas numeradas con ✓ cuando esa parte ya está elegida.
    /// - Botón «Siguiente» que pasa a la pestaña siguiente (en la última se oculta: allí se revisa la misión).
    /// - Una instrucción corta según la pestaña y la leyenda de lo que se ve en 3D.
    /// - Teclas 1, 2 y 3 para cambiar de pestaña.
    /// </summary>
    public class GuiaConfiguracion : MonoBehaviour
    {
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] GameObject vistaRevision;
        [SerializeField] TMP_Text textoPestanaOrbita;
        [SerializeField] TMP_Text textoPestanaLanzador;
        [SerializeField] TMP_Text textoPestanaInstrumentos;
        [SerializeField] UnityEngine.UI.Button botonSiguiente;
        [SerializeField] TMP_Text textoInstruccion;
        [SerializeField] TMP_Text textoLeyenda;

        const string Verde = "#22C55E";
        ConfiguracionUI.Pestana? pintada;
        string firma;

        void Awake()
        {
            botonSiguiente.onClick.AddListener(Siguiente);
        }

        public void Siguiente()
        {
            if (configuracion.PestanaActual == ConfiguracionUI.Pestana.Orbita)
                configuracion.MostrarPestana(ConfiguracionUI.Pestana.Lanzador);
            else if (configuracion.PestanaActual == ConfiguracionUI.Pestana.Lanzador)
                configuracion.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
        }

        void Update()
        {
            if (vistaRevision == null || !vistaRevision.activeInHierarchy) Teclas();

            var c = EstadoJuego.Configuracion;
            var pestana = configuracion.PestanaActual;
            var nuevaFirma = c.OrbitaId + "|" + c.LanzadorId + "|" + c.InstrumentoIds.Count + "|" + pestana;
            if (nuevaFirma == firma) return;
            firma = nuevaFirma;

            textoPestanaOrbita.text = Pestana(Iconos.Orbita, "Órbita", !string.IsNullOrEmpty(c.OrbitaId));
            textoPestanaLanzador.text = Pestana(Iconos.Lanzador, "Lanzador", !string.IsNullOrEmpty(c.LanzadorId));
            textoPestanaInstrumentos.text = Pestana(Iconos.Instrumentos, "Instrumentos", c.InstrumentoIds.Count > 0);

            botonSiguiente.gameObject.SetActive(pestana != ConfiguracionUI.Pestana.Instrumentos);
            var siguiente = pestana == ConfiguracionUI.Pestana.Orbita ? "Lanzador" : "Instrumentos";
            botonSiguiente.GetComponentInChildren<TMP_Text>().text = siguiente + "  " + Iconos.Siguiente;

            switch (pestana)
            {
                case ConfiguracionUI.Pestana.Orbita:
                    textoInstruccion.text = "1 · ELIGE LA ÓRBITA";
                    textoLeyenda.text = "MARTE\n<size=24><color=#94A3B8>El anillo cian muestra la forma de la órbita elegida.</color></size>";
                    break;
                case ConfiguracionUI.Pestana.Lanzador:
                    textoInstruccion.text = "2 · ELIGE EL COHETE";
                    textoLeyenda.text = "PLATAFORMA DE LANZAMIENTO\n<size=24><color=#94A3B8>Explorador: tipo Soyuz · Titán: tipo SLS (Artemis II)</color></size>";
                    break;
                default:
                    textoInstruccion.text = "3 · INSTALA LOS INSTRUMENTOS";
                    textoLeyenda.text = "VISTA DE LA NAVE\n<size=24><color=#94A3B8>Al elegir un instrumento verás dónde se instala.</color></size>";
                    break;
            }
        }

        static string Pestana(string icono, string nombre, bool lista) =>
            icono + " " + nombre + (lista ? " <color=" + Verde + ">" + Iconos.Verde + "</color>" : "");

        void Teclas()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) configuracion.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) configuracion.MostrarPestana(ConfiguracionUI.Pestana.Lanzador);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) configuracion.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
        }
    }
}
