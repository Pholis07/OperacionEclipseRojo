using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceMission.PlayTests
{
    /// <summary>
    /// Versión 8 (Eclipse Ink): el interruptor «Contornos de tinta» de Opciones. Se crea por código en el Menú,
    /// empieza marcado (contornos encendidos) y al desmarcarlo el shader deja de dibujar el contorno.
    /// </summary>
    public class OpcionesTests
    {
        bool contornos;

        [SetUp]
        public void Preparar() => contornos = Preferencias.ContornosTinta;

        [TearDown]
        public void Restaurar() { Preferencias.ContornosTinta = contornos; ContornosTinta.Aplicar(); }

        [UnityTest]
        public IEnumerator ElInterruptorDeContornosEstaEnOpcionesYApagaElContorno()
        {
            Preferencias.ContornosTinta = true;
            SceneManager.LoadScene("Menu");
            int cuadros = 0;
            while (SceneManager.GetActiveScene().name != "Menu" && cuadros++ < 600) yield return null;
            yield return null;

            var casillas = Object.FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var casilla = casillas.FirstOrDefault(t => t.name == "ContornosTinta");
            Assert.IsNotNull(casilla, "falta la casilla «Contornos de tinta» en Opciones");
            Assert.IsNotNull(casillas.FirstOrDefault(t => t.name == "PantallaCompleta"));
            Assert.IsTrue(casilla.isOn, "por defecto, contornos encendidos");
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_TintaSinContorno"));
            StringAssert.Contains(Idioma.Espanol ? "Contornos de tinta" : "Ink outlines", casilla.GetComponentInChildren<TMPro.TMP_Text>(true).text);

            // Con el panel abierto, la marca de la casilla se ve (y es la suya, no la de la casilla copiada).
            var opciones = Object.FindFirstObjectByType<OpcionesUI>();
            var abrir = (UnityEngine.UI.Button)typeof(OpcionesUI).GetField("botonAbrir", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(opciones);
            abrir.onClick.Invoke();
            yield return null;
            yield return null;
            Assert.IsTrue(casilla.gameObject.activeInHierarchy);
            Assert.IsNotNull(casilla.graphic);
            Assert.IsTrue(casilla.graphic.transform.IsChildOf(casilla.transform), "la marca pertenece a esta casilla");
            Assert.Greater(casilla.graphic.canvasRenderer.GetAlpha(), 0.9f, "marcada: la marca se ve");
            // La marca tenía tamaño negativo y no se veía en ninguna casilla (fallo anterior a la versión 8).
            foreach (var c in casillas.Where(t => t.transform.IsChildOf(casilla.transform.parent)))
            {
                var marca = c.graphic.rectTransform.rect.size;
                Assert.Greater(marca.x, 10f, c.name + ": la marca tiene que tener tamaño");
                Assert.Greater(marca.y, 10f, c.name);
            }

            casilla.isOn = false;
            yield return new WaitForSecondsRealtime(0.4f);   // la marca se desvanece en una décima
            Assert.IsFalse(Preferencias.ContornosTinta);
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_TintaSinContorno"), "modo ligero: sin contornos");
            Assert.Less(casilla.graphic.canvasRenderer.GetAlpha(), 0.1f);

            casilla.isOn = true;
            yield return null;
            Assert.AreEqual(0f, Shader.GetGlobalFloat("_TintaSinContorno"));
        }
    }
}
