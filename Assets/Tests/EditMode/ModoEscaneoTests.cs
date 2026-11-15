using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 7: modos de análisis del espectrómetro y de la cámara de alta resolución durante el escaneo.</summary>
    public class ModoEscaneoTests
    {
        GameObject raiz;
        ModoEscaneo modo;

        [SetUp]
        public void Preparar()
        {
            raiz = new GameObject("Vista", typeof(RectTransform));
            var plantilla = new GameObject("Plantilla", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            plantilla.transform.SetParent(raiz.transform, false);
            modo = new ModoEscaneo((RectTransform)raiz.transform, plantilla);
        }

        [TearDown]
        public void Limpiar() => Object.DestroyImmediate(raiz);

        static readonly string[] Cinco = { "termica", "radar", "alta_res", "atmosferico", "espectrometro" };

        // Renovación visual: los cinco instrumentos tienen su lectura (antes solo el espectrómetro y la cámara).
        [Test]
        public void LosCincoInstrumentosTienenSuLectura()
        {
            foreach (var id in Cinco)
            {
                Assert.IsTrue(ModoEscaneo.Tiene(id), id);
                modo.Empezar(id, true);
                Assert.IsTrue(modo.Visible, id);
                Assert.IsNotEmpty(modo.Titulo, id);
            }
            Assert.IsFalse(ModoEscaneo.Tiene("antena"), "lo que no es un instrumento científico no tiene lectura");
            modo.Empezar("antena", true);
            Assert.IsFalse(modo.Visible);
            Assert.AreEqual(5, System.Linq.Enumerable.Count(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(Cinco, ModoEscaneo.TituloDe))), "cinco lecturas distintas");
        }

        [Test]
        public void SinHallazgoElGraficoNoMuestraElRasgo([Values("termica", "radar", "atmosferico")] string id)
        {
            // El rasgo (mancha cálida, eco, polvo) solo aparece si la lectura real dio con la anomalía.
            float con = 0f, sin = 0f;
            for (int i = 10; i < 18; i++) { con += ModoEscaneo.Valor(id, i, true); sin += ModoEscaneo.Valor(id, i, false); }
            Assert.Greater(con, sin + 0.5f, "con hallazgo, el centro destaca");
            for (int i = 0; i < 4; i++) Assert.AreEqual(ModoEscaneo.Valor(id, i, true), ModoEscaneo.Valor(id, i, false), 0.02f, "lejos del centro no cambia");
        }

        [Test]
        public void UnaLecturaQueNoAportaLoDice()
        {
            modo.Empezar("radar", true);
            modo.Concluir("radar", true, false);
            StringAssert.Contains("adds no new evidence", modo.Nota);
            modo.Concluir("radar", false, false);
            StringAssert.StartsWith("No clear echo", modo.Nota);
        }

        [Test]
        public void ElEspectrometroPasaDeHiperespectralAMultiespectral()
        {
            modo.Empezar("espectrometro", true);
            Assert.IsTrue(modo.Visible);
            StringAssert.StartsWith("HYPERSPECTRAL", modo.Titulo);
            modo.Animar("espectrometro", 0.3f);
            StringAssert.StartsWith("HYPERSPECTRAL", modo.Titulo);
            modo.Animar("espectrometro", 0.8f);
            StringAssert.StartsWith("MULTISPECTRAL", modo.Titulo);
            Assert.AreEqual("", modo.Nota, "la nota llega al terminar");
            modo.Concluir("espectrometro", true);
            StringAssert.StartsWith("Signature consistent with", modo.Nota);
            modo.Ocultar();
            Assert.IsFalse(modo.Visible);
        }

        [Test]
        public void LasNotasSonPrudentes()
        {
            foreach (var instrumento in Cinco)
                foreach (var encontrada in new[] { true, false })
                {
                    var nota = ModoEscaneo.TextoNota(instrumento, encontrada).ToLowerInvariant();
                    Assert.IsNotNull(Idioma.EspanolDe(ModoEscaneo.TextoNota(instrumento, encontrada)), "sin traducción: " + nota);
                    StringAssert.DoesNotContain("life", nota);
                    StringAssert.DoesNotContain("liquid water", nota);
                    StringAssert.DoesNotContain("confirmed", nota);
                }
            // La cámara no da evidencia del subsuelo; el espectrómetro no afirma agua actual.
            StringAssert.Contains("does not show what lies below", ModoEscaneo.TextoNota("alta_res", true));
            StringAssert.Contains("does not prove there is water today", ModoEscaneo.TextoNota("espectrometro", true));
            // El radar no dice de qué es la capa; la térmica no da temperaturas; el sensor atmosférico da contexto.
            StringAssert.Contains("does not identify", ModoEscaneo.TextoNota("radar", true));
            StringAssert.DoesNotContain("ice", ModoEscaneo.TextoNota("radar", true).ToLowerInvariant());
            StringAssert.Contains("not temperatures", ModoEscaneo.TextoNota("termica", true));
            StringAssert.Contains("not a finding by itself", ModoEscaneo.TextoNota("atmosferico", true));
        }

        [Test]
        public void ElEscaneoCompletoDuraLoDelStoryboard()
        {
            Assert.GreaterOrEqual(SecuenciaEscaneo.DuracionCompleta, 9f);
            Assert.LessOrEqual(SecuenciaEscaneo.DuracionCompleta, 11f);
        }
    }
}
