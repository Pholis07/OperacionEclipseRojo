using System.Linq;
using System.Collections.Generic;
using SpaceMission;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Escenarios 3D de la cinemática de lanzamiento (storyboard A) y de la investigación orbital (storyboard B),
/// lejos de la escena de Configuración y fuera de su encuadre:
///   Plataforma (z = 600) → Órbita terrestre (z = 900) → Mapa de transferencia (z = 1200) → Marte (z = 1500).
/// Una sola cámara («CamaraCine») los dibuja en una textura: a pantalla completa en la cinemática y en el lugar
/// del mapa durante cada escaneo. Está apagada el resto del tiempo.
/// </summary>
public static partial class ConstructorEscenas
{
    static readonly Vector3 EscenarioLanzamiento = new Vector3(0f, 0f, 600f);
    static readonly Vector3 EscenarioEspacio = new Vector3(0f, 0f, 900f);
    static readonly Vector3 EscenarioMapa = new Vector3(0f, 0f, 1200f);
    static readonly Vector3 EscenarioMarte = new Vector3(0f, 0f, 1500f);
    static readonly Color CianOrbita = Hex("#69C6D2");

    static UnityEngine.UI.RawImage ultimoMapa;   // el mapa de la Operación (lo guarda ConstruirOperacion)

    static void ConstruirCinematica(RectTransform canvas, UnityEngine.UI.Button lanzar, MusicaEscena musica, OperacionUI operacion, CatalogoDatos catalogo)
    {
        // ---------- Cámara ----------
        var camara = new GameObject("CamaraCine").AddComponent<Camera>();
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = Hex("#020306");
        camara.nearClipPlane = 0.1f;
        camara.farClipPlane = 900f;
        camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camara.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        camara.enabled = false;

        var matAditivo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Sprite_Aditivo.mat");
        if (matAditivo == null)
        {
            matAditivo = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(matAditivo, "Assets/Materiales/Sprite_Aditivo.mat");
        }

        // ---------- 1) Plataforma de lanzamiento ----------
        var cohetes = ConstruirCohetes(EscenarioLanzamiento);
        cohetes.resaltadoExplorador.SetActive(false);
        cohetes.resaltadoTitan.SetActive(false);
        foreach (var c in cohetes.extras) c.resalte.SetActive(false);
        var plataforma = cohetes.explorador.transform.parent;
        plataforma.name = "PlataformaCine";
        Object.DestroyImmediate(cohetes.explorador.GetComponent<GiroLento>());
        Object.DestroyImmediate(cohetes.titan.GetComponent<GiroLento>());
        foreach (var c in cohetes.extras) Object.DestroyImmediate(c.cohete.GetComponent<GiroLento>());
        // El disco de la plataforma de Configuración no hace falta: la losa del complejo lo sustituye.
        Object.DestroyImmediate(plataforma.Find("Suelo").gameObject);
        var complejo = ConstruirComplejo(EscenarioLanzamiento);

        // Una llama por motor, hija de la pieza que la lleva (los propulsores se van con su fuego).
        // Explorador (Ares): la tobera de la primera etapa al pie y el motor de la etapa superior en el corte.
        var explorador = cohetes.explorador.transform;
        var (llamasExplorador, llamaSupExplorador) = LlamasExplorador(explorador);
        // Versión 9: llamas de los lanzadores de la tabla CohetesExtra.
        var llamasExtra = cohetes.extras.Select(c => LlamasExtra(c.cohete, c.id)).ToList();

        var titan = cohetes.titan.transform;
        var inferiorTitan = titan.Find("SLS_NASA/EtapaInferior");
        var superiorTitan = titan.Find("SLS_NASA/EtapaSuperior");
        var izqTitan = inferiorTitan.Find("PropulsorIzq");
        var derTitan = inferiorTitan.Find("PropulsorDer");
        var llamasTitan = ConstruirLlamas(ToberasSls(titan, inferiorTitan, izqTitan, derTitan, 11f));
        var baseSuperior = Limites(superiorTitan.gameObject);
        var llamaSupTitan = ConstruirLlamas(new[] { (superiorTitan, superiorTitan.InverseTransformPoint(new Vector3(baseSuperior.center.x, baseSuperior.min.y, baseSuperior.center.z)), 0.16f, 2.2f) });

        // ---------- 2) Órbita terrestre ----------
        var espacio = new GameObject("OrbitaTerrestre");
        var matTierra = CrearMaterial("Tierra", Color.white, 0f, 0.35f, ConstructorEstilo.Imagen("Texturas/Tierra_Superficie"), Vector2.one);
        var tierra = Malla(EsferaUV(), "Tierra", espacio.transform, EscenarioEspacio + new Vector3(0f, -128f, 40f),
            new Vector3(18f, 0f, 0f), Vector3.one * 240f, matTierra);
        tierra.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var matAtmTierra = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Atmosfera_Tierra.mat");
        if (matAtmTierra == null)
        {
            matAtmTierra = new Material(Shader.Find("OperacionEclipseRojo/Atmosfera"));
            AssetDatabase.CreateAsset(matAtmTierra, "Assets/Materiales/Atmosfera_Tierra.mat");
        }
        matAtmTierra.SetColor("_Color", new Color(0.35f, 0.6f, 1f) * 1.6f);
        matAtmTierra.SetFloat("_Potencia", 3f);
        EditorUtility.SetDirty(matAtmTierra);
        var atmTierra = Malla(EsferaUV(), "AtmosferaTierra", espacio.transform, tierra.transform.position, Vector3.zero,
            Vector3.one * 240f * 1.025f, matAtmTierra);
        atmTierra.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        CieloEstrellas(espacio.transform, EscenarioEspacio, 400f);
        var puntoEspacio = new GameObject("PuntoEspacio").transform;
        puntoEspacio.SetParent(espacio.transform, false);
        puntoEspacio.position = EscenarioEspacio;
        puntoEspacio.rotation = Quaternion.Euler(0f, 0f, -90f);   // el cohete avanza hacia +X con la Tierra debajo
        espacio.SetActive(false);

        // ---------- 3) Mapa de la transferencia ----------
        var mapa = new GameObject("MapaTransferencia");
        var centroMapa = new GameObject("Centro").transform;
        centroMapa.SetParent(mapa.transform, false);
        centroMapa.position = EscenarioMapa;
        var matSol = CrearMaterial("Sol_Mapa", Hex("#FFD27A"), 0f, 0.2f, null, Vector2.one, Hex("#FFB547") * 4f);
        Pieza(PrimitiveType.Sphere, "Sol", mapa.transform, EscenarioMapa, Vector3.zero, Vector3.one * 2.4f, matSol);
        LineaCirculo(mapa.transform, "OrbitaTierra", EscenarioMapa, 10f, new Color(0.45f, 0.65f, 1f, 0.45f));
        LineaCirculo(mapa.transform, "OrbitaMarte", EscenarioMapa, 15.2f, new Color(1f, 0.55f, 0.35f, 0.45f));
        var tierraMapa = Malla(EsferaUV(), "TierraMapa", mapa.transform, EscenarioMapa + Vector3.right * 10f, Vector3.zero, Vector3.one * 1.3f, matTierra);
        var marteMapa = Malla(EsferaUV(), "MarteMapa", mapa.transform, EscenarioMapa, Vector3.zero, Vector3.one * 1f,
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Marte.mat"));
        var matMarcador = CrearMaterial("Marcador_Orbita", Cian, 0f, 0.5f, null, Vector2.one, Cian * 3f);
        var marcador = Pieza(PrimitiveType.Sphere, "Nave", mapa.transform, EscenarioMapa, Vector3.zero, Vector3.one * 0.4f, matMarcador);
        var arco = new GameObject("Arco", typeof(LineRenderer)).GetComponent<LineRenderer>();
        arco.transform.SetParent(mapa.transform, false);
        arco.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat");
        arco.widthMultiplier = 0.16f;
        arco.startColor = arco.endColor = CianOrbita;
        arco.useWorldSpace = true;
        arco.positionCount = 0;
        arco.shadowCastingMode = ShadowCastingMode.Off;
        var estrellasMapa = Pieza(PrimitiveType.Quad, "Estrellas", mapa.transform, EscenarioMapa + new Vector3(0f, -40f, 0f), new Vector3(90f, 0f, 0f),
            new Vector3(420f, 240f, 1f), AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Estrellas.mat"));
        estrellasMapa.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        mapa.SetActive(false);

        // ---------- 4) Marte en órbita (llegada e investigación) ----------
        var raizOrbital = new GameObject("EscenarioOrbital");
        raizOrbital.transform.position = EscenarioMarte;
        const float diametroMarte = 26f;
        var marte = ConstruirMarte(EscenarioMarte, diametroMarte);
        CieloEstrellas(raizOrbital.transform, EscenarioMarte, 500f);
        var linea = raizOrbital.AddComponent<LineRenderer>();
        linea.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat");
        linea.widthMultiplier = 0.045f;
        linea.startColor = linea.endColor = new Color(CianOrbita.r, CianOrbita.g, CianOrbita.b, 0.55f);
        linea.shadowCastingMode = ShadowCastingMode.Off;
        var naveOrbita = ConstruirNave(null, false);
        naveOrbita.name = "NaveOrbita";
        naveOrbita.transform.localScale = Vector3.one * 0.3f;
        // Sin sombras sobre Marte: a esta escala se verían como una mancha enorme.
        foreach (var r in naveOrbita.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        var escenario = raizOrbital.AddComponent<EscenarioOrbital>();
        Asignar(escenario, "marte", marte.transform);
        Asignar(escenario, "radioPlaneta", diametroMarte / 2f);
        Asignar(escenario, "radioOrbita", diametroMarte / 2f * 1.32f);
        Asignar(escenario, "lineaOrbita", linea);
        Asignar(escenario, "satelite", naveOrbita.transform);
        Asignar(escenario, "nave", naveOrbita);

        SpriteRenderer Brillo(string nombre, Transform padre, Color color, float escala)
        {
            var sr = new GameObject(nombre, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            sr.transform.SetParent(padre, false);
            sr.transform.localScale = Vector3.one * escala;
            sr.sprite = sHalo;
            sr.color = color;
            sr.sharedMaterial = matAditivo;
            sr.gameObject.SetActive(false);
            return sr;
        }
        var pulsoZona = Brillo("ZonaInvestigacion", raizOrbital.transform, new Color(1f, 0.5f, 0.28f, 0.75f), 4f);
        pulsoZona.gameObject.AddComponent<PulsoAnomalia>();
        Asignar(pulsoZona.GetComponent<PulsoAnomalia>(), "anillo", pulsoZona);
        Asignar(pulsoZona.GetComponent<PulsoAnomalia>(), "periodo", 2.4f);
        Asignar(pulsoZona.GetComponent<PulsoAnomalia>(), "escalaMaxima", 1.8f);

        // Haz del instrumento: cono con el vértice en el satélite (largo 1 en Z; se escala hasta la superficie).
        var haz = new GameObject("HazInstrumento").transform;
        haz.SetParent(raizOrbital.transform, false);
        var matHaz = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Haz_Instrumento.mat");
        if (matHaz == null)
        {
            matHaz = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(matHaz, "Assets/Materiales/Haz_Instrumento.mat");
        }
        var conoHaz = Malla(GuardarMalla(Cono(0.02f, 0.5f, 1f), "Haz_Cono"), "Cono", haz, Vector3.zero, new Vector3(90f, 0f, 0f), Vector3.one, matHaz);
        conoHaz.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        haz.gameObject.SetActive(false);
        var mancha = Brillo("ManchaEscaneo", raizOrbital.transform, Color.white, 2f);

        // ---------- Pantalla de la cinemática (encima de todo) ----------
        var velo = Panel(canvas, "Cinematica", Color.black);
        Estirar(velo);
        var grupoVelo = velo.gameObject.AddComponent<CanvasGroup>();
        var pantalla = new GameObject("Pantalla", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        pantalla.transform.SetParent(velo, false);
        Estirar(pantalla.rectTransform);
        // Franjas de cine arriba y abajo.
        var franjaArriba = Panel(velo, "FranjaArriba", Color.black);
        Anclar(franjaArriba, new Vector2(0, 0.92f), Vector2.one);
        var franjaAbajo = Panel(velo, "FranjaAbajo", Color.black);
        Anclar(franjaAbajo, Vector2.zero, new Vector2(1, 0.08f));
        var fase = Texto(velo, "Fase", "", 24, Cian, FontStyles.Normal, TextAlignmentOptions.Left);
        fase.font = ConstructorEstilo.Mono;
        fase.characterSpacing = 6;
        Anclar(fase.rectTransform, new Vector2(0.04f, 0.925f), new Vector2(0.5f, 0.995f));
        var hud = Texto(velo, "Hud", "", 40, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
        hud.characterSpacing = 10;
        Anclar(hud.rectTransform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.2f));
        var cuenta = Texto(velo, "Cuenta", "", 150, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        cuenta.font = ConstructorEstilo.Mono;
        Anclar(cuenta.rectTransform, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.65f));
        // Destello blanco al encender los motores (encima de la imagen, debajo de los textos).
        var destello = Panel(velo, "Destello", new Color(1f, 0.96f, 0.9f, 1f));
        Estirar(destello);
        destello.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        destello.SetSiblingIndex(1);
        var grupoDestello = destello.gameObject.AddComponent<CanvasGroup>();
        grupoDestello.alpha = 0f;
        grupoDestello.blocksRaycasts = false;
        var omitir = Boton(velo, "BotonOmitir", "Skip  " + SpaceMission.Iconos.Omitir, false);
        Fijar(omitir.GetComponent<RectTransform>(), new Vector2(0.98f, 0.015f), new Vector2(1, 0), new Vector2(260, 64));
        var tOmitir = omitir.GetComponentInChildren<TextMeshProUGUI>();
        tOmitir.fontSize = 28;
        var negro = Panel(velo, "Corte", Color.black);
        Estirar(negro);
        negro.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var grupoCorte = negro.gameObject.AddComponent<CanvasGroup>();
        grupoCorte.blocksRaycasts = false;
        velo.gameObject.SetActive(false);

        var raizDirector = new GameObject("DirectorCinematica", typeof(AudioSource));
        var fuente = raizDirector.GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        var fuenteBucle = raizDirector.AddComponent<AudioSource>();
        fuenteBucle.playOnAwake = false;
        fuenteBucle.loop = true;
        fuenteBucle.spatialBlend = 0f;
        var director = raizDirector.AddComponent<DirectorCinematica>();
        Asignar(director, "botonLanzar", lanzar);
        Asignar(director, "camara", camara);
        Asignar(director, "musica", musica);
        Asignar(director, "fuente", fuente);
        Asignar(director, "velo", grupoVelo);
        Asignar(director, "pantalla", pantalla);
        Asignar(director, "corte", grupoCorte);
        Asignar(director, "textoHud", hud);
        Asignar(director, "textoFase", fase);
        Asignar(director, "textoCuenta", cuenta);
        Asignar(director, "botonOmitir", omitir);
        Asignar(director, "coheteExplorador", cohetes.explorador.transform);
        Asignar(director, "coheteTitan", cohetes.titan.transform);
        Asignar(director, "superiorExplorador", cohetes.explorador.transform.Find("EtapaSuperior"));
        Asignar(director, "superiorTitan", cohetes.titan.transform.Find("SLS_NASA/EtapaSuperior"));
        {
            var so = new SerializedObject(director);
            var lista = so.FindProperty("cohetesExtra");
            lista.arraySize = cohetes.extras.Count;
            for (int i = 0; i < cohetes.extras.Count; i++)
            {
                var (id, cohete, _) = cohetes.extras[i];
                var e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = id;
                e.FindPropertyRelative("cohete").objectReferenceValue = cohete;
                e.FindPropertyRelative("superior").objectReferenceValue = cohete.Find("EtapaSuperior");
                void Lista(string campo, Object[] valores)
                {
                    var p = e.FindPropertyRelative(campo);
                    p.arraySize = valores.Length;
                    for (int k = 0; k < valores.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = valores[k];
                }
                Lista("llamas", llamasExtra[i].pie.ToArray());
                Lista("llamaSuperior", llamasExtra[i].superior.ToArray());
                Lista("cofia", new Object[] { cohete.Find("EtapaSuperior/CofiaFrente"), cohete.Find("EtapaSuperior/CofiaDetras") });
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AsignarLista(director, "soloEnTierra", new Object[] { complejo.raiz });
        Asignar(director, "cielo", complejo.cielo);
        AsignarLista(director, "brazos", complejo.brazos);
        AsignarLista(director, "sujeciones", complejo.sujeciones);
        Asignar(director, "luzMotores", complejo.luz);
        Asignar(director, "chispas", complejo.chispas);
        AsignarLista(director, "humoLados", complejo.humoLados);
        Asignar(director, "vapor", complejo.vapor);
        Asignar(director, "estela", complejo.estela);
        Asignar(director, "aves", complejo.aves);
        AsignarLista(director, "llamasExplorador", llamasExplorador.ToArray());
        AsignarLista(director, "llamasTitan", llamasTitan.ToArray());
        AsignarLista(director, "llamaSuperiorExplorador", llamaSupExplorador.ToArray());
        AsignarLista(director, "llamaSuperiorTitan", llamaSupTitan.ToArray());
        AsignarLista(director, "propulsoresExplorador", new Object[0]);   // el Ares no tiene propulsores laterales
        AsignarLista(director, "propulsoresTitan", new Object[] { izqTitan, derTitan });
        Asignar(director, "fuenteBucle", fuenteBucle);
        Asignar(director, "destello", grupoDestello);
        AsignarLista(director, "cofiaExplorador", new Object[] { explorador.Find("EtapaSuperior/CofiaFrente"), explorador.Find("EtapaSuperior/CofiaDetras") });
        AsignarLista(director, "cofiaTitan", new Object[] { superiorTitan.Find("NarizIzq"), superiorTitan.Find("NarizDer") });
        var matChispaCine = MaterialAditivo("Particula_Chispa", ConstructorEstilo.Sprite("Chispa").texture, Color.white);
        var matGas = MaterialParticulas("Particula_Nube", ConstructorEstilo.Sprite("Nube").texture, false);
        var piros = Particulas("PirotecniaCofia", raizDirector.transform, Vector3.zero, Quaternion.identity, matChispaCine, 60, ps =>
        {
            var m = ps.main; m.loop = false; m.duration = 0.3f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            m.startColor = new Color(4f, 3f, 1.6f);
            var e = ps.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            var f = ps.shape; f.shapeType = ParticleSystemShapeType.Sphere; f.radius = 0.25f;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f;
        });
        var gas = Particulas("GasFrio", raizDirector.transform, Vector3.zero, Quaternion.identity, matGas, 40, ps =>
        {
            var m = ps.main; m.loop = false; m.duration = 0.6f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            m.startColor = new Color(1f, 1f, 1f, 0.55f);
            var e = ps.emission; e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 8), new ParticleSystem.Burst(0.25f, 8), new ParticleSystem.Burst(0.5f, 8) });
            var f = ps.shape; f.shapeType = ParticleSystemShapeType.Sphere; f.radius = 0.15f;
            var tam = ps.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2f));
            var col = ps.colorOverLifetime; col.enabled = true; col.color = Degradado(Color.white, Color.white, 0.55f);
        });
        Asignar(director, "pirotecnia", piros);
        Asignar(director, "gasFrio", gas);
        Asignar(director, "espacio", espacio);
        Asignar(director, "puntoEspacio", puntoEspacio);
        Asignar(director, "mapaTransferencia", mapa);
        Asignar(director, "centroMapa", centroMapa);
        Asignar(director, "tierraMapa", tierraMapa.transform);
        Asignar(director, "marteMapa", marteMapa.transform);
        Asignar(director, "marcadorMapa", marcador.transform);
        Asignar(director, "arco", arco);
        Asignar(director, "escenario", escenario);
        Asignar(director, "pulsoZona", pulsoZona);
        Asignar(director, "mapaOperacion", ultimoMapa);

        // ---------- Vista orbital en el lugar del mapa (cada escaneo) ----------
        var vista = Panel(ultimoMapa.transform, "VistaOrbital", Color.black);
        Estirar(vista);
        var grupoVista = vista.gameObject.AddComponent<CanvasGroup>();
        var pantallaVista = new GameObject("Pantalla", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        pantallaVista.transform.SetParent(vista, false);
        Estirar(pantallaVista.rectTransform);
        pantallaVista.raycastTarget = false;
        var reticula = Texto(vista, "Reticula", SpaceMission.Iconos.Mira, 72, CianOrbita, FontStyles.Normal, TextAlignmentOptions.Center);
        reticula.rectTransform.anchorMin = reticula.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        reticula.rectTransform.sizeDelta = new Vector2(90, 90);
        reticula.raycastTarget = false;
        var veloSuperior = Panel(vista, "VeloSuperior", new Color(0, 0, 0, 0.55f));
        Anclar(veloSuperior, new Vector2(0, 0.9f), Vector2.one);
        veloSuperior.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var textoInstrumento = Texto(vista, "Instrumento", "", 24, CianOrbita, FontStyles.Bold, TextAlignmentOptions.Left);
        textoInstrumento.characterSpacing = 4;
        // El nombre a la izquierda y «Click to skip» a la derecha, sin pisarse.
        Anclar(textoInstrumento.rectTransform, new Vector2(0.03f, 0.9f), new Vector2(0.66f, 1f));
        textoInstrumento.enableAutoSizing = true; textoInstrumento.fontSizeMin = 14; textoInstrumento.fontSizeMax = textoInstrumento.fontSize;
        textoInstrumento.textWrappingMode = TextWrappingModes.NoWrap;
        var veloInferior = Panel(vista, "VeloInferior", new Color(0, 0, 0, 0.6f));
        Anclar(veloInferior, Vector2.zero, new Vector2(1, 0.12f));
        veloInferior.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var textoEstado = Texto(vista, "Estado", "", 28, CianOrbita, FontStyles.Bold, TextAlignmentOptions.Center);
        textoEstado.font = ConstructorEstilo.Mono;
        textoEstado.characterSpacing = 4;
        Anclar(textoEstado.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.12f));
        var pista = Texto(vista, "Pista", "Click to skip", 20, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        Anclar(pista.rectTransform, new Vector2(0.68f, 0.9f), new Vector2(0.97f, 1f));
        pista.textWrappingMode = TextWrappingModes.NoWrap;
        MarcoHud(vista);
        // El panel entero es el botón «saltar»: tapa el mapa y evita escaneos dobles mientras se ve.
        var saltar = vista.gameObject.AddComponent<UnityEngine.UI.Button>();
        saltar.transition = UnityEngine.UI.Selectable.Transition.None;
        vista.gameObject.SetActive(false);

        var fuenteEscaneo = raizOrbital.AddComponent<AudioSource>();
        fuenteEscaneo.playOnAwake = false;
        fuenteEscaneo.spatialBlend = 0f;
        var secuencia = raizOrbital.AddComponent<SecuenciaEscaneo>();
        Asignar(secuencia, "operacion", operacion);
        Asignar(secuencia, "mapa", ultimoMapa);
        Asignar(secuencia, "escenario", escenario);
        Asignar(secuencia, "camara", camara);
        Asignar(secuencia, "director", director);
        Asignar(secuencia, "vista", grupoVista);
        Asignar(secuencia, "pantalla", pantallaVista);
        Asignar(secuencia, "saltar", saltar);
        Asignar(secuencia, "reticula", reticula.rectTransform);
        Asignar(secuencia, "textoEstado", textoEstado);
        Asignar(secuencia, "textoInstrumento", textoInstrumento);
        Asignar(secuencia, "haz", haz);
        Asignar(secuencia, "rendererHaz", conoHaz.GetComponent<Renderer>());
        Asignar(secuencia, "mancha", mancha);
        Asignar(secuencia, "fuente", fuenteEscaneo);
        Asignar(secuencia, "catalogo", catalogo);

        // ---------- «Pasa un mes» (entre «Resolver mes» y el resumen) ----------
        var padreOperacion = operacion.transform as RectTransform ?? canvas;
        var paso = Panel(padreOperacion, "TransicionMes", new Color(0.015f, 0.02f, 0.035f, 1f));
        Estirar(paso);
        paso.SetAsLastSibling();   // encima del resumen y de la decisión
        var grupoPaso = paso.gameObject.AddComponent<CanvasGroup>();
        var fondoPaso = new GameObject("Pantalla", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
        fondoPaso.transform.SetParent(paso, false);
        Estirar(fondoPaso.rectTransform);
        fondoPaso.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        fondoPaso.raycastTarget = false;
        var vinetaPaso = new GameObject("Vineta", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        vinetaPaso.transform.SetParent(paso, false);
        vinetaPaso.sprite = ConstructorEstilo.Sprite("Vineta");
        vinetaPaso.raycastTarget = false;
        Estirar(vinetaPaso.rectTransform);
        var tarjeta = Columna(paso, "Tarjeta", 16, TextAnchor.MiddleCenter, new Color(Fondo.r, Fondo.g, Fondo.b, 0.82f), 34);
        Anclar(tarjeta, new Vector2(0.32f, 0.27f), new Vector2(0.68f, 0.73f));
        tarjeta.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        MarcoHud(tarjeta);
        var etiquetaPaso = Texto(tarjeta, "Etiqueta", "TIME PASSES", 24, Cian, FontStyles.Normal, TextAlignmentOptions.Center);
        etiquetaPaso.font = ConstructorEstilo.Mono;
        etiquetaPaso.characterSpacing = 10;
        var ventana = Vacio(tarjeta, "Ventana");
        Alto(ventana, 150);
        ventana.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        TextMeshProUGUI Mes(string nombre)
        {
            var m = Texto(ventana, nombre, "MONTH 1", 120, TextoClaro, FontStyles.Bold, TextAlignmentOptions.Center);
            m.characterSpacing = 8;
            m.textWrappingMode = TextWrappingModes.NoWrap;
            Estirar(m.rectTransform);
            return m;
        }
        var mesAnterior = Mes("MesAnterior");
        var mesNuevo = Mes("MesNuevo");
        mesNuevo.color = Acento;
        var tira = Fila(tarjeta, "Meses", 6);
        Alto(tira, 14);
        var segmentos = new Object[12];
        for (int i = 0; i < 12; i++)
        {
            var seg = Panel(tira, "Mes" + (i + 1), new Color(1, 1, 1, 0.12f));
            seg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            segmentos[i] = seg.GetComponent<UnityEngine.UI.Image>();
        }
        var dias = Texto(tarjeta, "Dias", "DAY 0", 30, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Center);
        dias.font = ConstructorEstilo.Mono;
        dias.characterSpacing = 6;
        var suceso = Texto(tarjeta, "Suceso", "", 30, Acento, FontStyles.Bold, TextAlignmentOptions.Center);
        suceso.characterSpacing = 4;
        var pistaPaso = Texto(paso, "Pista", "Click to skip", 20, TextoSecundario, FontStyles.Normal, TextAlignmentOptions.Right);
        Anclar(pistaPaso.rectTransform, new Vector2(0.6f, 0.03f), new Vector2(0.97f, 0.08f));
        var saltarPaso = paso.gameObject.AddComponent<UnityEngine.UI.Button>();
        saltarPaso.transition = UnityEngine.UI.Selectable.Transition.None;
        paso.gameObject.SetActive(false);

        var fuentePaso = raizOrbital.AddComponent<AudioSource>();
        fuentePaso.playOnAwake = false;
        fuentePaso.spatialBlend = 0f;
        var transicion = raizOrbital.AddComponent<TransicionMes>();
        Asignar(transicion, "operacion", operacion);
        Asignar(transicion, "director", director);
        Asignar(transicion, "escenario", escenario);
        Asignar(transicion, "camara", camara);
        Asignar(transicion, "grupo", grupoPaso);
        Asignar(transicion, "pantalla", fondoPaso);
        Asignar(transicion, "saltar", saltarPaso);
        Asignar(transicion, "tarjeta", tarjeta);
        Asignar(transicion, "textoMesAnterior", mesAnterior);
        Asignar(transicion, "textoMesNuevo", mesNuevo);
        Asignar(transicion, "textoDias", dias);
        Asignar(transicion, "textoSuceso", suceso);
        AsignarLista(transicion, "segmentos", segmentos);
        Asignar(transicion, "fuente", fuentePaso);
        Asignar(secuencia, "transicion", transicion);
    }

    static void LineaCirculo(Transform padre, string nombre, Vector3 centro, float radio, Color color)
    {
        var l = new GameObject(nombre, typeof(LineRenderer)).GetComponent<LineRenderer>();
        l.transform.SetParent(padre, false);
        l.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Linea_Orbita.mat");
        l.widthMultiplier = 0.06f;
        l.startColor = l.endColor = color;
        l.useWorldSpace = true;
        l.loop = true;
        l.positionCount = 96;
        for (int i = 0; i < 96; i++)
        {
            float a = i * Mathf.PI * 2f / 96;
            l.SetPosition(i, centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio);
        }
        l.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>
    /// Esfera de estrellas vista desde dentro (escala negativa = caras hacia dentro). Versión 5, bloque 6: el cielo
    /// entero es el Deep Star Map 2020 de la NASA (Goddard SVS) en coordenadas galácticas, a 8k.
    /// </summary>
    static void CieloEstrellas(Transform padre, Vector3 centro, float diametro)
    {
        const string ruta = "Assets/Materiales/Cielo_NASA.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.SetTexture("_BaseMap", ConstructorEstilo.Imagen("Texturas/Cielo_NASA"));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(mat);
        var cielo = Malla(EsferaUV(), "CieloEstrellas", padre, centro, Vector3.zero, new Vector3(-diametro, diametro, diametro), mat);
        cielo.transform.position = centro;
        cielo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }
}
