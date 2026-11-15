using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceMission.Tests
{
    /// <summary>
    /// La parte automática de PM1 y la integridad de las escenas. Lo visual (que se lea bien en pantalla)
    /// se comprueba a mano abriendo el juego.
    /// </summary>
    public class InterfazTests
    {
        static readonly string[] NombresDeInstrumentos =
            { "camera", "infrared thermal", "radar", "spectrometer", "atmospheric sensor", "high-resolution" };

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        [Test]
        public void PM1_LosRequisitosSonTresYNoNombranInstrumentos()
        {
            Assert.AreEqual(3, TextosBriefing.Requisitos.Length);
            var texto = TextosBriefing.TextoRequisitos().ToLowerInvariant();
            foreach (var nombre in NombresDeInstrumentos)
                StringAssert.DoesNotContain(nombre, texto);
            StringAssert.Contains("evidence of the thermal anomaly", texto);
            StringAssert.Contains("evidence of what lies beneath the surface", texto);
            StringAssert.Contains("storm environment", texto);
        }

        [Test]
        public void PM1_LosLimitesSalenDeLosDatos()
        {
            var texto = TextosBriefing.TextoLimites(catalogo);
            StringAssert.Contains("1,250 credits", texto);
            StringAssert.Contains("700 W", texto);
            StringAssert.Contains("Explorer 500 kg", texto);
            StringAssert.Contains("Titan 750 kg", texto);
            StringAssert.DoesNotContain("1,100", texto);
            StringAssert.Contains("2 with Explorer", texto);
            StringAssert.Contains("4 with Titan", texto);
        }

        [Test]
        public void ElBriefingSeLlenaConElObjetivoDeLosDatos()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var ui = Object.FindFirstObjectByType<MisionUI>();
            Assert.IsNotNull(ui, "Falta MisionUI en la escena Mision");
            Assert.AreEqual(catalogo, ui.Catalogo);

            ui.LlenarBriefing();
            var textos = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsTrue(textos.Any(t => t.text == catalogo.mision.objetivo), "El objetivo no aparece en el Briefing");
            Assert.IsTrue(textos.Any(t => t.text.Contains("Titan 750 kg")), "Los límites no aparecen en el Briefing");
        }

        [TestCase("Assets/Escenas/Menu.unity")]
        [TestCase("Assets/Escenas/Mision.unity")]
        public void LaEscenaTieneCanvas1920x1080YUnSoloEventSystem(string ruta)
        {
            EditorSceneManager.OpenScene(ruta);
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            var escalador = Object.FindFirstObjectByType<UnityEngine.UI.CanvasScaler>();
            Assert.IsNotNull(escalador);
            Assert.AreEqual(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize, escalador.uiScaleMode);
            Assert.AreEqual(new Vector2(1920, 1080), escalador.referenceResolution);
            Assert.IsNotNull(Object.FindFirstObjectByType<UnityEngine.UI.GraphicRaycaster>());
        }

        [Test]
        public void ElMenuTieneSusBotonesConectados()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity");
            var ui = Object.FindFirstObjectByType<MenuUI>();
            Assert.IsNotNull(ui);
            var so = new SerializedObject(ui);
            foreach (var campo in new[] { "botonIniciar", "botonComoJugar", "botonCerrarComoJugar", "panelComoJugar" })
                Assert.IsNotNull(so.FindProperty(campo).objectReferenceValue, "Sin asignar: " + campo);
        }
    }
}
