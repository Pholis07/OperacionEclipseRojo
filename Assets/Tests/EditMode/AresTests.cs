using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>El modelo Ares 1 (B) de NASA 3D Resources como aspecto del cohete Explorador.</summary>
    public class AresTests
    {
        const string RutaFbx = "Assets/Arte/Modelos/NASA/ares1b.fbx";

        static Bounds Limites(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        [Test]
        public void ElModeloImportadoQuedaDerecho()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFbx));
            try
            {
                var b = Limites(go);
                Assert.Greater(b.size.y, b.size.x * 8f, "el cohete debe quedar de pie (Y es el eje largo)");
                Assert.Greater(b.size.y, b.size.z * 8f);
                Assert.AreEqual(0f, b.min.y, 0.05f, "la base en el suelo");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void ElExploradorEsElAresPartidoEnEtapasEnLaPlataformaYEnLaCinematica()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var cohetes = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Cohete_explorador").ToArray();
            Assert.AreEqual(2, cohetes.Length, "pestaña Lanzador y cinemática");
            foreach (var c in cohetes)
            {
                Assert.IsNull(c.Find("Bloque0"), "ya no es el Soyuz");
                var superior = c.Find("EtapaSuperior");
                Assert.IsNotNull(superior);
                foreach (var mitad in new[] { "CofiaFrente", "CofiaDetras" })
                    Assert.IsNotEmpty(superior.Find(mitad).GetComponentsInChildren<MeshRenderer>(true), mitad);
                // La primera etapa son las piezas que cuelgan del cohete y no de la etapa superior.
                var cuerpo = c.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("Ares_")).ToArray();
                Assert.IsNotEmpty(cuerpo.Where(r => !r.transform.IsChildOf(superior)), "primera etapa");
                Assert.IsNotEmpty(cuerpo.Where(r => r.transform.IsChildOf(superior)), "etapa superior");
                var b = cuerpo[0].bounds;
                foreach (var r in cuerpo) b.Encapsulate(r.bounds);
                Assert.AreEqual(7.5f, b.size.y, 0.4f, "alto del Explorador");
                Assert.Greater(b.size.y, b.size.x * 8f, "de pie");
                Assert.AreEqual(c.position.y, b.min.y, 0.1f, "apoyado en la plataforma");
                foreach (var r in cuerpo)
                    foreach (var m in r.sharedMaterials)
                        StringAssert.StartsWith("NASA_Cohete_Ares_", m.name, r.name);
            }
        }

        [Test]
        public void LaCinematicaSueltaLaPrimeraEtapaYAbreLaNariz()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var director = Object.FindObjectsByType<DirectorCinematica>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var so = new SerializedObject(director);
            Assert.AreEqual(0, so.FindProperty("propulsoresExplorador").arraySize, "el Ares no tiene propulsores laterales");
            var cofia = so.FindProperty("cofiaExplorador");
            Assert.AreEqual(2, cofia.arraySize);
            for (int i = 0; i < 2; i++) Assert.IsNotNull(cofia.GetArrayElementAtIndex(i).objectReferenceValue);
            Assert.AreEqual("EtapaSuperior", ((Transform)so.FindProperty("superiorExplorador").objectReferenceValue).name);
            Assert.Greater(so.FindProperty("llamasExplorador").arraySize, 0);
            Assert.Greater(so.FindProperty("llamaSuperiorExplorador").arraySize, 0);
        }

        [Test]
        public void LosCreditosDicenDondeSeUsaElAres()
        {
            var creditos = File.ReadAllText("CREDITOS.md");
            var fila = creditos.Split('\n').Single(l => l.StartsWith("| `ares1b.fbx`"));
            StringAssert.DoesNotContain("sin usar", fila);
            StringAssert.Contains("Explorador", fila);
            StringAssert.DoesNotContain("Explorador (inspirado en el Soyuz)", creditos);
        }
    }
}
