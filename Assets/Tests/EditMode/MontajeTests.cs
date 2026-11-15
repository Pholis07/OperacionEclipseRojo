using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Renovación visual: el montaje físico de los instrumentos en el constructor. La presentación no cambia la
    /// configuración ni los recursos, no deja piezas repetidas y la inspección tiene topes y vuelve a su sitio.
    /// </summary>
    public class MontajeTests
    {
        static readonly string[] Ids = { "termica", "radar", "alta_res", "atmosferico", "espectrometro" };

        ConfiguracionUI ui;
        Transform nave;

        [SetUp]
        public void Abrir()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();
            nave = Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(n => n.name == "Nave").transform;
        }

        [Test]
        public void CadaInstrumentoTieneFijacionesIndicadorYMecanismoPropio([ValueSource(nameof(Ids))] string id)
        {
            var modulo = nave.Find("Modulo_" + id);
            Assert.IsNotNull(modulo);
            var montaje = modulo.GetComponent<Instalacion>();
            Assert.AreEqual(2, montaje.Fijaciones.Length, "dos fijaciones");
            Assert.IsTrue(montaje.Fijaciones.All(f => f != null && f.GetComponentInChildren<MeshRenderer>() != null));
            Assert.IsNotNull(montaje.Indicador, "indicador de conexión");
            Assert.IsNotNull(modulo.Find("Conector"));
            var mecanismo = modulo.GetComponent<MecanismoInstrumento>();
            Assert.GreaterOrEqual(mecanismo.Piezas, 2, "piezas móviles con pivote propio");
            // Plegado y desplegado son poses distintas de piezas reales (no un destello ni un cambio de escala del modelo).
            var equipo = modulo.Find("Equipo");
            var escala = equipo.localScale;
            mecanismo.Desplegar(0f);
            var plegado = modulo.GetComponentsInChildren<Transform>(true).Select(t => (t.localPosition, t.localRotation)).ToArray();
            mecanismo.Desplegar(1f);
            var abierto = modulo.GetComponentsInChildren<Transform>(true).Select(t => (t.localPosition, t.localRotation)).ToArray();
            int movidas = Enumerable.Range(0, plegado.Length).Count(i =>
                (plegado[i].localPosition - abierto[i].localPosition).magnitude > 0.01f || Quaternion.Angle(plegado[i].localRotation, abierto[i].localRotation) > 1f);
            Assert.GreaterOrEqual(movidas, 2, "al desplegar se mueven piezas");
            Assert.AreEqual(escala, equipo.localScale, "el modelo del instrumento no se escala para fingir el despliegue");
            mecanismo.Desplegar(0f);
        }

        [Test]
        public void LosCincoMecanismosSonDistintos()
        {
            // Cada instrumento se reconoce por lo que despliega: nombres de pivote distintos en cada uno.
            var firmas = Ids.Select(id => string.Join(",", nave.Find("Modulo_" + id).GetComponentsInChildren<Transform>(true)
                .Where(t => t.GetComponent<MeshRenderer>() == null && t.name != "Fijacion_0" && t.name != "Fijacion_1" && t.parent != nave)
                .Select(t => t.name).Distinct().OrderBy(n => n))).ToArray();
            Assert.AreEqual(Ids.Length, firmas.Distinct().Count(), string.Join(" | ", firmas));
        }

        [Test]
        public void EnElHangarVaPlegadaYEnOrbitaDesplegada()
        {
            var naves = Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsFalse(naves.Single(n => n.name == "Nave").Desplegada, "constructor: integración");
            Assert.IsTrue(naves.Single(n => n.name == "NaveOrbita").Desplegada, "órbita");
            EditorSceneManager.OpenScene("Assets/Escenas/Resultado.unity");
            Assert.IsTrue(Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single().Desplegada, "informe");
        }

        [Test]
        public void InstalarYRetirarDiezVecesNoDejaResiduosNiCambiaLosRecursos()
        {
            ui.ElegirLanzador("titan");
            ui.AlternarInstrumento("radar");
            var antes = ui.Recalcular();
            int hijos = nave.childCount;
            for (int i = 0; i < 10; i++)
            {
                ui.AlternarInstrumento("termica");
                Assert.IsTrue(nave.Find("Modulo_termica").gameObject.activeSelf, "montado en el mismo cuadro");
                ui.AlternarInstrumento("termica");
                Assert.IsFalse(nave.Find("Modulo_termica").gameObject.activeSelf, "retirado en el mismo cuadro");
            }
            var despues = ui.Recalcular();
            Assert.AreEqual(hijos, nave.childCount, "ni módulos ni copias de más");
            Assert.AreEqual(1, nave.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Modulo_termica"));
            Assert.AreEqual(antes.Calculo.MasaKg, despues.Calculo.MasaKg);
            Assert.AreEqual(antes.Calculo.CostoCr, despues.Calculo.CostoCr);
            Assert.AreEqual(antes.Calculo.PotenciaW, despues.Calculo.PotenciaW);
            CollectionAssert.AreEqual(new[] { "radar" }, EstadoJuego.Configuracion.InstrumentoIds);
        }

        [Test]
        public void ElSoporteVacioLlevaTapaCiega()
        {
            ui.AlternarInstrumento("radar");
            var aspecto = nave.GetComponent<NaveSatelite>();
            aspecto.Aplicar(aspecto.Actual);
            Assert.IsFalse(nave.Find("Tapa_radar").gameObject.activeSelf, "soporte ocupado");
            Assert.IsTrue(nave.Find("Tapa_termica").gameObject.activeSelf, "soporte vacío");
        }

        [Test]
        public void LaInspeccionTieneTopesYVuelveASuSitio()
        {
            var camara = Object.FindFirstObjectByType<CamaraConfiguracion>(FindObjectsInactive.Include);
            camara.Girar(999f, 999f);
            Assert.AreEqual(CamaraConfiguracion.TopeGiro, camara.GiroUsuario);
            Assert.AreEqual(CamaraConfiguracion.TopeInclinacion, camara.InclinacionUsuario);
            camara.Girar(-9999f, -9999f);
            Assert.AreEqual(-CamaraConfiguracion.TopeGiro, camara.GiroUsuario);
            camara.Acercar(999f);
            Assert.AreEqual(CamaraConfiguracion.ZoomCerca, camara.ZoomUsuario, 1e-4f);
            camara.Acercar(-999f);
            Assert.AreEqual(CamaraConfiguracion.ZoomLejos, camara.ZoomUsuario, 1e-4f);
            // Diez vueltas de girar, acercar y restablecer: siempre a la misma vista, sin deriva.
            for (int i = 0; i < 10; i++)
            {
                camara.Girar(37f * i, -11f * i);
                camara.Acercar(i - 5);
                camara.Restablecer();
                Assert.AreEqual(0f, camara.GiroUsuario);
                Assert.AreEqual(0f, camara.InclinacionUsuario);
                Assert.AreEqual(1f, camara.ZoomUsuario);
            }
        }

        [Test]
        public void ElFocoSoloExisteParaLoInstaladoYNoCambiaLaConfiguracion()
        {
            var camara = Object.FindFirstObjectByType<CamaraConfiguracion>(FindObjectsInactive.Include);
            var barra = Object.FindFirstObjectByType<InspeccionNave>(FindObjectsInactive.Include);
            ui.MostrarPestana(ConfiguracionUI.Pestana.Instrumentos);
            Assert.IsFalse(camara.Enfocar("radar"), "sin instalar no hay foco");
            ui.AlternarInstrumento("radar");
            barra.Actualizar();
            Assert.IsTrue(barra.transform.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Foco_radar").gameObject.activeSelf);
            Assert.IsFalse(barra.transform.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Foco_termica").gameObject.activeSelf);
            Assert.IsTrue(camara.Enfocar("radar"));
            Assert.AreEqual("Modulo_radar", camara.ModuloEnFoco.name);
            CollectionAssert.AreEqual(new[] { "radar" }, EstadoJuego.Configuracion.InstrumentoIds, "enfocar no instala ni quita");
            camara.Girar(5f, 0f);
            Assert.IsNull(camara.ModuloEnFoco, "un gesto del jugador suelta el foco");
        }
    }
}
