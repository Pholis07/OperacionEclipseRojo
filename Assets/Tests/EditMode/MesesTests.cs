using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Operación por meses (versión 4, paquete «Operación Eclipse Rojo» v1.0). Los nombres llevan el id de la
    /// matriz de aceptación del paquete (MES, ORB, MEM, CIE, EVT). Sustituyen a las pruebas de la v3, que están
    /// en archivo/pruebas-v3 porque las reglas nuevas invalidan sus expectativas.
    /// </summary>
    public class MesesTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        Operacion Nueva(Configuracion c, MisionDatos nivel, params string[] herramientas)
        {
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var carga = new Carga(e, c, catalogo, nivel);
            foreach (var h in herramientas) Assert.IsTrue(carga.Cargar(h), h);
            var op = new Operacion(e, c, catalogo, nivel, 1, carga);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Objetivos.Clear();
            return op;
        }

        static Configuracion ConfigCaso2() => new Configuracion("media", "explorador", new[] { "termica", "radar" });

        /// <summary>Caso 2 en el nivel 2: sobran 430 cr y 7 de combustible (70 kg libres); límite de 700 W; exige térmica y radar.</summary>
        Operacion Caso2(params string[] herramientas) => Nueva(ConfigCaso2(), null, herramientas);

        /// <summary>Titán en elíptica, nivel 1: sobran 370 cr y 20 de combustible (el tope); límite de 800 W; sin eventos.</summary>
        Operacion Titan(params string[] herramientas) =>
            Nueva(new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }), catalogo.Nivel(0), herramientas);

        static void Observar(Operacion op, string id, Vector2 region)
        {
            if (!op.Encendidos.Contains(id)) Assert.IsTrue(op.Encender(id), "encender " + id);
            op.FijarRegion(id, region);
        }

        static ResultadoMes Resolver(Operacion op, bool enviar = false)
        {
            op.TransmitirPlaneado = enviar;
            var r = op.ResolverMes(op.Mes);
            Assert.IsTrue(r.Valido, r.Motivo);
            return r;
        }

        EventoDatos Evento(string id) => catalogo.eventos.First(e => e.id == id);
        static OpcionEvento Con(Operacion op, EfectoOpcion efecto) => op.Opciones.First(o => o.efecto == efecto);
        CategoriaResultado Categoria(Operacion op, Configuracion c, MisionDatos nivel = null) =>
            ResultadoCientifico.Categoria(Evaluador.Evaluar(c, catalogo, nivel), op, catalogo);

        // ---------- Datos ----------

        [Test]
        public void ReglasDeLaVersion4()
        {
            var r = catalogo.reglas;
            Assert.AreEqual(30, r.diasPorMes);
            Assert.AreEqual(40, r.creditosPorManiobra);
            Assert.AreEqual(2, r.diasPorManiobra);
            Assert.AreEqual(70f, r.calidadBase);
            Assert.AreEqual(60f, r.calidadMinima);
            Assert.AreEqual(2, r.cicloContacto, "contacto en los meses pares");
            Assert.AreEqual(150, r.vatiosTransmisor);
        }

        [TestCase(0, 12, 0)]
        [TestCase(1, 12, 3)]
        [TestCase(2, 12, 5)]
        public void LaCampanaDuraDoceMesesYCambianLosEventos(int nivel, int meses, int eventos)
        {
            Assert.AreEqual(meses, catalogo.Nivel(nivel).orbitasNavegacion);
            Assert.AreEqual(eventos, catalogo.Nivel(nivel).eventosPorMision);
            Assert.AreEqual(6, catalogo.Nivel(nivel).memoriaBloques);
        }

        [TestCase("termica", 1)]
        [TestCase("radar", 2)]
        [TestCase("alta_res", 1)]
        [TestCase("atmosferico", 1)]
        [TestCase("espectrometro", 2)]
        public void CadaInstrumentoOcupaSusBloques(string id, int bloques)
        {
            Assert.AreEqual(bloques, catalogo.Instrumento(id).bloquesDatos);
        }

        // ---------- Meses ----------

        [Test]
        public void MES01_LosInstrumentosEncendidosSiguenEncendidos()
        {
            var op = Caso2();
            Observar(op, "termica", Lejos);
            Resolver(op);
            Assert.AreEqual(2, op.Mes);
            CollectionAssert.Contains(op.Encendidos.ToList(), "termica");
            Assert.AreEqual(Lejos, op.Regiones["termica"], "la región también se conserva");
            var r = Resolver(op);
            Assert.AreEqual(1, r.Observaciones.Count, "sigue observando sin volver a encenderlo");
        }

        [Test]
        public void MES02_EditarYCancelarElPlanNoGastaNada()
        {
            var op = Titan();
            int creditos = op.CreditosDisponibles, combustible = op.Combustible;
            Observar(op, "termica", Centro);
            Observar(op, "radar", Lejos);
            Assert.IsTrue(op.PlanearManiobra(Altitud.Baja));
            op.TransmitirPlaneado = true;
            op.Apagar("radar");
            op.CancelarManiobra();
            op.TransmitirPlaneado = false;

            Assert.AreEqual(1, op.Mes);
            Assert.AreEqual(12, op.MesesRestantes);
            Assert.AreEqual(creditos, op.CreditosDisponibles);
            Assert.AreEqual(combustible, op.Combustible);
            Assert.AreEqual(0, op.MemoriaUsada);
            Assert.AreEqual(Altitud.Normal, op.Altitud);
            Assert.AreEqual(0, op.Observaciones, "encender no genera datos");
        }

        [Test]
        public void MES03_ConfirmarDosVecesElMismoMesCobraUnaSolaVez()
        {
            var op = Titan();
            Observar(op, "termica", Lejos);
            op.PlanearManiobra(Altitud.Baja);
            var primero = op.ResolverMes(1);
            var segundo = op.ResolverMes(1);

            Assert.IsTrue(primero.Valido);
            Assert.IsFalse(segundo.Valido, "el mes 1 ya se resolvió");
            Assert.AreEqual(2, op.Mes);
            Assert.AreEqual(370 - 40, op.CreditosDisponibles, "un solo cobro");
            Assert.AreEqual(1, op.Memoria.Count, "un solo registro");
        }

        [Test]
        public void MES06_ConTodoApagadoElMesAvanzaSinObservar()
        {
            var op = Caso2();
            var r = Resolver(op);
            Assert.AreEqual(2, op.Mes);
            Assert.AreEqual(0, r.Observaciones.Count);
            Assert.AreEqual(30, r.DiasUtiles);
            Assert.AreEqual(0, op.MemoriaUsada);
        }

        [Test]
        public void UnInstrumentoEncendidoSinRegionNoObserva()
        {
            var op = Caso2();
            op.Encender("termica");
            var r = Resolver(op);
            CollectionAssert.AreEqual(new[] { "termica" }, r.SinRegion);
            Assert.AreEqual(0, op.MemoriaUsada);
        }

        [Test]
        public void NoSePuedeEncenderLoQueNoCabeEnElLimiteDePotencia()
        {
            // Térmica y alta resolución en el Titán (versión 5: la alta resolución gasta 300 W, como HiRISE en proporción).
            var op = Nueva(new Configuracion("media", "titan", new[] { "termica", "alta_res" }), null, "bateria");   // límite de 700 W
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));   // 500 W
            op.AplicarEvento(Evento("sombra"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));   // 350 W
            Assert.IsTrue(op.Encender("termica"), "100 + 70 = 170 W");
            Assert.IsFalse(op.Encender("alta_res"), "170 + 300 = 470 W no caben en 350");
            Assert.AreEqual(0, op.Apagones, "un plan imposible se bloquea: no hay modo seguro");
        }

        [Test]
        public void AlAcabarseLosMesesLaMisionTerminaYYaNoSeResuelve()
        {
            var op = Caso2();
            for (int i = 0; i < 12; i++) Resolver(op);
            Assert.IsTrue(op.Terminada);
            Assert.IsFalse(op.ResolverMes(op.Mes).Valido);
        }

        // ---------- Maniobras ----------

        [Test]
        public void ORB01_LaManiobraCuestaCreditosCombustibleYDosDias()
        {
            // Mismo contrato que datos/escenario_mes_01.json del paquete: −40 cr, −3 de combustible, 28 días,
            // memoria de 1 a 4 de 6 y calidad nominal de 65.33.
            var op = Titan();
            op.PlanearManiobra(Altitud.Baja);
            Observar(op, "termica", Lejos);
            Resolver(op);                       // mes 1: baja (8 − 3 − 1 de arrastre = 4) y un registro (1 bloque)
            Assert.AreEqual(1, op.MemoriaUsada);
            int creditos = op.CreditosDisponibles, combustible = op.Combustible;

            Assert.IsTrue(op.PlanearManiobra(Altitud.Normal));
            Assert.AreEqual(28, op.DiasPrevistos, "la previsión ya lo muestra antes de confirmar");
            Observar(op, "radar", Lejos);
            var r = Resolver(op);

            Assert.IsTrue(r.Maniobra);
            Assert.AreEqual(28, r.DiasUtiles);
            Assert.AreEqual(creditos - 40, op.CreditosDisponibles);
            Assert.AreEqual(combustible - 3, op.Combustible, "en órbita normal ya no hay arrastre");
            Assert.AreEqual(Altitud.Normal, op.Altitud);
            Assert.AreEqual(4, op.MemoriaUsada, "1 + térmica (1) + radar (2)");
            Assert.AreEqual(65.33f, r.Observaciones[0].Calidad, 0.01f);
            CollectionAssert.AreEquivalent(new[] { "termica", "radar" }, op.Encendidos.ToList());
        }

        [Test]
        public void ORB02_UnaManiobraInvalidaNoCobraNiCambiaLaOrbita()
        {
            // Radar y espectrómetro en el Explorador: 495 de 500 kg, sin combustible.
            var op = Nueva(new Configuracion("media", "explorador", new[] { "radar", "espectrometro" }), null);
            Assert.AreEqual(0, op.Combustible);
            int creditos = op.CreditosDisponibles;
            Assert.IsFalse(op.PlanearManiobra(Altitud.Baja));
            Assert.IsNull(op.ManiobraPlaneada);
            var r = Resolver(op);
            Assert.IsFalse(r.Maniobra);
            Assert.AreEqual(30, r.DiasUtiles);
            Assert.AreEqual(creditos, op.CreditosDisponibles);
            Assert.AreEqual(Altitud.Normal, op.Altitud);
        }

        [Test]
        public void ORB03_ElegirLaOrbitaActualNoEsUnaManiobra()
        {
            var op = Titan();
            Assert.IsFalse(op.PlanearManiobra(Altitud.Normal));
            var r = Resolver(op);
            Assert.IsFalse(r.Maniobra);
            Assert.AreEqual(20, op.Combustible);
            Assert.AreEqual(370, op.CreditosDisponibles);
        }

        [Test]
        public void EnBajaLaCalidadSubeYElArrastreGastaCombustible()
        {
            var op = Titan();
            op.PlanearManiobra(Altitud.Baja);
            Observar(op, "termica", Lejos);
            var r = Resolver(op);
            Assert.AreEqual(70f * 28 / 30 * 1.2f, r.Observaciones[0].Calidad, 0.01f);
            Assert.AreEqual(16, op.Combustible, "20 − 3 de la maniobra − 1 de arrastre");
            Resolver(op);
            Assert.AreEqual(15, op.Combustible);
        }

        [Test]
        public void SinCombustibleEnBajaLaNaveSubeSolaYElMesSiguientePierdeDias()
        {
            var op = Caso2("bateria", "refrigeracion");   // 35 kg libres: 3 de combustible
            Assert.AreEqual(3, op.Combustible);
            op.PlanearManiobra(Altitud.Baja);              // 3 − 3 = 0
            var r = Resolver(op);
            Assert.IsTrue(r.SubidaForzada);
            Assert.AreEqual(Altitud.Normal, op.Altitud);
            Assert.AreEqual(1, op.SubidasForzadas);
            Assert.AreEqual(20, Resolver(op).DiasUtiles);
            Assert.AreEqual(30, Resolver(op).DiasUtiles, "solo el mes siguiente");
        }

        // ---------- Memoria, contacto y envío ----------

        [Test]
        public void ElContactoEsEnLosMesesParesYEnElUltimo()
        {
            var op = Titan();
            op.AplicarEvento(Evento("ventana"));
            op.Decidir(Con(op, EfectoOpcion.GanarOrbitas));   // 13 meses: el último es impar
            for (int mes = 1; mes <= 13; mes++)
            {
                Assert.AreEqual(mes, op.Mes);
                Assert.AreEqual(mes % 2 == 0 || mes == 13, op.ContactoAbierto, "mes " + mes);
                Resolver(op);
            }
            Assert.IsTrue(op.Terminada);
        }

        [Test]
        public void MEM01_ConLaMemoriaAlLimiteNoSeSobrescribeYSeRegistraLaOmision()
        {
            var op = Caso2();
            Observar(op, "radar", Lejos);
            for (int i = 0; i < 3; i++) Resolver(op);   // 3 registros de 2 bloques: 6 de 6
            Assert.AreEqual(6, op.MemoriaUsada);
            var antes = op.Memoria.ToList();

            var r = Resolver(op);
            CollectionAssert.AreEqual(new[] { "radar" }, r.SinMemoria);
            Assert.AreEqual(0, r.Observaciones.Count);
            CollectionAssert.AreEqual(antes, op.Memoria, "los datos anteriores siguen ahí");
        }

        [Test]
        public void MEM02_EnviarAlFinalDelMesNoLiberaEspacioParaEseMes()
        {
            var op = Caso2();
            Observar(op, "radar", Lejos);
            for (int i = 0; i < 3; i++) Resolver(op);   // memoria llena al empezar el mes 4 (hay contacto)
            Assert.IsTrue(op.ContactoAbierto);

            var r = Resolver(op, enviar: true);
            CollectionAssert.AreEqual(new[] { "radar" }, r.SinMemoria, "primero se observa y después se envía");
            Assert.AreEqual(6, r.BloquesEnviados);
            Assert.AreEqual(0, op.MemoriaUsada);
        }

        [Test]
        public void MEM03_SinContactoNoSeEnviaYLosDatosSeConservan()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Assert.IsFalse(op.ContactoAbierto, "el mes 1 no tiene contacto");
            var r = Resolver(op, enviar: true);

            Assert.IsNotNull(r.EnvioFallido);
            Assert.AreEqual(0, r.BloquesEnviados);
            CollectionAssert.IsEmpty(r.Enviadas);
            CollectionAssert.AreEqual(new[] { "TX-00" }, op.Recibidas);
            Assert.AreEqual(1, op.Memoria.Count);
            CollectionAssert.AreEqual(new[] { "TX-01" }, op.Memoria[0].Evidencias, "la evidencia sigue en la memoria");
        }

        [Test]
        public void MEM04_VolverAEnviarLaMismaEvidenciaNoLaDuplica()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Resolver(op);
            Resolver(op, enviar: true);   // mes 2: llega TX-01
            Resolver(op);                 // mes 3: la térmica vuelve a observar la anomalía
            var r = Resolver(op, enviar: true);

            CollectionAssert.IsEmpty(r.Enviadas, "ya se había recibido");
            Assert.AreEqual(1, op.Recibidas.Count(id => id == "TX-01"));
            Assert.AreEqual(CategoriaResultado.Parcial, Categoria(op, ConfigCaso2()));
        }

        [Test]
        public void ElTransmisorNecesitaPotenciaSoloParaLaBaseYSeApaganLosInstrumentos()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Observar(op, "radar", Centro);   // 430 W: con 150 W del transmisor serían 580
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));   // límite 500 W
            Resolver(op);
            var r = Resolver(op, enviar: true);
            Assert.IsNull(r.EnvioFallido, "100 W de base + 150 W caben: observar y enviar no se suman");
            CollectionAssert.AreEquivalent(new[] { "TX-01", "TX-02" }, r.Enviadas);
            Assert.AreEqual(0, op.Apagones);
        }

        // ---------- Ciencia ----------

        [Test]
        public void CIE01_UnaConfiguracionViableSinEvidenciaEnviadaNoGana()
        {
            var op = Caso2();
            Assert.AreEqual(CategoriaResultado.Victoria, Evaluador.Evaluar(ConfigCaso2(), catalogo).Categoria, "la configuración podía ganar");
            Observar(op, "termica", Centro);
            Observar(op, "radar", Centro);
            Resolver(op);                     // observado y en memoria, pero nunca enviado
            op.Terminar();
            Assert.AreEqual(CategoriaResultado.Fracaso, Categoria(op, ConfigCaso2()));
            Assert.AreEqual(CategoriaResultado.Fracaso, ResultadoCientifico.Categoria(Evaluador.Evaluar(ConfigCaso2(), catalogo), null, catalogo));
        }

        [Test]
        public void CIE02_UnaEvidenciaPrincipalEnviadaEsParcial()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Resolver(op);
            Resolver(op, enviar: true);
            Assert.AreEqual(CategoriaResultado.Parcial, Categoria(op, ConfigCaso2()));
            Assert.IsFalse(op.Completa);
        }

        [Test]
        public void CIE03_TodasLasPrincipalesEnviadasEsVictoria()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Observar(op, "radar", Centro);
            Resolver(op);
            var r = Resolver(op, enviar: true);
            CollectionAssert.AreEquivalent(new[] { "TX-01", "TX-02" }, r.Enviadas);
            Assert.IsTrue(op.Completa);
            Assert.AreEqual(4, op.ObservacionesAlCompletar, "los dos instrumentos siguieron encendidos el mes 2");
            Assert.AreEqual(10, op.MesesAlCompletar);
            Assert.AreEqual(CategoriaResultado.Victoria, Categoria(op, ConfigCaso2()));
        }

        [Test]
        public void CIE04_FueraDeLaRegionOConBajaCalidadNoEsEvidencia()
        {
            var lejos = Caso2();
            Observar(lejos, "termica", Lejos);
            Resolver(lejos);
            Resolver(lejos, enviar: true);
            Assert.AreEqual(CategoriaResultado.Fracaso, Categoria(lejos, ConfigCaso2()), "fuera de la región");

            var tormenta = Caso2();
            tormenta.AplicarEvento(Evento("tormenta"));
            tormenta.Decidir(Con(tormenta, EfectoOpcion.Aguantar));
            Observar(tormenta, "termica", Centro);
            var r = Resolver(tormenta);
            Assert.AreEqual(Lectura.Encontrada, r.Observaciones[0].Lectura);
            Assert.AreEqual(56f, r.Observaciones[0].Calidad, 0.01f, "70 × 0.8: por debajo de 60");
            CollectionAssert.IsEmpty(r.Observaciones[0].Evidencias);
            tormenta.Apagar("termica");   // si siguiera encendida, el mes 2 la observaría con calidad normal
            Resolver(tormenta, enviar: true);
            Assert.AreEqual(CategoriaResultado.Fracaso, Categoria(tormenta, ConfigCaso2()), "baja calidad");
            CollectionAssert.Contains(tormenta.Pendientes.Keys, "TX-01", "se puede volver a intentar otro mes");
        }

        [Test]
        public void LaCategoriaNuncaSuperaLoQuePermiteLaConfiguracion()
        {
            // Solo térmica y atmosférico en media: la configuración da como mucho «parcial».
            var c = new Configuracion("media", "explorador", new[] { "termica", "atmosferico" });
            var op = Nueva(c, null);
            Observar(op, "termica", Centro);
            Observar(op, "atmosferico", Centro);
            Resolver(op);
            Resolver(op, enviar: true);
            Assert.AreEqual(CategoriaResultado.Parcial, Categoria(op, c));
        }

        // ---------- Eventos ----------

        [Test]
        public void EVT01_UnEventoDeCierreNoBorraLoYaEnviado()
        {
            var op = Caso2();
            op.ProgramarEventos(new[] { Evento("sombra") }, new[] { 2 });
            Observar(op, "termica", Centro);
            Resolver(op);
            var r = Resolver(op, enviar: true);

            Assert.IsNotNull(r.Evento);
            Assert.AreEqual("sombra", r.Evento.Datos.id);
            CollectionAssert.AreEqual(new[] { "TX-01" }, r.Enviadas);
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            CollectionAssert.Contains(op.Recibidas, "TX-01");
        }

        [Test]
        public void EVT02_UnaDecisionPendienteBloqueaElMesSiguiente()
        {
            var op = Caso2();
            op.ProgramarEventos(new[] { Evento("sombra") }, new[] { 1 });
            Resolver(op);
            Assert.IsTrue(op.EsperandoDecision);
            Assert.AreEqual(700, op.LimiteW, "el efecto espera a la decisión");

            var bloqueado = op.ResolverMes(op.Mes);
            Assert.IsFalse(bloqueado.Valido);
            Assert.AreEqual(2, op.Mes);

            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            Assert.AreEqual(550, op.LimiteW);
            Resolver(op);
            Assert.AreEqual(3, op.Mes);
        }

        [Test]
        public void NoHayEventosDespuesDelUltimoMes()
        {
            var op = Caso2();
            op.ProgramarEventos(new[] { Evento("sombra") }, new[] { 12 });
            ResultadoMes r = null;
            for (int i = 0; i < 12; i++) r = Resolver(op);
            Assert.IsTrue(r.Termino);
            Assert.IsNull(r.Evento);
            Assert.IsFalse(op.EsperandoDecision);
        }

        [Test]
        public void LaSombraDuraTresMeses()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("sombra"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            for (int i = 0; i < 3; i++) { Assert.AreEqual(550, op.LimiteW); Resolver(op); }
            Assert.AreEqual(700, op.LimiteW);
        }

        [Test]
        public void UnEventoQueDejaElConsumoPorEncimaDelLimiteProvocaModoSeguro()
        {
            // Titán en elíptica con alta resolución, nivel 1: 575 W de 800 y sobran 170 cr.
            var op = Nueva(new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico", "alta_res" }), catalogo.Nivel(0));
            Observar(op, "termica", Lejos);
            Observar(op, "radar", Lejos);
            Observar(op, "atmosferico", Lejos);
            Observar(op, "alta_res", Lejos);               // 575 W de 800
            op.AplicarEvento(Evento("sombra"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));    // 650: todavía cabe
            Assert.AreEqual(0, op.Apagones);
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));    // 450: ya no

            Assert.AreEqual(1, op.Apagones);
            CollectionAssert.IsEmpty(op.Encendidos);
            Assert.AreEqual(170 - 50, op.CreditosDisponibles);
            Assert.AreEqual(15, Resolver(op).DiasUtiles, "el mes siguiente pierde 15 días");
            Assert.IsFalse(op.Terminada, "un modo seguro no termina la misión");
        }

        [Test]
        public void DesviarLaCorrienteQuitaDiezDiasAlMesSiguiente()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(Con(op, EfectoOpcion.Resolver));
            Assert.AreEqual(700, op.LimiteW);
            Assert.AreEqual(20, op.DiasPrevistos);
            Observar(op, "termica", Centro);
            var r = Resolver(op);
            Assert.AreEqual(20, r.DiasUtiles);
            CollectionAssert.IsEmpty(r.Observaciones[0].Evidencias, "calidad 46.7: no llega a 60");
            Assert.AreEqual(12 - 1, op.MesesRestantes, "ya no cuesta un mes entero");
        }

        [Test]
        public void ElSobrecalentamientoBloqueaDosMesesYLaRefrigeracionLoEvita()
        {
            var op = Caso2();
            Observar(op, "radar", Lejos);
            op.AplicarEvento(Evento("sobrecalentamiento"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            Assert.IsFalse(op.Encender("radar"));
            Resolver(op);
            Assert.IsFalse(op.Encender("radar"));
            Resolver(op);
            Assert.IsTrue(op.Encender("radar"), "pasaron los 2 meses");

            var con = Caso2("refrigeracion");
            Observar(con, "radar", Lejos);
            con.AplicarEvento(Evento("sobrecalentamiento"));
            Assert.IsTrue(con.Decidir(Con(con, EfectoOpcion.UsarHerramienta)));
            CollectionAssert.Contains(con.Encendidos.ToList(), "radar");
        }

        [Test]
        public void LaPerdidaDeOrientacionDejaUnMesSinLecturas()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            op.AplicarEvento(Evento("orientacion"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            Assert.AreEqual(Lectura.SinLectura, Resolver(op).Observaciones[0].Lectura);
            Assert.AreEqual(Lectura.Encontrada, Resolver(op).Observaciones[0].Lectura);
        }

        [Test]
        public void LaRespuestaDeHerramientaSoloApareceSiSeCargo()
        {
            var sin = Caso2("refrigeracion", "giroscopio");   // 40 kg libres: 4 de combustible, sin batería
            sin.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(sin.Opciones.Any(o => o.efecto == EfectoOpcion.UsarHerramienta));
            Assert.IsTrue(sin.PuedeElegir(Con(sin, EfectoOpcion.Acortar)), "pide 4 de combustible y hay 4");
            var pobre = Nueva(new Configuracion("media", "explorador", new[] { "radar", "espectrometro" }), null);   // 0 de combustible
            pobre.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(pobre.PuedeElegir(Con(pobre, EfectoOpcion.Acortar)), "pide 4 de combustible y no hay");

            var con = Caso2("bateria");
            con.AplicarEvento(Evento("sombra"));
            Assert.IsTrue(con.Decidir(Con(con, EfectoOpcion.UsarHerramienta)));
            Assert.AreEqual(700, con.LimiteW);
            con.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(con.PuedeElegir(Con(con, EfectoOpcion.UsarHerramienta)), "ya se usó");
        }

        [Test]
        public void EnLaTormentaSePuedeBajarGratis()
        {
            var op = Titan();
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(Con(op, EfectoOpcion.BajarAltitud));
            Assert.AreEqual(Altitud.Baja, op.Altitud);
            Assert.AreEqual(20, op.Combustible);
            Assert.AreEqual(370, op.CreditosDisponibles);
        }

        [Test]
        public void LaVentanaDespejadaPuedeAbrirElContactoDelMesSiguiente()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            op.ProgramarEventos(new[] { Evento("ventana") }, new[] { 2 });
            Resolver(op);
            Resolver(op);
            op.Decidir(Con(op, EfectoOpcion.AbrirContacto));
            Assert.AreEqual(3, op.Mes);
            Assert.IsTrue(op.ContactoAbierto, "el mes 3 es impar, pero la ventana lo abre");
            Assert.IsNull(Resolver(op, enviar: true).EnvioFallido);
            Assert.IsTrue(op.ContactoAbierto, "el mes 4 es par");
        }

        [Test]
        public void SiElMesSiguienteYaTeniaContactoLaVentanaAbreElDeDespues()
        {
            var op = Caso2();
            op.ProgramarEventos(new[] { Evento("ventana") }, new[] { 3 });
            for (int i = 0; i < 3; i++) Resolver(op);
            Assert.AreEqual(4, op.Mes);
            Assert.IsTrue(op.ContactoAbierto, "el mes 4 ya es par");
            op.Decidir(Con(op, EfectoOpcion.AbrirContacto));
            Resolver(op);
            Assert.AreEqual(5, op.Mes);
            Assert.IsTrue(op.ContactoAbierto, "la ventana extra cae en el mes 5, que no la tenía");
            Resolver(op);
            Assert.IsTrue(op.ContactoAbierto, "mes 6, par");
            Resolver(op);
            Assert.IsFalse(op.ContactoAbierto, "mes 7: la ventana extra ya pasó");
        }

        [Test]
        public void UnaHerramientaDejaDeSerUtilCuandoElEfectoYaPaso()
        {
            var op = Caso2("giroscopio");
            Observar(op, "termica", Lejos);
            op.AplicarEvento(Evento("orientacion"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            CollectionAssert.Contains(op.HerramientasUtiles.ToList(), "giroscopio", "todavía evitaría el mes sin lecturas");
            Assert.AreEqual(Lectura.SinLectura, Resolver(op).Observaciones[0].Lectura);
            CollectionAssert.IsEmpty(op.HerramientasUtiles.ToList(), "el mes sin lecturas ya pasó");
            Assert.IsFalse(op.UsarHerramienta("giroscopio"));
        }

        [Test]
        public void ElInformeNoCulpaALaConfiguracionSiFaltoEnviar()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Observar(op, "radar", Centro);
            Resolver(op);
            op.Terminar();
            var informe = InformeMision.Construir(Evaluador.Evaluar(ConfigCaso2(), catalogo), ConfigCaso2(), catalogo, op, null);
            Assert.AreEqual(TextosResultado.Titulo(CategoriaResultado.Fracaso), informe.Titulo);
            Assert.AreEqual(TextosResultado.TextoSinEnviar, informe.Texto);
        }

        [Test]
        public void LaPeticionDeLaNasaSeCumpleObservandoSuPunto()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("peticion"));
            op.Decidir(Con(op, EfectoOpcion.AceptarObjetivo));
            op.PonerPuntoPeticion(new Vector2(0.2f, 0.2f));
            Observar(op, "termica", new Vector2(0.22f, 0.2f));
            Resolver(op);
            Assert.IsTrue(op.PeticionCumplida);
            var e = Evaluador.Evaluar(ConfigCaso2(), catalogo);
            Assert.AreEqual(150, Puntuacion.Desglose(e, op, catalogo, null).First(l => l.Concepto.StartsWith("NASA request")).Puntos);
        }

        [Test]
        public void ElAzarProgramaLosEventosDelNivelYSeRepiteConLaMismaSemilla()
        {
            var c = ConfigCaso2();
            var e = Evaluador.Evaluar(c, catalogo, catalogo.Nivel(2));
            Operacion Jugar()
            {
                var op = new Operacion(e, c, catalogo, catalogo.Nivel(2), 42, new Carga(e, c, catalogo, catalogo.Nivel(2)));
                for (int i = 0; i < 40 && !op.Terminada; i++)
                {
                    if (op.EsperandoDecision) op.Decidir(op.Opciones.First(op.PuedeElegir));
                    else op.ResolverMes(op.Mes);
                }
                return op;
            }
            var a = Jugar();
            var b = Jugar();
            Assert.AreEqual(5, a.Eventos.Count, "nivel 3: cinco eventos");
            Assert.AreEqual(a.Anomalia, b.Anomalia);
            CollectionAssert.AreEqual(a.Eventos.Select(x => x.Datos.id), b.Eventos.Select(x => x.Datos.id));
            Assert.AreEqual(5, a.Eventos.Select(x => x.Datos.id).Distinct().Count(), "sin repetir");
        }

        // ---------- Objetivos, puntos e informe ----------

        /// <summary>Caso 2 ganado en 2 meses: observa con los dos en el mes 1 y envía en el 2.</summary>
        Operacion Ganada()
        {
            var op = Caso2();
            Observar(op, "termica", Centro);
            Observar(op, "radar", Centro);
            Resolver(op);
            Resolver(op, enviar: true);
            op.Terminar();
            return op;
        }

        [Test]
        public void LosPuntosSalenDeLoEnviadoYDeLoQueSobra()
        {
            var op = Ganada();
            var e = Evaluador.Evaluar(ConfigCaso2(), catalogo);
            var lineas = Puntuacion.Desglose(e, op, catalogo, null);
            // 1,000 de victoria + 430 / 2 + 10 meses × 25 + 7 de combustible × 10
            Assert.AreEqual(1000 + 215 + 250 + 70, Puntuacion.Total(lineas));
            Assert.IsFalse(lineas.Any(l => l.Concepto.Contains("low quality")));
        }

        [Test]
        public void SinEnviarNadaLosPuntosSonLosDelFracaso()
        {
            var op = Caso2();
            op.Terminar();
            var e = Evaluador.Evaluar(ConfigCaso2(), catalogo);
            Assert.AreEqual(100, Puntuacion.Desglose(e, op, catalogo, null)[0].Puntos);
        }

        [Test]
        public void SinEvidenciaEnviadaNoSumaNadaDeLoQueSobra()
        {
            // Abandonar en el mes 2 sin evidencia no debe acercarse a una partida jugada (antes daba 745 puntos).
            var op = Caso2();
            Observar(op, "termica", Lejos);
            Resolver(op);
            Resolver(op, enviar: true);   // envía datos, pero sin evidencia
            op.Terminar();
            var lineas = Puntuacion.Desglose(Evaluador.Evaluar(ConfigCaso2(), catalogo), op, catalogo, null);
            Assert.IsFalse(lineas.Any(l => l.Concepto.StartsWith("Budget")));
            Assert.IsFalse(lineas.Any(l => l.Concepto.StartsWith("Months")));
            Assert.IsFalse(lineas.Any(l => l.Concepto.StartsWith("Fuel")));
            Assert.AreEqual(100, Puntuacion.Total(lineas), "solo los puntos del fracaso");

            var parcial = Caso2();
            Observar(parcial, "termica", Centro);
            Resolver(parcial);
            Resolver(parcial, enviar: true);   // una evidencia principal
            parcial.Terminar();
            var conEvidencia = Puntuacion.Desglose(Evaluador.Evaluar(ConfigCaso2(), catalogo), parcial, catalogo, null);
            Assert.IsTrue(conEvidencia.Any(l => l.Concepto.StartsWith("Budget")));
            Assert.IsTrue(conEvidencia.Any(l => l.Concepto.StartsWith("Months")));
            Assert.AreEqual(70, conEvidencia.First(l => l.Concepto.StartsWith("Fuel")).Puntos, "7 de combustible × 10");
        }

        [Test]
        public void LasManiobrasSeRestanDelPresupuestoSobrante()
        {
            var op = Titan();
            op.PlanearManiobra(Altitud.Baja);
            Observar(op, "termica", Centro);
            Resolver(op);
            Resolver(op, enviar: true);   // lo sobrante solo cuenta con evidencia enviada
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" });
            var linea = Puntuacion.Desglose(Evaluador.Evaluar(c, catalogo, catalogo.Nivel(0)), op, catalogo, catalogo.Nivel(0))
                .First(l => l.Concepto.StartsWith("Budget"));
            Assert.AreEqual((370 - 40) / 2, linea.Puntos);
        }

        [Test]
        public void LosObjetivosSeEvaluanConLaCampana()
        {
            var op = Ganada();
            ObjetivoDatos O(string id) => catalogo.objetivos.First(o => o.id == id);
            Assert.IsTrue(Objetivos.Cumplido(O("rapido"), op), "4 observaciones");
            Assert.IsTrue(Objetivos.Cumplido(O("sin_apagones"), op));
            Assert.IsTrue(Objetivos.Cumplido(O("sin_herramientas"), op));
            Assert.IsTrue(Objetivos.Cumplido(O("enviar_pronto"), op), "quedaban 10 meses");
            Assert.IsTrue(Objetivos.Cumplido(O("combustible"), op), "7 de combustible llegan a 5");

            var sinEnviar = Caso2();
            Observar(sinEnviar, "termica", Centro);
            Resolver(sinEnviar);
            sinEnviar.Terminar();
            Assert.IsFalse(Objetivos.Cumplido(O("rapido"), sinEnviar));
            Assert.IsFalse(Objetivos.Cumplido(O("enviar_pronto"), sinEnviar));
        }

        [Test]
        public void CadaCampanaTraeDosObjetivosPosibles()
        {
            var c = ConfigCaso2();
            var e = Evaluador.Evaluar(c, catalogo);
            for (int semilla = 0; semilla < 20; semilla++)
            {
                var op = new Operacion(e, c, catalogo, null, semilla, new Carga(e, c, catalogo));
                Assert.AreEqual(2, op.Objetivos.Distinct().Count());
            }
            // Con menos de 5 de combustible al salir, el objetivo de combustible no sale: sería imposible.
            var c2 = new Configuracion("media", "explorador", new[] { "radar", "espectrometro" });   // 0 de combustible
            var e2 = Evaluador.Evaluar(c2, catalogo);
            for (int semilla = 0; semilla < 20; semilla++)
                Assert.IsFalse(new Operacion(e2, c2, catalogo, null, semilla, new Carga(e2, c2, catalogo)).Objetivos
                    .Any(o => o.tipo == TipoObjetivo.CombustibleMinimo), "semilla " + semilla);
        }

        [Test]
        public void ElInformeSoloCuentaLoQueLlegoALaTierra()
        {
            var ganada = InformeMision.Construir(Evaluador.Evaluar(ConfigCaso2(), catalogo), ConfigCaso2(), catalogo, Ganada(), null);
            Assert.AreEqual(TextosResultado.Titulo(CategoriaResultado.Victoria), ganada.Titulo);
            Assert.AreEqual(2, ganada.Evidencias.Count);
            StringAssert.Contains("The mission lasted 2 months, with 10 left over.", ganada.Recursos);

            var op = Caso2();
            Observar(op, "termica", Centro);
            Observar(op, "radar", Lejos);
            Resolver(op);
            Resolver(op, enviar: true);
            op.Terminar();
            var parcial = InformeMision.Construir(Evaluador.Evaluar(ConfigCaso2(), catalogo), ConfigCaso2(), catalogo, op, null);
            Assert.AreEqual(TextosResultado.Titulo(CategoriaResultado.Parcial), parcial.Titulo);
            Assert.AreEqual(1, parcial.Evidencias.Count);
            Assert.IsTrue(parcial.Faltantes.Any(f => f.StartsWith("Subsurface radar")), "el radar no encontró nada");
        }
    }
}
