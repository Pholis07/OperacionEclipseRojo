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
/// los cambios hechos a mano en Menu y Mision se pierden al volver a ejecutarlo.
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

    [MenuItem("Herramientas/Construir escenas")]
    public static void ConstruirTodo()
    {
        ConstruirMenu();
        ConstruirMision();
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorEscenas: listo");
    }

    // ---------- Menu ----------

    static void ConstruirMenu()
    {
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

        var continuar = Boton(col, "BotonContinuar", "Continuar", true);

        // Vista Configuración: se construye en la fase 6.
        var configuracion = Panel(canvas, "VistaConfiguracion", Fondo);
        Estirar(configuracion);
        var aviso = Texto(configuracion, "Pendiente", "Configuración de la misión (en construcción)", 48,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        Estirar(aviso.rectTransform);

        CrearEventSystem();

        var controlador = new GameObject("MisionUI").AddComponent<MisionUI>();
        Asignar(controlador, "catalogo", catalogo);
        Asignar(controlador, "vistaBriefing", briefing.gameObject);
        Asignar(controlador, "vistaConfiguracion", configuracion.gameObject);
        Asignar(controlador, "textoObjetivo", objetivo);
        Asignar(controlador, "textoRequisitos", requisitos);
        Asignar(controlador, "textoLimites", limites);
        Asignar(controlador, "textoColores", semaforo);
        Asignar(controlador, "botonContinuar", continuar);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Mision.unity");
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
