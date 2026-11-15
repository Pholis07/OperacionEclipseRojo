using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Renovación visual: los seis cohetes sin defectos de modelo. Piezas repetidas, triángulos al revés, cortes sin
    /// tapa, colores perdidos, brazos de la torre que no ajustan y carga que no cabe en la cofia.
    /// </summary>
    public class CohetesTests
    {
        static readonly string[] Ids = { "pionero", "explorador", "horizonte", "titan", "goliat", "coloso" };

        static Transform[] Cohetes(string id)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == "Cohete_" + id).ToArray();
        }

        static bool EsPiel(Renderer r) =>
            r is MeshRenderer && r.sharedMaterial != null && (r.sharedMaterial.name.StartsWith("NASA_Cohete_") || r.sharedMaterial.name.StartsWith("Cohete_"));

        static MeshFilter[] Piel(Transform cohete) =>
            cohete.GetComponentsInChildren<MeshFilter>(true).Where(mf => mf.sharedMesh != null && EsPiel(mf.GetComponent<Renderer>())).ToArray();

        [Test]
        public void HayDosDeCadaUno([ValueSource(nameof(Ids))] string id) =>
            Assert.AreEqual(2, Cohetes(id).Length, "pestaña Lanzador y cinemática");

        [Test]
        public void NingunaPiezaEstaRepetidaEnElMismoSitio([ValueSource(nameof(Ids))] string id)
        {
            foreach (var cohete in Cohetes(id))
            {
                var vistas = new List<(int vertices, Bounds limites, string nombre)>();
                foreach (var mf in Piel(cohete))
                {
                    if (mf.name.StartsWith("SLS_Tapa_")) continue;   // las dos tapas del Titán comparten plano, mirando a lados opuestos
                    var b = mf.GetComponent<Renderer>().bounds;
                    var repetida = vistas.FirstOrDefault(v => v.vertices == mf.sharedMesh.vertexCount &&
                        (v.limites.center - b.center).magnitude < 1e-3f && (v.limites.size - b.size).magnitude < 1e-3f);
                    Assert.IsNull(repetida.nombre, mf.name + " ocupa el mismo sitio que " + repetida.nombre + " (parpadean)");
                    vistas.Add((mf.sharedMesh.vertexCount, b, mf.name));
                }
            }
        }

        /// <summary>Aristas que dos triángulos recorren en el mismo sentido: uno de los dos mira hacia dentro.</summary>
        static (int mal, int total) AristasMalOrientadas(Mesh malla)
        {
            var vs = malla.vertices;
            float paso = Mathf.Max(malla.bounds.size.x, malla.bounds.size.y, malla.bounds.size.z) * 1e-4f;
            var ids = new Dictionary<Vector3Int, int>();
            var w = new int[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                var q = Vector3Int.RoundToInt(vs[i] / paso);
                if (!ids.TryGetValue(q, out var id)) ids[q] = id = ids.Count;
                w[i] = id;
            }
            var aristas = new Dictionary<(int, int), int>();
            int mal = 0;
            var t = malla.triangles;
            for (int k = 0; k < t.Length; k += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = w[t[k + e]], b = w[t[k + (e + 1) % 3]];
                    if (a == b) continue;
                    var clave = a < b ? (a, b) : (b, a);
                    int bit = a < b ? 1 : 2;
                    aristas.TryGetValue(clave, out var visto);
                    if ((visto & bit) != 0 && (visto & 4) == 0) { mal++; visto |= 4; }
                    aristas[clave] = visto | bit;
                }
            return (mal, aristas.Count);
        }

        [Test]
        public void LosTriangulosMiranHaciaFuera([ValueSource(nameof(Ids))] string id)
        {
            foreach (var mf in Piel(Cohetes(id)[0]))
            {
                if (mf.name.StartsWith("SLS_Tapa_")) continue;   // abanicos planos: no son una piel cerrada
                var (mal, total) = AristasMalOrientadas(mf.sharedMesh);
                // Es una medida aproximada: al soldar por posición, dos piezas distintas que se tocan comparten
                // aristas sin que ningún triángulo esté al revés (SLS: 3 %; piezas pequeñas del pie del Jupiter-C,
                // que traen caras dobles: 13 %). El cuerpo del Jupiter-C llegaba con un 35 %: eso es lo que se vigila.
                Assert.Less(mal, Mathf.Max(8, total / 5), mf.name + ": " + mal + " de " + total + " aristas con triángulos al revés (se ve el interior a trozos)");
            }
        }

        [Test]
        public void ElCorteEntreEtapasTieneTapa([ValueSource(nameof(Ids))] string id)
        {
            foreach (var cohete in Cohetes(id))
            {
                if (id == "titan")
                {
                    Assert.IsNotNull(cohete.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SLS_Tapa_Nucleo"), "boca del núcleo");
                    Assert.IsNotNull(cohete.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "SLS_Tapa_Superior"), "base de la etapa superior");
                    continue;
                }
                var superior = cohete.Find("EtapaSuperior");
                var etapa1 = Piel(cohete).Single(mf => mf.name.EndsWith("_Etapa1"));
                var etapa2 = Piel(cohete).Single(mf => mf.name.EndsWith("_Etapa2"));
                Assert.IsFalse(etapa1.transform.IsChildOf(superior));
                Assert.IsTrue(etapa2.transform.IsChildOf(superior));
                foreach (var mf in new[] { etapa1, etapa2 })
                {
                    var mats = mf.GetComponent<Renderer>().sharedMaterials;
                    int tapa = System.Array.FindIndex(mats, m => m.name.EndsWith("_Interior"));
                    Assert.GreaterOrEqual(tapa, 0, mf.name + " sin tapa: se vería el tubo hueco");
                    Assert.Greater(mf.sharedMesh.GetTriangles(tapa).Length, 0, mf.name + ": la tapa no tiene triángulos");
                }
                Assert.IsNotNull(superior.Find("MotorSuperior"), "tobera de la etapa superior");
            }
        }

        [Test]
        public void ElCorteEsRectoNoEnSierra([Values("pionero", "explorador", "horizonte", "goliat", "coloso")] string id)
        {
            var cohete = Cohetes(id)[0];
            var superior = cohete.Find("EtapaSuperior");
            float alto = PlataformaCohetes.Alto(id);
            var etapa1 = Piel(cohete).Single(mf => mf.name.EndsWith("_Etapa1"));
            var etapa2 = Piel(cohete).Single(mf => mf.name.EndsWith("_Etapa2"));
            float techo = etapa1.sharedMesh.vertices.Max(v => cohete.InverseTransformPoint(etapa1.transform.TransformPoint(v)).y);
            float suelo = etapa2.sharedMesh.vertices.Min(v => cohete.InverseTransformPoint(etapa2.transform.TransformPoint(v)).y);
            Assert.AreEqual(techo, suelo, alto * 0.002f, "las dos etapas se tocan en un plano, sin dientes ni hueco");
        }

        [Test]
        public void ElHorizonteTieneColoresPropios()
        {
            var colores = AssetDatabase.FindAssets("NASA_Cohete_AresA_ t:Material", new[] { "Assets/Materiales" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => !m.name.EndsWith("_DobleCara") && !m.name.EndsWith("_Interior"))
                .Select(m => ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor"))).Distinct().ToList();
            Assert.GreaterOrEqual(colores.Count, 4, "el Ares 1 A llegaba todo del mismo color: " + string.Join(", ", colores));
        }

        [Test]
        public void TodaLaPielUsaElEstiloDeTinta([ValueSource(nameof(Ids))] string id)
        {
            foreach (var mf in Piel(Cohetes(id)[0]))
                foreach (var m in mf.GetComponent<Renderer>().sharedMaterials)
                    Assert.AreEqual("OperacionEclipseRojo/Tinta", m.shader.name, m.name);
        }

        [Test]
        public void LosBrazosDeLaTorreTocanElCoheteSinAtravesarlo([ValueSource(nameof(Ids))] string id)
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var director = new SerializedObject(Object.FindObjectsByType<DirectorCinematica>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single());
            var lista = director.FindProperty("brazos");
            Assert.AreEqual(3, lista.arraySize);
            var brazo0 = (Transform)lista.GetArrayElementAtIndex(0).objectReferenceValue;
            // El cohete de la cinemática es el que comparte plataforma con el complejo de la torre.
            var complejo = brazo0.parent.parent;
            var cohete = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(t => t.name == "Cohete_" + id && (t.parent.position - complejo.position).magnitude < 0.01f);
            var perfil = cohete.GetComponent<PerfilCohete>();
            Assert.IsNotNull(perfil, "el cohete lleva sus medidas");
            float alto = PlataformaCohetes.Alto(id);
            var piel = Piel(cohete).SelectMany(mf => mf.sharedMesh.vertices.Select(v => cohete.InverseTransformPoint(mf.transform.TransformPoint(v)))).ToArray();
            for (int i = 0; i < 3; i++)
            {
                var pivote = (Transform)lista.GetArrayElementAtIndex(i).objectReferenceValue;
                var brazo = pivote.GetComponent<BrazoTorre>();
                Assert.IsNotNull(brazo);
                float altura = alto * (0.3f + 0.22f * i);   // como DirectorCinematica.PrepararComplejo
                pivote.localPosition = new Vector3(pivote.localPosition.x, altura, pivote.localPosition.z);
                brazo.Ajustar(perfil.HaciaTorre(altura - 0.3f, altura + 0.3f));
                // En el espacio de la plataforma (el cohete elegido se pone en su centro).
                var cabezal = complejo.InverseTransformPoint(brazo.Cabezal.position);
                var haciaTorre = new Vector3(cabezal.x, 0f, cabezal.z).normalized;
                var lateral = new Vector3(haciaTorre.z, 0f, -haciaTorre.x);
                float alcance = Vector3.Dot(cabezal, haciaTorre);
                var cerca = piel.Where(p => Mathf.Abs(p.y - altura) < 0.2f && Mathf.Abs(Vector3.Dot(p, lateral)) < 0.17f).ToArray();
                float costado = cerca.Length > 0 ? cerca.Max(p => Vector3.Dot(p, haciaTorre)) : perfil.HaciaTorre(altura - 0.2f, altura + 0.2f);
                Assert.LessOrEqual(costado, alcance + 0.01f, id + ", brazo " + i + ": el cabezal atraviesa el cohete");
                Assert.Less(alcance - costado, 0.45f, id + ", brazo " + i + ": el brazo no llega al cohete");
            }
        }

        [Test]
        public void LaCargaCabeEnLaCofia([ValueSource(nameof(Ids))] string id)
        {
            foreach (var cohete in Cohetes(id))
            {
                var perfil = cohete.GetComponent<PerfilCohete>();
                Assert.IsNotNull(perfil);
                float alto = PlataformaCohetes.Alto(id);
                var mitades = cohete.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "CofiaFrente" || t.name == "CofiaDetras" || t.name == "NarizIzq" || t.name == "NarizDer").ToArray();
                Assert.AreEqual(2, mitades.Length);
                var rs = mitades.SelectMany(m => m.GetComponentsInChildren<Renderer>(true)).ToArray();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float radioCofia = 0.5f * Mathf.Min(b.size.x, b.size.z);
                Assert.Greater(perfil.RadioCarga, alto * 0.005f, "radio de carga medido");
                Assert.Less(perfil.RadioCarga, radioCofia, "la carga no puede ser más ancha que la cofia");
                Assert.AreEqual(b.size.y, perfil.AltoCofia, alto * 0.02f);
            }
        }
    }
}
