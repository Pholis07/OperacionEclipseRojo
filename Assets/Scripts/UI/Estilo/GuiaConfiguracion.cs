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
    /// - Teclas 1 a 4 para cambiar de pestaña (orden en pantalla: Órbita, Lanzador, Satélite, Instrumentos).
    /// </summary>
    public class GuiaConfiguracion : MonoBehaviour
    {
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] GameObject vistaRevision;
        [SerializeField] TMP_Text textoPestanaOrbita;
        [SerializeField] TMP_Text textoPestanaLanzador;
        [SerializeField] TMP_Text textoPestanaInstrumentos;
        [SerializeField] TMP_Text textoPestanaSatelite;
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

        /// <summary>Orden de las pestañas en pantalla.</summary>
        static readonly ConfiguracionUI.Pestana[] Orden =
        {
            ConfiguracionUI.Pestana.Orbita, ConfiguracionUI.Pestana.Lanzador,
            ConfiguracionUI.Pestana.Satelite, ConfiguracionUI.Pestana.Instrumentos
        };

        bool ConSatelite => textoPestanaSatelite != null;

        ConfiguracionUI.Pestana? Despues(ConfiguracionUI.Pestana p)
        {
            int i = System.Array.IndexOf(Orden, p) + 1;
            if (i < Orden.Length && Orden[i] == ConfiguracionUI.Pestana.Satelite && !ConSatelite) i++;
            return i < Orden.Length ? Orden[i] : (ConfiguracionUI.Pestana?)null;
        }

        public void Siguiente()
        {
            var sig = Despues(configuracion.PestanaActual);
            if (sig != null) configuracion.MostrarPestana(sig.Value);
        }

        void Update()
        {
            if (vistaRevision == null || !vistaRevision.activeInHierarchy) Teclas();

            var c = EstadoJuego.Configuracion;
            var pestana = configuracion.PestanaActual;
            var nuevaFirma = c.OrbitaId + "|" + c.LanzadorId + "|" + c.SateliteId + "|" + c.InstrumentoIds.Count + "|" + pestana;
            if (nuevaFirma == firma) return;
            firma = nuevaFirma;

            textoPestanaOrbita.text = Pestana(Iconos.Orbita, "Orbit", !string.IsNullOrEmpty(c.OrbitaId));
            textoPestanaLanzador.text = Pestana(Iconos.Lanzador, "Launcher", !string.IsNullOrEmpty(c.LanzadorId));
            textoPestanaInstrumentos.text = Pestana(ConSatelite ? Iconos.Microscopio : Iconos.Instrumentos, "Instruments", c.InstrumentoIds.Count > 0);
            if (ConSatelite) textoPestanaSatelite.text = Pestana(Iconos.Satelite, "Satellite", !string.IsNullOrEmpty(c.SateliteId));

            var despues = Despues(pestana);
            botonSiguiente.gameObject.SetActive(despues != null);
            if (despues != null)
                botonSiguiente.GetComponentInChildren<TMP_Text>().text = NombrePestana(despues.Value) + "  " + Iconos.Siguiente;
            int n = ConSatelite ? 4 : 3;

            switch (pestana)
            {
                case ConfiguracionUI.Pestana.Orbita:
                    textoInstruccion.text = "1 · CHOOSE THE ORBIT";
                    textoLeyenda.text = "MARS\n<size=24><color=#94A3B8>Each color is an orbit: the chosen one glows and your spacecraft travels along it.</color></size>";
                    break;
                case ConfiguracionUI.Pestana.Lanzador:
                    textoInstruccion.text = "2 · CHOOSE THE ROCKET";
                    textoLeyenda.text = string.IsNullOrEmpty(c.LanzadorId)
                        ? "LAUNCH PAD\n<size=24><color=#94A3B8>Explorer: look inspired by the Soyuz · Titan: look inspired by the SLS. Fictional capabilities.</color></size>"
                        : PlataformaCohetes.EsTitan(c.LanzadorId)
                            ? "TITAN\n<size=24><color=#94A3B8>Look inspired by NASA’s SLS. Fictional capabilities.</color></size>"
                            : "EXPLORER\n<size=24><color=#94A3B8>Look inspired by the Soyuz. Fictional capabilities.</color></size>";
                    break;
                case ConfiguracionUI.Pestana.Satelite:
                    textoInstruccion.text = "3 · CHOOSE THE SATELLITE";
                    textoLeyenda.text = "YOUR SATELLITE\n<size=24><color=#94A3B8>Inspired by real NASA data. Green arrows help, red arrows cost: see the list on the left.</color></size>";
                    break;
                default:
                    textoInstruccion.text = n + " · INSTALL THE INSTRUMENTS";
                    textoLeyenda.text = "SPACECRAFT VIEW\n<size=24><color=#94A3B8>When you choose an instrument you will see where it is installed.</color></size>";
                    break;
            }
        }

        static string NombrePestana(ConfiguracionUI.Pestana p) =>
            p == ConfiguracionUI.Pestana.Lanzador ? "Launcher" : p == ConfiguracionUI.Pestana.Satelite ? "Satellite" :
            p == ConfiguracionUI.Pestana.Instrumentos ? "Instruments" : "Orbit";

        static string Pestana(string icono, string nombre, bool lista) =>
            icono + " " + nombre + (lista ? " <color=" + Verde + ">" + Iconos.Verde + "</color>" : "");

        void Teclas()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) configuracion.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) configuracion.MostrarPestana(ConfiguracionUI.Pestana.Lanzador);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame)
                configuracion.MostrarPestana(ConSatelite ? ConfiguracionUI.Pestana.Satelite : ConfiguracionUI.Pestana.Instrumentos);
            else if (ConSatelite && (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame))
                configuracion.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
        }
    }
}
