using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Lectura de una observación según la distancia a la anomalía.</summary>
    public enum Lectura { Frio, Tibio, Caliente, Encontrada, SinLectura }

    /// <summary>Altitud de la órbita durante la Operación.</summary>
    public enum Altitud { Normal, Baja }

    /// <summary>Un evento que ya ocurrió en esta misión.</summary>
    public class EventoActivo
    {
        public EventoDatos Datos;
        public int OrbitasRestantes;   // meses que le quedan (solo los temporales)
        public bool Resuelto;
        public bool EvitadoPorSatelite;   // el satélite no se vio afectado (Chandra y la orientación)
    }

    /// <summary>Datos que un instrumento registró un mes. Ocupa memoria hasta que se envía.</summary>
    public class Registro
    {
        public string Instrumento;
        public Vector2 Region;
        public int Mes;
        public Lectura Lectura;
        public float Calidad;      // 0 a 100
        public int Bloques;
        /// <summary>Transmisiones que este registro aporta al enviarse (vacío si no es evidencia válida).</summary>
        public readonly List<string> Evidencias = new List<string>();
    }

    /// <summary>Lo que pasó al resolver un mes. La interfaz y la animación solo leen esto: no calculan nada.</summary>
    public class ResultadoMes
    {
        public bool Valido;
        public string Motivo;        // por qué no se resolvió (solo si no es válido)
        public int Mes;
        public int DiasUtiles;
        /// <summary>Factores de la calidad de este mes: base × días/30 × tormenta × altitud (para el resumen).</summary>
        public float CalidadBase, FactorTormenta = 1f, FactorAltitud = 1f;
        public bool Maniobra;
        public Altitud Altitud;
        public int CreditosGastados;
        public int CombustibleGastado;
        public readonly List<Registro> Observaciones = new List<Registro>();
        public readonly List<string> SinMemoria = new List<string>();   // instrumentos cuyo registro no cupo
        public readonly List<string> SinRegion = new List<string>();    // encendidos sin región: no observaron
        public bool EnvioPedido;
        public string EnvioFallido;  // por qué no se envió (null si se envió o no se pidió)
        public int BloquesEnviados;
        public readonly List<string> Enviadas = new List<string>();     // transmisiones que llegaron a la Tierra
        public bool SubidaForzada;
        public EventoActivo Evento;  // evento de cierre, si lo hubo
        public bool Termino;
    }

    /// <summary>
    /// Fase de Operación por meses (paquete «Operación Eclipse Rojo» v1.0, PROPUESTA-GAMEPLAY.md v4).
    /// El jugador prepara el plan del mes (qué instrumentos van encendidos, qué región observa cada uno, si
    /// maniobra y si envía) y <see cref="ResolverMes"/> lo calcula una sola vez, en este orden:
    /// validar → maniobrar y cobrar → días útiles → observar → almacenar → transmitir → avanzar → evento de cierre.
    /// Editar el plan no gasta nada; solo resolver cobra. El resultado científico depende de lo enviado.
    /// </summary>
    public class Operacion
    {
        readonly CatalogoDatos catalogo;
        readonly ReglasOperacionDatos reglas;
        readonly MisionDatos nivel;
        readonly Evaluacion evaluacion;
        readonly System.Random azar;
        readonly HashSet<string> enLaNave;

        readonly List<EventoDatos> eventosProgramados = new List<EventoDatos>();
        readonly List<int> momentosEvento = new List<int>();   // mes tras cuyo cierre ocurre cada evento
        bool sinLecturaProxima;        // pérdida de orientación: el mes siguiente no da lecturas
        bool calidadReducidaProxima;   // tormenta: la calidad del mes siguiente baja
        int mesContactoForzado;        // mes con ventana extra (ventana despejada); 0 = ninguno
        int diasPerdidos;              // días que el mes siguiente ya tiene perdidos (apagón, desvío, subida forzada)

        public Vector2 Anomalia { get; private set; }
        public int MesesRestantes { get; private set; }
        public int MesesResueltos { get; private set; }
        /// <summary>El mes que se está planificando (empieza en 1).</summary>
        public int Mes => MesesResueltos + 1;
        public int Observaciones { get; private set; }
        public int Apagones { get; private set; }
        public int CreditosPerdidos { get; private set; }
        public int CreditosManiobras { get; private set; }
        public bool Terminada { get; private set; }

        /// <summary>Lo que se cargó antes de lanzar. Null (solo en pruebas): todas las herramientas, sin satélite ni combustible.</summary>
        public Carga Carga { get; }
        public SateliteDatos Satelite => Carga != null ? Carga.Satelite : null;
        public int CreditosCarga => Carga != null ? Carga.CreditosGastados : 0;
        public int Combustible { get; private set; }
        public Altitud Altitud { get; private set; }
        public int SubidasForzadas { get; private set; }

        /// <summary>Lo que ya se sabe del mes siguiente (para mostrar los modificadores activos).</summary>
        public bool SinLecturaProxima => sinLecturaProxima;
        public bool CalidadReducidaProxima => calidadReducidaProxima;
        public int DiasPerdidosProximos => diasPerdidos;

        // ---------- Plan del mes (editarlo no gasta nada) ----------

        /// <summary>Instrumentos encendidos. Siguen encendidos de un mes al siguiente.</summary>
        public readonly HashSet<string> Encendidos = new HashSet<string>();
        /// <summary>Región (punto del mapa, de 0 a 1) que observa cada instrumento. También se conserva.</summary>
        public readonly Dictionary<string, Vector2> Regiones = new Dictionary<string, Vector2>();
        /// <summary>Cambio de altitud pedido para este mes, o null.</summary>
        public Altitud? ManiobraPlaneada { get; private set; }
        /// <summary>Enviar a la Tierra al final de este mes.</summary>
        public bool TransmitirPlaneado;

        public readonly Dictionary<string, int> Bloqueados = new Dictionary<string, int>();
        public readonly HashSet<string> HerramientasUsadas = new HashSet<string>();
        public readonly List<EventoActivo> Eventos = new List<EventoActivo>();

        /// <summary>Todas las observaciones de la misión, enviadas o no (para la guía y el mapa).</summary>
        public readonly List<Registro> Historial = new List<Registro>();
        /// <summary>Registros en la memoria de la nave, en el orden en que se tomaron.</summary>
        public readonly List<Registro> Memoria = new List<Registro>();
        /// <summary>Transmisiones recibidas en la Tierra, en el orden en que llegaron (TX-00 primero).</summary>
        public readonly List<string> Recibidas = new List<string>();
        /// <summary>Evidencias que todavía no se observaron con calidad suficiente (transmisión → instrumento).</summary>
        public readonly Dictionary<string, string> Pendientes = new Dictionary<string, string>();

        public EventoActivo UltimoEvento { get; private set; }
        /// <summary>Evento que espera la respuesta del jugador. Mientras tanto no se puede resolver el mes.</summary>
        public EventoActivo Decision { get; private set; }
        public bool EsperandoDecision => Decision != null;

        public bool PeticionAceptada { get; private set; }
        public Vector2 PuntoPeticion { get; private set; }
        public bool PeticionCumplida { get; private set; }
        public int PuntosPeticion { get; private set; }
        public readonly List<ObjetivoDatos> Objetivos;
        /// <summary>Observaciones hechas y meses que quedaban cuando llegó la última evidencia (0 si no llegó).</summary>
        public int ObservacionesAlCompletar { get; private set; }
        public int MesesAlCompletar { get; private set; }

        public Operacion(Evaluacion evaluacion, Configuracion config, CatalogoDatos catalogo, MisionDatos nivel, int semilla,
            Carga carga = null)
        {
            Carga = carga;
            Combustible = carga != null ? carga.Combustible : 0;
            this.evaluacion = evaluacion;
            enLaNave = new HashSet<string>(config.InstrumentoIds);
            var orbita = string.IsNullOrEmpty(config.OrbitaId) ? null : catalogo.Orbita(config.OrbitaId);
            sinAlcance = new HashSet<string>(orbita != null ? orbita.instrumentosSinAlcance : new List<string>());
            this.catalogo = catalogo;
            this.nivel = nivel != null ? nivel : catalogo.mision;
            reglas = catalogo.reglas;
            azar = new System.Random(semilla);

            MesesRestantes = this.nivel.orbitasNavegacion;
            Anomalia = new Vector2(Aleatorio(0.15f, 0.85f), Aleatorio(0.15f, 0.85f));   // fija toda la misión

            foreach (var id in evaluacion.Transmisiones)
            {
                var t = catalogo.Transmision(id);
                if (string.IsNullOrEmpty(t.instrumentoId)) Recibidas.Add(id); // TX-00 llega al empezar
                else Pendientes[id] = t.instrumentoId;
            }
            ProgramarEventosAlAzar();
            Objetivos = SpaceMission.Objetivos.Elegir(catalogo, azar, Combustible);
        }

        // ---------- Satélite ----------

        public int CostoCambioAltitud => Satelite != null && Satelite.combustiblePorCambioAltitud >= 0
            ? Satelite.combustiblePorCambioAltitud : reglas.combustiblePorCambioAltitud;
        public int VatiosTransmisor => Satelite != null && Satelite.vatiosTransmisor >= 0 ? Satelite.vatiosTransmisor : reglas.vatiosTransmisor;
        public int CicloContacto => Satelite != null && Satelite.cicloContacto > 0 ? Satelite.cicloContacto : reglas.cicloContacto;
        /// <summary>AztechSat-1: puede enviar por la constelación con la ventana cerrada, pero pocos bloques.</summary>
        public bool PuedeEnviarSinVentana => Satelite != null && Satelite.envioSinVentana;
        /// <summary>LLCD tras seguir en una tormenta: el polvo bloquea el láser este mes.</summary>
        public bool EnvioBloqueado { get; private set; }

        float FactorSatelite(string instrumentoId) =>
            Satelite != null && Satelite.instrumentosConAlcance != null && System.Array.IndexOf(Satelite.instrumentosConAlcance, instrumentoId) >= 0
                ? Satelite.factorAlcance : 1f;

        // ---------- Potencia e instrumentos ----------

        public int LimiteW =>
            nivel.potenciaDisponibleW + (Satelite != null ? Satelite.limiteExtraW : 0) - Eventos.Where(e => !e.Resuelto &&
                (e.Datos.efecto == EfectoEvento.ReducirLimite || e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal))
                .Sum(e => e.Datos.vatios);

        int ConsumoBaseW => nivel.consumoBaseW + (Satelite != null ? Satelite.consumoExtraW : 0);
        public int ConsumoW => ConsumoBaseW + Encendidos.Sum(id => catalogo.Instrumento(id).ConsumoTotalW);

        /// <summary>Instrumentos de la nave, en el orden del catálogo (el orden en que observan y guardan).</summary>
        public IEnumerable<string> Instrumentos => catalogo.instrumentos.Select(i => i.id).Where(EnLaNave);
        public bool EnLaNave(string id) => enLaNave.Contains(id);

        /// <summary>Va a bordo pero no puede trabajar desde esta órbita (OrbitaDatos.instrumentosSinAlcance).</summary>
        public bool SinAlcance(string id) => sinAlcance.Contains(id);
        readonly HashSet<string> sinAlcance;

        /// <summary>Enciende un instrumento. No se puede si está bloqueado, no va en la nave, no trabaja desde esta órbita
        /// o no cabe en el límite de potencia.</summary>
        public bool Encender(string id)
        {
            if (Terminada || !EnLaNave(id) || SinAlcance(id) || Bloqueados.ContainsKey(id) || Encendidos.Contains(id)) return false;
            if (ConsumoW + catalogo.Instrumento(id).ConsumoTotalW > LimiteW) return false;
            Encendidos.Add(id);
            return true;
        }

        public void Apagar(string id) => Encendidos.Remove(id);

        /// <summary>Fija la región que observará un instrumento (punto del mapa, de 0 a 1).</summary>
        public void FijarRegion(string id, Vector2 punto)
        {
            if (!Terminada && EnLaNave(id)) Regiones[id] = punto;
        }

        /// <summary>Modo seguro por un evento: se apaga todo, cuesta créditos y el mes siguiente pierde días.</summary>
        void Apagon()
        {
            Apagones++;
            CreditosPerdidos += reglas.creditosPorReinicio;
            Encendidos.Clear();
            diasPerdidos += reglas.diasPerdidosPorApagon;
        }

        // ---------- Maniobra ----------

        /// <summary>Créditos de la misión que quedan (presupuesto menos nave, carga, reinicios y maniobras).</summary>
        public int CreditosDisponibles =>
            nivel.presupuestoCr - evaluacion.Calculo.CostoCr - CreditosCarga - CreditosPerdidos - CreditosManiobras;

        /// <summary>
        /// Pide cambiar de altitud este mes. No cobra nada: se cobra al resolver. Devuelve false (y no deja
        /// maniobra) si el destino es la altitud actual o no alcanzan el combustible o los créditos.
        /// </summary>
        public bool PlanearManiobra(Altitud destino)
        {
            ManiobraPlaneada = null;
            if (Terminada || destino == Altitud) return false;
            if (Combustible < CostoCambioAltitud || CreditosDisponibles < reglas.creditosPorManiobra) return false;
            ManiobraPlaneada = destino;
            return true;
        }

        public void CancelarManiobra() => ManiobraPlaneada = null;

        float FactorLectura => Altitud == Altitud.Baja ? reglas.factorAltitudBaja : 1f;

        // ---------- Memoria y contacto ----------

        /// <summary>A esta distancia (o menos) de la anomalía, una observación la encuentra (sin contar altitud ni satélite).</summary>
        public float RadioDeteccion => reglas.radioDeteccion;

        public int MemoriaCapacidad => nivel.memoriaBloques + (Satelite != null ? Satelite.memoriaExtra : 0);
        public int MemoriaUsada => Memoria.Sum(r => r.Bloques);

        /// <summary>Hay ventana de contacto con la Tierra este mes.</summary>
        public bool ContactoAbierto => MesesParaContacto == 0;

        /// <summary>
        /// Meses que faltan para la próxima ventana (0 = este mes). Regla de juego: se abre un mes de cada
        /// «cicloContacto» (con 2, los meses pares) y siempre en el último mes.
        /// </summary>
        public int MesesParaContacto => mesContactoForzado == Mes ? 0
            : System.Math.Min(CicloContacto - 1 - MesesResueltos % CicloContacto, System.Math.Max(0, MesesRestantes - 1));

        /// <summary>Días útiles que tendrá este mes con el plan actual.</summary>
        public int DiasPrevistos => System.Math.Max(0,
            reglas.diasPorMes - diasPerdidos - (ManiobraPlaneada.HasValue ? reglas.diasPorManiobra : 0));

        /// <summary>Bloques que ocuparían los registros de este mes con el plan actual.</summary>
        public int BloquesPrevistos => Encendidos.Where(Regiones.ContainsKey).Sum(id => catalogo.Instrumento(id).bloquesDatos);

        /// <summary>Por qué no se podría enviar este mes, o null si se puede.</summary>
        public string MotivoSinEnvio =>
            EnvioBloqueado ? Idioma.T("dust is blocking the laser")
            : !ContactoAbierto && !PuedeEnviarSinVentana ? Idioma.T("there is no contact with Earth this month")
            : ConsumoBaseW + VatiosTransmisor > LimiteW ? Idioma.T("there is not enough power for the transmitter")
            : null;

        // ---------- Resolver el mes ----------

        /// <summary>Todas las evidencias llegaron a la Tierra: se puede pasar al informe.</summary>
        public bool Completa => Pendientes.Count == 0 && !Memoria.Any(r => r.Evidencias.Count > 0);

        /// <summary>Por qué el plan actual no se puede resolver, o null si es válido.</summary>
        public string MotivoPlanInvalido =>
            Terminada ? Idioma.T("the mission has already ended")
            : EsperandoDecision ? Idioma.T("a decision is pending")
            : ConsumoW > LimiteW ? Idioma.T("power draw (") + ConsumoW + Idioma.T(" W) exceeds the limit (") + LimiteW + Idioma.T(" W): turn off an instrument")
            : null;

        /// <summary>
        /// Resuelve el mes «mes» con el plan actual, una sola vez. Si «mes» no es el mes en curso (doble clic,
        /// animación que repite) o el plan no es válido, no cobra ni cambia nada y devuelve un resultado no válido.
        /// </summary>
        public ResultadoMes ResolverMes(int mes)
        {
            var r = new ResultadoMes { Mes = mes };
            r.Motivo = mes != Mes ? Idioma.T("that month has already been resolved") : MotivoPlanInvalido;
            if (r.Motivo != null) return r;
            r.Valido = true;

            // 1. Maniobra: se cobra una vez y quita días de observación.
            int dias = reglas.diasPorMes - diasPerdidos;
            diasPerdidos = 0;
            if (ManiobraPlaneada.HasValue)
            {
                Combustible -= CostoCambioAltitud;
                CreditosManiobras += reglas.creditosPorManiobra;
                Altitud = ManiobraPlaneada.Value;
                dias -= reglas.diasPorManiobra;
                r.Maniobra = true;
                r.CombustibleGastado = CostoCambioAltitud;
                r.CreditosGastados = reglas.creditosPorManiobra;
            }
            r.DiasUtiles = dias = System.Math.Max(0, dias);
            r.Altitud = Altitud;

            // 2. Observar y almacenar: un registro por instrumento encendido con región, si cabe en la memoria.
            r.CalidadBase = reglas.calidadBase;
            r.FactorTormenta = calidadReducidaProxima ? reglas.factorCalidadTormenta : 1f;
            r.FactorAltitud = Altitud == Altitud.Baja ? reglas.factorCalidadBaja : 1f;
            float calidad = Mathf.Clamp(reglas.calidadBase * dias / reglas.diasPorMes * r.FactorTormenta * r.FactorAltitud, 0f, 100f);
            foreach (var id in Instrumentos.Where(Encendidos.Contains).ToList())
            {
                if (dias <= 0) break;
                if (!Regiones.TryGetValue(id, out var punto)) { r.SinRegion.Add(id); continue; }
                int bloques = catalogo.Instrumento(id).bloquesDatos;
                if (MemoriaUsada + bloques > MemoriaCapacidad) { r.SinMemoria.Add(id); continue; }   // no pisa datos anteriores
                var registro = Observar(id, punto, calidad, bloques);
                Memoria.Add(registro);
                Historial.Add(registro);
                r.Observaciones.Add(registro);
            }
            sinLecturaProxima = false;
            calidadReducidaProxima = false;

            // 3. Transmitir al final del mes: no libera memoria para las observaciones de este mismo mes.
            if (TransmitirPlaneado)
            {
                r.EnvioPedido = true;
                r.EnvioFallido = Memoria.Count == 0 ? Idioma.T("there is no data in memory") : MotivoSinEnvio;
                if (r.EnvioFallido == null) Transmitir(r);
            }

            // 4. Avanzar el mes.
            bool arrastre = Altitud == Altitud.Baja;
            Avanzar();
            r.SubidaForzada = arrastre && Altitud == Altitud.Normal;
            r.CombustibleGastado += arrastre && !r.SubidaForzada ? reglas.combustiblePorOrbitaBaja : 0;
            ManiobraPlaneada = null;
            TransmitirPlaneado = false;

            // 4b. Versión 9: si al cerrar el mes límite no ha llegado ninguna evidencia a la Tierra, Control de Misión
            //     cancela la misión (fracaso). Se avisa antes en el contador de evidencia de la Operación.
            if (!Terminada && reglas.mesLimiteSinEvidencia > 0 && MesesResueltos >= reglas.mesLimiteSinEvidencia && !HayEvidenciaEnTierra)
            {
                CanceladaSinEvidencia = true;
                Terminar();
            }

            // 5. Evento de cierre: afecta a lo que viene, nunca a lo ya enviado. No hay eventos tras el final.
            if (!Terminada && momentosEvento.Count > 0 && MesesResueltos >= momentosEvento[0])
            {
                momentosEvento.RemoveAt(0);
                var datos = eventosProgramados[0];
                eventosProgramados.RemoveAt(0);
                AplicarEvento(datos);
                r.Evento = UltimoEvento;
            }
            r.Termino = Terminada;
            return r;
        }

        Registro Observar(string id, Vector2 punto, float calidad, int bloques)
        {
            Observaciones++;
            float alcance = FactorLectura * FactorSatelite(id);
            var registro = new Registro { Instrumento = id, Region = punto, Mes = Mes, Bloques = bloques };
            if (sinLecturaProxima)
            {
                registro.Lectura = Lectura.SinLectura;
            }
            else
            {
                float d = Vector2.Distance(punto, Anomalia) / alcance;
                registro.Lectura = d <= reglas.radioDeteccion ? Lectura.Encontrada
                    : d <= reglas.distanciaCaliente ? Lectura.Caliente
                    : d <= reglas.distanciaTibio ? Lectura.Tibio
                    : Lectura.Frio;
                registro.Calidad = calidad;
            }

            if (PeticionAceptada && !PeticionCumplida && Vector2.Distance(punto, PuntoPeticion) / alcance <= reglas.radioDeteccion)
                PeticionCumplida = true;

            // Evidencia válida: el instrumento observó la anomalía con calidad suficiente.
            if (registro.Lectura == Lectura.Encontrada && registro.Calidad >= reglas.calidadMinima)
                foreach (var par in Pendientes.Where(p => p.Value == id).ToList())
                {
                    registro.Evidencias.Add(par.Key);
                    Pendientes.Remove(par.Key);
                }
            return registro;
        }

        void Transmitir(ResultadoMes r)
        {
            // Por la constelación (sin ventana) solo caben unos pocos bloques; no se parte un registro.
            int cupo = ContactoAbierto ? int.MaxValue : Satelite.bloquesEnvioSinVentana;
            foreach (var registro in Memoria.ToList())
            {
                if (r.BloquesEnviados + registro.Bloques > cupo) break;
                r.BloquesEnviados += registro.Bloques;
                r.Enviadas.AddRange(registro.Evidencias);
                Recibidas.AddRange(registro.Evidencias);
                Memoria.Remove(registro);
            }
            if (r.BloquesEnviados == 0) r.EnvioFallido = Idioma.T("the data does not fit through the constellation link");
            if (Completa && ObservacionesAlCompletar == 0)
            {
                ObservacionesAlCompletar = Observaciones;
                MesesAlCompletar = MesesRestantes - 1;   // los que quedan al cerrar este mes
            }
        }

        void Avanzar()
        {
            if (Altitud == Altitud.Baja)   // arrastre: gasta combustible; sin él la nave sube sola y pierde días
            {
                if (Combustible >= reglas.combustiblePorOrbitaBaja) Combustible -= reglas.combustiblePorOrbitaBaja;
                else
                {
                    Altitud = Altitud.Normal;
                    SubidasForzadas++;
                    diasPerdidos += reglas.diasPerdidosSinCombustible;
                }
            }
            MesesRestantes--;
            MesesResueltos++;
            EnvioBloqueado = false;
            foreach (var e in Eventos.Where(e => !e.Resuelto && e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal))
                if (--e.OrbitasRestantes <= 0) e.Resuelto = true;
            foreach (var id in Bloqueados.Keys.ToList())
                if (--Bloqueados[id] <= 0) Bloqueados.Remove(id);
            // El efecto ya pasó (el mes sin lecturas o el bloqueo): la herramienta deja de servir y no se gasta en vano.
            // Va antes del evento de cierre, así que nunca resuelve un evento del mismo mes.
            foreach (var e in Eventos.Where(e => !e.Resuelto && (e.Datos.efecto == EfectoEvento.AnularLectura
                         || (e.Datos.efecto == EfectoEvento.BloquearInstrumento && Bloqueados.Count == 0))))
                e.Resuelto = true;
            if (MesesRestantes <= 0) Terminar();
        }

        /// <summary>Fin de la misión (voluntario o por meses). Lo que no se envió no cuenta como evidencia.</summary>
        /// <summary>¿Llegó a la Tierra al menos una evidencia de un instrumento? (TX-00 es de la misión, no cuenta.)</summary>
        public bool HayEvidenciaEnTierra => Recibidas.Any(id => !string.IsNullOrEmpty(catalogo.Transmision(id).instrumentoId));

        /// <summary>Control de Misión la canceló por no tener evidencia en la Tierra al cerrar el mes límite.</summary>
        public bool CanceladaSinEvidencia { get; private set; }

        /// <summary>Mes al final del cual hace falta la primera evidencia en la Tierra (0 = sin límite).</summary>
        public int MesLimiteSinEvidencia => reglas.mesLimiteSinEvidencia;

        public void Terminar()
        {
            if (Terminada) return;
            Terminada = true;
            Decision = null;
            Encendidos.Clear();
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

        /// <summary>Fija qué eventos ocurren y al cierre de qué mes. Lo usan las pruebas para no depender del azar.</summary>
        public void ProgramarEventos(IList<EventoDatos> eventos, IList<int> alCierreDelMes)
        {
            eventosProgramados.Clear();
            momentosEvento.Clear();
            eventosProgramados.AddRange(eventos);
            momentosEvento.AddRange(alCierreDelMes);
        }

        /// <summary>Coloca la anomalía en un punto fijo. Para las pruebas.</summary>
        public void PonerAnomalia(Vector2 punto) => Anomalia = punto;

        /// <summary>Coloca el punto de la petición de la NASA en un sitio fijo. Para las pruebas.</summary>
        public void PonerPuntoPeticion(Vector2 punto) => PuntoPeticion = punto;

        /// <summary>Hace ocurrir un evento: la Operación queda en pausa hasta que el jugador elige con Decidir.</summary>
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
            if (datos.opciones.Count > 0)
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
                    MesesRestantes += o.duracion;
                    e.Resuelto = true;
                    break;
                case EfectoOpcion.AbrirContacto:
                    // Abre el primer mes que no tenía contacto (si el siguiente ya lo tenía, no serviría de nada).
                    mesContactoForzado = ContactoAbierto ? Mes + 1 : Mes;
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
            // Lo que antes costaba órbitas ahora cuesta días útiles del mes siguiente.
            diasPerdidos += o.orbitas * reglas.diasPerdidosPorOpcion;
            return true;
        }

        void AplicarEfecto(EventoActivo e, int meses)
        {
            switch (e.Datos.efecto)
            {
                case EfectoEvento.ReducirLimite:
                case EfectoEvento.ReducirLimiteTemporal:
                    if (ConsumoW > LimiteW) Apagon();   // evento inesperado: modo seguro
                    break;
                case EfectoEvento.BloquearInstrumento:
                    var mayor = Encendidos.OrderByDescending(id => catalogo.Instrumento(id).ConsumoTotalW).FirstOrDefault();
                    if (mayor != null)
                    {
                        Encendidos.Remove(mayor);
                        Bloqueados[mayor] = meses;
                    }
                    else e.Resuelto = true; // no había nada encendido: no pasa nada
                    break;
                case EfectoEvento.AnularLectura:
                    sinLecturaProxima = true;
                    break;
                case EfectoEvento.GanarOrbitas:
                    MesesRestantes += meses;
                    e.Resuelto = true;
                    break;
                case EfectoEvento.Tormenta:
                    calidadReducidaProxima = Satelite == null || !Satelite.inmuneTormenta;
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

        /// <summary>La herramienta va a bordo: se cargó, o no hay Carga (pruebas).</summary>
        public bool TieneHerramienta(string id) => Carga == null || Carga.Lleva(id);

        /// <summary>Usa una herramienta (una vez por misión) sobre un evento que se aguantó. Devuelve false si no resuelve nada.</summary>
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
