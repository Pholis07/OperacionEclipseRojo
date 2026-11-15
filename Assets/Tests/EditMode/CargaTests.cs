using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 3, paso 1 (PROPUESTA-GAMEPLAY.md): datos nuevos y Carga de la nave.</summary>
    public class CargaTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        Configuracion Caso2() => new Configuracion("media", "explorador", new[] { "termica", "radar" });   // 620 kg, 820 cr
        Configuracion TitanEliptica() => new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }); // 670 kg, 1,130 cr

        Carga NuevaCarga(Configuracion c, MisionDatos nivel = null) =>
            new Carga(Evaluador.Evaluar(c, catalogo, nivel), c, catalogo, nivel);

        [TestCase("bateria", 40, 20)]
        [TestCase("reconfiguracion", 30, 10)]
        [TestCase("refrigeracion", 30, 15)]
        [TestCase("giroscopio", 40, 15)]
        public void LasHerramientasTienenElCostoYLaMasaAprobados(string id, int costo, int masa)
        {
            var h = catalogo.Herramienta(id);
            Assert.AreEqual(costo, h.costoCr);
            Assert.AreEqual(masa, h.masaKg);
        }

        [Test]
        public void ReglasDeBodegaYCombustible()
        {
            Assert.AreEqual(2, catalogo.reglas.espaciosBodega);
            Assert.AreEqual(10, catalogo.reglas.kgPorCombustible);
            Assert.AreEqual(20, catalogo.reglas.combustibleMaximo);
            Assert.AreEqual(2, catalogo.reglas.objetivosPorMision);
        }

        [TestCase(0, 6)]
        [TestCase(1, 6)]
        [TestCase(2, 6)]
        public void LaMemoriaEsDeSeisBloquesDesdeLaVersion4(int nivel, int bloques)
        {
            Assert.AreEqual(bloques, catalogo.Nivel(nivel).memoriaBloques);
        }

        [Test]
        public void SinHerramientasLaMasaLibreSeVuelveCombustible()
        {
            var carga = NuevaCarga(Caso2());
            Assert.AreEqual(430, carga.CreditosDisponibles);
            Assert.AreEqual(30, carga.MasaLibreKg);
            Assert.AreEqual(3, carga.Combustible);
        }

        [Test]
        public void CadaHerramientaQuitaCreditosYCombustible()
        {
            var carga = NuevaCarga(Caso2());
            Assert.IsTrue(carga.Cargar("bateria"));
            Assert.AreEqual(390, carga.CreditosDisponibles);
            Assert.AreEqual(1, carga.Combustible, "quedan 10 kg libres");
            Assert.IsFalse(carga.Cargar("refrigeracion"), "15 kg no caben en 10 kg");
            Assert.IsTrue(carga.Cargar("reconfiguracion"));
            Assert.AreEqual(0, carga.Combustible);
            carga.Quitar("bateria");
            Assert.AreEqual(2, carga.Combustible);
            Assert.AreEqual(400, carga.CreditosDisponibles);
        }

        [Test]
        public void LaBodegaSoloTieneDosEspacios()
        {
            var carga = NuevaCarga(TitanEliptica(), catalogo.Nivel(0));   // 1,500 cr: sobra para todo
            Assert.AreEqual(8, carga.Combustible);
            Assert.IsTrue(carga.Cargar("reconfiguracion"));
            Assert.IsTrue(carga.Cargar("giroscopio"));
            Assert.AreEqual(0, carga.EspaciosLibres);
            Assert.IsFalse(carga.Cargar("refrigeracion"));
            Assert.IsFalse(carga.Cargar("giroscopio"), "no se carga dos veces");
        }

        [Test]
        public void EnNivel3ConVictoriaEnElipticaNoAlcanzaParaHerramientas()
        {
            var carga = NuevaCarga(TitanEliptica(), catalogo.Nivel(2));
            Assert.AreEqual(20, carga.CreditosDisponibles);
            Assert.IsFalse(catalogo.herramientas.Any(h => carga.PuedeCargar(h.id)));
        }

        [Test]
        public void ElCombustibleTieneTope()
        {
            var carga = NuevaCarga(new Configuracion("media", "titan", new[] { "atmosferico" }));   // 400 kg en 750
            Assert.AreEqual(20, carga.Combustible);
        }

        [Test]
        public void ConCargaSoloSirvenLasHerramientasQueSubieron()
        {
            var c = Caso2();
            var e = Evaluador.Evaluar(c, catalogo);
            var carga = new Carga(e, c, catalogo);
            carga.Cargar("bateria");
            var op = new Operacion(e, c, catalogo, null, 1, carga);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            Assert.AreEqual(1, op.Combustible);

            op.AplicarEvento(catalogo.eventos.First(x => x.id == "micrometeorito"));
            Assert.IsFalse(op.Opciones.Any(o => o.efecto == EfectoOpcion.UsarHerramienta), "la reconfiguración no subió");
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            CollectionAssert.IsEmpty(op.HerramientasUtiles.ToList());
            Assert.IsFalse(op.UsarHerramienta("reconfiguracion"));
            op.AplicarEvento(catalogo.eventos.First(x => x.id == "sombra"));
            Assert.IsTrue(op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.UsarHerramienta)));
            Assert.AreEqual(500, op.LimiteW, "la sombra quedó resuelta; sigue el micrometeorito");
        }

        [Test]
        public void LoGastadoEnLaCargaSeRestaDelSobrante()
        {
            var c = Caso2();
            var e = Evaluador.Evaluar(c, catalogo);
            var carga = new Carga(e, c, catalogo);
            carga.Cargar("bateria");
            var op = new Operacion(e, c, catalogo, null, 1, carga);
            // Desde que lo sobrante solo cuenta con evidencia enviada, se envía una antes de mirar los puntos.
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(new Vector2(0.5f, 0.5f));
            op.Encender("termica");
            op.FijarRegion("termica", new Vector2(0.5f, 0.5f));
            op.ResolverMes(op.Mes);
            op.TransmitirPlaneado = true;
            op.ResolverMes(op.Mes);
            var linea = Puntuacion.Desglose(e, op, catalogo, null).First(l => l.Concepto.StartsWith("Budget"));
            Assert.AreEqual((1250 - 820 - 40) / 2, linea.Puntos);
            Assert.AreEqual(CategoriaResultado.Victoria, e.Categoria, "la carga no cambia la categoría");
        }

        [Test]
        public void CadaEventoTieneRespuestasYLaDeHerramientaSoloSiHayHerramienta()
        {
            foreach (var ev in catalogo.eventos)
            {
                Assert.GreaterOrEqual(ev.opciones.Count, 2, ev.id);
                Assert.IsTrue(ev.opciones.All(o => !string.IsNullOrEmpty(o.texto)), ev.id);
                bool conHerramienta = ev.opciones.Any(o => o.efecto == EfectoOpcion.UsarHerramienta);
                Assert.AreEqual(!string.IsNullOrEmpty(ev.herramientaId), conHerramienta, ev.id);
            }
            var sombra = catalogo.eventos.First(x => x.id == "sombra").opciones[1];
            Assert.AreEqual(EfectoOpcion.Acortar, sombra.efecto);
            Assert.AreEqual(4, sombra.combustible);
            Assert.AreEqual(1, sombra.duracion);
        }

        [Test]
        public void HayCincoObjetivosYCadaMisionSacaDosDistintos()
        {
            Assert.AreEqual(5, catalogo.objetivos.Count);
            var a = Objetivos.Elegir(catalogo, new System.Random(3));
            var b = Objetivos.Elegir(catalogo, new System.Random(3));
            Assert.AreEqual(2, a.Count);
            Assert.AreNotSame(a[0], a[1]);
            CollectionAssert.AreEqual(a, b, "misma semilla, mismos objetivos");
        }
    }
}
