using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Vista Configuración, en el orden de la versión 6: Lanzador, Satélite, Instrumentos y Órbita; ayuda, barras,
    /// semáforo y nave. El satélite se paga en la Carga, pero su masa cuenta desde aquí (versión 6).
    /// Las órbitas a las que no llega la nave (Alcance) se ven bloqueadas y no se pueden elegir; una ya elegida que
    /// deja de alcanzarse se conserva y pone el semáforo en rojo (como los instrumentos al cambiar de lanzador).
    /// La selección vive en EstadoJuego.Configuracion; cambiar de pestaña o de lanzador no borra nada.
    /// </summary>
    public class ConfiguracionUI : MonoBehaviour
    {
        // Satélite va al final del enum para no cambiar los valores de las demás; en pantalla va antes de Instrumentos.
        public enum Pestana { Orbita, Lanzador, Instrumentos, Satelite }

        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] MisionUI mision;
        [SerializeField] NaveUI nave;

        [Header("Pestañas y opciones")]
        [SerializeField] UnityEngine.UI.Button pestanaOrbita;
        [SerializeField] UnityEngine.UI.Button pestanaLanzador;
        [SerializeField] UnityEngine.UI.Button pestanaInstrumentos;
        [SerializeField] UnityEngine.UI.Button pestanaSatelite;
        [SerializeField] Transform listaOpciones;
        [SerializeField] UnityEngine.UI.Button plantillaOpcion;
        [SerializeField] TMP_Text textoAyuda;

        [Header("Barras: masa, potencia, costo, espacios y frenado")]
        [SerializeField] RectTransform[] rellenosBarra;
        [SerializeField] TMP_Text[] textosBarra;

        [Header("Semáforo")]
        [SerializeField] UnityEngine.UI.Image luzEstado;
        [SerializeField] TMP_Text textoEstado;
        [SerializeField] TMP_Text textoMensaje;
        [SerializeField] UnityEngine.UI.Button botonRevisar;
        [SerializeField] UnityEngine.UI.Button botonBriefing;

        static readonly Color ColorElegida = new Color32(0x5A, 0x26, 0x14, 0xFF);
        static readonly Color ColorNormal = new Color32(0x2A, 0x2A, 0x31, 0xFF);
        static readonly Color ColorBloqueada = new Color32(0x1C, 0x1C, 0x22, 0xFF);       // órbita fuera de alcance
        static readonly Color ColorElegidaBloqueada = new Color32(0x4A, 0x16, 0x16, 0xFF); // elegida y ya no se alcanza
        static readonly Color ColorPestanaActiva = new Color32(0xE2, 0x64, 0x3A, 0xFF);
        static readonly Color ColorExcedida = new Color32(0xEF, 0x44, 0x44, 0xFF);
        static readonly Color ColorBarra = new Color32(0x5E, 0xC8, 0xE5, 0xFF); // cian: los datos van en cian

        readonly List<GameObject> opciones = new List<GameObject>();
        bool inicializada;

        public Pestana PestanaActual { get; private set; }
        public Evaluacion Ultima { get; private set; }
        public UnityEngine.UI.Button BotonRevisar => botonRevisar;
        public CatalogoDatos Catalogo => catalogo;
        public IReadOnlyList<TMP_Text> TextosBarra => textosBarra;
        public UnityEngine.UI.Image LuzEstado => luzEstado;

        Configuracion Config => EstadoJuego.Configuracion;

        void Awake() => Inicializar();

        /// <summary>Conecta los botones una sola vez. Público para las pruebas en modo edición.</summary>
        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            plantillaOpcion.gameObject.SetActive(false);
            pestanaOrbita.onClick.AddListener(() => MostrarPestana(Pestana.Orbita));
            pestanaLanzador.onClick.AddListener(() => MostrarPestana(Pestana.Lanzador));
            pestanaInstrumentos.onClick.AddListener(() => MostrarPestana(Pestana.Instrumentos));
            if (pestanaSatelite != null) pestanaSatelite.onClick.AddListener(() => MostrarPestana(Pestana.Satelite));
            botonRevisar.onClick.AddListener(() => mision.MostrarRevision(Ultima));
            botonBriefing.onClick.AddListener(() => mision.MostrarBriefing());
            MostrarPestana(Pestana.Lanzador);   // versión 6: primero el lanzador; la órbita, al final
        }

        void OnEnable()
        {
            if (inicializada) Recalcular();
        }

        public void MostrarPestana(Pestana pestana)
        {
            PestanaActual = pestana;
            PintarPestana(pestanaOrbita, pestana == Pestana.Orbita);
            PintarPestana(pestanaLanzador, pestana == Pestana.Lanzador);
            PintarPestana(pestanaInstrumentos, pestana == Pestana.Instrumentos);
            if (pestanaSatelite != null) PintarPestana(pestanaSatelite, pestana == Pestana.Satelite);
            textoAyuda.text = Idioma.T("Choose an option to see its description.");
            DibujarOpciones(); // cambiar de pestaña no cambia la configuración: no hace falta recalcular
        }

        // ---------- Selección ----------

        /// <summary>
        /// Elige la órbita si la nave llega a ella. Si no llega, no la elige: explica cuánto frenado falta y qué quitar.
        /// </summary>
        public bool ElegirOrbita(string id)
        {
            var orbita = catalogo.Orbita(id);
            var alcance = Alcance.Evaluar(Config, orbita, catalogo, Nivel);
            textoAyuda.text = PresentacionMision.Ayuda(orbita, alcance);
            if (!alcance.Alcanza && Config.OrbitaId != id)
            {
                ReproductorUI.Sonar(ArteDatos.Sonido.Error);
                return false;
            }
            Config.OrbitaId = id;
            Cambio();
            return true;
        }

        MisionDatos Nivel => catalogo.Nivel(EstadoJuego.NivelIndice);

        public void ElegirLanzador(string id)
        {
            Config.LanzadorId = id; // no borra instrumentos (diseño)
            textoAyuda.text = PresentacionMision.Ayuda(catalogo.Lanzador(id));
            Cambio();
        }

        /// <summary>Elige el satélite. Versión 6: su masa cuenta para la masa y el alcance, así que recalcula.</summary>
        public void ElegirSatelite(string id)
        {
            PresentacionMision.catalogoActual = catalogo;
            Config.SateliteId = id;
            var s = catalogo.Satelite(id);
            if (s != null) textoAyuda.text = PresentacionMision.Ayuda(s);
            Cambio();
        }

        public void AlternarInstrumento(string id)
        {
            if (Config.InstrumentoIds.Contains(id)) Config.QuitarInstrumento(id);
            else
            {
                Config.AgregarInstrumento(id);
                ReproductorUI.Sonar(ArteDatos.Sonido.Elegir, 0.8f);   // un solo sonido de montaje: tapa al clic
            }
            textoAyuda.text = PresentacionMision.Ayuda(catalogo.Instrumento(id));
            Cambio();
        }

        void Cambio()
        {
            DibujarOpciones();
            Recalcular();
        }

        // ---------- Dibujo ----------

        void DibujarOpciones()
        {
            foreach (var o in opciones) DestroyImmediate(o);
            opciones.Clear();

            switch (PestanaActual)
            {
                case Pestana.Orbita:
                    foreach (var o in catalogo.orbitas)
                    {
                        var alcance = Alcance.Evaluar(Config, o, catalogo, Nivel);
                        CrearOpcion(PresentacionMision.TextoOpcion(o, alcance), Config.OrbitaId == o.id, () => ElegirOrbita(o.id), !alcance.Alcanza);
                    }
                    CrearNota(PresentacionMision.NotaAlcance(Config, catalogo, Nivel));
                    break;
                case Pestana.Lanzador:
                    foreach (var l in catalogo.lanzadores)
                        CrearOpcion(PresentacionMision.TextoOpcion(l), Config.LanzadorId == l.id, () => ElegirLanzador(l.id));
                    break;
                case Pestana.Satelite:
                    foreach (var s in catalogo.satelites)
                        CrearOpcion(PresentacionMision.TextoOpcion(s), Config.SateliteId == s.id, () => ElegirSatelite(s.id));
                    break;
                default:
                    foreach (var i in catalogo.instrumentos)
                        CrearOpcion(PresentacionMision.TextoOpcion(i), Config.InstrumentoIds.Contains(i.id), () => AlternarInstrumento(i.id));
                    break;
            }
        }

        /// <summary>El satélite que se ve: el elegido o, si no hay, el primero del catálogo.</summary>
        public string SateliteVisible =>
            !string.IsNullOrEmpty(Config.SateliteId) ? Config.SateliteId : catalogo.satelites.Count > 0 ? catalogo.satelites[0].id : null;

        /// <param name="bloqueada">Órbita fuera de alcance: se ve apagada y con candado; al pulsarla se explica por qué.</param>
        void CrearOpcion(string texto, bool elegida, UnityEngine.Events.UnityAction alPulsar, bool bloqueada = false)
        {
            var boton = Instantiate(plantillaOpcion, listaOpciones);
            boton.gameObject.SetActive(true);
            boton.GetComponent<UnityEngine.UI.Image>().color =
                bloqueada ? (elegida ? ColorElegidaBloqueada : ColorBloqueada) : elegida ? ColorElegida : ColorNormal;
            // Casilla de la tarjeta: marcada y en color de acento si está elegida; aviso rojo si está fuera de alcance.
            var casilla = bloqueada ? "<color=#EF4444>" + Iconos.Rojo
                : elegida ? "<color=#E2643A>" + Iconos.Elegido : "<color=#64748B>" + Iconos.SinElegir;
            boton.GetComponentInChildren<TMP_Text>().text = casilla + "</color>" + texto;
            boton.onClick.AddListener(alPulsar);
            opciones.Add(boton.gameObject);
        }

        /// <summary>Texto que no se elige (la explicación del alcance), con la misma plantilla y más alto.</summary>
        void CrearNota(string texto)
        {
            var nota = Instantiate(plantillaOpcion, listaOpciones);
            nota.gameObject.name = "NotaAlcance";
            nota.gameObject.SetActive(true);
            nota.interactable = false;
            nota.GetComponent<UnityEngine.UI.Image>().color = new Color32(0x16, 0x1B, 0x26, 0xFF);
            var t = nota.GetComponentInChildren<TMP_Text>();
            t.text = "<size=85%>" + texto + "</size>";
            t.enableAutoSizing = true;
            t.fontSizeMin = 12;
            t.fontSizeMax = t.fontSize;
            var layout = nota.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layout != null) { layout.preferredHeight = 200; layout.minHeight = 160; }
            opciones.Add(nota.gameObject);
        }

        static void PintarPestana(UnityEngine.UI.Button boton, bool activa)
        {
            boton.GetComponent<UnityEngine.UI.Image>().color = activa ? ColorPestanaActiva : ColorNormal;
            boton.GetComponentInChildren<TMP_Text>().color = activa ? new Color32(0x0B, 0x0B, 0x10, 0xFF) : Color.white;
        }

        /// <summary>Calcula, evalúa (escribe la línea [Misión]) y actualiza barras, semáforo y nave.</summary>
        public Evaluacion Recalcular()
        {
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            Ultima = Evaluador.Evaluar(Config, catalogo, nivel);

            var barras = PresentacionMision.Barras(Ultima, Config, catalogo, nivel);
            for (int i = 0; i < barras.Length && i < textosBarra.Length; i++)
            {
                textosBarra[i].text = barras[i].Nombre + ": " + barras[i].Texto;
                rellenosBarra[i].anchorMax = new Vector2(barras[i].Fraccion, 1f);
                rellenosBarra[i].GetComponent<UnityEngine.UI.Image>().color = barras[i].Excedida ? ColorExcedida : ColorBarra;
            }

            luzEstado.color = PresentacionMision.ColorSemaforo(Ultima.Estado);
            textoEstado.text = PresentacionMision.NombreEstado(Ultima.Estado);
            textoMensaje.text = Ultima.Mensaje;
            if (nave != null) nave.Mostrar(Config);
            return Ultima;
        }
    }
}
