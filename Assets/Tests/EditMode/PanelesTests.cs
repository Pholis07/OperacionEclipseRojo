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
        public void LasAlasTienenBisagraYSePliegan([Values("mro", "magellan", "chandra", "llcd")] string id)
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
        public void ElAquariusQuedaRigidoYNoSeFinge()
        {
            // Su modelo no da dos alas separables: no lleva mecanismo de cuerpo (limitación anotada en el informe).
            Assert.IsNull(Cuerpo("aquarius").GetComponent<MecanismoInstrumento>());
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
