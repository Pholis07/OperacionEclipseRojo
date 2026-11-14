using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 3, paso 3 (PROPUESTA-GAMEPLAY.md): memoria, ventanas de contacto y envío a Tierra.</summary>
    public class MemoriaTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        /// <summary>Caso 2 con Carga (memoria de 3 en el nivel 2), sin eventos, anomalía en el centro y térmica encendida.</summary>
        Operacion Caso2()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Encender("termica");
            return op;
        }

        [Test]
        public void ReglasDeContacto()
        {
            Assert.AreEqual(3, catalogo.reglas.cicloContacto);
            Assert.AreEqual(150, catalogo.reglas.vatiosTransmisor);
        }

        [Test]
        public void LaVentanaDeContactoSeAbreUnaOrbitaDeCadaTres()
        {
            var op = Caso2();
            Assert.AreEqual(3, op.MemoriaCapacidad);
            Assert.AreEqual(2, op.OrbitasParaContacto);
            Assert.IsFalse(op.ContactoAbierto);
            op.Escanear("termica", Lejos);
            Assert.AreEqual(1, op.OrbitasParaContacto);
            op.Escanear("termica", Lejos);
            Assert.IsTrue(op.ContactoAbierto);
            op.Escanear("termica", Lejos);
            Assert.AreEqual(2, op.OrbitasParaContacto);
        }

        [Test]
        public void LoEncontradoQuedaEnMemoriaHastaEnviarlo()
        {
            var op = Caso2();
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("termica", Centro));
            CollectionAssert.AreEqual(new[] { "TX-01" }, op.EnMemoria);
            CollectionAssert.AreEqual(new[] { "TX-00" }, op.Recibidas);
            Assert.AreEqual(1, op.MemoriaUsada);
            Assert.IsFalse(op.Enviar(), "la ventana todavía no se abre");
        }

        [Test]
        public void ConLaMemoriaLlenaNoSePuedeEscanear()
        {
            var op = Caso2();
            for (int i = 0; i < 3; i++) Assert.IsNotNull(op.Escanear("termica", Lejos));
            Assert.IsTrue(op.MemoriaLlena);
            Assert.IsNull(op.Escanear("termica", Lejos));
            Assert.AreEqual(3, op.Escaneos);
            Assert.AreEqual(7, op.Orbitas, "el intento sin memoria no gasta órbita");
        }

        [Test]
        public void EnviarEnLaVentanaVaciaLaMemoriaSinGastarOrbita()
        {
            var op = Caso2();
            op.Escanear("termica", Centro);
            op.Encender("radar");
            op.Escanear("radar", Centro);
            Assert.IsTrue(op.ContactoAbierto);
            Assert.IsFalse(op.Completa, "falta enviar");

            Assert.IsTrue(op.Enviar());
            CollectionAssert.AreEqual(new[] { "TX-00", "TX-01", "TX-02" }, op.Recibidas);
            Assert.AreEqual(0, op.MemoriaUsada);
            Assert.AreEqual(8, op.Orbitas);
            Assert.IsTrue(op.Completa);
            Assert.AreEqual(2, op.EscaneosAlCompletar);
            Assert.AreEqual(8, op.OrbitasAlCompletar);
            Assert.IsFalse(op.Enviar(), "no queda nada que enviar");
        }

        [Test]
        public void SiElTransmisorNoCabeLaNaveSeApagaYNoSeEnvia()
        {
            var op = Caso2();
            op.Encender("radar");                                                   // 430 W
            op.Escanear("termica", Centro);
            op.AplicarEvento(catalogo.eventos.First(x => x.id == "micrometeorito")); // límite 500
            op.Escanear("termica", Lejos);
            Assert.IsTrue(op.ContactoAbierto);

            Assert.IsFalse(op.Enviar(), "430 + 150 > 500");
            Assert.AreEqual(1, op.Apagones);
            Assert.AreEqual(6, op.Orbitas);
            CollectionAssert.AreEqual(new[] { "TX-01" }, op.EnMemoria, "lo de la memoria no se pierde");
        }

        [Test]
        public void ApagandoInstrumentosSiCabeElTransmisor()
        {
            var op = Caso2();
            op.Encender("radar");
            op.Escanear("termica", Centro);
            op.AplicarEvento(catalogo.eventos.First(x => x.id == "micrometeorito"));
            op.Escanear("termica", Lejos);
            op.Apagar("radar");                                                     // 250 + 150 = 400
            Assert.IsTrue(op.Enviar());
            Assert.AreEqual(0, op.Apagones);
        }

        [Test]
        public void LoQueQuedaEnMemoriaAlTerminarLlegaConBajaCalidad()
        {
            var op = Caso2();
            op.Escanear("termica", Centro);
            op.Terminar();
            CollectionAssert.AreEqual(new[] { "TX-01", "TX-02" }, op.BajaCalidad);
            CollectionAssert.IsEmpty(op.EnMemoria);
        }

        [Test]
        public void AbrirContactoAbreLaVentanaSoloEstaOrbita()
        {
            var op = Caso2();
            op.Escanear("termica", Centro);
            op.AbrirContacto();
            Assert.IsTrue(op.ContactoAbierto);
            Assert.IsTrue(op.Enviar());
            op.AbrirContacto();
            op.Escanear("termica", Lejos);
            Assert.IsTrue(op.ContactoAbierto, "la órbita 2 es la ventana normal");
            op.Escanear("termica", Lejos);
            Assert.IsFalse(op.ContactoAbierto);
        }
    }
}
