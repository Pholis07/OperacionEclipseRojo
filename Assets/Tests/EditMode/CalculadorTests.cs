using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SpaceMission.Tests
{
    /// <summary>Casos de la sección 3: masa, potencia, costo y bloqueo (Gris, Rojo o Válida).</summary>
    public class CalculadorTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            Assert.IsNotNull(catalogo);
        }

        static Configuracion Config(string lanzador, string orbita, string instrumentos)
        {
            var ids = string.IsNullOrEmpty(instrumentos) ? new string[0] : instrumentos.Split('+');
            return new Configuracion(orbita, lanzador, ids);
        }

        static Limite[] Limites(string texto)
        {
            return string.IsNullOrEmpty(texto)
                ? new Limite[0]
                : texto.Split('+').Select(t => (Limite)Enum.Parse(typeof(Limite), t)).ToArray();
        }

        // Caso, lanzador, órbita, instrumentos, masa, potencia, costo, espacios, estado, límites superados
        [TestCase("1", "explorador", "polar", "termica+radar", 430, 220, 850, 2, EstadoSemaforo.Valida, "")]
        [TestCase("2", "explorador", "media", "termica+radar", 430, 220, 820, 2, EstadoSemaforo.Valida, "")]
        [TestCase("3", "explorador", "eliptica", "termica+radar", 430, 220, 870, 2, EstadoSemaforo.Valida, "")]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", 455, 275, 1130, 3, EstadoSemaforo.Valida, "")]
        [TestCase("5", "explorador", "media", "termica+atmosferico", 410, 225, 650, 2, EstadoSemaforo.Valida, "")]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", 570, 455, 870, 2, EstadoSemaforo.Valida, "")]
        [TestCase("7", "titan", "media", "termica+radar+espectrometro+atmosferico", 555, 500, 1300, 4, EstadoSemaforo.Rojo, "Costo")]
        [TestCase("B2", "explorador", "media", "termica+alta_res+atmosferico", 605, 525, 850, 3, EstadoSemaforo.Rojo, "Masa+Espacios+Alcance")]
        [TestCase("B4", "titan", "media", "termica+radar+alta_res+atmosferico", 650, 575, 1280, 4, EstadoSemaforo.Rojo, "Costo")]
        [TestCase("B4b", "titan", "media", "termica+alta_res+atmosferico+espectrometro", 705, 750, 1240, 4, EstadoSemaforo.Rojo, "Potencia")]
        [TestCase("B5", "titan", "eliptica", "termica+radar+espectrometro", 530, 445, 1260, 3, EstadoSemaforo.Rojo, "Costo")]
        [TestCase("B6", "explorador", "media", "radar", 395, 150, 640, 1, EstadoSemaforo.Valida, "")]
        [TestCase("RV1", "titan", "polar", "termica+alta_res+espectrometro", 680, 695, 1180, 3, EstadoSemaforo.Valida, "")]
        [TestCase("RV2", "titan", "media", "termica+espectrometro+atmosferico", 510, 450, 1040, 3, EstadoSemaforo.Valida, "")]
        [TestCase("RV3", "titan", "eliptica", "termica+alta_res+atmosferico", 605, 525, 1070, 3, EstadoSemaforo.Valida, "")]
        [TestCase("RV4", "explorador", "polar", "termica+atmosferico", 410, 225, 680, 2, EstadoSemaforo.Valida, "")]
        public void CasosCompletos(string caso, string lanzador, string orbita, string instrumentos,
            int masa, int potencia, int costo, int espacios, EstadoSemaforo estado, string limites)
        {
            var r = Calculador.Calcular(Config(lanzador, orbita, instrumentos), catalogo);

            Assert.AreEqual(masa, r.MasaKg, "masa del caso " + caso);
            Assert.AreEqual(potencia, r.PotenciaW, "potencia del caso " + caso);
            Assert.AreEqual(costo, r.CostoCr, "costo del caso " + caso);
            Assert.AreEqual(espacios, r.EspaciosUsados, "espacios del caso " + caso);
            Assert.AreEqual(estado, r.Estado, "estado del caso " + caso);
            CollectionAssert.AreEquivalent(Limites(limites), r.LimitesSuperados, "límites del caso " + caso);
            CollectionAssert.IsEmpty(r.Faltantes, "faltantes del caso " + caso);
        }

        [TestCase("B1", "titan", "media", "", Faltante.Instrumentos)]
        [TestCase("B7", "titan", null, "termica+radar", Faltante.Orbita)]
        [TestCase("B8", null, "media", "termica+radar", Faltante.Lanzador)]
        public void CasosIncompletosDanGris(string caso, string lanzador, string orbita, string instrumentos, Faltante falta)
        {
            var r = Calculador.Calcular(Config(lanzador, orbita, instrumentos), catalogo);

            Assert.AreEqual(EstadoSemaforo.Gris, r.Estado, "estado del caso " + caso);
            CollectionAssert.AreEqual(new[] { falta }, r.Faltantes, "faltantes del caso " + caso);
            CollectionAssert.IsEmpty(r.LimitesSuperados);
        }

        [Test]
        public void SinNadaFaltanLasTresCosas()
        {
            var r = Calculador.Calcular(new Configuracion(), catalogo);
            Assert.AreEqual(EstadoSemaforo.Gris, r.Estado);
            CollectionAssert.AreEquivalent(new[] { Faltante.Orbita, Faltante.Lanzador, Faltante.Instrumentos }, r.Faltantes);
        }

        [Test]
        public void CambiarDeLanzadorNoBorraInstrumentos()
        {
            var c = Config("titan", "media", "termica+alta_res+atmosferico");
            Assert.AreEqual(EstadoSemaforo.Valida, Calculador.Calcular(c, catalogo).Estado);

            c.LanzadorId = "explorador";
            var r = Calculador.Calcular(c, catalogo);

            Assert.AreEqual(3, c.InstrumentoIds.Count);
            Assert.AreEqual(EstadoSemaforo.Rojo, r.Estado);
            // Versión 5: la cámara de alta resolución pesa 195 kg (HiRISE) y el Explorador lleva 500 kg: falla también la masa.
            CollectionAssert.AreEquivalent(new[] { Limite.Masa, Limite.Espacios, Limite.Alcance }, r.LimitesSuperados);
        }

        [Test]
        public void UnInstrumentoRepetidoCuentaUnaVez()
        {
            var r = Calculador.Calcular(Config("explorador", "media", "radar+radar"), catalogo);
            Assert.AreEqual(1, r.EspaciosUsados);
            Assert.AreEqual(395, r.MasaKg);
        }

        [Test]
        public void ValoresIgualesAlLimiteNoBloquean()
        {
            // Copias en memoria: los assets no se tocan. El caso 1 da 430 kg, 220 W y 850 cr.
            var mision = Object.Instantiate(catalogo.mision);
            mision.potenciaDisponibleW = 220;
            mision.presupuestoCr = 850;
            var explorador = Object.Instantiate(catalogo.Lanzador("explorador"));
            explorador.capacidadKg = 430;
            var copia = Object.Instantiate(catalogo);
            copia.mision = mision;
            copia.lanzadores = copia.lanzadores.Select(l => l.id == "explorador" ? explorador : l).ToList();
            try
            {
                var r = Calculador.Calcular(Config("explorador", "polar", "termica+radar"), copia);
                Assert.AreEqual(EstadoSemaforo.Valida, r.Estado);
                CollectionAssert.IsEmpty(r.LimitesSuperados);
            }
            finally
            {
                Object.DestroyImmediate(copia);
                Object.DestroyImmediate(mision);
                Object.DestroyImmediate(explorador);
            }
        }

        [Test]
        public void LaLineaDeRegistroTieneElFormatoAcordado()
        {
            var r = Calculador.Calcular(Config("explorador", "polar", "termica+radar"), catalogo);
            Assert.AreEqual("[Misión] masa=430 potencia=220 costo=850 semáforo=VALIDA resultado=-", Calculador.LineaRegistro(r));
            Assert.AreEqual("[Misión] masa=430 potencia=220 costo=850 semáforo=VERDE resultado=VICTORIA",
                Calculador.LineaRegistro(r, EstadoSemaforo.Verde, "VICTORIA"));

            var gris = Calculador.Calcular(Config("titan", "media", ""), catalogo);
            Assert.AreEqual("[Misión] masa=- potencia=- costo=- semáforo=GRIS resultado=-", Calculador.LineaRegistro(gris));
        }
    }
}
