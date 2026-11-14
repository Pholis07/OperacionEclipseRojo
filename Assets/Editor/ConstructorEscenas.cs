using System.Collections.Generic;
using SpaceMission;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Arma la interfaz de las escenas por código. Se usa desde el menú Herramientas o en modo batch
/// (-executeMethod ConstructorEscenas.ConstruirTodo). OJO: reconstruye las escenas desde cero, así que
/// los cambios hechos a mano en las escenas que construye se pierden al volver a ejecutarlo.
/// Desde el 2026-11-14 la escena Menu la edita Diego a mano: ConstruirTodo ya NO la toca.
/// Ver GUIA-INTERFAZ.md en la raíz del proyecto.
///
/// Estilo (fase de arte): HUD con fuentes Rajdhani/Barlow/IBM Plex Mono e iconos Lucide (ConstructorEstilo),
/// botones con chaflán, fondos con fotos reales de NASA, Marte texturizado con atmósfera, estrellas,
/// nave de primitivas con materiales PBR, posprocesado (Bloom, ACES, viñeta) y animaciones que nunca
/// bloquean la lógica (ver Assets/Scripts/UI/Estilo).
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

    // Estilo HUD (referencias: Starfield y Vehicle Designer).
    static readonly Color Cian = Hex("#5EC8E5");                 // solo datos e indicadores
    static readonly Color LineaHud = Hex("#3B5568");
    static readonly Color TarjetaTranslucida = new Color(0.118f, 0.118f, 0.137f, 0.9f);
    static readonly Color CajaAyuda = new Color(0.082f, 0.082f, 0.102f, 0.95f);
    static readonly Color FondoBarra = Hex("#15161C");
    const float GrosorLinea = 2f;
    const float LargoEsquina = 48f;
    const float GrosorEsquina = 4f;

    static readonly string[] NombresPasos = { "Briefing", "Configuración", "Revisión", "Operación" };

    const string RutaCatalogo = "Assets/Datos/Catalogo.asset";

    /// <summary>Plataforma de lanzamiento de la escena Mision: lejos de la nave y del planeta, fuera de su encuadre.</summary>
    static readonly Vector3 Plataforma = new Vector3(70f, 0f, 0f);

    // Recursos de estilo, cargados por PrepararEstilo().
    static ArteDatos arte;
    static Sprite sChaflan, sChaflanBorde, sCirculo, sHalo;
    static Texture2D tSegmentos, tBarrido;

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
        PrepararEstilo();
        ConstruirMision();
        ConstruirResultado();
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenas: listo");
    }

    static void PrepararEstilo()
    {
        ConstructorEstilo.Asegurar();
        arte = ConstructorEstilo.Arte();
        sChaflan = ConstructorEstilo.Sprite("Chaflan_Relleno");
        sChaflanBorde = ConstructorEstilo.Sprite("Chaflan_Borde");
        sCirculo = ConstructorEstilo.Sprite("Circulo");
        sHalo = ConstructorEstilo.Sprite("Halo");
        tSegmentos = ConstructorEstilo.Imagen("Interfaz/Segmentos");
        tBarrido = ConstructorEstilo.Imagen("Interfaz/Barrido");
    }

    // ---------- Menu ----------

    /// <summary>
    /// Versión original del Menú (fase 5). Ya no se ejecuta sola: BORRA lo que se haya editado a mano en Menu.
    /// Solo para recuperarlo si se rompe por completo.
    /// </summary>
    [MenuItem("Herramientas/Recuperar Menu original (borra cambios manuales)")]
    public static void ConstruirMenu()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Recuperar Menu original",
                "Esto rehace la escena Menu desde cero y borra los cambios hechos a mano. ¿Seguir?", "Sí, rehacer", "Cancelar"))
            return;
        PrepararEstilo();
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

        var (panel, cerrar) = ConstruirComoJugar(canvas);

        CrearEventSystem();

        var controlador = new GameObject("MenuUI").AddComponent<MenuUI>();
        Asignar(controlador, "botonIniciar", iniciar);
        Asignar(controlador, "botonComoJugar", comoJugar);
        Asignar(controlador, "botonCerrarComoJugar", cerrar);
        Asignar(controlador, "panelComoJugar", panel.gameObject);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Menu.unity");
    }

    /// <summary>
    /// Menú estilo HUD (referencia: Starfield): pantalla de título, acciones en lista a la izquierda,
    /// la nave girando sobre Marte con estrellas, datos de la misión y Opciones. Punto de partida para que
    /// Diego lo retoque a mano; BORRA lo editado a mano en Menu.
    /// Respeta lo que piden las pruebas: MenuUI con sus 4 referencias, un EventSystem y Canvas 1920x1080.
    /// </summary>
    [MenuItem("Herramientas/Diseñar Menu estilo HUD (borra cambios manuales)")]
    public static void DisenarMenuHud()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Diseñar Menu estilo HUD",
                "Esto rehace la escena Menu con el diseño HUD y borra los cambios hechos a mano. ¿Seguir?", "Sí, rehacer", "Cancelar"))
            return;
        PrepararEstilo();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 3D: estrellas, Marte con atmósfera a la derecha y la nave completa girando encima.
        PrepararCamara();
        ConstruirEstrellas();
        var marte = ConstruirMarte(new Vector3(5.5f, -12.5f, 9f), 21f);
        Asignar(marte.AddComponent<GiroLento>(), "gradosPorSegundo", 1.5f);
        var naveUI = ConstruirNave();
        var nave = naveUI.transform;
        Object.DestroyImmediate(naveUI); // en el Menú la nave es decoración: sin lógica de módulos
        foreach (Transform hijo in nave)
        {
            hijo.gameObject.SetActive(true); // todos los módulos a la vista
            var instalacion = hijo.GetComponent<Instalacion>();
            if (instalacion != null) Object.DestroyImmediate(instalacion);
        }
        var pivote = new GameObject("PivoteNave").transform;
        pivote.position = new Vector3(3.4f, 0.9f, 0);
        pivote.gameObject.AddComponent<Parallax>();
        nave.SetParent(pivote, false);
        nave.localPosition = Vector3.zero;
        nave.localScale = Vector3.one * 0.85f;
        nave.gameObject.AddComponent<GiroLento>();
        ConstruirIluminacion();

        var canvas = CrearCanvas();

        // Velo oscuro a la izquierda para que el texto se lea sobre la escena.
        var velo = Panel(canvas, "Velo", new Color(Fondo.r, Fondo.g, Fondo.b, 0.8f));
        Anclar(velo, Vector2.zero, new Vector2(0.44f, 1));
        Trazo(velo, "BordeDerecho", new Vector2(1, 0), Vector2.one, new Vector2(-GrosorLinea, 0), Vector2.zero, LineaHud);

        var contenido = Columna(canvas, "Contenido", 26, TextAnchor.UpperLeft);
        Anclar(contenido, new Vector2(0.05f, 0.1f), new Vector2(0.41f, 0.9f));
        Encabezado(contenido, "Etiqueta", "SIMULACIÓN DE DISEÑO DE MISIONES");
        var titulo = Texto(contenido, "Titulo", "Operación\nEclipse Rojo", 88, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.fontStyle = FontStyles.UpperCase;
        titulo.lineSpacing = -8;
        titulo.characterSpacing = 3;
        Texto(contenido, "Premisa", "Una señal rara aparece en Marte y una tormenta de polvo dificulta la investigación.",
            34, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Alto(Vacio(contenido, "Espacio"), 24);
        var iniciar = Boton(contenido, "BotonIniciar", SpaceMission.Iconos.Lanzador + "   Iniciar misión", true);
        var comoJugar = Boton(contenido, "BotonComoJugar", SpaceMission.Iconos.Informe + "   Cómo jugar", false);
        var opciones = Boton(contenido, "BotonOpciones", SpaceMission.Iconos.Ajustes + "   Opciones", false);
        contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;

        // Datos de la misión a la derecha, como las fichas de Starfield (texto de ambientación, sin números del juego).
        var ficha = Columna(canvas, "FichaMision", 6, TextAnchor.UpperRight);
        Anclar(ficha, new Vector2(0.6f, 0.78f), new Vector2(0.96f, 0.95f));
        Dato(ficha, "DESTINO", "Marte · órbita");
        Dato(ficha, "ESTADO", "Tormenta de polvo activa");
        Dato(ficha, "SEÑAL", "Anomalía térmica sin explicar");

        var pie = Texto(canvas, "Pie", "Los valores del juego son ficticios: no son capacidades ni precios oficiales de NASA. " +
            "Imágenes de Marte: NASA/JPL-Caltech (ver créditos).", 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(pie.rectTransform, new Vector2(0.05f, 0.025f), new Vector2(0.43f, 0.1f));
        var teclas = Texto(canvas, "Teclas", Tecla("ENTER") + " Seleccionar     " + Tecla("↑ ↓") + " Moverse",
            24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        Anclar(teclas.rectTransform, new Vector2(0.55f, 0.03f), new Vector2(0.96f, 0.09f));

        // Paneles encima de todo, en este orden: Opciones, Cómo jugar y el velo de título.
        var opcionesUI = ConstruirOpciones(canvas, opciones);
        var (panel, cerrar) = ConstruirComoJugar(canvas);
        ConstruirPantallaTitulo(canvas, iniciar.gameObject);

        CrearEventSystem();
        // Con el foco puesto en «Iniciar misión», Enter y las flechas funcionan desde el principio.
        Object.FindAnyObjectByType<EventSystem>().firstSelectedGameObject = iniciar.gameObject;
        CrearMusica(ArteDatos.Musica.Menu);

        var controlador = new GameObject("MenuUI").AddComponent<MenuUI>();
        Asignar(controlador, "botonIniciar", iniciar);
        Asignar(controlador, "botonComoJugar", comoJugar);
        Asignar(controlador, "botonCerrarComoJugar", cerrar);
        Asignar(controlador, "panelComoJugar", panel.gameObject);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Menu.unity");
    }

    static void Dato(Transform padre, string etiqueta, string valor)
    {
        var t = Texto(padre, "Dato" + etiqueta, "<color=#" + ColorUtility.ToHtmlStringRGB(Cian) + ">" + etiqueta + "</color>   " + valor,
            24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        t.font = ConstructorEstilo.Mono;
        t.fontStyle = FontStyles.UpperCase;
    }

    static string Tecla(string tecla) =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(TextoClaro) + "><b>[ " + tecla + " ]</b></color>";

    static (RectTransform panel, UnityEngine.UI.Button cerrar) ConstruirComoJugar(RectTransform canvas)
    {
        // Panel «Cómo jugar», encima de todo y oculto al empezar.
        var panel = Panel(canvas, "PanelComoJugar", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 24, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.82f));
        MarcoHud(tarjeta);
        Texto(tarjeta, "TituloComoJugar", "Cómo jugar", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        string PasoJuego(string icono, string texto) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(Cian) + ">" + icono + "</color>  " + texto;
        var pasos = Texto(tarjeta, "Pasos",
            PasoJuego(SpaceMission.Iconos.Orbita, "Elige una órbita, un lanzador y los instrumentos científicos.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Masa, "No te pases de los límites de masa, potencia, presupuesto y espacios.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Verde, "El semáforo te avisa si puedes lanzar y si te falta alguna evidencia.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Senal, "Al lanzar, sigue las transmisiones de la nave y recibe un informe que explica el resultado."),
            34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        pasos.lineSpacing = 30;
        pasos.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cerrar = Boton(tarjeta, "BotonCerrar", "Entendido", true);
        panel.gameObject.SetActive(false);
        return (panel, cerrar);
    }

    static OpcionesUI ConstruirOpciones(RectTransform canvas, UnityEngine.UI.Button abrir)
    {
        var panel = Panel(canvas, "PanelOpciones", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 22, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.84f));
        MarcoHud(tarjeta);
        Texto(tarjeta, "TituloOpciones", "Opciones", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        var musica = Deslizador(tarjeta, "Musica", SpaceMission.Iconos.Volumen + "  Música");
        var efectos = Deslizador(tarjeta, "Efectos", SpaceMission.Iconos.Volumen + "  Efectos de la interfaz");
        var voz = Deslizador(tarjeta, "Voz", SpaceMission.Iconos.Senal + "  Voz de las transmisiones");
        var movimiento = Casilla(tarjeta, "ReducirMovimiento", "Reducir movimiento (sin sacudidas ni animaciones)");
        Alto(Vacio(tarjeta, "Espacio"), 8).GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cerrar = Boton(tarjeta, "BotonCerrarOpciones", "Listo", true);
        panel.gameObject.SetActive(false);

        // OpcionesUI conecta los botones en Awake: vive en un objeto siempre activo, no en el panel (que empieza oculto).
        var anfitrion = new GameObject("Opciones", typeof(RectTransform));
        anfitrion.transform.SetParent(canvas, false);
        var ui = anfitrion.AddComponent<OpcionesUI>();
        Asignar(ui, "botonAbrir", abrir);
        Asignar(ui, "botonCerrar", cerrar);
        Asignar(ui, "panel", panel.gameObject);
        Asignar(ui, "musica", musica);
        Asignar(ui, "efectos", efectos);
        Asignar(ui, "voz", voz);
        Asignar(ui, "reducirMovimiento", movimiento);
        return ui;
    }

    static UnityEngine.UI.Slider Deslizador(Transform padre, string nombre, string etiqueta)
    {
        Texto(padre, "Etiqueta" + nombre, etiqueta, 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        var go = UnityEngine.UI.DefaultControls.CreateSlider(new UnityEngine.UI.DefaultControls.Resources());
        go.name = "Deslizador" + nombre;
        go.transform.SetParent(padre, false);
        Alto((RectTransform)go.transform, 32);
        go.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().color = FondoBarra;
        go.transform.Find("Fill Area/Fill").GetComponent<UnityEngine.UI.Image>().color = Acento;
        var asa = go.transform.Find("Handle Slide Area/Handle").GetComponent<UnityEngine.UI.Image>();
        asa.sprite = sCirculo;
        asa.color = TextoClaro;
        ((RectTransform)asa.transform).sizeDelta = new Vector2(32, 0);
        var s = go.GetComponent<UnityEngine.UI.Slider>();
        var colores = s.colors;
        colores.highlightedColor = Cian;
        colores.selectedColor = Cian;
        s.colors = colores;
        return s;
    }

    static UnityEngine.UI.Toggle Casilla(Transform padre, string nombre, string etiqueta)
    {
        var go = UnityEngine.UI.DefaultControls.CreateToggle(new UnityEngine.UI.DefaultControls.Resources());
        go.name = "Casilla" + nombre;
        go.transform.SetParent(padre, false);
        Alto((RectTransform)go.transform, 48);
        Object.DestroyImmediate(go.transform.Find("Label").gameObject); // la etiqueta por defecto no es TextMeshPro
        var fondo = (RectTransform)go.transform.Find("Background");
        fondo.anchorMin = fondo.anchorMax = new Vector2(0, 0.5f);
        fondo.pivot = new Vector2(0, 0.5f);
        fondo.sizeDelta = new Vector2(40, 40);
        fondo.anchoredPosition = Vector2.zero;
        fondo.GetComponent<UnityEngine.UI.Image>().color = FondoBarra;
        var marca = fondo.Find("Checkmark").GetComponent<UnityEngine.UI.Image>();
        marca.color = Acento;
        marca.sprite = null;
        ((RectTransform)marca.transform).sizeDelta = new Vector2(-12, -12);
        Bordes(fondo, LineaHud);
        var t = Texto(go.transform, "Texto", etiqueta, 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(t.rectTransform, Vector2.zero, Vector2.one);
        t.rectTransform.offsetMin = new Vector2(60, 0);
        return go.GetComponent<UnityEngine.UI.Toggle>();
    }

    static void ConstruirPantallaTitulo(RectTransform canvas, GameObject foco)
    {
        var velo = Panel(canvas, "PantallaTitulo", new Color(Fondo.r, Fondo.g, Fondo.b, 0.93f));
        Estirar(velo);
        velo.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var col = Columna(velo, "Contenido", 18, TextAnchor.MiddleCenter);
        Anclar(col, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.75f));
        var etiqueta = Texto(col, "Etiqueta", "NASA SPACE APPS CHALLENGE 2026  ·  EQUIPO GANGBANGERS", 26, Cian,
            FontStyles.Normal, TextAlignmentOptions.Center);
        etiqueta.font = ConstructorEstilo.Mono;
        var titulo = Texto(col, "TituloJuego", "Operación Eclipse Rojo", 136, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        titulo.fontStyle = FontStyles.UpperCase;
        titulo.characterSpacing = 6;
        var aviso = Texto(velo, "Aviso", "PULSA CUALQUIER TECLA PARA COMENZAR", 30, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        aviso.characterSpacing = 8;
        Anclar(aviso.rectTransform, new Vector2(0.1f, 0.14f), new Vector2(0.9f, 0.22f));
        var ui = velo.gameObject.AddComponent<PantallaTitulo>();
        Asignar(ui, "aviso", aviso);
        Asignar(ui, "focoAlCerrar", foco);
    }

    // ---------- Mision ----------

    static void ConstruirMision()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        // Vista Briefing: expediente de la misión sobre una foto real de una tormenta de polvo.
        var briefing = Panel(canvas, "VistaBriefing", Fondo);
        Estirar(briefing);
        FondoFoto(briefing, ConstructorEstilo.Imagen("Fondos/Fondo_Briefing"));

        var ruta = Texto(briefing, "Ruta", Ruta(1), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        ruta.font = ConstructorEstilo.Mono;
        Anclar(ruta.rectTransform, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.97f));
        var lineaRuta = Panel(briefing, "LineaRuta", LineaHud);
        lineaRuta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Anclar(lineaRuta, new Vector2(0.05f, 0.915f), new Vector2(0.95f, 0.915f));
        lineaRuta.offsetMax = new Vector2(0, GrosorLinea);

        var col = Columna(briefing, "Expediente", 18, TextAnchor.UpperLeft);
        Anclar(col, new Vector2(0.05f, 0.15f), new Vector2(0.51f, 0.885f));
        col.gameObject.AddComponent<CanvasGroup>();
        Texto(col, "Titulo", "Briefing de la misión", 64, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Miniatura(col, "Region", ConstructorEstilo.Imagen("Evidencias/Evidencia_region"), 120,
            SpaceMission.Iconos.Objetivo + "  REGIÓN OBSERVADA  ·  NASA/JPL-Caltech/UArizona");
        Encabezado(col, "EtiquetaObjetivo", "OBJETIVO");
        var objetivo = Texto(col, "Objetivo", "", 34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Encabezado(col, "EtiquetaRequisitos", "REQUISITOS: LAS EVIDENCIAS QUE DEBES CONSEGUIR");
        var requisitos = Texto(col, "Requisitos", "", 30, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        requisitos.lineSpacing = 12;

        // Selector de nivel (gameplay aprobado): los textos y colores los pone MisionUI con los datos.
        Encabezado(col, "EtiquetaNivel", "NIVEL DE DIFICULTAD");
        var filaNivel = Fila(col, "Niveles", 16);
        Alto(filaNivel, 64);
        var botonesNivel = new UnityEngine.UI.Button[3];
        for (int i = 0; i < 3; i++)
        {
            botonesNivel[i] = Boton(filaNivel, "BotonNivel" + (i + 1), "Nivel " + (i + 1), false);
            var le = botonesNivel[i].GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 64; le.preferredHeight = 64;
            var t = botonesNivel[i].GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 28; t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = 28;
        }

        var datos = Columna(briefing, "Datos", 28, TextAnchor.UpperLeft);
        Anclar(datos, new Vector2(0.55f, 0.15f), new Vector2(0.95f, 0.885f));
        datos.gameObject.AddComponent<CanvasGroup>();
        var colLimites = Columna(datos, "Limites", 12, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        MarcoHud(colLimites);
        Encabezado(colLimites, "EtiquetaLimites", "LÍMITES");
        var limites = Texto(colLimites, "TextoLimites", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        limites.lineSpacing = 12;
        var colSemaforo = Columna(datos, "Semaforo", 12, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        MarcoHud(colSemaforo);
        Encabezado(colSemaforo, "EtiquetaSemaforo", "EL SEMÁFORO");
        var semaforo = Texto(colSemaforo, "TextoSemaforo", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        semaforo.lineSpacing = 12;

        // Barra inferior: qué hacer ahora y el único botón de la pantalla.
        var pista = Texto(briefing, "Pista", "Lee el objetivo y los requisitos. Después, configura tu nave.", 26,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(pista.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.6f, 0.12f));
        var continuar = Boton(briefing, "BotonContinuar", "Continuar  " + SpaceMission.Iconos.Siguiente, true);
        Fijar(continuar.GetComponent<RectTransform>(), new Vector2(0.95f, 0.05f), new Vector2(1, 0), new Vector2(520, 88));
        Aparecer(briefing, col, datos);

        var controlador = new GameObject("MisionUI").AddComponent<MisionUI>();
        AsignarLista(controlador, "botonesNivel", botonesNivel);

        // Escena 3D de Configuración: estrellas, Marte abajo con la órbita elegida y la nave en el centro.
        PrepararCamara();
        ConstruirEstrellas();
        var marte = ConstruirMarte(new Vector3(0, -17.5f, 16f), 26f);
        Asignar(marte.AddComponent<GiroLento>(), "gradosPorSegundo", 1.5f);
        ConstruirAnilloOrbita(marte.transform);
        var nave = ConstruirNave();
        // Más pequeña que en el Menú para que quepa entera entre los dos paneles de Configuración.
        nave.transform.position = new Vector3(0, 0.55f, 0);
        nave.transform.localScale = Vector3.one * 0.5f;
        var cohetes = ConstruirCohetes(Plataforma);
        ConstruirEstrellas(Plataforma + new Vector3(0, 4f, 60f));
        ConstruirIluminacion();

        // Vista Configuración: transparente en el centro para que se vea la nave 3D.
        var configuracion = Vacio(canvas, "VistaConfiguracion");
        Estirar(configuracion);
        configuracion.SetAsFirstSibling(); // debajo del Briefing
        var configUI = ConstruirConfiguracion(configuracion, catalogo, controlador, nave);
        var marteCentro = marte.transform.position;

        // Vista Revisión: tarjeta encima de la configuración.
        var revision = Panel(canvas, "VistaRevision", new Color(0, 0, 0, 0.8f));
        Estirar(revision);
        var revisionUI = ConstruirRevision(revision, catalogo, controlador);

        // Vista Operación (sustituye a la Investigación): mapa, energía, órbitas, eventos y herramientas,
        // con la imagen real del instrumento de cada transmisión.
        var investigacion = Panel(canvas, "VistaOperacion", Fondo);
        Estirar(investigacion);
        var operacionUI = ConstruirOperacion(investigacion, catalogo);

        var musica = CrearMusica(ArteDatos.Musica.Configuracion);
        ConstruirEfectoLanzamiento(canvas, revisionUI, musica);

        // Navegación de Configuración y cámara que se acerca a lo que se está eligiendo.
        Asignar(configuracion.GetComponent<GuiaConfiguracion>(), "vistaRevision", revision.gameObject);
        var camara = Camera.main.gameObject.AddComponent<CamaraConfiguracion>();
        Asignar(camara, "configuracion", configUI);
        Asignar(camara, "vistaConfiguracion", configuracion.gameObject);
        AsignarEncuadre(camara, "nave", new Vector3(0, 0.45f, -7.4f), new Vector3(0, 0.5f, 0), 60f);
        AsignarEncuadre(camara, "planeta", marteCentro + new Vector3(0, 12f, -45f), marteCentro + new Vector3(0, 1.5f, 0), 60f);
        AsignarEncuadre(camara, "plataforma", Plataforma + new Vector3(0, 5.3f, -17f), Plataforma + new Vector3(0, 5f, 0), 60f);
        Asignar(camara, "raizNave", nave.transform);
        Asignar(camara, "coheteExplorador", cohetes.explorador);
        Asignar(camara, "coheteTitan", cohetes.titan);
        Asignar(camara, "resaltadoExplorador", cohetes.resaltadoExplorador);
        Asignar(camara, "resaltadoTitan", cohetes.resaltadoTitan);
        Asignar(camara, "catalogo", catalogo);
        var etiqueta = Columna(configuracion, "EtiquetaInstalado", 2, TextAnchor.MiddleCenter, new Color(Fondo.r, Fondo.g, Fondo.b, 0.85f), 14);
        etiqueta.anchorMin = etiqueta.anchorMax = new Vector2(0.5f, 0.5f);
        etiqueta.sizeDelta = new Vector2(440, 84);
        etiqueta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        etiqueta.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childControlHeight = true;
        MarcoHud(etiqueta);
        var textoEtiqueta = Texto(etiqueta, "Texto", "", 26, Cian, FontStyles.Bold, TextAlignmentOptions.Center);
        etiqueta.gameObject.SetActive(false);
        Asignar(camara, "etiquetaFoco", etiqueta);
        Asignar(camara, "textoFoco", textoEtiqueta);

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
        FondoFoto(vista, ConstructorEstilo.Imagen("Texturas/Estrellas"), 0.9f, false);

        // Izquierda: instrumentos, energía, órbitas y la imagen de la última señal.
        var izq = Columna(vista, "PanelInstrumentos", 12, TextAnchor.UpperLeft, TarjetaTranslucida, 24);
        Anclar(izq, new Vector2(0.012f, 0.03f), new Vector2(0.30f, 0.97f));
        izq.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(izq);
        Encabezado(izq, "Etiqueta", SpaceMission.Iconos.Instrumentos + "  INSTRUMENTOS");
        Texto(izq, "Ayuda", "Pulsa uno para encenderlo y escanear con él; otra vez para apagarlo.", 24,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        var lista = Columna(izq, "ListaInstrumentos", 8, TextAnchor.UpperLeft);
        lista.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var plantilla = Boton(lista, "PlantillaInstrumento", "Instrumento", false);
        Opcion(plantilla, 24);

        var filaEnergia = Fila(izq, "FilaEnergia", 10);
        Alto(filaEnergia, 40);
        filaEnergia.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        var iconoEnergia = Texto(filaEnergia, "Icono", SpaceMission.Iconos.Potencia, 30, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
        iconoEnergia.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 36;
        var energia = Texto(filaEnergia, "TextoEnergia", "Energía", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        energia.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        var fondoBarra = Panel(izq, "BarraEnergia", FondoBarra);
        Alto(fondoBarra, 22);
        Bordes(fondoBarra, LineaHud);
        var relleno = Panel(fondoBarra, "Relleno", Hex("#22C55E"));
        relleno.anchorMin = Vector2.zero; relleno.anchorMax = new Vector2(0, 1);
        relleno.pivot = new Vector2(0, 0.5f); relleno.offsetMin = new Vector2(2, 2); relleno.offsetMax = new Vector2(0, -2);
        relleno.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        relleno.gameObject.AddComponent<BarraAnimada>();
        var segmentos = new GameObject("Segmentos", typeof(RectTransform), typeof(UnityEngine.UI.RawImage))
            .GetComponent<UnityEngine.UI.RawImage>();
        segmentos.transform.SetParent(fondoBarra, false);
        segmentos.texture = tSegmentos;
        segmentos.uvRect = new Rect(0, 0, 10, 1);
        segmentos.raycastTarget = false;
        Estirar(segmentos.rectTransform);

        var filaOrbitas = Fila(izq, "FilaOrbitas", 10);
        Alto(filaOrbitas, 40);
        filaOrbitas.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        var iconoOrbitas = Texto(filaOrbitas, "Icono", SpaceMission.Iconos.Orbita, 30, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
        iconoOrbitas.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 36;
        var orbitas = Texto(filaOrbitas, "TextoOrbitas", "Órbitas", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        orbitas.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;

        // Imagen real (NASA) del instrumento que envió la última transmisión.
        Encabezado(izq, "EtiquetaSenal", SpaceMission.Iconos.Senal + "  ÚLTIMA SEÑAL");
        var marcoImagen = Vacio(izq, "MarcoImagen");
        Alto(marcoImagen, 170);
        var imagen = new GameObject("Imagen", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        imagen.transform.SetParent(marcoImagen, false);
        imagen.raycastTarget = false;
        // Recorte centrado: las evidencias son 1024 x 640 y el marco es más apaisado.
        float aspectoMarco = (0.288f * 1920f - 48f) / 170f, aspectoFoto = 1024f / 640f;
        float alturaUv = Mathf.Min(1f, aspectoFoto / aspectoMarco);
        imagen.uvRect = new Rect(0, (1 - alturaUv) / 2f, 1, alturaUv);
        Estirar(imagen.rectTransform);
        var veloCredito = Panel(marcoImagen, "VeloCredito", new Color(0, 0, 0, 0.6f));
        veloCredito.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Anclar(veloCredito, Vector2.zero, new Vector2(1, 0.36f));
        var credito = Texto(marcoImagen, "Credito", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(credito.rectTransform, Vector2.zero, new Vector2(1, 0.36f));
        credito.margin = new Vector4(12, 0, 12, 0);
        credito.textWrappingMode = TextWrappingModes.NoWrap;
        credito.overflowMode = TextOverflowModes.Ellipsis;
        var veloFuente = Panel(marcoImagen, "VeloFuente", new Color(0, 0, 0, 0.6f));
        veloFuente.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Anclar(veloFuente, new Vector2(0, 0.7f), Vector2.one);
        var fuente = Texto(marcoImagen, "Fuente", "", 24, Cian, FontStyles.Bold, TextAlignmentOptions.Left);
        Anclar(fuente.rectTransform, new Vector2(0, 0.7f), Vector2.one);
        fuente.margin = new Vector4(16, 0, 12, 0);
        fuente.textWrappingMode = TextWrappingModes.NoWrap;
        fuente.overflowMode = TextOverflowModes.Ellipsis;
        var senal = Panel(marcoImagen, "SenalRecibida", new Color(Cian.r, Cian.g, Cian.b, 0.85f));
        Estirar(senal);
        senal.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var grupoSenal = senal.gameObject.AddComponent<CanvasGroup>();
        grupoSenal.alpha = 0f;
        grupoSenal.blocksRaycasts = false;
        var textoSenal = Texto(senal, "Texto", SpaceMission.Iconos.Senal + "   SEÑAL RECIBIDA", 32, Fondo, FontStyles.Bold,
            TextAlignmentOptions.Center);
        textoSenal.characterSpacing = 8;
        Estirar(textoSenal.rectTransform);
        MarcoHud(marcoImagen);

        // Centro: el paso, el mapa cuadrado que se puede pulsar y la última lectura debajo.
        var paso = Texto(vista, "Paso", Paso(4), 24, TextoSecundario,
            FontStyles.Normal, TextAlignmentOptions.Center);
        paso.font = ConstructorEstilo.Mono;
        Anclar(paso.rectTransform, new Vector2(0.31f, 0.925f), new Vector2(0.69f, 0.97f));
        var zonaMapa = Vacio(vista, "ZonaMapa");
        Anclar(zonaMapa, new Vector2(0.31f, 0.2f), new Vector2(0.69f, 0.915f));
        var goMapa = new GameObject("Mapa", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
        goMapa.transform.SetParent(zonaMapa, false);
        var ajuste = goMapa.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        ajuste.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = 1f;
        var capaMarcas = Vacio(goMapa.transform, "Marcas");
        Estirar(capaMarcas);
        MarcoHud((RectTransform)goMapa.transform);
        var mapa = goMapa.AddComponent<MapaMarteUI>();
        Asignar(mapa, "imagen", goMapa.GetComponent<UnityEngine.UI.RawImage>());
        Asignar(mapa, "capaMarcas", capaMarcas);
        Asignar(mapa, "texturaBase", ConstructorEstilo.Imagen("Texturas/Marte_Superficie"));
        var soMapa = new SerializedObject(mapa);
        soMapa.FindProperty("tamanoZona").floatValue = 0.18f;   // ~740x740 píxeles del mapa de 4096x2048: casi 1:1 en pantalla
        soMapa.ApplyModifiedPropertiesWithoutUndo();

        var cajaLectura = Columna(vista, "CajaLectura", 6, TextAnchor.UpperLeft, TarjetaTranslucida, 20);
        Anclar(cajaLectura, new Vector2(0.31f, 0.03f), new Vector2(0.69f, 0.185f));
        Bordes(cajaLectura, LineaHud);
        var lectura = Texto(cajaLectura, "TextoLectura", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        // Derecha: transmisión, eventos, herramientas y fin.
        var der = Columna(vista, "PanelDerecho", 12, TextAnchor.UpperLeft, TarjetaTranslucida, 24);
        Anclar(der, new Vector2(0.70f, 0.03f), new Vector2(0.988f, 0.97f));
        der.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(der);
        Encabezado(der, "EtiquetaTransmision", SpaceMission.Iconos.Senal + "  TRANSMISIÓN");
        var transmision = Texto(der, "TextoTransmision", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        transmision.lineSpacing = 4;
        transmision.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        transmision.gameObject.AddComponent<Escritura>();
        var progreso = Texto(der, "TextoProgreso", "", 24, Cian, FontStyles.Normal, TextAlignmentOptions.Left);
        progreso.font = ConstructorEstilo.Mono;
        Encabezado(der, "EtiquetaEventos", SpaceMission.Iconos.Amarillo + "  EVENTOS DE LA NAVE");
        var evento = Texto(der, "TextoEvento", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Encabezado(der, "EtiquetaHerramientas", SpaceMission.Iconos.Ajustes + "  HERRAMIENTAS (UN USO)");
        var herramientas = Columna(der, "ListaHerramientas", 6, TextAnchor.UpperLeft);
        var plantillaH = Boton(herramientas, "PlantillaHerramienta", "Herramienta", false);
        Opcion(plantillaH, 24);
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 52;
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52;
        var terminar = Boton(der, "BotonTerminar", "Terminar misión", true);
        terminar.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;
        Aparecer(vista, izq, der);

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

        var visorUI = vista.gameObject.AddComponent<VisorEvidencia>();
        Asignar(visorUI, "catalogo", catalogo);
        Asignar(visorUI, "arte", arte);
        Asignar(visorUI, "operacion", ui);
        Asignar(visorUI, "imagen", imagen);
        Asignar(visorUI, "textoFuente", fuente);
        Asignar(visorUI, "textoCredito", credito);
        Asignar(visorUI, "senal", grupoSenal);
        return ui;
    }

    /// <summary>Convierte un botón en fila de lista: ancho completo, 64 px y texto normal a la izquierda.</summary>
    static void Opcion(UnityEngine.UI.Button boton, float tamanoTexto)
    {
        var le = boton.GetComponent<UnityEngine.UI.LayoutElement>();
        le.preferredWidth = -1; le.minHeight = 64; le.preferredHeight = 64;
        var etiqueta = boton.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.font = ConstructorEstilo.Texto;
        etiqueta.fontSize = tamanoTexto;
        etiqueta.fontStyle = FontStyles.Normal;
        etiqueta.characterSpacing = 0;
        etiqueta.lineSpacing = -8;
        etiqueta.alignment = TextAlignmentOptions.Left;
        etiqueta.margin = new Vector4(16, 0, 16, 0);
        Asignar(boton.GetComponent<BotonVivo>(), "escalaEncima", 1.015f);
    }

    static void ConstruirEfectoLanzamiento(RectTransform canvas, RevisionUI revision, MusicaEscena musica)
    {
        var efecto = Panel(canvas, "EfectoLanzamiento", new Color(1f, 0.62f, 0.4f, 0.45f));
        Estirar(efecto);
        efecto.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var texto = Texto(efecto, "Texto", "LANZAMIENTO", 120, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        Estirar(texto.rectTransform);
        var ui = efecto.gameObject.AddComponent<EfectoLanzamiento>();
        var grupo = efecto.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        Asignar(ui, "botonLanzar", revision.BotonLanzar);
        Asignar(ui, "texto", texto);
        Asignar(ui, "musica", musica);
    }

    // ---------- Resultado ----------

    static void ConstruirResultado()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        var fondo = Panel(canvas, "Fondo", Fondo);
        Estirar(fondo);
        FondoFoto(fondo, ConstructorEstilo.Imagen("Fondos/Fondo_Resultado"));

        // Izquierda: conclusión, recursos y botón.
        var izq = Columna(canvas, "Conclusion", 26, TextAnchor.UpperLeft);
        Anclar(izq, new Vector2(0.05f, 0.06f), new Vector2(0.48f, 0.94f));
        Encabezado(izq, "Etiqueta", SpaceMission.Iconos.Informe + "  INFORME DE LA MISIÓN");
        var titulo = Texto(izq, "Titulo", "", 80, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.fontStyle = FontStyles.UpperCase;
        var conclusion = Texto(izq, "TextoConclusion", "", 32, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        conclusion.lineSpacing = 10;
        conclusion.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cajaRecursos = Columna(izq, "Recursos", 8, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Bordes(cajaRecursos, LineaHud);
        var recursos = Texto(cajaRecursos, "TextoRecursos", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var modificar = Boton(izq, "BotonModificar", "Modificar mi misión", true); // texto exacto: lo comprueban las pruebas

        // Derecha: evidencias, revelación y lo que faltó.
        var der = Columna(canvas, "Detalle", 28, TextAnchor.UpperLeft, TarjetaTranslucida, 40);
        Anclar(der, new Vector2(0.52f, 0.06f), new Vector2(0.95f, 0.94f));
        MarcoHud(der);
        var evidencias = Texto(der, "TextoEvidencias", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        evidencias.lineSpacing = 8;
        var faltantes = Texto(der, "TextoFaltantes", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        faltantes.lineSpacing = 8;
        var puntos = Texto(der, "TextoPuntos", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        puntos.lineSpacing = 8;
        Alto(Vacio(der, "Espacio"), 0).GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        Miniatura(der, "Mision", arte.imagenMision, 200, SpaceMission.Iconos.Senal + "  MARTE DURANTE LA TORMENTA  ·  NASA/JPL-Caltech/MSSS");

        CrearEventSystem();
        var musica = CrearMusica(ArteDatos.Musica.Ninguna);
        Asignar(musica, "segunResultado", true);

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
        var izq = Columna(vista, "PanelIzquierdo", 16, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Anclar(izq, new Vector2(0.015f, 0.03f), new Vector2(0.36f, 0.97f));
        izq.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(izq);
        Texto(izq, "Titulo", "Configura tu misión", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(izq);
        var pestanas = Fila(izq, "Pestanas", 10);
        Alto(pestanas, 72);
        var pOrbita = Boton(pestanas, "PestanaOrbita", SpaceMission.Iconos.Orbita + " Órbita", false);
        // GuiaConfiguracion añade a cada pestaña el ✓ cuando esa parte está elegida.
        var pLanzador = Boton(pestanas, "PestanaLanzador", SpaceMission.Iconos.Lanzador + " Lanzador", false);
        var pInstrumentos = Boton(pestanas, "PestanaInstrumentos", SpaceMission.Iconos.Instrumentos + " Instrumentos", false);
        foreach (var p in new[] { pOrbita, pLanzador, pInstrumentos })
        {
            var le = p.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
            var textoPestana = p.GetComponentInChildren<TextMeshProUGUI>();
            textoPestana.fontSize = 30;
            textoPestana.characterSpacing = 0;
            // Si no cabe, se reduce sola, nunca por debajo de 24 pt.
            textoPestana.enableAutoSizing = true;
            textoPestana.fontSizeMin = 24;
            textoPestana.fontSizeMax = 30;
            textoPestana.textWrappingMode = TextWrappingModes.NoWrap;
            textoPestana.margin = new Vector4(10, 0, 10, 0);
            // Las pestañas usan su propio sonido.
            Asignar(p.GetComponent<BotonVivo>(), "sonidoPulsar", ArteDatos.Sonido.Pestana);
        }

        var lista = Columna(izq, "ListaOpciones", 10, TextAnchor.UpperLeft);
        Alto(lista, 5 * 78 + 4 * 8);
        lista.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 8;
        var plantilla = Boton(lista, "PlantillaOpcion", "Opción", false);
        var lePlantilla = plantilla.GetComponent<UnityEngine.UI.LayoutElement>();
        lePlantilla.preferredWidth = -1; lePlantilla.minHeight = 78; lePlantilla.preferredHeight = 78;
        var etiqueta = plantilla.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.font = ConstructorEstilo.Texto;
        etiqueta.fontSize = 27;
        etiqueta.fontStyle = FontStyles.Normal;
        etiqueta.characterSpacing = 0;
        etiqueta.alignment = TextAlignmentOptions.Left;
        etiqueta.lineSpacing = -6;
        etiqueta.margin = new Vector4(20, 0, 16, 0);
        Asignar(plantilla.GetComponent<BotonVivo>(), "escalaEncima", 1.015f);

        var cajaAyuda = Columna(izq, "Ayuda", 8, TextAnchor.UpperLeft, CajaAyuda, 20);
        Bordes(cajaAyuda, LineaHud);
        var leAyuda = cajaAyuda.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leAyuda.flexibleHeight = 1;
        var ayuda = Texto(cajaAyuda, "TextoAyuda", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        ayuda.lineSpacing = 0;

        var filaNavegacion = Fila(izq, "Navegacion", 12);
        Alto(filaNavegacion, 64);
        var verBriefing = Boton(filaNavegacion, "BotonBriefing", SpaceMission.Iconos.Atras + "  Briefing", false);
        var siguiente = Boton(filaNavegacion, "BotonSiguiente", "Lanzador  " + SpaceMission.Iconos.Siguiente, true);
        foreach (var b in new[] { verBriefing, siguiente })
        {
            var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 64; le.preferredHeight = 64;
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 30; t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = 30;
        }

        // Panel derecho: barras, semáforo y botón de revisión.
        var der = Columna(vista, "PanelDerecho", 14, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Anclar(der, new Vector2(0.655f, 0.03f), new Vector2(0.985f, 0.97f));
        der.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(der);
        Encabezado(der, "EtiquetaRecursos", "RECURSOS");

        var rellenos = new RectTransform[4];
        var textos = new TMP_Text[4];
        string[] nombres = { "Masa", "Potencia", "Costo", "Espacios" };
        string[] iconos = { SpaceMission.Iconos.Masa, SpaceMission.Iconos.Potencia, SpaceMission.Iconos.Costo, SpaceMission.Iconos.Espacios };
        for (int i = 0; i < 4; i++)
        {
            var fila = Fila(der, "Fila" + nombres[i], 12);
            Alto(fila, 40);
            var grupoFila = fila.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            grupoFila.childForceExpandWidth = false;
            var icono = Texto(fila, "Icono" + nombres[i], iconos[i], 30, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
            icono.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;
            textos[i] = Texto(fila, "Texto" + nombres[i], nombres[i], 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
            textos[i].gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;

            var fondoBarra = Panel(der, "Barra" + nombres[i], FondoBarra);
            Alto(fondoBarra, 20);
            Bordes(fondoBarra, LineaHud);
            var relleno = Panel(fondoBarra, "Relleno", TextoSecundario);
            relleno.anchorMin = Vector2.zero; relleno.anchorMax = new Vector2(0, 1);
            relleno.pivot = new Vector2(0, 0.5f); relleno.offsetMin = new Vector2(2, 2); relleno.offsetMax = new Vector2(0, -2);
            relleno.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            relleno.gameObject.AddComponent<BarraAnimada>();
            // Segmentos encima del relleno (10 bloques), como las barras de Starfield.
            var segmentos = new GameObject("Segmentos", typeof(RectTransform), typeof(UnityEngine.UI.RawImage))
                .GetComponent<UnityEngine.UI.RawImage>();
            segmentos.transform.SetParent(fondoBarra, false);
            segmentos.texture = tSegmentos;
            segmentos.uvRect = new Rect(0, 0, 10, 1);
            segmentos.raycastTarget = false;
            Estirar(segmentos.rectTransform);
            rellenos[i] = relleno;
        }

        Alto(Vacio(der, "Espacio"), 6);
        Encabezado(der, "EtiquetaSemaforo", "SEMÁFORO");
        var filaLuz = Fila(der, "FilaSemaforo", 20);
        Alto(filaLuz, 72);
        filaLuz.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        filaLuz.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        var contenedorLuz = Vacio(filaLuz, "ContenedorLuz");
        var leLuz = contenedorLuz.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leLuz.preferredWidth = 64; leLuz.preferredHeight = 64;
        var halo = new GameObject("Halo", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        halo.transform.SetParent(contenedorLuz, false);
        halo.sprite = sHalo;
        halo.color = new Color(0.61f, 0.64f, 0.69f, 0.18f);
        halo.raycastTarget = false;
        Anclar(halo.rectTransform, new Vector2(-0.22f, -0.22f), new Vector2(1.22f, 1.22f)); // el halo apenas sobresale
        var luz = new GameObject("Luz", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        luz.transform.SetParent(contenedorLuz, false);
        luz.sprite = sCirculo;
        luz.color = Hex("#9CA3AF");
        luz.raycastTarget = false;
        Estirar(luz.rectTransform);
        var iconoLuz = Texto(luz.transform, "Icono", SpaceMission.Iconos.Gris, 34, Fondo, FontStyles.Normal, TextAlignmentOptions.Center);
        Estirar(iconoLuz.rectTransform);
        var estado = Texto(filaLuz, "TextoEstado", "GRIS", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        estado.characterSpacing = 4;
        var mensaje = Texto(der, "TextoMensaje", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        mensaje.lineSpacing = 6;
        mensaje.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var revisar = Boton(der, "BotonRevisar", "Revisar misión  " + SpaceMission.Iconos.Siguiente, true);
        revisar.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        var luzAnimada = luz.gameObject.AddComponent<LuzAnimada>();
        Asignar(luzAnimada, "halo", halo);
        Asignar(luzAnimada, "icono", iconoLuz);
        Asignar(luzAnimada, "panelASacudir", der);

        // Centro: queda libre para la nave; solo el paso actual arriba y una leyenda abajo.
        var paso = Texto(vista, "Paso", Paso(2), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        paso.font = ConstructorEstilo.Mono;
        Anclar(paso.rectTransform, new Vector2(0.37f, 0.93f), new Vector2(0.645f, 0.975f));
        var instruccion = Texto(vista, "Instruccion", "", 30, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        instruccion.characterSpacing = 3;
        Anclar(instruccion.rectTransform, new Vector2(0.37f, 0.875f), new Vector2(0.645f, 0.93f));
        var leyenda = Texto(vista, "LeyendaNave", "VISTA DE LA NAVE\n<size=24><color=#" + ColorUtility.ToHtmlStringRGB(TextoSecundario) +
            ">Cada instrumento elegido se instala y se ilumina. El anillo cian es tu órbita.</color></size>",
            26, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
        leyenda.margin = new Vector4(20, 0, 20, 0);   // que la segunda línea no se salga del fondo
        leyenda.fontSizeMax = 26; leyenda.fontSizeMin = 18; leyenda.enableAutoSizing = true;
        Anclar(leyenda.rectTransform, new Vector2(0.37f, 0.03f), new Vector2(0.645f, 0.13f));
        // Fondo oscuro detrás de la leyenda para que se lea sobre Marte.
        var fondoLeyenda = Panel(vista, "FondoLeyenda", new Color(Fondo.r, Fondo.g, Fondo.b, 0.7f));
        fondoLeyenda.GetComponent<UnityEngine.UI.Image>().sprite = sChaflan;
        fondoLeyenda.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;
        Anclar(fondoLeyenda, new Vector2(0.375f, 0.025f), new Vector2(0.64f, 0.135f));
        fondoLeyenda.SetSiblingIndex(leyenda.transform.GetSiblingIndex());
        Aparecer(vista, izq, der);

        var guia = vista.gameObject.AddComponent<GuiaConfiguracion>();
        Asignar(guia, "configuracion", ui);
        Asignar(guia, "textoPestanaOrbita", pOrbita.GetComponentInChildren<TextMeshProUGUI>());
        Asignar(guia, "textoPestanaLanzador", pLanzador.GetComponentInChildren<TextMeshProUGUI>());
        Asignar(guia, "textoPestanaInstrumentos", pInstrumentos.GetComponentInChildren<TextMeshProUGUI>());
        Asignar(guia, "botonSiguiente", siguiente);
        Asignar(guia, "textoInstruccion", instruccion);
        Asignar(guia, "textoLeyenda", leyenda);

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
        Asignar(ui, "luzEstado", luz);
        Asignar(ui, "textoEstado", estado);
        Asignar(ui, "textoMensaje", mensaje);
        Asignar(ui, "botonRevisar", revisar);
        Asignar(ui, "botonBriefing", verBriefing);
        return ui;
    }

    static RevisionUI ConstruirRevision(RectTransform vista, CatalogoDatos catalogo, MisionUI mision)
    {
        var ui = vista.gameObject.AddComponent<RevisionUI>();
        var tarjeta = Columna(vista, "Tarjeta", 22, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.92f));
        tarjeta.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(tarjeta);
        var paso = Texto(tarjeta, "Paso", Paso(3), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        paso.font = ConstructorEstilo.Mono;
        Texto(tarjeta, "Titulo", "Revisión de la misión", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        var resumen = Texto(tarjeta, "TextoResumen", "", 30, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        resumen.lineSpacing = 6;
        resumen.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var botones = Fila(tarjeta, "Botones", 24);
        Alto(botones, 88);
        var corregir = Boton(botones, "BotonCorregir", SpaceMission.Iconos.Atras + "  Corregir", false);
        var lanzar = Boton(botones, "BotonLanzar", SpaceMission.Iconos.Lanzador + "  Lanzar", true);
        Aparecer(vista, tarjeta);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "textoResumen", resumen);
        Asignar(ui, "botonCorregir", corregir);
        Asignar(ui, "botonLanzar", lanzar);
        return ui;
    }

    // ---------- Escena 3D ----------

    /// <summary>
    /// Nave de primitivas con materiales PBR: bus hexagonal con lámina dorada, antena parabólica, alas solares
    /// con celdas, toberas encendidas y un módulo por instrumento (aparecen al elegirlo, con el color de acento
    /// encendido). Los nombres Modulo_* los usan las pruebas.
    /// </summary>
    static NaveUI ConstruirNave()
    {
        var matDorado = CrearMaterial("Nave_Cuerpo", Color.white, 0.55f, 0.6f,
            ConstructorEstilo.Imagen("Texturas/Lamina_Dorada"), new Vector2(2, 2));
        var matPlata = CrearMaterial("Nave_Plata", Hex("#C9CED6"), 0.85f, 0.7f);
        var matBlanco = CrearMaterial("Nave_Blanco", Hex("#E5E7EB"), 0.1f, 0.45f);
        var matOscuro = CrearMaterial("Nave_Oscuro", Hex("#2B2F36"), 0.7f, 0.5f);
        var matPanel = CrearMaterial("Nave_Panel", Color.white, 0.35f, 0.85f,
            ConstructorEstilo.Imagen("Texturas/Celdas_Solares"), Vector2.one);
        var matModulo = CrearMaterial("Nave_Modulo", Acento, 0.2f, 0.5f, null, Vector2.one, Acento * 2.2f);
        var matTobera = CrearMaterial("Nave_Tobera", Hex("#FF8A3D"), 0f, 0.2f, null, Vector2.one, Hex("#FF8A3D") * 3.5f);
        var matCristal = CrearMaterial("Nave_Cristal", Hex("#0E1A2B"), 0.9f, 0.95f, null, Vector2.one, Cian * 0.4f);

        var mPrisma = GuardarMalla(Prisma(6, 0.9f, 2f), "Bus_Hexagonal");
        var mPlato = GuardarMalla(Paraboloide(1.05f, 0.32f), "Antena_Parabolica");
        var mCono = GuardarMalla(Cono(0.24f, 0.12f, 0.42f), "Tobera");

        var raiz = new GameObject("Nave");
        raiz.transform.position = Vector3.zero;
        raiz.transform.rotation = Quaternion.Euler(12, -28, 0);

        Malla(mPrisma, "Cuerpo", raiz.transform, Vector3.zero, Vector3.zero, Vector3.one, matDorado);
        Pieza(PrimitiveType.Cylinder, "Cubierta", raiz.transform, new Vector3(0, 1.03f, 0), Vector3.zero, new Vector3(1.75f, 0.05f, 1.75f), matPlata);
        Pieza(PrimitiveType.Cylinder, "Base", raiz.transform, new Vector3(0, -1.03f, 0), Vector3.zero, new Vector3(1.75f, 0.05f, 1.75f), matPlata);

        // Antena de alta ganancia sobre un mástil, inclinada hacia la Tierra (atrás y arriba).
        Pieza(PrimitiveType.Cylinder, "Mastil", raiz.transform, new Vector3(0, 1.45f, 0.1f), Vector3.zero, new Vector3(0.08f, 0.42f, 0.08f), matPlata);
        Malla(mPlato, "Antena", raiz.transform, new Vector3(0, 1.85f, 0.15f), new Vector3(-30, 0, 0), Vector3.one, matBlanco);
        Pieza(PrimitiveType.Cylinder, "Alimentador", raiz.transform, new Vector3(0, 2.2f, -0.05f), new Vector3(-30, 0, 0), new Vector3(0.06f, 0.28f, 0.06f), matOscuro);

        // Alas solares: brazo y dos paneles por lado.
        foreach (int lado in new[] { -1, 1 })
        {
            var ala = new GameObject(lado < 0 ? "AlaIzquierda" : "AlaDerecha").transform;
            ala.SetParent(raiz.transform, false);
            ala.localPosition = new Vector3(0.78f * lado, 0.25f, 0);
            ala.localRotation = Quaternion.Euler(18, 0, 0);
            Pieza(PrimitiveType.Cylinder, "Brazo", ala, new Vector3(0.4f * lado, 0, 0), new Vector3(0, 0, 90), new Vector3(0.07f, 0.4f, 0.07f), matPlata);
            for (int k = 0; k < 2; k++)
            {
                float x = (0.95f + k * 1.42f) * lado;
                Pieza(PrimitiveType.Cube, "Panel" + k, ala, new Vector3(x, 0, 0), Vector3.zero, new Vector3(1.36f, 0.035f, 1.05f), matPanel);
                Pieza(PrimitiveType.Cube, "Marco" + k, ala, new Vector3(x, -0.03f, 0), Vector3.zero, new Vector3(1.4f, 0.02f, 1.09f), matPlata);
            }
        }

        // Toberas con brillo (el Bloom las hace resplandecer).
        foreach (int lado in new[] { -1, 1 })
        {
            Malla(mCono, "Tobera", raiz.transform, new Vector3(0.38f * lado, -1.45f, 0), Vector3.zero, Vector3.one, matOscuro);
            Pieza(PrimitiveType.Cylinder, "Llama", raiz.transform, new Vector3(0.38f * lado, -1.46f, 0), Vector3.zero,
                new Vector3(0.17f, 0.01f, 0.17f), matTobera);
        }

        // Un módulo por instrumento, en el orden del catálogo. Aparecen al elegir el instrumento.
        string[] ids = { "termica", "radar", "alta_res", "atmosferico", "espectrometro" };
        var modulos = new GameObject[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            var m = new GameObject("Modulo_" + ids[i]).transform;
            m.SetParent(raiz.transform, false);
            switch (ids[i])
            {
                case "termica": // cámara térmica: tubo con lente mirando al frente
                    m.localPosition = new Vector3(0.42f, -0.45f, -0.85f);
                    Pieza(PrimitiveType.Cylinder, "Tubo", m, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.36f, 0.22f, 0.36f), matBlanco);
                    Pieza(PrimitiveType.Cylinder, "Anillo", m, new Vector3(0, 0, -0.2f), new Vector3(90, 0, 0), new Vector3(0.4f, 0.03f, 0.4f), matModulo);
                    Pieza(PrimitiveType.Cylinder, "Lente", m, new Vector3(0, 0, -0.23f), new Vector3(90, 0, 0), new Vector3(0.28f, 0.01f, 0.28f), matCristal);
                    break;
                case "radar": // radar de subsuelo: dipolo largo bajo la nave (como SHARAD)
                    m.localPosition = new Vector3(0, -1.12f, -0.3f);
                    Pieza(PrimitiveType.Cube, "Caja", m, Vector3.zero, Vector3.zero, new Vector3(0.36f, 0.16f, 0.3f), matBlanco);
                    Pieza(PrimitiveType.Cylinder, "Dipolo", m, Vector3.zero, new Vector3(0, 0, 90), new Vector3(0.035f, 2.6f, 0.035f), matBlanco);
                    foreach (int lado in new[] { -1, 1 })
                        Pieza(PrimitiveType.Sphere, "Punta", m, new Vector3(2.6f * lado, 0, 0), Vector3.zero, Vector3.one * 0.09f, matModulo);
                    Pieza(PrimitiveType.Cube, "Franja", m, new Vector3(0, 0.09f, 0), Vector3.zero, new Vector3(0.37f, 0.03f, 0.31f), matModulo);
                    break;
                case "alta_res": // cámara de alta resolución: telescopio grande
                    m.localPosition = new Vector3(-0.42f, 0.2f, -0.9f);
                    Pieza(PrimitiveType.Cylinder, "Tubo", m, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.5f, 0.4f, 0.5f), matBlanco);
                    Pieza(PrimitiveType.Cylinder, "Parasol", m, new Vector3(0, 0, -0.42f), new Vector3(90, 0, 0), new Vector3(0.56f, 0.06f, 0.56f), matModulo);
                    Pieza(PrimitiveType.Cylinder, "Lente", m, new Vector3(0, 0, -0.47f), new Vector3(90, 0, 0), new Vector3(0.42f, 0.01f, 0.42f), matCristal);
                    break;
                case "atmosferico": // sensor atmosférico: esfera en un mástil
                    m.localPosition = new Vector3(0.62f, 1.05f, -0.45f);
                    Pieza(PrimitiveType.Cylinder, "Mastil", m, new Vector3(0.12f, 0.3f, 0), new Vector3(0, 0, -22), new Vector3(0.05f, 0.32f, 0.05f), matPlata);
                    Pieza(PrimitiveType.Sphere, "Sensor", m, new Vector3(0.25f, 0.62f, 0), Vector3.zero, Vector3.one * 0.26f, matModulo);
                    break;
                default: // espectrómetro: caja con apertura
                    m.localPosition = new Vector3(0.42f, 0.42f, -0.88f);
                    Pieza(PrimitiveType.Cube, "Caja", m, Vector3.zero, Vector3.zero, new Vector3(0.42f, 0.32f, 0.3f), matBlanco);
                    Pieza(PrimitiveType.Cube, "Franja", m, new Vector3(0, 0.12f, -0.16f), Vector3.zero, new Vector3(0.43f, 0.05f, 0.02f), matModulo);
                    Pieza(PrimitiveType.Cylinder, "Apertura", m, new Vector3(0, -0.04f, -0.16f), new Vector3(90, 0, 0), new Vector3(0.16f, 0.01f, 0.16f), matCristal);
                    break;
            }
            m.gameObject.AddComponent<Instalacion>();
            modulos[i] = m.gameObject;
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

    // ---------- Cohetes ----------

    /// <summary>
    /// Plataforma con los dos lanzadores, a escala aproximada entre ellos (el SLS mide el doble que el Soyuz):
    /// - Explorador, inspirado en el Soyuz: etapa central gris claro, cuatro propulsores cónicos con faldón,
    ///   banda naranja, etapa superior y cofia ojival.
    /// - Titán, inspirado en el SLS de Artemis II: etapa central naranja (espuma aislante), dos propulsores sólidos
    ///   blancos, etapa superior, Orion y su torre de escape.
    /// Debajo de cada uno, un anillo de acento que se enciende cuando ese lanzador está elegido.
    /// </summary>
    static (GameObject explorador, GameObject titan, GameObject resaltadoExplorador, GameObject resaltadoTitan) ConstruirCohetes(Vector3 plataforma)
    {
        var blanco = CrearMaterial("Cohete_Blanco", Hex("#ECEEF0"), 0.05f, 0.55f);
        var grisVerde = CrearMaterial("Cohete_GrisSoyuz", Hex("#C9CFC6"), 0.05f, 0.45f);
        var naranja = CrearMaterial("Cohete_Naranja", Hex("#C8651E"), 0f, 0.2f);
        var oscuro = CrearMaterial("Nave_Oscuro", Hex("#2B2F36"), 0.7f, 0.5f);
        var negro = CrearMaterial("Cohete_Negro", Hex("#16181C"), 0.2f, 0.4f);
        var plata = CrearMaterial("Nave_Plata", Hex("#C9CED6"), 0.85f, 0.7f);
        var bandaNaranja = CrearMaterial("Cohete_BandaNaranja", Hex("#E2643A"), 0.05f, 0.5f);
        var azul = CrearMaterial("Cohete_Azul", Hex("#1E3A8A"), 0.1f, 0.6f);
        var llama = CrearMaterial("Nave_Tobera", Hex("#FF8A3D"), 0f, 0.2f, null, Vector2.one, Hex("#FF8A3D") * 3.5f);
        var suelo = CrearMaterial("Plataforma", Hex("#3A3F47"), 0.3f, 0.35f);
        var resalte = CrearMaterial("Plataforma_Resalte", Acento, 0f, 0.5f, null, Vector2.one, Acento * 1.6f);

        var raiz = new GameObject("PlataformaLanzamiento").transform;
        raiz.position = plataforma;
        Pieza(PrimitiveType.Cylinder, "Suelo", raiz, new Vector3(0, -0.06f, 0), Vector3.zero, new Vector3(9f, 0.06f, 4.5f), suelo);

        // ----- Explorador (Soyuz) -----
        var soyuz = new GameObject("Cohete_explorador").transform;
        soyuz.SetParent(raiz, false);
        soyuz.localPosition = new Vector3(-2.1f, 0, 0);
        var mNucleo = GuardarMalla(Torno(new[] { P(0.13f, 0.35f), P(0.17f, 0.5f), P(0.235f, 2.9f), P(0.24f, 3.3f), P(0.2f, 3.45f) }), "Soyuz_Nucleo");
        var mPropulsor = GuardarMalla(Torno(new[] { P(0.16f, 0.22f), P(0.25f, 0.42f), P(0.235f, 0.7f), P(0.07f, 2.55f), P(0f, 2.75f) }), "Soyuz_Propulsor");
        var mCofia = GuardarMalla(Torno(new[] { P(0.2f, 0f), P(0.26f, 0.12f), P(0.26f, 0.85f), P(0.22f, 1.1f), P(0.12f, 1.35f), P(0f, 1.5f) }), "Soyuz_Cofia");
        var mTobera = GuardarMalla(Cono(0.1f, 0.05f, 0.22f), "Tobera_Pequena");
        Malla(mNucleo, "Nucleo", soyuz, Vector3.zero, Vector3.zero, Vector3.one, grisVerde);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
            var propulsor = Malla(mPropulsor, "Propulsor" + i, soyuz, dir * 0.31f, new Vector3(0, a, 0), Vector3.one, grisVerde);
            propulsor.transform.localRotation = Quaternion.AngleAxis(-3f, Vector3.Cross(Vector3.up, dir)) * propulsor.transform.localRotation;
            Pieza(PrimitiveType.Cube, "Aleta" + i, soyuz, dir * 0.6f + Vector3.up * 0.45f, new Vector3(0, a, 0), new Vector3(0.02f, 0.28f, 0.18f), oscuro);
            Malla(mTobera, "Tobera" + i, soyuz, dir * 0.31f + Vector3.up * 0.0f, Vector3.zero, Vector3.one, oscuro);
            Pieza(PrimitiveType.Cylinder, "Llama" + i, soyuz, dir * 0.31f, Vector3.zero, new Vector3(0.12f, 0.005f, 0.12f), llama);
        }
        Malla(mTobera, "ToberaCentral", soyuz, Vector3.up * 0.13f, Vector3.zero, Vector3.one * 1.1f, oscuro);
        Pieza(PrimitiveType.Cylinder, "Celosia", soyuz, Vector3.up * 3.6f, Vector3.zero, new Vector3(0.38f, 0.15f, 0.38f), oscuro);
        Pieza(PrimitiveType.Cylinder, "BandaNaranja", soyuz, Vector3.up * 3.82f, Vector3.zero, new Vector3(0.41f, 0.07f, 0.41f), bandaNaranja);
        Pieza(PrimitiveType.Cylinder, "EtapaSuperior", soyuz, Vector3.up * 4.25f, Vector3.zero, new Vector3(0.4f, 0.36f, 0.4f), blanco);
        Malla(mCofia, "Cofia", soyuz, Vector3.up * 4.6f, Vector3.zero, Vector3.one, blanco);
        Pieza(PrimitiveType.Cube, "Emblema", soyuz, new Vector3(0, 5.2f, -0.262f), Vector3.zero, new Vector3(0.14f, 0.22f, 0.01f), azul);
        soyuz.gameObject.AddComponent<GiroLento>().enabled = false;

        // ----- Titán (SLS + Orion) -----
        var sls = new GameObject("Cohete_titan").transform;
        sls.SetParent(raiz, false);
        sls.localPosition = new Vector3(2.1f, 0, 0);
        var mEtapa = GuardarMalla(Torno(new[] { P(0.4f, 0.35f), P(0.42f, 0.45f), P(0.42f, 6.85f), P(0.4f, 6.95f) }), "SLS_EtapaCentral");
        var mSrb = GuardarMalla(Torno(new[] { P(0.24f, 0.25f), P(0.2f, 0.55f), P(0.2f, 6.2f), P(0.17f, 6.45f), P(0.08f, 6.8f), P(0f, 6.9f) }), "SLS_Propulsor");
        var mAdaptador = GuardarMalla(Torno(new[] { P(0.42f, 0f), P(0.27f, 0.7f) }), "SLS_Adaptador");
        var mLas = GuardarMalla(Torno(new[] { P(0.28f, 0f), P(0.26f, 0.2f), P(0.13f, 0.75f), P(0.05f, 0.95f), P(0.05f, 1.6f), P(0.07f, 1.62f), P(0.07f, 1.75f), P(0.04f, 1.8f), P(0f, 2.05f) }), "Orion_TorreEscape");
        var mRs25 = GuardarMalla(Cono(0.13f, 0.05f, 0.3f), "Tobera_RS25");
        Pieza(PrimitiveType.Cylinder, "SeccionMotores", sls, Vector3.up * 0.4f, Vector3.zero, new Vector3(0.84f, 0.08f, 0.84f), blanco);
        Malla(mEtapa, "EtapaCentral", sls, Vector3.zero, Vector3.zero, Vector3.one, naranja);
        Pieza(PrimitiveType.Cylinder, "Anillo", sls, Vector3.up * 5.3f, Vector3.zero, new Vector3(0.855f, 0.03f, 0.855f), CrearMaterial("Cohete_NaranjaOscuro", Hex("#9A4B14"), 0f, 0.3f));
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector3((i % 2 == 0 ? -1 : 1) * 0.17f, 0.32f, (i < 2 ? -1 : 1) * 0.17f);
            Malla(mRs25, "RS25_" + i, sls, pos - Vector3.up * 0.3f, Vector3.zero, Vector3.one, oscuro);
            Pieza(PrimitiveType.Cylinder, "Llama" + i, sls, pos - Vector3.up * 0.3f, Vector3.zero, new Vector3(0.24f, 0.005f, 0.24f), llama);
        }
        foreach (int lado in new[] { -1, 1 })
        {
            var x = lado * 0.66f;
            Malla(mSrb, lado < 0 ? "PropulsorIzquierdo" : "PropulsorDerecho", sls, new Vector3(x, 0, 0), Vector3.zero, Vector3.one, blanco);
            Pieza(PrimitiveType.Cylinder, "Faldon", sls, new Vector3(x, 0.38f, 0), Vector3.zero, new Vector3(0.5f, 0.14f, 0.5f), negro);
            foreach (var y in new[] { 1.6f, 3.4f, 5.2f })
                Pieza(PrimitiveType.Cylinder, "Union", sls, new Vector3(x, y, 0), Vector3.zero, new Vector3(0.41f, 0.015f, 0.41f), negro);
            Pieza(PrimitiveType.Cylinder, "Toberas", sls, new Vector3(x, 0.15f, 0), Vector3.zero, new Vector3(0.3f, 0.12f, 0.3f), oscuro);
            Pieza(PrimitiveType.Cylinder, "Llama", sls, new Vector3(x, 0.02f, 0), Vector3.zero, new Vector3(0.27f, 0.005f, 0.27f), llama);
            foreach (var y in new[] { 0.9f, 6.0f })
                Pieza(PrimitiveType.Cylinder, "Soporte", sls, new Vector3(lado * 0.42f, y, 0), new Vector3(0, 0, 90), new Vector3(0.05f, 0.12f, 0.05f), oscuro);
        }
        Malla(mAdaptador, "Adaptador", sls, Vector3.up * 6.95f, Vector3.zero, Vector3.one, blanco);
        Pieza(PrimitiveType.Cylinder, "EtapaSuperior", sls, Vector3.up * 7.95f, Vector3.zero, new Vector3(0.5f, 0.3f, 0.5f), plata);
        Pieza(PrimitiveType.Cylinder, "ModuloServicio", sls, Vector3.up * 8.5f, Vector3.zero, new Vector3(0.56f, 0.25f, 0.56f), blanco);
        Malla(mLas, "OrionYTorre", sls, Vector3.up * 8.75f, Vector3.zero, Vector3.one, blanco);
        sls.gameObject.AddComponent<GiroLento>().enabled = false;

        // Anillos que señalan el lanzador elegido.
        GameObject Resalte(string nombre, Vector3 x)
        {
            var r = Pieza(PrimitiveType.Cylinder, nombre, raiz, x + Vector3.up * 0.005f, Vector3.zero, new Vector3(1.9f, 0.004f, 1.9f), resalte);
            r.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            r.SetActive(false);
            return r;
        }
        var rExp = Resalte("ElegidoExplorador", soyuz.localPosition);
        var rTit = Resalte("ElegidoTitan", sls.localPosition);
        return (soyuz.gameObject, sls.gameObject, rExp, rTit);
    }

    static Vector2 P(float radio, float alto) => new Vector2(radio, alto);

    /// <summary>Malla de revolución: gira un perfil (radio, alto) de abajo arriba alrededor del eje Y.</summary>
    static Mesh Torno(Vector2[] perfil, int segmentos = 40)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int fila = segmentos + 1;
        for (int i = 0; i < perfil.Length; i++)
            for (int s = 0; s <= segmentos; s++)
            {
                float a = s * Mathf.PI * 2f / segmentos;
                v.Add(new Vector3(Mathf.Cos(a) * perfil[i].x, perfil[i].y, Mathf.Sin(a) * perfil[i].x));
                uv.Add(new Vector2((float)s / segmentos, (float)i / (perfil.Length - 1)));
            }
        for (int i = 0; i < perfil.Length - 1; i++)
            for (int s = 0; s < segmentos; s++)
            {
                int a = i * fila + s, b = a + fila;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        // Tapa inferior si el perfil no empieza en el eje.
        if (perfil[0].x > 0.001f)
        {
            int centro = v.Count;
            v.Add(new Vector3(0, perfil[0].y, 0)); uv.Add(new Vector2(0.5f, 0));
            for (int s = 0; s < segmentos; s++) t.AddRange(new[] { centro, s, s + 1 });
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Esfera UV de alta resolución (silueta suave al acercarse) con la proyección equirectangular de los mapas.</summary>
    static Mesh EsferaUV()
    {
        const string ruta = "Assets/Arte/Mallas/Esfera_Planeta.asset";
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente != null) return existente;
        const int lon = 128, lat = 64;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int y = 0; y <= lat; y++)
            for (int x = 0; x <= lon; x++)
            {
                float fi = Mathf.PI * y / lat, la = 2f * Mathf.PI * x / lon;
                var p = new Vector3(-Mathf.Sin(fi) * Mathf.Cos(la), -Mathf.Cos(fi), -Mathf.Sin(fi) * Mathf.Sin(la));
                v.Add(p * 0.5f); n.Add(p); uv.Add(new Vector2((float)x / lon, (float)y / lat)); // este a la derecha, sin espejo
            }
        for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int a = y * (lon + 1) + x, b = a + lon + 1;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); // caras hacia fuera
            }
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateTangents();
        m.RecalculateBounds();
        return GuardarMalla(m, "Esfera_Planeta");
    }

    static void AsignarEncuadre(Object destino, string campo, Vector3 posicion, Vector3 mirarA, float fov)
    {
        var so = new SerializedObject(destino);
        so.FindProperty(campo + ".posicion").vector3Value = posicion;
        so.FindProperty(campo + ".mirarA").vector3Value = mirarA;
        so.FindProperty(campo + ".campo").floatValue = fov;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Marte con la textura real (MARCI, tormenta global de 2018) y un borde de atmósfera.</summary>
    static GameObject ConstruirMarte(Vector3 posicion, float diametro)
    {
        var mat = CrearMaterial("Marte", Color.white, 0f, 0.1f, ConstructorEstilo.Imagen("Texturas/Marte_Superficie"), Vector2.one);
        mat.SetTexture("_BumpMap", ConstructorEstilo.Imagen("Texturas/Marte_Normal"));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat);
        var marte = Malla(EsferaUV(), "Marte", null, posicion, new Vector3(0, 0, -12), Vector3.one * diametro, mat);

        // Capa de polvo: la tormenta global de 2018 (MARCI) semitransparente, girando un poco más rápido.
        var matPolvo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Marte_Polvo.mat");
        if (matPolvo == null)
        {
            matPolvo = new Material(Shader.Find("OperacionEclipseRojo/CapaPolvo"));
            AssetDatabase.CreateAsset(matPolvo, "Assets/Materiales/Marte_Polvo.mat");
        }
        matPolvo.SetTexture("_BaseMap", ConstructorEstilo.Imagen("Texturas/Marte_Polvo"));
        matPolvo.SetFloat("_Opacidad", 0.5f);
        EditorUtility.SetDirty(matPolvo);
        var polvo = Malla(EsferaUV(), "TormentaPolvo", null, posicion, new Vector3(0, 40, -12), Vector3.one * diametro * 1.008f, matPolvo);
        polvo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Asignar(polvo.AddComponent<GiroLento>(), "gradosPorSegundo", 2.4f);
        var matAtmosfera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Atmosfera.mat");
        if (matAtmosfera == null)
        {
            matAtmosfera = new Material(Shader.Find("OperacionEclipseRojo/Atmosfera"));
            AssetDatabase.CreateAsset(matAtmosfera, "Assets/Materiales/Atmosfera.mat");
        }
        matAtmosfera.SetColor("_Color", new Color(1f, 0.55f, 0.35f) * 1.4f);
        matAtmosfera.SetFloat("_Potencia", 3.5f);
        EditorUtility.SetDirty(matAtmosfera);
        var atmosfera = Pieza(PrimitiveType.Sphere, "Atmosfera", null, posicion, Vector3.zero, Vector3.one * diametro * 1.035f, matAtmosfera);
        atmosfera.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        marte.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return marte;
    }

    static void ConstruirAnilloOrbita(Transform marte)
    {
        // No es hijo del planeta: así no gira con él. Mismo centro y escala (radio 0.5 = superficie).
        var go = new GameObject("AnilloOrbita", typeof(LineRenderer));
        go.transform.position = marte.position;
        go.transform.localScale = marte.localScale;
        var linea = go.GetComponent<LineRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, "Assets/Materiales/Linea_Orbita.mat");
        }
        linea.sharedMaterial = mat;
        linea.widthMultiplier = 0.07f;
        linea.startColor = linea.endColor = new Color(Cian.r, Cian.g, Cian.b, 0f);
        linea.shadowCastingMode = ShadowCastingMode.Off;
        linea.enabled = false;
        var anillo = go.AddComponent<AnilloOrbita>();
        Asignar(anillo, "radio", 0.66f);
        // Marcador de la nave en órbita: una esfera cian encendida (el Bloom la hace brillar).
        var matMarcador = CrearMaterial("Marcador_Orbita", Cian, 0f, 0.5f, null, Vector2.one, Cian * 3f);
        var marcador = Pieza(PrimitiveType.Sphere, "MarcadorNave", go.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.022f, matMarcador);
        marcador.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        marcador.SetActive(false);
        Asignar(anillo, "marcador", marcador.transform);
    }

    /// <summary>Cielo de estrellas propio en un plano lejano que llena la vista (se mueve un poco con el ratón).</summary>
    static void ConstruirEstrellas() => ConstruirEstrellas(new Vector3(0, 0.2f, 60f));

    static void ConstruirEstrellas(Vector3 centro)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Estrellas.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, "Assets/Materiales/Estrellas.mat");
        }
        mat.SetTexture("_BaseMap", ConstructorEstilo.Imagen("Texturas/Estrellas"));
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        var cielo = Pieza(PrimitiveType.Quad, "Estrellas", null, centro, Vector3.zero, new Vector3(128f, 72f, 1f), mat);
        cielo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var parallax = cielo.AddComponent<Parallax>();
        Asignar(parallax, "amplitud", new Vector2(2.5f, 1.4f));
    }

    /// <summary>Luz del Sol cálida de lado, luz de contorno fría desde atrás, ambiente casi nulo y posprocesado.</summary>
    static void ConstruirIluminacion()
    {
        var porDefecto = GameObject.Find("Directional Light");
        if (porDefecto != null) Object.DestroyImmediate(porDefecto);

        var sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.color = new Color(1f, 0.93f, 0.84f);
        sol.intensity = 2.1f;
        sol.shadows = LightShadows.Soft;
        sol.transform.rotation = Quaternion.Euler(24, 38, 0); // de frente y algo de lado: el planeta se ve casi todo iluminado

        var contra = new GameObject("LuzContorno").AddComponent<Light>();
        contra.type = LightType.Directional;
        contra.color = new Color(0.45f, 0.65f, 1f);
        contra.intensity = 1.1f;
        contra.shadows = LightShadows.None;
        contra.transform.rotation = Quaternion.Euler(12, -150, 0);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("#1A2130");
        RenderSettings.fog = false;

        var volumen = new GameObject("PostProceso").AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.sharedProfile = PerfilPostProceso();
        Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    static VolumeProfile PerfilPostProceso()
    {
        const string ruta = "Assets/Arte/PostProceso.asset";
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
        }
        // Los valores se aplican siempre, aunque el perfil ya exista, para que cambiarlos aquí tenga efecto.
        var bloom = Componente<Bloom>(perfil);
        bloom.threshold.Override(1.15f);   // solo brilla lo realmente encendido (toberas, módulos)
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.5f);
        Componente<Tonemapping>(perfil).mode.Override(TonemappingMode.ACES);
        var vineta = Componente<Vignette>(perfil);
        vineta.intensity.Override(0.3f);
        vineta.smoothness.Override(0.5f);
        var color = Componente<ColorAdjustments>(perfil);
        color.postExposure.Override(0.35f);
        color.contrast.Override(8f);
        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    static T Componente<T>(VolumeProfile perfil) where T : VolumeComponent
    {
        if (perfil.TryGet<T>(out var c)) return c;
        c = perfil.Add<T>(true);
        c.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(c, perfil);
        return c;
    }

    static void PrepararCamara()
    {
        var camara = Camera.main;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = Fondo;
        camara.transform.position = new Vector3(0, 0.2f, -8.5f);
        camara.transform.rotation = Quaternion.identity;
        camara.farClipPlane = 120f;
    }

    static MusicaEscena CrearMusica(ArteDatos.Musica musica)
    {
        var go = new GameObject("Audio", typeof(AudioSource));
        var reproductor = go.AddComponent<ReproductorUI>();
        Asignar(reproductor, "arte", arte);
        var m = go.AddComponent<MusicaEscena>();
        Asignar(m, "arte", arte);
        var so = new SerializedObject(m);
        so.FindProperty("musica").enumValueIndex = (int)musica;
        so.ApplyModifiedPropertiesWithoutUndo();
        return m;
    }

    // ---------- Mallas y materiales ----------

    static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion, Vector3 rotacion, Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(rotacion);
        go.transform.localScale = escala;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    static GameObject Malla(Mesh malla, string nombre, Transform padre, Vector3 posicion, Vector3 rotacion, Vector3 escala, Material material)
    {
        var go = new GameObject(nombre, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.Euler(rotacion);
        go.transform.localScale = escala;
        go.GetComponent<MeshFilter>().sharedMesh = malla;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    static Material CrearMaterial(string nombre, Color color, float metalico = 0f, float suavidad = 0.5f,
        Texture2D textura = null, Vector2 repeticion = default, Color? emision = null)
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
        mat.SetFloat("_Metallic", metalico);
        mat.SetFloat("_Smoothness", suavidad);
        mat.SetTexture("_BaseMap", textura);
        mat.SetTextureScale("_BaseMap", repeticion == default ? Vector2.one : repeticion);
        if (emision.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Guarda la malla en Assets/Arte/Mallas conservando el GUID si ya existía (las escenas la siguen encontrando).</summary>
    static Mesh GuardarMalla(Mesh nueva, string nombre)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Arte/Mallas")) AssetDatabase.CreateFolder("Assets/Arte", "Mallas");
        var ruta = "Assets/Arte/Mallas/" + nombre + ".asset";
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        nueva.name = nombre;
        if (existente == null)
        {
            AssetDatabase.CreateAsset(nueva, ruta);
            return nueva;
        }
        existente.Clear();
        existente.indexFormat = nueva.indexFormat;
        existente.vertices = nueva.vertices;
        existente.normals = nueva.normals;
        existente.uv = nueva.uv;
        existente.triangles = nueva.triangles;
        existente.tangents = nueva.tangents;
        existente.RecalculateBounds();
        EditorUtility.SetDirty(existente);
        return existente;
    }

    /// <summary>Prisma de caras planas (lados, radio, alto) con tapas. UV de los lados para repetir la lámina.</summary>
    static Mesh Prisma(int lados, float radio, float alto)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float h = alto / 2f;
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            var p0 = new Vector3(Mathf.Cos(a0) * radio, 0, Mathf.Sin(a0) * radio);
            var p1 = new Vector3(Mathf.Cos(a1) * radio, 0, Mathf.Sin(a1) * radio);
            var normal = ((p0 + p1) / 2f).normalized;
            int b = v.Count;
            v.AddRange(new[] { p0 + Vector3.down * h, p1 + Vector3.down * h, p1 + Vector3.up * h, p0 + Vector3.up * h });
            n.AddRange(new[] { normal, normal, normal, normal });
            uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, alto), new Vector2(0, alto) });
            t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        foreach (int s in new[] { 1, -1 }) // tapas
        {
            int centro = v.Count;
            v.Add(new Vector3(0, h * s, 0)); n.Add(Vector3.up * s); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                v.Add(new Vector3(Mathf.Cos(a) * radio, h * s, Mathf.Sin(a) * radio)); n.Add(Vector3.up * s);
                uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < lados; i++)
            {
                if (s > 0) t.AddRange(new[] { centro, centro + i + 2, centro + i + 1 });
                else t.AddRange(new[] { centro, centro + i + 1, centro + i + 2 });
            }
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Plato parabólico de dos caras (se ve por dentro y por fuera).</summary>
    static Mesh Paraboloide(float radio, float hondo, int segmentos = 40, int anillos = 10)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int r = 0; r <= anillos; r++)
        for (int s = 0; s <= segmentos; s++)
        {
            float k = (float)r / anillos, a = s * Mathf.PI * 2f / segmentos;
            v.Add(new Vector3(Mathf.Cos(a) * radio * k, hondo * k * k, Mathf.Sin(a) * radio * k));
            uv.Add(new Vector2((float)s / segmentos, k));
        }
        int fila = segmentos + 1;
        for (int r = 0; r < anillos; r++)
        for (int s = 0; s < segmentos; s++)
        {
            int a = r * fila + s, b = a + fila;
            t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        // Segunda cara: los mismos vértices duplicados con el orden invertido.
        int total = v.Count;
        v.AddRange(v.ToArray()); uv.AddRange(uv.ToArray());
        int nt = t.Count;
        for (int i = 0; i < nt; i += 3) t.AddRange(new[] { t[i] + total, t[i + 2] + total, t[i + 1] + total });
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Tronco de cono abierto (tobera), de radio de base a radio de punta. Solo la cara exterior.</summary>
    static Mesh Cono(float radioBase, float radioPunta, float alto, int segmentos = 28)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int s = 0; s <= segmentos; s++)
        {
            float a = s * Mathf.PI * 2f / segmentos;
            v.Add(new Vector3(Mathf.Cos(a) * radioBase, 0, Mathf.Sin(a) * radioBase));
            v.Add(new Vector3(Mathf.Cos(a) * radioPunta, alto, Mathf.Sin(a) * radioPunta));
            uv.Add(new Vector2((float)s / segmentos, 0)); uv.Add(new Vector2((float)s / segmentos, 1));
        }
        for (int s = 0; s < segmentos; s++)
        {
            int a = s * 2;
            t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
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

    // ---------- Piezas de interfaz ----------

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

    /// <summary>Foto de fondo a pantalla completa (primer hijo) con líneas de barrido muy tenues encima.</summary>
    static void FondoFoto(RectTransform padre, Texture2D foto, float alfa = 1f, bool barrido = true)
    {
        var img = new GameObject("FondoFoto", typeof(RectTransform), typeof(UnityEngine.UI.RawImage),
            typeof(UnityEngine.UI.AspectRatioFitter)).GetComponent<UnityEngine.UI.RawImage>();
        img.transform.SetParent(padre, false);
        img.transform.SetAsFirstSibling();
        img.texture = foto;
        img.color = new Color(1, 1, 1, alfa);
        img.raycastTarget = false;
        var ajuste = img.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        ajuste.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = (float)foto.width / foto.height;
        if (!barrido) return;
        var lineas = new GameObject("Barrido", typeof(RectTransform), typeof(UnityEngine.UI.RawImage))
            .GetComponent<UnityEngine.UI.RawImage>();
        lineas.transform.SetParent(padre, false);
        lineas.transform.SetSiblingIndex(1);
        lineas.texture = tBarrido;
        lineas.uvRect = new Rect(0, 0, 1920 / 4f, 1080 / 4f);
        lineas.raycastTarget = false;
        Estirar(lineas.rectTransform);
    }

    /// <summary>Imagen con marco y pie dentro de una columna (miniatura del Briefing).</summary>
    static void Miniatura(Transform padre, string nombre, Texture2D foto, float alto, string pie)
    {
        var marco = Vacio(padre, "Miniatura" + nombre);
        Alto(marco, alto);
        var img = new GameObject("Imagen", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        img.transform.SetParent(marco, false);
        img.texture = foto;
        img.raycastTarget = false;
        // Recorte centrado a lo ancho, sin deformar.
        float aspectoMarco = 860f / alto, aspectoFoto = (float)foto.width / foto.height;
        float alturaUv = Mathf.Min(1f, aspectoFoto / aspectoMarco);
        img.uvRect = new Rect(0, (1 - alturaUv) / 2f, 1, alturaUv);
        Estirar(img.rectTransform);
        var velo = Panel(marco, "Velo", new Color(0, 0, 0, 0.55f));
        Anclar(velo, Vector2.zero, new Vector2(1, 0.26f));
        var t = Texto(marco, "Pie", pie, 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        t.font = ConstructorEstilo.Mono;
        Anclar(t.rectTransform, Vector2.zero, new Vector2(1, 0.26f));
        t.margin = new Vector4(16, 0, 16, 0);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        MarcoHud(marco);
    }

    /// <summary>Entrada animada de la vista: fundido general y las piezas indicadas entran escalonadas.</summary>
    static void Aparecer(RectTransform vista, params RectTransform[] piezas)
    {
        var a = vista.gameObject.AddComponent<AparecerVista>();
        AsignarLista(a, "escalonados", piezas);
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

    /// <summary>
    /// Texto con la fuente según su papel: negrita grande (≥ 40 pt) usa Rajdhani Bold (títulos), negrita pequeña
    /// usa Rajdhani SemiBold (etiquetas) y el resto Barlow (texto). Rajdhani es estrecha: se compensa un 8 %.
    /// </summary>
    static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tamano, Color color,
        FontStyles estilo, TextAlignmentOptions alineacion)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        bool negrita = (estilo & FontStyles.Bold) != 0;
        if (negrita)
        {
            tmp.font = tamano >= 40 ? ConstructorEstilo.Titulos : ConstructorEstilo.Etiquetas;
            estilo &= ~FontStyles.Bold;
            tamano *= 1.08f;
        }
        else tmp.font = ConstructorEstilo.Texto;
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
        imagen.sprite = sChaflan;
        imagen.type = UnityEngine.UI.Image.Type.Sliced;
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

        // Los secundarios llevan marco fino (con el mismo chaflán) para leerse como botón sobre una tarjeta.
        if (!principal)
        {
            var borde = new GameObject("Borde", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
            borde.transform.SetParent(go.transform, false);
            borde.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            var img = borde.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sChaflanBorde;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = LineaHud;
            img.raycastTarget = false;
            Estirar((RectTransform)borde.transform);
        }

        // El texto va después del borde: es el primer TMP_Text del botón (las pruebas lo buscan así).
        var etiqueta = Texto(go.transform, "Texto", texto, 34, principal ? TextoSobreAcento : TextoClaro,
            FontStyles.Bold, TextAlignmentOptions.Center);
        etiqueta.fontStyle = FontStyles.UpperCase;
        etiqueta.characterSpacing = 3;
        Estirar(etiqueta.rectTransform);

        go.AddComponent<BotonVivo>();

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
        t.characterSpacing = 3;
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

    static RectTransform Alto(RectTransform rt, float alto)
    {
        var le = rt.GetComponent<UnityEngine.UI.LayoutElement>();
        if (le == null) le = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = alto;
        le.preferredHeight = alto;
        le.flexibleHeight = 0; // sin esto la fila crece con el espacio sobrante de la columna
        return rt;
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
        var prop = Propiedad(so, campo, destino);
        prop.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Asignar(Object destino, string campo, float valor)
    {
        var so = new SerializedObject(destino);
        Propiedad(so, campo, destino).floatValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Asignar(Object destino, string campo, bool valor)
    {
        var so = new SerializedObject(destino);
        Propiedad(so, campo, destino).boolValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Asignar(Object destino, string campo, Vector2 valor)
    {
        var so = new SerializedObject(destino);
        Propiedad(so, campo, destino).vector2Value = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Asignar(Object destino, string campo, ArteDatos.Sonido valor)
    {
        var so = new SerializedObject(destino);
        Propiedad(so, campo, destino).enumValueIndex = (int)valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SerializedProperty Propiedad(SerializedObject so, string campo, Object destino)
    {
        var prop = so.FindProperty(campo);
        if (prop == null) throw new System.ArgumentException("No existe el campo " + campo + " en " + destino.GetType().Name);
        return prop;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
