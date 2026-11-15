using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 3, paso 5 (PROPUESTA-GAMEPLAY.md): objetivos secundarios y puntos.</summary>
    public class ObjetivosTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

        CatalogoDatos catalogo;
        Configuracion config;
        Evaluacion evaluacion;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            config = new Configuracion("media", "explorador", new[] { "termica", "radar" });   // caso 2: 820 cr, 3 de combustible
            evaluacion = Evaluador.Evaluar(config, catalogo);
        }

        ObjetivoDatos O(string id) => catalogo.objetivos.First(o => o.id == id);

        /// <summary>Encuentra todo en 2 escaneos, lo envía en la ventana de la órbita 2 y termina.</summary>
        Operacion MisionCompleta(params string[] objetivos)
        {
            var op = new Operacion(evaluacion, config, catalogo, null, 1, new Carga(evaluacion, config, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Objetivos.Clear();
            op.Objetivos.AddRange(objetivos.Select(O));
            op.Encender("termica");
            op.Encender("radar");
            op.Escanear("termica", Centro);
            op.Escanear("radar", Centro);
            Assert.IsTrue(op.Enviar());
            op.Terminar();
            return op;
        }

        [Test]
        public void ConCargaCadaMisionTraeDosObjetivos()
        {
            for (int semilla = 0; semilla < 10; semilla++)
            {
                var op = new Operacion(evaluacion, config, catalogo, null, semilla, new Carga(evaluacion, config, catalogo));
                Assert.AreEqual(2, op.Objetivos.Count);
                Assert.AreEqual(2, op.Objetivos.Distinct().Count());
            }
        }

        [Test]
        public void NoSaleElObjetivoDeCombustibleSiNoSePuedeCumplir()
        {
            for (int semilla = 0; semilla < 30; semilla++)
            {
                var op = new Operacion(evaluacion, config, catalogo, null, semilla, new Carga(evaluacion, config, catalogo));   // 3 de combustible
                Assert.IsFalse(op.Objetivos.Any(o => o.tipo == TipoObjetivo.CombustibleMinimo), "semilla " + semilla);
            }
        }

        [Test]
        public void SinCargaNoHayObjetivosYLosPuntosNoCambian()
        {
            var op = new Operacion(evaluacion, config, catalogo, null, 1);
            CollectionAssert.IsEmpty(op.Objetivos);
            Assert.AreEqual("", Objetivos.Resumen(op));
        }

        [TestCase("rapido", 150)]
        [TestCase("sin_apagones", 100)]
        [TestCase("combustible", 100)]
        [TestCase("enviar_pronto", 100)]
        [TestCase("sin_herramientas", 150)]
        public void LosObjetivosTienenLosPuntosAprobados(string id, int puntos)
        {
            Assert.AreEqual(puntos, O(id).puntos);
        }

        [Test]
        public void SoloLosObjetivosCumplidosSumanPuntos()
        {
            var op = MisionCompleta("rapido", "combustible");
            var lineas = Puntuacion.Desglose(evaluacion, op, catalogo, null);
            Assert.IsTrue(lineas.Any(l => l.Concepto == "Objetivo: " + O("rapido").texto && l.Puntos == 150));
            Assert.IsFalse(lineas.Any(l => l.Concepto.Contains(O("combustible").texto)), "3 de combustible no llegan a 5");
            // 1,000 + 430 / 2 + 8 órbitas × 25 + 3 de combustible × 10 + 150
            Assert.AreEqual(1595, Puntuacion.Total(lineas));
        }

        [Test]
        public void SiQuedaAlgoSinEnviarNoSeCumplenLosDeEncontrarYEnviar()
        {
            var op = new Operacion(evaluacion, config, catalogo, null, 1, new Carga(evaluacion, config, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Encender("termica");
            op.Escanear("termica", Centro);
            op.Terminar();
            Assert.IsFalse(Objetivos.Cumplido(O("rapido"), op));
            Assert.IsFalse(Objetivos.Cumplido(O("enviar_pronto"), op));
        }

        [Test]
        public void ElResumenDiceCualesSeCumplieron()
        {
            var op = MisionCompleta("sin_apagones", "combustible");
            Assert.AreEqual("Objetivos extra: Ningún apagón (cumplido), Terminar con 5 de combustible o más (no cumplido).",
                Objetivos.Resumen(op));
        }

        [Test]
        public void ElInformeMuestraLaCargaElCombustibleYLosObjetivos()
        {
            var op = MisionCompleta("sin_apagones");
            var informe = InformeMision.Construir(evaluacion, config, catalogo, op, null);
            StringAssert.Contains("Te quedaron 3 de combustible.", informe.Recursos);
            StringAssert.Contains("Objetivos extra: Ningún apagón (cumplido).", informe.Recursos);
        }
    }
}
