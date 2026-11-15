using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 6: guía de cada pantalla en el modo normal y explicación «?».</summary>
    public class GuiaPantallasTests
    {
        CatalogoDatos catalogo;
        bool guiaAntes;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            guiaAntes = Preferencias.GuiaActiva;
            Preferencias.GuiaActiva = true;
        }

        [TearDown]
        public void Restaurar() => Preferencias.GuiaActiva = guiaAntes;

        static Configuracion Config(string lanzador, string orbita, string instrumentos, string satelite = null) =>
            new Configuracion(orbita, lanzador, string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+'))
                { SateliteId = satelite };

        ConsejoPantalla EnConfiguracion(Configuracion c, ConfiguracionUI.Pestana pestana) =>
            GuiaPantallas.Configuracion(c, pestana, Evaluador.Evaluar(c, catalogo));

        [Test]
        public void LaConfiguracionGuiaPasoAPasoEnElOrdenNuevo()
        {
            var c = Config(null, null, "");
            StringAssert.StartsWith("Choose a rocket", EnConfiguracion(c, ConfiguracionUI.Pestana.Lanzador).Texto);
            Assert.AreEqual("PestanaLanzador", EnConfiguracion(c, ConfiguracionUI.Pestana.Orbita).Destino, "sin cohete, se señala su pestaña");

            c.LanzadorId = "explorador";
            Assert.AreEqual("BotonSiguiente", EnConfiguracion(c, ConfiguracionUI.Pestana.Lanzador).Destino);
            StringAssert.StartsWith("Choose a satellite", EnConfiguracion(c, ConfiguracionUI.Pestana.Satelite).Texto);
            c.SateliteId = "mro";
            Assert.AreEqual("BotonSiguiente", EnConfiguracion(c, ConfiguracionUI.Pestana.Satelite).Destino);
            StringAssert.StartsWith("Install the instruments", EnConfiguracion(c, ConfiguracionUI.Pestana.Instrumentos).Texto);

            c.AgregarInstrumento("termica");
            c.AgregarInstrumento("radar");
            Assert.AreEqual("BotonSiguiente", EnConfiguracion(c, ConfiguracionUI.Pestana.Instrumentos).Destino);
            StringAssert.StartsWith("Choose an orbit you can reach", EnConfiguracion(c, ConfiguracionUI.Pestana.Orbita).Texto);

            c.OrbitaId = "media";
            var listo = EnConfiguracion(c, ConfiguracionUI.Pestana.Orbita);
            StringAssert.StartsWith("Green", listo.Texto);
            Assert.AreEqual("BotonRevisar", listo.Destino);
        }

        [Test]
        public void LaGuiaAvisaDeLoQueEstaEnRojo()
        {
            // Demasiada masa para el Explorador: lo dice antes de llegar a la órbita.
            var pesada = EnConfiguracion(Config("explorador", null, "termica+alta_res+atmosferico"), ConfiguracionUI.Pestana.Instrumentos);
            StringAssert.StartsWith("A bar is in red", pesada.Texto);
            Assert.AreEqual("PanelDerecho", pesada.Destino);
            // Órbita elegida que ya no se alcanza.
            StringAssert.StartsWith("Red: your spacecraft cannot reach this orbit",
                EnConfiguracion(Config("explorador", "media", "radar+espectrometro"), ConfiguracionUI.Pestana.Orbita).Texto);
            StringAssert.StartsWith("Yellow", EnConfiguracion(Config("explorador", "eliptica", "termica+radar"), ConfiguracionUI.Pestana.Orbita).Texto);
        }

        [Test]
        public void RevisionYCargaSenalanElBotonQueToca()
        {
            Assert.AreEqual("BotonLanzar", GuiaPantallas.Revision(Evaluador.Evaluar(Config("explorador", "media", "termica+radar"), catalogo)).Destino);
            Assert.AreEqual("BotonCorregir", GuiaPantallas.Revision(Evaluador.Evaluar(Config("titan", "media", ""), catalogo)).Destino);
            Assert.AreEqual("BotonLanzarCarga", GuiaPantallas.Carga(null).Destino);
            Assert.AreEqual("BotonContinuar", GuiaPantallas.Briefing().Destino);
        }

        [Test]
        public void CadaPantallaTieneSuExplicacionEnLosDosIdiomas()
        {
            foreach (PantallaMision p in System.Enum.GetValues(typeof(PantallaMision)))
            {
                var en = GuiaPantallas.Ayuda(p);
                Assert.Greater(en.Length, 80, p.ToString());
                Assert.IsNotNull(Idioma.EspanolDe(en), "sin traducción la ayuda de " + p);
            }
            var c = Config("explorador", null, "");
            foreach (ConfiguracionUI.Pestana pestana in System.Enum.GetValues(typeof(ConfiguracionUI.Pestana)))
                Assert.IsNotNull(Idioma.EspanolDe(EnConfiguracion(c, pestana).Texto), pestana.ToString());
            Assert.IsNotNull(Idioma.EspanolDe(GuiaPantallas.Briefing().Texto));
            Assert.IsNotNull(Idioma.EspanolDe(GuiaPantallas.Carga(null).Texto));
        }

        [Test]
        public void ComoJugarExplicaElJuegoCompleto()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity");
            var menu = Object.FindFirstObjectByType<MenuUI>(FindObjectsInactive.Include);
            menu.LlenarComoJugar();
            var pasos = GameObject.Find("Canvas") != null
                ? System.Array.Find(Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), t => t.name == "Pasos")
                : null;
            Assert.IsNotNull(pasos, "el panel «Cómo jugar» tiene su texto de pasos");
            foreach (var clave in new[] { "Build your spacecraft", "How far can you go?", "Operate month by month", "Send to Earth", "Read the report" })
                StringAssert.Contains(clave, pasos.text);
            foreach (var linea in MenuUI.TextoComoJugar().Split('\n'))
            {
                var texto = linea.Substring(linea.IndexOf("  ") + 2);
                Assert.IsNotNull(Idioma.EspanolDe(texto), "sin traducción: " + texto);
            }
        }

        [Test]
        public void EnLaEscenaLaTarjetaSenalaYSePuedeOcultar()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var guia = Object.FindFirstObjectByType<GuiaPantallasUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(guia, "Falta GuiaPantallasUI en la escena Mision");
            Assert.IsNotNull(GameObject.Find("BotonAyuda"), "el botón «?» se ve");

            guia.Actualizar();
            Assert.IsTrue(guia.TarjetaVisible);
            Assert.AreEqual("BotonContinuar", guia.Consejo.Destino);
            Assert.IsNotNull(GameObject.Find("BotonContinuar").GetComponent<ResaltarGuia>(), "el botón que toca late");

            Preferencias.GuiaActiva = false;
            guia.Actualizar();
            Assert.IsFalse(guia.TarjetaVisible, "«×» la oculta");
            Assert.IsNull(GameObject.Find("BotonContinuar").GetComponent<ResaltarGuia>());
            Assert.IsNotNull(GameObject.Find("BotonAyuda"), "el «?» sigue a mano para volver a mostrarla");
        }
    }
}
