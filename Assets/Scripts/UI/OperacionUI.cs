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

        static readonly Color ColorNormal = new Color32(0x2A, 0x2A, 0x31, 0xFF);
        static readonly Color ColorEncendido = new Color32(0x14, 0x53, 0x2D, 0xFF);
        static readonly Color ColorEscaner = new Color32(0x5A, 0x26, 0x14, 0xFF);
        static readonly Color ColorBloqueado = new Color32(0x3F, 0x3F, 0x46, 0xFF);

        readonly List<GameObject> filas = new List<GameObject>();
        bool inicializada;

        public Operacion Op { get; private set; }
        public string Escaner { get; private set; }
        public string UltimoMensaje => textoLectura.text;

        CatalogoDatos Catalogo => catalogo;
        ReglasOperacionDatos Reglas => catalogo.reglas;

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            plantillaInstrumento.gameObject.SetActive(false);
            plantillaHerramienta.gameObject.SetActive(false);
            mapa.AlPulsar += EscanearEn;
            botonTerminar.onClick.AddListener(Terminar);
        }

        /// <summary>Empieza la Operación de una misión lanzada. La semilla fija solo la usan las pruebas.</summary>
        public void Comenzar(Evaluacion e, int? semilla = null)
        {
            Inicializar();
            if (e == null || !e.SePuedeLanzar) return;
            int s = semilla ?? System.Environment.TickCount;
            Op = new Operacion(e, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice), s);
            EstadoJuego.Operacion = Op;
            mapa.Generar(s);
            mapa.Limpiar();
            Escaner = Op.Instrumentos.FirstOrDefault();
            textoLectura.text = "Pulsa un instrumento para encenderlo y escanear con él; pulsa otra vez para apagarlo. " +
                                "Después pulsa en el mapa: cada escaneo gasta " + Reglas.orbitasPorEscaneo + " órbita. " +
                                "Si el consumo pasa del límite, la nave se apaga.";
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

            int recibidas = Op.Recibidas.Count, apagones = Op.Apagones, eventos = Op.Eventos.Count;
            var lectura = Op.Escanear(Escaner, punto);
            if (lectura == null) return;

            mapa.Marcar(punto, ColorLectura(lectura.Value));
            textoLectura.text = TextoLectura(lectura.Value, Escaner, Op.Recibidas.Count > recibidas);
            if (Op.Recibidas.Count > recibidas) MostrarTransmision(Op.Recibidas.Last());
            if (Op.Eventos.Count > eventos) MostrarEvento(Op.UltimoEvento);
            if (Op.Apagones > apagones) textoLectura.text += "\n" + MensajeApagon();
            if (Op.Terminada) textoLectura.text += "\nSe acabaron las órbitas: lo que falte llegará con baja calidad.";
            Refrescar();
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

            foreach (var h in Catalogo.herramientas)
            {
                bool usada = Op.HerramientasUsadas.Contains(h.id);
                bool util = Op.HerramientasUtiles.Contains(h.id);
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Herramienta_" + h.id;
                boton.interactable = util && !Op.Terminada;
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

            int total = Op.Recibidas.Count + Op.Pendientes.Count + Op.BajaCalidad.Count;
            textoProgreso.text = "Transmisiones recibidas: " + Op.Recibidas.Count + " de " + total;
            botonTerminar.GetComponentInChildren<TMP_Text>().text = Op.Completa || Op.Terminada ? "Ver informe" : "Terminar misión";
        }
    }
}
