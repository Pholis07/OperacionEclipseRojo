using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 5, bloque 3: la guía propone el siguiente paso según el estado de la Operación.</summary>
    public class GuiaTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        bool consejosAntes;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            consejosAntes = Preferencias.GuiaActiva;
            Preferencias.GuiaActiva = true;
        }

        [TearDown]
        public void Restaurar() => Preferencias.GuiaActiva = consejosAntes;

        Operacion Caso2()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            return op;
        }

        PasoGuia Paso(Operacion op) => Guia.Siguiente(op, catalogo);

        [Test]
        public void AlEmpezarPideEncenderUnInstrumento()
        {
            var p = Paso(Caso2());
            Assert.AreEqual(ObjetivoGuia.Instrumento, p.Objetivo);
            Assert.AreEqual("termica", p.InstrumentoId);
            StringAssert.Contains("Turn on", p.Texto);
        }

        [Test]
        public void ConElInstrumentoEncendidoPideLaRegion()
        {
            var op = Caso2();
            op.Encender("termica");
            var p = Paso(op);
            Assert.AreEqual(ObjetivoGuia.Mapa, p.Objetivo);
            Assert.AreEqual("termica", p.InstrumentoId);
        }

        [Test]
        public void ConElPlanListoPideResolver()
        {
            var op = Caso2();
            op.Encender("termica");
            op.FijarRegion("termica", Lejos);
            var p = Paso(op);
            Assert.AreEqual(ObjetivoGuia.Resolver, p.Objetivo);
            StringAssert.Contains("Resolve month 1", p.Texto);
        }

        [Test]
        public void TrasUnaLecturaFriaSugiereMoverLaRegion()
        {
            var op = Caso2();
            op.Encender("termica");
            op.FijarRegion("termica", Lejos);
            op.ResolverMes(1);
            var p = Paso(op);
            Assert.AreEqual(ObjetivoGuia.Mapa, p.Objetivo);
            StringAssert.Contains("was cold", p.Texto);
            op.FijarRegion("termica", new Vector2(0.3f, 0.3f));
            Assert.AreEqual(ObjetivoGuia.Resolver, Paso(op).Objetivo, "con la región movida, ya se puede resolver");
        }

        [Test]
        public void ConEvidenciaEnMemoriaYContactoPideEnviar()
        {
            var op = Caso2();
            op.Encender("termica");
            op.FijarRegion("termica", Centro);
            op.ResolverMes(1);   // el mes 2 tiene contacto
            var p = Paso(op);
            Assert.AreEqual(ObjetivoGuia.Enviar, p.Objetivo);
            StringAssert.Contains("so your evidence counts", p.Texto);
            op.TransmitirPlaneado = true;
            Assert.AreNotEqual(ObjetivoGuia.Enviar, Paso(op).Objetivo);
        }

        [Test]
        public void AvisaCuandoLaMemoriaNoAlcanzara()
        {
            var op = Caso2();
            op.Encender("radar");
            op.FijarRegion("radar", Lejos);
            op.ResolverMes(1);
            op.FijarRegion("radar", new Vector2(0.9f, 0.1f));
            op.ResolverMes(2);
            op.FijarRegion("radar", new Vector2(0.1f, 0.9f));
            op.ResolverMes(3);   // 6 de 6 bloques, mes 4 con contacto: primero pide enviar
            Assert.AreEqual(ObjetivoGuia.Enviar, Paso(op).Objetivo);
            op.ResolverMes(4);   // sin enviar: el mes 5 no tiene contacto y el radar no cabe
            var p = Paso(op);
            Assert.IsTrue(p.Aviso);
            StringAssert.Contains("Memory will be full", p.Texto);
        }

        [Test]
        public void ConUnaDecisionPendientePideDecidir()
        {
            var op = Caso2();
            op.AplicarEvento(catalogo.eventos.First(e => e.id == "sombra"));
            Assert.AreEqual(ObjetivoGuia.Decision, Paso(op).Objetivo);
        }

        [Test]
        public void ConTodaLaEvidenciaRecibidaPideElInforme()
        {
            var op = Caso2();
            op.Encender("termica"); op.FijarRegion("termica", Centro);
            op.Encender("radar"); op.FijarRegion("radar", Centro);
            op.ResolverMes(1);
            op.TransmitirPlaneado = true;
            op.ResolverMes(2);
            Assert.IsTrue(op.Completa);
            Assert.AreEqual(ObjetivoGuia.Informe, Paso(op).Objetivo);
        }

        [Test]
        public void SiLaCalidadNoAlcanzaLoExplica()
        {
            var op = Caso2();
            op.AplicarEvento(catalogo.eventos.First(e => e.id == "tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            op.Encender("termica");
            op.FijarRegion("termica", Centro);
            op.ResolverMes(1);   // calidad 56: encontrada, pero no vale
            var p = Paso(op);
            Assert.AreEqual(ObjetivoGuia.Mapa, p.Objetivo);
            StringAssert.Contains("below 60", p.Texto);
        }

        [Test]
        public void EnLaPantallaLaGuiaSeVeYResaltaLoQueToca()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            var guia = (TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoGuia").objectReferenceValue;
            StringAssert.Contains("Turn on the Infrared thermal camera", guia.text);
            Assert.AreEqual(1, Object.FindObjectsByType<ResaltarGuia>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "un solo resaltado");

            ui.AlternarInstrumento("termica");
            ui.FijarRegionEn(new Vector2(0.9f, 0.9f));
            StringAssert.Contains("Resolve month 1", guia.text);
            var resaltado = Object.FindObjectsByType<ResaltarGuia>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, resaltado.Length);
            Assert.AreEqual("BotonResolver", resaltado[0].name);
        }

        [Test]
        public void ConLosConsejosApagadosNoGuiaNiResalta()
        {
            Preferencias.GuiaActiva = false;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            var guia = (TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoGuia").objectReferenceValue;
            Assert.AreEqual("", guia.text, "sin paso a paso");
            Assert.AreEqual(0, Object.FindObjectsByType<ResaltarGuia>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "sin resaltado");
            // Encenderlos de nuevo vuelve a guiar.
            Preferencias.GuiaActiva = true;
            ui.AlternarInstrumento("termica");
            Assert.IsNotEmpty(guia.text);
            Assert.AreEqual(1, Object.FindObjectsByType<ResaltarGuia>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        }
    }
}
