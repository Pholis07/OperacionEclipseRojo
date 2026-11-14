using SpaceMission;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Arma la interfaz de las escenas por código. Se usa desde el menú Herramientas o en modo batch
/// (-executeMethod ConstructorEscenas.ConstruirTodo). OJO: reconstruye las escenas desde cero, así que
/// los cambios hechos a mano en las escenas que construye se pierden al volver a ejecutarlo.
/// Desde el 2026-11-14 la escena Menu la edita Diego a mano: ConstruirTodo ya NO la toca.
/// Ver GUIA-INTERFAZ.md en la raíz del proyecto.
/// </summary>
public static class ConstructorEscenas
{
    // Paleta (estilo HUD oscuro con acento rojo marciano). Contraste del texto claro sobre el fondo > 15:1.
    static readonly Color Fondo = Hex("#0B0B10");
    static readonly Color Tarjeta = Hex("#1E1E23");
    static readonly Color TextoClaro = Hex("#F8FAFC");
    static readonly Color TextoSecundario = Hex("#94A3B8");
    static readonly Color Acento = Hex("#E2643A");
    static readonly Color TextoSobreAcento = Hex("#0B0B10");
    static readonly Color Horizonte = new Color(0.55f, 0.18f, 0.08f, 0.55f);

    const string RutaCatalogo = "Assets/Datos/Catalogo.asset";

    [MenuItem("Herramientas/Importar recursos de TextMeshPro")]
    public static void ImportarTMP()
    {
        // La importación es asíncrona. En modo batch (sin -quit) se cierra Unity cuando termina.
        if (Application.isBatchMode)
        {
            AssetDatabase.importPackageCompleted += _ => { Debug.Log("ImportarTMP: listo"); EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, error) => { Debug.LogError("ImportarTMP: " + error); EditorApplication.Exit(1); };
        }
        TMP_PackageResourceImporter.ImportResources(true, false, false);
    }

    /// <summary>Reconstruye las escenas que todavía genera el código (Mision y Resultado). No toca Menu.</summary>
    [MenuItem("Herramientas/Construir escenas (sin Menu)")]
    public static void ConstruirTodo()
    {
        ConstruirMision();
        ConstruirResultado();
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenas: listo");
    }

    // ---------- Menu ----------

