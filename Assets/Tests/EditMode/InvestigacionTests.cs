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

        [TestCase("1", "explorador", "polar", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("2", "explorador", "media", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("3", "explorador", "eliptica", "termica+radar", "TX-00,TX-01,TX-02")]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", "TX-00,TX-01,TX-02,TX-03")]
        [TestCase("5", "explorador", "media", "termica+atmosferico", "TX-00,TX-01,TX-03")]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", "TX-00,TX-04")]
        public void LaInvestigacionMuestraLasTransmisionesEnOrdenUnaPorUna(string caso, string lanzador, string orbita,
            string instrumentos, string esperadas)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<InvestigacionUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "Falta InvestigacionUI en la escena Mision");

            ui.Comenzar(Evaluador.Evaluar(Config(lanzador, orbita, instrumentos), catalogo));
            var vistos = new List<string> { ui.TextoActual };
            var botones = new List<string> { ui.EtiquetaBoton.text };
            while (ui.Avanzar())
            {
                vistos.Add(ui.TextoActual);
                botones.Add(ui.EtiquetaBoton.text);
            }

            var textos = esperadas.Split(',').Select(id => catalogo.Transmision(id).texto).ToArray();
            CollectionAssert.AreEqual(textos, vistos, "transmisiones del caso " + caso);
            // «Continuar investigación» entre cada una; la última lleva al informe.
            CollectionAssert.AreEqual(
                Enumerable.Repeat("Continuar investigación", textos.Length - 1).Concat(new[] { "Ver informe" }).ToArray(),
                botones, "botones del caso " + caso);
        }

        [Test]
        public void Caso7NoLlegaAInvestigacion()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<InvestigacionUI>(FindObjectsInactive.Include);
            var e = Evaluador.Evaluar(Config("titan", "media", "termica+radar+espectrometro+atmosferico"), catalogo);

            Assert.IsFalse(e.SePuedeLanzar);
            ui.Comenzar(e);
            Assert.IsEmpty(ui.TextoActual);
            Assert.IsFalse(ui.Avanzar());
        }

        [Test]
        public void F1_Caso6MuestraElTextoDeFracasoYLaRevelacionDeAltaResolucion()
        {
            var c = Config("titan", "polar", "alta_res+atmosferico");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo);

            Assert.AreEqual("Investigación fallida", informe.Titulo);
            Assert.AreEqual(TextoFracasoF1, informe.Texto);
            Assert.AreEqual(1, informe.Revelaciones.Count);
            StringAssert.StartsWith("Cámara de alta resolución", informe.Revelaciones[0]);
            CollectionAssert.IsEmpty(informe.Evidencias);
            CollectionAssert.AreEqual(
                new[] { "TX-A1", "TX-A2", "TX-A5" }.Select(id => catalogo.Transmision(id).texto).ToArray(), informe.Faltantes);
        }

        [Test]
        public void PM2_LaConfiguracionMasBarataDaVictoriaMuestraRecursosYNoDaPuntos()
        {
            var c = Config("explorador", "media", "termica+radar");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo);

            Assert.AreEqual("Misión cumplida", informe.Titulo);
            Assert.AreEqual("Usaste 820 de 1,250 créditos, 620 de 650 kg, 430 de 700 W y 2 de 2 espacios.", informe.Recursos);
            CollectionAssert.IsEmpty(informe.Faltantes);

            var todo = string.Join(" ", new[] { informe.Titulo, informe.Texto, informe.Recursos }
                .Concat(informe.Evidencias).Concat(informe.Revelaciones)).ToLowerInvariant();
            foreach (var palabra in new[] { "punto", "puntaje", "medalla", "calificación", "estrellas", "ranking" })
                StringAssert.DoesNotContain(palabra, todo);
        }

        [Test]
        public void LasEvidenciasDicenQueInstrumentoLasDio()
        {
            var c = Config("explorador", "media", "termica+radar");
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo);
            CollectionAssert.AreEqual(new[]
            {
                "Cámara térmica infrarroja: " + catalogo.Instrumento("termica").pistaAsociada + ".",
                "Radar de subsuelo: " + catalogo.Instrumento("radar").pistaAsociada + "."
            }, informe.Evidencias);
        }

        [Test]
        public void LaEscenaResultadoNoTieneNuevaMisionNiPuntos()
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
            Assert.IsFalse(textos.Any(t => t.ToLowerInvariant().Contains("puntos")));
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
