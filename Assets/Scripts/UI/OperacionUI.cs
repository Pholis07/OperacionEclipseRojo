using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Vista Operación (sustituye a la Investigación): instrumentos con interruptor, energía, órbitas,
    /// mapa para escanear, eventos y herramientas. Toda la lógica está en Operacion; aquí solo se dibuja.
    /// </summary>
    public class OperacionUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] MapaMarteUI mapa;

        [Header("Instrumentos")]
        [SerializeField] Transform listaInstrumentos;
        [SerializeField] UnityEngine.UI.Button plantillaInstrumento;   // fila: botón «elegir para escanear»
        [SerializeField] RectTransform rellenoEnergia;
        [SerializeField] TMP_Text textoEnergia;
        [SerializeField] TMP_Text textoOrbitas;

        [Header("Mensajes")]
        [SerializeField] TMP_Text textoLectura;
        [SerializeField] TMP_Text textoEvento;
        [SerializeField] TMP_Text textoTransmision;
        [SerializeField] TMP_Text textoProgreso;

        [Header("Herramientas y fin")]
        [SerializeField] Transform listaHerramientas;
        [SerializeField] UnityEngine.UI.Button plantillaHerramienta;
        [SerializeField] UnityEngine.UI.Button botonTerminar;

        [Header("Gameplay v3: altitud, memoria, decisiones y objetivos (opcionales en escenas viejas)")]
        [SerializeField] TMP_Text textoCombustible;
        [SerializeField] UnityEngine.UI.Button botonAltitud;
        [SerializeField] TMP_Text textoMemoria;
        [SerializeField] UnityEngine.UI.Button botonEnviar;
        [SerializeField] UnityEngine.UI.Button botonEsperar;
        [SerializeField] TMP_Text textoObjetivos;
        [SerializeField] GameObject panelDecision;
        [SerializeField] TMP_Text textoDecision;
        [SerializeField] Transform listaOpciones;
        [SerializeField] UnityEngine.UI.Button plantillaOpcion;

        static readonly Color ColorNormal = new Color32(0x2A, 0x2A, 0x31, 0xFF);
        static readonly Color ColorEncendido = new Color32(0x14, 0x53, 0x2D, 0xFF);
        static readonly Color ColorEscaner = new Color32(0x5A, 0x26, 0x14, 0xFF);
        static readonly Color ColorBloqueado = new Color32(0x3F, 0x3F, 0x46, 0xFF);

        readonly List<GameObject> filas = new List<GameObject>();
        bool inicializada;

        /// <summary>
        /// Después de cada escaneo válido: instrumento, punto del mapa (0 a 1), lectura y si llegó una transmisión
        /// nueva. Solo lo usa la vista 3D (SecuenciaEscaneo); la lógica ya terminó cuando se avisa.
        /// </summary>
        public event System.Action<string, Vector2, Lectura, bool> AlEscanear;

        public Operacion Op { get; private set; }
        public string Escaner { get; private set; }
        public string UltimoMensaje => textoLectura.text;

        CatalogoDatos Catalogo => catalogo;
        ReglasOperacionDatos Reglas => catalogo.reglas;

        // Presentación de Diego (cinemática y vista 3D del escaneo): la ventana de decisión espera a que terminen.
        SecuenciaEscaneo secuenciaEscaneo;
        DirectorCinematica director;

        void Update() => ActualizarPanelDecision();

        /// <summary>La ventana de decisión se ve si hay un evento en pausa y no se está viendo una animación.</summary>
        void ActualizarPanelDecision()
        {
            if (panelDecision == null || Op == null) return;
            bool ocupada = (secuenciaEscaneo != null && secuenciaEscaneo.Activa) || (director != null && director.Reproduciendo);
            bool ver = Op.EsperandoDecision && !ocupada;
            if (panelDecision.activeSelf != ver) panelDecision.SetActive(ver);
        }

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            plantillaInstrumento.gameObject.SetActive(false);
            plantillaHerramienta.gameObject.SetActive(false);
            mapa.AlPulsar += EscanearEn;
            botonTerminar.onClick.AddListener(Terminar);
            if (plantillaOpcion != null) plantillaOpcion.gameObject.SetActive(false);
            if (panelDecision != null) panelDecision.SetActive(false);
            if (botonAltitud != null) botonAltitud.onClick.AddListener(CambiarAltitud);
            if (botonEnviar != null) botonEnviar.onClick.AddListener(Enviar);
            if (botonEsperar != null) botonEsperar.onClick.AddListener(Esperar);
            secuenciaEscaneo = FindFirstObjectByType<SecuenciaEscaneo>(FindObjectsInactive.Include);
            director = FindFirstObjectByType<DirectorCinematica>(FindObjectsInactive.Include);
        }

        /// <summary>Empieza la Operación sin Carga (modo anterior a la v3). La semilla fija solo la usan las pruebas.</summary>
        public void Comenzar(Evaluacion e, int? semilla = null) => Comenzar(e, null, semilla);

        /// <summary>Empieza la Operación de una misión lanzada con lo que se cargó (v3).</summary>
        public void Comenzar(Evaluacion e, Carga carga, int? semilla = null)
        {
            Inicializar();
            if (e == null || !e.SePuedeLanzar) return;
            int s = semilla ?? System.Environment.TickCount;
            Op = new Operacion(e, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice), s, carga);
            EstadoJuego.Operacion = Op;
            mapa.Generar(s);
            mapa.Limpiar();
            Escaner = Op.Instrumentos.FirstOrDefault();
            textoLectura.text = "Pulsa un instrumento para encenderlo y escanear con él; pulsa otra vez para apagarlo. " +
                                "Después pulsa en el mapa: cada escaneo gasta " + Reglas.orbitasPorEscaneo + " órbita. " +
                                "Si el consumo pasa del límite, la nave se apaga.";
            if (Op.ConMemoria)
                textoLectura.text += " Lo que encuentres queda en la memoria: envíalo a la Tierra cuando se abra la ventana de contacto.";
            textoEvento.text = "Sin incidencias.";
            MostrarTransmision(Op.Recibidas.Last());
            Refrescar();
        }

        // ---------- Acciones del jugador ----------

        public void AlternarInstrumento(string id)
        {
            if (Op == null || Op.Terminada) return;
            if (Op.Encendidos.Contains(id))
            {
                Op.Apagar(id);
            }
            else
            {
                int apagones = Op.Apagones;
                if (!Op.Encender(id))
                    textoLectura.text = Nombre(id) + " está bloqueado por el sobrecalentamiento.";
                else if (Op.Apagones > apagones)
                    textoLectura.text = MensajeApagon();
                else
                    Escaner = id;
            }
            Refrescar();
        }

        public void ElegirEscaner(string id)
        {
            Escaner = id;
            Refrescar();
        }

        public void EscanearEn(Vector2 punto)
        {
            if (Op == null || Op.Terminada) return;
            if (Escaner == null || !Op.Encendidos.Contains(Escaner))
            {
                textoLectura.text = "Para escanear, enciende el instrumento elegido (" + (Escaner != null ? Nombre(Escaner) : "ninguno") + ").";
                return;
            }

            if (Op.EsperandoDecision) return;
            if (Op.MemoriaLlena)
            {
                textoLectura.text = "<color=#EAB308><b>Memoria llena.</b></color> Envía los datos a la Tierra en la próxima ventana de contacto.";
                return;
            }

            int recibidas = Op.Recibidas.Count, enMemoria = Op.EnMemoria.Count, apagones = Op.Apagones, eventos = Op.Eventos.Count;
            var ultimoEvento = Op.UltimoEvento;
            var lectura = Op.Escanear(Escaner, punto);
            if (lectura == null) return;

            mapa.Marcar(punto, ColorLectura(lectura.Value));
            textoLectura.text = TextoLectura(lectura.Value, Escaner, Op.Recibidas.Count > recibidas || Op.EnMemoria.Count > enMemoria);
            if (Op.EnMemoria.Count > enMemoria) textoLectura.text += " Los datos quedan en la memoria hasta que los envíes.";
            if (Op.Recibidas.Count > recibidas) MostrarTransmision(Op.Recibidas.Last());
            if (Op.Eventos.Count > eventos) MostrarEvento(Op.UltimoEvento);
            if (Op.EsperandoDecision && Op.UltimoEvento != ultimoEvento) MostrarEvento(Op.UltimoEvento);
            if (Op.Apagones > apagones) textoLectura.text += "\n" + MensajeApagon();
            if (Op.Terminada) textoLectura.text += "\nSe acabaron las órbitas: lo que falte llegará con baja calidad.";
            Refrescar();
            AlEscanear?.Invoke(Escaner, punto, lectura.Value, Op.Recibidas.Count > recibidas || Op.EnMemoria.Count > enMemoria);
        }

        public void UsarHerramienta(string id)
        {
            if (Op == null) return;
            if (Op.UsarHerramienta(id))
            {
                textoEvento.text = "<b>" + Catalogo.Herramienta(id).nombre + "</b>: problema resuelto.";
            }
            Refrescar();
        }

        public void CambiarAltitud()
        {
            if (Op == null) return;
            var nueva = Op.Altitud == Altitud.Normal ? Altitud.Baja : Altitud.Normal;
            if (!Op.CambiarAltitud(nueva))
                textoLectura.text = "No alcanza el combustible para cambiar de altitud (cuesta " + Reglas.combustiblePorCambioAltitud + ").";
            else
                textoLectura.text = nueva == Altitud.Baja
                    ? "Bajaste a la órbita baja: las lecturas alcanzan más lejos, pero el arrastre gasta " + Reglas.combustiblePorOrbitaBaja + " de combustible por órbita."
                    : "Subiste a la órbita normal: sin arrastre.";
            Refrescar();
        }

        public void Enviar()
        {
            if (Op == null) return;
            int recibidas = Op.Recibidas.Count, apagones = Op.Apagones;
            if (Op.Enviar())
            {
                textoLectura.text = "<color=#22C55E><b>Datos enviados a la Tierra.</b></color> La memoria quedó libre.";
                if (Op.Recibidas.Count > recibidas) MostrarTransmision(Op.Recibidas.Last());
            }
            else if (Op.Apagones > apagones)
                textoLectura.text = "El transmisor necesita " + Reglas.vatiosTransmisor + " W y no cabían.\n" + MensajeApagon();
            Refrescar();
        }

        public void Esperar()
        {
            if (Op == null || !Op.Esperar()) return;
            textoLectura.text = "Dejaste pasar una órbita sin escanear.";
            if (Op.Terminada) textoLectura.text += "\nSe acabaron las órbitas: lo que falte llegará con baja calidad.";
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
            textoEvento.text = "<b><color=#EAB308>" + evento.Datos.nombre + "</color></b>\nDecidiste: " + opcion.texto + ".";
            if (Op.PeticionAceptada && opcion.efecto == EfectoOpcion.AceptarObjetivo)
            {
                mapa.Marcar(Op.PuntoPeticion, ColorPeticion);
                textoLectura.text = "La NASA marcó un punto morado en el mapa: escanéalo para cumplir la petición.";
            }
            if (Op.Apagones > apagones) textoLectura.text = MensajeApagon();
            Refrescar();
        }

        public void Terminar()
        {
            if (Op == null) return;
            Op.Terminar();
            if (Application.isPlaying) SceneManager.LoadScene("Resultado");
        }

        // ---------- Dibujo ----------

        void MostrarTransmision(string id)
        {
            textoTransmision.text = "<b>" + Encabezado(id) + "</b>\n" + Catalogo.Transmision(id).texto;
        }

        void MostrarEvento(EventoActivo e)
        {
            var herramienta = string.IsNullOrEmpty(e.Datos.herramientaId) ? null : Catalogo.Herramienta(e.Datos.herramientaId);
            textoEvento.text = "<b><color=#EAB308>" + e.Datos.nombre + "</color></b>\n" + e.Datos.texto +
                               (herramienta != null && !e.Resuelto ? "\nSe resuelve con: " + herramienta.nombre + "." : "");
        }

        string Encabezado(string transmisionId)
        {
            var t = Catalogo.Transmision(transmisionId);
            return string.IsNullOrEmpty(t.instrumentoId) ? "Inicio de la observación" : "Transmisión: " + Nombre(t.instrumentoId);
        }

        string MensajeApagon() =>
            "<color=#EF4444><b>¡Apagón!</b></color> El consumo superó el límite y la nave entró en modo seguro: pierdes " +
            Reglas.orbitasPerdidasPorApagon + " órbitas y " + Reglas.creditosPorReinicio + " créditos.";

        string TextoLectura(Lectura l, string instrumento, bool nueva)
        {
            switch (l)
            {
                case Lectura.Encontrada:
                    return nueva ? "<color=#22C55E><b>¡Encontrada!</b></color> " + Nombre(instrumento) + " observó la anomalía."
                                 : "<color=#22C55E><b>Aquí está la anomalía.</b></color> " + Nombre(instrumento) + " no aporta nada nuevo aquí.";
                case Lectura.Caliente: return "<color=#E2643A><b>Caliente</b></color>: muy cerca.";
                case Lectura.Tibio: return "<color=#EAB308><b>Tibio</b></color>: te acercas.";
                case Lectura.Frio: return "<color=#60A5FA><b>Frío</b></color>: lejos de la anomalía.";
                default: return "<b>Sin lectura</b>: la nave estaba desorientada.";
            }
        }

        static readonly Color ColorPeticion = new Color32(0xA8, 0x55, 0xF7, 0xFF);

        string TextoDecision(EventoActivo e) =>
            "<b><color=#EAB308>" + e.Datos.nombre + "</color></b>\n" + e.Datos.texto + "\n\n<b>Control de Misión:</b> ¿qué hacemos?";

        void DibujarDecision()
        {
            if (panelDecision == null) return;
            ActualizarPanelDecision();
            if (!Op.EsperandoDecision) return;
            textoDecision.text = TextoDecision(Op.Decision);
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

        void DibujarNave()
        {
            if (textoCombustible != null)
                textoCombustible.text = Op.ConMemoria
                    ? "Altitud: " + (Op.Altitud == Altitud.Baja ? "baja" : "normal") + "  ·  Combustible: " + Op.Combustible
                    : "Altitud: normal";
            if (botonAltitud != null)
            {
                botonAltitud.gameObject.SetActive(Op.ConMemoria);
                botonAltitud.interactable = !Op.Terminada && !Op.EsperandoDecision && Op.Combustible >= Reglas.combustiblePorCambioAltitud;
                botonAltitud.GetComponentInChildren<TMP_Text>().text =
                    (Op.Altitud == Altitud.Normal ? "Bajar a órbita baja" : "Subir a órbita normal") +
                    " (" + Reglas.combustiblePorCambioAltitud + " de combustible)";
            }
            if (textoMemoria != null)
                textoMemoria.text = !Op.ConMemoria ? "" :
                    "Memoria: " + Op.MemoriaUsada + " / " + Op.MemoriaCapacidad + " bloques  ·  " +
                    (Op.ContactoAbierto ? "<color=#22C55E>contacto con la Tierra ABIERTO</color>"
                        : "contacto en " + Op.OrbitasParaContacto + (Op.OrbitasParaContacto == 1 ? " órbita" : " órbitas"));
            if (botonEnviar != null)
            {
                botonEnviar.gameObject.SetActive(Op.ConMemoria);
                botonEnviar.interactable = !Op.Terminada && !Op.EsperandoDecision && Op.ContactoAbierto && Op.MemoriaUsada > 0;
                botonEnviar.GetComponentInChildren<TMP_Text>().text = "Enviar a la Tierra (+" + Reglas.vatiosTransmisor + " W)";
            }
            if (botonEsperar != null)
            {
                botonEsperar.gameObject.SetActive(Op.ConMemoria);
                botonEsperar.interactable = !Op.Terminada && !Op.EsperandoDecision;
                botonEsperar.GetComponentInChildren<TMP_Text>().text = "Esperar 1 órbita";
            }
            if (textoObjetivos != null)
            {
                var lineas = Op.Objetivos.Select(o => "• " + o.texto + " (+" + o.puntos + ")").ToList();
                if (Op.PeticionAceptada)
                    lineas.Add("• <color=#A855F7>Petición de la NASA</color>: escanear el punto morado (+" + Op.PuntosPeticion + ")" +
                               (Op.PeticionCumplida ? " <color=#22C55E>cumplida</color>" : ""));
                textoObjetivos.text = lineas.Count > 0 ? "<b>Objetivos extra</b>\n" + string.Join("\n", lineas) : "";
            }
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

        string Nombre(string instrumentoId) => Catalogo.Instrumento(instrumentoId).nombre;

        void Refrescar()
        {
            foreach (var f in filas) { if (Application.isPlaying) Destroy(f); else DestroyImmediate(f); }
            filas.Clear();

            foreach (var id in Op.Instrumentos)
            {
                var i = Catalogo.Instrumento(id);
                bool encendido = Op.Encendidos.Contains(id);
                bool bloqueado = Op.Bloqueados.TryGetValue(id, out int resta);
                string estado = bloqueado ? "BLOQUEADO (" + resta + " órbitas)" : encendido ? "ENCENDIDO" : "apagado";
                var fila = Instantiate(plantillaInstrumento, listaInstrumentos);
                fila.gameObject.SetActive(true);
                fila.name = "Instrumento_" + id;
                fila.GetComponent<UnityEngine.UI.Image>().color =
                    bloqueado ? ColorBloqueado : Escaner == id ? ColorEscaner : encendido ? ColorEncendido : ColorNormal;
                fila.GetComponentInChildren<TMP_Text>().text =
                    (Escaner == id ? "» " : "") + i.nombre + "  ·  " + i.ConsumoTotalW + " W  ·  " + estado;
                fila.onClick.AddListener(() => { if (Escaner == id || !Op.Encendidos.Contains(id)) AlternarInstrumento(id); else ElegirEscaner(id); });
                filas.Add(fila.gameObject);
            }

            foreach (var h in Catalogo.herramientas.Where(x => Op.TieneHerramienta(x.id)))
            {
                bool usada = Op.HerramientasUsadas.Contains(h.id);
                bool util = Op.HerramientasUtiles.Contains(h.id);
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Herramienta_" + h.id;
                boton.interactable = util && !Op.Terminada && !Op.EsperandoDecision;
                boton.GetComponentInChildren<TMP_Text>().text = h.nombre + (usada ? " (usada)" : "");
                boton.onClick.AddListener(() => UsarHerramienta(h.id));
                filas.Add(boton.gameObject);
            }

            int limite = Mathf.Max(1, Op.LimiteW);
            rellenoEnergia.anchorMax = new Vector2(Mathf.Clamp01((float)Op.ConsumoW / limite), 1);
            rellenoEnergia.GetComponent<UnityEngine.UI.Image>().color =
                Op.ConsumoW > Op.LimiteW * 0.9f ? new Color32(0xEF, 0x44, 0x44, 0xFF) : new Color32(0x22, 0xC5, 0x5E, 0xFF);
            textoEnergia.text = "Energía: " + Op.ConsumoW + " / " + Op.LimiteW + " W";
            textoOrbitas.text = "Órbitas de navegación: " + Op.Orbitas;

            int total = Op.Recibidas.Count + Op.EnMemoria.Count + Op.Pendientes.Count + Op.BajaCalidad.Count;
            textoProgreso.text = "Transmisiones recibidas: " + Op.Recibidas.Count + " de " + total +
                                 (Op.EnMemoria.Count > 0 ? "  ·  " + Op.EnMemoria.Count + " en memoria" : "");
            DibujarNave();
            DibujarDecision();
            botonTerminar.GetComponentInChildren<TMP_Text>().text = Op.Completa || Op.Terminada ? "Ver informe" : "Terminar misión";
        }
    }
}
