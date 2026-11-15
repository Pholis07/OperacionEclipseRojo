using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 5, bloque 5: el juego en inglés o en español, con el selector en el título y en Opciones.</summary>
    public class IdiomaTests
    {
        CatalogoDatos catalogo;
        string guardado;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
            guardado = PlayerPrefs.HasKey(Idioma.Clave) ? PlayerPrefs.GetString(Idioma.Clave) : null;
            PlayerPrefs.DeleteKey(Idioma.Clave);
            Idioma.Recargar();
        }

        [TearDown]
        public void Restaurar()
        {
            if (guardado == null) PlayerPrefs.DeleteKey(Idioma.Clave); else PlayerPrefs.SetString(Idioma.Clave, guardado);
            Idioma.Recargar();
        }

        [Test]
        public void PorDefectoElJuegoEstaEnIngles()
        {
            Assert.IsFalse(Idioma.Espanol);
            Assert.AreEqual("Infrared thermal camera", catalogo.Instrumento("termica").Nombre);
            Assert.AreEqual("Mission accomplished", TextosResultado.TituloVictoria);
        }

        [Test]
        public void ElegirEspanolSeGuardaYAvisa()
        {
            int avisos = 0;
            System.Action contar = () => avisos++;
            Idioma.AlCambiar += contar;
            try
            {
                Idioma.Espanol = true;
                Idioma.Espanol = true;
            }
            finally { Idioma.AlCambiar -= contar; }
            Assert.AreEqual(1, avisos, "solo avisa cuando cambia");
            Assert.AreEqual("es", PlayerPrefs.GetString(Idioma.Clave));
            Idioma.Recargar();
            Assert.IsTrue(Idioma.Espanol, "se recuerda");
        }

        [Test]
        public void EnEspanolLosDatosSeMuestranTraducidos()
        {
            Idioma.Espanol = true;
            Assert.AreEqual("Radar de subsuelo", catalogo.Instrumento("radar").Nombre);
            Assert.AreEqual("Explorador", catalogo.Lanzador("explorador").Nombre);
            Assert.AreEqual("Elíptica", catalogo.Orbita("eliptica").Nombre);
            StringAssert.StartsWith("Basado en SHARAD", catalogo.Instrumento("radar").DatoNasa);
            Assert.AreEqual("Misión cumplida", TextosResultado.TituloVictoria);
            Assert.AreEqual("radar", catalogo.Instrumento("radar").id, "los ids no cambian");
            Assert.AreEqual("Subsurface radar", catalogo.Instrumento("radar").nombre, "el dato guardado sigue en inglés");
        }

        [Test]
        public void TodosLosTextosVisiblesDeLosDatosTienenTraduccion()
        {
            // Los nombres propios («Chandra (1999)») y los créditos de modelos no se traducen.
            var faltan = new System.Collections.Generic.List<string>();
            void Ver(string id, string texto) { if (!string.IsNullOrEmpty(texto) && !Traducciones.Es.ContainsKey(texto)) faltan.Add(id + ": " + texto); }
            foreach (var i in catalogo.instrumentos) { Ver(i.id, i.nombre); Ver(i.id, i.textoAyuda); Ver(i.id, i.pistaAsociada); }
            foreach (var o in catalogo.orbitas) { Ver(o.id, o.nombre); Ver(o.id, o.descripcionBreve); Ver(o.id, o.ventaja); Ver(o.id, o.limitacion); }
            foreach (var l in catalogo.lanzadores) { Ver(l.id, l.nombre); Ver(l.id, l.descripcion); }
            foreach (var s in catalogo.satelites) { Ver(s.id, s.nombre); Ver(s.id, s.descripcion); Ver(s.id, s.ventaja); Ver(s.id, s.desventaja); }
            foreach (var h in catalogo.herramientas) { Ver(h.id, h.nombre); Ver(h.id, h.descripcion); }
            foreach (var e in catalogo.eventos) { Ver(e.id, e.nombre); Ver(e.id, e.texto); foreach (var o in e.opciones) Ver(e.id, o.texto); }
            foreach (var t in catalogo.transmisiones) Ver(t.id, t.texto);
            foreach (var o in catalogo.objetivos) Ver(o.id, o.texto);
            for (int n = 0; n < 3; n++) { var m = catalogo.Nivel(n); Ver("nivel " + n, m.nombreNivel); Ver("nivel " + n, m.objetivo); }
            CollectionAssert.IsEmpty(faltan, "sin traducción:\n" + string.Join("\n", faltan));
        }

        [Test]
        public void LaTraduccionCompuestaRespetaLosIconos()
        {
            Idioma.Espanol = true;
            Assert.AreEqual(Iconos.Senal + "  ÚLTIMA SEÑAL", Idioma.TCompuesto(Iconos.Senal + "  LATEST SIGNAL"));
            Assert.AreEqual("Continuar  " + Iconos.Siguiente, Idioma.TCompuesto("Continue  " + Iconos.Siguiente));
            Assert.AreEqual("Revisar misión  " + Iconos.Siguiente, Idioma.TCompuesto("Review mission  " + Iconos.Siguiente));
            Assert.AreEqual("PASO 2 DE 4  ·  CONFIGURACIÓN", Idioma.TCompuesto("STEP 2 OF 4  ·  SETUP"));
            Assert.AreEqual("VERDE", PresentacionMision.NombreEstado(EstadoSemaforo.Verde));
            Assert.AreEqual(Iconos.Lanzador + "   Iniciar misión  " + Iconos.Siguiente, Idioma.TCompuesto(Iconos.Lanzador + "   Start mission  " + Iconos.Siguiente));
            Assert.AreEqual("<color=#22D3EE>" + Iconos.Orbita + "</color>  Elige una órbita, un lanzador y los instrumentos científicos.\n<color=#22D3EE>" + Iconos.Masa + "</color>  No te pases de los límites de masa, potencia, presupuesto y espacios.",
                Idioma.TCompuesto("<color=#22D3EE>" + Iconos.Orbita + "</color>  Choose an orbit, a launcher and the science instruments.\n<color=#22D3EE>" + Iconos.Masa + "</color>  Stay within the mass, power, budget and slot limits."));
            Assert.AreEqual("texto sin par", Idioma.TCompuesto("texto sin par"));
        }

        [Test]
        public void LaGuiaYElInformeSalenEnEspanol()
        {
            Idioma.Espanol = true;
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo);
            var op = new Operacion(e, c, catalogo, null, 1, new Carga(e, c, catalogo));
            StringAssert.StartsWith("Enciende Cámara térmica infrarroja", Guia.Siguiente(op, catalogo).Texto);
            op.Terminar();
            var informe = InformeMision.Construir(e, c, catalogo, op, null);
            Assert.AreEqual("Investigación fallida", informe.Titulo);
            StringAssert.StartsWith("Usaste 820 de 1,250 créditos", informe.Recursos);
        }

        [Test]
        public void LosTextosFijosDeLaEscenaSeTraducenYVuelven()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Mision.unity");
            var boton = GameObject.Find("BotonContinuar").GetComponentInChildren<TMP_Text>();
            var original = boton.text;
            StringAssert.StartsWith("Continue", original);
            Idioma.Espanol = true;
            TraductorEscena.Traducir();
            StringAssert.StartsWith("Continuar", boton.text);
            Idioma.Espanol = false;
            TraductorEscena.Traducir();
            Assert.AreEqual(original, boton.text);
        }

        [Test]
        public void ElSelectorEstaEnElTituloYEnOpcionesYCambiaElIdioma()
        {
            EditorSceneManager.OpenScene("Assets/Escenas/Menu.unity");
            var selectores = Object.FindObjectsByType<SelectorIdioma>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CollectionAssert.AreEquivalent(new[] { "SelectorIdiomaTitulo", "SelectorIdiomaOpciones" }, selectores.Select(s => s.name).ToArray());
            var espanol = selectores[0].transform.Find("BotonEspanol").GetComponent<UnityEngine.UI.Button>();
            var so = new SerializedObject(selectores[0]);
            Assert.AreSame(espanol, so.FindProperty("botonEspanol").objectReferenceValue);
            Idioma.Espanol = true;
            selectores[0].Pintar();
            Assert.AreEqual((Color)new Color32(0xE2, 0x64, 0x3A, 0xFF), espanol.GetComponent<UnityEngine.UI.Image>().color, "el elegido se resalta");
        }

        [Test]
        public void ElDiccionarioNoTieneTextosVacios()
        {
            Assert.Greater(Traducciones.Es.Count, 600);
            // « orbit» queda vacío a propósito: «maniobra a órbita» ya lo lleva.
            var vacios = Traducciones.Es.Where(p => string.IsNullOrEmpty(p.Value)).Select(p => p.Key).ToArray();
            CollectionAssert.AreEqual(new[] { " orbit" }, vacios);
        }
    }
}
