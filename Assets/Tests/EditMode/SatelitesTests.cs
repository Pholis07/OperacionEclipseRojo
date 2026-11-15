using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Mecánicas de los satélites (aprobadas el 2026-11-14): datos, Carga y Operación.</summary>
    public class SatelitesTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        SateliteDatos S(string id) => catalogo.Satelite(id);

        [TestCase("mro", 0, 0)]
        [TestCase("magellan", 30, 10)]
        [TestCase("chandra", 30, 40)]
        [TestCase("aquarius", 40, 20)]
        [TestCase("llcd", 50, 10)]
        [TestCase("aztechsat", 15, 0)]
        public void LosSatelitesTienenElCostoYLaMasaAprobados(string id, int costo, int masa)
        {
            Assert.AreEqual(costo, S(id).costoCr);
            Assert.AreEqual(masa, S(id).masaKg);
            Assert.IsNotEmpty(S(id).ventaja);
            Assert.IsNotEmpty(S(id).desventaja);
        }

        [Test]
        public void ElMroEsElPrimeroYEsNeutro()
        {
            var mro = catalogo.satelites[0];
            Assert.AreEqual("mro", mro.id, "es el que se ve sin elegir satélite");
            Assert.AreEqual(1f, mro.factorAlcance);
            Assert.AreEqual(-1, mro.combustiblePorCambioAltitud);
            Assert.AreEqual(0, mro.memoriaExtra);
            Assert.IsFalse(mro.inmuneOrientacion || mro.inmuneTormenta || mro.tormentaBloqueaEnvio);
            Assert.AreEqual(0, mro.consumoExtraW);
            Assert.AreEqual(-1, mro.vatiosTransmisor);
            Assert.AreEqual(-1, mro.cicloContacto);
        }

        [Test]
        public void LosEfectosSonLosAprobados()
        {
            CollectionAssert.AreEqual(new[] { "radar" }, S("magellan").instrumentosConAlcance);
            Assert.AreEqual(1.3f, S("magellan").factorAlcance);
            Assert.AreEqual(2, S("magellan").combustiblePorCambioAltitud);
            Assert.AreEqual(-1, S("magellan").memoriaExtra);

            CollectionAssert.AreEqual(new[] { "termica", "alta_res" }, S("chandra").instrumentosConAlcance);
            Assert.AreEqual(1.2f, S("chandra").factorAlcance);
            Assert.IsTrue(S("chandra").inmuneOrientacion);

            Assert.AreEqual(1, S("aquarius").memoriaExtra);
            Assert.IsTrue(S("aquarius").inmuneTormenta);
            Assert.AreEqual(50, S("aquarius").consumoExtraW);

            Assert.AreEqual(50, S("llcd").vatiosTransmisor);
            Assert.AreEqual(1, S("llcd").cicloContacto, "versión 4: contacto todos los meses");
            Assert.IsTrue(S("llcd").tormentaBloqueaEnvio);
        }

        // ---------- Carga ----------

        Carga CargaCaso2(string satelite, MisionDatos nivel = null)
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = satelite };   // sobran 430 cr y 30 kg
            return new Carga(Evaluador.Evaluar(c, catalogo, nivel), c, catalogo, nivel);
        }

        [Test]
        public void SinSateliteElegidoSeUsaElMro()
        {
            var carga = CargaCaso2(null);
            Assert.AreEqual("mro", carga.Satelite.id);
            Assert.AreEqual(430, carga.CreditosDisponibles);
            Assert.AreEqual(3, carga.Combustible);
            Assert.IsTrue(carga.PuedeLanzar);
        }

        [Test]
        public void ElSateliteSeCobraEnLaCargaYQuitaCombustible()
        {
            var carga = CargaCaso2("magellan");
            Assert.AreEqual(400, carga.CreditosDisponibles);
            Assert.AreEqual(20, carga.MasaLibreKg);
            Assert.AreEqual(2, carga.Combustible);
            Assert.IsTrue(carga.Cargar("bateria"));
            Assert.AreEqual(0, carga.Combustible);
            Assert.AreEqual(70, carga.CreditosGastados);
        }

        [Test]
        public void SiNoAlcanzaSeBloqueaLanzarHastaUsarElMro()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "chandra" };
            var carga = new Carga(Evaluador.Evaluar(c, catalogo), c, catalogo);
            Assert.AreEqual(-10, carga.MasaLibreKg, "40 kg no caben en 30");
            Assert.AreEqual(0, carga.Combustible);
            Assert.IsFalse(carga.PuedeLanzar);
            Assert.IsFalse(carga.Cargar("reconfiguracion"));

            carga.UsarMro();
            Assert.AreEqual("mro", carga.Satelite.id);
            Assert.AreEqual("mro", c.SateliteId, "la configuración también cambia");
            Assert.IsTrue(carga.PuedeLanzar);
            Assert.AreEqual(3, carga.Combustible);
        }

        [Test]
        public void EnNivel3ConVictoriaEnElipticaSoloCabenElMroYElAztechSat()
        {
            foreach (var s in catalogo.satelites)
            {
                var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }) { SateliteId = s.id };
                var carga = new Carga(Evaluador.Evaluar(c, catalogo, catalogo.Nivel(2)), c, catalogo, catalogo.Nivel(2));
                Assert.AreEqual(s.id == "mro" || s.id == "aztechsat", carga.PuedeLanzar, s.id + ": sobran 20 cr");
            }
        }

        [Test]
        public void LoPagadoPorElSateliteSeRestaDeLosPuntos()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "llcd" };
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            // Desde que lo sobrante solo cuenta con evidencia enviada, se envía una antes de mirar los puntos.
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(new Vector2(0.5f, 0.5f));
            op.Encender("termica");
            op.FijarRegion("termica", new Vector2(0.5f, 0.5f));
            op.ResolverMes(op.Mes);
            op.TransmitirPlaneado = true;
            op.ResolverMes(op.Mes);
            var linea = Puntuacion.Desglose(e, op, catalogo, null).First(l => l.Concepto.StartsWith("Budget"));
            Assert.AreEqual((1250 - 820 - 50) / 2, linea.Puntos);
        }

        // ---------- Operación por meses ----------

        /// <summary>Titán en elíptica, nivel 1 (sobran 370 cr y 80 kg; 800 W; sin eventos), anomalía en el centro.</summary>
        Operacion Nueva(string satelite)
        {
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }) { SateliteId = satelite };
            var nivel = catalogo.Nivel(0);
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var op = new Operacion(e, c, catalogo, nivel, 1, new Carga(e, c, catalogo, nivel));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            return op;
        }

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

        EventoDatos Evento(string id) => catalogo.eventos.First(x => x.id == id);
        static Vector2 A(float d) => new Vector2(0.5f + d, 0.5f);

        [Test]
        public void ConElMroLaOperacionEsLaNormal()
        {
            var op = Nueva("mro");
            Assert.AreEqual(6, op.MemoriaCapacidad);
            Assert.AreEqual(100, op.ConsumoW);
            Assert.AreEqual(800, op.LimiteW);
            Assert.AreEqual(3, op.CostoCambioAltitud);
            Assert.AreEqual(150, op.VatiosTransmisor);
            Assert.AreEqual(2, op.CicloContacto);
            Observar(op, "termica", A(0.14f));
            Assert.AreEqual(Lectura.Caliente, Resolver(op).Observaciones[0].Lectura, "0.14: fuera del radio de 0.12");
        }

        [Test]
        public void MagellanLeeMasLejosConElRadarYManiobraMasBarato()
        {
            var op = Nueva("magellan");   // 80 − 10 = 70 kg: 7 de combustible
            Assert.AreEqual(5, op.MemoriaCapacidad, "6 − 1");
            Observar(op, "termica", A(0.15f));
            Observar(op, "radar", A(0.15f));
            var r = Resolver(op);
            Assert.AreEqual(Lectura.Caliente, r.Observaciones[0].Lectura, "la térmica no cambia");
            Assert.AreEqual(Lectura.Encontrada, r.Observaciones[1].Lectura, "radar: 0.15 / 1.3 < 0.12");

            Assert.IsTrue(op.PlanearManiobra(Altitud.Baja));
            Resolver(op);
            Assert.AreEqual(7 - 2 - 1, op.Combustible, "2 de la maniobra y 1 de arrastre");
        }

        [Test]
        public void ChandraLeeMasLejosConLasCamarasYNoSeDesorienta()
        {
            var op = Nueva("chandra");
            Assert.AreEqual(4, op.Combustible, "40 kg menos");
            op.AplicarEvento(Evento("orientacion"));
            Assert.IsFalse(op.EsperandoDecision);
            Assert.IsTrue(op.UltimoEvento.EvitadoPorSatelite);
            Observar(op, "termica", A(0.14f));
            Assert.AreEqual(Lectura.Encontrada, Resolver(op).Observaciones[0].Lectura, "0.14 / 1.2 < 0.12, y con lectura");
        }

        [Test]
        public void AquariusTieneMasMemoriaConsumeMasYNoPierdeCalidadEnLaTormenta()
        {
            var op = Nueva("aquarius");
            Assert.AreEqual(7, op.MemoriaCapacidad);
            Assert.AreEqual(150, op.ConsumoW, "100 de base + 50 del radiómetro");
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Observar(op, "termica", Centro);
            var r = Resolver(op);
            Assert.AreEqual(70f, r.Observaciones[0].Calidad, 0.01f);
            CollectionAssert.AreEqual(new[] { "TX-01" }, r.Observaciones[0].Evidencias);
        }

        [Test]
        public void LlcdTieneContactoTodosLosMesesYTransmiteConMenosPotencia()
        {
            var op = Nueva("llcd");
            Assert.AreEqual(50, op.VatiosTransmisor);
            Assert.IsTrue(op.ContactoAbierto, "mes 1: con el MRO no habría contacto");
            Observar(op, "termica", Centro);
            var r = Resolver(op, enviar: true);
            CollectionAssert.AreEqual(new[] { "TX-01" }, r.Enviadas, "observa y envía el mismo mes");
            Assert.IsTrue(op.ContactoAbierto, "mes 2");
        }

        [Test]
        public void ConLlcdSeguirEnLaTormentaBloqueaElEnvioDelMesSiguiente()
        {
            var op = Nueva("llcd");
            Observar(op, "termica", Centro);
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Assert.IsTrue(op.EnvioBloqueado);
            var r = Resolver(op, enviar: true);
            StringAssert.Contains("laser", r.EnvioFallido);
            Assert.AreEqual(1, op.Memoria.Count, "los datos siguen en la memoria");
            Assert.IsFalse(op.EnvioBloqueado);
            Assert.IsNull(Resolver(op, enviar: true).EnvioFallido, "solo un mes");
        }

        [Test]
        public void ConLlcdBajarEnLaTormentaNoBloqueaElEnvio()
        {
            var op = Nueva("llcd");
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.BajarAltitud));
            Assert.IsFalse(op.EnvioBloqueado);
        }

        // ---------- Pantalla de Carga ----------

        [Test]
        public void EnLaPantallaDeCargaSinAlcanceSeBloqueaLanzarYUsarElMroLoDesbloquea()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "chandra" };
            var ui = Object.FindFirstObjectByType<CargaUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "Falta CargaUI en la escena Mision");
            ui.Mostrar(Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo));

            var so = new SerializedObject(ui);
            var lanzar = (UnityEngine.UI.Button)so.FindProperty("botonLanzar").objectReferenceValue;
            var usarMro = (UnityEngine.UI.Button)so.FindProperty("botonUsarMro").objectReferenceValue;
            var texto = (TMPro.TMP_Text)so.FindProperty("textoRecursos").objectReferenceValue;
            Assert.IsFalse(lanzar.interactable);
            Assert.IsTrue(usarMro.gameObject.activeSelf);
            StringAssert.Contains("Chandra Observatory", texto.text);
            StringAssert.Contains("is not enough", texto.text);

            ui.UsarMro();
            Assert.IsTrue(lanzar.interactable);
            Assert.IsFalse(usarMro.gameObject.activeSelf);
            Assert.AreEqual("mro", EstadoJuego.Configuracion.SateliteId);
        }

        [Test]
        public void ElInformeDiceElSatelite()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "magellan" };
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            op.Terminar();
            StringAssert.Contains("Satellite: Magellan Mapper.", InformeMision.Construir(e, c, catalogo, op, null).Recursos);
        }

        // ---------- AztechSat-1 ----------

        [Test]
        public void ElAztechSatTieneLosDatosAprobados()
        {
            var a = S("aztechsat");
            Assert.AreEqual("AztechSat-1 Nanosatellite", a.nombre);
            Assert.IsTrue(a.envioSinVentana);
            Assert.AreEqual(2, a.bloquesEnvioSinVentana, "versión 4: sin contacto caben 2 bloques (antes costaba 1 órbita)");
            Assert.AreEqual(-100, a.limiteExtraW);
            Assert.AreEqual(-1, a.memoriaExtra);
            Assert.AreEqual("mro", catalogo.satelites[0].id);
            Assert.AreEqual(6, catalogo.satelites.Count);
        }

        [Test]
        public void ElAztechSatTieneMenosPotenciaYMemoria()
        {
            var op = Nueva("aztechsat");
            Assert.AreEqual(700, op.LimiteW, "800 − 100");
            Assert.AreEqual(5, op.MemoriaCapacidad, "6 − 1");
            Assert.AreEqual(8, op.Combustible, "no pesa: 80 kg libres");
        }

        [Test]
        public void ElAztechSatEnviaSinContactoPeroSoloDosBloques()
        {
            var op = Nueva("aztechsat");
            Assert.IsFalse(op.ContactoAbierto, "mes 1");
            Observar(op, "termica", Centro);   // 1 bloque
            Observar(op, "radar", Centro);     // 2 bloques
            var r = Resolver(op, enviar: true);
            CollectionAssert.AreEqual(new[] { "TX-01" }, r.Enviadas, "cabe la térmica; el radar ya no (1 + 2 > 2)");
            Assert.AreEqual(1, r.BloquesEnviados);
            Assert.AreEqual(2, op.MemoriaUsada, "el registro del radar sigue en la memoria");

            op.Apagar("termica");
            op.Apagar("radar");
            var mes2 = Resolver(op, enviar: true);   // mes par: contacto normal, sin límite
            CollectionAssert.AreEqual(new[] { "TX-02" }, mes2.Enviadas);
            Assert.AreEqual(0, op.MemoriaUsada);
        }

        [Test]
        public void ElSateliteNoCambiaLaEvaluacion()
        {
            foreach (var s in catalogo.satelites)
            {
                var c = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = s.id };
                var e = Evaluador.Evaluar(c, catalogo);
                Assert.AreEqual(CategoriaResultado.Victoria, e.Categoria, s.id);
                Assert.AreEqual(820, e.Calculo.CostoCr, s.id);
                Assert.AreEqual(620, e.Calculo.MasaKg, s.id);
                Assert.AreEqual(430, e.Calculo.PotenciaW, s.id);
            }
        }
    }
}
