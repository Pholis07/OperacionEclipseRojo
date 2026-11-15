using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Fase 6: los casos 1 a 7 y B1 a B8 dan en pantalla los valores y colores de la tabla,
    /// y «Lanzar» está bloqueado en Gris y Rojo.
    /// </summary>
    public class ConfiguracionTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        static Configuracion Config(string lanzador, string orbita, string instrumentos)
        {
            var ids = string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+');
            return new Configuracion(orbita, lanzador, ids);
        }

        // Caso, lanzador, órbita, instrumentos, barras (masa | potencia | costo | espacios), semáforo
        [TestCase("1", "explorador", "polar", "termica+radar", "430 / 500 kg|220 / 700 W|850 / 1,250 cr|2 / 2", EstadoSemaforo.Verde)]
        [TestCase("2", "explorador", "media", "termica+radar", "430 / 500 kg|220 / 700 W|820 / 1,250 cr|2 / 2", EstadoSemaforo.Verde)]
        [TestCase("3", "explorador", "eliptica", "termica+radar", "430 / 500 kg|220 / 700 W|870 / 1,250 cr|2 / 2", EstadoSemaforo.Amarillo)]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", "455 / 750 kg|275 / 700 W|1,130 / 1,250 cr|3 / 4", EstadoSemaforo.Verde)]
        [TestCase("5", "explorador", "media", "termica+atmosferico", "410 / 500 kg|225 / 700 W|650 / 1,250 cr|2 / 2", EstadoSemaforo.Amarillo)]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", "570 / 750 kg|455 / 700 W|870 / 1,250 cr|2 / 4", EstadoSemaforo.Amarillo)]
        [TestCase("7", "titan", "media", "termica+radar+espectrometro+atmosferico", "555 / 750 kg|500 / 700 W|1,300 / 1,250 cr|4 / 4", EstadoSemaforo.Rojo)]
        [TestCase("B1", "titan", "media", "", "— / 750 kg|— / 700 W|— / 1,250 cr|— / 4", EstadoSemaforo.Gris)]
        [TestCase("B2", "explorador", "media", "termica+alta_res+atmosferico", "605 / 500 kg|525 / 700 W|850 / 1,250 cr|3 / 2", EstadoSemaforo.Rojo)]
        [TestCase("B4", "titan", "media", "termica+radar+alta_res+atmosferico", "650 / 750 kg|575 / 700 W|1,280 / 1,250 cr|4 / 4", EstadoSemaforo.Rojo)]
        [TestCase("B4b", "titan", "media", "termica+alta_res+atmosferico+espectrometro", "705 / 750 kg|750 / 700 W|1,240 / 1,250 cr|4 / 4", EstadoSemaforo.Rojo)]
        [TestCase("B5", "titan", "eliptica", "termica+radar+espectrometro", "530 / 750 kg|445 / 700 W|1,260 / 1,250 cr|3 / 4", EstadoSemaforo.Rojo)]
        [TestCase("B6", "explorador", "media", "radar", "395 / 500 kg|150 / 700 W|640 / 1,250 cr|1 / 2", EstadoSemaforo.Amarillo)]
        [TestCase("B7", "titan", null, "termica+radar", "— / 750 kg|— / 700 W|— / 1,250 cr|— / 4", EstadoSemaforo.Gris)]
        [TestCase("B8", null, "media", "termica+radar", "— / — kg|— / 700 W|— / 1,250 cr|— / —", EstadoSemaforo.Gris)]
        public void LosCasosDanLosValoresYColoresDeLaTabla(string caso, string lanzador, string orbita, string instrumentos,
            string barras, EstadoSemaforo semaforo)
        {
            var c = Config(lanzador, orbita, instrumentos);
            var e = Evaluador.Evaluar(c, catalogo);
            var obtenidas = PresentacionMision.Barras(e, c, catalogo);

            CollectionAssert.AreEqual(barras.Split('|'), obtenidas.Select(b => b.Texto).ToArray(), "barras del caso " + caso);
            Assert.AreEqual(semaforo, e.Estado, "semáforo del caso " + caso);
            Assert.AreEqual(PresentacionMision.ColorSemaforo(semaforo), PresentacionMision.ColorSemaforo(e.Estado));
            // Solo las barras de un límite superado se pintan como excedidas.
            CollectionAssert.AreEquivalent(
                e.Calculo.LimitesSuperados.Select(l => (int)l).ToArray(),
                Enumerable.Range(0, 4).Where(i => obtenidas[i].Excedida).ToArray(), "barras excedidas del caso " + caso);
        }

        [Test]
        public void LosCuatroEstadosTienenColoresDistintos()
        {
            var colores = new[] { EstadoSemaforo.Gris, EstadoSemaforo.Rojo, EstadoSemaforo.Amarillo, EstadoSemaforo.Verde }
                .Select(PresentacionMision.ColorSemaforo).ToArray();
            CollectionAssert.AllItemsAreUnique(colores);
        }

        [Test]
        public void EnLaEscenaLasElecciónesActualizanBarrasSemaforoYNave()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "Falta ConfiguracionUI en la escena Mision");
            ui.Inicializar();

            // Caso 7 armado pulsando opciones, con el lanzador elegido al final.
            ui.ElegirOrbita("media");
            foreach (var id in new[] { "termica", "radar", "espectrometro", "atmosferico" }) ui.AlternarInstrumento(id);
            ui.ElegirLanzador("titan");
            Assert.AreEqual(EstadoSemaforo.Rojo, ui.Ultima.Estado);
            Assert.AreEqual("Mass: 555 / 750 kg", ui.TextosBarra[0].text);
            Assert.AreEqual(PresentacionMision.ColorSemaforo(EstadoSemaforo.Rojo), ui.LuzEstado.color);

            // Quitar dos instrumentos y pasar a Explorador: no se borran los que quedan (caso 2).
            ui.AlternarInstrumento("espectrometro");
            ui.AlternarInstrumento("atmosferico");
            ui.ElegirLanzador("explorador");
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, EstadoJuego.Configuracion.InstrumentoIds);
            Assert.AreEqual(EstadoSemaforo.Verde, ui.Ultima.Estado);
            Assert.AreEqual("Cost: 820 / 1,250 cr", ui.TextosBarra[2].text);

            // La nave muestra solo los módulos elegidos.
            // Desde la vista orbital de Diego hay dos NaveUI en la escena: se usa la que maneja la Configuración.
            var nave = (NaveUI)new SerializedObject(ui).FindProperty("nave").objectReferenceValue;
            Assert.IsTrue(nave.transform.Find("Modulo_termica").gameObject.activeSelf);
            Assert.IsFalse(nave.transform.Find("Modulo_espectrometro").gameObject.activeSelf);

            // Cambiar de pestaña no pierde la selección.
            ui.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            ui.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
            Assert.AreEqual("media", EstadoJuego.Configuracion.OrbitaId);
            Assert.AreEqual(2, EstadoJuego.Configuracion.InstrumentoIds.Count);
        }

        [TestCase("explorador", "media", "termica+radar", true)]
        [TestCase("explorador", "eliptica", "termica+radar", true)]
        [TestCase("titan", "media", "termica+radar+espectrometro+atmosferico", false)]
        [TestCase("titan", "media", "", false)]
        public void LanzarSoloSeHabilitaEnAmarilloOVerde(string lanzador, string orbita, string instrumentos, bool habilitado)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Configuracion = Config(lanzador, orbita, instrumentos);
            var revision = Object.FindFirstObjectByType<RevisionUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(revision, "Falta RevisionUI en la escena Mision");
            revision.Inicializar();

            revision.Mostrar(Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo));
            Assert.AreEqual(habilitado, revision.BotonLanzar.interactable);
        }

        // AUD-01 (paquete v1.0): reconstruir la vista o cambiar de pestaña no pide ningún sonido; instalar pide uno.
        [Test]
        public void ReconstruirLaVistaNoSuenaEInstalarSuenaUnaVez()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();

            int antes = ReproductorUI.Peticiones;
            ui.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
            ui.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            ui.ElegirOrbita("media");
            ui.Recalcular();
            Assert.AreEqual(antes, ReproductorUI.Peticiones, "navegar y redibujar no suena");

            ui.AlternarInstrumento("termica");
            Assert.AreEqual(antes + 1, ReproductorUI.Peticiones, "instalar: un sonido");
            ui.AlternarInstrumento("termica");
            Assert.AreEqual(antes + 1, ReproductorUI.Peticiones, "quitar no añade otro");
        }
    }
}
