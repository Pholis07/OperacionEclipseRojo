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
        public bool EvitadoPorSatelite;   // el satélite no se vio afectado (Chandra y la orientación)
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
        bool lecturaRuidosa;   // tormenta: el próximo escaneo frío se lee como tibio

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
        public int MemoriaCapacidad => nivel.memoriaBloques + (Satelite != null ? Satelite.memoriaExtra : 0);

        // ---------- Satélite (mecánicas aprobadas el 2026-11-14) ----------

        /// <summary>El satélite de la Carga. Sin Carga no hay satélite y nada cambia.</summary>
        public SateliteDatos Satelite => Carga != null ? Carga.Satelite : null;

        public int CostoCambioAltitud => Satelite != null && Satelite.combustiblePorCambioAltitud >= 0
            ? Satelite.combustiblePorCambioAltitud : reglas.combustiblePorCambioAltitud;
        public int VatiosTransmisor => Satelite != null && Satelite.vatiosTransmisor >= 0 ? Satelite.vatiosTransmisor : reglas.vatiosTransmisor;
        public int CicloContacto => Satelite != null && Satelite.cicloContacto > 0 ? Satelite.cicloContacto : reglas.cicloContacto;

        /// <summary>AztechSat-1: puede enviar por la constelación aunque la ventana esté cerrada (cuesta órbitas).</summary>
        public bool PuedeEnviarSinVentana => Satelite != null && Satelite.envioSinVentana;

        /// <summary>LLCD en la tormenta: el polvo bloquea el láser en esta órbita.</summary>
        public bool EnvioBloqueado { get; private set; }

        /// <summary>Cuánto más lejos lee este instrumento con el satélite (1 = igual).</summary>
        float FactorSatelite(string instrumentoId) =>
            Satelite != null && Satelite.instrumentosConAlcance != null && System.Array.IndexOf(Satelite.instrumentosConAlcance, instrumentoId) >= 0
                ? Satelite.factorAlcance : 1f;
        public int MemoriaUsada { get; private set; }
        public bool MemoriaLlena => ConMemoria && MemoriaUsada >= MemoriaCapacidad;
        /// <summary>Transmisiones encontradas que todavía no se enviaron a la Tierra.</summary>
        public readonly List<string> EnMemoria = new List<string>();
        public int OrbitasTranscurridas { get; private set; }
        bool contactoForzado;

        /// <summary>Evento que espera la respuesta del jugador (v3). Mientras tanto la Operación está en pausa.</summary>
        public EventoActivo Decision { get; private set; }
        public bool EsperandoDecision => Decision != null;

        /// <summary>Petición de la NASA aceptada: punto a escanear y si ya se cumplió.</summary>
        public bool PeticionAceptada { get; private set; }
        public Vector2 PuntoPeticion { get; private set; }
        public bool PeticionCumplida { get; private set; }
        public int PuntosPeticion { get; private set; }
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
            // Los objetivos son de la v3: sin Carga no hay (y los puntos del modo anterior no cambian).
            Objetivos = ConMemoria ? SpaceMission.Objetivos.Elegir(catalogo, azar, Combustible) : new List<ObjetivoDatos>();
        }

        // ---------- Energía ----------

        public int LimiteW =>
            nivel.potenciaDisponibleW + (Satelite != null ? Satelite.limiteExtraW : 0) - Eventos.Where(e => !e.Resuelto &&
                (e.Datos.efecto == EfectoEvento.ReducirLimite || e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal))
                .Sum(e => e.Datos.vatios);

        public int ConsumoW => nivel.consumoBaseW + (Satelite != null ? Satelite.consumoExtraW : 0)
                               + Encendidos.Sum(id => catalogo.Instrumento(id).ConsumoTotalW);

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
            if (Terminada || EsperandoDecision || nueva == Altitud || Combustible < CostoCambioAltitud) return false;
            Combustible -= CostoCambioAltitud;
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
                EnvioBloqueado = false;
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
            if (Terminada || EsperandoDecision || !Encendidos.Contains(instrumentoId) || MemoriaLlena) return null;
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
                float d = Vector2.Distance(punto, Anomalia) / (FactorLectura * FactorSatelite(instrumentoId));
                lectura = d <= reglas.radioDeteccion ? Lectura.Encontrada
                    : d <= reglas.distanciaCaliente ? Lectura.Caliente
                    : d <= reglas.distanciaTibio ? Lectura.Tibio
                    : Lectura.Frio;
                if (lecturaRuidosa && lectura == Lectura.Frio) lectura = Lectura.Tibio;
            }
            lecturaRuidosa = false;

            if (PeticionAceptada && !PeticionCumplida
                && Vector2.Distance(punto, PuntoPeticion) / (FactorLectura * FactorSatelite(instrumentoId)) <= reglas.radioDeteccion)
                PeticionCumplida = true;

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
        public bool ContactoAbierto => OrbitasParaContacto == 0;

        /// <summary>
        /// Órbitas que faltan para la próxima ventana de contacto (0 = abierta ahora). Se abre 1 órbita de cada
        /// «cicloContacto» y, además, siempre en la última órbita, para poder enviar lo que quede en memoria.
        /// </summary>
        public int OrbitasParaContacto => contactoForzado ? 0
            : System.Math.Min(CicloContacto - 1 - OrbitasTranscurridas % CicloContacto, System.Math.Max(0, Orbitas - 1));

        /// <summary>
        /// Deja pasar una órbita sin escanear (por ejemplo, con la memoria llena hasta la próxima ventana).
        /// No dispara eventos: los eventos van tras los escaneos.
        /// </summary>
        public bool Esperar()
        {
            if (Terminada || EsperandoDecision) return false;
            GastarOrbitas(1);
            return true;
        }

        /// <summary>Abre una ventana de contacto en esta órbita (respuesta a la ventana despejada).</summary>
        public void AbrirContacto() => contactoForzado = true;

        /// <summary>
        /// Envía a la Tierra lo que hay en memoria. Solo con la ventana abierta y algo que enviar; no gasta órbita.
        /// El transmisor consume energía extra: si no cabe, la nave se apaga y no se envía nada.
        /// </summary>
        public bool Enviar()
        {
            if (Terminada || EsperandoDecision || !ConMemoria || EnvioBloqueado || MemoriaUsada == 0) return false;
            if (!ContactoAbierto && !PuedeEnviarSinVentana) return false;
            bool porConstelacion = !ContactoAbierto;
            if (ConsumoW + VatiosTransmisor > LimiteW)
            {
                Apagon();
                return false;
            }
            Recibidas.AddRange(evaluacion.Transmisiones.Where(EnMemoria.Contains));
            EnMemoria.Clear();
            MemoriaUsada = 0;
            RegistrarSiCompleta(Orbitas);
            if (porConstelacion) GastarOrbitas(Satelite.orbitasEnvioSinVentana);   // la constelación transmite despacio
            return true;
        }

        /// <summary>El jugador termina la misión (o se acabaron las órbitas): lo pendiente llega con baja calidad.</summary>
        public void Terminar()
        {
            if (Terminada) return;
            Terminada = true;
            Decision = null;
            Encendidos.Clear();
            foreach (var id in evaluacion.Transmisiones.Where(id => Pendientes.ContainsKey(id) || EnMemoria.Contains(id)))
                BajaCalidad.Add(id);
            Pendientes.Clear();
            EnMemoria.Clear();
        }

        // ---------- Eventos y herramientas ----------

        void ProgramarEventosAlAzar()
        {
            // Sin Carga (modo anterior a la v3) no salen los eventos que solo tienen sentido con decisiones.
            var posibles = catalogo.eventos.Where(e => ConMemoria ||
                (e.efecto != EfectoEvento.Peticion && e.efecto != EfectoEvento.Tormenta)).ToList();
            int cantidad = Mathf.Min(nivel.eventosPorMision, posibles.Count);
            var mezclados = posibles.OrderBy(_ => azar.Next()).Take(cantidad).ToList();
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

        /// <summary>
        /// Hace ocurrir un evento. Con Carga (v3) y respuestas en los datos, la Operación se pausa hasta
        /// que el jugador elige con Decidir; si no, el efecto se aplica al momento, como antes.
        /// </summary>
        public void AplicarEvento(EventoDatos datos)
        {
            var e = new EventoActivo { Datos = datos, OrbitasRestantes = datos.orbitas };
            UltimoEvento = e;
            if (datos.efecto == EfectoEvento.AnularLectura && Satelite != null && Satelite.inmuneOrientacion)
            {
                e.Resuelto = e.EvitadoPorSatelite = true;   // ni pausa ni efecto
                Eventos.Add(e);
                return;
            }
            if (ConMemoria && datos.opciones.Count > 0)
            {
                Decision = e;
                return;
            }
            Eventos.Add(e);
            AplicarEfecto(e, datos.orbitas);
        }

        /// <summary>Respuestas que se muestran: la de herramienta solo si la herramienta subió a la nave.</summary>
        public IEnumerable<OpcionEvento> Opciones => Decision == null ? Enumerable.Empty<OpcionEvento>()
            : Decision.Datos.opciones.Where(o => o.efecto != EfectoOpcion.UsarHerramienta || TieneHerramienta(Decision.Datos.herramientaId));

        /// <summary>Se puede elegir: se muestra, alcanza el combustible y la herramienta no se usó.</summary>
        public bool PuedeElegir(OpcionEvento o) =>
            Opciones.Contains(o) && Combustible >= o.combustible
            && (o.efecto != EfectoOpcion.UsarHerramienta || !HerramientasUsadas.Contains(Decision.Datos.herramientaId));

        /// <summary>Responde al evento en pausa. Devuelve false si esa respuesta no se puede elegir.</summary>
        public bool Decidir(OpcionEvento o)
        {
            if (Terminada || !PuedeElegir(o)) return false;
            var e = Decision;
            var d = e.Datos;
            Decision = null;
            Combustible -= o.combustible;
            Eventos.Add(e);

            switch (o.efecto)
            {
                case EfectoOpcion.UsarHerramienta:
                    HerramientasUsadas.Add(d.herramientaId);
                    e.Resuelto = true;
                    break;
                case EfectoOpcion.Acortar:
                    e.OrbitasRestantes = o.duracion;
                    AplicarEfecto(e, o.duracion);
                    break;
                case EfectoOpcion.Aguantar:
                    AplicarEfecto(e, d.orbitas);
                    if (d.efecto == EfectoEvento.Tormenta && Satelite != null && Satelite.tormentaBloqueaEnvio) EnvioBloqueado = true;
                    break;
                case EfectoOpcion.GanarOrbitas:
                    Orbitas += o.duracion;
                    e.Resuelto = true;
                    break;
                case EfectoOpcion.AbrirContacto:
                    AbrirContacto();
                    e.Resuelto = true;
                    break;
                case EfectoOpcion.AceptarObjetivo:
                    PeticionAceptada = true;
                    PuntosPeticion = d.puntos;
                    PuntoPeticion = new Vector2(Aleatorio(0.15f, 0.85f), Aleatorio(0.15f, 0.85f));
                    e.Resuelto = true;
                    break;
                case EfectoOpcion.BajarAltitud:
                    Altitud = Altitud.Baja;
                    e.Resuelto = true;
                    break;
                default:   // Resolver
                    e.Resuelto = true;
                    break;
            }
            if (o.orbitas > 0) GastarOrbitas(o.orbitas);
            return true;
        }

        /// <summary>Coloca el punto de la petición de la NASA en un sitio fijo. Para las pruebas.</summary>
        public void PonerPuntoPeticion(Vector2 punto) => PuntoPeticion = punto;

        void AplicarEfecto(EventoActivo e, int orbitas)
        {
            var datos = e.Datos;
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
                        Bloqueados[mayor] = orbitas;
                    }
                    else e.Resuelto = true; // no había nada encendido: no pasa nada
                    break;
                case EfectoEvento.AnularLectura:
                    sinLecturaProxima = true;
                    break;
                case EfectoEvento.GanarOrbitas:
                    Orbitas += orbitas;
                    e.Resuelto = true;
                    break;
                case EfectoEvento.Tormenta:
                    lecturaRuidosa = Satelite == null || !Satelite.inmuneTormenta;
                    e.Resuelto = true;
                    break;
                default:   // Peticion rechazada
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
            if (Terminada || EsperandoDecision || !TieneHerramienta(herramientaId) || HerramientasUsadas.Contains(herramientaId)) return false;
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
