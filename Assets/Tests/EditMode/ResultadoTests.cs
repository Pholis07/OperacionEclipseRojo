using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>
    /// Versión 8: el informe final. Veredicto tipo póster (evidencias de un vistazo) y detalles como tablero
    /// (barras de recursos, fichas, una fila por evidencia, tabla de puntos y botón al menú).
    /// </summary>
    public class ResultadoTests
    {
        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            EstadoJuego.Reiniciar();
            Idioma.Espanol = false;
        }

        [TearDown]
        public void Limpiar() => EstadoJuego.Reiniciar();

        /// <summary>Abre la escena con una misión jugada: observa la anomalía en el mes 1 y, si se pide, envía en el 2.</summary>
        ResultadoUI Abrir(string lanzador, string orbita, string instrumentos, bool enviar, string satelite = "mro")
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Resultado.unity");
            var c = new Configuracion(orbita, lanzador, instrumentos.Split('+')) { SateliteId = satelite };
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var op = new Operacion(e, c, catalogo, nivel, 7, new Carga(e, c, catalogo, nivel));
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            foreach (var id in op.Instrumentos.ToList())
            {
                Assert.IsTrue(op.Encender(id), id);
                op.FijarRegion(id, op.Anomalia);
            }
            Assert.IsTrue(op.ResolverMes(1).Valido);
            if (enviar) { op.TransmitirPlaneado = true; Assert.IsTrue(op.ResolverMes(2).Valido); }
            op.Terminar();
            EstadoJuego.Configuracion = c;
            EstadoJuego.Resultado = e;
            EstadoJuego.Operacion = op;
            EstadoJuego.CategoriaFinal = ResultadoCientifico.Categoria(e, op, catalogo);
            var ui = Object.FindFirstObjectByType<ResultadoUI>();
            ui.Mostrar(e, c);
            return ui;
        }

        static string Texto(string nombre) =>
            Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == nombre).text;

        [Test]
        public void LasBarrasMuestranLosRecursosDeLaEvaluacion()
        {
            var ui = Abrir("explorador", "media", "termica+radar", true);
            var r = EstadoJuego.Resultado.Calculo;
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            var lanzador = catalogo.Lanzador("explorador");
            Assert.AreEqual(4, ui.Informe.Barras.Count);
            CollectionAssert.AreEqual(new[] { r.CostoCr, r.MasaKg, r.PotenciaW, r.EspaciosUsados }, ui.Informe.Barras.Select(b => b.Usado).ToArray());
            CollectionAssert.AreEqual(new[] { nivel.presupuestoCr, lanzador.capacidadKg, nivel.potenciaDisponibleW, lanzador.espacios },
                ui.Informe.Barras.Select(b => b.Limite).ToArray());
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(ui.Informe.Barras[i].Fraccion, ui.Barras[i].anchorMax.x, 0.001f, "barra " + i);
                Assert.That(ui.Barras[i].anchorMax.x, Is.InRange(0.01f, 1f));
                StringAssert.Contains(TextosBriefing.Numero(ui.Informe.Barras[i].Usado) + " / " + TextosBriefing.Numero(ui.Informe.Barras[i].Limite), ui.TextosBarra[i].text);
            }
            StringAssert.Contains("Credits", ui.TextosBarra[0].text);
            StringAssert.Contains("kg", ui.TextosBarra[1].text);
        }

        [Test]
        public void EnLaVictoriaTodasLasEvidenciasLlegaron()
        {
            var ui = Abrir("explorador", "media", "termica+radar", true);
            Assert.AreEqual(CategoriaResultado.Victoria, EstadoJuego.CategoriaFinal);
            Assert.AreEqual(2, ui.Informe.Filas.Count);
            Assert.IsTrue(ui.Informe.Filas.All(f => f.Llego && f.Principal));
            CollectionAssert.AreEqual(new[] { "termica", "radar" }, ui.Informe.Filas.Select(f => f.InstrumentoId).ToArray());
            StringAssert.Contains("Reached Earth", ui.TextoEvidencias);
            StringAssert.DoesNotContain("Missing", ui.TextoEvidencias);
            StringAssert.DoesNotContain(ResultadoUI.ColorMal, ui.TextoEvidencias);
            // El veredicto las resume de un vistazo, con su marca.
            var resumen = Texto("TextoResumen");
            StringAssert.Contains("Infrared thermal camera", resumen);
            StringAssert.Contains("Subsurface radar", resumen);
            Assert.AreEqual(2, resumen.Split('\n').Length);
            StringAssert.Contains(Iconos.Verde, resumen);
        }

        [Test]
        public void EnElParcialSeVeQueLlegoYQueFalto()
        {
            var ui = Abrir("explorador", "media", "termica+atmosferico", true);
            Assert.AreEqual(CategoriaResultado.Parcial, EstadoJuego.CategoriaFinal);
            var filas = ui.Informe.Filas;
            Assert.IsTrue(filas.First(f => f.InstrumentoId == "termica").Llego);
            Assert.IsTrue(filas.First(f => f.InstrumentoId == "atmosferico").Llego);
            Assert.IsFalse(filas.First(f => f.InstrumentoId == "atmosferico").Principal, "el sensor atmosférico es complementario");
            var radar = filas.First(f => f.InstrumentoId == "radar");
            Assert.IsFalse(radar.Llego, "el radar no iba a bordo");
            Assert.IsFalse(string.IsNullOrEmpty(radar.Detalle), "dice por qué faltó");
            StringAssert.Contains("Missing", ui.TextoEvidencias);
            StringAssert.Contains("supporting", ui.TextoEvidencias);
            StringAssert.Contains(Iconos.Rojo, Texto("TextoResumen"));
        }

        [Test]
        public void EnElFracasoNadaLlegoYLaMemoriaLoDice()
        {
            var ui = Abrir("explorador", "media", "termica+radar", false);
            Assert.AreEqual(CategoriaResultado.Fracaso, EstadoJuego.CategoriaFinal);
            Assert.IsTrue(ui.Informe.Filas.All(f => !f.Llego));
            StringAssert.DoesNotContain("Reached Earth", ui.TextoEvidencias);
            Assert.AreEqual("1 / " + (1 + EstadoJuego.Operacion.MesesRestantes), ui.Informe.Fichas[0].Valor, "meses usados");
            Assert.AreEqual(EstadoJuego.Operacion.Memoria.Count.ToString(), ui.Informe.Fichas[2].Valor, "registros sin enviar");
            Assert.Greater(EstadoJuego.Operacion.Memoria.Count, 0);
            StringAssert.Contains("Records unsent", Texto("TextoFicha2"));
        }

        [Test]
        public void LaTablaDePuntosSumaElTotal()
        {
            var ui = Abrir("explorador", "media", "termica+radar", true);
            var lineas = Puntuacion.Desglose(EstadoJuego.Resultado, EstadoJuego.Operacion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice));
            foreach (var l in lineas) StringAssert.Contains(l.Concepto, ui.TextoPuntosActual);
            StringAssert.Contains("<b>" + TextosBriefing.Numero(Puntuacion.Total(lineas)) + "</b>", ui.TextoPuntosActual);
            StringAssert.Contains("Total", ui.TextoPuntosActual);
        }

        [Test]
        public void ElMenuYModificarSoloEstanEnLosDetalles()
        {
            var ui = Abrir("explorador", "media", "termica+radar", true);
            Assert.IsNull(GameObject.Find("BotonMenu"), "en el veredicto solo está «Ver detalles»");
            Assert.IsNull(GameObject.Find("BotonModificar"));
            Assert.IsNotNull(GameObject.Find("BotonDetalle"));
            ui.VerDetalle();
            Assert.IsNotNull(GameObject.Find("BotonMenu"));
            Assert.IsNotNull(GameObject.Find("BotonModificar"));
            Assert.IsNotNull(GameObject.Find("BotonVeredicto"));
            StringAssert.Contains("Menu", GameObject.Find("BotonMenu").GetComponentInChildren<TMP_Text>().text);
            // Ir al menú no borra lo elegido: se puede volver a jugar con la misma nave.
            var c = EstadoJuego.Configuracion;
            ui.VolverAlMenu();
            Assert.AreSame(c, EstadoJuego.Configuracion);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NingunTextoDelInformeQuedaCortado(bool espanol)
        {
            // El peor caso: Titán con los cinco instrumentos (cinco filas de evidencia y la tabla de puntos más larga).
            Idioma.Espanol = espanol;
            try
            {
                var ui = Abrir("titan", "eliptica", "termica+radar+atmosferico+alta_res", true, "chandra");
                TraductorEscena.Traducir();
                foreach (var detalle in new[] { false, true })
                {
                    if (detalle) ui.VerDetalle();
                    Canvas.ForceUpdateCanvases();
                    foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                    {
                        t.ForceMeshUpdate();
                        Assert.IsFalse(t.isTextTruncated, (detalle ? "detalle: " : "veredicto: ") + t.name + " cortado: " + t.text);
                    }
                }
            }
            finally { Idioma.Espanol = false; }
        }

        [Test]
        public void SinMisionLanzadaElTableroQuedaVacio()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Resultado.unity");
            var ui = Object.FindFirstObjectByType<ResultadoUI>();
            ui.Mostrar(null, null);
            Assert.IsNull(ui.Informe);
            Assert.AreEqual("", ui.TextoEvidencias);
            Assert.IsTrue(ui.TextosBarra.All(t => t.text == ""));
            Assert.IsTrue(ui.Barras.All(b => b.anchorMax.x == 0f));
        }

        [Test]
        public void LaTormentaDelFracasoDejaVerMarte()
        {
            Assert.That(FinalAnimado.TormentaFracaso, Is.InRange(0.3f, 0.6f), "antes subía al 95 % y borraba el relieve");
        }
    }
}
