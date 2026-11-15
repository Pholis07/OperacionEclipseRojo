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
            foreach (var id in new[] { "pionero", "explorador", "mro", "termica", "radar", "media", "polar" })
                Assert.IsTrue(Progreso.Desbloqueado(id), id);
            foreach (var id in new[] { "horizonte", "titan", "goliat", "coloso", "chandra", "llcd", "espectrometro", "eliptica", "areoestacionaria" })
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
            CollectionAssert.AreEquivalent(new[] { "horizonte", "titan", "magellan", "chandra", "aquarius", "alta_res", "atmosferico", "eliptica" },
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
            Assert.AreEqual(6, catalogo.lanzadores.Count);
            Assert.IsTrue(catalogo.lanzadores.All(l => l == c || (l.capacidadKg < c.capacidadKg && l.costoCr < c.costoCr && l.masaInyectadaKg < c.masaInyectadaKg)));
            // En la pestaña van del más barato al más caro, y cuanto más caro, más capaz.
            for (int i = 1; i < catalogo.lanzadores.Count; i++)
            {
                var a = catalogo.lanzadores[i - 1]; var b = catalogo.lanzadores[i];
                Assert.Less(a.costoCr, b.costoCr, b.id);
                Assert.Less(a.capacidadKg, b.capacidadKg, b.id);
                Assert.LessOrEqual(a.espacios, b.espacios, b.id);
            }
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
            CollectionAssert.AreEquivalent(new[] { "Bloqueado_horizonte", "Bloqueado_titan", "Bloqueado_goliat", "Bloqueado_coloso" }, bloqueados);
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

        [TestCase("pionero", "JupiterC_")]
        [TestCase("horizonte", "AresA_")]
        [TestCase("goliat", "Goliat_")]
        [TestCase("coloso", "SaturnV_")]
        public void CadaLanzadorNuevoEstaEnLaPlataformaYEnLaCinematica(string id, string prefijo)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var cohetes = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Cohete_" + id).ToArray();
            Assert.AreEqual(2, cohetes.Length, "pestaña Lanzador y cinemática");
            foreach (var c in cohetes)
            {
                var cuerpo = c.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith(prefijo)).ToArray();
                Assert.IsNotEmpty(cuerpo);
                var b = cuerpo[0].bounds; foreach (var r in cuerpo) b.Encapsulate(r.bounds);
                Assert.AreEqual(PlataformaCohetes.Alto(id), b.size.y, 0.6f, "alto");
                Assert.Greater(b.size.y, b.size.x * 4f, "de pie");
                Assert.AreEqual(c.position.y, b.min.y, 0.1f, "apoyado en la plataforma");
                Assert.IsNotEmpty(c.Find("EtapaSuperior/CofiaFrente").GetComponentsInChildren<MeshRenderer>(true));
                foreach (var r in cuerpo) foreach (var m in r.sharedMaterials) Assert.IsNull(m.GetTexture("_BaseMap"), "sin texturas con marcas: " + m.name);
            }
            var director = new SerializedObject(Object.FindObjectsByType<DirectorCinematica>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single());
            var lista = director.FindProperty("cohetesExtra");
            SerializedProperty e = null;
            for (int i = 0; i < lista.arraySize; i++) if (lista.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id) e = lista.GetArrayElementAtIndex(i);
            Assert.IsNotNull(e, "la cinemática conoce el cohete");
            Assert.IsNotNull(e.FindPropertyRelative("cohete").objectReferenceValue);
            Assert.AreEqual(2, e.FindPropertyRelative("cofia").arraySize);
            Assert.Greater(e.FindPropertyRelative("llamas").arraySize, 0);
        }

        [Test]
        public void ElPioneroLlegaConUnInstrumentoYSoloDaParcial()
        {
            var e = Evaluador.Evaluar(new Configuracion("media", "pionero", new[] { "radar" }), catalogo, catalogo.Nivel(0));
            Assert.IsTrue(e.SePuedeLanzar);
            Assert.AreEqual(CategoriaResultado.Parcial, e.Categoria, "con un solo espacio no caben térmica y radar");
        }
    }
}
