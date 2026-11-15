using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 5, bloque 2: los buffs y debuffs salen de los datos y se muestran con flecha y color.</summary>
    public class ModificadoresTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            PresentacionMision.catalogoActual = catalogo;
        }

        System.Collections.Generic.List<Modificador> De(string satelite) => Modificadores.DeSatelite(catalogo.Satelite(satelite), catalogo);
        static bool Tiene(System.Collections.Generic.List<Modificador> l, bool positivo, string texto) =>
            l.Any(m => m.Positivo == positivo && m.Texto.Contains(texto));

        Operacion Caso2(params string[] herramientas)
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var carga = new Carga(e, c, catalogo);
            foreach (var h in herramientas) Assert.IsTrue(carga.Cargar(h), h);
            var op = new Operacion(e, c, catalogo, null, 1, carga);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            return op;
        }

        [Test]
        public void ElMroNoTieneModificadores()
        {
            CollectionAssert.IsEmpty(De("mro"));
            StringAssert.Contains("reference spacecraft", PresentacionMision.Modificadores(De("mro")));
        }

        [Test]
        public void LosModificadoresDelChandraSalenDeSusDatos()
        {
            var l = De("chandra");
            Assert.IsTrue(Tiene(l, true, "read +20% farther"));
            Assert.IsTrue(Tiene(l, true, "orientation"));
            Assert.IsTrue(Tiene(l, false, "Memory 6 → 5 blocks"));
            Assert.IsTrue(Tiene(l, false, "40 kg: -4 fuel"));
            Assert.IsTrue(Tiene(l, false, "Costs 30 credits"));
        }

        [Test]
        public void LosDemasSatelitesTambien()
        {
            Assert.IsTrue(Tiene(De("aztechsat"), false, "Power limit 700 → 600 W"));
            Assert.IsTrue(Tiene(De("aztechsat"), true, "without contact (2 blocks"));
            Assert.IsTrue(Tiene(De("llcd"), true, "every month"));
            Assert.IsTrue(Tiene(De("llcd"), true, "Transmitter 50 W instead of 150 W"));
            Assert.IsTrue(Tiene(De("llcd"), false, "storm"));
            Assert.IsTrue(Tiene(De("aquarius"), false, "+50 W"));
            Assert.IsTrue(Tiene(De("aquarius"), true, "Memory 6 → 7"));
            Assert.IsTrue(Tiene(De("magellan"), true, "Altitude change: 2 fuel instead of 3"));
        }

        [Test]
        public void ElFormatoDistingueBuffsYDebuffsPorIconoYColor()
        {
            var texto = PresentacionMision.Modificadores(De("chandra"));
            StringAssert.Contains(PresentacionMision.ColorBuff + ">" + Iconos.Sube, texto);
            StringAssert.Contains(PresentacionMision.ColorDebuff + ">" + Iconos.Baja, texto);
            Assert.Less(texto.IndexOf(Iconos.Sube, System.StringComparison.Ordinal), texto.IndexOf(Iconos.Baja, System.StringComparison.Ordinal), "los buffs van primero");
        }

        [Test]
        public void LosModificadoresActivosSiguenLaOperacion()
        {
            var op = Caso2("bateria");
            Assert.IsTrue(Tiene(Modificadores.Activos(op, catalogo), true, "Backup battery on board"));
            Assert.IsTrue(op.PlanearManiobra(Altitud.Baja));
            op.ResolverMes(1);
            var activos = Modificadores.Activos(op, catalogo);
            Assert.IsTrue(Tiene(activos, true, "Low orbit: readings reach ×1.5 farther"));
            Assert.IsTrue(Tiene(activos, false, "drag: -1 fuel a month"));

            op.AplicarEvento(catalogo.eventos.First(e => e.id == "sombra"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Assert.IsTrue(Tiene(Modificadores.Activos(op, catalogo), false, "-150 W (3 months left)"));

            op.AplicarEvento(catalogo.eventos.First(e => e.id == "tormenta"));
            op.Decidir(op.Opciones.First(o => o.efecto == EfectoOpcion.Aguantar));
            Assert.IsTrue(Tiene(Modificadores.Activos(op, catalogo), false, "quality ×0.8"));
        }

        [Test]
        public void ElResumenGuardaLosFactoresDeLaCalidad()
        {
            var op = Caso2();
            op.PlanearManiobra(Altitud.Baja);
            op.Encender("termica");
            op.FijarRegion("termica", new Vector2(0.9f, 0.9f));
            var r = op.ResolverMes(1);
            Assert.AreEqual(70f, r.CalidadBase);
            Assert.AreEqual(1.2f, r.FactorAltitud);
            Assert.AreEqual(1f, r.FactorTormenta);
            Assert.AreEqual(70f * 28 / 30 * 1.2f, r.Observaciones[0].Calidad, 0.01f);
        }

        [Test]
        public void EnLaCargaElSateliteMuestraSusModificadores()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "chandra" };
            var ui = Object.FindFirstObjectByType<CargaUI>(FindObjectsInactive.Include);
            ui.Mostrar(Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo));
            var texto = ((TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoRecursos").objectReferenceValue).text;
            StringAssert.Contains(Iconos.Baja + " Memory 6 → 5 blocks", texto);
            StringAssert.Contains(Iconos.Sube + " ", texto);
        }

        [Test]
        public void LaOperacionMuestraLosModificadoresActivos()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" }) { SateliteId = "chandra" };
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            var texto = ((TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoModificadores").objectReferenceValue).text;
            StringAssert.Contains("ACTIVE MODIFIERS", texto);
            StringAssert.Contains("Memory 6 → 5 blocks", texto);
        }
    }
}
