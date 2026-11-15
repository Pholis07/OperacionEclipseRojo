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
            aviso = "Elige un instrumento, enciéndelo y pulsa en el mapa la región que observará. " +
                    "Cuando el plan esté listo, pulsa «Resolver mes». Preparar el plan no gasta nada.";
            textoEvento.text = "Sin incidencias.";
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
            if (Op.Encendidos.Contains(id)) Op.Apagar(id);
            else if (!Op.Encender(id))
                aviso = Op.Bloqueados.ContainsKey(id)
                    ? Nombre(id) + " está bloqueado por el sobrecalentamiento."
                    : "No hay potencia para encender " + Nombre(id) + ": apaga otro instrumento.";
            else if (!Op.Regiones.ContainsKey(id))
                aviso = Nombre(id) + " encendido. Pulsa en el mapa la región que observará.";
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
            aviso = "Región fijada para " + Nombre(Escaner) + "." +
                    (Op.Encendidos.Contains(Escaner) ? "" : " Enciéndelo para que observe este mes.");
            Refrescar();
        }

        public void AlternarManiobra()
        {
            if (Op == null) return;
            if (Op.ManiobraPlaneada.HasValue) Op.CancelarManiobra();
            else if (!Op.PlanearManiobra(Op.Altitud == Altitud.Normal ? Altitud.Baja : Altitud.Normal))
                aviso = "No se puede maniobrar: hacen falta " + Op.CostoCambioAltitud + " de combustible y " +
                        Reglas.creditosPorManiobra + " créditos. No se cobró nada.";
            Refrescar();
        }

        public void AlternarEnvio()
        {
            if (Op == null) return;
            Op.TransmitirPlaneado = !Op.TransmitirPlaneado;
            Refrescar();
        }

        // ---------- Resolver ----------

        /// <summary>Resuelve el mes en curso una sola vez y muestra el resumen.</summary>
        public void ResolverMes()
        {
            if (Op == null || resumenPendiente) return;
            int recibidas = Op.Recibidas.Count;
            var r = Op.ResolverMes(Op.Mes);
            if (!r.Valido)
            {
                aviso = "<color=#F4C46A><b>No se puede resolver:</b></color> " + r.Motivo + ".";
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
            textoEvento.text = "<b><color=#F4C46A>" + evento.Datos.nombre + "</color></b>\nDecidiste: " + opcion.texto + ".";
            if (Op.PeticionAceptada && opcion.efecto == EfectoOpcion.AceptarObjetivo)
                aviso = "La NASA marcó un punto morado en el mapa: obsérvalo con cualquier instrumento para cumplir la petición.";
            if (Op.Apagones > apagones) aviso = MensajeApagon();
            Refrescar();
        }

        public void UsarHerramienta(string id)
        {
            if (Op == null) return;
            if (Op.UsarHerramienta(id))
                textoEvento.text = "<b>" + catalogo.Herramienta(id).nombre + "</b>: problema resuelto.";
            Refrescar();
        }

        public void Terminar()
        {
            if (Op == null) return;
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
                decidi.Add("observar con " + string.Join(", ", r.Observaciones.Select(o => o.Instrumento)
                    .Concat(r.SinMemoria).Concat(r.SinRegion).Select(Nombre)));
            else decidi.Add("no observar");
            if (r.Maniobra) decidi.Add("maniobrar a la órbita " + (r.Altitud == Altitud.Baja ? "baja" : "normal"));
            if (r.EnvioPedido) decidi.Add("enviar a la Tierra");

            var ocurrio = new List<string>();
            foreach (var o in r.Observaciones)
                ocurrio.Add("• " + Nombre(o.Instrumento) + ": " + TextoLectura(o) + (o.Evidencias.Count > 0
                    ? " <color=#22C55E>Evidencia válida, en memoria.</color>" : ""));
            foreach (var id in r.SinMemoria) ocurrio.Add("• " + Nombre(id) + ": <color=#F4C46A>sin memoria</color>, no se registró nada.");
            foreach (var id in r.SinRegion) ocurrio.Add("• " + Nombre(id) + ": encendido sin región, no observó.");
            if (r.DiasUtiles == 0) ocurrio.Add("• El mes no tuvo días útiles: no se observó.");
            if (r.Observaciones.Count == 0 && r.SinMemoria.Count == 0 && r.SinRegion.Count == 0 && r.DiasUtiles > 0)
                ocurrio.Add("• Ningún instrumento observó este mes.");
            if (r.EnvioPedido)
                ocurrio.Add(r.EnvioFallido != null
                    ? "• <color=#F4C46A>No se envió:</color> " + r.EnvioFallido + ". Los datos siguen en la memoria."
                    : "• Envío: " + r.BloquesEnviados + " bloques a la Tierra" +
                      (r.Enviadas.Count > 0 ? ", con <color=#22C55E>" + r.Enviadas.Count + " evidencia(s) recibida(s)</color>." : ", sin evidencias nuevas."));
            if (r.SubidaForzada) ocurrio.Add("• Sin combustible para el arrastre: la nave subió sola a la órbita normal.");

            var gaste = new List<string>();
            if (r.CreditosGastados > 0) gaste.Add(r.CreditosGastados + " créditos");
            if (r.CombustibleGastado > 0) gaste.Add(r.CombustibleGastado + " de combustible");
            if (r.Maniobra) gaste.Add(Reglas.diasPorManiobra + " días por la maniobra");

            string sigue = r.Termino ? "La campaña terminó. Pulsa «Ver informe»."
                : "Mes " + Op.Mes + ". " + (Op.ContactoAbierto ? "Hay contacto con la Tierra." : "Contacto en " + Meses(Op.MesesParaContacto) + ".") +
                  (r.Evento != null && Op.EsperandoDecision ? " Control de Misión espera una decisión: " + r.Evento.Datos.nombre + "." : "") +
                  " Los instrumentos encendidos siguen encendidos.";

            return "<b>Mes " + r.Mes + " resuelto</b>  ·  " + r.DiasUtiles + " días útiles\n\n" +
                   "<b>Decidiste:</b> " + string.Join(", ", decidi) + ".\n\n" +
                   "<b>Ocurrió:</b>\n" + string.Join("\n", ocurrio) + "\n\n" +
                   "<b>Gastaste:</b> " + (gaste.Count > 0 ? string.Join(", ", gaste) : "nada") + ".\n\n" +
                   "<b>Sigue:</b> " + sigue;
        }

        static string Meses(int n) => n + (n == 1 ? " mes" : " meses");

        string TextoLectura(Registro o)
        {
            string calidad = " (calidad " + Mathf.RoundToInt(o.Calidad) + ")";
            switch (o.Lectura)
            {
                case Lectura.Encontrada:
                    return "<color=#22C55E><b>anomalía observada</b></color>" + calidad + "." +
                           (o.Calidad < Reglas.calidadMinima ? " <color=#F4C46A>Calidad insuficiente: no vale como evidencia.</color>" : "");
                case Lectura.Caliente: return "<color=#E2643A><b>caliente</b></color>, muy cerca" + calidad + ".";
                case Lectura.Tibio: return "<color=#EAB308><b>tibio</b></color>, te acercas" + calidad + ".";
                case Lectura.Frio: return "<color=#60A5FA><b>frío</b></color>, lejos de la anomalía" + calidad + ".";
                default: return "<b>sin lectura</b>: la nave estaba desorientada.";
            }
        }

        string TextoPrevision()
        {
            string motivo = Op.MotivoPlanInvalido;
            int prevista = Op.MemoriaUsada + Op.BloquesPrevistos;
            var t = "<b>Previsión del mes " + Op.Mes + ":</b> " + Op.DiasPrevistos + " días útiles  ·  consumo " + Op.ConsumoW + " / " + Op.LimiteW + " W" +
                    "  ·  memoria " + prevista + " / " + Op.MemoriaCapacidad + " bloques" +
                    (prevista > Op.MemoriaCapacidad ? " <color=#F4C46A>(no cabrá todo)</color>" : "");
            if (Op.ManiobraPlaneada.HasValue)
                t += "\nManiobra: " + Reglas.creditosPorManiobra + " cr, " + Op.CostoCambioAltitud + " de combustible y " + Reglas.diasPorManiobra + " días.";
            if (Op.TransmitirPlaneado)
                t += "\nEnvío al final del mes: " + (Op.MotivoSinEnvio == null
                    ? "sí (los instrumentos se apagan solos durante la ventana)."
                    : "<color=#F4C46A>no se podrá, " + Op.MotivoSinEnvio + "</color>.");
            if (motivo != null && !Op.Terminada && !Op.EsperandoDecision) t += "\n<color=#F4C46A>" + motivo + ".</color>";
            if (Op.Terminada) t = "La campaña terminó. Lo que no se envió no cuenta como evidencia.";
            return t + (aviso.Length > 0 ? "\n" + aviso : "");
        }

        void MostrarTransmision(string id)
        {
            var t = catalogo.Transmision(id);
            string encabezado = string.IsNullOrEmpty(t.instrumentoId) ? "Inicio de la observación" : "Transmisión: " + Nombre(t.instrumentoId);
            textoTransmision.text = "<b>" + encabezado + "</b>\n" + t.texto;
        }

        void MostrarEvento(EventoActivo e)
        {
            textoEvento.text = "<b><color=#F4C46A>" + e.Datos.nombre + "</color></b>\n" + (e.EvitadoPorSatelite
                ? "Al " + Op.Satelite.nombre + " no le afectó: su apunte es muy preciso."
                : "Control de Misión espera tu decisión.");
        }

        string MensajeApagon() =>
            "<color=#EF4444><b>Modo seguro.</b></color> El evento dejó el consumo por encima del límite: la nave apagó los instrumentos, " +
            "el reinicio costó " + Reglas.creditosPorReinicio + " créditos y el mes siguiente pierde " + Reglas.diasPerdidosPorApagon + " días.";

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

        string Nombre(string instrumentoId) => catalogo.Instrumento(instrumentoId).nombre;

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
                string estado = bloqueado ? "BLOQUEADO (" + Meses(resta) + ")"
                    : !encendido ? "apagado"
                    : Op.Regiones.ContainsKey(id) ? "OBSERVANDO" : "ENCENDIDO, SIN REGIÓN";
                var fila = Instantiate(plantillaInstrumento, listaInstrumentos);
                fila.gameObject.SetActive(true);
                fila.name = "Instrumento_" + id;
                fila.GetComponent<UnityEngine.UI.Image>().color =
                    bloqueado ? ColorBloqueado : Escaner == id ? ColorElegido : encendido ? ColorEncendido : ColorNormal;
                fila.GetComponentInChildren<TMP_Text>().text =
                    (Escaner == id ? "» " : "") + i.nombre + "  ·  " + i.ConsumoTotalW + " W  ·  " + i.bloquesDatos + " bl.  ·  " + estado;
                fila.onClick.AddListener(() => { if (Escaner == id || !Op.Encendidos.Contains(id)) AlternarInstrumento(id); else ElegirEscaner(id); });
                filas.Add(fila.gameObject);
            }

            foreach (var h in catalogo.herramientas.Where(x => Op.TieneHerramienta(x.id)))
            {
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Herramienta_" + h.id;
                boton.interactable = Op.HerramientasUtiles.Contains(h.id) && !pausa;
                boton.GetComponentInChildren<TMP_Text>().text = h.nombre + (Op.HerramientasUsadas.Contains(h.id) ? " (usada)" : "");
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
            textoEnergia.text = "Potencia: " + Op.ConsumoW + " / " + Op.LimiteW + " W";
            textoOrbitas.text = Op.Terminada ? "Campaña terminada"
                : "Mes " + Op.Mes + " de " + (Op.MesesResueltos + Op.MesesRestantes);

            textoCombustible.text = "Altitud: " + (Op.Altitud == Altitud.Baja ? "baja" : "normal") + "  ·  Combustible: " + Op.Combustible +
                                    "  ·  Créditos: " + TextosBriefing.Numero(Op.CreditosDisponibles);
            botonAltitud.interactable = !pausa;
            botonAltitud.GetComponentInChildren<TMP_Text>().text = Op.ManiobraPlaneada.HasValue
                ? "Maniobra planeada: cancelar"
                : (Op.Altitud == Altitud.Normal ? "Bajar a órbita baja" : "Subir a órbita normal") +
                  " (" + Reglas.creditosPorManiobra + " cr, " + Op.CostoCambioAltitud + " comb., " + Reglas.diasPorManiobra + " días)";

            textoMemoria.text = "Memoria: " + Op.MemoriaUsada + " / " + Op.MemoriaCapacidad + " bloques  ·  " +
                (Op.EnvioBloqueado ? "<color=#F4C46A>el polvo bloquea el láser</color>"
                : Op.ContactoAbierto ? "<color=#22C55E>contacto con la Tierra este mes</color>"
                : "contacto en " + Meses(Op.MesesParaContacto) + (Op.PuedeEnviarSinVentana ? " (Globalstar: " + Op.Satelite.bloquesEnvioSinVentana + " bloques)" : ""));
            botonEnviar.interactable = !pausa;
            botonEnviar.GetComponentInChildren<TMP_Text>().text = "Enviar al final del mes: " + (Op.TransmitirPlaneado ? "SÍ" : "no");
            botonEnviar.GetComponent<UnityEngine.UI.Image>().color = Op.TransmitirPlaneado ? ColorEncendido : ColorNormal;

            botonResolver.interactable = !pausa;
            botonResolver.GetComponentInChildren<TMP_Text>().text = "Resolver mes " + (Op.Terminada ? "" : Op.Mes.ToString());
            botonTerminar.GetComponentInChildren<TMP_Text>().text = Op.Completa || Op.Terminada ? "Ver informe" : "Terminar misión";

            var objetivos = Op.Objetivos.Select(o => "• " + o.texto + " (+" + o.puntos + ")").ToList();
            if (Op.PeticionAceptada)
                objetivos.Add("• <color=#A855F7>Petición de la NASA</color>: observar el punto morado (+" + Op.PuntosPeticion + ")" +
                              (Op.PeticionCumplida ? " <color=#22C55E>cumplida</color>" : ""));
            textoObjetivos.text = objetivos.Count > 0 ? "<b>Objetivos extra</b>\n" + string.Join("\n", objetivos) : "";

            int enMemoria = Op.Memoria.Sum(r => r.Evidencias.Count);
            int recibidas = Op.Recibidas.Count(id => !string.IsNullOrEmpty(catalogo.Transmision(id).instrumentoId));   // TX-00 es de la misión
            int total = recibidas + enMemoria + Op.Pendientes.Count;
            textoProgreso.text = "Evidencias recibidas: " + recibidas + " de " + total +
                                 (enMemoria > 0 ? "  ·  " + enMemoria + " en memoria" : "");
            textoLectura.text = TextoPrevision();
            DibujarDecision();
            ActualizarPaneles();
        }

        void DibujarDecision()
        {
            if (!Op.EsperandoDecision) return;
            textoDecision.text = "<b><color=#F4C46A>" + Op.Decision.Datos.nombre + "</color></b>\n" + Op.Decision.Datos.texto +
                                 "\n\n<b>Control de Misión:</b> ¿qué hacemos?";
            int i = 0;
            foreach (var o in Op.Opciones.ToList())
            {
                int indice = i++;
                bool puede = Op.PuedeElegir(o);
                var boton = Instantiate(plantillaOpcion, listaOpciones);
                boton.gameObject.SetActive(true);
                boton.name = "Respuesta_" + indice;
                boton.interactable = puede;
                boton.GetComponentInChildren<TMP_Text>().text = o.texto + (puede ? "" : "  (no alcanza el combustible)");
                boton.onClick.AddListener(() => Decidir(indice));
                filas.Add(boton.gameObject);
            }
        }
    }
}
