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

        [Test]
        public void SoloElEspectrometroYLaCamaraTienenModo()
        {
            Assert.IsTrue(ModoEscaneo.Tiene("espectrometro"));
            Assert.IsTrue(ModoEscaneo.Tiene("alta_res"));
            foreach (var id in new[] { "termica", "radar", "atmosferico" }) Assert.IsFalse(ModoEscaneo.Tiene(id), id);
            modo.Empezar("radar", true);
            Assert.IsFalse(modo.Visible);
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
            foreach (var instrumento in new[] { "espectrometro", "alta_res" })
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
        }

        [Test]
        public void ElEscaneoCompletoDuraLoDelStoryboard()
        {
            Assert.GreaterOrEqual(SecuenciaEscaneo.DuracionCompleta, 9f);
            Assert.LessOrEqual(SecuenciaEscaneo.DuracionCompleta, 11f);
        }
    }
}
