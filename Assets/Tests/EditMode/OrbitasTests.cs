using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 7 (paquete de diseño): en el visor se ve solo la órbita elegida, y hay cinco órbitas: las tres
    /// originales, la ecuatorial baja y la areoestacionaria.
    /// </summary>
    public class OrbitasTests
    {
        static readonly string[] Cinco = { "polar", "media", "eliptica", "ecuatorial", "areoestacionaria" };
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar() => catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");

        static Configuracion Config(string lanzador, string orbita, string instrumentos) =>
            new Configuracion(orbita, lanzador, string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+'));

        (ConfiguracionUI ui, AnilloOrbita anillo, MisionUI mision) AbrirConfiguracion()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var mision = Object.FindFirstObjectByType<MisionUI>(FindObjectsInactive.Include);
            mision.MostrarConfiguracion();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();
            var anillo = Object.FindFirstObjectByType<AnilloOrbita>(FindObjectsInactive.Include);
            Assert.IsNotNull(anillo, "Falta AnilloOrbita en la escena Mision");
            return (ui, anillo, mision);
        }

        /// <summary>Todas las líneas de órbita de la escena que se están dibujando (la del anillo y sus hijas, menos el halo y la estela).</summary>
        static int LineasDeOrbita(AnilloOrbita anillo) =>
            anillo.GetComponentsInChildren<LineRenderer>(true)
                .Count(l => l.enabled && l.gameObject.activeInHierarchy && l.name != "Brillo" && l.name != "Estela");

        [Test]
        public void LasCincoOrbitasEstanEnElCatalogoYSeSabenDibujar()
        {
            CollectionAssert.AreEqual(Cinco, catalogo.orbitas.Select(o => o.id).ToArray());
            CollectionAssert.AreEquivalent(Cinco, Trayectoria.Ids.ToArray());
            CollectionAssert.AllItemsAreUnique(Cinco.Select(Trayectoria.ColorHex).ToArray(), "cada órbita tiene su color");
            Assert.IsTrue(Trayectoria.NoAEscala("areoestacionaria"));
            Assert.IsFalse(Cinco.Take(4).Any(Trayectoria.NoAEscala));
            // Las tres originales no cambian de costo.
            Assert.AreEqual((120, 90, 140), (catalogo.Orbita("polar").costoCr, catalogo.Orbita("media").costoCr, catalogo.Orbita("eliptica").costoCr));
        }

        [Test]
        public void SinEleccionNoSeDibujaNingunaTrayectoria()
        {
            var (ui, anillo, _) = AbrirConfiguracion();
            foreach (ConfiguracionUI.Pestana p in System.Enum.GetValues(typeof(ConfiguracionUI.Pestana)))
            {
                ui.MostrarPestana(p);
                anillo.Actualizar();
                Assert.AreEqual(0, anillo.TrayectoriasVisibles, "pestaña " + p);
                Assert.AreEqual(0, LineasDeOrbita(anillo), "pestaña " + p);
                Assert.AreEqual(0, anillo.MarcadoresVisibles, "pestaña " + p);
            }
            StringAssert.StartsWith("SELECT AN ORBIT", Object.FindFirstObjectByType<GuiaConfiguracion>(FindObjectsInactive.Include).RotuloOrbita(null));
        }

        [Test]
        public void SoloSeVeLaOrbitaElegidaEnLasVeinteTransiciones()
        {
            var (ui, anillo, _) = AbrirConfiguracion();
            ui.ElegirLanzador("titan");
            ui.AlternarInstrumento("termica");
            ui.MostrarPestana(ConfiguracionUI.Pestana.Orbita);
            foreach (var desde in Cinco)
                foreach (var hasta in Cinco.Where(o => o != desde))
                {
                    Assert.IsTrue(ui.ElegirOrbita(desde));
                    anillo.Actualizar();
                    Assert.IsTrue(ui.ElegirOrbita(hasta));
                    anillo.Actualizar();   // un solo paso: la anterior se apaga en el mismo cuadro en que entra la nueva
                    Assert.AreEqual(hasta, anillo.Dibujada, desde + " → " + hasta);
                    Assert.AreEqual(1, anillo.TrayectoriasVisibles, desde + " → " + hasta);
                    Assert.AreEqual(1, LineasDeOrbita(anillo), desde + " → " + hasta);
                    Assert.AreEqual(1, anillo.MarcadoresVisibles, desde + " → " + hasta);
                    Assert.AreEqual(hasta, EstadoJuego.Configuracion.OrbitaId, "lo que se ve es lo que se cobra");
                }
        }

        [Test]
        public void CincoCambiosRapidosDejanUnaSola()
        {
            var (ui, anillo, _) = AbrirConfiguracion();
            ui.ElegirLanzador("titan");
            ui.AlternarInstrumento("termica");
            foreach (var id in Cinco) ui.ElegirOrbita(id);   // sin dejar pasar ningún cuadro entre uno y otro
            anillo.Actualizar();
            Assert.AreEqual("areoestacionaria", anillo.Dibujada);
            Assert.AreEqual(1, LineasDeOrbita(anillo));
        }

        [Test]
        public void LaEleccionSeConservaAlCambiarDePestanaYEnLaRevision()
        {
            var (ui, anillo, mision) = AbrirConfiguracion();
            ui.ElegirLanzador("explorador");
            ui.AlternarInstrumento("termica");
            ui.AlternarInstrumento("radar");
            ui.ElegirOrbita("ecuatorial");
            foreach (var p in new[] { ConfiguracionUI.Pestana.Lanzador, ConfiguracionUI.Pestana.Satelite, ConfiguracionUI.Pestana.Instrumentos, ConfiguracionUI.Pestana.Orbita })
            {
                ui.MostrarPestana(p);
                anillo.Actualizar();
                Assert.AreEqual("ecuatorial", anillo.Dibujada, "pestaña " + p);
                Assert.AreEqual(1, LineasDeOrbita(anillo), "pestaña " + p);
            }
            mision.MostrarRevision(ui.Recalcular());
            anillo.Actualizar();
            Assert.AreEqual("ecuatorial", anillo.Dibujada, "la Revisión se ve encima de la misma órbita");
            mision.MostrarBriefing();
            anillo.Actualizar();
            Assert.AreEqual(0, LineasDeOrbita(anillo), "en el Briefing, Marte sin trayectorias");
            mision.MostrarConfiguracion();
            anillo.Actualizar();
            Assert.AreEqual("ecuatorial", anillo.Dibujada, "al volver se recupera la elegida");
        }

        [Test]
        public void ElRotuloDiceLaOrbitaYAvisaSiNoEstaAEscala()
        {
            AbrirConfiguracion();
            var guia = Object.FindFirstObjectByType<GuiaConfiguracion>(FindObjectsInactive.Include);
            StringAssert.Contains("MID INCLINED", guia.RotuloOrbita("media"));
            StringAssert.Contains("400 km", guia.RotuloOrbita("media"));
            StringAssert.Contains("150 × 6,200 km", guia.RotuloOrbita("eliptica"));
            StringAssert.Contains("17,031 km", guia.RotuloOrbita("areoestacionaria"));
            StringAssert.Contains("Schematic, not to scale", guia.RotuloOrbita("areoestacionaria"));
            StringAssert.DoesNotContain("not to scale", guia.RotuloOrbita("polar"));
        }

        [Test]
        public void LaCamaraEncajaLaOrbitaEnElHuecoCentral()
        {
            var (_, anillo, _) = AbrirConfiguracion();
            var camara = Object.FindFirstObjectByType<CamaraConfiguracion>();
            var so = new SerializedObject(camara);
            var p = so.FindProperty("planeta");
            var planeta = new CamaraConfiguracion.Encuadre
            {
                posicion = p.FindPropertyRelative("posicion").vector3Value,
                mirarA = p.FindPropertyRelative("mirarA").vector3Value,
                campo = p.FindPropertyRelative("campo").floatValue
            };
            float sinOrbita = Vector3.Distance(camara.EncajarOrbita(planeta, null).posicion, anillo.Centro);
            foreach (var id in Cinco)
            {
                var e = camara.EncajarOrbita(planeta, id);
                float distancia = Vector3.Distance(e.posicion, anillo.Centro);
                Assert.GreaterOrEqual(distancia, sinOrbita - 0.01f, id + ": nunca más cerca que con el planeta solo");
                // Las circulares dejan a Marte en el centro; la elíptica se encuadra con el planeta a un lado.
                float descentrado = Vector3.Distance(anillo.Centro, e.mirarA);
                if (Trayectoria.Excentricidad(id) > 0f) Assert.Greater(descentrado, anillo.RadioPlaneta * 0.2f, id);
                else Assert.Less(descentrado, anillo.RadioPlaneta * 0.05f, id + ": Marte en el centro");
            }
            Assert.Greater(Vector3.Distance(camara.EncajarOrbita(planeta, "areoestacionaria").posicion, anillo.Centro),
                Vector3.Distance(camara.EncajarOrbita(planeta, "polar").posicion, anillo.Centro), "la lejana pide alejarse más");
        }

        // ---------- Reglas de las dos órbitas nuevas ----------

        [Test]
        public void LaEcuatorialBajaGanaConTermicaYRadar()
        {
            var e = Evaluador.Evaluar(Config("explorador", "ecuatorial", "termica+radar"), catalogo);
            Assert.AreEqual(EstadoSemaforo.Verde, e.Estado);
            Assert.AreEqual(CategoriaResultado.Victoria, e.Categoria);
            Assert.AreEqual(830, e.Calculo.CostoCr, "100 de base + 190 del Explorador + 100 de la órbita + 440 de instrumentos");
            Assert.AreEqual(2116, e.Calculo.DeltaVNecesarioMs);
            Assert.AreEqual(478, Alcance.MasaMaximaKg(catalogo.Lanzador("explorador"), catalogo.Orbita("ecuatorial"), catalogo.reglas));
            // Desbloquea la revelación de la cámara de alta resolución (con el Titán, que tiene sitio).
            var conCamara = Evaluador.Evaluar(Config("titan", "ecuatorial", "termica+radar+alta_res"), catalogo);
            CollectionAssert.Contains(conCamara.Transmisiones, "TX-04");
        }

        [Test]
        public void LaAreoestacionariaDaComoMuchoUnResultadoParcial()
        {
            var orbita = catalogo.Orbita("areoestacionaria");
            Assert.AreEqual(1941, Alcance.DeltaVInsercionMs(orbita, catalogo.reglas));
            CollectionAssert.AreEquivalent(new[] { "radar", "alta_res" }, orbita.instrumentosSinAlcance);

            // Aunque lleve térmica y radar: el radar no trabaja desde tan lejos.
            var e = Evaluador.Evaluar(Config("explorador", "areoestacionaria", "termica+radar"), catalogo);
            Assert.AreEqual(EstadoSemaforo.Amarillo, e.Estado);
            Assert.AreEqual(CategoriaResultado.Parcial, e.Categoria);
            Assert.IsTrue(e.SePuedeLanzar);
            StringAssert.Contains("at most a partial result", e.Mensaje);
            CollectionAssert.DoesNotContain(e.Transmisiones, "TX-02", "el radar no da evidencia");
            CollectionAssert.Contains(e.Transmisiones, "TX-01", "la térmica sí");

            // Con el sensor atmosférico, su revelación sí llega (vigilancia de la tormenta).
            var conSensor = Evaluador.Evaluar(Config("titan", "areoestacionaria", "termica+atmosferico"), catalogo);
            CollectionAssert.Contains(conSensor.Transmisiones, "TX-03");
            Assert.AreEqual(CategoriaResultado.Parcial, conSensor.Categoria);

            // Sin ninguna pista principal útil, fracaso anunciado.
            var soloRadar = Evaluador.Evaluar(Config("explorador", "areoestacionaria", "radar"), catalogo);
            Assert.AreEqual(CategoriaResultado.Fracaso, soloRadar.Categoria);

            // Y jugando: aunque se reciba todo lo posible, el resultado final sigue siendo parcial.
            var op = new Operacion(e, Config("explorador", "areoestacionaria", "termica+radar"), catalogo, catalogo.mision, 7);
            op.Recibidas.Add("TX-01");
            Assert.AreEqual(CategoriaResultado.Parcial, ResultadoCientifico.Categoria(e, op, catalogo));
        }

        [Test]
        public void LasTresOrbitasOriginalesNoCambianDeResultado()
        {
            Assert.AreEqual(CategoriaResultado.Victoria, Evaluador.Evaluar(Config("explorador", "polar", "termica+radar"), catalogo).Categoria);
            Assert.AreEqual(CategoriaResultado.Victoria, Evaluador.Evaluar(Config("explorador", "media", "termica+radar"), catalogo).Categoria);
            Assert.AreEqual(CategoriaResultado.Parcial, Evaluador.Evaluar(Config("explorador", "eliptica", "termica+radar"), catalogo).Categoria);
            foreach (var id in new[] { "polar", "media", "eliptica" })
                CollectionAssert.IsEmpty(catalogo.Orbita(id).instrumentosSinAlcance, id);
        }
    }
}
