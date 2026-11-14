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

    // Estilo HUD (referencias: Starfield y Vehicle Designer): líneas finas frías, paneles algo translúcidos
    // sobre la escena 3D y esquinas marcadas con el acento.
    static readonly Color LineaHud = Hex("#3B5568");
    static readonly Color TarjetaTranslucida = new Color(0.118f, 0.118f, 0.137f, 0.92f);
    static readonly Color CajaAyuda = Hex("#15151A");
    static readonly Color RejillaFondo = Hex("#0B0B10");
    static readonly Color RejillaLinea = Hex("#1C2A38");
    static readonly Color RejillaLineaMayor = Hex("#2C4256");
    static readonly Color Marte = Hex("#A8472A");
    const float GrosorLinea = 2f;
    const float LargoEsquina = 48f;
    const float GrosorEsquina = 4f;

    static readonly string[] NombresPasos = { "Briefing", "Configuración", "Revisión", "Investigación" };

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

    /// <summary>
    /// Menú estilo HUD (referencia: Starfield): acciones en lista a la izquierda, nave 3D girando sobre Marte
    /// a la derecha. Punto de partida para que Diego lo retoque a mano; BORRA lo editado a mano en Menu.
    /// Respeta lo que piden las pruebas: MenuUI con sus 4 referencias, un EventSystem y Canvas 1920x1080.
    /// </summary>
    [MenuItem("Herramientas/Diseñar Menu estilo HUD (borra cambios manuales)")]
    public static void DisenarMenuHud()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Diseñar Menu estilo HUD",
                "Esto rehace la escena Menu con el diseño HUD y borra los cambios hechos a mano. ¿Seguir?", "Sí, rehacer", "Cancelar"))
            return;
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 3D: Marte abajo a la derecha, la nave completa girando encima y la rejilla del hangar.
        PrepararCamara();
        var marte = Primitiva(PrimitiveType.Sphere, "Marte", null, new Vector3(4.5f, -13.5f, 8f), Vector3.one * 22f,
            CrearMaterial("Marte", Marte));
        marte.AddComponent<GiroLento>();
        var naveUI = ConstruirNave();
        var nave = naveUI.transform;
        Object.DestroyImmediate(naveUI); // en el Menú la nave es decoración: sin lógica de módulos
        foreach (Transform hijo in nave) hijo.gameObject.SetActive(true); // todos los módulos a la vista
        nave.position = new Vector3(3.2f, 0.6f, 0);
        nave.localScale = Vector3.one * 0.9f;
        nave.gameObject.AddComponent<GiroLento>();

        var canvas = CrearCanvas();

        // Velo oscuro a la izquierda para que el texto se lea sobre la escena.
        var velo = Panel(canvas, "Velo", new Color(Fondo.r, Fondo.g, Fondo.b, 0.75f));
        Anclar(velo, Vector2.zero, new Vector2(0.46f, 1));
        Bordes(velo, LineaHud);

        var contenido = Columna(canvas, "Contenido", 28, TextAnchor.UpperLeft);
        Anclar(contenido, new Vector2(0.05f, 0.12f), new Vector2(0.42f, 0.9f));
        Encabezado(contenido, "Etiqueta", "SIMULACIÓN DE DISEÑO DE MISIONES");
        Texto(contenido, "Titulo", "Operación\nEclipse Rojo", 96, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Texto(contenido, "Premisa", "Una señal rara aparece en Marte y una tormenta de polvo dificulta la investigación.",
            36, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        var hueco = Vacio(contenido, "Espacio");
        Alto(hueco, 40);
        var iniciar = Boton(contenido, "BotonIniciar", "Iniciar misión", true);
        var comoJugar = Boton(contenido, "BotonComoJugar", "Cómo jugar", false);
        contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;

        var pie = Texto(canvas, "Pie", "Los valores del juego son ficticios: no son capacidades ni precios oficiales de NASA.",
            24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(pie.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.45f, 0.09f));
        var teclas = Texto(canvas, "Teclas", "<b>ENTER</b>  Seleccionar     <b>FLECHAS</b>  Moverse",
            24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        Anclar(teclas.rectTransform, new Vector2(0.55f, 0.03f), new Vector2(0.96f, 0.09f));

        // Panel «Cómo jugar», encima de todo (último hijo del Canvas) y oculto al empezar.
        var panel = Panel(canvas, "PanelComoJugar", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 24, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        MarcoHud(tarjeta);
        Texto(tarjeta, "TituloComoJugar", "Cómo jugar", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        var pasos = Texto(tarjeta, "Pasos",
            "1. Elige una órbita, un lanzador y los instrumentos científicos.\n" +
            "2. No te pases de los límites de masa, potencia, presupuesto y espacios.\n" +
            "3. El semáforo te avisa si puedes lanzar y si te falta alguna evidencia.\n" +
            "4. Al lanzar, sigue las transmisiones de la nave y recibe un informe que explica el resultado.",
            36, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        pasos.lineSpacing = 20;
        var cerrar = Boton(tarjeta, "BotonCerrar", "Entendido", true);
        panel.gameObject.SetActive(false);

        CrearEventSystem();
        // Con el foco puesto en «Iniciar misión», Enter y las flechas funcionan desde el principio.
        Object.FindAnyObjectByType<EventSystem>().firstSelectedGameObject = iniciar.gameObject;

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

        // Vista Briefing: expediente de la misión. Izquierda lo que hay que lograr; derecha los datos de consulta.
        var briefing = Panel(canvas, "VistaBriefing", Fondo);
        Estirar(briefing);

        var ruta = Texto(briefing, "Ruta", Ruta(1), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(ruta.rectTransform, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.97f));
        var lineaRuta = Panel(briefing, "LineaRuta", LineaHud);
        lineaRuta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Anclar(lineaRuta, new Vector2(0.05f, 0.915f), new Vector2(0.95f, 0.915f));
        lineaRuta.offsetMax = new Vector2(0, GrosorLinea);

        var col = Columna(briefing, "Expediente", 20, TextAnchor.UpperLeft);
        Anclar(col, new Vector2(0.05f, 0.17f), new Vector2(0.51f, 0.88f));
        Texto(col, "Titulo", "Briefing de la misión", 64, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Encabezado(col, "EtiquetaObjetivo", "OBJETIVO");
        var objetivo = Texto(col, "Objetivo", "", 36, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Encabezado(col, "EtiquetaRequisitos", "REQUISITOS: LAS EVIDENCIAS QUE DEBES CONSEGUIR");
        var requisitos = Texto(col, "Requisitos", "", 32, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        requisitos.lineSpacing = 20;

        var datos = Columna(briefing, "Datos", 28, TextAnchor.UpperLeft);
        Anclar(datos, new Vector2(0.55f, 0.17f), new Vector2(0.95f, 0.88f));
        var colLimites = Columna(datos, "Limites", 12, TextAnchor.UpperLeft, Tarjeta, 28);
        MarcoHud(colLimites);
        Encabezado(colLimites, "EtiquetaLimites", "LÍMITES");
        var limites = Texto(colLimites, "TextoLimites", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        var colSemaforo = Columna(datos, "Semaforo", 12, TextAnchor.UpperLeft, Tarjeta, 28);
        MarcoHud(colSemaforo);
        Encabezado(colSemaforo, "EtiquetaSemaforo", "EL SEMÁFORO");
        var semaforo = Texto(colSemaforo, "TextoSemaforo", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);

        // Barra inferior: qué hacer ahora y el único botón de la pantalla.
        var pista = Texto(briefing, "Pista", "Lee el objetivo y los requisitos. Después, configura tu nave.", 26,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(pista.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.6f, 0.13f));
        var continuar = Boton(briefing, "BotonContinuar", "Continuar", true);
        Fijar(continuar.GetComponent<RectTransform>(), new Vector2(0.95f, 0.05f), new Vector2(1, 0), new Vector2(520, 88));

        var controlador = new GameObject("MisionUI").AddComponent<MisionUI>();
        var nave = ConstruirNave();
        // Más pequeña que en el Menú para que quepa entera entre los dos paneles de Configuración.
        nave.transform.position = new Vector3(0, 0.4f, 0);
        nave.transform.localScale = Vector3.one * 0.55f;
        ConstruirSuelo();
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

        // Vista Investigación: las transmisiones de una en una.
        var investigacion = Panel(canvas, "VistaInvestigacion", Fondo);
        Estirar(investigacion);
        var investigacionUI = ConstruirInvestigacion(investigacion, catalogo);

        CrearEventSystem();
        Asignar(controlador, "investigacion", investigacionUI);

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

    static InvestigacionUI ConstruirInvestigacion(RectTransform vista, CatalogoDatos catalogo)
    {
        var ui = vista.gameObject.AddComponent<InvestigacionUI>();
        var tarjeta = Columna(vista, "Tarjeta", 32, TextAnchor.UpperLeft, Tarjeta, 56);
        Anclar(tarjeta, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
        MarcoHud(tarjeta);
        Texto(tarjeta, "Paso", Paso(4), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Encabezado(tarjeta, "Etiqueta", "INVESTIGACIÓN EN CURSO");
        var encabezado = Texto(tarjeta, "Encabezado", "", 36, TextoSecundario, FontStyles.Bold, TextAlignmentOptions.Left);
        var texto = Texto(tarjeta, "TextoTransmision", "", 40, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        texto.lineSpacing = 10;
        texto.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var continuar = Boton(tarjeta, "BotonContinuar", "Continuar investigación", true);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "textoEncabezado", encabezado);
        Asignar(ui, "textoTransmision", texto);
        Asignar(ui, "botonContinuar", continuar);
        return ui;
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
        Encabezado(izq, "Etiqueta", "INFORME DE LA MISIÓN");
        var titulo = Texto(izq, "Titulo", "", 72, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        var conclusion = Texto(izq, "TextoConclusion", "", 34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        conclusion.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cajaRecursos = Columna(izq, "Recursos", 8, TextAnchor.UpperLeft, Tarjeta, 28);
        Bordes(cajaRecursos, LineaHud);
        var recursos = Texto(cajaRecursos, "TextoRecursos", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var modificar = Boton(izq, "BotonModificar", "Modificar mi misión", true);

        // Derecha: evidencias, revelación y lo que faltó.
        var der = Columna(canvas, "Detalle", 28, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(der, new Vector2(0.52f, 0.06f), new Vector2(0.95f, 0.94f));
        MarcoHud(der);
        var evidencias = Texto(der, "TextoEvidencias", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var faltantes = Texto(der, "TextoFaltantes", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        CrearEventSystem();

        var ui = new GameObject("ResultadoUI").AddComponent<ResultadoUI>();
        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "textoTitulo", titulo);
        Asignar(ui, "textoConclusion", conclusion);
        Asignar(ui, "textoEvidencias", evidencias);
        Asignar(ui, "textoFaltantes", faltantes);
        Asignar(ui, "textoRecursos", recursos);
        Asignar(ui, "botonModificar", modificar);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Resultado.unity");
    }

    static ConfiguracionUI ConstruirConfiguracion(RectTransform vista, CatalogoDatos catalogo, MisionUI mision, NaveUI nave)
    {
        var ui = vista.gameObject.AddComponent<ConfiguracionUI>();

        // Panel izquierdo: pestañas, opciones y ayuda.
        var izq = Columna(vista, "PanelIzquierdo", 18, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Anclar(izq, new Vector2(0.015f, 0.03f), new Vector2(0.36f, 0.97f));
        MarcoHud(izq);
        Texto(izq, "Titulo", "Configura tu misión", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(izq);
        var pestanas = Fila(izq, "Pestanas", 12);
        Alto(pestanas, 72);
        var pOrbita = Boton(pestanas, "PestanaOrbita", "Órbita", false);
        var pLanzador = Boton(pestanas, "PestanaLanzador", "Lanzador", false);
        var pInstrumentos = Boton(pestanas, "PestanaInstrumentos", "Instrumentos", false);
        foreach (var p in new[] { pOrbita, pLanzador, pInstrumentos })
        {
            var le = p.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
            var textoPestana = p.GetComponentInChildren<TextMeshProUGUI>();
            textoPestana.fontSize = 30;
            // «Instrumentos» no cabe a 30 pt en negrita: se reduce sola, nunca por debajo de 24 pt.
            textoPestana.enableAutoSizing = true;
            textoPestana.fontSizeMin = 24;
            textoPestana.fontSizeMax = 30;
            textoPestana.textWrappingMode = TextWrappingModes.NoWrap;
        }

        var lista = Columna(izq, "ListaOpciones", 10, TextAnchor.UpperLeft);
        Alto(lista, 420);
        var plantilla = Boton(lista, "PlantillaOpcion", "Opción", false);
        var lePlantilla = plantilla.GetComponent<UnityEngine.UI.LayoutElement>();
        lePlantilla.preferredWidth = -1; lePlantilla.minHeight = 72; lePlantilla.preferredHeight = 72;
        var etiqueta = plantilla.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.fontSize = 26; etiqueta.fontStyle = FontStyles.Normal; etiqueta.alignment = TextAlignmentOptions.Left;
        etiqueta.margin = new Vector4(20, 0, 20, 0);

        var cajaAyuda = Columna(izq, "Ayuda", 8, TextAnchor.UpperLeft, CajaAyuda, 20);
        Bordes(cajaAyuda, LineaHud);
        var leAyuda = cajaAyuda.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leAyuda.flexibleHeight = 1;
        var ayuda = Texto(cajaAyuda, "TextoAyuda", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        var verBriefing = Boton(izq, "BotonBriefing", "Ver briefing", false);
        verBriefing.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        // Panel derecho: barras, semáforo y botón de revisión.
        var der = Columna(vista, "PanelDerecho", 16, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Anclar(der, new Vector2(0.655f, 0.03f), new Vector2(0.985f, 0.97f));
        MarcoHud(der);
        Encabezado(der, "EtiquetaRecursos", "RECURSOS");

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

        Encabezado(der, "EtiquetaSemaforo", "SEMÁFORO");
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

        // Centro: queda libre para la nave; solo el paso actual arriba y una leyenda abajo.
        var paso = Texto(vista, "Paso", Paso(2), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        Anclar(paso.rectTransform, new Vector2(0.37f, 0.91f), new Vector2(0.645f, 0.97f));
        var leyenda = Texto(vista, "LeyendaNave", "VISTA DE LA NAVE\n<size=24>Cada instrumento elegido aparece como un módulo rojo.</size>",
            26, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        Anclar(leyenda.rectTransform, new Vector2(0.37f, 0.04f), new Vector2(0.645f, 0.13f));

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
        MarcoHud(tarjeta);
        Texto(tarjeta, "Paso", Paso(3), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Texto(tarjeta, "Titulo", "Revisión de la misión", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
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
        le.flexibleHeight = 0; // sin esto la fila crece con el espacio sobrante de la columna
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
        colores.selectedColor = colores.highlightedColor; // el foco del teclado se ve igual que el ratón encima
        boton.colors = colores;

        var etiqueta = Texto(go.transform, "Texto", texto, 40, principal ? TextoSobreAcento : TextoClaro,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(etiqueta.rectTransform);
        // Los secundarios llevan marco fino para que se lean como botón aunque estén sobre una tarjeta.
        if (!principal) Bordes(go.GetComponent<RectTransform>(), LineaHud);

        // En una columna centrada el botón no se estira a todo el ancho.
        var grupo = padre.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (grupo != null && grupo.childAlignment == TextAnchor.MiddleCenter)
            grupo.childForceExpandWidth = false;
        return boton;
    }

    // ---------- Piezas HUD ----------

    /// <summary>Marco de línea fina alrededor de un panel. No participa en el layout.</summary>
    static void Bordes(RectTransform rt, Color color)
    {
        Trazo(rt, "BordeArriba", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -GrosorLinea), Vector2.zero, color);
        Trazo(rt, "BordeAbajo", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, GrosorLinea), color);
        Trazo(rt, "BordeIzquierdo", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(GrosorLinea, 0), color);
        Trazo(rt, "BordeDerecho", new Vector2(1, 0), Vector2.one, new Vector2(-GrosorLinea, 0), Vector2.zero, color);
    }

    /// <summary>Escuadra de acento en la esquina superior izquierda, como los marcos de Starfield.</summary>
    static void Esquina(RectTransform rt)
    {
        var arriba = new Vector2(0, 1);
        Trazo(rt, "EsquinaH", arriba, arriba, new Vector2(0, -GrosorEsquina), new Vector2(LargoEsquina, 0), Acento);
        Trazo(rt, "EsquinaV", arriba, arriba, new Vector2(0, -LargoEsquina), new Vector2(GrosorEsquina, 0), Acento);
    }

    /// <summary>Panel HUD completo: marco fino y escuadra de acento.</summary>
    static void MarcoHud(RectTransform rt)
    {
        Bordes(rt, LineaHud);
        Esquina(rt);
    }

    static void Trazo(RectTransform padre, string nombre, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
        go.transform.SetParent(padre, false);
        go.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var imagen = go.GetComponent<UnityEngine.UI.Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = offMin;
        r.offsetMax = offMax;
    }

    /// <summary>Línea horizontal fina dentro de una columna (subrayado de las etiquetas).</summary>
    static void Linea(Transform padre)
    {
        var linea = Panel(padre, "Linea", LineaHud);
        linea.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Alto(linea, GrosorLinea);
    }

    /// <summary>Etiqueta en mayúsculas con el color de acento y subrayada.</summary>
    static TextMeshProUGUI Encabezado(Transform padre, string nombre, string texto)
    {
        var t = Texto(padre, nombre, texto, 28, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        t.characterSpacing = 2;
        Linea(padre);
        return t;
    }

    /// <summary>«1 BRIEFING · 2 CONFIGURACIÓN · …» con el paso actual resaltado.</summary>
    static string Ruta(int pasoActual)
    {
        var partes = new string[NombresPasos.Length];
        for (int i = 0; i < NombresPasos.Length; i++)
        {
            var paso = (i + 1) + "  " + NombresPasos[i].ToUpperInvariant();
            partes[i] = i + 1 == pasoActual
                ? "<color=#" + ColorUtility.ToHtmlStringRGB(Acento) + "><b>" + paso + "</b></color>"
                : paso;
        }
        return string.Join("     ·     ", partes);
    }

    static string Paso(int pasoActual) =>
        "PASO " + pasoActual + " DE " + NombresPasos.Length + "  ·  " + NombresPasos[pasoActual - 1].ToUpperInvariant();

    /// <summary>Coloca un elemento suelto (fuera de un layout) con tamaño fijo.</summary>
    static void Fijar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 tamano)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.sizeDelta = tamano;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>Suelo con rejilla de plano técnico bajo la nave (como el hangar de Starfield).</summary>
    static void ConstruirSuelo()
    {
        var mat = CrearMaterialRejilla();
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloRejilla";
        Object.DestroyImmediate(suelo.GetComponent<Collider>());
        suelo.transform.position = new Vector3(0, -2.6f, 6f);
        suelo.transform.localScale = new Vector3(8, 1, 8); // 80 x 80 unidades
        suelo.GetComponent<Renderer>().sharedMaterial = mat;
        suelo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Material CrearMaterialRejilla()
    {
        const string rutaTextura = "Assets/Materiales/Rejilla.png";
        const int lado = 128;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, true);
        for (int y = 0; y < lado; y++)
        for (int x = 0; x < lado; x++)
        {
            bool mayor = x < 2 || y < 2;
            bool menor = x % 32 == 0 || y % 32 == 0;
            tex.SetPixel(x, y, mayor ? RejillaLineaMayor : menor ? RejillaLinea : RejillaFondo);
        }
        tex.Apply();
        if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
        System.IO.File.WriteAllBytes(rutaTextura, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(rutaTextura);
        var importador = (TextureImporter)AssetImporter.GetAtPath(rutaTextura);
        importador.wrapMode = TextureWrapMode.Repeat;
        importador.mipmapEnabled = true;
        importador.anisoLevel = 8;
        importador.SaveAndReimport();

        const string rutaMat = "Assets/Materiales/Suelo_Rejilla.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, rutaMat);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura));
        mat.SetTextureScale("_BaseMap", new Vector2(20, 20)); // celdas mayores de 4 unidades
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
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
