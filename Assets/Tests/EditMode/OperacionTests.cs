using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Gameplay aprobado (PROPUESTA-GAMEPLAY.md v2): energía, apagón, órbitas, escaneo, eventos, herramientas, puntos y niveles.</summary>
    public class OperacionTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        /// <summary>Caso 2 (Explorador, media inclinada, térmica y radar), sin eventos y con la anomalía en el centro.</summary>
        Operacion Caso2(MisionDatos nivel = null)
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var op = new Operacion(Evaluador.Evaluar(c, catalogo, nivel), c, catalogo, nivel, 1);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            return op;
        }

        EventoDatos Evento(string id) => catalogo.eventos.First(e => e.id == id);

        [Test]
        public void AlEmpezarLlegaTX00YQuedanLasTransmisionesPorEncontrar()
        {
            var op = Caso2();
            CollectionAssert.AreEqual(new[] { "TX-00" }, op.Recibidas);
            CollectionAssert.AreEquivalent(new[] { "TX-01", "TX-02" }, op.Pendientes.Keys);
            Assert.AreEqual(10, op.Orbitas);
            Assert.AreEqual(700, op.LimiteW);
            Assert.AreEqual(100, op.ConsumoW, "solo la nave: los instrumentos empiezan apagados");
        }

        [Test]
        public void EncenderSumaElConsumoTotalDelInstrumento()
        {
            var op = Caso2();
            Assert.IsTrue(op.Encender("termica"));
            Assert.IsTrue(op.Encender("radar"));
            Assert.AreEqual(430, op.ConsumoW);
            Assert.AreEqual(0, op.Apagones);
            Assert.IsFalse(op.Encender("espectrometro"), "no se puede encender un instrumento que no lleva la nave");
        }

        [Test]
        public void EscanearGastaUnaOrbitaYDaLecturaSegunLaDistancia()
        {
            var op = Caso2();
            op.Encender("termica");
            Assert.AreEqual(Lectura.Frio, op.Escanear("termica", Lejos));
            Assert.AreEqual(9, op.Orbitas);
            Assert.AreEqual(Lectura.Tibio, op.Escanear("termica", Centro + new Vector2(0.35f, 0)));
            Assert.AreEqual(Lectura.Caliente, op.Escanear("termica", Centro + new Vector2(0.2f, 0)));
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("termica", Centro + new Vector2(0.05f, 0)));
            CollectionAssert.AreEqual(new[] { "TX-00", "TX-01" }, op.Recibidas);
            Assert.AreEqual(6, op.Orbitas);
        }

        [Test]
        public void ConElInstrumentoApagadoNoSeEscaneaNiSeGastaTiempo()
        {
            var op = Caso2();
            Assert.IsNull(op.Escanear("radar", Centro));
            Assert.AreEqual(10, op.Orbitas);
        }

        [Test]
        public void SiElConsumoSuperaElLimiteLaNaveSeApagaPeroNoSePierde()
        {
            var op = Caso2();
            op.Encender("termica");
            op.Encender("radar");                         // 430 W
            op.AplicarEvento(Evento("micrometeorito"));   // límite 500: aguanta
            Assert.AreEqual(0, op.Apagones);
            op.AplicarEvento(Evento("sombra"));           // límite 350 < 430: apagón

            Assert.AreEqual(1, op.Apagones);
            CollectionAssert.IsEmpty(op.Encendidos);
            Assert.AreEqual(8, op.Orbitas, "pierde 2 órbitas");
            Assert.AreEqual(50, op.CreditosPerdidos);
            Assert.IsFalse(op.Terminada, "un apagón no termina la misión");
        }

        [Test]
        public void EncenderDeMasProvocaApagon()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("micrometeorito"));   // 500 W
            op.AplicarEvento(Evento("sombra"));           // 350 W
            op.Encender("radar");                         // 300 W: cabe
            Assert.AreEqual(0, op.Apagones);
            op.Encender("termica");                       // 430 W: no cabe
            Assert.AreEqual(1, op.Apagones);
        }

        [Test]
        public void LasHerramientasResuelvenSuEventoUnaSolaVez()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("sombra"));
            Assert.AreEqual(550, op.LimiteW);
            CollectionAssert.Contains(op.HerramientasUtiles.ToList(), "bateria");
            Assert.IsTrue(op.UsarHerramienta("bateria"));
            Assert.AreEqual(700, op.LimiteW);
            op.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(op.UsarHerramienta("bateria"), "cada herramienta se usa una vez por misión");
            Assert.IsFalse(op.UsarHerramienta("giroscopio"), "una herramienta sin evento que resolver no hace nada");
        }

        [Test]
        public void LaSombraSeVaSolaTrasSusOrbitas()
        {
            var op = Caso2();
            op.Encender("termica");
            op.AplicarEvento(Evento("sombra"));
            for (int i = 0; i < 3; i++) op.Escanear("termica", Lejos);
            Assert.AreEqual(700, op.LimiteW);
        }

        [Test]
        public void ElSobrecalentamientoBloqueaElQueMasConsumeHastaRefrigerarlo()
        {
            var op = Caso2();
            op.Encender("termica");
            op.Encender("radar");
            op.AplicarEvento(Evento("sobrecalentamiento"));
            CollectionAssert.DoesNotContain(op.Encendidos.ToList(), "radar");
            Assert.IsFalse(op.Encender("radar"));
            Assert.IsTrue(op.UsarHerramienta("refrigeracion"));
            Assert.IsTrue(op.Encender("radar"));
        }

        [Test]
        public void LaPerdidaDeOrientacionAnulaLaProximaLectura()
        {
            var op = Caso2();
            op.Encender("termica");
            op.AplicarEvento(Evento("orientacion"));
            Assert.AreEqual(Lectura.SinLectura, op.Escanear("termica", Centro));
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("termica", Centro));
        }

        [Test]
        public void LaVentanaDespejadaDaUnaOrbita()
        {
            var op = Caso2();
            op.AplicarEvento(Evento("ventana"));
            Assert.AreEqual(11, op.Orbitas);
        }

        [Test]
        public void SiSeAcabanLasOrbitasLoPendienteLlegaConBajaCalidad()
        {
            var op = Caso2();
            op.Encender("termica");
            for (int i = 0; i < 10; i++) op.Escanear("termica", Lejos);
            Assert.IsTrue(op.Terminada);
            CollectionAssert.AreEqual(new[] { "TX-01", "TX-02" }, op.BajaCalidad);
            Assert.IsNull(op.Escanear("termica", Centro), "terminada: no se puede seguir");
        }

        [Test]
        public void LosEventosProgramadosOcurrenTrasSuEscaneo()
        {
            var op = Caso2();
            op.ProgramarEventos(new[] { Evento("micrometeorito") }, new[] { 2 });
            op.Encender("termica");
            op.Escanear("termica", Lejos);
            Assert.AreEqual(700, op.LimiteW);
            op.Escanear("termica", Lejos);
            Assert.AreEqual(500, op.LimiteW);
            Assert.AreEqual("micrometeorito", op.UltimoEvento.Datos.id);
        }

        [Test]
        public void ElAzarProgramaTantosEventosComoDiceElNivelYSeRepiteConLaMismaSemilla()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var a = new Operacion(e, c, catalogo, null, 42);
            var b = new Operacion(e, c, catalogo, null, 42);
            Assert.AreEqual(a.Anomalia, b.Anomalia);
            foreach (var op in new[] { a, b })
            {
                // Escanea hasta el escaneo 8 (el último en que puede caer un evento con 10 órbitas),
                // resolviendo un sobrecalentamiento si apaga la térmica.
                for (int intentos = 0; op.Escaneos < 8 && intentos < 40; intentos++)
                {
                    if (!op.Encender("termica")) op.UsarHerramienta("refrigeracion");
                    op.Escanear("termica", Lejos);
                }
            }
            Assert.AreEqual(catalogo.mision.eventosPorMision, a.Eventos.Count, "nivel 2: dos eventos");
            CollectionAssert.AreEqual(a.Eventos.Select(x => x.Datos.id), b.Eventos.Select(x => x.Datos.id));
        }

        [Test]
        public void LaCategoriaDelResultadoNoDependeDeLaOperacion()
        {
            var op = Caso2();
            op.Encender("termica");
            for (int i = 0; i < 10; i++) op.Escanear("termica", Lejos);
            Assert.AreEqual(CategoriaResultado.Victoria, Evaluador.Evaluar(
                new Configuracion("media", "explorador", new[] { "termica", "radar" }), catalogo).Categoria);
        }

        [Test]
        public void PuntosDelCaso2EncontrandoTodoALaPrimera()
        {
            var op = Caso2();
            op.Encender("termica");
            op.Encender("radar");
            op.Escanear("termica", Centro);
            op.Escanear("radar", Centro);
            Assert.IsTrue(op.Completa);

            var e = Evaluador.Evaluar(new Configuracion("media", "explorador", new[] { "termica", "radar" }), catalogo);
            var lineas = Puntuacion.Desglose(e, op, catalogo, null);
            // 1,000 por victoria + (1,250 − 820) / 2 = 215 + 8 órbitas × 25 = 200
            Assert.AreEqual(1415, Puntuacion.Total(lineas));
        }

        [Test]
        public void ApagonesYBajaCalidadRestanPuntos()
        {
            var op = Caso2();
            op.Encender("termica");
            op.Encender("radar");
            op.AplicarEvento(Evento("micrometeorito"));
            op.AplicarEvento(Evento("sombra"));           // apagón: −50 cr y −50 puntos
            op.Terminar();                                // TX-01 y TX-02 con baja calidad: −200

            var e = Evaluador.Evaluar(new Configuracion("media", "explorador", new[] { "termica", "radar" }), catalogo);
            // 1,000 + (1,250 − 820 − 50) / 2 = 190 + 8 × 25 = 200 − 200 − 50
            Assert.AreEqual(1140, Puntuacion.Total(Puntuacion.Desglose(e, op, catalogo, null)));
        }

        [TestCase(0, "1. Entrenamiento", 1500, 800, 12, 0)]
        [TestCase(1, "2. Misión estándar", 1250, 700, 10, 2)]
        [TestCase(2, "3. Tormenta severa", 1150, 650, 8, 3)]
        public void LosNivelesTienenLosValoresAprobados(int indice, string nombre, int presupuesto, int potencia, int orbitas, int eventos)
        {
            var n = catalogo.Nivel(indice);
            Assert.AreEqual(nombre, n.nombreNivel);
            Assert.AreEqual(presupuesto, n.presupuestoCr);
            Assert.AreEqual(potencia, n.potenciaDisponibleW);
            Assert.AreEqual(orbitas, n.orbitasNavegacion);
            Assert.AreEqual(eventos, n.eventosPorMision);
            Assert.AreEqual(catalogo.mision.objetivo, n.objetivo);
        }

        [Test]
        public void ElNivel2EsLaMisionDelDiseno()
        {
            Assert.AreSame(catalogo.mision, catalogo.Nivel(1));
        }

        [Test]
        public void EnElNivel3LaVictoriaEnElipticaCabe()
        {
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" });
            Assert.AreEqual(EstadoSemaforo.Verde, Evaluador.Evaluar(c, catalogo, catalogo.Nivel(2)).Estado);
        }

        [Test]
        public void CadaEventoTieneUnaHerramientaQueExisteSalvoLaVentana()
        {
            Assert.AreEqual(5, catalogo.eventos.Count);
            foreach (var e in catalogo.eventos.Where(e => e.efecto != EfectoEvento.GanarOrbitas))
                Assert.IsNotNull(catalogo.Herramienta(e.herramientaId), "sin herramienta: " + e.id);
        }
    }
}
