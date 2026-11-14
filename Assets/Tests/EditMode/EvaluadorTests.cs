using NUnit.Framework;
using UnityEditor;

namespace SpaceMission.Tests
{
    /// <summary>Casos de la sección 4: semáforo final, resultado y transmisiones.</summary>
    public class EvaluadorTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            Assert.IsNotNull(catalogo);
        }

        Evaluacion Evaluar(string lanzador, string orbita, string instrumentos)
        {
            var ids = string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+');
            return Evaluador.Evaluar(new Configuracion(orbita, lanzador, ids), catalogo);
        }

        static string[] Lista(string texto) => string.IsNullOrEmpty(texto) ? new string[0] : texto.Split(',');

        // Caso, lanzador, órbita, instrumentos, semáforo, resultado ("" = no se lanza), transmisiones en orden, ausentes, fracaso seguro
        [TestCase("1", "explorador", "polar", "termica+radar", EstadoSemaforo.Verde, "Victoria", "TX-00,TX-01,TX-02", "TX-A4,TX-A5", false)]
        [TestCase("2", "explorador", "media", "termica+radar", EstadoSemaforo.Verde, "Victoria", "TX-00,TX-01,TX-02", "", false)]
        [TestCase("3", "explorador", "eliptica", "termica+radar", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01,TX-02", "TX-A3", false)]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", EstadoSemaforo.Verde, "Victoria", "TX-00,TX-01,TX-02,TX-03", "", false)]
        [TestCase("5", "explorador", "media", "termica+atmosferico", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01,TX-03", "TX-A2", false)]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", EstadoSemaforo.Amarillo, "Fracaso", "TX-00,TX-04", "TX-A1,TX-A2,TX-A5", true)]
        [TestCase("7", "titan", "media", "termica+radar+espectrometro+atmosferico", EstadoSemaforo.Rojo, "", "", "", false)]
        [TestCase("B1", "titan", "media", "", EstadoSemaforo.Gris, "", "", "", false)]
        [TestCase("B2", "explorador", "media", "termica+alta_res+atmosferico", EstadoSemaforo.Rojo, "", "", "", false)]
        [TestCase("B4", "titan", "media", "termica+radar+alta_res+atmosferico", EstadoSemaforo.Rojo, "", "", "", false)]
        [TestCase("B4b", "titan", "media", "termica+alta_res+atmosferico+espectrometro", EstadoSemaforo.Rojo, "", "", "", false)]
        [TestCase("B5", "titan", "eliptica", "termica+radar+espectrometro", EstadoSemaforo.Rojo, "", "", "", false)]
        [TestCase("B6", "explorador", "media", "radar", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-02", "TX-A1", false)]
        [TestCase("B7", "titan", null, "termica+radar", EstadoSemaforo.Gris, "", "", "", false)]
        [TestCase("B8", null, "media", "termica+radar", EstadoSemaforo.Gris, "", "", "", false)]
        [TestCase("RV1", "titan", "polar", "termica+alta_res+espectrometro", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01,TX-04,TX-05", "TX-A2", false)]
        [TestCase("RV2", "titan", "media", "termica+espectrometro+atmosferico", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01,TX-03", "TX-A2", false)]
        [TestCase("RV3", "titan", "eliptica", "termica+alta_res+atmosferico", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01,TX-03", "TX-A2", false)]
        [TestCase("RV4", "explorador", "polar", "termica+atmosferico", EstadoSemaforo.Amarillo, "Parcial", "TX-00,TX-01", "TX-A2,TX-A4,TX-A5", false)]
        public void Casos(string caso, string lanzador, string orbita, string instrumentos, EstadoSemaforo semaforo,
            string resultado, string transmisiones, string ausentes, bool fracasoSeguro)
        {
            var e = Evaluar(lanzador, orbita, instrumentos);

            Assert.AreEqual(semaforo, e.Estado, "semáforo del caso " + caso);
            Assert.AreEqual(resultado != "", e.SePuedeLanzar, "lanzamiento del caso " + caso);
            if (resultado != "")
                Assert.AreEqual(resultado, e.Categoria.ToString(), "resultado del caso " + caso);
            CollectionAssert.AreEqual(Lista(transmisiones), e.Transmisiones, "transmisiones del caso " + caso);
            CollectionAssert.AreEqual(Lista(ausentes), e.Ausentes, "ausentes del caso " + caso);
            Assert.AreEqual(fracasoSeguro, e.Mensaje.Contains("fracaso seguro"), "aviso del caso " + caso + ": " + e.Mensaje);
        }

        [Test]
        public void Caso6EsFracasoConAvisoYAunAsiIncluyeTX04()
        {
            var e = Evaluar("titan", "polar", "alta_res+atmosferico");
            Assert.AreEqual(CategoriaResultado.Fracaso, e.Categoria);
            StringAssert.Contains("fracaso seguro", e.Mensaje);
            CollectionAssert.Contains(e.Transmisiones, "TX-04");
            CollectionAssert.IsEmpty(e.PistasPrincipales);
        }

        [Test]
        public void LasPistasYLasEvidenciasQueFaltanSonLasDeLaOrbita()
        {
            var e = Evaluar("explorador", "eliptica", "termica+radar");
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, e.PistasPrincipales);
            CollectionAssert.AreEqual(new[] { "atmosferico" }, e.EvidenciasFaltantes);
            // El mensaje usa las palabras de la evidencia, no el nombre del instrumento.
            StringAssert.Contains(catalogo.Instrumento("atmosferico").pistaAsociada, e.Mensaje);
        }

        [Test]
        public void ElMensajeRojoDiceCadaLimiteSuperado()
        {
            var e = Evaluar("titan", "media", "termica+radar+espectrometro+atmosferico");
            StringAssert.Contains("masa", e.Mensaje);
            StringAssert.Contains("potencia", e.Mensaje);
            StringAssert.Contains("presupuesto", e.Mensaje);
            StringAssert.DoesNotContain("espacios", e.Mensaje);
        }

        [Test]
        public void ElMensajeGrisDiceQueFalta()
        {
            var e = Evaluar("titan", null, "");
            StringAssert.Contains("órbita", e.Mensaje);
            StringAssert.Contains("instrumento", e.Mensaje);
            StringAssert.DoesNotContain("lanzador", e.Mensaje);
        }

        [Test]
        public void LaLineaDeRegistroLlevaElResultado()
        {
            Assert.AreEqual("[Misión] masa=620 potencia=430 costo=850 semáforo=VERDE resultado=VICTORIA",
                Evaluar("explorador", "polar", "termica+radar").LineaRegistro);
            Assert.AreEqual("[Misión] masa=485 potencia=320 costo=800 semáforo=AMARILLO resultado=FRACASO",
                Evaluar("titan", "polar", "alta_res+atmosferico").LineaRegistro);
            Assert.AreEqual("[Misión] masa=780 potencia=760 costo=1300 semáforo=ROJO resultado=-",
                Evaluar("titan", "media", "termica+radar+espectrometro+atmosferico").LineaRegistro);
        }
    }
}
