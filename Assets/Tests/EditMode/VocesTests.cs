using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 6, voces: suenan en el idioma del juego y no se enciman (las transmisiones van en fila).
    /// </summary>
    public class VocesTests
    {
        ArteDatos arte;
        string guardado;

        [SetUp]
        public void Preparar()
        {
            arte = AssetDatabase.LoadAssetAtPath<ArteDatos>("Assets/Arte/Arte.asset");
            guardado = PlayerPrefs.HasKey(Idioma.Clave) ? PlayerPrefs.GetString(Idioma.Clave) : null;
        }

        [TearDown]
        public void Restaurar()
        {
            if (guardado == null) PlayerPrefs.DeleteKey(Idioma.Clave); else PlayerPrefs.SetString(Idioma.Clave, guardado);
            Idioma.Recargar();
            foreach (var r in Object.FindObjectsByType<ReproductorUI>(FindObjectsSortMode.None)) Object.DestroyImmediate(r.gameObject);
        }

        static readonly ArteDatos.Efecto[] Control =
            { ArteDatos.Efecto.VozEncendido, ArteDatos.Efecto.VozDespegue, ArteDatos.Efecto.VozSeparacion, ArteDatos.Efecto.VozOrbita };

        [Test]
        public void CadaVozTieneGrabacionEnLosDosIdiomas()
        {
            Assert.IsNotNull(arte.vozEncendido); Assert.IsNotNull(arte.vozEncendidoEs);
            Assert.IsNotNull(arte.vozDespegue); Assert.IsNotNull(arte.vozDespegueEs);
            Assert.IsNotNull(arte.vozSeparacion); Assert.IsNotNull(arte.vozSeparacionEs);
            Assert.IsNotNull(arte.vozOrbita); Assert.IsNotNull(arte.vozOrbitaEs);
            CollectionAssert.AreEqual(new[] { "TX-00", "TX-01", "TX-02", "TX-03", "TX-04", "TX-05" },
                arte.voces.Select(v => v.transmisionId).ToArray());
            foreach (var v in arte.voces)
            {
                Assert.IsNotNull(v.clip, v.transmisionId + " en inglés");
                Assert.IsNotNull(v.clipEs, v.transmisionId + " en español");
                Assert.AreNotSame(v.clip, v.clipEs, v.transmisionId);
                StringAssert.EndsWith("_en", v.clip.name);
                Assert.IsFalse(v.clipEs.name.EndsWith("_en"), v.transmisionId);
            }
        }

        [Test]
        public void LosAudiosDeLaVersion7EstanConectados()
        {
            // Los que faltaban del paquete de diseño: ambiente de la Configuración, transición, órbita, espectro y revelación.
            Assert.AreEqual("AMB_Configuracion_Sistemas", arte.Clip(ArteDatos.Ambiente.Configuracion).name);
            Assert.AreEqual("SFX_UI_Transition", arte.Clip(ArteDatos.Sonido.Transicion).name);
            Assert.AreEqual("SFX_Orbit_Select", arte.Clip(ArteDatos.Sonido.OrbitaElegir).name);
            Assert.AreEqual("SFX_Orbit_Confirm", arte.Clip(ArteDatos.Sonido.OrbitaConfirmar).name);
            Assert.AreEqual("SFX_Spectral_Switch", arte.Clip(ArteDatos.Efecto.ConmutacionEspectral).name);
            Assert.AreEqual("MUS_Revelacion_Breve", arte.Clip(ArteDatos.Efecto.Revelacion).name);
            Assert.IsNotNull(arte.Clip(ArteDatos.Sonido.Hover), "el sonido de pasar el cursor existe y ya se usa (BotonVivo)");
            // Los valores anteriores de los enumerados no cambian (se guardan en las escenas).
            Assert.AreEqual(9, (int)ArteDatos.Sonido.Senal);
            Assert.AreEqual(2, (int)ArteDatos.Ambiente.Orbital);
        }

        [Test]
        public void LasVocesSiguenAlIdiomaElegido()
        {
            Idioma.Espanol = false;
            foreach (var e in Control) StringAssert.EndsWith("_en", arte.Clip(e).name, e.ToString());
            StringAssert.EndsWith("_en", arte.VozDe("TX-01").name);

            Idioma.Espanol = true;
            foreach (var e in Control) Assert.IsFalse(arte.Clip(e).name.EndsWith("_en"), e + " en español");
            Assert.AreEqual("voz_TX-01", arte.VozDe("TX-01").name);
        }

        [Test]
        public void SiFaltaUnIdiomaSuenaElOtro()
        {
            var clip = AudioClip.Create("solo", 10, 1, 8000, false);
            Idioma.Espanol = true;
            Assert.AreSame(clip, ArteDatos.SegunIdioma(clip, null));
            Idioma.Espanol = false;
            Assert.AreSame(clip, ArteDatos.SegunIdioma(null, clip));
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void LasTransmisionesVanEnFilaSinCortarse()
        {
            var go = new GameObject("Reproductor", typeof(AudioSource));
            var reproductor = go.AddComponent<ReproductorUI>();

            reproductor.Encolar(arte.VozDe("TX-01"));
            reproductor.Encolar(arte.VozDe("TX-02"));
            reproductor.Encolar(null);   // sin grabación: no ocupa turno
            Assert.AreEqual(2, reproductor.VocesEnFila, "la segunda espera a que termine la primera; ninguna se corta");

            reproductor.CallarVoces();
            Assert.AreEqual(0, reproductor.VocesEnFila);
        }
    }
}