    /// <summary>
    /// Versión original del Menú. Ya no se ejecuta sola: BORRA lo que se haya editado a mano en Menu.
    /// Solo para recuperarlo si se rompe por completo.
    /// </summary>
    [MenuItem("Herramientas/Recuperar Menu original (borra cambios manuales)")]
    public static void ConstruirMenu()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Recuperar Menu original",
                "Esto rehace la escena Menu desde cero y borra los cambios hechos a mano. ¿Seguir?", "Sí, rehacer", "Cancelar"))
            return;
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();

        var fondo = Panel(canvas, "Fondo", Fondo);
        Estirar(fondo);
        var horizonte = Panel(canvas, "HorizonteMarte", Horizonte);
        Anclar(horizonte, new Vector2(0, 0), new Vector2(1, 0.35f));

        var contenido = Columna(canvas, "Contenido", 32, TextAnchor.MiddleCenter);
        Anclar(contenido, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f));

        Texto(contenido, "Titulo", "Operación Eclipse Rojo", 96, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        Texto(contenido, "Premisa", "Una señal rara aparece en Marte y una tormenta de polvo dificulta la investigación.",
            40, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        var iniciar = Boton(contenido, "BotonIniciar", "Iniciar misión", true);
        var comoJugar = Boton(contenido, "BotonComoJugar", "Cómo jugar", false);

        // Panel «Cómo jugar», encima de todo y oculto al empezar.
        var panel = Panel(canvas, "PanelComoJugar", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 24, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        Texto(tarjeta, "TituloComoJugar", "Cómo jugar", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Texto(tarjeta, "Pasos",
            "1. Elige una órbita, un lanzador y los instrumentos científicos.\n" +
            "2. No te pases de los límites de masa, potencia, presupuesto y espacios.\n" +
            "3. El semáforo te avisa si puedes lanzar y si te falta alguna evidencia.\n" +
            "4. Al lanzar, sigue las transmisiones de la nave y recibe un informe que explica el resultado.",
            36, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        var cerrar = Boton(tarjeta, "BotonCerrar", "Entendido", true);

        CrearEventSystem();

        var controlador = new GameObject("MenuUI").AddComponent<MenuUI>();
        Asignar(controlador, "botonIniciar", iniciar);
        Asignar(controlador, "botonComoJugar", comoJugar);
        Asignar(controlador, "botonCerrarComoJugar", cerrar);
        Asignar(controlador, "panelComoJugar", panel.gameObject);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Menu.unity");
    }

    // ---------- Mision ----------

    static void ConstruirMision()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        // Vista Briefing
        var briefing = Panel(canvas, "VistaBriefing", Fondo);
        Estirar(briefing);
        var col = Columna(briefing, "Contenido", 20, TextAnchor.UpperLeft);
        Anclar(col, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f));

        Texto(col, "Titulo", "Briefing de la misión", 64, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Texto(col, "EtiquetaObjetivo", "OBJETIVO", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var objetivo = Texto(col, "Objetivo", "", 36, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Texto(col, "EtiquetaRequisitos", "REQUISITOS: LAS EVIDENCIAS QUE DEBES CONSEGUIR", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var requisitos = Texto(col, "Requisitos", "", 32, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);

        var fila = Fila(col, "LimitesYSemaforo", 32);
        var colLimites = Columna(fila, "Limites", 12, TextAnchor.UpperLeft, Tarjeta, 28);
        Texto(colLimites, "EtiquetaLimites", "LÍMITES", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var limites = Texto(colLimites, "TextoLimites", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        var colSemaforo = Columna(fila, "Semaforo", 12, TextAnchor.UpperLeft, Tarjeta, 28);
        Texto(colSemaforo, "EtiquetaSemaforo", "EL SEMÁFORO", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var semaforo = Texto(colSemaforo, "TextoSemaforo", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);

        // Selector de nivel (gameplay aprobado): los textos y colores los pone MisionUI con los datos.
        var filaNivel = Fila(col, "Niveles", 16);
        Alto(filaNivel, 72);
        var botonesNivel = new UnityEngine.UI.Button[3];
        for (int i = 0; i < 3; i++)
        {
            botonesNivel[i] = Boton(filaNivel, "BotonNivel" + (i + 1), "Nivel " + (i + 1), false);
            var le = botonesNivel[i].GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
            botonesNivel[i].GetComponentInChildren<TextMeshProUGUI>().fontSize = 30;
        }

        var continuar = Boton(col, "BotonContinuar", "Continuar", true);

        var controlador = new GameObject("MisionUI").AddComponent<MisionUI>();
        AsignarLista(controlador, "botonesNivel", botonesNivel);
        var nave = ConstruirNave();
        PrepararCamara();

        // Vista Configuración: transparente en el centro para que se vea la nave 3D.
        var configuracion = Vacio(canvas, "VistaConfiguracion");
        Estirar(configuracion);
        configuracion.SetAsFirstSibling(); // debajo del Briefing
        var configUI = ConstruirConfiguracion(configuracion, catalogo, controlador, nave);

        // Vista Revisión: tarjeta encima de la configuración.
        var revision = Panel(canvas, "VistaRevision", new Color(0, 0, 0, 0.85f));
        Estirar(revision);
        var revisionUI = ConstruirRevision(revision, catalogo, controlador);

        // Vista Operación (sustituye a la Investigación): mapa, energía, órbitas, eventos y herramientas.
        var investigacion = Panel(canvas, "VistaOperacion", Fondo);
        Estirar(investigacion);
        var operacionUI = ConstruirOperacion(investigacion, catalogo);

        CrearEventSystem();
        Asignar(controlador, "operacion", operacionUI);

        Asignar(controlador, "catalogo", catalogo);
        Asignar(controlador, "vistaBriefing", briefing.gameObject);
        Asignar(controlador, "vistaConfiguracion", configuracion.gameObject);
        Asignar(controlador, "vistaRevision", revision.gameObject);
        Asignar(controlador, "vistaInvestigacion", investigacion.gameObject);
        Asignar(controlador, "revision", revisionUI);
        Asignar(controlador, "textoObjetivo", objetivo);
        Asignar(controlador, "textoRequisitos", requisitos);
        Asignar(controlador, "textoLimites", limites);
        Asignar(controlador, "textoColores", semaforo);
        Asignar(controlador, "botonContinuar", continuar);

        // Al abrir la escena solo se ve el Briefing (MisionUI también lo hace al empezar).
        configuracion.gameObject.SetActive(false);
        revision.gameObject.SetActive(false);
        investigacion.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Mision.unity");
    }

    static OperacionUI ConstruirOperacion(RectTransform vista, CatalogoDatos catalogo)
    {
        var ui = vista.gameObject.AddComponent<OperacionUI>();

        // Izquierda: instrumentos, energía y órbitas.
        var izq = Columna(vista, "PanelInstrumentos", 14, TextAnchor.UpperLeft, Tarjeta, 24);
        Anclar(izq, new Vector2(0.012f, 0.03f), new Vector2(0.30f, 0.97f));
        Texto(izq, "Etiqueta", "INSTRUMENTOS", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        Texto(izq, "Ayuda", "Pulsa uno para encenderlo y escanear con él; otra vez para apagarlo.", 22,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        var lista = Columna(izq, "ListaInstrumentos", 8, TextAnchor.UpperLeft);
        lista.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var plantilla = Boton(lista, "PlantillaInstrumento", "Instrumento", false);
        Opcion(plantilla, 22);
        var energia = Texto(izq, "TextoEnergia", "Energía", 30, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var fondoBarra = Panel(izq, "BarraEnergia", Hex("#2A2A31"));
        Alto(fondoBarra, 28);
        var relleno = Panel(fondoBarra, "Relleno", Hex("#22C55E"));
        relleno.anchorMin = Vector2.zero; relleno.anchorMax = new Vector2(0, 1);
        relleno.pivot = new Vector2(0, 0.5f); relleno.offsetMin = Vector2.zero; relleno.offsetMax = Vector2.zero;
        var orbitas = Texto(izq, "TextoOrbitas", "Órbitas", 30, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);

        // Centro: mapa cuadrado que se puede pulsar, y la última lectura debajo.
        var zonaMapa = Vacio(vista, "ZonaMapa");
        Anclar(zonaMapa, new Vector2(0.31f, 0.2f), new Vector2(0.69f, 0.97f));
        var goMapa = new GameObject("Mapa", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
        goMapa.transform.SetParent(zonaMapa, false);
        var ajuste = goMapa.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        ajuste.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = 1f;
        var capaMarcas = Vacio(goMapa.transform, "Marcas");
        Estirar(capaMarcas);
        var mapa = goMapa.AddComponent<MapaMarteUI>();
        Asignar(mapa, "imagen", goMapa.GetComponent<UnityEngine.UI.RawImage>());
        Asignar(mapa, "capaMarcas", capaMarcas);

        var cajaLectura = Columna(vista, "CajaLectura", 6, TextAnchor.UpperLeft, Tarjeta, 20);
        Anclar(cajaLectura, new Vector2(0.31f, 0.03f), new Vector2(0.69f, 0.18f));
        var lectura = Texto(cajaLectura, "TextoLectura", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Derecha: transmisión, eventos, herramientas y fin.
        var der = Columna(vista, "PanelDerecho", 14, TextAnchor.UpperLeft, Tarjeta, 24);
        Anclar(der, new Vector2(0.70f, 0.03f), new Vector2(0.988f, 0.97f));
        Texto(der, "EtiquetaTransmision", "TRANSMISIÓN", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var transmision = Texto(der, "TextoTransmision", "", 22, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        transmision.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var progreso = Texto(der, "TextoProgreso", "", 24, TextoSecundario, FontStyles.Bold, TextAlignmentOptions.Left);
        Texto(der, "EtiquetaEventos", "EVENTOS DE LA NAVE", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var evento = Texto(der, "TextoEvento", "", 22, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Texto(der, "EtiquetaHerramientas", "HERRAMIENTAS (una vez cada una)", 24, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var herramientas = Columna(der, "ListaHerramientas", 6, TextAnchor.UpperLeft);
        var plantillaH = Boton(herramientas, "PlantillaHerramienta", "Herramienta", false);
        Opcion(plantillaH, 22);
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 52;
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52;
        var terminar = Boton(der, "BotonTerminar", "Terminar misión", true);
        terminar.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mapa", mapa);
        Asignar(ui, "listaInstrumentos", lista);
        Asignar(ui, "plantillaInstrumento", plantilla);
        Asignar(ui, "rellenoEnergia", relleno);
        Asignar(ui, "textoEnergia", energia);
        Asignar(ui, "textoOrbitas", orbitas);
        Asignar(ui, "textoLectura", lectura);
        Asignar(ui, "textoEvento", evento);
        Asignar(ui, "textoTransmision", transmision);
        Asignar(ui, "textoProgreso", progreso);
        Asignar(ui, "listaHerramientas", herramientas);
        Asignar(ui, "plantillaHerramienta", plantillaH);
        Asignar(ui, "botonTerminar", terminar);
        return ui;
    }

    /// <summary>Convierte un botón en fila de lista: ancho completo, 64 px y texto a la izquierda.</summary>
    static void Opcion(UnityEngine.UI.Button boton, float tamanoTexto)
    {
        var le = boton.GetComponent<UnityEngine.UI.LayoutElement>();
        le.preferredWidth = -1; le.minHeight = 64; le.preferredHeight = 64;
        var etiqueta = boton.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.fontSize = tamanoTexto; etiqueta.fontStyle = FontStyles.Normal; etiqueta.alignment = TextAlignmentOptions.Left;
        etiqueta.margin = new Vector4(16, 0, 16, 0);
    }

    // ---------- Resultado ----------

    static void ConstruirResultado()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        var fondo = Panel(canvas, "Fondo", Fondo);
        Estirar(fondo);

        // Izquierda: conclusión, recursos y botón.
        var izq = Columna(canvas, "Conclusion", 28, TextAnchor.UpperLeft);
        Anclar(izq, new Vector2(0.05f, 0.06f), new Vector2(0.48f, 0.94f));
        Texto(izq, "Etiqueta", "INFORME DE LA MISIÓN", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var titulo = Texto(izq, "Titulo", "", 72, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var conclusion = Texto(izq, "TextoConclusion", "", 34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        conclusion.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cajaRecursos = Columna(izq, "Recursos", 8, TextAnchor.UpperLeft, Tarjeta, 28);
        var recursos = Texto(cajaRecursos, "TextoRecursos", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var modificar = Boton(izq, "BotonModificar", "Modificar mi misión", true);

        // Derecha: evidencias, revelación y lo que faltó.
        var der = Columna(canvas, "Detalle", 28, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(der, new Vector2(0.52f, 0.06f), new Vector2(0.95f, 0.94f));
        var evidencias = Texto(der, "TextoEvidencias", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var faltantes = Texto(der, "TextoFaltantes", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var puntos = Texto(der, "TextoPuntos", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        CrearEventSystem();

        var ui = new GameObject("ResultadoUI").AddComponent<ResultadoUI>();
        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "textoTitulo", titulo);
        Asignar(ui, "textoConclusion", conclusion);
        Asignar(ui, "textoEvidencias", evidencias);
        Asignar(ui, "textoFaltantes", faltantes);
        Asignar(ui, "textoRecursos", recursos);
        Asignar(ui, "textoPuntos", puntos);
        Asignar(ui, "botonModificar", modificar);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Resultado.unity");
    }

    static ConfiguracionUI ConstruirConfiguracion(RectTransform vista, CatalogoDatos catalogo, MisionUI mision, NaveUI nave)
    {
        var ui = vista.gameObject.AddComponent<ConfiguracionUI>();

        // Panel izquierdo: pestañas, opciones y ayuda.
        var izq = Columna(vista, "PanelIzquierdo", 18, TextAnchor.UpperLeft, Tarjeta, 28);
        Anclar(izq, new Vector2(0.015f, 0.03f), new Vector2(0.36f, 0.97f));
        Texto(izq, "Titulo", "Configura tu misión", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var pestanas = Fila(izq, "Pestanas", 12);
        Alto(pestanas, 72);
        var pOrbita = Boton(pestanas, "PestanaOrbita", "Órbita", false);
        var pLanzador = Boton(pestanas, "PestanaLanzador", "Lanzador", false);
        var pInstrumentos = Boton(pestanas, "PestanaInstrumentos", "Instrumentos", false);
        foreach (var p in new[] { pOrbita, pLanzador, pInstrumentos })
        {
            var le = p.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
            p.GetComponentInChildren<TextMeshProUGUI>().fontSize = 30;
        }

        var lista = Columna(izq, "ListaOpciones", 10, TextAnchor.UpperLeft);
        Alto(lista, 420);
        var plantilla = Boton(lista, "PlantillaOpcion", "Opción", false);
        var lePlantilla = plantilla.GetComponent<UnityEngine.UI.LayoutElement>();
        lePlantilla.preferredWidth = -1; lePlantilla.minHeight = 72; lePlantilla.preferredHeight = 72;
        var etiqueta = plantilla.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.fontSize = 26; etiqueta.fontStyle = FontStyles.Normal; etiqueta.alignment = TextAlignmentOptions.Left;
        etiqueta.margin = new Vector4(20, 0, 20, 0);

        var cajaAyuda = Columna(izq, "Ayuda", 8, TextAnchor.UpperLeft, Hex("#15151A"), 20);
        var leAyuda = cajaAyuda.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leAyuda.flexibleHeight = 1;
        var ayuda = Texto(cajaAyuda, "TextoAyuda", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        var verBriefing = Boton(izq, "BotonBriefing", "Ver briefing", false);
        verBriefing.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        // Panel derecho: barras, semáforo y botón de revisión.
        var der = Columna(vista, "PanelDerecho", 16, TextAnchor.UpperLeft, Tarjeta, 28);
        Anclar(der, new Vector2(0.655f, 0.03f), new Vector2(0.985f, 0.97f));
        Texto(der, "EtiquetaRecursos", "RECURSOS", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);

        var rellenos = new RectTransform[4];
        var textos = new TMP_Text[4];
        string[] nombres = { "Masa", "Potencia", "Costo", "Espacios" };
        for (int i = 0; i < 4; i++)
        {
            textos[i] = Texto(der, "Texto" + nombres[i], nombres[i], 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
            var fondoBarra = Panel(der, "Barra" + nombres[i], Hex("#2A2A31"));
            Alto(fondoBarra, 24);
            var relleno = Panel(fondoBarra, "Relleno", TextoSecundario);
            relleno.anchorMin = Vector2.zero; relleno.anchorMax = new Vector2(0, 1);
            relleno.pivot = new Vector2(0, 0.5f); relleno.offsetMin = Vector2.zero; relleno.offsetMax = Vector2.zero;
            rellenos[i] = relleno;
        }

        Texto(der, "EtiquetaSemaforo", "SEMÁFORO", 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        var filaLuz = Fila(der, "FilaSemaforo", 16);
        Alto(filaLuz, 56);
        filaLuz.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        var luz = Panel(filaLuz, "Luz", Hex("#9CA3AF"));
        var leLuz = luz.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leLuz.preferredWidth = 56; leLuz.preferredHeight = 56;
        var estado = Texto(filaLuz, "TextoEstado", "GRIS", 40, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var mensaje = Texto(der, "TextoMensaje", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        mensaje.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var revisar = Boton(der, "BotonRevisar", "Revisar misión", true);
        revisar.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "nave", nave);
        Asignar(ui, "pestanaOrbita", pOrbita);
        Asignar(ui, "pestanaLanzador", pLanzador);
        Asignar(ui, "pestanaInstrumentos", pInstrumentos);
        Asignar(ui, "listaOpciones", lista);
        Asignar(ui, "plantillaOpcion", plantilla);
        Asignar(ui, "textoAyuda", ayuda);
        AsignarLista(ui, "rellenosBarra", rellenos);
        AsignarLista(ui, "textosBarra", textos);
        Asignar(ui, "luzEstado", luz.GetComponent<UnityEngine.UI.Image>());
        Asignar(ui, "textoEstado", estado);
        Asignar(ui, "textoMensaje", mensaje);
        Asignar(ui, "botonRevisar", revisar);
        Asignar(ui, "botonBriefing", verBriefing);
        return ui;
    }

    static RevisionUI ConstruirRevision(RectTransform vista, CatalogoDatos catalogo, MisionUI mision)
    {
        var ui = vista.gameObject.AddComponent<RevisionUI>();
        var tarjeta = Columna(vista, "Tarjeta", 24, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.92f));
        Texto(tarjeta, "Titulo", "Revisión de la misión", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var resumen = Texto(tarjeta, "TextoResumen", "", 30, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        resumen.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var botones = Fila(tarjeta, "Botones", 24);
        Alto(botones, 88);
        var corregir = Boton(botones, "BotonCorregir", "Corregir", false);
        var lanzar = Boton(botones, "BotonLanzar", "Lanzar", true);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "textoResumen", resumen);
        Asignar(ui, "botonCorregir", corregir);
        Asignar(ui, "botonLanzar", lanzar);
        return ui;
    }

    /// <summary>Nave fija hecha con primitivas: cuerpo, dos paneles solares y un módulo por instrumento.</summary>
    static NaveUI ConstruirNave()
    {
        var matCuerpo = CrearMaterial("Nave_Cuerpo", Hex("#B8BEC6"));
        var matPanel = CrearMaterial("Nave_Panel", Hex("#1E3A8A"));
        var matModulo = CrearMaterial("Nave_Modulo", Acento);

        var raiz = new GameObject("Nave");
        raiz.transform.position = new Vector3(0, 0, 0);
        raiz.transform.rotation = Quaternion.Euler(10, -25, 0);

        Primitiva(PrimitiveType.Cylinder, "Cuerpo", raiz.transform, Vector3.zero, new Vector3(1.2f, 1.4f, 1.2f), matCuerpo);
        Primitiva(PrimitiveType.Cube, "PanelIzquierdo", raiz.transform, new Vector3(-2.3f, 0.3f, 0), new Vector3(2.6f, 0.06f, 1.1f), matPanel);
        Primitiva(PrimitiveType.Cube, "PanelDerecho", raiz.transform, new Vector3(2.3f, 0.3f, 0), new Vector3(2.6f, 0.06f, 1.1f), matPanel);

        // Un módulo por instrumento, en el orden del catálogo. Aparecen al elegir el instrumento.
        string[] ids = { "termica", "radar", "alta_res", "atmosferico", "espectrometro" };
        Vector3[] posiciones =
        {
            new Vector3(0, -1.65f, 0), new Vector3(0.75f, -0.6f, -0.3f), new Vector3(-0.75f, -0.6f, -0.3f),
            new Vector3(0, 1.75f, 0), new Vector3(0, 0.4f, -0.75f)
        };
        var modulos = new GameObject[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            var forma = i == 3 ? PrimitiveType.Sphere : PrimitiveType.Cube;
            modulos[i] = Primitiva(forma, "Modulo_" + ids[i], raiz.transform, posiciones[i], Vector3.one * 0.45f, matModulo);
            modulos[i].SetActive(false);
        }

        var nave = raiz.AddComponent<NaveUI>();
        var so = new SerializedObject(nave);
        var pIds = so.FindProperty("idsInstrumento");
        var pModulos = so.FindProperty("modulos");
        pIds.arraySize = ids.Length;
        pModulos.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++)
        {
            pIds.GetArrayElementAtIndex(i).stringValue = ids[i];
            pModulos.GetArrayElementAtIndex(i).objectReferenceValue = modulos[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return nave;
    }

    static void PrepararCamara()
    {
        var camara = Camera.main;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = Fondo;
        camara.transform.position = new Vector3(0, 0.2f, -8.5f);
        camara.transform.rotation = Quaternion.identity;
    }

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static Material CrearMaterial(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
        var ruta = "Assets/Materiales/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void Alto(RectTransform rt, float alto)
    {
        var le = rt.GetComponent<UnityEngine.UI.LayoutElement>();
        if (le == null) le = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = alto;
        le.preferredHeight = alto;
    }

    static void AsignarLista(Object destino, string campo, Object[] valores)
    {
        var so = new SerializedObject(destino);
        var prop = so.FindProperty(campo);
        prop.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Piezas ----------

    static RectTransform CrearCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        escalador.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;
        return go.GetComponent<RectTransform>();
    }

    static void CrearEventSystem()
    {
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    static RectTransform Panel(Transform padre, string nombre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(padre, false);
        var imagen = go.GetComponent<UnityEngine.UI.Image>();
        imagen.color = color;
        // Los fondos no bloquean clics, salvo el panel modal, que sí debe taparlos.
        imagen.raycastTarget = color.a > 0.8f || nombre.StartsWith("Panel");
        return go.GetComponent<RectTransform>();
    }

    static RectTransform Columna(Transform padre, string nombre, float espacio, TextAnchor alineacion,
        Color? color = null, int relleno = 0)
    {
        var rt = color.HasValue ? Panel(padre, nombre, color.Value) : Vacio(padre, nombre);
        var grupo = rt.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        grupo.spacing = espacio;
        grupo.childAlignment = alineacion;
        grupo.padding = new RectOffset(relleno, relleno, relleno, relleno);
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = false;
        return rt;
    }

    static RectTransform Fila(Transform padre, string nombre, float espacio)
    {
        var rt = Vacio(padre, nombre);
        var grupo = rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        grupo.spacing = espacio;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = true;
        return rt;
    }

    static RectTransform Vacio(Transform padre, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tamano, Color color,
        FontStyles estilo, TextAlignmentOptions alineacion)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.color = color;
        tmp.fontStyle = estilo;
        tmp.alignment = alineacion;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    static UnityEngine.UI.Button Boton(Transform padre, string nombre, string texto, bool principal)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
        go.transform.SetParent(padre, false);
        var imagen = go.GetComponent<UnityEngine.UI.Image>();
        imagen.color = principal ? Acento : Tarjeta;
        var layout = go.GetComponent<UnityEngine.UI.LayoutElement>();
        layout.minHeight = 88;
        layout.preferredHeight = 88;
        layout.preferredWidth = 520;
        layout.flexibleWidth = 0;

        var boton = go.GetComponent<UnityEngine.UI.Button>();
        var colores = boton.colors;
        colores.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colores.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        boton.colors = colores;

        var etiqueta = Texto(go.transform, "Texto", texto, 40, principal ? TextoSobreAcento : TextoClaro,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(etiqueta.rectTransform);

        // En una columna centrada el botón no se estira a todo el ancho.
        var grupo = padre.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (grupo != null && grupo.childAlignment == TextAnchor.MiddleCenter)
            grupo.childForceExpandWidth = false;
        return boton;
    }

    static void Estirar(RectTransform rt) => Anclar(rt, Vector2.zero, Vector2.one);

    static void Anclar(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Asignar(Object destino, string campo, Object valor)
    {
        var so = new SerializedObject(destino);
        var prop = so.FindProperty(campo);
        if (prop == null) throw new System.ArgumentException("No existe el campo " + campo + " en " + destino.GetType().Name);
        prop.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
