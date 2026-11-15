using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceMission.PlayTests
{
    /// <summary>
    /// Fase 8: recorre el juego en modo Play, pulsando los botones reales, para los 19 casos de la tabla
    /// del prompt 08 (generados de esa tabla con un script). Compara la línea [Misión] de la consola,
    /// el bloqueo de «Lanzar», el número de transmisiones y «Modificar mi misión».
    /// </summary>
    public class RecorridoTests
    {
        const int MaxCuadros = 600;
        readonly List<string> lineas = new List<string>();

        void Capturar(string mensaje, string pila, LogType tipo)
        {
            if (mensaje.StartsWith("[Misión]")) lineas.Add(mensaje);
        }

        [SetUp]
        public void Preparar()
        {
            lineas.Clear();
            Application.logMessageReceived += Capturar;
            EstadoJuego.Reiniciar();
        }

        [TearDown]
        public void Limpiar() => Application.logMessageReceived -= Capturar;

        [TestCase("1", "explorador", "polar", "termica+radar", "[Misión] masa=430 potencia=220 costo=850 semáforo=VERDE resultado=VICTORIA", 3, ExpectedResult = null)]
        [TestCase("2", "explorador", "media", "termica+radar", "[Misión] masa=430 potencia=220 costo=820 semáforo=VERDE resultado=VICTORIA", 3, ExpectedResult = null)]
        [TestCase("3", "explorador", "eliptica", "termica+radar", "[Misión] masa=430 potencia=220 costo=870 semáforo=AMARILLO resultado=PARCIAL", 3, ExpectedResult = null)]
        [TestCase("4", "titan", "eliptica", "termica+radar+atmosferico", "[Misión] masa=455 potencia=275 costo=1130 semáforo=VERDE resultado=VICTORIA", 4, ExpectedResult = null)]
        [TestCase("5", "explorador", "media", "termica+atmosferico", "[Misión] masa=410 potencia=225 costo=650 semáforo=AMARILLO resultado=PARCIAL", 3, ExpectedResult = null)]
        [TestCase("6", "titan", "polar", "alta_res+atmosferico", "[Misión] masa=570 potencia=455 costo=870 semáforo=AMARILLO resultado=FRACASO", 2, ExpectedResult = null)]
        [TestCase("7", "titan", "media", "termica+radar+espectrometro+atmosferico", "[Misión] masa=555 potencia=500 costo=1300 semáforo=ROJO resultado=-", 0, ExpectedResult = null)]
        [TestCase("B1", "titan", "media", "", "[Misión] masa=- potencia=- costo=- semáforo=GRIS resultado=-", 0, ExpectedResult = null)]
        [TestCase("B2", "explorador", "media", "termica+alta_res+atmosferico", "[Misión] masa=605 potencia=525 costo=850 semáforo=ROJO resultado=-", 0, ExpectedResult = null)]
        [TestCase("B4", "titan", "media", "termica+radar+alta_res+atmosferico", "[Misión] masa=650 potencia=575 costo=1280 semáforo=ROJO resultado=-", 0, ExpectedResult = null)]
        [TestCase("B4b", "titan", "media", "termica+alta_res+atmosferico+espectrometro", "[Misión] masa=705 potencia=750 costo=1240 semáforo=ROJO resultado=-", 0, ExpectedResult = null)]
        [TestCase("B5", "titan", "eliptica", "termica+radar+espectrometro", "[Misión] masa=530 potencia=445 costo=1260 semáforo=ROJO resultado=-", 0, ExpectedResult = null)]
        [TestCase("B6", "explorador", "media", "radar", "[Misión] masa=395 potencia=150 costo=640 semáforo=AMARILLO resultado=PARCIAL", 2, ExpectedResult = null)]
        [TestCase("B7", "titan", null, "termica+radar", "[Misión] masa=- potencia=- costo=- semáforo=GRIS resultado=-", 0, ExpectedResult = null)]
        [TestCase("B8", null, "media", "termica+radar", "[Misión] masa=- potencia=- costo=- semáforo=GRIS resultado=-", 0, ExpectedResult = null)]
        [TestCase("RV1", "titan", "polar", "termica+alta_res+espectrometro", "[Misión] masa=680 potencia=695 costo=1180 semáforo=AMARILLO resultado=PARCIAL", 4, ExpectedResult = null)]
        [TestCase("RV2", "titan", "media", "termica+espectrometro+atmosferico", "[Misión] masa=510 potencia=450 costo=1040 semáforo=AMARILLO resultado=PARCIAL", 3, ExpectedResult = null)]
        [TestCase("RV3", "titan", "eliptica", "termica+alta_res+atmosferico", "[Misión] masa=605 potencia=525 costo=1070 semáforo=AMARILLO resultado=PARCIAL", 3, ExpectedResult = null)]
        [TestCase("RV4", "explorador", "polar", "termica+atmosferico", "[Misión] masa=410 potencia=225 costo=680 semáforo=AMARILLO resultado=PARCIAL", 2, ExpectedResult = null)]
        public IEnumerator Recorrido(string caso, string lanzador, string orbita, string instrumentos,
            string lineaEsperada, int transmisiones)
        {
            SceneManager.LoadScene("Menu");
            yield return EsperarEscena("Menu");

            Pulsar("BotonIniciar");
            yield return EsperarEscena("Mision");
            Pulsar("BotonContinuar"); // del Briefing
            yield return null;

            var catalogo = Object.FindFirstObjectByType<MisionUI>().Catalogo;
            if (lanzador != null) { Pulsar("PestanaLanzador"); PulsarOpcion(catalogo.Lanzador(lanzador).nombre); }
            if (orbita != null) { Pulsar("PestanaOrbita"); PulsarOpcion(catalogo.Orbita(orbita).nombre); }
            if (instrumentos != "")
            {
                Pulsar("PestanaInstrumentos");
                foreach (var id in instrumentos.Split('+')) PulsarOpcion(catalogo.Instrumento(id).nombre);
            }
            yield return null;

            Assert.IsNotEmpty(lineas, "No se escribió ninguna línea [Misión] en el caso " + caso);
            Debug.Log("RECORRIDO " + caso + " | esperado: " + lineaEsperada + " | obtenido: " + lineas.Last());
            Assert.AreEqual(lineaEsperada, lineas.Last(), "línea [Misión] del caso " + caso);

            Pulsar("BotonRevisar");
            yield return null;
            var lanzar = Boton("BotonLanzar");
            Assert.AreEqual(transmisiones > 0, lanzar.interactable, "«Lanzar» del caso " + caso);

            if (transmisiones == 0)
            {
                Pulsar("BotonCorregir");
                yield return null;
                Assert.IsTrue(Object.FindFirstObjectByType<MisionUI>().VistaActivaEsConfiguracion);
                yield break;
            }

            var elegidas = EstadoJuego.Configuracion.InstrumentoIds.ToArray();
            Pulsar("BotonLanzar");          // de la Revisión a la Carga (gameplay v3)
            yield return null;
            var primera = Boton("Carga_" + catalogo.herramientas[0].id);
            if (primera.interactable) primera.onClick.Invoke();   // sube una herramienta si cabe
            Pulsar("BotonLanzarCarga");
            yield return null;

            // Fase de Operación por meses, como un jugador: cada mes enciende lo que aún no tiene evidencia, le fija
            // la región de la anomalía, pide el envío si hay contacto y pulsa «Resolver mes». Lee el resumen y
            // responde a los eventos con la primera opción posible.
            var ui = Object.FindFirstObjectByType<OperacionUI>();
            Assert.IsNotNull(ui?.Op, "No empezó la Operación en el caso " + caso);
            var op = ui.Op;
            Assert.IsNotNull(op.Carga, "la Operación debe empezar con la Carga");
            for (int vuelta = 0; vuelta < 40 && !op.Terminada && !op.Completa; vuelta++)
            {
                // El resumen espera a que termine la animación para verse; aquí se cierra sin esperarla.
                if (ui.ResumenVisible) { ui.ContinuarResumen(); yield return null; continue; }
                if (op.EsperandoDecision) { Responder(ui); yield return null; continue; }
                foreach (var h in op.HerramientasUtiles.ToList()) ui.UsarHerramienta(h);
                foreach (var id in op.Instrumentos.ToList())
                {
                    bool hacefalta = op.Pendientes.ContainsValue(id);
                    if (hacefalta != op.Encendidos.Contains(id)) ui.AlternarInstrumento(id);   // enciende o apaga
                    if (hacefalta) { ui.ElegirEscaner(id); ui.FijarRegionEn(op.Anomalia); }
                }
                if (op.Memoria.Count > 0 && op.MotivoSinEnvio == null && !op.TransmitirPlaneado) ui.AlternarEnvio();
                int mes = op.Mes;
                Pulsar("BotonResolver");
                Assert.AreEqual(mes + 1, op.Mes, "«Resolver mes» avanza exactamente un mes en el caso " + caso);
                yield return null;
            }
            Debug.Log("OPERACION " + caso + " | meses: " + op.MesesResueltos + " | recibidas: " + string.Join(",", op.Recibidas) +
                      " | eventos: " + string.Join(",", op.Eventos.Select(e => e.Datos.id)) + " | apagones: " + op.Apagones);
            Assert.IsTrue(op.Completa, "no llegó toda la evidencia en el caso " + caso);
            Assert.AreEqual(transmisiones, op.Recibidas.Count, "evidencias recibidas del caso " + caso);
            if (ui.ResumenVisible) { ui.ContinuarResumen(); yield return null; }
            if (op.EsperandoDecision) { Responder(ui); yield return null; }

            Pulsar("BotonTerminar");
            yield return EsperarEscena("Resultado");

            // Con toda la evidencia enviada, el resultado es el que permitía la configuración.
            Assert.AreEqual(EstadoJuego.Resultado.Categoria, EstadoJuego.CategoriaFinal, "categoría final del caso " + caso);
            var titulo = GameObject.Find("Titulo").GetComponent<TMP_Text>().text;
            Assert.AreEqual(TextosResultado.Titulo(EstadoJuego.CategoriaFinal.Value), titulo, "título del informe del caso " + caso);

            // El informe va en dos pantallas: el veredicto primero y «Modificar mi misión» en el detalle.
            Assert.IsNull(GameObject.Find("BotonModificar"), "el veredicto no muestra todavía el detalle");
            Pulsar("BotonDetalle");
            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<ResultadoUI>().DetalleVisible, "«Ver detalles» abre el detalle");
            Pulsar("BotonModificar");
            yield return EsperarEscena("Mision");
            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<MisionUI>().VistaActivaEsConfiguracion, "Modificar debe abrir Configuración");
            CollectionAssert.AreEqual(elegidas, EstadoJuego.Configuracion.InstrumentoIds, "Modificar debe conservar los instrumentos");
            Assert.AreEqual(orbita, EstadoJuego.Configuracion.OrbitaId);
            Assert.AreEqual(lanzador, EstadoJuego.Configuracion.LanzadorId);
        }

        /// <summary>Responde al evento en pausa con la primera opción que se pueda elegir.</summary>
        static void Responder(OperacionUI ui)
        {
            var op = ui.Op;
            if (!op.EsperandoDecision) return;
            var opciones = op.Opciones.ToList();
            ui.Decidir(opciones.FindIndex(op.PuedeElegir));
        }

        static IEnumerator EsperarEscena(string nombre)
        {
            int cuadros = 0;
            while (SceneManager.GetActiveScene().name != nombre && cuadros++ < MaxCuadros) yield return null;
            Assert.AreEqual(nombre, SceneManager.GetActiveScene().name, "No se cargó la escena " + nombre);
            yield return null; // deja correr Awake y Start
        }

        static UnityEngine.UI.Button Boton(string nombre)
        {
            var go = GameObject.Find(nombre);
            Assert.IsNotNull(go, "No se encontró el botón activo " + nombre);
            return go.GetComponent<UnityEngine.UI.Button>();
        }

        static void Pulsar(string nombre)
        {
            var boton = Boton(nombre);
            Assert.IsTrue(boton.interactable, nombre + " no está habilitado");
            boton.onClick.Invoke();
        }

        static void PulsarOpcion(string texto)
        {
            var lista = GameObject.Find("ListaOpciones");
            Assert.IsNotNull(lista, "No hay lista de opciones visible");
            var boton = lista.GetComponentsInChildren<UnityEngine.UI.Button>()
                .FirstOrDefault(b => b.name != "PlantillaOpcion" && b.GetComponentInChildren<TMP_Text>().text.Contains(texto));
            Assert.IsNotNull(boton, "No hay opción con el texto " + texto);
            boton.onClick.Invoke();
        }
    }
}
