using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 6: hasta qué órbita llega la nave, con física real (vis-viva y ecuación del cohete, sin aerofrenado).
    /// </summary>
    public class AlcanceTests
    {
        CatalogoDatos catalogo;
        ReglasOperacionDatos reglas;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            reglas = catalogo.reglas;
        }

        static Configuracion Config(string lanzador, string orbita, string instrumentos, string satelite = null) =>
            new Configuracion(orbita, lanzador, string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+'))
                { SateliteId = satelite };

        LanzadorDatos Lanzador(string id) => catalogo.Lanzador(id);
        OrbitaDatos Orbita(string id) => catalogo.Orbita(id);

        [Test]
        public void LosDatosRealesEstanCargados()
        {
            Assert.AreEqual(1223, Lanzador("explorador").masaInyectadaKg, "Mars Express en Soyuz-Fregat");
            Assert.AreEqual(1850, Lanzador("titan").masaInyectadaKg);
            Assert.AreEqual(2.7f, reglas.velocidadLlegadaKmS);
            Assert.AreEqual(230, reglas.impulsoEspecificoS);
            Assert.AreEqual((255, 320), (Orbita("polar").periapsisKm, Orbita("polar").apoapsisKm), "MRO");
            Assert.AreEqual((400, 400), (Orbita("media").periapsisKm, Orbita("media").apoapsisKm), "TGO");
            Assert.AreEqual((150, 6200), (Orbita("eliptica").periapsisKm, Orbita("eliptica").apoapsisKm), "MAVEN");
            Assert.IsFalse(string.IsNullOrEmpty(reglas.datoNasaAlcance));
            Assert.IsFalse(string.IsNullOrEmpty(reglas.datoNasaAlcanceEs));
        }

        [Test]
        public void CadaOrbitaPideSuFrenadoReal()
        {
            // Una órbita baja y redonda pide más frenado que una elíptica alta. Polar y media casi no se distinguen.
            Assert.AreEqual(2106, Alcance.DeltaVInsercionMs(Orbita("polar"), reglas));
            Assert.AreEqual(2106, Alcance.DeltaVInsercionMs(Orbita("media"), reglas));
            Assert.AreEqual(1407, Alcance.DeltaVInsercionMs(Orbita("eliptica"), reglas));
        }

        [TestCase("explorador", "polar", 480)]
        [TestCase("explorador", "media", 480)]
        [TestCase("explorador", "eliptica", 655)]
        [TestCase("titan", "polar", 727)]
        [TestCase("titan", "eliptica", 991)]
        public void LaMasaMaximaEsElUmbralExacto(string lanzador, string orbita, int masaMaxima)
        {
            var l = Lanzador(lanzador);
            var o = Orbita(orbita);
            Assert.AreEqual(masaMaxima, Alcance.MasaMaximaKg(l, o, reglas));
            Assert.IsTrue(Alcance.Evaluar(l, o, masaMaxima, reglas).Alcanza, "con la masa máxima justa sí llega");
            Assert.IsFalse(Alcance.Evaluar(l, o, masaMaxima + 1, reglas).Alcanza, "con 1 kg más ya no");
        }

        [Test]
        public void LaFormulaReproduceMisionesReales()
        {
            // MRO: 2,180 kg al salir y 1,031 kg en seco. Solo con motores no llegaba a 255 × 320 km: necesitó aerofrenado.
            var mro = ScriptableObject.CreateInstance<LanzadorDatos>();
            mro.masaInyectadaKg = 2180;
            Assert.AreEqual(1688, Alcance.DeltaVDisponibleMs(mro, 1031, reglas));
            Assert.IsFalse(Alcance.Evaluar(mro, Orbita("polar"), 1031, reglas).Alcanza);
            Object.DestroyImmediate(mro);
        }

        [Test]
        public void SinDatosNoSeCalculaYTodoEsAlcanzable()
        {
            var l = ScriptableObject.CreateInstance<LanzadorDatos>();
            var a = Alcance.Evaluar(l, Orbita("polar"), 5000, reglas);
            Assert.IsFalse(a.Calculable);
            Assert.IsTrue(a.Alcanza);
            Assert.AreEqual(0, Alcance.DeltaVDisponibleMs(Lanzador("explorador"), 5000, reglas), "sin combustible, sin frenado");
            Object.DestroyImmediate(l);
        }

        [Test]
        public void ElSatelitePesaYDecideLaOrbita()
        {
            // Térmica + radar (430 kg) llega a todas las órbitas con cualquier satélite (Chandra: 470 kg; la más
            // exigente, la ecuatorial baja, admite 478).
            foreach (var s in catalogo.satelites)
                Assert.AreEqual(5, Alcance.Alcanzables(Config("explorador", null, "termica+radar", s.id), catalogo).Count, s.id);

            // Radar + espectrómetro (495 kg) ya no llega a las bajas: con el Explorador quedan la elíptica y la
            // areoestacionaria (piden menos frenado); con el Titán, todas.
            CollectionAssert.AreEqual(new[] { "eliptica", "areoestacionaria" },
                Alcance.Alcanzables(Config("explorador", null, "radar+espectrometro"), catalogo).Select(o => o.id).ToArray());
            Assert.AreEqual(5, Alcance.Alcanzables(Config("titan", null, "radar+espectrometro", "chandra"), catalogo).Count);
        }

        [Test]
        public void SinAlcanceElSemaforoEsRojoYDiceQueOrbitasSiLlegan()
        {
            var e = Evaluador.Evaluar(Config("explorador", "media", "radar+espectrometro"), catalogo);
            Assert.AreEqual(EstadoSemaforo.Rojo, e.Estado);
            CollectionAssert.AreEqual(new[] { Limite.Alcance }, e.Calculo.LimitesSuperados);
            StringAssert.Contains("Not enough braking for this orbit: it needs 2,106 m/s", e.Mensaje);
            StringAssert.Contains("Within reach: Elliptical", e.Mensaje);
        }

        [Test]
        public void CadaNivelSigueSiendoGanable()
        {
            foreach (var nivel in catalogo.niveles)
            {
                var e = Evaluador.Evaluar(Config("explorador", "media", "termica+radar", "chandra"), catalogo, nivel);
                Assert.AreEqual(CategoriaResultado.Victoria, e.Categoria, nivel.name);
                Assert.IsTrue(e.SePuedeLanzar, nivel.name);
            }
        }

        [Test]
        public void EnLaConfiguracionUnaOrbitaFueraDeAlcanceNoSeElige()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();
            Assert.AreEqual(ConfiguracionUI.Pestana.Lanzador, ui.PestanaActual, "versión 6: se empieza por el lanzador");
            Assert.AreEqual(5, ui.TextosBarra.Count, "masa, potencia, costo, espacios y frenado");

            ui.ElegirLanzador("explorador");
            ui.AlternarInstrumento("radar");
            ui.AlternarInstrumento("espectrometro");
            ui.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            Assert.IsFalse(ui.ElegirOrbita("media"), "495 kg no llegan a la órbita media con el Explorador");
            Assert.IsNull(EstadoJuego.Configuracion.OrbitaId);
            Assert.IsTrue(ui.ElegirOrbita("eliptica"));
            Assert.AreEqual(EstadoSemaforo.Amarillo, ui.Ultima.Estado);
            StringAssert.StartsWith("Braking: 1,407 / 2,040 m/s", ui.TextosBarra[4].text);
        }

        [Test]
        public void ElDatoDeLaNasaSoloSeVeAlPulsarSuBoton()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();

            ui.ElegirLanzador("explorador");
            StringAssert.DoesNotContain("NASA data", ui.TextoAyuda, "cerrado al elegir");
            Assert.IsFalse(ui.NasaAbierto);
            ui.AlternarNasa();
            StringAssert.Contains("NASA data", ui.TextoAyuda);
            StringAssert.Contains("1,223 kg", ui.TextoAyuda);
            ui.AlternarNasa();
            StringAssert.DoesNotContain("NASA data", ui.TextoAyuda, "se vuelve a cerrar");

            // Otra opción empieza cerrada aunque la anterior estuviera abierta.
            ui.AlternarNasa();
            ui.AlternarInstrumento("termica");
            Assert.IsFalse(ui.NasaAbierto);

            // La nota del alcance es corta; al pulsarla explica la fórmula en la ayuda.
            ui.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            var nota = System.Array.Find(ui.GetComponentsInChildren<UnityEngine.UI.Button>(true), b => b.name == "NotaAlcance")?.gameObject;
            Assert.IsNotNull(nota, "la pestaña de órbitas tiene su nota de alcance");
            var textoNota = nota.GetComponentInChildren<TMPro.TMP_Text>().text;
            StringAssert.Contains("Your braking: <b>2,606 m/s</b>", textoNota);
            StringAssert.DoesNotContain("ln(", textoNota);
            nota.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            StringAssert.Contains("230 s × 9.807 × ln(1,223 kg ÷ 385 kg)", ui.TextoAyuda);
        }

        [Test]
        public void UnaOrbitaElegidaQueDejaDeAlcanzarseSeConservaEnRojo()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();

            ui.ElegirLanzador("explorador");
            ui.AlternarInstrumento("termica");
            ui.AlternarInstrumento("radar");
            Assert.IsTrue(ui.ElegirOrbita("media"));
            Assert.AreEqual(EstadoSemaforo.Verde, ui.Ultima.Estado);

            // Un satélite más pesado no basta (470 de 480 kg); cambiar el radar por el espectrómetro sí.
            ui.ElegirSatelite("chandra");
            Assert.AreEqual(EstadoSemaforo.Verde, ui.Ultima.Estado);
            ui.AlternarInstrumento("radar");
            ui.AlternarInstrumento("espectrometro");
            Assert.AreEqual("media", EstadoJuego.Configuracion.OrbitaId, "no se borra la órbita elegida");
            Assert.AreEqual(EstadoSemaforo.Rojo, ui.Ultima.Estado);
            CollectionAssert.Contains(ui.Ultima.Calculo.LimitesSuperados, Limite.Alcance);
            Assert.IsTrue(ui.ElegirOrbita("media"), "la ya elegida se puede volver a pulsar (para ver la explicación)");
        }
    }
}
