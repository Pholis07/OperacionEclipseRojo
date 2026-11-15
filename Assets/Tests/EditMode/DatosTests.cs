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

        // Versión 5: masa = real × 3 y potencia = real × 5 (THEMIS, SHARAD, HiRISE, MCS, CRISM); la tormenta ya no suma potencia.
        [TestCase("termica", 35, 70, 0, 70, 180)]
        [TestCase("radar", 45, 50, 0, 50, 260)]
        [TestCase("alta_res", 195, 300, 0, 300, 200)]
        [TestCase("atmosferico", 25, 55, 0, 55, 90)]
        [TestCase("espectrometro", 100, 225, 0, 225, 220)]
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
        public void LaAyudaDeCadaInstrumentoMuestraSuConsumoYElDatoDeLaNasa(string id)
        {
            var i = catalogo.Instrumento(id);
            var ayuda = PresentacionMision.Ayuda(i);
            StringAssert.Contains("Power:</b> " + i.ConsumoTotalW + " W", ayuda);
            StringAssert.Contains("NASA data:", ayuda);
            Assert.IsNotEmpty(i.datoNasaEs, "Falta el dato NASA en español de " + id);
            Assert.IsNotEmpty(i.fuenteNasa, "Falta la fuente de " + id);
            Assert.IsNotEmpty(i.pistaAsociada, "Falta la evidencia que aporta " + id);
        }

        [Test]
        public void CadaOrbitaLanzadorYSateliteTieneSuDatoNasaEnLosDosIdiomas()
        {
            foreach (var o in catalogo.orbitas) Comprobar(o.id, o.datoNasa, o.datoNasaEs, o.fuenteNasa);
            foreach (var l in catalogo.lanzadores) Comprobar(l.id, l.datoNasa, l.datoNasaEs, l.fuenteNasa);
            foreach (var s in catalogo.satelites) Comprobar(s.id, s.datoNasa, s.datoNasaEs, s.fuenteNasa);
            foreach (var i in catalogo.instrumentos) Comprobar(i.id, i.datoNasa, i.datoNasaEs, i.fuenteNasa);
        }

        static void Comprobar(string id, string en, string es, string fuente)
        {
            Assert.IsNotEmpty(en, "falta el dato NASA en inglés de " + id);
            Assert.IsNotEmpty(es, "falta el dato NASA en español de " + id);
            Assert.IsNotEmpty(fuente, "falta la fuente de " + id);
            Assert.AreNotEqual(en, es, "el dato NASA de " + id + " no está traducido");
        }

        [TestCase("explorador", 190, 500, 2)]
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
