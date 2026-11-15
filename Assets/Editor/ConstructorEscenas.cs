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
public static partial class ConstructorEscenas
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

    static readonly string[] NombresPasos = { "Briefing", "Setup", "Review", "Operation" };

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
        CrearDatosSatelites();
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

        Texto(contenido, "Titulo", "Operation Red Eclipse", 96, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        Texto(contenido, "Premisa", "A strange signal appears on Mars and a dust storm makes the investigation harder.",
            40, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        var iniciar = Boton(contenido, "BotonIniciar", "Start mission", true);
        var comoJugar = Boton(contenido, "BotonComoJugar", "How to play", false);

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

        // 3D «Centro de control en alerta»: Marte entero a la derecha, una órbita cian fina con el MRO de NASA
        // recorriéndola y el pulso de la región de la anomalía.
        PrepararCamara();
        Camera.main.backgroundColor = Hex("#080D17");
        // Lente más cerrada y cámara más atrás (mismo encuadre): con 60° y Marte cerca y a un lado, la perspectiva
        // lo estiraba y se veía ovalado.
        Camera.main.fieldOfView = 32f;
        Camera.main.transform.position = new Vector3(0, 0.2f, -26.2f);
        ConstruirEstrellas();
        var centroMarte = new Vector3(6.6f, 0.1f, 9f);
        var marte = ConstruirMarte(centroMarte, 13.5f);
        Asignar(marte.AddComponent<GiroLento>(), "gradosPorSegundo", 1.2f);

        // Órbita que se nota: línea cian más gruesa, un halo ancho y tenue con la misma forma y una estela
        // aditiva que sigue al satélite (más grande que antes, para que se lea a primera vista).
        var orbita = new GameObject("OrbitaMenu", typeof(LineRenderer));
        orbita.transform.position = centroMarte;
        var lineaOrbita = orbita.GetComponent<LineRenderer>();
        lineaOrbita.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat")
                                     ?? new Material(Shader.Find("Sprites/Default"));
        lineaOrbita.widthMultiplier = 0.05f;
        lineaOrbita.startColor = lineaOrbita.endColor = new Color(0.41f, 0.78f, 0.82f, 0.75f); // #69C6D2
        lineaOrbita.shadowCastingMode = ShadowCastingMode.Off;
        var brilloOrbita = LineaHija(orbita.transform, "Brillo", MaterialAditivo("Brillo_Orbita", null, Color.white), 0.32f,
            new Color(0.37f, 0.78f, 0.9f, 0.10f), new Color(0.37f, 0.78f, 0.9f, 0.10f));
        var estela = LineaHija(orbita.transform, "Estela", MaterialAditivo("Brillo_Orbita", null, Color.white), 0.2f,
            new Color(0.37f, 0.85f, 1f, 0f), new Color(0.6f, 0.95f, 1f, 0.95f));
        estela.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1f));
        var satelite = InstanciarModelo("NASA/Sat_MRO", "SateliteMRO", null, 2.1f, new Vector3(180f, 0f, 0f), true);
        var pivoteSat = new GameObject("SateliteEnOrbita").transform;
        satelite.transform.SetParent(pivoteSat, false);
        satelite.transform.localPosition = Vector3.zero;
        var decorativa = orbita.AddComponent<OrbitaDecorativa>();
        Asignar(decorativa, "satelite", pivoteSat);
        Asignar(decorativa, "semiejeMayor", 9.6f);
        Asignar(decorativa, "excentricidad", 0.18f);
        Asignar(decorativa, "estela", estela);
        Asignar(decorativa, "brillo", brilloOrbita);
        var soOrbita = new SerializedObject(decorativa);
        soOrbita.FindProperty("inclinacion").vector3Value = new Vector3(76f, 0f, 24f);
        soOrbita.ApplyModifiedPropertiesWithoutUndo();
        decorativa.Dibujar();
        // Sin sombras del satélite sobre el planeta (a esta escala se verían como una mancha enorme).
        foreach (var r in satelite.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

        // «Eclipse»: el Sol asoma por detrás del borde de Marte (halo grande y núcleo, aditivos, con un pulso lento).
        var eclipse = new GameObject("SolEclipse").transform;
        eclipse.position = centroMarte + new Vector3(5.4f, 5.6f, 5f);   // fuera del disco: asoma por el borde
        var matBrillo = MaterialAditivo("Brillo_Sol", null, Color.white);
        SpriteRenderer Resplandor(string nombre, Color color, float escala)
        {
            var sr = new GameObject(nombre, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sr.transform.SetParent(eclipse, false);
            sr.transform.localScale = Vector3.one * escala;
            sr.sprite = ConstructorEstilo.Sprite("Resplandor");
            sr.color = color;
            sr.sharedMaterial = matBrillo;
            return sr;
        }
        var corona = Resplandor("Corona", new Color(1f, 0.55f, 0.3f, 0.35f), 7f);
        Resplandor("Nucleo", new Color(1f, 0.88f, 0.7f, 0.85f), 2.2f);
        Asignar(eclipse.gameObject.AddComponent<BrilloRespira>(), "sprite", corona);

        // Pulso de la anomalía: un halo cálido que crece cada 6 s sobre la cara visible del planeta.
        var dirAnomalia = new Vector3(-0.42f, 0.22f, -1f).normalized;
        var anomalia = new GameObject("Anomalia").transform;
        anomalia.position = centroMarte + dirAnomalia * 13.5f * 0.505f;
        anomalia.rotation = Quaternion.LookRotation(-dirAnomalia);
        var matSprite = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Sprite_Aditivo.mat");
        if (matSprite == null)
        {
            matSprite = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(matSprite, "Assets/Materiales/Sprite_Aditivo.mat");
        }
        SpriteRenderer CrearSprite(string nombre, Sprite sprite, Color color, float escala)
        {
            var sr = new GameObject(nombre, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sr.transform.SetParent(anomalia, false);
            sr.transform.localScale = Vector3.one * escala;
            sr.sprite = sprite;
            sr.color = color;
            sr.sharedMaterial = matSprite;
            return sr;
        }
        var halo = CrearSprite("Pulso", sHalo, new Color(1f, 0.45f, 0.25f, 0.8f), 0.45f);
        CrearSprite("Centro", sCirculo, new Color(1f, 0.55f, 0.3f, 1f), 0.05f);
        var pulso = anomalia.gameObject.AddComponent<PulsoAnomalia>();
        Asignar(pulso, "anillo", halo);
        ConstruirIluminacion();

        var canvas = CrearCanvas();

        // Velo oscuro a la izquierda para que el texto se lea sobre la escena.
        var velo = Panel(canvas, "Velo", new Color(0.031f, 0.051f, 0.09f, 0.82f));
        Anclar(velo, Vector2.zero, new Vector2(0.44f, 1));
        Trazo(velo, "BordeDerecho", new Vector2(1, 0), Vector2.one, new Vector2(-GrosorLinea, 0), Vector2.zero, LineaHud);

        var contenido = Columna(canvas, "Contenido", 22, TextAnchor.UpperLeft);
        Anclar(contenido, new Vector2(0.05f, 0.16f), new Vector2(0.41f, 0.9f));
        var kicker = Texto(contenido, "Etiqueta", "SPACE MISSION  /  01", 26, Cian, FontStyles.Bold, TextAlignmentOptions.Left);
        kicker.characterSpacing = 8;
        kicker.font = ConstructorEstilo.Mono;
        var titulo = Texto(contenido, "Titulo", "Operation\n<color=#" + ColorUtility.ToHtmlStringRGB(Acento) + ">Red</color> Eclipse",
            92, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.fontStyle = FontStyles.UpperCase;
        titulo.lineSpacing = -8;
        titulo.characterSpacing = 3;
        Texto(contenido, "Lema", "The signal shouldn't be there.", 40, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Texto(contenido, "Premisa", "An anomaly appears beneath the Martian storm. Design an orbital mission and find out what lies under the surface.",
            30, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Alto(Vacio(contenido, "Espacio"), 12);
        var iniciar = Boton(contenido, "BotonIniciar", SpaceMission.Iconos.Lanzador + "   Start mission  " + SpaceMission.Iconos.Siguiente, true);
        var secundarios = Fila(contenido, "Secundarios", 12);
        Alto(secundarios, 72);
        var comoJugar = Boton(secundarios, "BotonComoJugar", SpaceMission.Iconos.Informe + "  How to play", false);
        var opciones = Boton(secundarios, "BotonOpciones", SpaceMission.Iconos.Ajustes + "  Options", false);
        var creditos = Boton(secundarios, "BotonCreditos", SpaceMission.Iconos.Satelite + "  Credits", false);
        foreach (var b in new[] { comoJugar, opciones, creditos })
        {
            var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 26; t.enableAutoSizing = true; t.fontSizeMin = 20; t.fontSizeMax = 26; t.characterSpacing = 1;
        }
        contenido.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;
        secundarios.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 640;

        // Arriba a la derecha: aviso de la señal con un punto que late.
        var aviso = Fila(canvas, "AvisoSenal", 14);
        Anclar(aviso, new Vector2(0.71f, 0.905f), new Vector2(0.965f, 0.955f));
        aviso.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.031f, 0.051f, 0.09f, 0.75f);
        Bordes(aviso, LineaHud);
        var filaAviso = aviso.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        filaAviso.padding = new RectOffset(22, 18, 0, 0);
        filaAviso.childForceExpandWidth = false;
        filaAviso.childAlignment = TextAnchor.MiddleLeft;
        var contPunto = Vacio(aviso, "Punto");
        var lePunto = contPunto.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        lePunto.preferredWidth = 18; lePunto.preferredHeight = 18;
        var punto = new GameObject("Luz", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        punto.transform.SetParent(contPunto, false);
        punto.sprite = sCirculo; punto.color = Acento; punto.raycastTarget = false;
        Estirar(punto.rectTransform);
        var latido = new GameObject("Latido", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        latido.transform.SetParent(contPunto, false);
        latido.sprite = sHalo; latido.color = Acento; latido.raycastTarget = false;
        Estirar(latido.rectTransform);
        var pulsoUI = contPunto.gameObject.AddComponent<PulsoAnomalia>();
        Asignar(pulsoUI, "anilloUI", latido);
        Asignar(pulsoUI, "periodo", 2f);
        Asignar(pulsoUI, "duracion", 1.2f);
        var textoAviso = Texto(aviso, "Texto", "ANOMALOUS SIGNAL DETECTED", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        textoAviso.font = ConstructorEstilo.Mono;
        textoAviso.characterSpacing = 4;

        // Abajo a la derecha: el objetivo de la misión.
        var objetivo = Columna(canvas, "Objetivo", 2, TextAnchor.MiddleLeft, new Color(0.031f, 0.051f, 0.09f, 0.75f), 16);
        Anclar(objetivo, new Vector2(0.56f, 0.035f), new Vector2(0.76f, 0.125f));
        Bordes(objetivo, LineaHud);
        var etObjetivo = Texto(objetivo, "Etiqueta", "TARGET", 22, Cian, FontStyles.Bold, TextAlignmentOptions.Left);
        etObjetivo.characterSpacing = 6;
        Texto(objetivo, "Valor", "MARS · ORBITAL MISSION", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);

        // Abajo a la izquierda: el patrocinador y, debajo, el aviso de valores ficticios (más corto: el detalle
        // está en Créditos).
        var patrocinioMenu = FranjaPatrocinio(canvas, "Patrocinio", "SPONSORED BY", 84);
        var grupoPatrocinio = patrocinioMenu.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        grupoPatrocinio.childAlignment = TextAnchor.MiddleLeft;
        Anclar(patrocinioMenu, new Vector2(0.05f, 0.055f), new Vector2(0.41f, 0.14f));
        var pie = Texto(canvas, "Pie", "Fictional values, not official NASA data. NASA does not sponsor this game. Image and model sources in Credits.",
            18, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        Anclar(pie.rectTransform, new Vector2(0.05f, 0.012f), new Vector2(0.43f, 0.05f));
        pie.enableAutoSizing = true; pie.fontSizeMin = 14; pie.fontSizeMax = 18;
        var teclas = Texto(canvas, "Teclas", Tecla("ENTER") + " Select     " + Tecla("↑ ↓ ← →") + " Move",
            22, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        Anclar(teclas.rectTransform, new Vector2(0.765f, 0.035f), new Vector2(0.97f, 0.125f));
        teclas.enableAutoSizing = true; teclas.fontSizeMin = 18; teclas.fontSizeMax = 22;
        teclas.textWrappingMode = TextWrappingModes.NoWrap;

        // Paneles encima de todo, en este orden: Opciones, Cómo jugar y el velo de título.
        var opcionesUI = ConstruirOpciones(canvas, opciones);
        var (panel, cerrar) = ConstruirComoJugar(canvas);
        ConstruirCreditos(canvas, creditos);
        ConstruirPantallaTitulo(canvas, iniciar.gameObject, velo, contenido, aviso, objetivo, patrocinioMenu, pie.rectTransform, teclas.rectTransform);

        CrearEventSystem();
        // Con el foco puesto en «Iniciar misión», Enter y las flechas funcionan desde el principio.
        Object.FindAnyObjectByType<EventSystem>().firstSelectedGameObject = iniciar.gameObject;
        CrearMusica(ArteDatos.Musica.Menu, ArteDatos.Ambiente.Control);

        var controlador = new GameObject("MenuUI").AddComponent<MenuUI>();
        Asignar(controlador, "botonIniciar", iniciar);
        Asignar(controlador, "botonComoJugar", comoJugar);
        Asignar(controlador, "botonCerrarComoJugar", cerrar);
        Asignar(controlador, "panelComoJugar", panel.gameObject);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Menu.unity");
    }

    static void ConstruirCreditos(RectTransform canvas, UnityEngine.UI.Button abrir)
    {
        var panel = Panel(canvas, "PanelCreditos", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 20, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.16f, 0.1f), new Vector2(0.84f, 0.9f));
        MarcoHud(tarjeta);
        Texto(tarjeta, "TituloCreditos", "Credits", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        string Seccion(string titulo, string texto) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(Cian) + "><b>" + titulo + "</b></color>\n" + texto;
        var lista = Texto(tarjeta, "Lista",
            Seccion("TEAM", "Adrian Robles · Diego Esparza — NASA Space Apps Challenge 2026") + "\n\n" +
            Seccion("MARS IMAGERY", "NASA/JPL-Caltech, MSSS, University of Arizona (HiRISE), Arizona State University (THEMIS), " +
                "ASI (SHARAD) and JHUAPL (CRISM). Mars and Earth textures: Solar System Scope (CC BY 4.0).") + "\n\n" +
            Seccion("STARS", "Deep Star Maps 2020: NASA/Goddard Space Flight Center Scientific Visualization Studio. Gaia DR2: ESA/Gaia/DPAC.") + "\n\n" +
            Seccion("3D MODELS", "NASA 3D Resources: Mars Reconnaissance Orbiter, Magellan, Chandra, Aquarius, LLCD and Space Launch System. " +
                "Instruments: the team's own kit.") + "\n\n" +
            Seccion("AUDIO", "Music, sound effects and voices generated with ElevenLabs.") + "\n\n" +
            Seccion("FONTS AND ICONS", "Rajdhani, Barlow and IBM Plex Mono (SIL OFL 1.1). Lucide icons (ISC).") + "\n\n" +
            "<color=#" + ColorUtility.ToHtmlStringRGB(TextoSecundario) + ">NASA does not sponsor or endorse this game. " +
            "Mass, power and cost values are fictional. Full details in CREDITOS.md.</color>",
            28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lista.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        lista.enableAutoSizing = true; lista.fontSizeMin = 20; lista.fontSizeMax = 28;   // el español es más largo
        var cerrar = Boton(tarjeta, "BotonCerrarCreditos", "Close", true);
        panel.gameObject.SetActive(false);
        var control = panel.parent.gameObject.AddComponent<AbrirPanel>();
        Asignar(control, "abrir", abrir);
        Asignar(control, "cerrar", cerrar);
        Asignar(control, "panel", panel.gameObject);
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
        Texto(tarjeta, "TituloComoJugar", "How to play", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        string PasoJuego(string icono, string texto) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(Cian) + ">" + icono + "</color>  " + texto;
        var pasos = Texto(tarjeta, "Pasos",
            PasoJuego(SpaceMission.Iconos.Orbita, "Choose an orbit, a launcher and the science instruments.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Masa, "Stay within the mass, power, budget and slot limits.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Verde, "The STATUS light tells you whether you can launch and whether any evidence is missing.") + "\n" +
            PasoJuego(SpaceMission.Iconos.Senal, "After launch, follow the spacecraft's transmissions and get a report that explains the outcome."),
            34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        pasos.lineSpacing = 30;
        pasos.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cerrar = Boton(tarjeta, "BotonCerrar", "Got it", true);
        panel.gameObject.SetActive(false);
        return (panel, cerrar);
    }

    static OpcionesUI ConstruirOpciones(RectTransform canvas, UnityEngine.UI.Button abrir)
    {
        var panel = Panel(canvas, "PanelOpciones", new Color(0, 0, 0, 0.85f));
        Estirar(panel);
        var tarjeta = Columna(panel, "Tarjeta", 22, TextAnchor.UpperLeft, Tarjeta, 48);
        Anclar(tarjeta, new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.94f));
        tarjeta.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 14;
        MarcoHud(tarjeta);
        Texto(tarjeta, "TituloOpciones", "Options", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        var musica = Deslizador(tarjeta, "Musica", SpaceMission.Iconos.Volumen + "  Music");
        var ambiente = Deslizador(tarjeta, "Ambiente", SpaceMission.Iconos.Volumen + "  Ambience");
        var efectos = Deslizador(tarjeta, "Efectos", SpaceMission.Iconos.Volumen + "  Interface effects");
        var voz = Deslizador(tarjeta, "Voz", SpaceMission.Iconos.Senal + "  Transmission voice");
        var silencio = Casilla(tarjeta, "Silencio", "Mute all sound");
        var movimiento = Casilla(tarjeta, "ReducirMovimiento", "Reduce motion (no shaking or cinematics)");
        // Versión 5, bloque 5: idioma.
        Texto(tarjeta, "EtiquetaIdioma", "Language", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        ConstruirSelectorIdioma(tarjeta, "SelectorIdiomaOpciones", 56);
        Alto(Vacio(tarjeta, "Espacio"), 8).GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var cerrar = Boton(tarjeta, "BotonCerrarOpciones", "Done", true);
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
        Asignar(ui, "ambiente", ambiente);
        Asignar(ui, "silencio", silencio);
        return ui;
    }

    /// <summary>Versión 5, bloque 5: botones «English» y «Español» con SelectorIdioma.</summary>
    static RectTransform ConstruirSelectorIdioma(Transform padre, string nombre, float alto)
    {
        var fila = Fila(padre, nombre, 12);
        Alto(fila, alto);
        var ingles = Boton(fila, "BotonIngles", "English", false);
        var espanol = Boton(fila, "BotonEspanol", "Español", false);
        foreach (var b in new[] { ingles, espanol })
        {
            var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight = alto; le.preferredHeight = alto; le.preferredWidth = -1; le.flexibleWidth = 1;
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = 28;
        }
        var ui = fila.gameObject.AddComponent<SelectorIdioma>();
        Asignar(ui, "botonIngles", ingles);
        Asignar(ui, "botonEspanol", espanol);
        return fila;
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

    static void ConstruirPantallaTitulo(RectTransform canvas, GameObject foco, params RectTransform[] entradas)
    {
        // Velo con viñeta: el centro deja ver Marte y los bordes se oscurecen. Debajo de la viñeta, un tinte
        // del fondo para que el título se lea sobre el planeta.
        var velo = Panel(canvas, "PantallaTitulo", new Color(Fondo.r, Fondo.g, Fondo.b, 0.55f));
        Estirar(velo);
        velo.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var vineta = new GameObject("Vineta", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        vineta.transform.SetParent(velo, false);
        vineta.sprite = ConstructorEstilo.Sprite("Vineta");
        vineta.color = Color.white;
        vineta.raycastTarget = false;
        Estirar(vineta.rectTransform);

        var col = Columna(velo, "Contenido", 14, TextAnchor.MiddleCenter);
        Anclar(col, new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.8f));
        var etiqueta = Texto(col, "Etiqueta", "NASA SPACE APPS CHALLENGE 2026  ·  TEAM GANGBANGERS", 26, Cian,
            FontStyles.Normal, TextAlignmentOptions.Center);
        etiqueta.font = ConstructorEstilo.Mono;
        etiqueta.characterSpacing = 6;
        var titulo = Texto(col, "TituloJuego", "Operation <color=#" + ColorUtility.ToHtmlStringRGB(Acento) + ">Red</color> Eclipse",
            136, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        titulo.fontStyle = FontStyles.UpperCase;
        titulo.characterSpacing = 6;
        titulo.textWrappingMode = TextWrappingModes.NoWrap;
        titulo.enableAutoSizing = true; titulo.fontSizeMin = 60; titulo.fontSizeMax = 147;
        var linea = Panel(col, "LineaTitulo", Acento);
        linea.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Alto(linea, 3);
        var leLinea = linea.GetComponent<UnityEngine.UI.LayoutElement>();
        leLinea.preferredWidth = 620; leLinea.flexibleWidth = 0;
        col.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childForceExpandWidth = false;
        var lema = Texto(col, "Lema", "The signal shouldn't be there.", 38, TextoSecundario, FontStyles.Italic, TextAlignmentOptions.Center);

        var aviso = Texto(velo, "Aviso", "PRESS ANY KEY TO START", 30, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        aviso.characterSpacing = 8;
        Anclar(aviso.rectTransform, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.27f));

        // Versión 5, bloque 5: idioma, arriba a la derecha (PantallaTitulo no se cierra al pulsarlo).
        var selector = ConstruirSelectorIdioma(velo, "SelectorIdiomaTitulo", 56);
        selector.anchorMin = selector.anchorMax = selector.pivot = new Vector2(1, 1);
        selector.sizeDelta = new Vector2(420, 56);
        selector.anchoredPosition = new Vector2(-40, -36);

        var patrocinio = FranjaPatrocinio(velo, "Patrocinio", "SPONSORED BY", 110);
        Anclar(patrocinio, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.17f));

        // Destello al cerrar (blanco, transparente al empezar).
        var destello = Panel(velo, "Destello", new Color(1f, 0.95f, 0.9f, 0f));
        destello.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Estirar(destello);

        var fuente = velo.gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        var ui = velo.gameObject.AddComponent<PantallaTitulo>();
        Asignar(ui, "aviso", aviso);
        Asignar(ui, "focoAlCerrar", foco);
        Asignar(ui, "etiqueta", etiqueta);
        Asignar(ui, "titulo", titulo);
        Asignar(ui, "linea", linea);
        Asignar(ui, "lema", lema);
        Asignar(ui, "patrocinio", patrocinio.GetComponent<CanvasGroup>());
        Asignar(ui, "contenido", col);
        Asignar(ui, "destello", destello.GetComponent<UnityEngine.UI.Image>());
        Asignar(ui, "selectorIdioma", selector);
        var grupos = new List<CanvasGroup>();
        foreach (var e in entradas)
        {
            var g = e.GetComponent<CanvasGroup>();
            if (g == null) g = e.gameObject.AddComponent<CanvasGroup>();
            grupos.Add(g);
        }
        AsignarLista(ui, "entradas", grupos.ToArray());
        if (arte != null && arte.sonidoTitulo != null) Asignar(ui, "sonidoCerrar", arte.sonidoTitulo);
    }

    /// <summary>
    /// Franja «Patrocinado por» con los dos logos de National Bulwark Services (versión clara, para fondos
    /// oscuros). Lleva un CanvasGroup para poder animarla. No bloquea clics.
    /// </summary>
    static RectTransform FranjaPatrocinio(Transform padre, string nombre, string texto, float altoLogo)
    {
        var fila = Fila(padre, nombre, 22);
        fila.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        var grupo = fila.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        grupo.childForceExpandWidth = false;
        grupo.childForceExpandHeight = false;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childAlignment = TextAnchor.MiddleCenter;
        var etiqueta = Texto(fila, "Etiqueta", texto, 20, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        etiqueta.font = ConstructorEstilo.Mono;
        etiqueta.characterSpacing = 6;
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        foreach (var cual in new[] { "Fence", "Security" })
        {
            var img = new GameObject("Logo" + cual, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement))
                .GetComponent<UnityEngine.UI.Image>();
            img.transform.SetParent(fila, false);
            img.sprite = ConstructorEstilo.Patrocinio(cual, true);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var le = img.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = altoLogo; le.preferredHeight = altoLogo; le.minHeight = altoLogo;
        }
        return fila;
    }

    // ---------- Mision ----------

    static void ConstruirMision()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvas = CrearCanvas();
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        // Vista Briefing: expediente de la misión sobre el Marte 3D (versión 6: el mismo planeta del menú y de la
        // configuración, en vez de una foto). Un velo oscuro a la izquierda deja leer el texto.
        var briefing = Panel(canvas, "VistaBriefing", new Color(0, 0, 0, 0));
        Estirar(briefing);
        var velo = new GameObject("VeloBriefing", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        velo.transform.SetParent(briefing, false);
        velo.texture = ConstructorEstilo.Imagen("Interfaz/Degradado_Izquierda");
        velo.raycastTarget = false;
        Estirar(velo.rectTransform);

        var ruta = Texto(briefing, "Ruta", Ruta(1), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        ruta.font = ConstructorEstilo.Mono;
        Anclar(ruta.rectTransform, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.97f));
        var lineaRuta = Panel(briefing, "LineaRuta", LineaHud);
        lineaRuta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        Anclar(lineaRuta, new Vector2(0.05f, 0.915f), new Vector2(0.95f, 0.915f));
        lineaRuta.offsetMax = new Vector2(0, GrosorLinea);

        // Versión 6: solo lo esencial (objetivo, lo que necesitas y el nivel). Los límites y el semáforo están en
        // el «?» del Briefing y en las barras de la Configuración; Marte se ve limpio a la derecha.
        var col = Columna(briefing, "Expediente", 22, TextAnchor.UpperLeft);
        Anclar(col, new Vector2(0.05f, 0.17f), new Vector2(0.47f, 0.885f));
        col.gameObject.AddComponent<CanvasGroup>();
        Texto(col, "Titulo", "Mission briefing", 64, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Encabezado(col, "EtiquetaObjetivo", "OBJECTIVE");
        var objetivo = Texto(col, "Objetivo", "", 34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        objetivo.lineSpacing = 6;
        Encabezado(col, "EtiquetaRequisitos", "YOU NEED");
        var requisitos = Texto(col, "Requisitos", "", 32, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        requisitos.lineSpacing = 14;

        // Selector de nivel (gameplay aprobado): los textos y colores los pone MisionUI con los datos.
        Encabezado(col, "EtiquetaNivel", "DIFFICULTY LEVEL");
        var filaNivel = Fila(col, "Niveles", 16);
        Alto(filaNivel, 64);
        var botonesNivel = new UnityEngine.UI.Button[3];
        for (int i = 0; i < 3; i++)
        {
            botonesNivel[i] = Boton(filaNivel, "BotonNivel" + (i + 1), "Level " + (i + 1), false);
            var le = botonesNivel[i].GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 64; le.preferredHeight = 64;
            var t = botonesNivel[i].GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 28; t.enableAutoSizing = true; t.fontSizeMin = 24; t.fontSizeMax = 28;
        }
        // Una sola línea con lo que cambia el nivel (presupuesto, meses y eventos).
        var limites = Texto(col, "ResumenNivel", "", 26, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);

        var continuar = Boton(briefing, "BotonContinuar", "Continue  " + SpaceMission.Iconos.Siguiente, true);
        Fijar(continuar.GetComponent<RectTransform>(), new Vector2(0.95f, 0.05f), new Vector2(1, 0), new Vector2(520, 88));
        // Versión 7: volver al menú, abajo a la izquierda (la tarjeta de consejos va a su derecha).
        var botonMenu = Boton(briefing, "BotonMenu", SpaceMission.Iconos.Atras + "  Menu", false);
        Fijar(botonMenu.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0, 0), new Vector2(220, 88));
        AjustarTexto(botonMenu);
        // Versión 5, bloque 4: la Misión 0 (tutorial), a la izquierda de «Continue». MisionUI pone el texto.
        var botonTutorial = Boton(briefing, "BotonTutorial", "Mission 0: tutorial", false);
        Fijar(botonTutorial.GetComponent<RectTransform>(), new Vector2(0.667f, 0.05f), new Vector2(1, 0), new Vector2(460, 88));
        var etiquetaTutorial = botonTutorial.GetComponentInChildren<TextMeshProUGUI>();
        etiquetaTutorial.enableAutoSizing = true; etiquetaTutorial.fontSizeMin = 20; etiquetaTutorial.fontSizeMax = 30;
        Aparecer(briefing, col);

        var controlador = new GameObject("MisionUI").AddComponent<MisionUI>();
        AsignarLista(controlador, "botonesNivel", botonesNivel);
        Asignar(controlador, "botonMenu", botonMenu);

        // Escena 3D de Configuración: estrellas, Marte abajo con la órbita elegida y la nave en el centro.
        PrepararCamara();
        ConstruirEstrellas();
        var marte = ConstruirMarte(new Vector3(0, -17.5f, 16f), 26f);
        Asignar(marte.AddComponent<GiroLento>(), "gradosPorSegundo", 1.5f);
        var anillo = ConstruirAnilloOrbita(marte.transform, catalogo);
        var nave = ConstruirNave();
        // Más pequeña que en el Menú para que quepa entera entre los dos paneles de Configuración.
        nave.transform.position = new Vector3(0, 0.55f, 0);
        nave.transform.localScale = Vector3.one * 0.6f;
        var cohetes = ConstruirCohetes(Plataforma);
        ConstruirEstrellas(Plataforma + new Vector3(0, 4f, 60f));
        ConstruirIluminacion();

        // Vista Configuración: transparente en el centro para que se vea la nave 3D.
        var configuracion = Vacio(canvas, "VistaConfiguracion");
        Estirar(configuracion);
        configuracion.SetAsFirstSibling(); // debajo del Briefing
        var configUI = ConstruirConfiguracion(configuracion, catalogo, controlador, nave);
        Asignar(anillo, "configuracion", configUI);
        var marteCentro = marte.transform.position;

        // Vista Revisión: tarjeta encima de la configuración.
        var revision = Panel(canvas, "VistaRevision", new Color(0, 0, 0, 0.8f));
        Estirar(revision);
        var revisionUI = ConstruirRevision(revision, catalogo, controlador);

        // Vista Carga (gameplay v3): herramientas que suben a la nave, entre Revisión y Lanzar.
        var vistaCarga = Panel(canvas, "VistaCarga", new Color(0, 0, 0, 0.8f));
        Estirar(vistaCarga);
        var cargaUI = ConstruirCarga(vistaCarga, catalogo, controlador);

        // Vista Operación (sustituye a la Investigación): mapa, energía, órbitas, eventos y herramientas,
        // con la imagen real del instrumento de cada transmisión.
        var investigacion = Panel(canvas, "VistaOperacion", Fondo);
        Estirar(investigacion);
        var operacionUI = ConstruirOperacion(investigacion, catalogo);
        var tutorialUI = ConstruirTutorial(investigacion, controlador, operacionUI);
        Asignar(controlador, "botonTutorial", botonTutorial);
        Asignar(controlador, "tutorial", tutorialUI);

        // Versión 7: ambiente de sistemas de la sala de control mientras se prepara la misión.
        var musica = CrearMusica(ArteDatos.Musica.Configuracion, ArteDatos.Ambiente.Configuracion);
        // Cinemática Tierra → Marte al lanzar y vista orbital en cada escaneo (ConstructorEscenas.Cine.cs).
        // Con la Carga (gameplay v3) la cinemática empieza en su «Lanzar», no en el de la Revisión.
        ConstruirCinematica(canvas, cargaUI.BotonLanzar, musica, operacionUI, catalogo);

        // Navegación de Configuración y cámara que se acerca a lo que se está eligiendo.
        Asignar(configuracion.GetComponent<GuiaConfiguracion>(), "vistaRevision", revision.gameObject);
        var camara = Camera.main.gameObject.AddComponent<CamaraConfiguracion>();
        Asignar(camara, "configuracion", configUI);
        Asignar(camara, "vistaConfiguracion", configuracion.gameObject);
        AsignarEncuadre(camara, "nave", new Vector3(0, 0.45f, -7.4f), new Vector3(0, 0.5f, 0), 60f);
        AsignarEncuadre(camara, "planeta", marteCentro + new Vector3(0, 12f, -45f), marteCentro + new Vector3(0, 1.5f, 0), 60f);
        // Briefing: Marte grande a la derecha, detrás del expediente.
        AsignarEncuadre(camara, "briefing", marteCentro + new Vector3(-13f, 11f, -40f), marteCentro + new Vector3(-12f, 9f, 0), 55f);
        Asignar(camara, "vistaBriefing", briefing.gameObject);
        AsignarEncuadre(camara, "plataforma", Plataforma + new Vector3(0, 5.3f, -17f), Plataforma + new Vector3(0, 5f, 0), 60f);
        Asignar(camara, "raizNave", nave.transform);
        // Pestaña Lanzador: sin elección los dos cohetes; con elección solo el elegido, en el centro, con un
        // encuadre a su altura (Explorador 6.1, Titán 11).
        var plataformaUI = cohetes.explorador.transform.parent.gameObject.AddComponent<PlataformaCohetes>();
        Asignar(plataformaUI, "explorador", cohetes.explorador.transform);
        Asignar(plataformaUI, "titan", cohetes.titan.transform);
        Asignar(plataformaUI, "resalteExplorador", cohetes.resaltadoExplorador.transform);
        Asignar(plataformaUI, "resalteTitan", cohetes.resaltadoTitan.transform);
        AsignarEncuadre(camara, "plataformaExplorador", Plataforma + new Vector3(2.2f, 3.6f, -10.5f), Plataforma + new Vector3(0, 3.3f, 0), 55f);
        AsignarEncuadre(camara, "plataformaTitan", Plataforma + new Vector3(3.2f, 6.2f, -17.5f), Plataforma + new Vector3(0, 5.8f, 0), 55f);
        Asignar(camara, "catalogo", catalogo);
        // Etiqueta del módulo instalado: línea guía y punto sobre el módulo (debajo de la etiqueta).
        var guia = Panel(configuracion, "GuiaInstalado", new Color(Cian.r, Cian.g, Cian.b, 0.85f));
        guia.anchorMin = guia.anchorMax = new Vector2(0.5f, 0.5f);
        guia.sizeDelta = new Vector2(100, 2);
        guia.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        guia.gameObject.SetActive(false);
        var punto = new GameObject("PuntoInstalado", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        punto.transform.SetParent(configuracion, false);
        punto.rectTransform.anchorMin = punto.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        punto.rectTransform.sizeDelta = new Vector2(14, 14);
        punto.sprite = sCirculo;
        punto.color = Cian;
        punto.raycastTarget = false;
        punto.gameObject.SetActive(false);
        Asignar(camara, "guiaFoco", guia);
        Asignar(camara, "puntoFoco", punto.rectTransform);
        var etiqueta = Columna(configuracion, "EtiquetaInstalado", 2, TextAnchor.MiddleCenter, new Color(Fondo.r, Fondo.g, Fondo.b, 0.88f), 10);
        etiqueta.anchorMin = etiqueta.anchorMax = new Vector2(0.5f, 0.5f);
        etiqueta.sizeDelta = new Vector2(330, 64);
        etiqueta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        etiqueta.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().childControlHeight = true;
        MarcoHud(etiqueta);
        var textoEtiqueta = Texto(etiqueta, "Texto", "", 21, Cian, FontStyles.Bold, TextAlignmentOptions.Center);
        textoEtiqueta.enableAutoSizing = true; textoEtiqueta.fontSizeMin = 15; textoEtiqueta.fontSizeMax = 22;
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
        Asignar(controlador, "vistaCarga", vistaCarga.gameObject);
        Asignar(controlador, "carga", cargaUI);
        Asignar(controlador, "textoObjetivo", objetivo);
        Asignar(controlador, "textoRequisitos", requisitos);
        Asignar(controlador, "textoLimites", limites);
        Asignar(controlador, "botonContinuar", continuar);

        // Versión 6: guía de cada pantalla y botón «?», encima de todas las vistas.
        ConstruirGuiaPantallas(canvas, controlador, configUI, cargaUI, Object.FindFirstObjectByType<DirectorCinematica>(FindObjectsInactive.Include));

        // Al abrir la escena solo se ve el Briefing (MisionUI también lo hace al empezar).
        configuracion.gameObject.SetActive(false);
        revision.gameObject.SetActive(false);
        vistaCarga.gameObject.SetActive(false);
        investigacion.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(escena, "Assets/Escenas/Mision.unity");
    }

    /// <summary>
    /// Versión 6: tarjeta de la guía (bombilla, siguiente paso y «×»), botón «?» y panel con la explicación completa de
    /// la pantalla. GuiaPantallasUI la mueve según la pantalla.
    /// </summary>
    static void ConstruirGuiaPantallas(RectTransform canvas, MisionUI mision, ConfiguracionUI configuracion, CargaUI carga,
        DirectorCinematica cinematica)
    {
        var raiz = Fila(canvas, "GuiaPantallas", 8);
        var grupo = raiz.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        grupo.childForceExpandWidth = false;
        grupo.childForceExpandHeight = false;
        grupo.childAlignment = TextAnchor.UpperLeft;
        Anclar(raiz, new Vector2(0.37f, 0.8f), new Vector2(0.63f, 0.875f));

        var tarjeta = Fila(raiz, "TarjetaGuia", 8);
        var fondo = tarjeta.gameObject.AddComponent<UnityEngine.UI.Image>();
        fondo.color = new Color(Fondo.r, Fondo.g, Fondo.b, 0.9f);
        var leTarjeta = tarjeta.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leTarjeta.flexibleWidth = 1; leTarjeta.flexibleHeight = 1;
        var grupoTarjeta = tarjeta.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        grupoTarjeta.padding = new RectOffset(16, 6, 8, 8);
        grupoTarjeta.childForceExpandWidth = false;
        Bordes(tarjeta, new Color(Cian.r, Cian.g, Cian.b, 0.6f));
        var texto = Texto(tarjeta, "TextoGuiaPantalla", "", 22, TextoClaro, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        texto.enableAutoSizing = true; texto.fontSizeMin = 14; texto.fontSizeMax = 22;
        texto.overflowMode = TextOverflowModes.Truncate;
        texto.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        var ocultar = Boton(tarjeta, "BotonOcultarGuia", "×", false);
        ocultar.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);   // solo la «×», sin caja
        var textoOcultar = ocultar.GetComponentInChildren<TextMeshProUGUI>();
        textoOcultar.fontSize = 34; textoOcultar.color = TextoSecundario; textoOcultar.characterSpacing = 0;
        var leOcultar = ocultar.GetComponent<UnityEngine.UI.LayoutElement>();
        leOcultar.preferredWidth = 36; leOcultar.minWidth = 36; leOcultar.minHeight = 36; leOcultar.preferredHeight = 36; leOcultar.flexibleWidth = 0;

        // El «?» va aparte, en un sitio fijo de cada pantalla (GuiaPantallasUI.Colocar): nunca tapa texto.
        var ayuda = Boton(canvas, "BotonAyuda", "?", true);
        ayuda.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        ayuda.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32;

        // Panel de ayuda: velo que tapa todo y una tarjeta en el centro.
        var panel = Panel(canvas, "PanelAyuda", new Color(0, 0, 0, 0.78f));
        Estirar(panel);
        var caja = Columna(panel, "CajaAyuda", 18, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(caja, new Vector2(0.22f, 0.1f), new Vector2(0.78f, 0.9f));
        MarcoHud(caja);
        Encabezado(caja, "EtiquetaAyuda", SpaceMission.Iconos.Pista + "  HOW TO PLAY THIS SCREEN");
        var textoAyuda = Texto(caja, "TextoAyuda", "", 30, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        textoAyuda.lineSpacing = 10;
        textoAyuda.enableAutoSizing = true; textoAyuda.fontSizeMin = 16; textoAyuda.fontSizeMax = 30;
        textoAyuda.overflowMode = TextOverflowModes.Truncate;
        textoAyuda.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var filaAyuda = Fila(caja, "BotonesAyuda", 16);
        Alto(filaAyuda, 72);
        var alternar = Boton(filaAyuda, "BotonAlternarGuia", "Hide tips", false);
        var cerrar = Boton(filaAyuda, "BotonCerrarAyuda", "Close", true);
        foreach (var b in new[] { alternar, cerrar })
        {
            var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 72; le.preferredHeight = 72;
        }

        var ui = canvas.gameObject.AddComponent<GuiaPantallasUI>();
        Asignar(ui, "mision", mision);
        Asignar(ui, "configuracion", configuracion);
        Asignar(ui, "carga", carga);
        Asignar(ui, "cinematica", cinematica);
        Asignar(ui, "raiz", raiz);
        Asignar(ui, "tarjeta", tarjeta.gameObject);
        Asignar(ui, "texto", texto);
        Asignar(ui, "botonOcultar", ocultar);
        Asignar(ui, "botonAyuda", ayuda);
        Asignar(ui, "panelAyuda", panel.gameObject);
        Asignar(ui, "textoAyuda", textoAyuda);
        Asignar(ui, "botonAlternarGuia", alternar);
        Asignar(ui, "botonCerrarAyuda", cerrar);
        Asignar(ui, "rectAyuda", ayuda.GetComponent<RectTransform>());
        panel.gameObject.SetActive(false);
    }

    /// <summary>Vista Carga (gameplay v3): interfaz básica, misma tarjeta que la Revisión. Diego la estiliza.</summary>
    static CargaUI ConstruirCarga(RectTransform vista, CatalogoDatos catalogo, MisionUI mision)
    {
        var ui = vista.gameObject.AddComponent<CargaUI>();
        var tarjeta = Columna(vista, "Tarjeta", 12, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(tarjeta, new Vector2(0.22f, 0.04f), new Vector2(0.78f, 0.96f));
        tarjeta.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(tarjeta);
        var paso = Texto(tarjeta, "Paso", Paso(3), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        paso.font = ConstructorEstilo.Mono;
        Texto(tarjeta, "Titulo", "Spacecraft cargo", 56, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        var recursos = Texto(tarjeta, "TextoRecursos", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        recursos.lineSpacing = 4;
        var leRecursos = recursos.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leRecursos.minHeight = 240; leRecursos.preferredHeight = 300;
        Encoger(recursos, 17f);
        Encabezado(tarjeta, "EtiquetaHerramientas", SpaceMission.Iconos.Ajustes + "  TOOLS");
        var lista = Columna(tarjeta, "ListaCarga", 8, TextAnchor.UpperLeft);
        lista.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var plantilla = Boton(lista, "PlantillaCarga", "Tool", false);
        Opcion(plantilla, 26);
        plantilla.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 68;
        plantilla.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 68;
        plantilla.GetComponentInChildren<TextMeshProUGUI>().lineSpacing = 0;
        var botones = Fila(tarjeta, "Botones", 24);
        Alto(botones, 88);
        var volver = Boton(botones, "BotonVolverCarga", SpaceMission.Iconos.Atras + "  Review", false);
        AjustarTexto(volver);
        var usarMro = Boton(botones, "BotonUsarMro", "Use the MRO", false);
        AjustarTexto(usarMro);
        var lanzar = Boton(botones, "BotonLanzarCarga", SpaceMission.Iconos.Lanzador + "  Launch", true);
        AjustarTexto(lanzar);
        Aparecer(vista, tarjeta);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "textoRecursos", recursos);
        Asignar(ui, "listaHerramientas", lista);
        Asignar(ui, "plantillaHerramienta", plantilla);
        Asignar(ui, "botonVolver", volver);
        Asignar(ui, "botonLanzar", lanzar);
        Asignar(ui, "botonUsarMro", usarMro);
        return ui;
    }

    /// <summary>
    /// Versión 5, bloque 4: tarjeta de la Misión 0 sobre la parte baja del centro de la Operación (la anomalía del
    /// tutorial está arriba del mapa, así que no la tapa). TutorialUI la muestra y la oculta.
    /// </summary>
    static TutorialUI ConstruirTutorial(RectTransform vista, MisionUI mision, OperacionUI operacion)
    {
        var tarjeta = Columna(vista, "PanelTutorial", 10, TextAnchor.UpperLeft, Tarjeta, 24);
        Anclar(tarjeta, new Vector2(0.31f, 0.03f), new Vector2(0.69f, 0.36f));
        MarcoHud(tarjeta);
        var titulo = Texto(tarjeta, "TituloTutorial", "", 28, Cian, FontStyles.Bold, TextAlignmentOptions.Left);
        var cuerpo = Texto(tarjeta, "TextoTutorial", "", 22, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        cuerpo.enableAutoSizing = true; cuerpo.fontSizeMin = 15; cuerpo.fontSizeMax = 22;
        cuerpo.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var fila = Fila(tarjeta, "BotonesTutorial", 12);
        Alto(fila, 56);
        var saltar = BotonAccion(fila, "BotonSaltarTutorial", "Skip tutorial");
        var siguiente = Boton(fila, "BotonSiguienteTutorial", "Start", true);
        var le = siguiente.GetComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = 56; le.preferredHeight = 56; le.preferredWidth = -1; le.flexibleWidth = 1;
        siguiente.GetComponentInChildren<TextMeshProUGUI>().enableAutoSizing = true;
        siguiente.GetComponentInChildren<TextMeshProUGUI>().fontSizeMin = 18;
        siguiente.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = 28;

        var ui = vista.gameObject.AddComponent<TutorialUI>();
        Asignar(ui, "mision", mision);
        Asignar(ui, "operacion", operacion);
        Asignar(ui, "panel", tarjeta.gameObject);
        Asignar(ui, "textoTitulo", titulo);
        Asignar(ui, "textoCuerpo", cuerpo);
        Asignar(ui, "botonSiguiente", siguiente);
        Asignar(ui, "botonSaltar", saltar);
        tarjeta.gameObject.SetActive(false);
        return ui;
    }

    /// <summary>Botón de acción pequeño (52 px) dentro de un panel de la Operación.</summary>
    static UnityEngine.UI.Button BotonAccion(Transform padre, string nombre, string texto)
    {
        var b = Boton(padre, nombre, texto, false);
        Opcion(b, 24);
        var le = b.GetComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = 52; le.preferredHeight = 52;
        return b;
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
        Encabezado(izq, "Etiqueta", SpaceMission.Iconos.Instrumentos + "  INSTRUMENTS");
        Texto(izq, "Ayuda", "Click one to select it and turn it on; then click its region on the map.", 24,
            TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        var lista = Columna(izq, "ListaInstrumentos", 8, TextAnchor.UpperLeft);
        lista.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var plantilla = Boton(lista, "PlantillaInstrumento", "Instrument", false);
        Opcion(plantilla, 24);
        // Versión 5: buffs y debuffs activos (satélite, altitud, eventos, bloqueos), con flecha y color.
        var modificadores = Texto(izq, "TextoModificadores", "", 20, TextoClaro, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
        modificadores.enableAutoSizing = true; modificadores.fontSizeMin = 15; modificadores.fontSizeMax = 20;
        modificadores.overflowMode = TextOverflowModes.Truncate;
        var leMod = modificadores.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leMod.flexibleHeight = 1; leMod.minHeight = 60;

        var filaEnergia = Fila(izq, "FilaEnergia", 10);
        Alto(filaEnergia, 40);
        filaEnergia.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
        var iconoEnergia = Texto(filaEnergia, "Icono", SpaceMission.Iconos.Potencia, 30, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
        iconoEnergia.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 36;
        var energia = Texto(filaEnergia, "TextoEnergia", "Energy", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
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
        var orbitas = Texto(filaOrbitas, "TextoOrbitas", "Orbits", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        orbitas.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;

        // Gameplay v3: altitud y combustible.
        var combustible = Texto(izq, "TextoCombustible", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        var altitud = BotonAccion(izq, "BotonAltitud", "Descend to low orbit");

        // Imagen real (NASA) del instrumento que envió la última transmisión.
        Encabezado(izq, "EtiquetaSenal", SpaceMission.Iconos.Senal + "  LATEST SIGNAL");
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
        var textoSenal = Texto(senal, "Texto", SpaceMission.Iconos.Senal + "   SIGNAL RECEIVED", 32, Fondo, FontStyles.Bold,
            TextAlignmentOptions.Center);
        textoSenal.characterSpacing = 8;
        Estirar(textoSenal.rectTransform);
        MarcoHud(marcoImagen);

        // Centro: la guía (siguiente paso), el mapa cuadrado que se puede pulsar y la última lectura debajo.
        // Versión 5: donde antes decía «STEP 4 OF 4», la guía dice qué hacer ahora (OperacionUI.DibujarGuia).
        var paso = Texto(vista, "TextoGuia", Paso(4), 22, Cian, FontStyles.Bold, TextAlignmentOptions.Center);
        // Versión 6: la guía puede ocupar dos líneas (antes se cortaba); se achica antes de truncar.
        paso.enableAutoSizing = true; paso.fontSizeMin = 13; paso.fontSizeMax = 22;
        paso.textWrappingMode = TextWrappingModes.Normal;
        paso.overflowMode = TextOverflowModes.Truncate;
        Anclar(paso.rectTransform, new Vector2(0.31f, 0.92f), new Vector2(0.655f, 0.975f));   // a la derecha va el «?» (versión 6)
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
        ultimoMapa = goMapa.GetComponent<UnityEngine.UI.RawImage>();
        Asignar(mapa, "imagen", goMapa.GetComponent<UnityEngine.UI.RawImage>());
        Asignar(mapa, "capaMarcas", capaMarcas);
        Asignar(mapa, "texturaBase", ConstructorEstilo.Imagen("Texturas/Marte_Superficie"));
        var soMapa = new SerializedObject(mapa);
        soMapa.FindProperty("tamanoZona").floatValue = 0.18f;   // ~740x740 píxeles del mapa de 4096x2048: casi 1:1 en pantalla
        soMapa.ApplyModifiedPropertiesWithoutUndo();
        // Región de la misión: el cráter Gale (relieve de NASA 3D Resources) y marcas redondas.
        Asignar(mapa, "texturaRegion", ConstructorEstilo.Imagen("Texturas/Gale_Color"));
        Asignar(mapa, "spriteMarca", sCirculo);
        CapasMapa((RectTransform)goMapa.transform, capaMarcas);

        var cajaLectura = Columna(vista, "CajaLectura", 6, TextAnchor.UpperLeft, TarjetaTranslucida, 20);
        Anclar(cajaLectura, new Vector2(0.31f, 0.03f), new Vector2(0.69f, 0.185f));
        Bordes(cajaLectura, LineaHud);
        var lectura = Texto(cajaLectura, "TextoLectura", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        var leLectura = lectura.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leLectura.preferredHeight = 1; leLectura.flexibleHeight = 1;
        Encoger(lectura, 15f);

        // Derecha: transmisión, eventos, herramientas y fin.
        var der = Columna(vista, "PanelDerecho", 12, TextAnchor.UpperLeft, TarjetaTranslucida, 24);
        Anclar(der, new Vector2(0.70f, 0.03f), new Vector2(0.988f, 0.97f));
        der.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(der);
        Encabezado(der, "EtiquetaTransmision", SpaceMission.Iconos.Senal + "  TRANSMISSION");
        var transmision = Texto(der, "TextoTransmision", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        transmision.lineSpacing = 4;
        transmision.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        // Con las filas del gameplay v3 el panel tiene menos alto: el texto se achica para no pisar lo de abajo.
        transmision.enableAutoSizing = true;
        transmision.fontSizeMin = 16;
        transmision.fontSizeMax = 24;
        transmision.overflowMode = TextOverflowModes.Truncate;
        transmision.gameObject.AddComponent<Escritura>();
        var progreso = Texto(der, "TextoProgreso", "", 24, Cian, FontStyles.Normal, TextAlignmentOptions.Left);
        progreso.font = ConstructorEstilo.Mono;
        // Gameplay v3: memoria y envío a la Tierra.
        var memoria = Texto(der, "TextoMemoria", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
        Encoger(progreso, 16f);
        Encoger(memoria, 16f);
        var enviar = BotonAccion(der, "BotonEnviar", "Send at month end");
        Encabezado(der, "EtiquetaEventos", SpaceMission.Iconos.Amarillo + "  SPACECRAFT EVENTS");
        var evento = Texto(der, "TextoEvento", "", 24, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Encoger(evento, 16f);
        Encabezado(der, "EtiquetaHerramientas", SpaceMission.Iconos.Ajustes + "  TOOLS (SINGLE USE)");
        var herramientas = Columna(der, "ListaHerramientas", 6, TextAnchor.UpperLeft);
        var plantillaH = Boton(herramientas, "PlantillaHerramienta", "Tool", false);
        Opcion(plantillaH, 24);
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().minHeight = 52;
        plantillaH.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 52;
        var objetivos = Texto(der, "TextoObjetivos", "", 22, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Encoger(objetivos, 15f);
        // Versión 4 (meses): «Resolver mes» es la acción principal; terminar la misión pasa a secundaria.
        var resolver = Boton(der, "BotonResolver", "Resolve month", true);
        resolver.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;
        var terminar = BotonAccion(der, "BotonTerminar", "End mission");
        terminar.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        Aparecer(vista, izq, der);

        // Resumen del mes: qué decidí, qué ocurrió, qué gasté y qué sigue. Va antes que la decisión del evento.
        var resumen = Panel(vista, "PanelResumen", new Color(0, 0, 0, 0.7f));
        Estirar(resumen);
        var tarjetaResumen = Columna(resumen, "Tarjeta", 16, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(tarjetaResumen, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f));
        MarcoHud(tarjetaResumen);
        Encabezado(tarjetaResumen, "Etiqueta", SpaceMission.Iconos.Informe + "  MONTH SUMMARY");
        var textoResumen = Texto(tarjetaResumen, "TextoResumen", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        textoResumen.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        textoResumen.enableAutoSizing = true; textoResumen.fontSizeMin = 18; textoResumen.fontSizeMax = 26;
        textoResumen.overflowMode = TextOverflowModes.Truncate;
        var continuar = Boton(tarjetaResumen, "BotonContinuarResumen", "Continue", true);
        AjustarTexto(continuar);
        continuar.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = -1;

        // Gameplay v3: ventana de decisión encima de todo cuando ocurre un evento.
        var decision = Panel(vista, "PanelDecision", new Color(0, 0, 0, 0.7f));
        Estirar(decision);
        var tarjetaDecision = Columna(decision, "Tarjeta", 16, TextAnchor.UpperLeft, Tarjeta, 40);
        Anclar(tarjetaDecision, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.78f));
        MarcoHud(tarjetaDecision);
        Encabezado(tarjetaDecision, "Etiqueta", SpaceMission.Iconos.Amarillo + "  SPACECRAFT EVENT");
        var textoDecision = Texto(tarjetaDecision, "TextoDecision", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        Encoger(textoDecision, 18f);
        textoDecision.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var opciones = Columna(tarjetaDecision, "ListaRespuestas", 8, TextAnchor.UpperLeft);
        var plantillaOpcion = Boton(opciones, "PlantillaRespuesta", "Response", false);
        Opcion(plantillaOpcion, 24);
        Encoger(plantillaOpcion.GetComponentInChildren<TextMeshProUGUI>(), 16f);

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
        Asignar(ui, "textoCombustible", combustible);
        Asignar(ui, "botonAltitud", altitud);
        Asignar(ui, "textoMemoria", memoria);
        Asignar(ui, "botonEnviar", enviar);
        Asignar(ui, "botonResolver", resolver);
        Asignar(ui, "textoModificadores", modificadores);
        Asignar(ui, "textoGuia", paso);
        Asignar(ui, "panelResumen", resumen.gameObject);
        Asignar(ui, "textoResumen", textoResumen);
        Asignar(ui, "botonContinuar", continuar);
        Asignar(ui, "textoObjetivos", objetivos);
        Asignar(ui, "panelDecision", decision.gameObject);
        Asignar(ui, "textoDecision", textoDecision);
        Asignar(ui, "listaOpciones", opciones);
        Asignar(ui, "plantillaOpcion", plantillaOpcion);

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

    // ---------- Resultado ----------

    static void ConstruirResultado()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(RutaCatalogo);

        // Escena 3D detrás del informe: Marte con su tormenta, la nave elegida en órbita y la Tierra a lo lejos.
        PrepararCamara();
        Camera.main.backgroundColor = Hex("#05070D");
        ConstruirEstrellas();
        ConstruirMarte(new Vector3(4.6f, -2.6f, 12f), 13f);
        var polvo = GameObject.Find("TormentaPolvo");
        var naveFinal = ConstruirNave(null, false);
        naveFinal.name = "NaveFinal";
        naveFinal.transform.position = new Vector3(0.6f, 1.15f, 2.6f);
        naveFinal.transform.localScale = Vector3.one * 0.3f;
        foreach (var r in naveFinal.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        var matTierraFinal = CrearMaterial("Tierra", Color.white, 0f, 0.35f, ConstructorEstilo.Imagen("Texturas/Tierra_Superficie"), Vector2.one);
        var tierraFinal = Malla(EsferaUV(), "Tierra", null, new Vector3(-11f, 6f, 48f), new Vector3(18f, 0f, 0f), Vector3.one * 2.4f, matTierraFinal);
        var matBrilloFinal = MaterialAditivo("Brillo_Sol", null, Color.white);
        var pulsos = new Object[6];
        for (int i = 0; i < pulsos.Length; i++)
        {
            var sr = new GameObject("PulsoDatos" + i, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sr.sprite = ConstructorEstilo.Sprite("Resplandor");
            sr.sharedMaterial = matBrilloFinal;
            sr.transform.localScale = Vector3.one * 0.35f;
            sr.gameObject.SetActive(false);
            pulsos[i] = sr;
        }
        var matBloque = CrearMaterial("Bloque_Memoria", Acento, 0f, 0.5f, null, Vector2.one, Acento * 2f);
        var bloques = new Object[6];
        for (int i = 0; i < bloques.Length; i++)
        {
            var b = Pieza(PrimitiveType.Cube, "BloqueMemoria" + i, null, Vector3.zero, Vector3.zero, Vector3.one * 0.09f, matBloque);
            b.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            b.SetActive(false);
            bloques[i] = b.transform;
        }
        ConstruirIluminacion();

        var canvas = CrearCanvas();
        // Velo sobre la escena 3D (se oscurece al entrar el informe).
        var fondo = Panel(canvas, "Fondo", new Color(Fondo.r, Fondo.g, Fondo.b, 0.74f));
        Estirar(fondo);
        fondo.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var grupoFondo = fondo.gameObject.AddComponent<CanvasGroup>();
        grupoFondo.blocksRaycasts = false;
        var estatica = new GameObject("Estatica", typeof(RectTransform), typeof(UnityEngine.UI.RawImage), typeof(CanvasGroup));
        estatica.transform.SetParent(canvas, false);
        Estirar((RectTransform)estatica.transform);
        var imgEstatica = estatica.GetComponent<UnityEngine.UI.RawImage>();
        imgEstatica.texture = tBarrido;
        imgEstatica.uvRect = new Rect(0, 0, 1920 / 3f, 1080 / 3f);
        imgEstatica.color = new Color(0.8f, 0.85f, 0.9f, 1f);
        imgEstatica.raycastTarget = false;
        var grupoEstatica = estatica.GetComponent<CanvasGroup>();
        grupoEstatica.alpha = 0f;
        grupoEstatica.blocksRaycasts = false;

        // El informe va en dos pantallas para no saturar:
        // 1) VistaVeredicto: título, conclusión, puntos y sello, con «Ver detalles».
        // 2) VistaDetalle: evidencias, lo que faltó, desglose de puntos y recursos, con «Modificar mi misión».
        var vistaVeredicto = Vacio(canvas, "VistaVeredicto");
        Estirar(vistaVeredicto);
        var vistaDetalle = Vacio(canvas, "VistaDetalle");
        Estirar(vistaDetalle);
        vistaDetalle.gameObject.AddComponent<AparecerVista>();

        // Veredicto, a la izquierda: conclusión y botón al detalle. A la derecha se ve Marte.
        var izq = Columna(vistaVeredicto, "Conclusion", 26, TextAnchor.UpperLeft);
        Anclar(izq, new Vector2(0.05f, 0.06f), new Vector2(0.48f, 0.94f));
        Encabezado(izq, "Etiqueta", SpaceMission.Iconos.Informe + "  MISSION REPORT");
        var titulo = Texto(izq, "Titulo", "", 80, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        titulo.fontStyle = FontStyles.UpperCase;
        var conclusion = Texto(izq, "TextoConclusion", "", 34, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        conclusion.lineSpacing = 10;
        conclusion.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var detalle = Boton(izq, "BotonDetalle", "View details", true);
        AjustarTexto(detalle);

        // Detalle, a la izquierda: recursos usados y las acciones.
        var izqDetalle = Columna(vistaDetalle, "Acciones", 26, TextAnchor.UpperLeft);
        Anclar(izqDetalle, new Vector2(0.05f, 0.06f), new Vector2(0.48f, 0.94f));
        Encabezado(izqDetalle, "EtiquetaDetalle", SpaceMission.Iconos.Informe + "  MISSION DETAILS");
        var cajaRecursos = Columna(izqDetalle, "Recursos", 8, TextAnchor.UpperLeft, TarjetaTranslucida, 28);
        Bordes(cajaRecursos, LineaHud);
        var recursos = Texto(cajaRecursos, "TextoRecursos", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        // El cuadro mide lo que su texto (antes se estiraba hasta los botones y quedaba medio vacío).
        Alto(Vacio(izqDetalle, "EspacioDetalle"), 0).GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var modificar = Boton(izqDetalle, "BotonModificar", "Modify my mission", true);
        AjustarTexto(modificar); // texto exacto: lo comprueban las pruebas
        var volver = Boton(izqDetalle, "BotonVeredicto", "Back to the verdict", false);
        AjustarTexto(volver);

        // Detalle, a la derecha: evidencias, revelación, lo que faltó y el desglose de puntos.
        var der = Columna(vistaDetalle, "Detalle", 28, TextAnchor.UpperLeft, TarjetaTranslucida, 40);
        Anclar(der, new Vector2(0.52f, 0.13f), new Vector2(0.95f, 0.94f));
        MarcoHud(der);
        var evidencias = Texto(der, "TextoEvidencias", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        evidencias.lineSpacing = 8;
        var faltantes = Texto(der, "TextoFaltantes", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        faltantes.lineSpacing = 8;
        var puntos = Texto(der, "TextoPuntos", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        puntos.lineSpacing = 8;
        // Con muchas líneas (carga, satélite, objetivos) los textos se achican en vez de encimarse.
        foreach (var t in new[] { conclusion, recursos, evidencias, faltantes, puntos })
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = 18;
            t.fontSizeMax = t.fontSize;
            t.overflowMode = TextOverflowModes.Truncate;
        }
        Alto(Vacio(der, "Espacio"), 0).GetComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        Miniatura(der, "Mision", arte.imagenMision, 150, SpaceMission.Iconos.Senal + "  MARS DURING THE STORM  ·  NASA/JPL-Caltech/MSSS");

        // Arriba a la derecha: total de puntos (cuenta hacia arriba) y el sello del resultado.
        var cabecera = Vacio(vistaVeredicto, "Cabecera");
        Anclar(cabecera, new Vector2(0.52f, 0.76f), new Vector2(0.95f, 0.95f));
        var grupoCabecera = cabecera.gameObject.AddComponent<CanvasGroup>();
        var total = Texto(cabecera, "TotalPuntos", "", 84, Acento, FontStyles.Bold, TextAlignmentOptions.Left);
        Anclar(total.rectTransform, Vector2.zero, new Vector2(0.62f, 1f));
        total.textWrappingMode = TextWrappingModes.NoWrap;
        var sello = Vacio(vistaVeredicto, "Sello");
        sello.anchorMin = sello.anchorMax = new Vector2(0.86f, 0.855f);
        sello.sizeDelta = new Vector2(250, 250);
        sello.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        var aro = new GameObject("Aro", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        aro.transform.SetParent(sello, false);
        aro.sprite = ConstructorEstilo.Sprite("Anillo");
        aro.raycastTarget = false;
        Estirar(aro.rectTransform);
        var textoSello = Texto(sello, "TextoSello", "", 34, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        textoSello.characterSpacing = 4;
        textoSello.lineSpacing = -10;
        textoSello.enableAutoSizing = true; textoSello.fontSizeMin = 20; textoSello.fontSizeMax = 36;
        Anclar(textoSello.rectTransform, new Vector2(0.14f, 0.2f), new Vector2(0.86f, 0.8f));
        sello.localRotation = Quaternion.Euler(0, 0, -8f);

        // Abajo a la derecha: agradecimiento al patrocinador.
        var franja = FranjaPatrocinio(canvas, "Patrocinio", "THANKS TO OUR SPONSOR", 72);
        Anclar(franja, new Vector2(0.52f, 0.015f), new Vector2(0.95f, 0.115f));

        // Clic en cualquier parte durante la entrada: la salta (apagado fuera de la entrada).
        var saltarEntrada = Panel(canvas, "SaltarEntrada", new Color(0, 0, 0, 0f));
        Estirar(saltarEntrada);
        saltarEntrada.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var botonSaltar = saltarEntrada.gameObject.AddComponent<UnityEngine.UI.Button>();
        botonSaltar.transition = UnityEngine.UI.Selectable.Transition.None;
        saltarEntrada.gameObject.SetActive(false);

        CrearEventSystem();
        var musica = CrearMusica(ArteDatos.Musica.Ninguna);
        Asignar(musica, "segunResultado", true);

        var final = new GameObject("FinalAnimado", typeof(AudioSource));
        final.GetComponent<AudioSource>().playOnAwake = false;
        var animado = final.AddComponent<FinalAnimado>();
        Asignar(animado, "catalogo", catalogo);
        Asignar(animado, "camara", Camera.main);
        Asignar(animado, "nave", naveFinal);
        Asignar(animado, "tierra", tierraFinal.transform);
        if (polvo != null) Asignar(animado, "polvo", polvo.GetComponent<Renderer>());
        AsignarLista(animado, "pulsos", pulsos);
        AsignarLista(animado, "bloquesMemoria", bloques);
        var grupoIzq = izq.gameObject.AddComponent<CanvasGroup>();
        AsignarLista(animado, "paneles", new Object[] { grupoIzq, grupoCabecera, franja.GetComponent<CanvasGroup>() });
        Asignar(animado, "veloInforme", grupoFondo);
        Asignar(animado, "sello", sello);
        Asignar(animado, "textoSello", textoSello);
        Asignar(animado, "aroSello", aro);
        Asignar(animado, "totalPuntos", total);
        Asignar(animado, "estatica", grupoEstatica);
        Asignar(animado, "saltar", botonSaltar);
        Asignar(animado, "fuente", final.GetComponent<AudioSource>());

        var ui = new GameObject("ResultadoUI").AddComponent<ResultadoUI>();
        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "textoTitulo", titulo);
        Asignar(ui, "textoConclusion", conclusion);
        Asignar(ui, "textoEvidencias", evidencias);
        Asignar(ui, "textoFaltantes", faltantes);
        Asignar(ui, "textoRecursos", recursos);
        Asignar(ui, "textoPuntos", puntos);
        Asignar(ui, "botonModificar", modificar);
        Asignar(ui, "botonDetalle", detalle);
        Asignar(ui, "botonVeredicto", volver);
        Asignar(ui, "vistaVeredicto", vistaVeredicto.gameObject);
        Asignar(ui, "vistaDetalle", vistaDetalle.gameObject);
        vistaDetalle.gameObject.SetActive(false);

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
        Texto(izq, "Titulo", "Set up your mission", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(izq);
        // Cuatro pestañas en dos filas, en el orden de los pasos (versión 6): Lanzador, Satélite / Instrumentos, Órbita.
        var pestanas = Columna(izq, "Pestanas", 8, TextAnchor.UpperLeft);
        Alto(pestanas, 2 * 60 + 8);
        var fila1 = Fila(pestanas, "Fila1", 10);
        Alto(fila1, 60);
        var fila2 = Fila(pestanas, "Fila2", 10);
        Alto(fila2, 60);
        // GuiaConfiguracion añade a cada pestaña el ✓ cuando esa parte está elegida.
        var pLanzador = Boton(fila1, "PestanaLanzador", SpaceMission.Iconos.Lanzador + " Launcher", false);
        var pSatelite = Boton(fila1, "PestanaSatelite", SpaceMission.Iconos.Satelite + " Satellite", false);
        var pInstrumentos = Boton(fila2, "PestanaInstrumentos", SpaceMission.Iconos.Microscopio + " Instruments", false);
        var pOrbita = Boton(fila2, "PestanaOrbita", SpaceMission.Iconos.Orbita + " Orbit", false);
        foreach (var p in new[] { pOrbita, pLanzador, pSatelite, pInstrumentos })
        {
            var le = p.GetComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = -1; le.flexibleWidth = 1; le.minHeight = 60; le.preferredHeight = 60;
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
        // Hasta 6 opciones (los satélites, con el AztechSat-1).
        Alto(lista, 6 * 62 + 5 * 6);   // opciones de 62: deja sitio a la ayuda de abajo
        lista.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 6;
        var plantilla = Boton(lista, "PlantillaOpcion", "Option", false);
        var lePlantilla = plantilla.GetComponent<UnityEngine.UI.LayoutElement>();
        lePlantilla.preferredWidth = -1; lePlantilla.minHeight = 62; lePlantilla.preferredHeight = 62;
        var etiqueta = plantilla.GetComponentInChildren<TextMeshProUGUI>();
        etiqueta.font = ConstructorEstilo.Texto;
        etiqueta.fontSize = 26;
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
        // La ayuda ocupa lo que queda de la caja y el texto se achica para caber (nunca se sale de la caja).
        var leTextoAyuda = ayuda.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        leTextoAyuda.preferredHeight = 1; leTextoAyuda.flexibleHeight = 1;
        ayuda.enableAutoSizing = true; ayuda.fontSizeMin = 15; ayuda.fontSizeMax = 24;
        ayuda.overflowMode = TextOverflowModes.Truncate;
        // Versión 6: botón pequeño «+ Dato NASA» abajo a la derecha de la caja; el dato real solo se ve si se pulsa.
        cajaAyuda.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().padding.bottom = 64;
        var botonNasa = Boton(cajaAyuda, "BotonNasa", "+ NASA data", false);
        botonNasa.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var rtNasa = botonNasa.GetComponent<RectTransform>();
        rtNasa.anchorMin = rtNasa.anchorMax = rtNasa.pivot = new Vector2(1f, 0f);
        rtNasa.anchoredPosition = new Vector2(-14f, 12f);
        rtNasa.sizeDelta = new Vector2(230f, 44f);
        botonNasa.GetComponent<UnityEngine.UI.Image>().color = new Color(Cian.r, Cian.g, Cian.b, 0.18f);
        var textoNasa = botonNasa.GetComponentInChildren<TextMeshProUGUI>();
        textoNasa.fontSize = 22; textoNasa.color = Cian; textoNasa.characterSpacing = 1;
        botonNasa.gameObject.SetActive(false);

        var filaNavegacion = Fila(izq, "Navegacion", 12);
        Alto(filaNavegacion, 64);
        var verBriefing = Boton(filaNavegacion, "BotonBriefing", SpaceMission.Iconos.Atras + "  Briefing", false);
        var siguiente = Boton(filaNavegacion, "BotonSiguiente", "Satellite  " + SpaceMission.Iconos.Siguiente, true);
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
        Encabezado(der, "EtiquetaRecursos", "RESOURCES");

        // Versión 6: quinta barra, el frenado (el que pide la órbita sobre el que tiene la nave).
        var rellenos = new RectTransform[5];
        var textos = new TMP_Text[5];
        string[] nombres = { "Masa", "Potencia", "Costo", "Espacios", "Alcance" };   // nombres de los objetos: no cambiar
        string[] etiquetasRecursos = { "Mass", "Power", "Cost", "Slots", "Braking" };
        string[] iconos = { SpaceMission.Iconos.Masa, SpaceMission.Iconos.Potencia, SpaceMission.Iconos.Costo, SpaceMission.Iconos.Espacios, SpaceMission.Iconos.Lanzador };
        for (int i = 0; i < nombres.Length; i++)
        {
            var fila = Fila(der, "Fila" + nombres[i], 12);
            Alto(fila, 40);
            var grupoFila = fila.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            grupoFila.childForceExpandWidth = false;
            var icono = Texto(fila, "Icono" + nombres[i], iconos[i], 30, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
            icono.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 40;
            textos[i] = Texto(fila, "Texto" + nombres[i], etiquetasRecursos[i], 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.Left);
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
        Encabezado(der, "EtiquetaSemaforo", "STATUS");
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
        var estado = Texto(filaLuz, "TextoEstado", "GREY", 48, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        estado.characterSpacing = 4;
        var mensaje = Texto(der, "TextoMensaje", "", 26, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        mensaje.lineSpacing = 6;
        mensaje.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        // Con el alcance (versión 6) el mensaje rojo puede ser largo: se achica en vez de salirse.
        mensaje.enableAutoSizing = true; mensaje.fontSizeMin = 16; mensaje.fontSizeMax = 26;
        mensaje.overflowMode = TextOverflowModes.Truncate;
        var revisar = Boton(der, "BotonRevisar", "Review mission  " + SpaceMission.Iconos.Siguiente, true);
        AjustarTexto(revisar);
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
        var leyenda = Texto(vista, "LeyendaNave", "SPACECRAFT VIEW\n<size=24><color=#" + ColorUtility.ToHtmlStringRGB(TextoSecundario) +
            ">Select an instrument to see where it is mounted.</color></size>",
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
        Asignar(guia, "textoPestanaSatelite", pSatelite.GetComponentInChildren<TextMeshProUGUI>());
        Asignar(guia, "botonSiguiente", siguiente);
        Asignar(guia, "textoInstruccion", instruccion);
        Asignar(guia, "textoLeyenda", leyenda);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "nave", nave);
        Asignar(ui, "pestanaOrbita", pOrbita);
        Asignar(ui, "pestanaLanzador", pLanzador);
        Asignar(ui, "pestanaInstrumentos", pInstrumentos);
        Asignar(ui, "pestanaSatelite", pSatelite);
        Asignar(ui, "listaOpciones", lista);
        Asignar(ui, "plantillaOpcion", plantilla);
        Asignar(ui, "textoAyuda", ayuda);
        Asignar(ui, "botonNasa", botonNasa);
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
        Anclar(tarjeta, new Vector2(0.25f, 0.045f), new Vector2(0.75f, 0.955f));
        tarjeta.gameObject.AddComponent<CanvasGroup>();
        MarcoHud(tarjeta);
        var paso = Texto(tarjeta, "Paso", Paso(3), 24, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Left);
        paso.font = ConstructorEstilo.Mono;
        Texto(tarjeta, "Titulo", "Mission review", 52, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Left);
        Linea(tarjeta);
        // Versión 7: el resumen va en una zona con scroll (rueda del ratón o arrastre) y su barra a la derecha;
        // con el satélite y el frenado ya no cabía y se cortaba detrás de los botones.
        var zona = Vacio(tarjeta, "ZonaResumen");
        zona.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleHeight = 1;
        var fondoZona = zona.gameObject.AddComponent<UnityEngine.UI.Image>();
        fondoZona.color = new Color(0, 0, 0, 0);   // transparente, pero recibe la rueda y el arrastre
        zona.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        var resumen = Texto(zona, "TextoResumen", "", 28, TextoClaro, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        resumen.lineSpacing = 4;
        var rtResumen = resumen.rectTransform;
        rtResumen.anchorMin = new Vector2(0, 1); rtResumen.anchorMax = new Vector2(1, 1); rtResumen.pivot = new Vector2(0, 1);
        rtResumen.offsetMin = new Vector2(0, 0); rtResumen.offsetMax = new Vector2(-22, 0);
        var ajusteResumen = resumen.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        ajusteResumen.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var barra = Panel(zona, "BarraScroll", FondoBarra);
        barra.anchorMin = new Vector2(1, 0); barra.anchorMax = new Vector2(1, 1); barra.pivot = new Vector2(1, 0.5f);
        barra.offsetMin = new Vector2(-10, 0); barra.offsetMax = new Vector2(0, 0);
        var asa = Panel(barra, "Asa", Cian);
        asa.offsetMin = asa.offsetMax = Vector2.zero;
        var scrollbar = barra.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
        scrollbar.handleRect = asa;
        scrollbar.targetGraphic = asa.GetComponent<UnityEngine.UI.Image>();
        scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
        var scroll = zona.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.content = rtResumen;
        scroll.viewport = zona;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
        var botones = Fila(tarjeta, "Botones", 24);
        Alto(botones, 88);
        var corregir = Boton(botones, "BotonCorregir", SpaceMission.Iconos.Atras + "  Fix", false);
        AjustarTexto(corregir);
        var lanzar = Boton(botones, "BotonLanzar", "Load the spacecraft  " + SpaceMission.Iconos.Siguiente, true);
        AjustarTexto(lanzar);
        Aparecer(vista, tarjeta);

        Asignar(ui, "catalogo", catalogo);
        Asignar(ui, "mision", mision);
        Asignar(ui, "textoResumen", resumen);
        Asignar(ui, "scroll", scroll);
        Asignar(ui, "botonCorregir", corregir);
        Asignar(ui, "botonLanzar", lanzar);
        return ui;
    }

    // ---------- Escena 3D ----------

    // La nave (satélites de NASA y módulos de instrumento) está en ConstructorEscenas.Nasa.cs.

    // ---------- Cohetes ----------

    /// <summary>
    /// Plataforma con los dos lanzadores, a escala aproximada entre ellos (el SLS mide el doble que el Soyuz):
    /// - Explorador, inspirado en el Soyuz: etapa central gris claro, cuatro propulsores cónicos con faldón,
    ///   banda naranja, etapa superior y cofia ojival.
    /// - Titán: el modelo 3D del SLS de NASA 3D Resources (pintado por zonas: etapa central naranja, propulsores
    ///   blancos, toberas oscuras), separado en etapa inferior y superior.
    /// Debajo de cada uno, un anillo de acento que se enciende cuando ese lanzador está elegido.
    /// </summary>
    static (GameObject explorador, GameObject titan, GameObject resaltadoExplorador, GameObject resaltadoTitan) ConstruirCohetes(Vector3 plataforma)
    {
        var blanco = CrearMaterial("Cohete_Blanco", Hex("#ECEEF0"), 0.05f, 0.55f);
        var grisVerde = CrearMaterial("Cohete_GrisSoyuz", Hex("#C9CFC6"), 0.3f, 0.5f, ConstructorEstilo.Imagen("Texturas/Metal_Cepillado"), new Vector2(4f, 8f));
        var oscuro = CrearMaterial("Nave_Oscuro", Hex("#2B2F36"), 0.7f, 0.5f);
        var bandaNaranja = CrearMaterial("Cohete_BandaNaranja", Hex("#E2643A"), 0.05f, 0.5f);
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
            // Cada propulsor con su aleta y su tobera en un bloque (en la cinemática se abren en cruz).
            var bloque = new GameObject("Bloque" + i).transform;
            bloque.SetParent(soyuz, false);
            bloque.localPosition = dir * 0.31f;
            var propulsor = Malla(mPropulsor, "Propulsor" + i, bloque, Vector3.zero, new Vector3(0, a, 0), Vector3.one, grisVerde);
            propulsor.transform.localRotation = Quaternion.AngleAxis(-3f, Vector3.Cross(Vector3.up, dir)) * propulsor.transform.localRotation;
            Pieza(PrimitiveType.Cube, "Aleta" + i, bloque, dir * 0.29f + Vector3.up * 0.45f, new Vector3(0, a, 0), new Vector3(0.02f, 0.28f, 0.18f), oscuro);
            Malla(mTobera, "Tobera" + i, bloque, Vector3.zero, Vector3.zero, Vector3.one, oscuro);
            Pieza(PrimitiveType.Cylinder, "Llama" + i, bloque, Vector3.zero, Vector3.zero, new Vector3(0.12f, 0.005f, 0.12f), llama);
        }
        Malla(mTobera, "ToberaCentral", soyuz, Vector3.up * 0.13f, Vector3.zero, Vector3.one * 1.1f, oscuro);
        // Etapa superior y cofia juntas: en la cinemática se separan de la etapa inferior.
        var superiorSoyuz = new GameObject("EtapaSuperior").transform;
        superiorSoyuz.SetParent(soyuz, false);
        Pieza(PrimitiveType.Cylinder, "Celosia", superiorSoyuz, Vector3.up * 3.6f, Vector3.zero, new Vector3(0.38f, 0.15f, 0.38f), oscuro);
        Pieza(PrimitiveType.Cylinder, "BandaNaranja", superiorSoyuz, Vector3.up * 3.82f, Vector3.zero, new Vector3(0.41f, 0.07f, 0.41f), bandaNaranja);
        Pieza(PrimitiveType.Cylinder, "Etapa", superiorSoyuz, Vector3.up * 4.25f, Vector3.zero, new Vector3(0.4f, 0.36f, 0.4f), blanco);
        // Cofia en dos mitades (se abren en órbita para liberar el satélite). Cada mitad gira sobre una bisagra
        // en su borde inferior; material de doble cara para que se vea por dentro al abrirse.
        var perfilCofia = new[] { P(0.2f, 0f), P(0.26f, 0.12f), P(0.26f, 0.85f), P(0.22f, 1.1f), P(0.12f, 1.35f), P(0f, 1.5f) };
        var blancoDoble = DobleCara(blanco, "Cohete_Blanco_DobleCara");
        foreach (var (nombre, desde, lado) in new[] { ("CofiaDetras", 0f, 1f), ("CofiaFrente", Mathf.PI, -1f) })
        {
            var malla = GuardarMalla(Torno(perfilCofia, 20, desde, desde + Mathf.PI), "Soyuz_" + nombre);
            var bisagra = new GameObject(nombre).transform;
            bisagra.SetParent(superiorSoyuz, false);
            bisagra.localPosition = new Vector3(0f, 4.6f, lado * 0.2f);
            Malla(malla, "Mitad", bisagra, new Vector3(0f, 0f, -lado * 0.2f), Vector3.zero, Vector3.one, blancoDoble);
        }
        // Logo del patrocinador en la cofia blanca, por delante y por detrás (calcomanía curva).
        LogoPatrocinio(superiorSoyuz, "Fence", false, 0.266f, 5.08f, 0.52f);
        // Cada logo va con su mitad de la cofia.
        superiorSoyuz.Find("LogoFrente").SetParent(superiorSoyuz.Find("CofiaFrente"), true);
        superiorSoyuz.Find("LogoDetras").SetParent(superiorSoyuz.Find("CofiaDetras"), true);
        soyuz.gameObject.AddComponent<GiroLento>().enabled = false;

        // ----- Titán (SLS, modelo de NASA 3D Resources) -----
        var sls = new GameObject("Cohete_titan").transform;
        sls.SetParent(raiz, false);
        sls.localPosition = new Vector3(2.1f, 0, 0);
        var slsModelo = ConstruirSls(sls, 11f);
        DividirSls(slsModelo.inferior);   // propulsores laterales aparte (se sueltan en la cinemática)
        DividirNarizSls(slsModelo.superior);   // la nariz en dos mitades (se abren para liberar el satélite)
        // Logo del patrocinador en el núcleo naranja (versión clara: letras blancas), por delante y por detrás.
        // El radio del núcleo se mide en la malla a esa altura (entre los dos propulsores laterales).
        float alturaLogo = 5.6f;
        LogoPatrocinio(slsModelo.inferior != null ? slsModelo.inferior.transform : sls, "Security", true,
            RadioNucleo(sls, alturaLogo) + 0.012f, alturaLogo, 1.35f, sls);
        foreach (int lado in new[] { -1, 1 })   // brillo de los motores al pie (el Bloom lo hace resplandecer)
            Pieza(PrimitiveType.Cylinder, "Llama", sls, new Vector3(lado * 0.7f, 0.02f, 0), Vector3.zero, new Vector3(0.3f, 0.005f, 0.3f), llama);
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

    /// <summary>
    /// Calcomanía curva con el logo del patrocinador alrededor de un cuerpo cilíndrico (eje Y), centrada en −Z
    /// (la cara que mira a la cámara) y otra igual en +Z. «alto» es el lado del logo (es cuadrado). Material URP
    /// Lit con recorte por alfa: recibe la luz como el resto del cohete. Las medidas van en el espacio de
    /// «referencia» (por defecto, el propio padre).
    /// </summary>
    static void LogoPatrocinio(Transform padre, string cual, bool claro, float radio, float altura, float alto, Transform referencia = null)
    {
        var sprite = ConstructorEstilo.Patrocinio(cual, claro);
        if (sprite == null) return;
        var mat = CrearMaterial("Patrocinio_" + cual + (claro ? "_Claro" : ""), Color.white, 0.1f, 0.55f, sprite.texture, Vector2.one);
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.45f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(mat);
        var malla = GuardarMalla(ParcheCilindro(radio, alto), "Logo_" + cual + "_" + Mathf.RoundToInt(radio * 1000));
        referencia = referencia != null ? referencia : padre;
        foreach (var (nombre, giro) in new[] { ("LogoFrente", 0f), ("LogoDetras", 180f) })
        {
            var go = Malla(malla, nombre, padre, Vector3.zero, Vector3.zero, Vector3.one, mat);
            // Posición y giro en el espacio de la referencia (el cohete), aunque el padre sea una pieza escalada.
            go.transform.position = referencia.TransformPoint(new Vector3(0f, altura, 0f));
            go.transform.rotation = referencia.rotation * Quaternion.Euler(0f, giro, 0f);
            go.transform.localScale = Vector3.one;
            var l = go.transform.lossyScale;
            var r = referencia.lossyScale;
            go.transform.localScale = new Vector3(r.x / l.x, r.y / l.y, r.z / l.z);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    /// <summary>Sector de cilindro (radio, lado) centrado en −Z, con UV 0–1: la calcomanía del logo.</summary>
    static Mesh ParcheCilindro(float radio, float lado, int segmentos = 24)
    {
        float arco = lado / radio;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= segmentos; i++)
        {
            float u = i / (float)segmentos;
            float a = (u - 0.5f) * arco;
            var dir = new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a));
            v.Add(dir * radio + Vector3.down * lado * 0.5f); n.Add(dir); uv.Add(new Vector2(u, 0f));
            v.Add(dir * radio + Vector3.up * lado * 0.5f); n.Add(dir); uv.Add(new Vector2(u, 1f));
        }
        for (int i = 0; i < segmentos; i++)
        {
            int b = i * 2;
            t.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 });
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateTangents();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Radio del núcleo del cohete a una altura dada (vértices cerca del eje X = 0), en su espacio local.</summary>
    static float RadioNucleo(Transform cohete, float altura)
    {
        float radio = 0f;
        foreach (var mf in cohete.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            foreach (var vert in mf.sharedMesh.vertices)
            {
                var p = cohete.InverseTransformPoint(mf.transform.TransformPoint(vert));
                if (Mathf.Abs(p.y - altura) < 0.35f && Mathf.Abs(p.x) < 0.12f) radio = Mathf.Max(radio, Mathf.Abs(p.z));
            }
        }
        return radio > 0.05f ? radio : 0.47f;
    }

    /// <summary>Malla de revolución: gira un perfil (radio, alto) de abajo arriba alrededor del eje Y.</summary>
    static Mesh Torno(Vector2[] perfil, int segmentos = 40, float desde = 0f, float hasta = Mathf.PI * 2f)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int fila = segmentos + 1;
        for (int i = 0; i < perfil.Length; i++)
            for (int s = 0; s <= segmentos; s++)
            {
                float a = desde + s * (hasta - desde) / segmentos;   // por defecto la vuelta entera
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

    /// <summary>
    /// Material de Marte con el shader Planeta: mapa 8K en dos mitades, relieve, detalle de cerca, terminador
    /// suave y borde de polvo. Lo comparten el Menú, Configuración, la cinemática y la investigación.
    /// </summary>
    static Material MaterialPlaneta()
    {
        const string ruta = "Assets/Materiales/Marte.mat";
        var shader = Shader.Find("OperacionEclipseRojo/Planeta");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        mat.shaderKeywords = new string[0];
        mat.SetTexture("_MapaOeste", ConstructorEstilo.Imagen("Texturas/Marte_Superficie_Oeste"));
        mat.SetTexture("_MapaEste", ConstructorEstilo.Imagen("Texturas/Marte_Superficie_Este"));
        mat.SetTexture("_Normales", ConstructorEstilo.Imagen("Texturas/Marte_Normal"));
        mat.SetFloat("_FuerzaNormales", 1.8f);   // relieve marcado: cráteres, volcanes y cañones
        mat.SetTexture("_Detalle", ConstructorEstilo.Imagen("Texturas/Marte_Detalle"));
        mat.SetFloat("_RepeticionDetalle", 48f);
        mat.SetFloat("_FuerzaDetalle", 0.45f);
        mat.SetFloat("_Suavidad", 0.08f);
        mat.SetColor("_ColorBorde", new Color(1f, 0.45f, 0.22f) * 0.6f);
        mat.SetFloat("_PotenciaBorde", 5f);
        mat.SetFloat("_Brillo", 1.05f);
        mat.SetColor("_Ambiente", new Color(0.05f, 0.055f, 0.075f));
        mat.SetFloat("_Saturacion", 0.78f);   // óxido y caramelo, no naranja chillón
        mat.SetFloat("_Contraste", 1.35f);
        mat.SetFloat("_Gamma", 1.22f);
        // Relieve del cráter Gale (NASA 3D Resources, Curiosity Landing Site) en su sitio del planeta.
        mat.SetTexture("_ParcheNormal", ConstructorEstilo.Imagen("Texturas/Gale_Normal"));
        mat.SetVector("_RectParche", new Vector4(0.8728f, 0.452f, 0.0205f, 0.041f));
        mat.SetFloat("_FuerzaParche", 1.6f);
        // Versión 6: nubes de hielo de agua (GeneradorCapasMarte), escarcha en los casquetes y crepúsculo azulado.
        mat.SetTexture("_Nubes", ConstructorEstilo.Imagen("Texturas/Marte_Nubes"));
        mat.SetFloat("_FuerzaNubes", 0.18f);
        mat.SetFloat("_VelocidadNubes", 0.0015f);
        mat.SetColor("_ColorNubes", new Color(0.93f, 0.95f, 1f));
        mat.SetFloat("_Escarcha", 0.55f);
        mat.SetFloat("_BrilloHielo", 0.6f);
        mat.SetColor("_ColorCrepusculo", new Color(0.25f, 0.45f, 0.9f));
        mat.SetFloat("_FuerzaCrepusculo", 0.22f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Marte con la textura real (MARCI, tormenta global de 2018) y un borde de atmósfera.</summary>
    static GameObject ConstruirMarte(Vector3 posicion, float diametro)
    {
        var mat = MaterialPlaneta();
        var marte = Malla(EsferaUV(), "Marte", null, posicion, new Vector3(0, 0, -12), Vector3.one * diametro, mat);

        // Capa de polvo: la tormenta global de 2018 (MARCI) semitransparente, girando un poco más rápido.
        var matPolvo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Marte_Polvo.mat");
        if (matPolvo == null)
        {
            matPolvo = new Material(Shader.Find("OperacionEclipseRojo/CapaPolvo"));
            AssetDatabase.CreateAsset(matPolvo, "Assets/Materiales/Marte_Polvo.mat");
        }
        matPolvo.SetTexture("_BaseMap", ConstructorEstilo.Imagen("Texturas/Marte_Polvo"));
        matPolvo.SetFloat("_Opacidad", 0.07f);   // apenas un velo: el relieve se ve limpio
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
        matAtmosfera.SetColor("_Color", new Color(1f, 0.48f, 0.26f) * 0.9f);
        matAtmosfera.SetFloat("_Potencia", 4.5f);
        matAtmosfera.SetFloat("_Intensidad", 1f);
        EditorUtility.SetDirty(matAtmosfera);
        // Misma esfera fina que el planeta: con la primitiva de Unity el borde se ve facetado de cerca.
        var atmosfera = Malla(EsferaUV(), "Atmosfera", null, posicion, Vector3.zero, Vector3.one * diametro * 1.016f, matAtmosfera);   // halo fino, no una banda pálida
        atmosfera.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        marte.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return marte;
    }

    /// <summary>
    /// Órbita de Configuración: solo la elegida (línea del propio objeto, con halo, estela y marcador), con su
    /// color (Trayectoria.Color). Versión 7: una sola trayectoria visible.
    /// </summary>
    static AnilloOrbita ConstruirAnilloOrbita(Transform marte, CatalogoDatos catalogo)
    {
        // No es hijo del planeta: así no gira con él. Mismo centro y escala (radio 0.5 = superficie).
        var go = new GameObject("AnilloOrbita", typeof(LineRenderer));
        go.transform.position = marte.position;
        go.transform.localScale = marte.localScale;
        float escala = marte.localScale.x;
        var linea = go.GetComponent<LineRenderer>();
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, "Assets/Materiales/Linea_Orbita.mat");
        }
        linea.sharedMaterial = mat;
        linea.widthMultiplier = 0.16f;
        linea.numCapVertices = 2;
        linea.startColor = linea.endColor = new Color(Cian.r, Cian.g, Cian.b, 0f);
        linea.shadowCastingMode = ShadowCastingMode.Off;
        linea.enabled = false;
        var anillo = go.AddComponent<AnilloOrbita>();
        Asignar(anillo, "radio", 0.66f);

        var matBrillo = MaterialAditivo("Brillo_Orbita", null, Color.white);
        var brillo = LineaHija(go.transform, "Brillo", matBrillo, 0.7f, Color.clear, Color.clear);
        brillo.loop = true;
        brillo.enabled = false;
        var estela = LineaHija(go.transform, "Estela", matBrillo, 0.42f, Color.clear, Color.clear);
        estela.widthCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 1f));
        Asignar(anillo, "brillo", brillo);
        Asignar(anillo, "estela", estela);

        // Versión 7: solo se dibuja la órbita elegida (la línea del propio objeto); ya no hay líneas ni etiquetas
        // de las otras. Quedan los dos rótulos de los extremos, para la órbita excéntrica.
        var cerca = TextoMundo(go.transform, "Periapsis", "· closest ·", 0.8f / escala, Trayectoria.Color("eliptica"));
        var lejos = TextoMundo(go.transform, "Apoapsis", "· farthest ·", 0.8f / escala, Trayectoria.Color("eliptica"));
        cerca.gameObject.SetActive(false);
        lejos.gameObject.SetActive(false);
        AsignarLista(anillo, "extremos", new Object[] { cerca.transform, lejos.transform });

        // Marcador de la nave en órbita: una esfera encendida (el Bloom la hace brillar) con un halo aditivo.
        var matMarcador = CrearMaterial("Marcador_Orbita", Cian, 0f, 0.5f, null, Vector2.one, Cian * 3f);
        var marcador = Pieza(PrimitiveType.Sphere, "MarcadorNave", go.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.03f, matMarcador);
        marcador.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var halo = new GameObject("Halo", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        halo.transform.SetParent(marcador.transform, false);
        halo.transform.localScale = Vector3.one * 0.55f;
        halo.sprite = ConstructorEstilo.Sprite("Resplandor");
        halo.sharedMaterial = MaterialAditivo("Brillo_Sol", null, Color.white);
        marcador.SetActive(false);
        Asignar(anillo, "marcador", marcador.transform);
        Asignar(anillo, "haloMarcador", halo);
        return anillo;
    }

    /// <summary>Texto 3D (TextMeshPro) para etiquetas en la escena; «tamano» es la altura de línea en unidades locales.</summary>
    static TextMeshPro TextoMundo(Transform padre, string nombre, string texto, float tamano, Color color)
    {
        var go = new GameObject(nombre, typeof(TextMeshPro));
        go.transform.SetParent(padre, false);
        var t = go.GetComponent<TextMeshPro>();
        t.font = ConstructorEstilo.Etiquetas;
        t.text = texto;
        t.fontSize = 10f;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.fontStyle = FontStyles.UpperCase;
        t.characterSpacing = 4;
        t.rectTransform.sizeDelta = new Vector2(40, 5);
        go.transform.localScale = Vector3.one * tamano;   // fontSize 10 ≈ 1 unidad de alto con escala 1
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        return t;
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
        mat.SetFloat("_Cull", 0f);   // también la esfera de estrellas vista desde dentro
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
        sol.transform.rotation = Quaternion.Euler(20, 58, 0); // más de lado: el terminador da volumen y se ve el relieve
        RenderSettings.sun = sol;

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

    static MusicaEscena CrearMusica(ArteDatos.Musica musica, ArteDatos.Ambiente ambiente = ArteDatos.Ambiente.Tormenta)
    {
        var go = new GameObject("Audio", typeof(AudioSource));
        var reproductor = go.AddComponent<ReproductorUI>();
        Asignar(reproductor, "arte", arte);
        var m = go.AddComponent<MusicaEscena>();
        Asignar(m, "arte", arte);
        var so = new SerializedObject(m);
        so.FindProperty("musica").enumValueIndex = (int)musica;
        so.FindProperty("ambiente").enumValueIndex = (int)ambiente;
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
        // Eclipse Ink: el contorno necesita la normal suavizada, que las primitivas de Unity no traen.
        if (LlevaContorno(material))
        {
            var filtro = go.GetComponent<MeshFilter>();
            filtro.sharedMesh = PrimitivaTinta(tipo, filtro.sharedMesh);
        }
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

    /// <summary>Pone (o quita, con null) el mapa de normales de un material URP Lit.</summary>
    static void Relieve(Material mat, string normal, float fuerza)
    {
        var textura = normal != null ? ConstructorEstilo.Imagen("Texturas/" + normal) : null;
        mat.SetTexture("_BumpMap", textura);
        mat.SetFloat("_BumpScale", fuerza);
        if (textura != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat);
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
        Entintar(mat);   // Eclipse Ink: solo los materiales de la lista (ConstructorEscenas.Tinta.cs)
        return mat;
    }

    /// <summary>
    /// Capas finas sobre el mapa de la región (debajo de las marcas): polvo de la tormenta que se mueve despacio,
    /// retícula de coordenadas, viñeta, rótulos del cráter Gale y escala.
    /// </summary>
    static void CapasMapa(RectTransform mapa, RectTransform marcas)
    {
        var polvo = new GameObject("Tormenta", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        polvo.transform.SetParent(mapa, false);
        Estirar(polvo.rectTransform);
        polvo.texture = ConstructorEstilo.Imagen("Texturas/Marte_Polvo");
        polvo.uvRect = new Rect(0.3f, 0.35f, 0.25f, 0.5f);
        polvo.color = new Color(1f, 0.85f, 0.7f, 0.14f);
        polvo.raycastTarget = false;
        var mover = polvo.gameObject.AddComponent<DesplazarUV>();
        Asignar(mover, "velocidad", new Vector2(0.006f, 0f));
        var reticula = new GameObject("Reticula", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        reticula.transform.SetParent(mapa, false);
        Estirar(reticula.rectTransform);
        reticula.texture = ConstructorEstilo.Imagen("Interfaz/Reticula");
        reticula.uvRect = new Rect(0, 0, 4, 4);
        reticula.color = new Color(Cian.r, Cian.g, Cian.b, 0.22f);
        reticula.raycastTarget = false;
        var vineta = new GameObject("Vineta", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        vineta.transform.SetParent(mapa, false);
        Estirar(vineta.rectTransform);
        vineta.sprite = ConstructorEstilo.Sprite("Vineta");
        vineta.color = new Color(1, 1, 1, 0.35f);
        vineta.raycastTarget = false;
        TextMeshProUGUI Rotulo(string nombre, string texto, Vector2 min, Vector2 max, TextAlignmentOptions alineacion, float tamano)
        {
            var t = Texto(mapa, nombre, texto, tamano, TextoClaro, FontStyles.Normal, alineacion);
            t.font = ConstructorEstilo.Mono;
            t.characterSpacing = 4;
            t.color = new Color(1f, 1f, 1f, 0.75f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Anclar(t.rectTransform, min, max);
            return t;
        }
        Rotulo("RotuloCrater", "GALE CRATER", new Vector2(0.03f, 0.92f), new Vector2(0.6f, 0.98f), TextAlignmentOptions.Left, 20);
        Rotulo("RotuloMonte", "AEOLIS MONS", new Vector2(0.38f, 0.43f), new Vector2(0.62f, 0.47f), TextAlignmentOptions.Center, 15);
        Rotulo("Coordenadas", "5.4°S  137.8°E", new Vector2(0.6f, 0.92f), new Vector2(0.97f, 0.98f), TextAlignmentOptions.Right, 16);
        Rotulo("Escala", "|——— 50 km ———|", new Vector2(0.03f, 0.02f), new Vector2(0.4f, 0.07f), TextAlignmentOptions.Left, 15);
        Rotulo("Fuente", "RELIEF: NASA 3D RESOURCES", new Vector2(0.5f, 0.02f), new Vector2(0.97f, 0.07f), TextAlignmentOptions.Right, 13);
        marcas.SetAsLastSibling();
    }

    /// <summary>Copia de un material URP Lit que se dibuja por las dos caras (piezas abiertas: cofias).</summary>
    static Material DobleCara(Material original, string nombre)
    {
        var ruta = "Assets/Materiales/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(original);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.CopyPropertiesFromMaterial(original);
        mat.SetFloat("_Cull", 0f);
        mat.doubleSidedGI = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Material aditivo (brillos, estelas, chispas): Assets/Materiales/&lt;nombre&gt;.mat, reutilizado si existe.</summary>
    static Material MaterialAditivo(string nombre, Texture textura, Color color)
    {
        var ruta = "Assets/Materiales/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("OperacionEclipseRojo/Aditivo"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = Shader.Find("OperacionEclipseRojo/Aditivo");
        mat.SetTexture("_MainTex", textura);
        mat.SetColor("_Color", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>LineRenderer en un hijo con transformación identidad (posiciones en el espacio local del padre).</summary>
    static LineRenderer LineaHija(Transform padre, string nombre, Material material, float ancho, Color inicio, Color fin)
    {
        var go = new GameObject(nombre, typeof(LineRenderer));
        go.transform.SetParent(padre, false);
        var l = go.GetComponent<LineRenderer>();
        l.sharedMaterial = material;
        l.widthMultiplier = ancho;
        l.useWorldSpace = false;
        l.startColor = inicio;
        l.endColor = fin;
        l.numCapVertices = 2;
        l.shadowCastingMode = ShadowCastingMode.Off;
        l.receiveShadows = false;
        return l;
    }

    /// <summary>Guarda la malla en Assets/Arte/Mallas conservando el GUID si ya existía (las escenas la siguen encontrando).</summary>
    static Mesh GuardarMalla(Mesh nueva, string nombre, bool contorno = false)
    {
        // Eclipse Ink: normal suavizada para el contorno de tinta (canal UV 3), solo en las mallas que lo llevan.
        if (contorno) NormalesContorno.Preparar(nueva);
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
        existente.subMeshCount = nueva.subMeshCount;
        for (int s = 0; s < nueva.subMeshCount; s++) existente.SetTriangles(nueva.GetTriangles(s), s);
        existente.tangents = nueva.tangents;
        if (contorno)
        {
            var suaves = new List<Vector2>();
            nueva.GetUVs(NormalesContorno.Canal, suaves);
            existente.SetUVs(NormalesContorno.Canal, suaves);
        }
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

    /// <summary>El texto se achica hasta «minimo» si no cabe en su caja y nunca se sale de ella.</summary>
    static void Encoger(TMP_Text t, float minimo)
    {
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = minimo;
        t.overflowMode = TextOverflowModes.Truncate;
    }

    /// <summary>El texto del botón se achica si no cabe (etiquetas largas en inglés).</summary>
    static void AjustarTexto(UnityEngine.UI.Button boton, float minimo = 18f)
    {
        var t = boton.GetComponentInChildren<TextMeshProUGUI>();
        if (t == null) return;
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = minimo;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.margin = new Vector4(14, 0, 14, 0);
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
        "STEP " + pasoActual + " OF " + NombresPasos.Length + "  ·  " + NombresPasos[pasoActual - 1].ToUpperInvariant();

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
