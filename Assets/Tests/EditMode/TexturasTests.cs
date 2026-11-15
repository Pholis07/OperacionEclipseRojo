using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 5, bloque 6: cielo de la NASA y texturas propias de las naves.</summary>
    public class TexturasTests
    {
        const string Texturas = "Assets/Arte/Texturas/";

        static Texture Mapa(string material, string propiedad = "_BaseMap") =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/" + material + ".mat").GetTexture(propiedad);

        static Texture2D Textura(string nombre) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + nombre + ".png") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + nombre + ".jpg");

        [Test]
        public void ElCieloDeLasCinematicasEsElMapaDeLaNasaA8k()
        {
            var cielo = Textura("Cielo_NASA");
            Assert.AreEqual(8192, cielo.width);
            Assert.AreEqual(4096, cielo.height, "proyección equirrectangular 2:1");
            Assert.IsFalse(cielo.mipmapCount > 1, "sin mipmaps, las estrellas se ven nítidas");
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var esferas = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.name == "CieloEstrellas").ToArray();
            Assert.AreEqual(2, esferas.Length, "espacio cercano y órbita de Marte");
            foreach (var r in esferas)   // abrir la escena recarga los assets: se compara la ruta
                Assert.AreEqual(Texturas + "Cielo_NASA.jpg", AssetDatabase.GetAssetPath(r.sharedMaterial.GetTexture("_BaseMap")));
        }

        [Test]
        public void LosFondosPlanosUsanElRecorteDeLaNasa()
        {
            ((TextureImporter)AssetImporter.GetAtPath(Texturas + "Estrellas.png")).GetSourceTextureWidthAndHeight(out int ancho, out int alto);
            Assert.AreEqual(new Vector2Int(2048, 1152), new Vector2Int(ancho, alto), "recorte 1:1 del mapa 8k");
            Assert.AreSame(Textura("Estrellas"), Mapa("Estrellas"));
        }

        [TestCase("Instrumentos/01_Camara_Termica_Infrarroja")]
        [TestCase("Instrumentos/04_Sensor_Atmosferico")]
        [TestCase("NASA/Cohete_SLS_Inferior")]
        public void LosModelosSinUvRecibenUvDeCaja(string modelo)
        {
            var mallas = AssetDatabase.LoadAllAssetsAtPath("Assets/Arte/Modelos/" + modelo + ".obj").OfType<Mesh>().ToArray();
            Assert.IsNotEmpty(mallas);
            foreach (var m in mallas) Assert.AreEqual(m.vertexCount, m.uv.Length, m.name);
        }

        [Test]
        public void CadaMaterialLlevaSuTexturaPropia()
        {
            Assert.AreSame(Textura("Lamina_Arrugada"), Mapa("Instrumento_OroTermico"));
            Assert.AreSame(Textura("Lamina_Arrugada_Normal"), Mapa("Instrumento_OroTermico", "_BumpMap"));
            Assert.AreSame(Textura("Celdas_Solares"), Mapa("Instrumento_PanelAzul"));
            Assert.AreSame(Textura("Metal_Cepillado"), Mapa("Instrumento_TitanioClaro"));
            Assert.AreSame(Textura("Celdas_Solares"), Mapa("CubeSat_Celdas"));
            Assert.AreSame(Textura("Metal_Cepillado"), Mapa("CubeSat_Aluminio"));
            Assert.AreSame(Textura("Espuma_Naranja"), Mapa("NASA_Cohete_SLS_naranja"));
            Assert.AreSame(Textura("Espuma_Naranja_Normal"), Mapa("NASA_Cohete_SLS_naranja", "_BumpMap"));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Instrumento_OroTermico.mat").IsKeywordEnabled("_NORMALMAP"));
        }

        [TestCase("Lamina_Arrugada_Normal")]
        [TestCase("Espuma_Naranja_Normal")]
        public void LasNormalesSeImportanComoNormales(string nombre)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(Texturas + nombre + ".png");
            Assert.AreEqual(TextureImporterType.NormalMap, ti.textureType);
            Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode);
        }

        [TestCase("Lamina_Arrugada")]
        [TestCase("Espuma_Naranja")]
        [TestCase("Metal_Cepillado")]
        public void LasTexturasSeRepitenSinCostura(string nombre)
        {
            // El salto entre la última y la primera columna (y fila) no es mayor que entre dos vecinas cualquiera.
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(Texturas + nombre + ".png"));
            float Salto(int x0, int y0, int x1, int y1)
            {
                float suma = 0;
                for (int i = 0; i < t.width; i++)
                {
                    var a = x0 < 0 ? t.GetPixel(i, y0) : t.GetPixel(x0, i);
                    var b = x1 < 0 ? t.GetPixel(i, y1) : t.GetPixel(x1, i);
                    suma += Mathf.Abs(a.grayscale - b.grayscale);
                }
                return suma / t.width;
            }
            int n = t.width - 1;
            Assert.LessOrEqual(Salto(n, 0, 0, 0), Salto(n / 2, 0, n / 2 + 1, 0) * 1.6f + 0.004f, "costura vertical");
            Assert.LessOrEqual(Salto(-1, n, -1, 0), Salto(-1, n / 2, -1, n / 2 + 1) * 1.6f + 0.004f, "costura horizontal");
            Object.DestroyImmediate(t);
        }

        [Test]
        public void LosCreditosNombranAlEstudioDeLaNasa()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity");
            var lista = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Lista");
            StringAssert.Contains("NASA/Goddard Space Flight Center Scientific Visualization Studio", lista.text);
            StringAssert.Contains("Gaia DR2: ESA/Gaia/DPAC", lista.text);
            StringAssert.Contains("NASA/Goddard Space Flight Center Scientific Visualization Studio", File.ReadAllText("CREDITOS.md"));
        }
    }
}
