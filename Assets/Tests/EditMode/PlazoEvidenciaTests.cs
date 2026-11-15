using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 9 (Adrian, 2026-11-15): quien no juega pierde. Si al cerrar el mes 6 no ha llegado ninguna evidencia
    /// a la Tierra, Control de Misión cancela la misión. Y cada decisión de la Operación dice qué causó.
    /// </summary>
    public class PlazoEvidenciaTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar() => catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");

        Operacion Caso2()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            return op;
        }

        [Test]
        public void ElLimiteEsElMes6()
        {
            Assert.AreEqual(6, catalogo.reglas.mesLimiteSinEvidencia);
        }

        [Test]
        public void SinHacerNadaLaMisionSeCancelaAlCerrarElMes6()
        {
            var op = Caso2();
            for (int mes = 1; mes <= 5; mes++)
            {
                Assert.IsTrue(op.ResolverMes(mes).Valido);
                Assert.IsFalse(op.Terminada, "sigue hasta el mes 6 (mes " + mes + ")");
            }
            var r = op.ResolverMes(6);
            Assert.IsTrue(r.Termino);
            Assert.IsTrue(op.Terminada);
            Assert.IsTrue(op.CanceladaSinEvidencia);
            Assert.AreEqual(6, op.MesesResueltos, "no se juegan los meses 7 a 12");
        }

        [Test]
        public void ConEvidenciaEnLaTierraSigueDespuesDelMes6()
        {
            var op = Caso2();
            op.Encender("termica");
            op.FijarRegion("termica", Centro);
            op.ResolverMes(1);
            op.TransmitirPlaneado = true;   // el mes 2 tiene contacto
            op.ResolverMes(2);
            Assert.IsTrue(op.HayEvidenciaEnTierra);
            op.Apagar("termica");
            for (int mes = 3; mes <= 7; mes++) op.ResolverMes(mes);
            Assert.IsFalse(op.Terminada);
            Assert.IsFalse(op.CanceladaSinEvidencia);
        }

        [Test]
        public void ObservarSinEnviarNoBasta()
        {
            var op = Caso2();
            op.Encender("termica");
            op.FijarRegion("termica", Centro);
            for (int mes = 1; mes <= 6; mes++) op.ResolverMes(mes);
            Assert.IsTrue(op.CanceladaSinEvidencia, "la evidencia en memoria no cuenta: tiene que llegar a la Tierra");
        }

        [Test]
        public void ElInformeExplicaLaCancelacion()
        {
            var op = Caso2();
            for (int mes = 1; mes <= 6; mes++) op.ResolverMes(mes);
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var informe = InformeMision.Construir(Evaluador.Evaluar(c, catalogo), c, catalogo, op, null);
            StringAssert.Contains("Mission Control cancelled the mission", informe.Texto);
            StringAssert.Contains("month 6", informe.Texto);
            Assert.AreEqual(CategoriaResultado.Fracaso, ResultadoCientifico.Categoria(Evaluador.Evaluar(c, catalogo), op, catalogo));
        }

        [Test]
        public void CadaDecisionDiceQueCauso()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            EstadoJuego.Reiniciar();
            EstadoJuego.Configuracion = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo);
            var ui = Object.FindFirstObjectByType<OperacionUI>(FindObjectsInactive.Include);
            ui.Comenzar(e, new Carga(e, EstadoJuego.Configuracion, catalogo), 7);
            var lectura = (TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoLectura").objectReferenceValue;
            var progreso = (TMPro.TMP_Text)new SerializedObject(ui).FindProperty("textoProgreso").objectReferenceValue;
            StringAssert.Contains("by the end of month 6", progreso.text, "el plazo se ve desde el principio");

            ui.AlternarInstrumento("radar");
            StringAssert.Contains("Subsurface radar ON: uses 50 W and 2 memory block(s) per month.", lectura.text);
            ui.AlternarInstrumento("radar");
            StringAssert.Contains("Subsurface radar OFF: frees 50 W", lectura.text);
            ui.AlternarManiobra();
            StringAssert.Contains("Maneuver planned: 40 cr", lectura.text);
            StringAssert.Contains("drag uses", lectura.text);
            ui.AlternarManiobra();
            StringAssert.Contains("Maneuver cancelled", lectura.text);
            ui.AlternarEnvio();
            StringAssert.Contains("send", lectura.text);
        }
    }
}
