using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 5, bloque 4: Misión 0, un prólogo guiado que siempre termina en fracaso (paquete v1.0, sección 8).
    /// Enseña a elegir y encender un instrumento, marcar la región, resolver el mes, leer el resumen y programar el
    /// envío. Al intentar enviar, una tormenta global como la de 2018 corta el contacto: el fracaso no depende de lo
    /// que haga el jugador. Se puede saltar y repetir; no guarda récords ni gasta créditos; recuerda que se completó.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [SerializeField] MisionUI mision;
        [SerializeField] OperacionUI operacion;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text textoTitulo;
        [SerializeField] TMP_Text textoCuerpo;
        [SerializeField] UnityEngine.UI.Button botonSiguiente;
        [SerializeField] UnityEngine.UI.Button botonSaltar;

        public const string ClaveCompletado = "tutorial_completado";
        /// <summary>Dónde está la anomalía en la Misión 0 (arriba del mapa, lejos de la tarjeta).</summary>
        public static readonly Vector2 Anomalia = new Vector2(0.62f, 0.68f);
        const int Semilla = 2018;
        static readonly Color ColorPunto = new Color32(0xA8, 0x55, 0xF7, 0xFF);

        public enum Paso { Inactivo, Bienvenida, Encender, Region, Resolver, Resumen, Enviar, ResolverEnvio, Final }
        public Paso Actual { get; private set; } = Paso.Inactivo;

        public static bool Completado
        {
            get { try { return PlayerPrefs.GetInt(ClaveCompletado, 0) == 1; } catch { return false; } }
        }

        /// <summary>La nave de la primera misión: siempre la misma (Explorador, órbita media, térmica y radar, MRO).</summary>
        public static Configuracion ConfiguracionFija() =>
            new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = Carga.IdMro };

        bool inicializado;

        void Awake() => Inicializar();

        void Inicializar()
        {
            if (inicializado) return;
            inicializado = true;
            botonSiguiente.onClick.AddListener(Siguiente);
            botonSaltar.onClick.AddListener(Terminar);
            panel.SetActive(false);
        }

        public void Comenzar()
        {
            Inicializar();
            var op = operacion.Op;
            op.ProgramarEventos(new EventoDatos[0], new int[0]);   // en la Misión 0 no hay eventos al azar
            op.PonerAnomalia(Anomalia);
            op.Objetivos.Clear();
            operacion.GuiaForzada = GuiaDelPaso;
            operacion.AntesDeResolver = AntesDeResolver;
            operacion.TerminarForzado = Terminar;   // «End mission» en el tutorial = saltarlo
            Ir(Paso.Bienvenida);
        }

        void Update()
        {
            if (Actual == Paso.Inactivo || operacion.Op == null) return;
            Comprobar();
        }

        /// <summary>Avanza cuando el jugador hizo lo que pide el paso. Las pruebas lo llaman sin esperar a Update.</summary>
        public void Comprobar()
        {
            var op = operacion.Op;
            switch (Actual)
            {
                case Paso.Encender:
                    if (op.Encendidos.Contains("termica")) Ir(Paso.Region);
                    break;
                case Paso.Region:
                    if (op.Regiones.TryGetValue("termica", out var p) && Vector2.Distance(p, Anomalia) <= op.RadioDeteccion)
                        Ir(Paso.Resolver);
                    break;
                case Paso.Resolver:
                    if (op.Mes == 2) Ir(Paso.Resumen);
                    break;
                case Paso.Resumen:
                    if (!operacion.ResumenVisible) Ir(Paso.Enviar);
                    break;
                case Paso.Enviar:
                    if (op.TransmitirPlaneado) Ir(Paso.ResolverEnvio);
                    break;
            }
        }

        /// <summary>El botón de la tarjeta: empezar (bienvenida) o diseñar la siguiente misión (final).</summary>
        public void Siguiente()
        {
            if (Actual == Paso.Bienvenida) Ir(Paso.Encender);
            else if (Actual == Paso.Final) Terminar();
        }

        /// <summary>Al pulsar «Resolver mes» con el envío programado llega la tormenta: el mes no se resuelve.</summary>
        bool AntesDeResolver()
        {
            if (Actual == Paso.Resolver) return true;   // el único mes que se resuelve de verdad
            if (Actual == Paso.ResolverEnvio) Ir(Paso.Final);
            return false;                               // en los demás pasos todavía no toca
        }

        /// <summary>Fin del tutorial (completado o saltado): no hay penalización y se vuelve al Briefing.</summary>
        public void Terminar()
        {
            try { PlayerPrefs.SetInt(ClaveCompletado, 1); PlayerPrefs.Save(); } catch { }
            operacion.GuiaForzada = null;
            operacion.AntesDeResolver = null;
            operacion.TerminarForzado = null;
            Actual = Paso.Inactivo;
            panel.SetActive(false);
            EstadoJuego.Reiniciar();
            mision.MostrarBriefing();
        }

        void Ir(Paso paso)
        {
            Actual = paso;
            var (titulo, cuerpo, boton) = Textos(paso);
            // En el resumen la tarjeta taparía su botón «Continue»: la guía de arriba basta.
            panel.SetActive(paso != Paso.Resumen);
            textoTitulo.text = titulo;
            textoCuerpo.text = cuerpo;
            botonSiguiente.gameObject.SetActive(boton != null);
            if (boton != null) botonSiguiente.GetComponentInChildren<TMP_Text>().text = boton;
            botonSaltar.gameObject.SetActive(paso != Paso.Final);
            if (paso == Paso.Region) operacion.MarcarEnMapa(Anomalia, ColorPunto);
            operacion.Redibujar();
            if (paso == Paso.Region) operacion.MarcarEnMapa(Anomalia, ColorPunto);   // Redibujar limpia el mapa
        }

        PasoGuia? GuiaDelPaso()
        {
            switch (Actual)
            {
                case Paso.Encender: return Guia(ObjetivoGuia.Instrumento, Idioma.T("Click the Infrared thermal camera to select it and turn it on."), "termica");
                case Paso.Region: return Guia(ObjetivoGuia.Mapa, Idioma.T("Click the purple spot on the map: that is where the signal comes from."), "termica");
                case Paso.Resolver: return Guia(ObjetivoGuia.Resolver, Idioma.T("Press \u00abResolve month 1\u00bb: the camera observes for 30 days."));
                case Paso.Enviar: return Guia(ObjetivoGuia.Enviar, Idioma.T("Turn on \u00abSend at month end\u00bb: evidence only counts when it reaches Earth."));
                case Paso.ResolverEnvio: return Guia(ObjetivoGuia.Resolver, Idioma.T("Press \u00abResolve month 2\u00bb to send the data."));
                case Paso.Final: return Guia(ObjetivoGuia.Ninguno, Idioma.T("Contact lost."));
                case Paso.Resumen: return Guia(ObjetivoGuia.Ninguno, Idioma.T("Read the month summary, then press \u00abContinue\u00bb."));
                default: return Guia(ObjetivoGuia.Ninguno, Idioma.T("Read the card below."));
            }
        }

        static PasoGuia Guia(ObjetivoGuia o, string texto, string instrumento = null) =>
            new PasoGuia { Objetivo = o, Texto = texto, InstrumentoId = instrumento };

        static (string, string, string) Textos(Paso paso)
        {
            switch (paso)
            {
                case Paso.Bienvenida:
                    return (Idioma.T("MISSION 0 \u00b7 THE FIRST SIGNAL"),
                        Idioma.T("Before your mission, an older orbiter was the first to see a strange thermal signal on Mars. ") +
                        Idioma.T("Fly it for two months and learn the controls. You can skip this at any time."), Idioma.T("Start"));
                case Paso.Encender:
                    return (Idioma.T("1 \u00b7 INSTRUMENTS"),
                        Idioma.T("Each instrument looks for one kind of evidence. Turning one on uses power (left bar) but does not observe yet."), null);
                case Paso.Region:
                    return (Idioma.T("2 \u00b7 WHERE TO LOOK"),
                        Idioma.T("Each instrument observes the region you mark. Close to the anomaly you get a hot reading; on top of it, evidence."), null);
                case Paso.Resolver:
                    return (Idioma.T("3 \u00b7 ONE MONTH AT A TIME"),
                        Idioma.T("Planning costs nothing. Time only passes when you resolve the month, and everything happens at once."), null);
                case Paso.Resumen:
                    return (Idioma.T("4 \u00b7 THE MONTH SUMMARY"),
                        Idioma.T("It tells you what you decided, what happened and what you spent. The camera found the anomaly: the data is in memory, not on Earth yet."), null);
                case Paso.Enviar:
                    return (Idioma.T("5 \u00b7 SENDING TO EARTH"),
                        Idioma.T("There is contact with Earth only in some months. This one has it: schedule the transmission."), null);
                case Paso.ResolverEnvio:
                    return (Idioma.T("6 \u00b7 SEND"), Idioma.T("The data leaves at the end of the month."), null);
                default:
                    return (Idioma.T("CONTACT LOST"),
                        Idioma.T("A global dust storm, like the one in 2018, darkens the solar panels and the orbiter falls silent before sending its data.\n\n") +
                        Idioma.T("<b>The first mission detected the signal, but could not send enough evidence. Now we know what went wrong. Design the next mission.</b>"),
                        Idioma.T("Design the next mission"));
            }
        }
    }
}
