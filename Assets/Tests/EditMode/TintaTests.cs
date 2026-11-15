using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 8 (Eclipse Ink): cel shading selectivo. El shader Tinta va solo en el satélite y los instrumentos
    /// (piloto); los planetas, las órbitas, las partículas y los logos siguen con sus shaders.
    /// </summary>
    public class TintaTests
    {
        const string Tinta = "OperacionEclipseRojo/Tinta";

        static Material Mat(string nombre) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/" + nombre + ".mat");

        static IEnumerable<Material> Todos() =>
            AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materiales" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));

        [Test]
        public void ElShaderCompilaYConservaLosNombresDeUrpLit()
        {
            var shader = Shader.Find(Tinta);
            Assert.IsNotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "el shader Tinta tiene errores");
            var m = new Material(shader);
            foreach (var p in new[] { "_BaseMap", "_BaseColor", "_BumpMap", "_BumpScale", "_EmissionColor", "_Metallic", "_Smoothness", "_Cull", "_Cutoff" })
                Assert.IsTrue(m.HasProperty(p), p);
            Assert.AreEqual(2f, m.GetFloat("_Cull"), "por defecto, solo la cara de delante");
            Object.DestroyImmediate(m);
        }

        [TestCase("NASA_Sat_MRO_")]
        [TestCase("NASA_Sat_Chandra_")]
        [TestCase("CubeSat_")]
        [TestCase("Instrumento_")]
        [TestCase("Nave_Pla")]
        public void ElSateliteYLosInstrumentosLlevanTinta(string prefijo)
        {
            var mats = Todos().Where(m => m.name.StartsWith(prefijo)).ToList();
            Assert.IsNotEmpty(mats, prefijo);
            foreach (var m in mats) Assert.AreEqual(Tinta, m.shader.name, m.name);
        }

        [TestCase("Marte")]
        [TestCase("Marte_Polvo")]
        [TestCase("Atmosfera")]
        [TestCase("Tierra")]
        [TestCase("Sol_Mapa")]
        [TestCase("Linea_Orbita")]
        [TestCase("Marcador_Orbita")]
        [TestCase("Haz_Instrumento")]
        [TestCase("Holograma_Instrumento")]
        [TestCase("Particula_Nube")]
        [TestCase("Llama_Motor")]
        [TestCase("Bloque_Memoria")]
        [TestCase("Estrellas")]
        public void LosPlanetasLasOrbitasYLasParticulasNoLlevanTinta(string nombre)
        {
            var m = Mat(nombre);
            Assert.IsNotNull(m, nombre);
            Assert.AreNotEqual(Tinta, m.shader.name);
        }

        [Test]
        public void LosVidriosYLoQueEmiteLuzNoLlevanContorno()
        {
            foreach (var m in Todos().Where(x => x.shader.name == Tinta))
            {
                bool emite = m.GetColor("_EmissionColor").maxColorComponent > 0.01f;
                if (m.name.Contains("Vidrio") || emite) Assert.AreEqual(0f, m.GetFloat("_Contorno"), m.name);
                else Assert.That(m.GetFloat("_Contorno"), Is.InRange(0.5f, 2f), m.name + ": de 1 a 2 px a 1080p");
            }
        }

        [Test]
        public void LasTexturasSeConservanAlCambiarDeShader()
        {
            Assert.IsNotNull(Mat("Instrumento_OroTermico").GetTexture("_BaseMap"));
            Assert.IsNotNull(Mat("Instrumento_OroTermico").GetTexture("_BumpMap"));
            Assert.IsTrue(Mat("Instrumento_OroTermico").IsKeywordEnabled("_NORMALMAP"));
            Assert.IsNotNull(Mat("CubeSat_Celdas").GetTexture("_BaseMap"));
            Assert.IsTrue(Todos().Any(m => m.name.StartsWith("NASA_Sat_MRO_") && m.GetTexture("_BaseMap") != null));
        }

        [Test]
        public void LaNormalSuavizadaVaYVuelve()
        {
            foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.left,
                         new Vector3(1, 1, 1).normalized, new Vector3(-0.3f, 0.2f, -0.9f).normalized })
            {
                var uv = NormalesContorno.Codificar(d);
                Assert.That(uv.x, Is.InRange(0.049f, 0.951f));
                Assert.That(uv.y, Is.InRange(0.049f, 0.951f));
                Assert.Less(Vector3.Angle(d, NormalesContorno.Decodificar(uv)), 0.5f, d.ToString());
            }
        }

        [Test]
        public void LaEsquinaDeUnCuboApuntaEnDiagonal()
        {
            var cubo = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Arte/Mallas/Tinta_Cube.asset");
            Assert.IsNotNull(cubo, "copia del cubo con la normal suavizada");
            var uv = new List<Vector2>();
            cubo.GetUVs(NormalesContorno.Canal, uv);
            Assert.AreEqual(cubo.vertexCount, uv.Count);
            var v = cubo.vertices;
            for (int i = 0; i < v.Length; i++)
                Assert.Less(Vector3.Angle(v[i].normalized, NormalesContorno.Decodificar(uv[i])), 1f, "vértice " + i);
        }

        [TestCase("NASA/Sat_MRO")]
        [TestCase("NASA/Sat_Chandra")]
        [TestCase("Instrumentos/02_Radar_de_Subsuelo")]
        public void LosModelosTraenLaNormalSuavizada(string modelo)
        {
            var mallas = AssetDatabase.LoadAllAssetsAtPath("Assets/Arte/Modelos/" + modelo + ".obj").OfType<Mesh>().ToArray();
            Assert.IsNotEmpty(mallas);
            foreach (var m in mallas)
            {
                var uv = new List<Vector2>();
                m.GetUVs(NormalesContorno.Canal, uv);
                Assert.AreEqual(m.vertexCount, uv.Count, m.name);
                Assert.IsTrue(uv.All(p => p.x + p.y > 0.05f), m.name + ": (0, 0) queda reservado para «sin canal»");
            }
        }

        [Test]
        public void LasPiezasConContornoDeLaNaveUsanMallasPreparadas()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            int revisadas = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!r.sharedMaterials.Any(m => m != null && m.shader.name == Tinta && m.GetFloat("_Contorno") > 0f)) continue;
                var malla = r.GetComponent<MeshFilter>().sharedMesh;
                var uv = new List<Vector2>();
                malla.GetUVs(NormalesContorno.Canal, uv);
                Assert.AreEqual(malla.vertexCount, uv.Count, r.name + " / " + malla.name);
                revisadas++;
            }
            Assert.Greater(revisadas, 20);
        }

        [Test]
        public void LosContornosSeApaganDesdeLasPreferencias()
        {
            bool antes = Preferencias.ContornosTinta;
            try
            {
                Preferencias.ContornosTinta = false;
                ContornosTinta.Aplicar();
                Assert.AreEqual(1f, Shader.GetGlobalFloat("_TintaSinContorno"));
                Preferencias.ContornosTinta = true;
                ContornosTinta.Aplicar();
                Assert.AreEqual(0f, Shader.GetGlobalFloat("_TintaSinContorno"));
            }
            finally { Preferencias.ContornosTinta = antes; ContornosTinta.Aplicar(); }
        }
    }
}
