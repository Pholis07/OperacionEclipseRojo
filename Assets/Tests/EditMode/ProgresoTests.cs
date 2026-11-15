using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 9: lo que se desbloquea al ganar niveles, y el tercer lanzador (Coloso, Saturn V).</summary>
    public class ProgresoTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            Progreso.Reiniciar();
        }

        [TearDown]
        public void Restaurar() => Progreso.DesbloquearTodo();   // ProgresoAbierto deja el original al final

        [Test]
        public void AlEmpezarSoloEstaLoBasicoYAlcanzaParaGanar()
        {
            Assert.AreEqual(0, Progreso.NivelesGanados);
            foreach (var id in new[] { "explorador", "mro", "termica", "radar", "media", "polar" })
                Assert.IsTrue(Progreso.Desbloqueado(id), id);
            foreach (var id in new[] { "titan", "coloso", "chandra", "llcd", "espectrometro", "eliptica", "areoestacionaria" })
                Assert.IsFalse(Progreso.Desbloqueado(id), id);
            Assert.IsTrue(Progreso.NivelDisponible(0));
            Assert.IsFalse(Progreso.NivelDisponible(1));
            // Lo básico gana el nivel 1.
            var e = Evaluador.Evaluar(new Configuracion("media", "explorador", new[] { "termica", "radar" }), catalogo, catalogo.Nivel(0));
            Assert.AreEqual(EstadoSemaforo.Verde, e.Estado);
        }

        [Test]
        public void CadaVictoriaAbreElSiguienteNivelYSuEquipo()
        {
            CollectionAssert.AreEquivalent(new[] { "titan", "magellan", "chandra", "aquarius", "alta_res", "atmosferico", "eliptica" },
                Progreso.RegistrarVictoria(0));
            Assert.IsTrue(Progreso.NivelDisponible(1));
            Assert.IsFalse(Progreso.NivelDisponible(2));
            CollectionAssert.IsEmpty(Progreso.RegistrarVictoria(0), "volver a ganar el nivel 1 no da nada más");
            CollectionAssert.IsEmpty(Progreso.RegistrarVictoria(2), "un nivel que aún no toca no cuenta");
            CollectionAssert.Contains(Progreso.RegistrarVictoria(1), "coloso");
            Assert.IsTrue(Progreso.NivelDisponible(2));
            Assert.IsTrue(catalogo.lanzadores.All(l => Progreso.Desbloqueado(l.id)));
            Assert.IsTrue(catalogo.satelites.All(s => Progreso.Desbloqueado(s.id)));
            Assert.IsTrue(catalogo.orbitas.All(o => Progreso.Desbloqueado(o.id)));
            Assert.IsTrue(catalogo.instrumentos.All(i => Progreso.Desbloqueado(i.id)));
        }

        [Test]
        public void ElColosoEsElLanzadorMasCapaz()
        {
            var c = catalogo.Lanzador("coloso");
            Assert.IsNotNull(c);
            Assert.AreEqual(3, catalogo.lanzadores.Count);
            Assert.IsTrue(catalogo.lanzadores.All(l => l == c || (l.capacidadKg < c.capacidadKg && l.costoCr < c.costoCr && l.masaInyectadaKg < c.masaInyectadaKg)));
            // Con todos los instrumentos cabe en masa y espacios; la potencia (800 de 700 W) sigue mandando.
            var e = Evaluador.Evaluar(new Configuracion("media", "coloso", catalogo.instrumentos.Select(i => i.id)), catalogo);
            Assert.AreEqual(EstadoSemaforo.Rojo, e.Estado);
            CollectionAssert.AreEquivalent(new[] { Limite.Potencia, Limite.Costo }, e.Calculo.LimitesSuperados);
        }

        [Test]
        public void EnLaConfiguracionLoBloqueadoSaleConCandadoYNoSeElige()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            var mision = Object.FindFirstObjectByType<MisionUI>(FindObjectsInactive.Include);
            mision.MostrarConfiguracion();
            var ui = Object.FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            ui.Inicializar();
            ui.MostrarPestana(ConfiguracionUI.Pestana.Lanzador);
            var bloqueados = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b.name.StartsWith("Bloqueado_")).Select(b => b.name).ToList();
            CollectionAssert.AreEquivalent(new[] { "Bloqueado_titan", "Bloqueado_coloso" }, bloqueados);
            var candado = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == "Bloqueado_titan");
            StringAssert.Contains("Win level 1", candado.GetComponentInChildren<TMP_Text>().text);
            candado.onClick.Invoke();
            Assert.IsTrue(string.IsNullOrEmpty(EstadoJuego.Configuracion.LanzadorId), "pulsar el candado no lo elige");
            StringAssert.Contains("Locked", ui.TextoAyuda);
        }

        [Test]
        public void ElBriefingSoloDejaElegirLosNivelesAbiertos()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();   // nivel 2 por defecto, todavía cerrado
            var mision = Object.FindFirstObjectByType<MisionUI>(FindObjectsInactive.Include);
            mision.LlenarBriefing();
            Assert.AreEqual(0, EstadoJuego.NivelIndice, "pasa al último nivel abierto");
            mision.ElegirNivel(2);
            Assert.AreEqual(0, EstadoJuego.NivelIndice);
        }

        [Test]
        public void ElColosoEstaEnLaPlataformaYEnLaCinematica()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var cohetes = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Cohete_coloso").ToArray();
            Assert.AreEqual(2, cohetes.Length, "pestaña Lanzador y cinemática");
            foreach (var c in cohetes)
            {
                var cuerpo = c.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("SaturnV_")).ToArray();
                Assert.IsNotEmpty(cuerpo);
                var b = cuerpo[0].bounds; foreach (var r in cuerpo) b.Encapsulate(r.bounds);
                Assert.AreEqual(12.5f, b.size.y, 0.6f, "alto del Coloso");
                Assert.Greater(b.size.y, b.size.x * 6f, "de pie");
                Assert.IsNotEmpty(c.Find("EtapaSuperior/CofiaFrente").GetComponentsInChildren<MeshRenderer>(true));
                foreach (var r in cuerpo) foreach (var m in r.sharedMaterials) Assert.IsNull(m.GetTexture("_BaseMap"), "sin las texturas con banderas: " + m.name);
            }
            var director = new SerializedObject(Object.FindObjectsByType<DirectorCinematica>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single());
            Assert.IsNotNull(director.FindProperty("coheteColoso").objectReferenceValue);
            Assert.AreEqual(2, director.FindProperty("cofiaColoso").arraySize);
            Assert.Greater(director.FindProperty("llamasColoso").arraySize, 0);
        }
    }
}
