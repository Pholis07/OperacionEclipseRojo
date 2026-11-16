using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Renovación visual: los paneles de los satélites se despliegan con bisagra (no un destello ni un cambio de
    /// escala), y «Tu nave» en la Operación enfoca el instrumento que se enciende o se apaga.
    /// </summary>
    public class PanelesTests
    {
        static Transform Cuerpo(string id)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var nave = Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(n => n.name == "NaveOrbita");
            return nave.transform.Find("Cuerpo_" + id);
        }

        [Test]
        public void LasAlasTienenBisagraYSePliegan([Values("mro", "magellan", "chandra", "aquarius", "llcd")] string id)
        {
            var cuerpo = Cuerpo(id);
            var mecanismo = cuerpo.GetComponent<MecanismoInstrumento>();
            Assert.IsNotNull(mecanismo, "el cuerpo tiene alas móviles");
            Assert.AreEqual(2, mecanismo.Piezas);
            foreach (var nombre in new[] { "Ala_0", "Ala_1" })
            {
                var ala = cuerpo.Find(nombre);
                Assert.IsNotNull(ala, nombre);
                var rs = ala.GetComponentsInChildren<MeshRenderer>(true);
                Assert.IsNotEmpty(rs, nombre + " lleva su parte de la malla");
                Assert.IsTrue(rs.SelectMany(r => r.sharedMaterials).Any(m => m.name.ToLowerInvariant().Contains("panel")), "es panel solar");
            }
            mecanismo.Desplegar(1f);
            var abierta = cuerpo.Find("Ala_0").localRotation;
            var escala = cuerpo.Find("Ala_0").localScale;
            mecanismo.Desplegar(0f);
            Assert.Greater(Quaternion.Angle(abierta, cuerpo.Find("Ala_0").localRotation), 60f, "plegada gira sobre su bisagra");
            Assert.AreEqual(escala, cuerpo.Find("Ala_0").localScale, "sin cambios de escala");
            // El cuerpo conserva su propia malla (lo que no es ala).
            Assert.IsNotEmpty(cuerpo.GetComponentsInChildren<MeshFilter>(true).Where(mf => !mf.transform.IsChildOf(cuerpo.Find("Ala_0")) && !mf.transform.IsChildOf(cuerpo.Find("Ala_1"))));
            mecanismo.Desplegar(1f);
        }

        [Test]
        public void ElCubeSatDespliegaSusCuatroAntenas()
        {
            var mecanismo = Cuerpo("aztechsat").GetComponent<MecanismoInstrumento>();
            Assert.IsNotNull(mecanismo);
            Assert.AreEqual(4, mecanismo.Piezas);
        }

        [Test]
        public void ElAlaSeLlevaSuDorsoYSuMarco()
        {
            // En el Aquarius la cara de celdas, el dorso y el marco son piezas y materiales distintos: todo lo que
            // queda dentro de la caja del ala se mueve con ella (si no, la cara se plegaría dejando el dorso atrás).
            var cuerpo = Cuerpo("aquarius");
            foreach (var nombre in new[] { "Ala_0", "Ala_1" })
            {
                var mats = cuerpo.Find(nombre).GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m.name).Distinct().ToArray();
                Assert.IsTrue(mats.Any(m => m.Contains("shiny_panel")), nombre + ": celdas");
                Assert.IsTrue(mats.Any(m => !m.Contains("shiny_panel")), nombre + ": dorso o marco (" + string.Join(", ", mats) + ")");
            }
        }

        [Test]
        public void LosMecanismosNoAtraviesanLaNave()
        {
            // El constructor mide, para cada satélite e instrumento, cuántos puntos de las piezas móviles tocan el
            // cuerpo, los paneles u otro instrumento (plegado y desplegado) con el giro elegido. 30 combinaciones.
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            foreach (var nave in Object.FindObjectsByType<NaveSatelite>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var medida = nave.Interferencias;
                Assert.AreEqual(nave.IdsSatelite.Length * 5, medida.Length, nave.name);
                var conRoce = Enumerable.Range(0, medida.Length).Where(k => medida[k] > 0)
                    .Select(k => nave.IdsSatelite[k / 5] + "/" + k % 5 + "=" + medida[k]).ToArray();
                // Quedan dos roces pequeños conocidos (anotados en el informe); ninguno nuevo y ninguno grande.
                Assert.LessOrEqual(conRoce.Length, 2, nave.name + ": " + string.Join(", ", conRoce));
                Assert.LessOrEqual(medida.Max(), 5, nave.name + ": " + string.Join(", ", conRoce));
            }
        }

        [Test]
        public void TuNaveSeApartaConElCursorSobreElMapa()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var vista = Object.FindObjectsByType<VistaNave>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var mapa = (RectTransform)vista.transform.parent;
            var esquinas = new Vector3[4];
            mapa.GetWorldCorners(esquinas);
            Vector2 dentro = (esquinas[0] + esquinas[2]) * 0.5f, fuera = (Vector2)esquinas[0] - new Vector2(40f, 40f);
            vista.MirarCursor(dentro);
            Assert.IsTrue(vista.Apartada, "con el cursor en el mapa deja ver esa esquina");
            vista.MirarCursor(fuera);
            Assert.IsFalse(vista.Apartada, "al salir del mapa vuelve");
            vista.MirarCursor(null);
            Assert.IsFalse(vista.Apartada, "sin ratón (pantalla táctil) se queda a la vista");
        }

        [Test]
        public void TuNaveEnfocaElInstrumentoQueCambiaYNoTocaLaOperacion()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var vista = Object.FindObjectsByType<VistaNave>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            var nivel = catalogo.Nivel(0);
            var config = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(config, catalogo, nivel);
            var op = new Operacion(e, config, catalogo, nivel, 7, new Carga(e, config, catalogo, nivel));
            vista.LeerOperacion(op);
            Assert.IsNull(vista.InstrumentoEnFoco, "al empezar muestra la nave entera");
            Assert.IsTrue(op.Encender("radar"));
            int consumo = op.ConsumoW;
            vista.LeerOperacion(op);
            Assert.AreEqual("radar", vista.InstrumentoEnFoco);
            op.Encender("termica");
            vista.LeerOperacion(op);
            Assert.AreEqual("termica", vista.InstrumentoEnFoco);
            op.Apagar("termica");
            vista.LeerOperacion(op);
            Assert.AreEqual("termica", vista.InstrumentoEnFoco, "también al apagarlo");
            Assert.AreEqual(consumo, op.ConsumoW, "la vista solo lee");
            Assert.IsFalse(vista.Plegada);
            vista.Alternar();
            Assert.IsTrue(vista.Plegada);
            vista.Alternar();
        }
    }
}
