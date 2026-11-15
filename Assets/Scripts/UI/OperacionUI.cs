using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Vista Operación por meses (versión 4). El jugador prepara el plan del mes (instrumentos encendidos, región
    /// de cada uno en el mapa, maniobra y envío), ve la previsión y pulsa «Resolver mes». Después lee el resumen
    /// y, si hubo un evento, decide. Toda la lógica está en Operacion: aquí solo se dibuja lo que devuelve.
    /// </summary>
    public class OperacionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] MapaMarteUI mapa;

        [Header("Instrumentos")]
        [SerializeField] Transform listaInstrumentos;
        [SerializeField] UnityEngine.UI.Button plantillaInstrumento;
        [SerializeField] RectTransform rellenoEnergia;
        [SerializeField] TMP_Text textoEnergia;
        [SerializeField] TMP_Text textoOrbitas;   // «Mes N de T»

        [Header("Mensajes")]
        [SerializeField] TMP_Text textoLectura;   // previsión del mes y último aviso
        [SerializeField] TMP_Text textoEvento;
        [SerializeField] TMP_Text textoTransmision;
        [SerializeField] TMP_Text textoProgreso;

        [Header("Herramientas y fin")]
        [SerializeField] Transform listaHerramientas;
        [SerializeField] UnityEngine.UI.Button plantillaHerramienta;
        [SerializeField] UnityEngine.UI.Button botonTerminar;

        [Header("Plan del mes")]
        [SerializeField] TMP_Text textoCombustible;
        [SerializeField] UnityEngine.UI.Button botonAltitud;   // planear o cancelar la maniobra
        [SerializeField] TMP_Text textoMemoria;
        [SerializeField] UnityEngine.UI.Button botonEnviar;    // enviar al final del mes: sí o no
        [SerializeField] UnityEngine.UI.Button botonResolver;
        [SerializeField] TMP_Text textoObjetivos;
        [SerializeField] TMP_Text textoModificadores;   // versión 5: buffs y debuffs activos
        [SerializeField] TMP_Text textoGuia;            // versión 5: el siguiente paso (Guia)

        [Header("Resumen del mes y decisión del evento")]
        [SerializeField] GameObject panelResumen;
        [SerializeField] TMP_Text textoResumen;
        [SerializeField] UnityEngine.UI.Button botonContinuar;
        [SerializeField] GameObject panelDecision;
        [SerializeField] TMP_Text textoDecision;
        [SerializeField] Transform listaOpciones;
        [SerializeField] UnityEngine.UI.Button plantillaOpcion;

        static readonly Color ColorNormal = new Color32(0x2A, 0x2A, 0x31, 0xFF);
        static readonly Color ColorEncendido = new Color32(0x14, 0x53, 0x2D, 0xFF);
        static readonly Color ColorElegido = new Color32(0x5A, 0x26, 0x14, 0xFF);
        static readonly Color ColorBloqueado = new Color32(0x3F, 0x3F, 0x46, 0xFF);
        static readonly Color ColorRegion = new Color32(0xF3, 0xF4, 0xF7, 0xFF);
        static readonly Color ColorPeticion = new Color32(0xA8, 0x55, 0xF7, 0xFF);

        readonly List<GameObject> filas = new List<GameObject>();
        readonly List<Registro> historial = new List<Registro>();   // observaciones ya resueltas, para marcarlas en el mapa
        bool inicializada;
        bool resumenPendiente;
        ResaltarGuia resaltado;

        /// <summary>Tutorial: si devuelve un paso, la guía lo usa en vez de calcular el suyo.</summary>
        public System.Func<PasoGuia?> GuiaForzada;
        /// <summary>Tutorial: se llama al pulsar «Resolver mes»; si devuelve false, el mes no se resuelve.</summary>
        public System.Func<bool> AntesDeResolver;
        /// <summary>Tutorial: «Terminar misión» lo cierra en vez de ir al informe.</summary>
        public System.Action TerminarForzado;

        /// <summary>El último paso que propuso la guía (para las pruebas).</summary>
        public PasoGuia PasoActual { get; private set; }
        Evaluacion evaluacion;
        string aviso = "";

        // Presentación (cinemática y vista 3D): el resumen y la decisión esperan a que terminen.
        SecuenciaEscaneo secuenciaEscaneo;
        DirectorCinematica director;
        TransicionMes transicionMes;

        /// <summary>
        /// Tras resolver un mes con observaciones: instrumento, región, lectura y si aportó evidencia nueva, de la
        /// observación más relevante. Solo lo usa la vista 3D (SecuenciaEscaneo); la lógica ya terminó cuando se avisa.
        /// </summary>
        public event System.Action<string, Vector2, Lectura, bool> AlEscanear;

        /// <summary>
        /// Tras resolver un mes válido (la lógica ya terminó y el resumen ya está pendiente). Solo lo usa la
        /// transición «pasa un mes» (TransicionMes); no cambia ninguna regla.
        /// </summary>
        public event System.Action<ResultadoMes> AlResolverMes;

        public Operacion Op { get; private set; }
        /// <summary>Instrumento elegido: el clic en el mapa fija su región.</summary>
        public string Escaner { get; private set; }
        public ResultadoMes UltimoResultado { get; private set; }
        public string UltimoMensaje => textoLectura.text;
        public bool ResumenVisible => resumenPendiente;

        /// <summary>Hay algo a pantalla completa (resumen, decisión, transición de mes o vista del escaneo).</summary>
        public bool Ocupada => Op != null && (resumenPendiente || Op.EsperandoDecision ||
            (secuenciaEscaneo != null && secuenciaEscaneo.Activa) || (transicionMes != null && transicionMes.Activa));

        ReglasOperacionDatos Reglas => catalogo.reglas;

        void Update() => ActualizarPaneles();

        /// <summary>El resumen y luego la decisión se ven cuando no se está viendo una animación.</summary>
        void ActualizarPaneles()
        {
            if (Op == null) return;
            bool ocupada = (secuenciaEscaneo != null && secuenciaEscaneo.Activa) || (director != null && director.Reproduciendo) ||
                           (transicionMes != null && transicionMes.Activa);
            Ver(panelResumen, resumenPendiente && !ocupada);
            Ver(panelDecision, Op.EsperandoDecision && !resumenPendiente && !ocupada);
        }

        static void Ver(GameObject panel, bool visible)
        {
            if (panel != null && panel.activeSelf != visible) panel.SetActive(visible);
        }

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            plantillaInstrumento.gameObject.SetActive(false);
            plantillaHerramienta.gameObject.SetActive(false);
            if (plantillaOpcion != null) plantillaOpcion.gameObject.SetActive(false);
            Ver(panelDecision, false);
            Ver(panelResumen, false);
            mapa.AlPulsar += FijarRegionEn;
            botonTerminar.onClick.AddListener(Terminar);
            botonAltitud.onClick.AddListener(AlternarManiobra);
            botonEnviar.onClick.AddListener(AlternarEnvio);
            botonResolver.onClick.AddListener(ResolverMes);
            botonContinuar.onClick.AddListener(ContinuarResumen);
            secuenciaEscaneo = FindFirstObjectByType<SecuenciaEscaneo>(FindObjectsInactive.Include);
            director = FindFirstObjectByType<DirectorCinematica>(FindObjectsInactive.Include);
            transicionMes = FindFirstObjectByType<TransicionMes>(FindObjectsInactive.Include);
        }

        /// <summary>Empieza la Operación de una misión lanzada con lo que se cargó. La semilla fija solo la usan las pruebas.</summary>
        public void Comenzar(Evaluacion e, Carga carga, int? semilla = null)
        {
            Inicializar();
            if (e == null || !e.SePuedeLanzar) return;
            int s = semilla ?? System.Environment.TickCount;
            evaluacion = e;
            Op = new Operacion(e, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice), s, carga);
            EstadoJuego.Operacion = Op;
            EstadoJuego.CategoriaFinal = null;
            historial.Clear();
            resumenPendiente = false;
            UltimoResultado = null;
            mapa.Generar(s);
            Escaner = Op.Instrumentos.FirstOrDefault();
            aviso = Idioma.T("Choose an instrument, turn it on and click the region on the map it will observe. ") +
                    Idioma.T("When the plan is ready, press “Resolve month”. Preparing the plan costs nothing.");
            textoEvento.text = Idioma.T("No incidents.");
            MostrarTransmision(Op.Recibidas.Last());
            Refrescar();
        }

        // ---------- Plan del mes (nada de esto gasta recursos) ----------

        /// <summary>Clic en un instrumento: lo elige; si ya estaba elegido (o estaba apagado), lo enciende o apaga.</summary>
        public void AlternarInstrumento(string id)
        {
            // Con resumen o decisión pendientes (aunque estén ocultos tras la animación) no se cambia el plan:
            // si no, apagar todo antes de aguantar un evento esquivaría su efecto.
            if (Op == null || Op.Terminada || Op.EsperandoDecision || resumenPendiente) return;
            Escaner = id;
            var datos = catalogo.Instrumento(id);
            if (Op.Encendidos.Contains(id))
            {
                Op.Apagar(id);
                aviso = string.Format(Idioma.T("{0} OFF: frees {1} W; it will not observe this month."), Nombre(id), datos.ConsumoTotalW);
            }
            else if (!Op.Encender(id))
                aviso = Op.SinAlcance(id)
                    ? Nombre(id) + Idioma.T(" cannot work from this orbit: it gives no evidence here.")
                    : Op.Bloqueados.ContainsKey(id)
                    ? Nombre(id) + Idioma.T(" is locked out by overheating.")
                    : Idioma.T("Not enough power to turn on ") + Nombre(id) + Idioma.T(": turn off another instrument.");
            else
                aviso = string.Format(Idioma.T("{0} ON: uses {1} W and {2} memory block(s) per month."), Nombre(id), datos.ConsumoTotalW, datos.bloquesDatos) +
                        (Op.Regiones.ContainsKey(id) ? "" : Idioma.T(" Now click the region on the map."));
            Refrescar();
        }

        public void ElegirEscaner(string id)
        {
            Escaner = id;
            Refrescar();
        }

        /// <summary>Clic en el mapa: fija la región del instrumento elegido. No observa nada todavía.</summary>
        public void FijarRegionEn(Vector2 punto)
        {
            if (Op == null || Op.Terminada || Op.EsperandoDecision || resumenPendiente || Escaner == null) return;
            Op.FijarRegion(Escaner, punto);
            aviso = Idioma.T("Region set for ") + Nombre(Escaner) + "." +
                    (Op.Encendidos.Contains(Escaner) ? Idioma.T(" It will observe there when you resolve the month.") : Idioma.T(" Turn it on so it observes this month."));
            Refrescar();
        }

        public void AlternarManiobra()
        {
            if (Op == null) return;
            if (Op.ManiobraPlaneada.HasValue) { Op.CancelarManiobra(); aviso = Idioma.T("Maneuver cancelled: nothing was charged."); }
            else if (Op.PlanearManiobra(Op.Altitud == Altitud.Normal ? Altitud.Baja : Altitud.Normal))
                aviso = string.Format(Idioma.T("Maneuver planned: {0} cr, {1} fuel and {2} fewer days of observation this month."),
                            Reglas.creditosPorManiobra, Op.CostoCambioAltitud, Reglas.diasPorManiobra) +
                        (Op.ManiobraPlaneada == Altitud.Baja
                            ? string.Format(Idioma.T(" In low orbit quality improves (×{0}), but drag uses {1} fuel every month."), Reglas.factorCalidadBaja, Reglas.combustiblePorOrbitaBaja)
                            : "");
            else
                aviso = Idioma.T("Cannot maneuver: you need ") + Op.CostoCambioAltitud + Idioma.T(" fuel and ") +
                        Reglas.creditosPorManiobra + Idioma.T(" credits. Nothing was charged.");
            Refrescar();
        }

        public void AlternarEnvio()
        {
            if (Op == null) return;
            Op.TransmitirPlaneado = !Op.TransmitirPlaneado;
            aviso = !Op.TransmitirPlaneado ? Idioma.T("Will not send: the data stays in memory.")
                : Op.MotivoSinEnvio == null ? Idioma.T("Will send at month end: data only counts once it reaches Earth.")
                : string.Format(Idioma.T("Cannot send this month: {0}."), Op.MotivoSinEnvio);
            Refrescar();
        }

        // ---------- Resolver ----------

        /// <summary>Resuelve el mes en curso una sola vez y muestra el resumen.</summary>
        public void ResolverMes()
        {
            if (Op == null || resumenPendiente) return;
            if (AntesDeResolver != null && !AntesDeResolver()) return;
            int recibidas = Op.Recibidas.Count;
            var r = Op.ResolverMes(Op.Mes);
            if (!r.Valido)
            {
                aviso = Idioma.T("<color=#F4C46A><b>Cannot resolve:</b></color> ") + r.Motivo + ".";
                Refrescar();
                return;
            }
            UltimoResultado = r;
            historial.AddRange(r.Observaciones);
            resumenPendiente = true;
            textoResumen.text = TextoResumen(r);
            aviso = "";
            if (Op.Recibidas.Count > recibidas) MostrarTransmision(Op.Recibidas.Last());
            if (r.Evento != null) MostrarEvento(r.Evento);
            AlResolverMes?.Invoke(r);
            Refrescar();

            var destacada = r.Observaciones.OrderByDescending(o => o.Evidencias.Count)
                .ThenByDescending(o => o.Lectura == Lectura.Encontrada).FirstOrDefault();
            if (destacada != null)
                AlEscanear?.Invoke(destacada.Instrumento, destacada.Region, destacada.Lectura, destacada.Evidencias.Count > 0);
        }

        /// <summary>Cierra el resumen del mes. Si hay un evento, después aparece su decisión.</summary>
        public void ContinuarResumen()
        {
            resumenPendiente = false;
            ActualizarPaneles();
            Refrescar();
        }

        /// <summary>Responde al evento en pausa con la opción número «indice» de las que se muestran.</summary>
        public void Decidir(int indice)
        {
            if (Op == null || !Op.EsperandoDecision) return;
            var opcion = Op.Opciones.ElementAtOrDefault(indice);
            var evento = Op.Decision;
            int apagones = Op.Apagones;
            if (opcion == null || !Op.Decidir(opcion)) return;
            textoEvento.text = "<b><color=#F4C46A>" + evento.Datos.Nombre + Idioma.T("</color></b>\nYou decided: ") + opcion.Texto + ".";
            if (Op.PeticionAceptada && opcion.efecto == EfectoOpcion.AceptarObjetivo)
                aviso = Idioma.T("NASA marked a purple point on the map: observe it with any instrument to fulfill the request.");
            if (Op.Apagones > apagones) aviso = MensajeApagon();
            Refrescar();
        }

        public void UsarHerramienta(string id)
        {
            if (Op == null) return;
            if (Op.UsarHerramienta(id))
                textoEvento.text = "<b>" + catalogo.Herramienta(id).Nombre + Idioma.T("</b>: problem solved.");
            Refrescar();
        }

        public void Terminar()
        {
            if (Op == null) return;
            if (TerminarForzado != null) { TerminarForzado(); return; }
            Op.Terminar();
            EstadoJuego.CategoriaFinal = ResultadoCientifico.Categoria(evaluacion, Op, catalogo);
            if (Application.isPlaying) SceneManager.LoadScene("Resultado");
        }

        // ---------- Textos ----------

        /// <summary>Qué decidí, qué ocurrió, qué gasté y qué sigue.</summary>
        string TextoResumen(ResultadoMes r)
        {
            var decidi = new List<string>();
            if (r.Observaciones.Count + r.SinMemoria.Count + r.SinRegion.Count > 0)
                decidi.Add(Idioma.T("observe with ") + string.Join(", ", r.Observaciones.Select(o => o.Instrumento)
                    .Concat(r.SinMemoria).Concat(r.SinRegion).Select(Nombre)));
            else decidi.Add(Idioma.T("not observe"));
            if (r.Maniobra) decidi.Add(Idioma.T("maneuver to ") + (r.Altitud == Altitud.Baja ? Idioma.T("low") : "normal") + Idioma.T(" orbit"));
            if (r.EnvioPedido) decidi.Add(Idioma.T("send to Earth"));

            var ocurrio = new List<string>();
            foreach (var o in r.Observaciones)
                ocurrio.Add("• " + Nombre(o.Instrumento) + ": " + TextoLectura(o) + (o.Evidencias.Count > 0
                    ? Idioma.T(" <color=#22C55E>Valid evidence, in memory.</color>") : ""));
            foreach (var id in r.SinMemoria) ocurrio.Add("• " + Nombre(id) + Idioma.T(": <color=#F4C46A>no memory</color>, nothing was recorded."));
            foreach (var id in r.SinRegion) ocurrio.Add("• " + Nombre(id) + Idioma.T(": ON with no region, did not observe."));
            if (r.DiasUtiles == 0) ocurrio.Add(Idioma.T("• The month had no useful days: nothing was observed."));
            if (r.Observaciones.Count == 0 && r.SinMemoria.Count == 0 && r.SinRegion.Count == 0 && r.DiasUtiles > 0)
                ocurrio.Add(Idioma.T("• No instrument observed this month."));
            if (r.EnvioPedido)
                ocurrio.Add(r.EnvioFallido != null
                    ? Idioma.T("• <color=#F4C46A>Not sent:</color> ") + r.EnvioFallido + Idioma.T(". The data is still in memory.")
                    : Idioma.T("• Transmission: ") + r.BloquesEnviados + (r.BloquesEnviados == 1 ? Idioma.T(" block") : Idioma.T(" blocks")) + Idioma.T(" to Earth") +
                      (r.Enviadas.Count > 0 ? Idioma.T(", with <color=#22C55E>") + r.Enviadas.Count + (r.Enviadas.Count == 1 ? Idioma.T(" piece") : Idioma.T(" pieces")) + Idioma.T(" of evidence received</color>.") : Idioma.T(", no new evidence.")));
            if (r.SubidaForzada) ocurrio.Add(Idioma.T("• Not enough fuel to counter drag: the spacecraft climbed back to normal orbit on its own."));

            var gaste = new List<string>();
            if (r.CreditosGastados > 0) gaste.Add(r.CreditosGastados + Idioma.T(" credits"));
            if (r.CombustibleGastado > 0) gaste.Add(r.CombustibleGastado + Idioma.T(" fuel"));
            if (r.Maniobra) gaste.Add(Reglas.diasPorManiobra + Idioma.T(" days on the maneuver"));

            string sigue = Op.CanceladaSinEvidencia
                ? string.Format(Idioma.T("<color=#EF4444>Mission Control cancelled the mission: no evidence reached Earth by the end of month {0}.</color> Press “View report”."), Op.MesLimiteSinEvidencia)
                : r.Termino ? Idioma.T("The campaign is over. Press “View report”.")
                : Idioma.T("Month ") + Op.Mes + ". " + (Op.ContactoAbierto ? Idioma.T("Contact with Earth is open.") : Idioma.T("Contact in ") + Meses(Op.MesesParaContacto) + ".") +
                  (r.Evento != null && Op.EsperandoDecision ? Idioma.T(" Mission Control awaits a decision: ") + r.Evento.Datos.Nombre + "." : "") +
                  Idioma.T(" Instruments that are ON stay ON.");

            return Idioma.T("<b>Month ") + r.Mes + Idioma.T(" resolved</b>  ·  ") + r.DiasUtiles + (r.DiasUtiles == 1 ? Idioma.T(" useful day") : Idioma.T(" useful days")) + "\n\n" +
                   Idioma.T("<b>You decided:</b> ") + string.Join(", ", decidi) + ".\n\n" +
                   Idioma.T("<b>What happened:</b>\n") + string.Join("\n", ocurrio) + "\n\n" +
                   (r.Observaciones.Count > 0 ? Idioma.T("<b>Quality:</b> ") + DesgloseCalidad(r) + "\n\n" : "") +
                   Idioma.T("<b>You spent:</b> ") + (gaste.Count > 0 ? string.Join(", ", gaste) : Idioma.T("nothing")) + ".\n\n" +
                   Idioma.T("<b>Next:</b> ") + sigue;
        }

        static string Meses(int n) => n + (n == 1 ? Idioma.T(" month") : Idioma.T(" months"));

        /// <summary>«70 base × 28/30 days ▼ × 0.8 storm ▲ × 1.2 low orbit = 63»: de dónde sale la calidad del mes.</summary>
        string DesgloseCalidad(ResultadoMes r)
        {
            var t = Mathf.RoundToInt(r.CalidadBase) + " base";
            if (r.DiasUtiles < Reglas.diasPorMes)
                t += " <color=" + PresentacionMision.ColorDebuff + ">" + Iconos.Baja + " × " + r.DiasUtiles + "/" + Reglas.diasPorMes + Idioma.T(" days</color>");
            if (r.FactorTormenta != 1f)
                t += " <color=" + PresentacionMision.ColorDebuff + ">" + Iconos.Baja + " × " + r.FactorTormenta.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + Idioma.T(" storm</color>");
            if (r.FactorAltitud != 1f)
                t += " <color=" + PresentacionMision.ColorBuff + ">" + Iconos.Sube + " × " + r.FactorAltitud.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + Idioma.T(" low orbit</color>");
            float q = Mathf.Clamp(r.CalidadBase * r.DiasUtiles / Reglas.diasPorMes * r.FactorTormenta * r.FactorAltitud, 0f, 100f);
            return t + " = <b>" + Mathf.RoundToInt(q) + Idioma.T("</b> (valid evidence needs ") + Mathf.RoundToInt(Reglas.calidadMinima) + ")";
        }

        string TextoLectura(Registro o)
        {
            string calidad = Idioma.T(" (quality ") + Mathf.RoundToInt(o.Calidad) + ")";
            switch (o.Lectura)
            {
                case Lectura.Encontrada:
                    return Idioma.T("<color=#22C55E><b>anomaly observed</b></color>") + calidad + "." +
                           (o.Calidad < Reglas.calidadMinima ? Idioma.T(" <color=#F4C46A>Quality too low: does not count as evidence.</color>") : "");
                case Lectura.Caliente: return Idioma.T("<color=#E2643A><b>hot</b></color>, very close") + calidad + ".";
                case Lectura.Tibio: return Idioma.T("<color=#EAB308><b>warm</b></color>, getting closer") + calidad + ".";
                case Lectura.Frio: return Idioma.T("<color=#60A5FA><b>cold</b></color>, far from the anomaly") + calidad + ".";
                default: return Idioma.T("<b>no reading</b>: the spacecraft was disoriented.");
            }
        }

        string TextoPrevision()
        {
            string motivo = Op.MotivoPlanInvalido;
            int prevista = Op.MemoriaUsada + Op.BloquesPrevistos;
            var t = Idioma.T("<b>Month ") + Op.Mes + Idioma.T(" forecast:</b> ") + Op.DiasPrevistos + (Op.DiasPrevistos == 1 ? Idioma.T(" useful day") : Idioma.T(" useful days")) + Idioma.T("  ·  power draw ") + Op.ConsumoW + " / " + Op.LimiteW + " W" +
                    Idioma.T("  ·  memory ") + prevista + " / " + Op.MemoriaCapacidad + Idioma.T(" blocks") +
                    (prevista > Op.MemoriaCapacidad ? Idioma.T(" <color=#F4C46A>(not everything will fit)</color>") : "");
            if (Op.ManiobraPlaneada.HasValue)
                t += Idioma.T("\nManeuver: ") + Reglas.creditosPorManiobra + " cr, " + Op.CostoCambioAltitud + Idioma.T(" fuel and ") + Reglas.diasPorManiobra + Idioma.T(" days.");
            if (Op.TransmitirPlaneado)
                t += Idioma.T("\nSend at month end: ") + (Op.MotivoSinEnvio == null
                    ? Idioma.T("yes (instruments turn off automatically during the window).")
                    : Idioma.T("<color=#F4C46A>not possible, ") + Op.MotivoSinEnvio + "</color>.");
            if (motivo != null && !Op.Terminada && !Op.EsperandoDecision) t += "\n<color=#F4C46A>" + motivo + ".</color>";
            if (Op.Terminada) t = Idioma.T("The campaign is over. Anything not sent does not count as evidence.");
            return t + (aviso.Length > 0 ? "\n" + aviso : "");
        }

        void MostrarTransmision(string id)
        {
            var t = catalogo.Transmision(id);
            string encabezado = string.IsNullOrEmpty(t.instrumentoId) ? Idioma.T("Start of observation") : Idioma.T("Transmission: ") + Nombre(t.instrumentoId);
            textoTransmision.text = "<b>" + encabezado + "</b>\n" + t.Texto;
        }

        void MostrarEvento(EventoActivo e)
        {
            textoEvento.text = "<b><color=#F4C46A>" + e.Datos.Nombre + "</color></b>\n" + (e.EvitadoPorSatelite
                ? Idioma.T("It did not affect the ") + Op.Satelite.Nombre + Idioma.T(": its pointing is very precise.")
                : Idioma.T("Mission Control awaits your decision."));
        }

        string MensajeApagon() =>
            Idioma.T("<color=#EF4444><b>Safe mode.</b></color> The event pushed power draw above the limit: the spacecraft turned off its instruments, ") +
            Idioma.T("the restart cost ") + Reglas.creditosPorReinicio + Idioma.T(" credits and next month loses ") + Reglas.diasPerdidosPorApagon + Idioma.T(" days.");

        /// <summary>
        /// Versión 9: mientras no haya llegado ninguna evidencia, el plazo del mes límite; ámbar cuando quedan dos
        /// meses o menos y rojo en el último (al cerrarlo sin evidencia, Control de Misión cancela la misión).
        /// </summary>
        string AvisoPlazo(int recibidas)
        {
            int limite = Op.MesLimiteSinEvidencia;
            if (recibidas > 0 || limite <= 0 || Op.Terminada || EstadoJuego.Tutorial || Op.Mes > limite) return "";
            int quedan = limite - Op.Mes;   // meses que quedan después de este
            string color = quedan == 0 ? "#EF4444" : quedan <= 2 ? "#F4C46A" : "#94A3B8";
            return "\n<size=85%><color=" + color + ">" + (quedan == 0
                ? string.Format(Idioma.T("Last chance: send evidence this month or the mission is cancelled at the end of month {0}."), limite)
                : string.Format(Idioma.T("The first evidence must reach Earth by the end of month {0}."), limite)) + "</color></size>";
        }

        static Color ColorLectura(Lectura l)
        {
            switch (l)
            {
                case Lectura.Encontrada: return new Color32(0x22, 0xC5, 0x5E, 0xFF);
                case Lectura.Caliente: return new Color32(0xE2, 0x64, 0x3A, 0xFF);
                case Lectura.Tibio: return new Color32(0xEA, 0xB3, 0x08, 0xFF);
                case Lectura.Frio: return new Color32(0x60, 0xA5, 0xFA, 0xFF);
                default: return new Color32(0x9C, 0xA3, 0xAF, 0xFF);
            }
        }

        string Nombre(string instrumentoId) => catalogo.Instrumento(instrumentoId).Nombre;

        // ---------- Dibujo ----------

        void Refrescar()
        {
            foreach (var f in filas) { if (Application.isPlaying) Destroy(f); else DestroyImmediate(f); }
            filas.Clear();
            bool pausa = Op.Terminada || Op.EsperandoDecision || resumenPendiente;

            foreach (var id in Op.Instrumentos)
            {
                var i = catalogo.Instrumento(id);
                bool encendido = Op.Encendidos.Contains(id);
                bool bloqueado = Op.Bloqueados.TryGetValue(id, out int resta);
                string estado = Op.SinAlcance(id) ? Idioma.T("OUT OF RANGE")
                    : bloqueado ? Idioma.T("LOCKED (") + Meses(resta) + ")"
                    : !encendido ? Idioma.T("OFF")
                    : Op.Regiones.ContainsKey(id) ? Idioma.T("OBSERVING") : Idioma.T("ON, NO REGION");
                var fila = Instantiate(plantillaInstrumento, listaInstrumentos);
                fila.gameObject.SetActive(true);
                fila.name = "Instrumento_" + id;
                fila.GetComponent<UnityEngine.UI.Image>().color =
                    bloqueado ? ColorBloqueado : Escaner == id ? ColorElegido : encendido ? ColorEncendido : ColorNormal;
                fila.GetComponentInChildren<TMP_Text>().text =
                    (Escaner == id ? "→ " : "") + i.Nombre + "  ·  " + i.ConsumoTotalW + " W  ·  " + i.bloquesDatos + " bl.  ·  " + estado;
                fila.onClick.AddListener(() => { if (Escaner == id || !Op.Encendidos.Contains(id)) AlternarInstrumento(id); else ElegirEscaner(id); });
                filas.Add(fila.gameObject);
            }

            foreach (var h in catalogo.herramientas.Where(x => Op.TieneHerramienta(x.id)))
            {
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Herramienta_" + h.id;
                boton.interactable = Op.HerramientasUtiles.Contains(h.id) && !pausa;
                boton.GetComponentInChildren<TMP_Text>().text = h.Nombre + (Op.HerramientasUsadas.Contains(h.id) ? Idioma.T(" (used)") : "");
                boton.onClick.AddListener(() => UsarHerramienta(h.id));
                filas.Add(boton.gameObject);
            }

            // Mapa: regiones del plan (claro), encima las observaciones ya hechas (color de la lectura) y la petición (morado).
            mapa.Limpiar();
            foreach (var par in Op.Regiones) mapa.Marcar(par.Value, ColorRegion);
            foreach (var o in historial) mapa.Marcar(o.Region, ColorLectura(o.Lectura));
            if (Op.PeticionAceptada && !Op.PeticionCumplida) mapa.Marcar(Op.PuntoPeticion, ColorPeticion);

            int limite = Mathf.Max(1, Op.LimiteW);
            rellenoEnergia.anchorMax = new Vector2(Mathf.Clamp01((float)Op.ConsumoW / limite), 1);
            rellenoEnergia.GetComponent<UnityEngine.UI.Image>().color =
                Op.ConsumoW > Op.LimiteW * 0.9f ? new Color32(0xEF, 0x44, 0x44, 0xFF) : new Color32(0x22, 0xC5, 0x5E, 0xFF);
            textoEnergia.text = Idioma.T("Power: ") + Op.ConsumoW + " / " + Op.LimiteW + " W";
            textoOrbitas.text = Op.Terminada ? Idioma.T("Campaign over")
                : Idioma.T("Month ") + Op.Mes + Idioma.T(" of ") + (Op.MesesResueltos + Op.MesesRestantes);

            textoCombustible.text = Idioma.T("Altitude: ") + (Op.Altitud == Altitud.Baja ? Idioma.T("low") : "normal") + Idioma.T("  ·  Fuel: ") + Op.Combustible +
                                    Idioma.T("  ·  Credits: ") + TextosBriefing.Numero(Op.CreditosDisponibles);
            botonAltitud.interactable = !pausa;
            botonAltitud.GetComponentInChildren<TMP_Text>().text = Op.ManiobraPlaneada.HasValue
                ? Idioma.T("Maneuver planned: cancel")
                : (Op.Altitud == Altitud.Normal ? Idioma.T("Descend to low orbit") : Idioma.T("Climb to normal orbit")) +
                  " (" + Reglas.creditosPorManiobra + " cr, " + Op.CostoCambioAltitud + Idioma.T(" fuel, ") + Reglas.diasPorManiobra + Idioma.T(" days)");

            textoMemoria.text = Idioma.T("Memory: ") + Op.MemoriaUsada + " / " + Op.MemoriaCapacidad + Idioma.T(" blocks  ·  ") +
                (Op.EnvioBloqueado ? Idioma.T("<color=#F4C46A>dust is blocking the laser</color>")
                : Op.ContactoAbierto ? Idioma.T("<color=#22C55E>contact with Earth this month</color>")
                : Idioma.T("contact in ") + Meses(Op.MesesParaContacto) + (Op.PuedeEnviarSinVentana ? " (Globalstar: " + Op.Satelite.bloquesEnvioSinVentana + Idioma.T(" blocks)") : ""));
            botonEnviar.interactable = !pausa;
            botonEnviar.GetComponentInChildren<TMP_Text>().text = Idioma.T("Send at month end: ") + (Op.TransmitirPlaneado ? Idioma.T("YES") : "no");
            botonEnviar.GetComponent<UnityEngine.UI.Image>().color = Op.TransmitirPlaneado ? ColorEncendido : ColorNormal;

            botonResolver.interactable = !pausa;
            botonResolver.GetComponentInChildren<TMP_Text>().text = Idioma.T("Resolve month ") + (Op.Terminada ? "" : Op.Mes.ToString());
            botonTerminar.GetComponentInChildren<TMP_Text>().text = Op.Completa || Op.Terminada ? Idioma.T("View report") : Idioma.T("End mission");

            var objetivos = Op.Objetivos.Select(o => "• " + o.Texto + " (+" + o.puntos + ")").ToList();
            if (Op.PeticionAceptada)
                objetivos.Add(Idioma.T("• <color=#A855F7>NASA request</color>: observe the purple point (+") + Op.PuntosPeticion + ")" +
                              (Op.PeticionCumplida ? Idioma.T(" <color=#22C55E>fulfilled</color>") : ""));
            textoObjetivos.text = objetivos.Count > 0 ? Idioma.T("<b>Bonus objectives</b>\n") + string.Join("\n", objetivos) : "";
            if (textoModificadores != null)
            {
                var activos = Modificadores.Activos(Op, catalogo);
                textoModificadores.text = activos.Count == 0 ? "" : Idioma.T("<b>ACTIVE MODIFIERS</b>") + PresentacionMision.Modificadores(activos);
            }

            int enMemoria = Op.Memoria.Sum(r => r.Evidencias.Count);
            int recibidas = Op.Recibidas.Count(id => !string.IsNullOrEmpty(catalogo.Transmision(id).instrumentoId));   // TX-00 es de la misión
            int total = recibidas + enMemoria + Op.Pendientes.Count;
            textoProgreso.text = Idioma.T("Evidence received: ") + recibidas + Idioma.T(" of ") + total +
                                 (enMemoria > 0 ? "  ·  " + enMemoria + Idioma.T(" in memory") : "") + AvisoPlazo(recibidas);
            textoLectura.text = TextoPrevision();
            DibujarGuia(pausa);
            DibujarDecision();
            ActualizarPaneles();
        }

        /// <summary>Línea «siguiente paso» con bombilla y contorno que late en lo que hay que usar.</summary>
        void DibujarGuia(bool pausa)
        {
            if (textoGuia == null) return;
            var paso = GuiaForzada?.Invoke() ?? Guia.Siguiente(Op, catalogo);
            GameObject destino = null;
            string texto = paso.Texto;
            switch (paso.Objetivo)
            {
                case ObjetivoGuia.Instrumento: destino = Fila(paso.InstrumentoId); break;
                case ObjetivoGuia.Mapa:
                    // Si el instrumento no está elegido, primero hay que elegirlo: se señala su fila.
                    if (Escaner != paso.InstrumentoId)
                    {
                        destino = Fila(paso.InstrumentoId);
                        texto = Idioma.T("Select the ") + Nombre(paso.InstrumentoId) + Idioma.T(", then click the map. ") + texto;
                    }
                    else destino = mapa.gameObject;
                    break;
                case ObjetivoGuia.Enviar: destino = botonEnviar.gameObject; break;
                case ObjetivoGuia.Resolver: destino = botonResolver.gameObject; break;
                case ObjetivoGuia.Informe: destino = botonTerminar.gameObject; break;
            }
            if (resumenPendiente) { destino = null; texto = Idioma.T("Read the month summary and press \u00abContinue\u00bb."); }
            PasoActual = paso;
            // Consejos apagados (Opciones o «Ocultar consejos»): sin el paso a paso ni el resaltado. Los avisos
            // (algo no tiene sentido o hay que enviar) se siguen leyendo; la Misión 0 siempre guía.
            bool consejos = Preferencias.GuiaActiva || EstadoJuego.Tutorial || GuiaForzada != null;
            if (!consejos) destino = null;
            textoGuia.text = !consejos && !paso.Aviso ? ""
                : "<color=" + (paso.Aviso ? "#F4C46A" : "#75D6DF") + ">" + Iconos.Pista + "  " + texto + "</color>";
            ResaltarGuia.Mover(ref resaltado, destino);
        }

        /// <summary>Vuelve a dibujar la pantalla (el tutorial lo pide al cambiar de paso).</summary>
        public void Redibujar()
        {
            if (Op != null) Refrescar();
        }

        /// <summary>Marca un punto en el mapa (el tutorial señala dónde mirar).</summary>
        public void MarcarEnMapa(Vector2 punto, Color color) => mapa.Marcar(punto, color);

        GameObject Fila(string instrumentoId) =>
            filas.FirstOrDefault(f => f != null && f.name == "Instrumento_" + instrumentoId);

        void DibujarDecision()
        {
            if (!Op.EsperandoDecision) return;
            textoDecision.text = "<b><color=#F4C46A>" + Op.Decision.Datos.Nombre + "</color></b>\n" + Op.Decision.Datos.Texto +
                                 Idioma.T("\n\n<b>Mission Control:</b> what do we do?");
            int i = 0;
            foreach (var o in Op.Opciones.ToList())
            {
                int indice = i++;
                bool puede = Op.PuedeElegir(o);
                var boton = Instantiate(plantillaOpcion, listaOpciones);
                boton.gameObject.SetActive(true);
                boton.name = "Respuesta_" + indice;
                boton.interactable = puede;
                boton.GetComponentInChildren<TMP_Text>().text = o.Texto + (puede ? "" : Idioma.T("  (not enough fuel)"));
                boton.onClick.AddListener(() => Decidir(indice));
                filas.Add(boton.gameObject);
            }
        }
    }
}
