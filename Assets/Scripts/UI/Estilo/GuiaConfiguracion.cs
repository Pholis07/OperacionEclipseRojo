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
    /// - Teclas 1 a 4 para cambiar de pestaña. Orden de la versión 6: Lanzador, Satélite, Instrumentos y Órbita
    ///   (primero se arma la nave; su masa y el lanzador deciden a qué órbitas llega).
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
        public static readonly ConfiguracionUI.Pestana[] Orden =
        {
            ConfiguracionUI.Pestana.Lanzador, ConfiguracionUI.Pestana.Satelite,
            ConfiguracionUI.Pestana.Instrumentos, ConfiguracionUI.Pestana.Orbita
        };

        bool ConSatelite => textoPestanaSatelite != null;

        /// <summary>Pestañas que se ven, en orden (sin la de satélite si la escena no la tiene).</summary>
        System.Collections.Generic.List<ConfiguracionUI.Pestana> Visibles =>
            new System.Collections.Generic.List<ConfiguracionUI.Pestana>(
                System.Linq.Enumerable.Where(Orden, p => p != ConfiguracionUI.Pestana.Satelite || ConSatelite));

        /// <summary>Número del paso en pantalla (1, 2, 3…).</summary>
        int Paso(ConfiguracionUI.Pestana p) => Visibles.IndexOf(p) + 1;

        ConfiguracionUI.Pestana? Despues(ConfiguracionUI.Pestana p)
        {
            var v = Visibles;
            int i = v.IndexOf(p) + 1;
            return i < v.Count ? v[i] : (ConfiguracionUI.Pestana?)null;
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

            textoPestanaOrbita.text = Pestana(Iconos.Orbita, Idioma.T("Orbit"), !string.IsNullOrEmpty(c.OrbitaId));
            textoPestanaLanzador.text = Pestana(Iconos.Lanzador, Idioma.T("Launcher"), !string.IsNullOrEmpty(c.LanzadorId));
            textoPestanaInstrumentos.text = Pestana(ConSatelite ? Iconos.Microscopio : Iconos.Instrumentos, Idioma.T("Instruments"), c.InstrumentoIds.Count > 0);
            if (ConSatelite) textoPestanaSatelite.text = Pestana(Iconos.Satelite, Idioma.T("Satellite"), !string.IsNullOrEmpty(c.SateliteId));

            var despues = Despues(pestana);
            botonSiguiente.gameObject.SetActive(despues != null);
            if (despues != null)
                botonSiguiente.GetComponentInChildren<TMP_Text>().text = NombrePestana(despues.Value) + "  " + Iconos.Siguiente;
            int n = Paso(pestana);

            switch (pestana)
            {
                case ConfiguracionUI.Pestana.Orbita:
                    textoInstruccion.text = n + Idioma.T(" · CHOOSE THE ORBIT");
                    textoLeyenda.text = Idioma.T("MARS\n<size=24><color=#94A3B8>Each color is an orbit. Low orbits need more braking: the ones your spacecraft cannot reach are locked.</color></size>");
                    break;
                case ConfiguracionUI.Pestana.Lanzador:
                    textoInstruccion.text = n + Idioma.T(" · CHOOSE THE ROCKET");
                    var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : configuracion.Catalogo.Lanzador(c.LanzadorId);
                    textoLeyenda.text = lanzador == null
                        ? Idioma.T("LAUNCH PAD\n<size=24><color=#94A3B8>The rocket decides how much mass you can carry and how far it can take it.</color></size>")
                        : (PlataformaCohetes.EsTitan(c.LanzadorId) ? Idioma.T("TITAN\n<size=24><color=#94A3B8>Look inspired by NASA’s SLS. ")
                                                                   : Idioma.T("EXPLORER\n<size=24><color=#94A3B8>Like the Soyuz-Fregat. "))
                          + Idioma.T("Sends ") + TextosBriefing.Numero(lanzador.masaInyectadaKg) + Idioma.T(" kg toward Mars.</color></size>");
                    break;
                case ConfiguracionUI.Pestana.Satelite:
                    textoInstruccion.text = n + Idioma.T(" · CHOOSE THE SATELLITE");
                    textoLeyenda.text = Idioma.T("YOUR SATELLITE\n<size=24><color=#94A3B8>Inspired by real NASA data. Green arrows help, red arrows cost: see the list on the left.</color></size>");
                    break;
                default:
                    textoInstruccion.text = n + Idioma.T(" · INSTALL THE INSTRUMENTS");
                    textoLeyenda.text = Idioma.T("SPACECRAFT VIEW\n<size=24><color=#94A3B8>When you choose an instrument you will see where it is installed.</color></size>");
                    break;
            }
        }

        static string NombrePestana(ConfiguracionUI.Pestana p) =>
            p == ConfiguracionUI.Pestana.Lanzador ? Idioma.T("Launcher") : p == ConfiguracionUI.Pestana.Satelite ? Idioma.T("Satellite") :
            p == ConfiguracionUI.Pestana.Instrumentos ? Idioma.T("Instruments") : Idioma.T("Orbit");

        static string Pestana(string icono, string nombre, bool lista) =>
            icono + " " + nombre + (lista ? " <color=" + Verde + ">" + Iconos.Verde + "</color>" : "");

        void Teclas()
        {
            var k = Keyboard.current;
            if (k == null) return;
            var v = Visibles;
            int tecla = k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame ? 0
                : k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame ? 1
                : k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame ? 2
                : k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame ? 3 : -1;
            if (tecla >= 0 && tecla < v.Count) configuracion.MostrarPestana(v[tecla]);
        }
    }
}
