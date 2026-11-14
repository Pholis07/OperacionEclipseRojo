using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Lectura de un escaneo según la distancia a la anomalía.</summary>
    public enum Lectura { Frio, Tibio, Caliente, Encontrada, SinLectura }

    /// <summary>Altitud de la órbita durante la Operación (v3).</summary>
    public enum Altitud { Normal, Baja }

    /// <summary>Un evento que ya ocurrió en esta misión.</summary>
    public class EventoActivo
    {
        public EventoDatos Datos;
        public int OrbitasRestantes;   // solo para los temporales
        public bool Resuelto;
    }

    /// <summary>
    /// Fase de Operación (diseño aprobado, PROPUESTA-GAMEPLAY.md v2): energía con límite que los eventos
    /// reducen, instrumentos que se encienden y apagan, apagón si el consumo supera el límite (cuesta órbitas
    /// y créditos, no la partida), escaneo del mapa que gasta órbitas, eventos aleatorios y herramientas.
    /// La categoría del resultado no cambia aquí: solo cuántas transmisiones llegan bien y los recursos.
    /// </summary>
    public class Operacion
    {
        readonly CatalogoDatos catalogo;
        readonly ReglasOperacionDatos reglas;
        readonly MisionDatos nivel;
        readonly Evaluacion evaluacion;
        readonly System.Random azar;

        readonly List<EventoDatos> eventosProgramados = new List<EventoDatos>();
        readonly List<int> momentosEvento = new List<int>();   // número de escaneo tras el que ocurre cada evento
        bool sinLecturaProxima;

        public Vector2 Anomalia { get; private set; }
        public int Orbitas { get; private set; }
        public int Escaneos { get; private set; }
        public int Apagones { get; private set; }
        public int CreditosPerdidos { get; private set; }
        public bool Terminada { get; private set; }

        /// <summary>Lo que se cargó antes de lanzar (v3). Null = sin pantalla de Carga: todas las herramientas a bordo.</summary>
        public Carga Carga { get; }
        public int Combustible { get; private set; }
        public Altitud Altitud { get; private set; }
        /// <summary>Veces que la nave subió sola porque no le quedaba combustible para seguir en baja.</summary>
        public int SubidasForzadas { get; private set; }

        /// <summary>Con Carga (v3) hay memoria y envío a Tierra; sin ella, lo encontrado llega al momento, como antes.</summary>
        public bool ConMemoria => Carga != null;
        public int MemoriaCapacidad => nivel.memoriaBloques;
        public int MemoriaUsada { get; private set; }
        public bool MemoriaLlena => ConMemoria && MemoriaUsada >= MemoriaCapacidad;
        /// <summary>Transmisiones encontradas que todavía no se enviaron a la Tierra.</summary>
        public readonly List<string> EnMemoria = new List<string>();
        public int OrbitasTranscurridas { get; private set; }
        bool contactoForzado;
        public int CreditosCarga => Carga != null ? Carga.CreditosGastados : 0;
        public readonly List<ObjetivoDatos> Objetivos;
        /// <summary>Escaneos hechos y órbitas que quedaban cuando llegó la última transmisión (0 si no llegó).</summary>
        public int EscaneosAlCompletar { get; private set; }
        public int OrbitasAlCompletar { get; private set; }

        public readonly HashSet<string> Encendidos = new HashSet<string>();
        public readonly Dictionary<string, int> Bloqueados = new Dictionary<string, int>();
        public readonly HashSet<string> HerramientasUsadas = new HashSet<string>();
        public readonly List<EventoActivo> Eventos = new List<EventoActivo>();

        /// <summary>Transmisiones recibidas bien, en el orden en que llegaron (TX-00 primero).</summary>
        public readonly List<string> Recibidas = new List<string>();
        /// <summary>Transmisiones que faltan por encontrar (id de transmisión → id de instrumento).</summary>
        public readonly Dictionary<string, string> Pendientes = new Dictionary<string, string>();
        /// <summary>Transmisiones que llegaron con baja calidad porque se acabaron las órbitas.</summary>
        public readonly List<string> BajaCalidad = new List<string>();

        /// <summary>Lo último que pasó, para mostrarlo en pantalla.</summary>
        public EventoActivo UltimoEvento { get; private set; }

        public Operacion(Evaluacion evaluacion, Configuracion config, CatalogoDatos catalogo, MisionDatos nivel, int semilla,
            Carga carga = null)
        {
            Carga = carga;
            Combustible = carga != null ? carga.Combustible : 0;
            this.evaluacion = evaluacion;
            enLaNave = new HashSet<string>(config.InstrumentoIds);
            this.catalogo = catalogo;
            this.nivel = nivel != null ? nivel : catalogo.mision;
            reglas = catalogo.reglas;
            azar = new System.Random(semilla);

            Orbitas = this.nivel.orbitasNavegacion;
            Anomalia = new Vector2(Aleatorio(0.15f, 0.85f), Aleatorio(0.15f, 0.85f));

            foreach (var id in evaluacion.Transmisiones)
            {
                var t = catalogo.Transmision(id);
                if (string.IsNullOrEmpty(t.instrumentoId)) Recibidas.Add(id); // TX-00 llega al empezar
                else Pendientes[id] = t.instrumentoId;
            }
            ProgramarEventosAlAzar();
            Objetivos = SpaceMission.Objetivos.Elegir(catalogo, azar);
        }

        // ---------- Energía ----------

        public int LimiteW =>
            nivel.potenciaDisponibleW - Eventos.Where(e => !e.Resuelto &&
                (e.Datos.efecto == EfectoEvento.ReducirLimite || e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal))
                .Sum(e => e.Datos.vatios);

        public int ConsumoW => nivel.consumoBaseW + Encendidos.Sum(id => catalogo.Instrumento(id).ConsumoTotalW);

        readonly HashSet<string> enLaNave;

        /// <summary>Instrumentos que el jugador eligió en la configuración (los únicos que puede encender).</summary>
        public IEnumerable<string> Instrumentos => catalogo.instrumentos.Select(i => i.id).Where(EnLaNave);

        public bool EnLaNave(string id) => enLaNave.Contains(id);

        /// <summary>Enciende un instrumento. Devuelve false si no se pudo (bloqueado o no está en la nave).
        /// Si el consumo pasa el límite, la nave se apaga.</summary>
        public bool Encender(string id)
        {
            if (Terminada || !EnLaNave(id) || Bloqueados.ContainsKey(id)) return false;
            Encendidos.Add(id);
            ComprobarEnergia();
            return true;
        }

        public void Apagar(string id) => Encendidos.Remove(id);

        void ComprobarEnergia()
        {
            if (ConsumoW > LimiteW) Apagon();
        }

        void Apagon()
        {
            Apagones++;
            CreditosPerdidos += reglas.creditosPorReinicio;
            Encendidos.Clear();
            GastarOrbitas(reglas.orbitasPerdidasPorApagon);
        }

        // ---------- Combustible y altitud ----------

        /// <summary>Cambia de altitud pagando combustible. Devuelve false si ya estaba ahí o no alcanza.</summary>
        public bool CambiarAltitud(Altitud nueva)
        {
            if (Terminada || nueva == Altitud || Combustible < reglas.combustiblePorCambioAltitud) return false;
            Combustible -= reglas.combustiblePorCambioAltitud;
            Altitud = nueva;
            return true;
        }

        /// <summary>En baja, las lecturas alcanzan más lejos.</summary>
        float FactorLectura => Altitud == Altitud.Baja ? reglas.factorAltitudBaja : 1f;

        /// <summary>El arrastre de la órbita baja gasta combustible; si no queda, la nave sube sola y pierde órbitas.</summary>
        int Arrastre()
        {
            if (Altitud != Altitud.Baja) return 0;
            if (Combustible >= reglas.combustiblePorOrbitaBaja)
            {
                Combustible -= reglas.combustiblePorOrbitaBaja;
                return 0;
            }
            Altitud = Altitud.Normal;
            SubidasForzadas++;
            return reglas.orbitasPerdidasSinCombustible;
        }

        // ---------- Tiempo y escaneo ----------

        void GastarOrbitas(int cantidad)
        {
            for (int i = 0; i < cantidad && !Terminada; i++)
            {
                cantidad += Arrastre();
                Orbitas--;
                OrbitasTranscurridas++;
                contactoForzado = false;
                foreach (var e in Eventos.Where(e => !e.Resuelto && e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal))
                    if (--e.OrbitasRestantes <= 0) e.Resuelto = true;
                foreach (var id in Bloqueados.Keys.ToList())
                    if (--Bloqueados[id] <= 0) Bloqueados.Remove(id);
                if (Orbitas <= 0) Terminar();
            }
        }

        /// <summary>Escanea el punto (coordenadas de 0 a 1) con un instrumento encendido.</summary>
        public Lectura? Escanear(string instrumentoId, Vector2 punto)
        {
            if (Terminada || !Encendidos.Contains(instrumentoId) || MemoriaLlena) return null;
            if (ConMemoria) MemoriaUsada++;
            Escaneos++;

            Lectura lectura;
            if (sinLecturaProxima)
            {
                sinLecturaProxima = false;
                lectura = Lectura.SinLectura;
            }
            else
            {
                float d = Vector2.Distance(punto, Anomalia) / FactorLectura;
                lectura = d <= reglas.radioDeteccion ? Lectura.Encontrada
                    : d <= reglas.distanciaCaliente ? Lectura.Caliente
                    : d <= reglas.distanciaTibio ? Lectura.Tibio
                    : Lectura.Frio;
            }

            if (lectura == Lectura.Encontrada)
            {
                foreach (var par in Pendientes.Where(p => p.Value == instrumentoId).ToList())
                {
                    (ConMemoria ? EnMemoria : Recibidas).Add(par.Key);
                    Pendientes.Remove(par.Key);
                }
                RegistrarSiCompleta(Orbitas - reglas.orbitasPorEscaneo);
            }

            GastarOrbitas(reglas.orbitasPorEscaneo);
            if (!Terminada && momentosEvento.Count > 0 && Escaneos >= momentosEvento[0])
            {
                momentosEvento.RemoveAt(0);
                var datos = eventosProgramados[0];
                eventosProgramados.RemoveAt(0);
                AplicarEvento(datos);
            }
            return lectura;
        }

        /// <summary>Todas las transmisiones ya llegaron: se puede pasar al informe.</summary>
        public bool Completa => Pendientes.Count == 0 && EnMemoria.Count == 0;

        void RegistrarSiCompleta(int orbitasQueQuedan)
        {
            if (!Completa || EscaneosAlCompletar != 0) return;
            EscaneosAlCompletar = Escaneos;
            OrbitasAlCompletar = orbitasQueQuedan;
        }

        // ---------- Memoria y envío a Tierra ----------

        /// <summary>La ventana de contacto con la Tierra está abierta en esta órbita.</summary>
        public bool ContactoAbierto => contactoForzado || OrbitasParaContacto == 0;

        /// <summary>Órbitas que faltan para la próxima ventana de contacto (0 = abierta ahora).</summary>
        public int OrbitasParaContacto => contactoForzado ? 0
            : reglas.cicloContacto - 1 - OrbitasTranscurridas % reglas.cicloContacto;

        /// <summary>Abre una ventana de contacto en esta órbita (respuesta a la ventana despejada).</summary>
        public void AbrirContacto() => contactoForzado = true;

        /// <summary>
        /// Envía a la Tierra lo que hay en memoria. Solo con la ventana abierta y algo que enviar; no gasta órbita.
        /// El transmisor consume energía extra: si no cabe, la nave se apaga y no se envía nada.
        /// </summary>
        public bool Enviar()
        {
            if (Terminada || !ConMemoria || !ContactoAbierto || MemoriaUsada == 0) return false;
            if (ConsumoW + reglas.vatiosTransmisor > LimiteW)
            {
                Apagon();
                return false;
            }
            Recibidas.AddRange(evaluacion.Transmisiones.Where(EnMemoria.Contains));
            EnMemoria.Clear();
            MemoriaUsada = 0;
            RegistrarSiCompleta(Orbitas);
            return true;
        }

        /// <summary>El jugador termina la misión (o se acabaron las órbitas): lo pendiente llega con baja calidad.</summary>
        public void Terminar()
        {
            if (Terminada) return;
            Terminada = true;
            Encendidos.Clear();
            foreach (var id in evaluacion.Transmisiones.Where(id => Pendientes.ContainsKey(id) || EnMemoria.Contains(id)))
                BajaCalidad.Add(id);
            Pendientes.Clear();
            EnMemoria.Clear();
        }

        // ---------- Eventos y herramientas ----------

        void ProgramarEventosAlAzar()
        {
            int cantidad = Mathf.Min(nivel.eventosPorMision, catalogo.eventos.Count);
            var mezclados = catalogo.eventos.OrderBy(_ => azar.Next()).Take(cantidad).ToList();
            int ultimo = Mathf.Max(1, nivel.orbitasNavegacion - 2);
            var momentos = Enumerable.Range(1, ultimo).OrderBy(_ => azar.Next()).Take(cantidad).OrderBy(m => m).ToList();
            ProgramarEventos(mezclados, momentos);
        }

        /// <summary>Fija qué eventos ocurren y tras qué escaneo. Lo usan las pruebas para no depender del azar.</summary>
        public void ProgramarEventos(IList<EventoDatos> eventos, IList<int> despuesDelEscaneo)
        {
            eventosProgramados.Clear();
            momentosEvento.Clear();
            eventosProgramados.AddRange(eventos);
            momentosEvento.AddRange(despuesDelEscaneo);
        }

        /// <summary>Coloca la anomalía en un punto fijo. Para las pruebas.</summary>
        public void PonerAnomalia(Vector2 punto) => Anomalia = punto;

        public void AplicarEvento(EventoDatos datos)
        {
            var e = new EventoActivo { Datos = datos, OrbitasRestantes = datos.orbitas };
            Eventos.Add(e);
            UltimoEvento = e;

            switch (datos.efecto)
            {
                case EfectoEvento.ReducirLimite:
                case EfectoEvento.ReducirLimiteTemporal:
                    ComprobarEnergia();
                    break;
                case EfectoEvento.BloquearInstrumento:
                    var mayor = Encendidos.OrderByDescending(id => catalogo.Instrumento(id).ConsumoTotalW).FirstOrDefault();
                    if (mayor != null)
                    {
                        Encendidos.Remove(mayor);
                        Bloqueados[mayor] = datos.orbitas;
                    }
                    else e.Resuelto = true; // no había nada encendido: no pasa nada
                    break;
                case EfectoEvento.AnularLectura:
                    sinLecturaProxima = true;
                    break;
                case EfectoEvento.GanarOrbitas:
                    Orbitas += datos.orbitas;
                    e.Resuelto = true;
                    break;
            }
        }

        /// <summary>Herramientas que ahora mismo resolverían un evento activo.</summary>
        public IEnumerable<string> HerramientasUtiles =>
            Eventos.Where(e => !e.Resuelto && !string.IsNullOrEmpty(e.Datos.herramientaId)
                               && TieneHerramienta(e.Datos.herramientaId)
                               && !HerramientasUsadas.Contains(e.Datos.herramientaId))
                   .Select(e => e.Datos.herramientaId).Distinct();

        /// <summary>La herramienta va a bordo: se cargó, o no hubo pantalla de Carga.</summary>
        public bool TieneHerramienta(string id) => Carga == null || Carga.Lleva(id);

        /// <summary>Usa una herramienta (una vez por misión). Devuelve false si no resuelve nada.</summary>
        public bool UsarHerramienta(string herramientaId)
        {
            if (Terminada || !TieneHerramienta(herramientaId) || HerramientasUsadas.Contains(herramientaId)) return false;
            var e = Eventos.FirstOrDefault(x => !x.Resuelto && x.Datos.herramientaId == herramientaId);
            if (e == null) return false;

            HerramientasUsadas.Add(herramientaId);
            e.Resuelto = true;
            if (e.Datos.efecto == EfectoEvento.BloquearInstrumento) Bloqueados.Clear();
            if (e.Datos.efecto == EfectoEvento.AnularLectura) sinLecturaProxima = false;
            return true;
        }

        float Aleatorio(float min, float max) => min + (float)azar.NextDouble() * (max - min);
    }
}
