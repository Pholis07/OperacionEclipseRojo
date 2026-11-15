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
            Assert.AreEqual(2, S("llcd").cicloContacto);
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
            var linea = Puntuacion.Desglose(e, op, catalogo, null).First(l => l.Concepto.StartsWith("Presupuesto"));
            Assert.AreEqual((1250 - 820 - 50) / 2, linea.Puntos);
        }

        // ---------- Operación ----------

        /// <summary>Titán en elíptica, nivel 1 (sobran 370 cr y 80 kg), anomalía en el centro y térmica encendida.</summary>
        Operacion Nueva(string satelite)
        {
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }) { SateliteId = satelite };
            var nivel = catalogo.Nivel(0);
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var op = new Operacion(e, c, catalogo, nivel, 1, new Carga(e, c, catalogo, nivel));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Encender("termica");
            return op;
        }

        EventoDatos Evento(string id) => catalogo.eventos.First(x => x.id == id);
        static Vector2 A(float d) => new Vector2(0.5f + d, 0.5f);

        [Test]
        public void ConElMroLaOperacionEsLaDeSiempre()
        {
            var op = Nueva("mro");
            Assert.AreEqual(4, op.MemoriaCapacidad);
            Assert.AreEqual(230, op.ConsumoW);
            Assert.AreEqual(3, op.CostoCambioAltitud);
            Assert.AreEqual(150, op.VatiosTransmisor);
            Assert.AreEqual(3, op.CicloContacto);
            Assert.AreEqual(Lectura.Caliente, op.Escanear("termica", A(0.14f)), "0.14: fuera del radio de 0.12");
        }

        [Test]
        public void MagellanLeeMasLejosConElRadarYCambiaDeAltitudMasBarato()
        {
            var op = Nueva("magellan");   // 80 − 10 = 70 kg: 7 de combustible
            Assert.AreEqual(3, op.MemoriaCapacidad, "4 − 1");
            op.Encender("radar");
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("radar", A(0.15f)), "0.15 / 1.3 < 0.12");
            Assert.AreEqual(Lectura.Caliente, op.Escanear("termica", A(0.15f)), "la térmica no cambia");
            Assert.IsTrue(op.CambiarAltitud(Altitud.Baja));
            Assert.AreEqual(5, op.Combustible);
        }

        [Test]
        public void ChandraLeeMasLejosConLasCamarasYNoSeDesorienta()
        {
            var op = Nueva("chandra");
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("termica", A(0.14f)), "0.14 / 1.2 < 0.12");
            op.AplicarEvento(Evento("orientacion"));
            Assert.IsFalse(op.EsperandoDecision);
            Assert.IsTrue(op.UltimoEvento.EvitadoPorSatelite);
            Assert.AreNotEqual(Lectura.SinLectura, op.Escanear("termica", Lejos));
        }

        [Test]
        public void AquariusTieneMasMemoriaConsumeMasYNoSeConfundeEnLaTormenta()
        {
            var op = Nueva("aquarius");
            Assert.AreEqual(5, op.MemoriaCapacidad);
            Assert.AreEqual(280, op.ConsumoW, "100 + 50 + 130");
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Assert.AreEqual(Lectura.Frio, op.Escanear("termica", Lejos), "sin lectura falsa");
        }

        [Test]
        public void LlcdEnviaConMenosEnergiaYMasVentanas()
        {
            var op = Nueva("llcd");
            Assert.AreEqual(50, op.VatiosTransmisor);
            Assert.AreEqual(1, op.OrbitasParaContacto, "ciclo de 2");
            op.Escanear("termica", Centro);
            Assert.IsTrue(op.ContactoAbierto);
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));   // límite 600
            op.Encender("radar");
            op.Encender("atmosferico");   // 100 + 130 + 200 + 70 = 500: con 150 W no cabría, con 50 sí
            Assert.IsTrue(op.Enviar());
            Assert.AreEqual(0, op.Apagones);
        }

        [Test]
        public void ConLlcdSeguirEnLaTormentaBloqueaElEnvioEsaOrbita()
        {
            var op = Nueva("llcd");
            op.Escanear("termica", Centro);
            Assert.IsTrue(op.ContactoAbierto);
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Assert.IsTrue(op.EnvioBloqueado);
            Assert.IsFalse(op.Enviar());
            op.Esperar();
            op.Esperar();   // la siguiente ventana (ciclo de 2)
            Assert.IsFalse(op.EnvioBloqueado);
            Assert.IsTrue(op.Enviar());
        }

        [Test]
        public void ConLlcdBajarEnLaTormentaNoBloqueaElEnvio()
        {
            var op = Nueva("llcd");
            op.Escanear("termica", Centro);
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.BajarAltitud));
            Assert.IsFalse(op.EnvioBloqueado);
            Assert.IsTrue(op.Enviar());
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
            StringAssert.Contains("Observatorio Chandra", texto.text);
            StringAssert.Contains("No alcanza", texto.text);

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
            StringAssert.Contains("Satélite: Cartógrafo Magellan.", InformeMision.Construir(e, c, catalogo, op, null).Recursos);
        }

        // ---------- AztechSat-1 ----------

        [Test]
        public void ElAztechSatTieneLosDatosAprobados()
        {
            var a = S("aztechsat");
            Assert.AreEqual("Nanosatélite AztechSat-1", a.nombre);
            Assert.IsTrue(a.envioSinVentana);
            Assert.AreEqual(1, a.orbitasEnvioSinVentana);
            Assert.AreEqual(-100, a.limiteExtraW);
            Assert.AreEqual(-1, a.memoriaExtra);
            Assert.AreEqual("mro", catalogo.satelites[0].id);
            Assert.AreEqual(6, catalogo.satelites.Count);
        }

        [Test]
        public void ElAztechSatTieneMenosEnergiaYMemoria()
        {
            var op = Nueva("aztechsat");
            Assert.AreEqual(700, op.LimiteW, "800 − 100");
            Assert.AreEqual(3, op.MemoriaCapacidad, "4 − 1");
            Assert.AreEqual(8, op.Combustible, "no pesa: 80 kg libres");
        }

        [Test]
        public void ElAztechSatEnviaSinVentanaPagandoUnaOrbita()
        {
            var op = Nueva("aztechsat");
            op.Escanear("termica", Centro);   // órbita 1: la ventana normal es en 1 órbita más
            Assert.IsFalse(op.ContactoAbierto);
            Assert.IsTrue(op.PuedeEnviarSinVentana);
            Assert.IsTrue(op.Enviar());
            CollectionAssert.Contains(op.Recibidas, "TX-01");
            Assert.AreEqual(10, op.Orbitas, "12 − 1 del escaneo − 1 del envío por la constelación");

            op.Escanear("termica", Lejos);    // órbita 3: ventana cerrada
            op.Esperar();
            op.Esperar();                     // órbita 5: ventana normal
            Assert.IsTrue(op.ContactoAbierto);
            Assert.AreEqual(7, op.Orbitas);
            Assert.IsTrue(op.Enviar());
            Assert.AreEqual(7, op.Orbitas, "con la ventana abierta no cuesta");
        }

        [Test]
        public void SinElAztechSatNoSeEnviaConLaVentanaCerrada()
        {
            var op = Nueva("mro");
            op.Escanear("termica", Centro);
            Assert.IsFalse(op.PuedeEnviarSinVentana);
            Assert.IsFalse(op.Enviar());
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
