using NUnit.Framework;
using UnityEditor;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Arreglos de la revisión del PR #1 (versiones 6 y 7): los instrumentos sin alcance desde la órbita
    /// areoestacionaria y el alcance que la Carga debe seguir respetando.
    /// </summary>
    public class SinAlcanceTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar() => catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");

        Operacion Operar(Configuracion c)
        {
            var e = Evaluador.Evaluar(c, catalogo);
            return new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
        }

        [Test]
        public void EnLaAreoestacionariaElRadarNoSeEnciendeYLaTermicaSi()
        {
            var op = Operar(new Configuracion("areoestacionaria", "titan", new[] { "termica", "radar" }));
            Assert.IsTrue(op.SinAlcance("radar"));
            Assert.IsFalse(op.Encender("radar"), "no gasta potencia ni memoria si no puede dar evidencia");
            Assert.IsFalse(op.Encendidos.Contains("radar"));
            Assert.IsFalse(op.SinAlcance("termica"));
            Assert.IsTrue(op.Encender("termica"));
        }

        [Test]
        public void EnOtraOrbitaElRadarSiSeEnciende()
        {
            var op = Operar(new Configuracion("media", "titan", new[] { "termica", "radar" }));
            Assert.IsFalse(op.SinAlcance("radar"));
            Assert.IsTrue(op.Encender("radar"));
        }

        [Test]
        public void ElInformeNoCulpaAlRadarAusenteSiIbaABordo()
        {
            var e = Evaluador.Evaluar(new Configuracion("areoestacionaria", "titan", new[] { "termica", "radar" }), catalogo);
            CollectionAssert.DoesNotContain(e.Ausentes, "TX-A2", "el radar iba a bordo: lo que falló fue la distancia");
            StringAssert.Contains("cannot work", e.Mensaje);
        }

        [Test]
        public void SinRadarNiCamaraElMensajeNoHablaDeLaDistancia()
        {
            var e = Evaluador.Evaluar(new Configuracion("areoestacionaria", "titan", new[] { "termica" }), catalogo);
            StringAssert.DoesNotContain("cannot work", e.Mensaje);
            CollectionAssert.Contains(e.Ausentes, "TX-A2", "sin radar a bordo sí falta el radar");
        }

        [Test]
        public void LaCargaNoDejaPasarseDelAlcance()
        {
            // Explorador + polar + térmica + radar + Chandra = 470 kg secos; a la polar llega con 480 como máximo.
            var c = new Configuracion("polar", "explorador", new[] { "termica", "radar" }) { SateliteId = "chandra" };
            Assert.AreEqual(480, Alcance.MasaMaximaKg(catalogo.Lanzador("explorador"), catalogo.Orbita("polar"), catalogo.reglas));
            var carga = new Carga(Evaluador.Evaluar(c, catalogo), c, catalogo);
            Assert.IsTrue(carga.AlcanzaCon());
            Assert.IsFalse(carga.PuedeCargar("bateria"), "20 kg más: 490 kg ya no llegan");
            Assert.IsFalse(carga.Cargar("bateria"));
            Assert.IsTrue(carga.Cargar("reconfiguracion"), "10 kg más: 480 kg sí llegan");
            Assert.IsFalse(carga.PuedeCargar("giroscopio"), "otros 15 kg ya no");
        }
    }
}
