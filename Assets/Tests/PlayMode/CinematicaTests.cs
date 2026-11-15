using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceMission.PlayTests
{
    /// <summary>
    /// Versión 6: la cinemática del lanzamiento entera, sin saltar, como la ve el jugador. Nunca suenan dos voces a
    /// la vez (control de misión y transmisiones) y la primera transmisión solo empieza cuando termina la cinemática.
    /// Si el equipo no tiene salida de audio (algunos modos batch), las fuentes no llegan a sonar y la prueba se
    /// marca como no concluyente en vez de pasar sin comprobar nada.
    /// </summary>
    public class CinematicaTests
    {
        const float TopeSegundos = 150f;
        string idiomaGuardado;

        [SetUp]
        public void Preparar()
        {
            idiomaGuardado = PlayerPrefs.HasKey(Idioma.Clave) ? PlayerPrefs.GetString(Idioma.Clave) : null;
            EstadoJuego.Reiniciar();
        }

        [TearDown]
        public void Restaurar()
        {
            if (idiomaGuardado == null) PlayerPrefs.DeleteKey(Idioma.Clave); else PlayerPrefs.SetString(Idioma.Clave, idiomaGuardado);
            Idioma.Recargar();
        }

        static IEnumerator Lanzar(string lanzador)
        {
            SceneManager.LoadScene("Mision");
            int cuadros = 0;
            while (SceneManager.GetActiveScene().name != "Mision" && cuadros++ < 600) yield return null;
            yield return null;
            var mision = Object.FindFirstObjectByType<MisionUI>();
            mision.MostrarConfiguracion();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>();
            ui.ElegirLanzador(lanzador);
            ui.AlternarInstrumento("termica");
            ui.AlternarInstrumento("radar");
            Assert.IsTrue(ui.ElegirOrbita("media"));
            yield return null;
            mision.MostrarRevision(ui.Recalcular());
            yield return null;
            GameObject.Find("BotonLanzar").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            GameObject.Find("BotonLanzarCarga").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaCinematicaEnteraNoEnciamaVoces([Values("explorador", "titan")] string lanzador)
        {
            bool espanol = lanzador == "explorador";   // una en cada idioma
            Idioma.Espanol = espanol;
            Preferencias.ReducirMovimiento = false;
            yield return Lanzar(lanzador);
            var director = Object.FindFirstObjectByType<DirectorCinematica>();
            Assert.IsTrue(director.Reproduciendo, "la cinemática empieza al lanzar");

            int maxVoces = 0, cuadrosConVoz = 0;
            float inicio = Time.realtimeSinceStartup;
            while (director.Reproduciendo && Time.realtimeSinceStartup - inicio < TopeSegundos)
            {
                int voces = Atenuacion.VocesSonando;
                maxVoces = Mathf.Max(maxVoces, voces);
                if (voces > 0) cuadrosConVoz++;
                Assert.IsNull(ReproductorUI.VozSonando, "una transmisión sonó durante la cinemática");
                yield return null;
            }
            Assert.IsFalse(director.Reproduciendo, "la cinemática no terminó en " + TopeSegundos + " s");
            float duracion = Time.realtimeSinceStartup - inicio;
            Debug.Log("CINEMATICA " + lanzador + " (" + (espanol ? "es" : "en") + "): " + duracion.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s");
            Assert.That(duracion, Is.InRange(38f, 52f), "versión 8: las 10 fases a su tiempo original (unos 45 s; el video de referencia dura 49)");
            if (cuadrosConVoz == 0) Assert.Inconclusive("Sin salida de audio: no sonó ninguna voz.");
            Assert.LessOrEqual(maxVoces, 1, "dos voces sonaron a la vez");
            Assert.IsFalse(director.Hablando, "control de misión siguió hablando después de la cinemática");

            // Después llega la transmisión inicial, en el idioma del juego.
            float espera = Time.realtimeSinceStartup;
            while (ReproductorUI.VozSonando == null && Time.realtimeSinceStartup - espera < 3f) yield return null;
            var voz = ReproductorUI.VozSonando;
            Assert.IsNotNull(voz, "la transmisión inicial suena al terminar la cinemática");
            Assert.AreEqual(!espanol, voz.name.EndsWith("_en"), "la voz va en el idioma del juego: " + voz.name);
            Assert.LessOrEqual(Atenuacion.VocesSonando, 1);
        }

        /// <summary>
        /// Renovación visual: la cinemática termina con los seis lanzadores (a tiempo acelerado) y deja todo como
        /// estaba: cohete y brazos en su sitio y el satélite a su escala. Un error en el registro hace fallar la prueba.
        /// </summary>
        [UnityTest]
        public IEnumerator LaCinematicaTerminaConCadaLanzador(
            [Values("pionero", "explorador", "horizonte", "titan", "goliat", "coloso")] string lanzador)
        {
            Preferencias.ReducirMovimiento = false;
            SceneManager.LoadScene("Mision");
            int cuadros = 0;
            while (SceneManager.GetActiveScene().name != "Mision" && cuadros++ < 600) yield return null;
            yield return null;
            EstadoJuego.Configuracion = new Configuracion("media", lanzador, new[] { "radar" });
            var director = Object.FindFirstObjectByType<DirectorCinematica>();
            var escenario = Object.FindFirstObjectByType<EscenarioOrbital>(FindObjectsInactive.Include);
            var escala = escenario.Satelite.localScale;
            var brazos = Object.FindObjectsByType<BrazoTorre>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(3, brazos.Length);
            var cohetes = Object.FindObjectsByType<PerfilCohete>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var poses = cohetes.Select(c => (c.transform, c.transform.localPosition, c.transform.localRotation)).ToArray();
            int finales = 0;
            director.AlTerminar += () => finales++;
            Time.timeScale = 6f;
            try
            {
                director.Reproducir();
                Assert.IsTrue(director.Reproduciendo);
                float inicio = Time.realtimeSinceStartup;
                while (director.Reproduciendo && Time.realtimeSinceStartup - inicio < 60f) yield return null;
                Assert.IsFalse(director.Reproduciendo, "la cinemática no terminó");
            }
            finally { Time.timeScale = 1f; }
            yield return null;
            Assert.AreEqual(1, finales, "el final se avisa una sola vez");
            Assert.AreEqual(escala, escenario.Satelite.localScale, "el satélite vuelve a su escala");
            foreach (var b in brazos) Assert.Less(Quaternion.Angle(Quaternion.identity, b.transform.localRotation), 0.1f, "los brazos vuelven a su sitio");
            foreach (var (t, p, r) in poses)
            {
                Assert.Less((t.localPosition - p).magnitude, 1e-3f, t.name + " vuelve a su posición");
                Assert.Less(Quaternion.Angle(t.localRotation, r), 0.1f, t.name + " vuelve a su giro");
            }
        }

        [UnityTest]
        public IEnumerator SaltarLaCinematicaCallaAControlDeMision()
        {
            Preferencias.ReducirMovimiento = false;
            yield return Lanzar("titan");
            var director = Object.FindFirstObjectByType<DirectorCinematica>();
            float inicio = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - inicio < 7f) yield return null;   // en plena toma con voz
            director.Omitir();
            yield return null;
            Assert.IsFalse(director.Reproduciendo);
            Assert.IsFalse(director.Hablando, "saltar corta la voz y vacía la fila");
            Assert.LessOrEqual(Atenuacion.VocesSonando, 1, "como mucho, la transmisión inicial");
        }
    }
}
