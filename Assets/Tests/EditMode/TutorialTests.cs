using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 5, bloque 4: Misión 0 (TUT-01 del paquete): guiada, siempre en fracaso, sin penalización.</summary>
    public class TutorialTests
    {
        const string ClaveRecord = "mejor_puntaje_nivel_1";
        int recordAntes;
        int completadoAntes;
        MisionUI mision;
        TutorialUI tutorial;
        OperacionUI ui;

        [SetUp]
        public void Preparar()
        {
            recordAntes = PlayerPrefs.GetInt(ClaveRecord, -1);
            completadoAntes = PlayerPrefs.GetInt(TutorialUI.ClaveCompletado, -1);
            PlayerPrefs.DeleteKey(TutorialUI.ClaveCompletado);
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            mision = Object.FindFirstObjectByType<MisionUI>(FindObjectsInactive.Include);
            tutorial = Object.FindFirstObjectByType<TutorialUI>(FindObjectsInactive.Include);
            ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(tutorial, "Falta TutorialUI en la escena Mision");
            mision.IniciarTutorial();
        }

        [TearDown]
        public void Restaurar()
        {
            if (completadoAntes < 0) PlayerPrefs.DeleteKey(TutorialUI.ClaveCompletado);
            else PlayerPrefs.SetInt(TutorialUI.ClaveCompletado, completadoAntes);
            EstadoJuego.Reiniciar();
        }

        [Test]
        public void EmpiezaConLaNaveFijaYDirectoEnLaOperacion()
        {
            Assert.IsTrue(EstadoJuego.Tutorial);
            Assert.AreEqual(TutorialUI.Paso.Bienvenida, tutorial.Actual);
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, EstadoJuego.Configuracion.InstrumentoIds);
            Assert.AreEqual(TutorialUI.Anomalia, ui.Op.Anomalia);
            Assert.AreEqual(1, ui.Op.Mes);
        }

        [Test]
        public void SeJuegaPasoAPasoYSiempreTerminaEnFracaso()
        {
            tutorial.Siguiente();
            Assert.AreEqual(TutorialUI.Paso.Encender, tutorial.Actual);
            ui.ResolverMes();
            Assert.AreEqual(1, ui.Op.Mes, "antes de tiempo, «Resolver mes» no hace nada");

            ui.AlternarInstrumento("termica");
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.Region, tutorial.Actual);
            StringAssert.Contains("purple spot", ui.PasoActual.Texto);

            ui.FijarRegionEn(new Vector2(0.1f, 0.1f));
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.Region, tutorial.Actual, "lejos del punto no avanza");
            ui.FijarRegionEn(TutorialUI.Anomalia);
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.Resolver, tutorial.Actual);

            ui.ResolverMes();
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.Resumen, tutorial.Actual);
            Assert.AreEqual("TX-01", ui.Op.Memoria.Single().Evidencias.Single(), "la térmica encontró la anomalía");
            ui.ContinuarResumen();
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.Enviar, tutorial.Actual);

            ui.AlternarEnvio();
            tutorial.Comprobar();
            Assert.AreEqual(TutorialUI.Paso.ResolverEnvio, tutorial.Actual);
            ui.ResolverMes();
            Assert.AreEqual(TutorialUI.Paso.Final, tutorial.Actual);
            Assert.AreEqual(2, ui.Op.Mes, "la tormenta llega antes de enviar");
            CollectionAssert.AreEqual(new[] { "TX-00" }, ui.Op.Recibidas, "nada llegó a la Tierra");
            Assert.AreEqual(CategoriaResultado.Fracaso, ResultadoCientifico.Categoria(EstadoJuego.Resultado, ui.Op,
                AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset")));

            tutorial.Siguiente();   // «Design the next mission»
            Assert.AreEqual(TutorialUI.Paso.Inactivo, tutorial.Actual);
            Assert.IsTrue(TutorialUI.Completado);
            Assert.IsFalse(EstadoJuego.Tutorial);
            Assert.IsNull(EstadoJuego.Operacion);
            Assert.AreEqual(0, EstadoJuego.Configuracion.InstrumentoIds.Count, "la campaña empieza limpia");
            Assert.AreEqual(recordAntes, PlayerPrefs.GetInt(ClaveRecord, -1), "no toca los récords");
        }

        [Test]
        public void SePuedeSaltarEnCualquierMomento()
        {
            tutorial.Siguiente();
            ui.AlternarInstrumento("termica");
            tutorial.Terminar();
            Assert.IsTrue(TutorialUI.Completado);
            Assert.IsFalse(EstadoJuego.Tutorial);
            Assert.IsNull(ui.GuiaForzada);
            Assert.IsNull(ui.AntesDeResolver);
        }

        [Test]
        public void TerminarMisionEnElTutorialLoSaltaSinIrAlInforme()
        {
            tutorial.Siguiente();
            ui.Terminar();
            Assert.AreEqual(TutorialUI.Paso.Inactivo, tutorial.Actual);
            Assert.IsTrue(TutorialUI.Completado);
            Assert.IsNull(EstadoJuego.CategoriaFinal);
        }

        [Test]
        public void SePuedeRepetir()
        {
            tutorial.Terminar();
            mision.IniciarTutorial();
            Assert.AreEqual(TutorialUI.Paso.Bienvenida, tutorial.Actual);
            Assert.AreEqual(1, ui.Op.Mes);
        }
    }
}
