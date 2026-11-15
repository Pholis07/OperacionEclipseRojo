using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace SpaceMission.Tests
{
    /// <summary>Comprueban que los assets de Assets/Datos coinciden con las tablas del diseño.</summary>
    public class DatosTests
    {
        const string RutaCatalogo = "Assets/Datos/Catalogo.asset";
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);
            Assert.IsNotNull(catalogo, "Falta " + RutaCatalogo);
        }

        [Test]
        public void ElCatalogoTieneLosElementosEsperados()
        {
            Assert.IsNotNull(catalogo.mision);
            Assert.AreEqual(3, catalogo.orbitas.Count);
            Assert.AreEqual(2, catalogo.lanzadores.Count);
            Assert.AreEqual(5, catalogo.instrumentos.Count);
        }

        [Test]
        public void LosIdsSonUnicosYSonLosEsperados()
        {
            CollectionAssert.AreEquivalent(new[] { "polar", "media", "eliptica" }, catalogo.orbitas.Select(o => o.id).ToArray());
            CollectionAssert.AreEquivalent(new[] { "explorador", "titan" }, catalogo.lanzadores.Select(l => l.id).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "termica", "radar", "alta_res", "atmosferico", "espectrometro" },
                catalogo.instrumentos.Select(i => i.id).ToArray());
            CollectionAssert.AllItemsAreUnique(catalogo.transmisiones.Select(t => t.id).ToArray());
        }

        [Test]
        public void LasListasDeLasOrbitasApuntanAInstrumentosQueExisten()
        {
            foreach (var orbita in catalogo.orbitas)
            {
                foreach (var id in orbita.requisitoDeVictoria.Concat(orbita.revelacionesDesbloqueadas))
                    Assert.IsNotNull(catalogo.Instrumento(id), orbita.id + " apunta a un instrumento que no existe: " + id);
            }
        }

        [Test]
        public void LosRequisitosDeVictoriaYLasRevelacionesSonLosDelDiseno()
        {
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, catalogo.Orbita("polar").requisitoDeVictoria);
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, catalogo.Orbita("media").requisitoDeVictoria);
            CollectionAssert.AreEqual(new[] { "termica", "radar", "atmosferico" }, catalogo.Orbita("eliptica").requisitoDeVictoria);

            CollectionAssert.AreEquivalent(new[] { "alta_res", "espectrometro" }, catalogo.Orbita("polar").revelacionesDesbloqueadas);
            CollectionAssert.AreEquivalent(new[] { "atmosferico" }, catalogo.Orbita("media").revelacionesDesbloqueadas);
            CollectionAssert.AreEquivalent(new[] { "atmosferico" }, catalogo.Orbita("eliptica").revelacionesDesbloqueadas);
        }

        [TestCase("termica", 100, 100, 30, 130, 180)]
        [TestCase("radar", 170, 180, 20, 200, 260)]
        [TestCase("alta_res", 85, 90, 60, 150, 130)]
        [TestCase("atmosferico", 50, 60, 10, 70, 90)]
        [TestCase("espectrometro", 110, 130, 130, 260, 220)]
        public void LosInstrumentosCoincidenConLaTabla(string id, int masa, int normal, int extra, int total, int costo)
        {
            var i = catalogo.Instrumento(id);
            Assert.AreEqual(masa, i.masaKg);
            Assert.AreEqual(normal, i.potenciaNormalW);
            Assert.AreEqual(extra, i.extraTormentaW);
            Assert.AreEqual(total, i.ConsumoTotalW);
            Assert.AreEqual(costo, i.costoCr);
        }

        [TestCase("termica")]
        [TestCase("radar")]
        [TestCase("alta_res")]
        [TestCase("atmosferico")]
        [TestCase("espectrometro")]
        public void LaAyudaDeCadaInstrumentoMuestraElDesgloseDelConsumo(string id)
        {
            var i = catalogo.Instrumento(id);
            var desglose = i.ConsumoTotalW + " W: " + i.potenciaNormalW + " W normal + " + i.extraTormentaW + " W due to the storm";
            StringAssert.Contains(desglose, i.textoAyuda);
            Assert.IsNotEmpty(i.pistaAsociada, "Falta la evidencia que aporta " + id);
        }

        [TestCase("explorador", 190, 650, 2)]
        [TestCase("titan", 360, 750, 4)]
        public void LosLanzadoresCoincidenConLaTabla(string id, int costo, int kg, int espacios)
        {
            var l = catalogo.Lanzador(id);
            Assert.AreEqual(costo, l.costoCr);
            Assert.AreEqual(kg, l.capacidadKg);
            Assert.AreEqual(espacios, l.espacios);
        }

        [TestCase("polar", 120)]
        [TestCase("media", 90)]
        [TestCase("eliptica", 140)]
        public void LasOrbitasCoincidenConLaTabla(string id, int costo)
        {
            Assert.AreEqual(costo, catalogo.Orbita(id).costoCr);
        }

        [Test]
        public void LosRecursosGlobalesCoincidenConLaTabla()
        {
            var m = catalogo.mision;
            Assert.AreEqual(1250, m.presupuestoCr);
            Assert.AreEqual(700, m.potenciaDisponibleW);
            Assert.AreEqual(350, m.masaBaseKg);
            Assert.AreEqual(100, m.costoBaseCr);
            Assert.AreEqual(100, m.consumoBaseW);
            Assert.IsNotEmpty(m.objetivo);
            Assert.IsNotEmpty(m.narrativa);
        }

        [Test]
        public void CadaInstrumentoTieneTransmisionDePresenteYDeAusente()
        {
            // DT3: TX-0x es la de presente y TX-Ax la de ausente. TX-00 es de la misión.
            Assert.AreEqual(11, catalogo.transmisiones.Count);
            Assert.IsNotNull(catalogo.Transmision("TX-00"));
            foreach (var i in catalogo.instrumentos)
            {
                var deEste = catalogo.transmisiones.Where(t => t.instrumentoId == i.id).ToList();
                Assert.AreEqual(1, deEste.Count(t => !t.id.StartsWith("TX-A")), "presente de " + i.id);
                Assert.AreEqual(1, deEste.Count(t => t.id.StartsWith("TX-A")), "ausente de " + i.id);
                Assert.IsTrue(deEste.All(t => t.tipo == deEste[0].tipo), "tipo distinto en las de " + i.id);
            }
            var principales = catalogo.transmisiones.Where(t => t.tipo == TipoTransmision.Principal)
                .Select(t => t.instrumentoId).Distinct().ToArray();
            CollectionAssert.AreEquivalent(new[] { "termica", "radar" }, principales);
        }

        [Test]
        public void LaCategoriaDeResultadoSoloAdmiteVictoriaParcialOFracaso()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Victoria", "Parcial", "Fracaso" },
                Enum.GetNames(typeof(CategoriaResultado)));
        }

        [Test]
        public void NoSeGuardaNingunCampoSemaforo()
        {
            var tipos = typeof(EstadoJuego).Assembly.GetTypes();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (var tipo in tipos)
            {
                foreach (var miembro in tipo.GetFields(flags).Cast<MemberInfo>().Concat(tipo.GetProperties(flags)))
                    StringAssert.DoesNotContain("semaforo", miembro.Name.ToLowerInvariant(),
                        tipo.Name + "." + miembro.Name + " guarda el semáforo, que debe calcularse");
            }
            foreach (var nombre in Enum.GetNames(typeof(CategoriaResultado)))
                StringAssert.DoesNotContain("semaforo", nombre.ToLowerInvariant());
        }
    }
}
