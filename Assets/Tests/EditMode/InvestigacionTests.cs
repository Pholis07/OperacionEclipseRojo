using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Fase 7: Investigación, informe final, PM2, F1 y «Modificar mi misión».</summary>
    public class InvestigacionTests
    {
        // Texto de fracaso de la decisión 6 (F1), con el punto final de la frase.
        const string TextoFracasoF1 =
            "La nave ejecutó la misión, pero ninguno de sus instrumentos observó la anomalía principal. " +
            "Los datos complementarios no bastan para explicarla. Revisa el equipo científico antes de volver a intentarlo.";

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            EstadoJuego.Reiniciar();
        }

        static Configuracion Config(string lanzador, string orbita, string instrumentos) =>
            new Configuracion(orbita, lanzador, instrumentos.Split('+'));

        /// <summary>Juega la misión sin interfaz: observa la anomalía con todo en el mes 1 y envía en el mes 2.</summary>
        Operacion Jugada(Configuracion c)
        {
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 7, new Carga(e, c, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            foreach (var id in op.Instrumentos.ToList())
            {
                Assert.IsTrue(op.Encender(id), id);
                op.FijarRegion(id, op.Anomalia);
            }
            Assert.IsTrue(op.ResolverMes(1).Valido);
            op.TransmitirPlaneado = true;
            Assert.IsTrue(op.ResolverMes(2).Valido);
            op.Terminar();
            return op;
        }

        // Versión 4 (meses): las evidencias llegan cuando el instrumento observa la anomalía y los datos se envían.
        // Se juega con la interfaz real: elegir instrumento, encender, fijar región, «Resolver mes», resumen y envío.
        [TestCase("1", "explorador", "polar", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("2", "explorador", "media", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("3", "explorador", "eliptica", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", "TX-00,TX-01,TX-02,TX-03")]
        [TestCase("5", "explorador", "media", "termica+atmosferico", "TX-00,TX-01,TX-03")]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", "TX-00,TX-04")]
        public void LaOperacionPorMesesEntregaLasEvidenciasDeLaTabla(string caso, string lanzador, string orbita,
            string instrumentos, string esperadas)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "Falta OperacionUI en la escena Mision");

            EstadoJuego.Configuracion = Config(lanzador, orbita, instrumentos);
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            EstadoJuego.Resultado = e;
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            var op = ui.Op;
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            CollectionAssert.AreEqual(new[] { "TX-00" }, op.Recibidas, "TX-00 llega al empezar");

            foreach (var id in op.Instrumentos.ToList())
            {
                ui.AlternarInstrumento(id);          // lo elige y lo enciende
                ui.FijarRegionEn(op.Anomalia);       // clic en el mapa: su región
            }
            Assert.AreEqual(1, op.Mes, "preparar el plan no avanza el mes");
            Assert.AreEqual(0, op.Observaciones, "ni genera datos");

            ui.ResolverMes();
            Assert.IsTrue(ui.ResumenVisible);
            ui.ResolverMes();
            Assert.AreEqual(2, op.Mes, "con el resumen abierto no se resuelve otra vez");
            ui.ContinuarResumen();
            ui.AlternarEnvio();
            ui.ResolverMes();
            ui.ContinuarResumen();

            CollectionAssert.AreEquivalent(esperadas.Split(','), op.Recibidas, "evidencias del caso " + caso);
            Assert.IsTrue(op.Completa);
            Assert.AreEqual(3, op.Mes);
        }

        [Test]
        public void UnaMisionQueNoSePuedeLanzarNoEmpiezaLaOperacion()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            var e = Evaluador.Evaluar(Config("titan", "media", "termica+radar+espectrometro+atmosferico"), catalogo);

            Assert.IsFalse(e.SePuedeLanzar);
            ui.Comenzar(e, null);
            Assert.IsNull(ui.Op);
        }

        [Test]
        public void EnLaPantallaUnEventoPideDecisionDespuesDelResumen()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            EstadoJuego.Configuracion = Config("explorador", "media", "termica+radar");
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            ui.Op.ProgramarEventos(new[] { catalogo.eventos.First(x => x.id == "sombra") }, new[] { 1 });

            ui.Op.Encender("termica");
            ui.ResolverMes();
            Assert.IsTrue(ui.ResumenVisible);
            Assert.IsTrue(ui.Op.EsperandoDecision);
            ui.AlternarInstrumento("termica");
            CollectionAssert.Contains(ui.Op.Encendidos.ToList(), "termica", "con el resumen abierto no se cambia el plan");
            ui.ContinuarResumen();
            ui.AlternarInstrumento("termica");
            CollectionAssert.Contains(ui.Op.Encendidos.ToList(), "termica", "ni con la decisión pendiente: no se esquiva el evento");
            ui.ResolverMes();
            Assert.AreEqual(2, ui.Op.Mes, "con la decisión pendiente no se resuelve");
            StringAssert.Contains("No se puede resolver", ui.UltimoMensaje);

            ui.Decidir(ui.Op.Opciones.Count() - 1);   // aguantar
            Assert.IsFalse(ui.Op.EsperandoDecision);
            Assert.AreEqual(550, ui.Op.LimiteW);
            ui.ResolverMes();
            Assert.AreEqual(3, ui.Op.Mes);
        }

        [Test]
        public void ElInformeDeUnaMisionSinPistasPrincipalesEsFracasoConSuRevelacion()
        {
            var c = Config("titan", "polar", "alta_res+atmosferico");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo, Jugada(c));

            Assert.AreEqual("Investigación fallida", informe.Titulo);
            Assert.AreEqual(TextoFracasoF1, informe.Texto);
            Assert.AreEqual(1, informe.Revelaciones.Count);
            StringAssert.StartsWith("Cámara de alta resolución", informe.Revelaciones[0]);
            CollectionAssert.IsEmpty(informe.Evidencias);
            CollectionAssert.AreEqual(
                new[] { "TX-A1", "TX-A2", "TX-A5" }.Select(id => catalogo.Transmision(id).texto).ToArray(), informe.Faltantes);
        }

        [Test]
        public void ElInformeDeUnaMisionGanadaDaVictoriaYLosRecursos()
        {
            var c = Config("explorador", "media", "termica+radar");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo, Jugada(c));

            Assert.AreEqual("Misión cumplida", informe.Titulo);
            StringAssert.StartsWith("Usaste 820 de 1,250 créditos, 620 de 650 kg, 430 de 700 W y 2 de 2 espacios.", informe.Recursos);
            CollectionAssert.IsEmpty(informe.Faltantes);
            CollectionAssert.AreEqual(new[]
            {
                "Cámara térmica infrarroja: " + catalogo.Instrumento("termica").pistaAsociada + ".",
                "Radar de subsuelo: " + catalogo.Instrumento("radar").pistaAsociada + "."
            }, informe.Evidencias);
        }

        [Test]
        public void SinOperarLaMisionElInformeEsFracaso()
        {
            var c = Config("explorador", "media", "termica+radar");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo);
            Assert.AreEqual("Investigación fallida", informe.Titulo);
            CollectionAssert.IsEmpty(informe.Evidencias);
            Assert.AreEqual(2, informe.Faltantes.Count, "ninguna evidencia llegó a la Tierra");
        }

        [Test]
        public void LaEscenaResultadoNoTieneNuevaMision()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Resultado.unity");
            var ui = Object.FindFirstObjectByType<ResultadoUI>();
            Assert.IsNotNull(ui, "Falta ResultadoUI en la escena Resultado");

            var c = Config("titan", "polar", "alta_res+atmosferico");
            ui.Mostrar(Evaluador.Evaluar(c, catalogo), c);
            var textos = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(t => t.text).ToList();
            Assert.IsTrue(textos.Contains("Investigación fallida"));
            Assert.IsTrue(textos.Contains("Modificar mi misión"));
            Assert.IsFalse(textos.Any(t => t.Contains("Nueva misión")));
        }

        [Test]
        public void ModificarMiMisionConservaLasEleccionesYAbreConfiguracion()
        {
            var c = Config("explorador", "eliptica", "termica+radar");
            EstadoJuego.Configuracion = c;
            EstadoJuego.Resultado = Evaluador.Evaluar(c, catalogo);

            EditorSceneManager.OpenScene("Assets/Escenas/Resultado.unity");
            Object.FindFirstObjectByType<ResultadoUI>().ModificarMision();

            Assert.AreSame(c, EstadoJuego.Configuracion);
            Assert.AreEqual("eliptica", EstadoJuego.Configuracion.OrbitaId);
            Assert.AreEqual("explorador", EstadoJuego.Configuracion.LanzadorId);
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, EstadoJuego.Configuracion.InstrumentoIds);

            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var mision = Object.FindFirstObjectByType<MisionUI>();
            mision.AbrirVistaInicial();
            Assert.IsTrue(mision.VistaActivaEsConfiguracion, "Debería abrir en Configuración, no en el Briefing");
            Assert.IsFalse(EstadoJuego.EntrarEnConfiguracion);
        }

        [Test]
        public void LasTresEscenasEstanEnElBuildEnOrden()
        {
            CollectionAssert.AreEqual(
                new[] { "Assets/Escenas/Menu.unity", "Assets/Escenas/Mision.unity", "Assets/Escenas/Resultado.unity" },
                EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray());
        }
    }
}
