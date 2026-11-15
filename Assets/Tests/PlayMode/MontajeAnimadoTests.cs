using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceMission.PlayTests
{
    /// <summary>
    /// Renovación visual: el montaje y la retirada animados, en juego. La animación termina en la pose final, la
    /// copia que se retira desaparece y pulsar deprisa no deja dos piezas ni cobra dos veces.
    /// </summary>
    public class MontajeAnimadoTests
    {
        ConfiguracionUI ui;
        Transform nave;

        IEnumerator Abrir()
        {
            EstadoJuego.Reiniciar();
            Preferencias.ReducirMovimiento = false;
            SceneManager.LoadScene("Mision");
            int cuadros = 0;
            while (SceneManager.GetActiveScene().name != "Mision" && cuadros++ < 600) yield return null;
            yield return null;
            Object.FindFirstObjectByType<MisionUI>().MostrarConfiguracion();
            ui = Object.FindFirstObjectByType<ConfiguracionUI>();
            ui.ElegirLanzador("titan");
            ui.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
            nave = Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(n => n.name == "Nave").transform;
            yield return null;
        }

        static IEnumerator Esperar(float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (Time.realtimeSinceStartup < fin) yield return null;
        }

        [UnityTest]
        public IEnumerator ElMontajeLlegaDeLadoYTerminaFijadoYConectado()
        {
            yield return Abrir();
            ui.AlternarInstrumento("radar");
            var modulo = nave.Find("Modulo_radar");
            var montaje = modulo.GetComponent<Instalacion>();
            Assert.IsTrue(modulo.gameObject.activeSelf, "activo desde el primer cuadro");
            yield return null;
            Assert.IsTrue(montaje.Montando);
            var llegada = modulo.localPosition;
            Assert.IsFalse(montaje.Indicador.activeSelf, "aún sin conectar");
            Assert.Greater(Quaternion.Angle(Quaternion.identity, montaje.Fijaciones[0].localRotation), 30f, "fijaciones abiertas mientras llega");
            yield return Esperar(Instalacion.Duracion + 0.6f);
            Assert.IsFalse(montaje.Montando);
            Assert.Greater((llegada - modulo.localPosition).magnitude, 0.3f, "vino desde fuera");
            Assert.IsTrue(montaje.Indicador.activeSelf, "indicador de conexión encendido");
            Assert.Less(Quaternion.Angle(Quaternion.identity, montaje.Fijaciones[0].localRotation), 0.5f, "fijaciones cerradas");
        }

        [UnityTest]
        public IEnumerator LaRetiradaSeVeYNoDejaPiezasFantasma()
        {
            yield return Abrir();
            ui.AlternarInstrumento("termica");
            yield return Esperar(Instalacion.Duracion + 0.4f);
            var costo = ui.Recalcular().Calculo.CostoCr;
            ui.AlternarInstrumento("termica");
            Assert.IsFalse(nave.Find("Modulo_termica").gameObject.activeSelf, "retirado en el mismo cuadro");
            yield return null; yield return null;
            Assert.IsNotNull(nave.Find(Retirada.Prefijo + "Modulo_termica"), "se ve irse");
            Assert.Less(ui.Recalcular().Calculo.CostoCr, costo, "los recursos no esperan a la animación");
            yield return Esperar(Retirada.Duracion + 0.6f);
            Assert.IsNull(nave.Find(Retirada.Prefijo + "Modulo_termica"), "la copia desaparece");
            Assert.IsTrue(nave.Find("Tapa_termica").gameObject.activeSelf, "queda la tapa ciega");
        }

        [UnityTest]
        public IEnumerator PulsarDeprisaNoDuplicaNiCobraDeMas()
        {
            yield return Abrir();
            ui.AlternarInstrumento("radar");
            yield return Esperar(0.2f);
            ui.AlternarInstrumento("radar");   // se retira a mitad del montaje
            yield return null;
            ui.AlternarInstrumento("radar");   // y se vuelve a montar enseguida
            yield return null; yield return null;
            Assert.AreEqual(1, EstadoJuego.Configuracion.InstrumentoIds.Count(i => i == "radar"));
            Assert.IsNull(nave.Find(Retirada.Prefijo + "Modulo_radar"), "al volver a montar, la copia que se iba desaparece");
            yield return Esperar(Instalacion.Duracion + 0.8f);
            var modulos = nave.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("Modulo_radar")).ToArray();
            Assert.AreEqual(1, modulos.Length, "una sola pieza");
            var montaje = modulos[0].GetComponent<Instalacion>();
            Assert.IsFalse(montaje.Montando);
            Assert.IsTrue(montaje.Indicador.activeSelf);
            // Mismo resultado que montarlo una sola vez.
            var unaVez = Evaluador.Evaluar(new Configuracion(EstadoJuego.Configuracion.OrbitaId, "titan", new[] { "radar" })
                { SateliteId = EstadoJuego.Configuracion.SateliteId }, CatalogoDe());
            Assert.AreEqual(unaVez.Calculo.CostoCr, ui.Recalcular().Calculo.CostoCr);
            Assert.AreEqual(unaVez.Calculo.MasaKg, ui.Recalcular().Calculo.MasaKg);
        }

        static CatalogoDatos CatalogoDe() => Resources.FindObjectsOfTypeAll<CatalogoDatos>().First();

        [UnityTest]
        public IEnumerator ConMovimientoReducidoQuedaMontadoAlInstante()
        {
            yield return Abrir();
            Preferencias.ReducirMovimiento = true;
            try
            {
                ui.AlternarInstrumento("espectrometro");
                yield return null;
                var montaje = nave.Find("Modulo_espectrometro").GetComponent<Instalacion>();
                Assert.IsFalse(montaje.Montando);
                Assert.IsTrue(montaje.Indicador.activeSelf);
                ui.AlternarInstrumento("espectrometro");
                yield return null; yield return null;
                Assert.IsNull(nave.Find(Retirada.Prefijo + "Modulo_espectrometro"), "sin animación de retirada");
            }
            finally { Preferencias.ReducirMovimiento = false; }
        }
    }
}
