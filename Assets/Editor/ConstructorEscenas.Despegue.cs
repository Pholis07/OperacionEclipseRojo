using System.Collections.Generic;
using SpaceMission;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Complejo de lanzamiento de la cinemática (todo propio, hecho por código): cielo con nubes, pasto, camino,
/// playa y mar, colinas con bruma, unos 260 árboles de pocos polígonos (en 3 mallas: 3 dibujados), losa con
/// marcas y foso de llamas, torre de celosía con brazos que se retiran, pararrayos, torre de agua, reflectores y
/// edificios. Efectos: una llama por motor (shader Llama), luz de los motores, chispas, humo por los lados del
/// foso, vapor, columna de humo del ascenso y una bandada de aves. ≈ 1,200 partículas como máximo.
/// </summary>
public static partial class ConstructorEscenas
{
    struct Complejo
    {
        public GameObject raiz;
        public Renderer cielo;
        public Transform[] brazos;
        public Light luz;
        public ParticleSystem chispas, vapor, estela, aves;
        public ParticleSystem[] humoLados;
    }

    static Complejo ConstruirComplejo(Vector3 b)
    {
        var c = new Complejo();
        var raiz = new GameObject("ComplejoLanzamiento");
        raiz.transform.position = b;
        c.raiz = raiz;
        var t = raiz.transform;

        // ----- Cielo (esfera vista desde dentro) -----
        var matCielo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Cielo_Lanzamiento.mat");
        if (matCielo == null)
        {
            matCielo = new Material(Shader.Find("OperacionEclipseRojo/Cielo"));
            AssetDatabase.CreateAsset(matCielo, "Assets/Materiales/Cielo_Lanzamiento.mat");
        }
        matCielo.shader = Shader.Find("OperacionEclipseRojo/Cielo");
        matCielo.SetTexture("_Nubes", ConstructorEstilo.Imagen("Texturas/Nubes"));
        var sol = GameObject.Find("Sol");
        var haciaSol = sol != null ? -sol.transform.forward : new Vector3(-0.5f, 0.4f, -0.7f).normalized;
        matCielo.SetVector("_DireccionSol", haciaSol);
        EditorUtility.SetDirty(matCielo);
        var cielo = Malla(EsferaUV(), "Cielo", t, Vector3.zero, Vector3.zero, Vector3.one * 980f, matCielo);
        cielo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        cielo.GetComponent<Renderer>().receiveShadows = false;
        c.cielo = cielo.GetComponent<Renderer>();

        // ----- Suelo: pasto, camino, playa y mar -----
        var matPasto = CrearMaterial("Lanzamiento_Pasto", Color.white, 0f, 0.12f, ConstructorEstilo.Imagen("Texturas/Pasto"), new Vector2(70, 26));
        Pieza(PrimitiveType.Quad, "Pasto", t, new Vector3(0, -0.02f, -100f), new Vector3(90, 0, 0), new Vector3(1000, 460, 1), matPasto)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var matArena = CrearMaterial("Lanzamiento_Arena", Hex("#CBB98E"), 0f, 0.2f);
        Pieza(PrimitiveType.Quad, "Playa", t, new Vector3(0, -0.06f, 136f), new Vector3(90, 0, 0), new Vector3(1000, 16, 1), matArena);
        var matMar = CrearMaterial("Lanzamiento_Mar", Hex("#2F5F82"), 0.1f, 0.93f);
        Pieza(PrimitiveType.Quad, "Mar", t, new Vector3(0, -0.35f, 520f), new Vector3(90, 0, 0), new Vector3(1600, 760, 1), matMar)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var matCamino = CrearMaterial("Lanzamiento_Camino", Hex("#3C3D40"), 0f, 0.3f);
        Pieza(PrimitiveType.Quad, "Camino", t, new Vector3(0, 0.01f, -120f), new Vector3(90, 0, 0), new Vector3(7, 200, 1), matCamino);

        // ----- Colinas lejanas (la bruma las funde con el cielo) -----
        var matColina = CrearMaterial("Lanzamiento_Colina", Hex("#56704E"), 0f, 0.1f);
        var semilla = new System.Random(5);
        for (int i = 0; i < 16; i++)
        {
            float a = Mathf.Lerp(150f, 390f, i / 15f) * Mathf.Deg2Rad;
            float r = 330f + (float)semilla.NextDouble() * 80f;
            var pos = new Vector3(Mathf.Cos(a) * r, -6f, Mathf.Sin(a) * r);
            var escala = new Vector3(90f + (float)semilla.NextDouble() * 80f, 22f + (float)semilla.NextDouble() * 30f, 70f + (float)semilla.NextDouble() * 50f);
            Pieza(PrimitiveType.Sphere, "Colina" + i, t, pos, new Vector3(0, a * Mathf.Rad2Deg, 0), escala, matColina)
                .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ----- Árboles (3 mallas) -----
        ConstruirArboles(t, semilla);

        // ----- Plataforma: losa, foso de llamas -----
        var matLosa = CrearMaterial("Lanzamiento_Losa", Color.white, 0f, 0.25f, ConstructorEstilo.Imagen("Texturas/Losa"), Vector2.one);
        Pieza(PrimitiveType.Cube, "Losa", t, new Vector3(0, -0.5f, 0), Vector3.zero, new Vector3(34f, 1f, 34f), matLosa);
        var matFoso = CrearMaterial("Lanzamiento_Foso", Hex("#1A1A1C"), 0f, 0.1f);
        Pieza(PrimitiveType.Cube, "Foso", t, new Vector3(0, 0.01f, 0), Vector3.zero, new Vector3(32f, 0.02f, 3.4f), matFoso);

        // ----- Torre de servicio: celosía con brazos que se retiran -----
        var matTorre = CrearMaterial("Torre_Servicio", Hex("#5B6470"), 0.6f, 0.4f);
        var matTorreAcento = CrearMaterial("Torre_Acento", Hex("#B9472A"), 0.3f, 0.45f);
        var torre = new GameObject("TorreServicio").transform;
        torre.SetParent(t, false);
        torre.localPosition = new Vector3(0f, 0f, 3.4f);
        const float lado = 1.8f, altoTorre = 17f;
        foreach (var (x, z) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            Pieza(PrimitiveType.Cube, "Pata", torre, new Vector3(x * lado / 2, altoTorre / 2, z * lado / 2), Vector3.zero, new Vector3(0.14f, altoTorre, 0.14f), matTorre);
        for (float y = 1.4f; y < altoTorre; y += 1.4f)
        {
            Pieza(PrimitiveType.Cube, "Banda", torre, new Vector3(0, y, -lado / 2), Vector3.zero, new Vector3(lado, 0.09f, 0.09f), matTorre);
            Pieza(PrimitiveType.Cube, "Banda", torre, new Vector3(0, y, lado / 2), Vector3.zero, new Vector3(lado, 0.09f, 0.09f), matTorre);
            Pieza(PrimitiveType.Cube, "Banda", torre, new Vector3(-lado / 2, y, 0), Vector3.zero, new Vector3(0.09f, 0.09f, lado), matTorre);
            Pieza(PrimitiveType.Cube, "Banda", torre, new Vector3(lado / 2, y, 0), Vector3.zero, new Vector3(0.09f, 0.09f, lado), matTorre);
            float diag = Mathf.Sqrt(lado * lado + 1.4f * 1.4f);
            float ang = Mathf.Atan2(1.4f, lado) * Mathf.Rad2Deg;
            Pieza(PrimitiveType.Cube, "Cruz", torre, new Vector3(0, y - 0.7f, -lado / 2), new Vector3(0, 0, ang), new Vector3(diag, 0.06f, 0.06f), matTorre);
            Pieza(PrimitiveType.Cube, "Cruz", torre, new Vector3(-lado / 2, y - 0.7f, 0), new Vector3(ang, 90, 0), new Vector3(diag, 0.06f, 0.06f), matTorre);
        }
        Pieza(PrimitiveType.Cube, "Cima", torre, new Vector3(0, altoTorre + 0.2f, 0), Vector3.zero, new Vector3(lado + 0.3f, 0.4f, lado + 0.3f), matTorreAcento);
        Pieza(PrimitiveType.Cylinder, "Antena", torre, new Vector3(0, altoTorre + 2.4f, 0), Vector3.zero, new Vector3(0.12f, 2f, 0.12f), matTorre);
        // Brazos: el pivote en la cara de la torre; giran hacia un lado para soltar el cohete.
        var brazos = new List<Transform>();
        for (int i = 0; i < 3; i++)
        {
            var pivote = new GameObject("Brazo" + i).transform;
            pivote.SetParent(torre, false);
            pivote.localPosition = new Vector3(-lado / 2, 3f + i * 2.5f, -lado / 2);
            Pieza(PrimitiveType.Cube, "Viga", pivote, new Vector3(0.6f, 0f, -1.1f), Vector3.zero, new Vector3(0.35f, 0.3f, 2.4f), matTorreAcento);
            Pieza(PrimitiveType.Cube, "Baranda", pivote, new Vector3(0.6f, 0.35f, -1.1f), Vector3.zero, new Vector3(0.36f, 0.05f, 2.4f), matTorre);
            brazos.Add(pivote);
        }
        c.brazos = brazos.ToArray();

        // ----- Pararrayos, torre de agua, reflectores y edificios -----
        var matMastil = CrearMaterial("Lanzamiento_Mastil", Hex("#C8CCD2"), 0.7f, 0.5f);
        foreach (var (x, z) in new[] { (-11f, -9f), (11f, -9f), (-11f, 12f), (11f, 12f) })
        {
            Pieza(PrimitiveType.Cylinder, "Pararrayos", t, new Vector3(x, 13f, z), Vector3.zero, new Vector3(0.3f, 13f, 0.3f), matMastil);
            Pieza(PrimitiveType.Cylinder, "Punta", t, new Vector3(x, 27f, z), Vector3.zero, new Vector3(0.1f, 1.2f, 0.1f), matMastil);
        }
        var agua = new GameObject("TorreAgua").transform;
        agua.SetParent(t, false);
        agua.localPosition = new Vector3(-24f, 0f, 18f);
        foreach (var (x, z) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (-1.4f, 1.4f), (1.4f, 1.4f) })
            Pieza(PrimitiveType.Cylinder, "Pata", agua, new Vector3(x, 5f, z), Vector3.zero, new Vector3(0.25f, 5f, 0.25f), matMastil);
        Pieza(PrimitiveType.Sphere, "Tanque", agua, new Vector3(0, 11.5f, 0), Vector3.zero, new Vector3(5f, 4.2f, 5f), matMastil);
        var matFoco = CrearMaterial("Lanzamiento_Foco", Hex("#FFF4D6"), 0f, 0.5f, null, Vector2.one, Hex("#FFE7B0") * 2.5f);
        foreach (var (x, z) in new[] { (-15f, -12f), (15f, -12f), (-15f, 14f), (15f, 14f) })
        {
            Pieza(PrimitiveType.Cylinder, "Poste", t, new Vector3(x, 4f, z), Vector3.zero, new Vector3(0.18f, 4f, 0.18f), matTorre);
            var foco = Pieza(PrimitiveType.Cube, "Foco", t, new Vector3(x, 8.2f, z), Vector3.zero, new Vector3(1.2f, 0.6f, 0.4f), matFoco);
            foco.transform.LookAt(t.position + Vector3.up * 3f);
        }
        var matEdificio = CrearMaterial("Lanzamiento_Edificio", Hex("#C9CDD1"), 0f, 0.35f);
        var matFranja = CrearMaterial("Lanzamiento_Franja", Hex("#2E4A7A"), 0f, 0.4f);
        Pieza(PrimitiveType.Cube, "Hangar", t, new Vector3(-85f, 17f, 95f), new Vector3(0, 20, 0), new Vector3(30f, 34f, 26f), matEdificio);
        Pieza(PrimitiveType.Cube, "HangarFranja", t, new Vector3(-85f, 30f, 95f), new Vector3(0, 20, 0), new Vector3(30.2f, 3f, 26.2f), matFranja);
        Pieza(PrimitiveType.Cube, "Control", t, new Vector3(60f, 4f, 90f), new Vector3(0, -15, 0), new Vector3(18f, 8f, 12f), matEdificio);

        // ----- Luz de los motores (apagada hasta el encendido) -----
        var luz = new GameObject("LuzMotores").AddComponent<Light>();
        luz.transform.SetParent(t, false);
        luz.transform.localPosition = Vector3.up * 0.5f;
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.62f, 0.3f);
        luz.range = 80f;
        luz.intensity = 0f;
        luz.shadows = LightShadows.None;
        luz.enabled = false;
        c.luz = luz;

        // ----- Partículas -----
        var matNube = MaterialParticulas("Particula_Nube", ConstructorEstilo.Sprite("Nube").texture, false);
        var matChispa = MaterialAditivo("Particula_Chispa", ConstructorEstilo.Sprite("Chispa").texture, Color.white);
        var matAve = MaterialParticulas("Particula_Ave", ConstructorEstilo.Sprite("Ave").texture, false);

        c.chispas = Particulas("Chispas", t, Vector3.zero, Quaternion.Euler(-90, 0, 0), matChispa, 240, p =>
        {
            var m = p.main; m.loop = false; m.duration = 1.4f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 14f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(4f, 2.8f, 1.2f), new Color(4f, 1.6f, 0.4f));
            m.gravityModifier = 1.6f;
            var e = p.emission; e.rateOverTime = 220f;
            var f = p.shape; f.shapeType = ParticleSystemShapeType.Cone; f.angle = 70f; f.radius = 1.2f;
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.03f; r.lengthScale = 1.5f;
        });
        var lados = new List<ParticleSystem>();
        foreach (int s in new[] { -1, 1 })
            lados.Add(Particulas("HumoFoso" + (s < 0 ? "Izq" : "Der"), t, new Vector3(s * 14f, 0.8f, 0f), Quaternion.LookRotation(new Vector3(s, 0.12f, 0f)), matNube, 230, p =>
            {
                var m = p.main; m.loop = true; m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 5.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(8f, 17f);
                m.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                m.startColor = new Color(0.9f, 0.88f, 0.85f, 0.75f);
                var e = p.emission; e.rateOverTime = 50f;
                var f = p.shape; f.shapeType = ParticleSystemShapeType.Cone; f.angle = 25f; f.radius = 1.5f;
                var limite = p.limitVelocityOverLifetime; limite.enabled = true; limite.limit = 3f; limite.dampen = 0.08f;
                var tam = p.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 2.6f));
                var col = p.colorOverLifetime; col.enabled = true; col.color = Degradado(new Color(1f, 0.8f, 0.6f), new Color(0.92f, 0.92f, 0.92f), 0.7f);
                var vel = p.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(0.6f, 2.2f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);   // las tres en el mismo modo
            }));
        c.humoLados = lados.ToArray();
        c.vapor = Particulas("Vapor", t, new Vector3(0, 0.5f, 0), Quaternion.Euler(-90, 0, 0), matNube, 150, p =>
        {
            var m = p.main; m.loop = true; m.duration = 4f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            m.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startColor = new Color(1f, 1f, 1f, 0.6f);
            var e = p.emission; e.rateOverTime = 40f;
            var f = p.shape; f.shapeType = ParticleSystemShapeType.Circle; f.radius = 5f;
            var tam = p.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 2f));
            var col = p.colorOverLifetime; col.enabled = true; col.color = Degradado(Color.white, Color.white, 0.6f);
        });
        c.estela = Particulas("EstelaAscenso", t, Vector3.zero, Quaternion.Euler(90, 0, 0), matNube, 380, p =>
        {
            var m = p.main; m.loop = true; m.duration = 5f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startColor = new Color(0.95f, 0.93f, 0.9f, 0.7f);
            var e = p.emission; e.rateOverTime = 70f;
            var f = p.shape; f.shapeType = ParticleSystemShapeType.Cone; f.angle = 8f; f.radius = 0.6f;
            var tam = p.sizeOverLifetime; tam.enabled = true; tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 3f));
            var col = p.colorOverLifetime; col.enabled = true; col.color = Degradado(new Color(1f, 0.85f, 0.65f), new Color(0.95f, 0.95f, 0.95f), 0.7f);
        });
        c.aves = Particulas("Aves", t, new Vector3(-30f, 7f, 30f), Quaternion.Euler(-60, 40, 0), matAve, 14, p =>
        {
            var m = p.main; m.loop = false; m.duration = 0.6f;
            m.startLifetime = 8f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            m.startColor = new Color(0.08f, 0.09f, 0.1f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = p.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var f = p.shape; f.shapeType = ParticleSystemShapeType.Cone; f.angle = 35f; f.radius = 4f;
            // Aleteo: la altura de la silueta sube y baja varias veces.
            var tam = p.sizeOverLifetime; tam.enabled = true; tam.separateAxes = true;
            var aleteo = new AnimationCurve();
            for (int k = 0; k <= 24; k++) aleteo.AddKey(k / 24f, k % 2 == 0 ? 1f : 0.35f);
            tam.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
            tam.y = new ParticleSystem.MinMaxCurve(1f, aleteo);
            tam.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
        });

        // ----- Nubes bajas (bocanadas grandes que el cohete atraviesa al subir) -----
        Particulas("NubesBajas", t, new Vector3(0, 95f, 0), Quaternion.identity, matNube, 40, p =>
        {
            var m = p.main; m.loop = false; m.duration = 1f; m.playOnAwake = true;
            m.startLifetime = 100000f;
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(40f, 75f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startColor = new Color(1f, 1f, 1f, 0.8f);
            var e = p.emission; e.rateOverTime = 0f; e.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var f = p.shape; f.shapeType = ParticleSystemShapeType.Box; f.scale = new Vector3(220f, 18f, 220f);
        });
        return c;
    }

    static Gradient Degradado(Color inicio, Color fin, float alfaMax)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(inicio, 0f), new GradientColorKey(fin, 0.3f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alfaMax, 0.12f), new GradientAlphaKey(alfaMax * 0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    /// <summary>Material de partículas con transparencia normal (humo, nubes, aves).</summary>
    static Material MaterialParticulas(string nombre, Texture textura, bool aditivo)
    {
        var ruta = "Assets/Materiales/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.mainTexture = textura;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static ParticleSystem Particulas(string nombre, Transform padre, Vector3 posicion, Quaternion giro, Material material, int maximo,
        System.Action<ParticleSystem> configurar)
    {
        var go = new GameObject(nombre, typeof(ParticleSystem));
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = giro;
        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.playOnAwake = false;
        m.maxParticles = maximo;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        configurar(ps);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    /// <summary>Árboles de pocos polígonos (pinos y frondosos) unidos en 3 mallas: troncos, copas oscuras y claras.</summary>
    static void ConstruirArboles(Transform padre, System.Random azar)
    {
        var troncos = new MallaPlana(); var oscuras = new MallaPlana(); var claras = new MallaPlana();
        var lugares = new List<(Vector3 p, float escala)>();
        int intentos = 0;
        while (lugares.Count < 240 && intentos++ < 5000)
        {
            float a = (float)(azar.NextDouble() * Mathf.PI * 2);
            float r = 30f + (float)azar.NextDouble() * 150f;
            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (p.z > 118f) continue;                                  // playa y mar
            if (Mathf.Abs(p.x) < 8f && p.z < -16f) continue;           // camino
            if (p.x < -14f && p.x > -34f && p.z > 8f && p.z < 28f) continue;   // torre de agua
            lugares.Add((p, 0.8f + (float)azar.NextDouble() * 0.7f));
        }
        // Grupos en primer plano para las tomas (plano general y «desde los árboles»).
        foreach (var centro in new[] { new Vector3(-36f, 0f, -52f), new Vector3(26f, 0f, -42f) })
            for (int i = 0; i < 9; i++)
                lugares.Add((centro + new Vector3((float)azar.NextDouble() * 10f - 5f, 0f, (float)azar.NextDouble() * 8f - 4f), 1f + (float)azar.NextDouble() * 0.5f));

        foreach (var (p, e) in lugares)
        {
            bool pino = azar.NextDouble() < 0.55;
            float giro = (float)azar.NextDouble() * 360f;
            var copa = azar.NextDouble() < 0.5 ? oscuras : claras;
            troncos.Prisma(p, 0.18f * e, 1.6f * e, 6, giro);
            if (pino)
            {
                for (int k = 0; k < 3; k++)
                    copa.Cono(p + Vector3.up * (1.2f + k * 1.25f) * e, (1.9f - k * 0.5f) * e, 2.2f * e, 7, giro + k * 20f);
            }
            else
                copa.Bola(p + Vector3.up * 2.6f * e, 1.7f * e, giro);
        }
        var matTronco = CrearMaterial("Arbol_Tronco", Hex("#5A4232"), 0f, 0.15f);
        var matOscura = CrearMaterial("Arbol_CopaOscura", Hex("#2F5A33"), 0f, 0.12f);
        var matClara = CrearMaterial("Arbol_CopaClara", Hex("#4E7A3A"), 0f, 0.12f);
        foreach (var (malla, nombre, mat) in new[] { (troncos, "Arboles_Troncos", matTronco), (oscuras, "Arboles_CopasOscuras", matOscura), (claras, "Arboles_CopasClaras", matClara) })
        {
            var m = GuardarMalla(malla.Crear(), nombre);
            var go = Malla(m, nombre, padre, Vector3.zero, Vector3.zero, Vector3.one, mat);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        }
    }

    /// <summary>Malla de caras planas (normales por cara: aspecto de pocos polígonos).</summary>
    class MallaPlana
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<int> t = new List<int>();

        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(normal); n.Add(normal); n.Add(normal);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public void Prisma(Vector3 baseP, float radio, float alto, int lados, float giro)
        {
            for (int i = 0; i < lados; i++)
            {
                float a0 = (giro + i * 360f / lados) * Mathf.Deg2Rad, a1 = (giro + (i + 1) * 360f / lados) * Mathf.Deg2Rad;
                var p0 = baseP + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radio;
                var p1 = baseP + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radio;
                var arriba = Vector3.up * alto;
                Tri(p0, p0 + arriba, p1);
                Tri(p1, p0 + arriba, p1 + arriba);
            }
        }

        public void Cono(Vector3 baseP, float radio, float alto, int lados, float giro)
        {
            var punta = baseP + Vector3.up * alto;
            for (int i = 0; i < lados; i++)
            {
                float a0 = (giro + i * 360f / lados) * Mathf.Deg2Rad, a1 = (giro + (i + 1) * 360f / lados) * Mathf.Deg2Rad;
                var p0 = baseP + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radio;
                var p1 = baseP + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radio;
                Tri(p0, punta, p1);
                Tri(p1, baseP, p0);
            }
        }

        /// <summary>Copa redonda: octaedro subdividido una vez (32 caras), algo aplastado.</summary>
        public void Bola(Vector3 centro, float radio, float giro)
        {
            var q = Quaternion.Euler(0, giro, 0);
            var ejes = new[] { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
            for (int i = 0; i < 4; i++)
                foreach (var polo in new[] { Vector3.up, Vector3.down })
                {
                    var a = ejes[i]; var b = ejes[(i + 1) % 4]; var c = polo;
                    var ab = (a + b).normalized; var bc = (b + c).normalized; var ca = (c + a).normalized;
                    foreach (var (x, y, z) in new[] { (a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca) })
                    {
                        Vector3 P(Vector3 d) => centro + q * Vector3.Scale(d, new Vector3(1f, 0.82f, 1f)) * radio;
                        if (polo == Vector3.up) Tri(P(x), P(z), P(y)); else Tri(P(x), P(y), P(z));
                    }
                }
        }

        public Mesh Crear()
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Malla de llama: perfil que se abre un poco y se afila; v = 0 en la tobera y 1 en la punta (hacia −Y).</summary>
    static Mesh MallaLlama()
    {
        const int anillos = 14, segmentos = 18;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= anillos; i++)
        {
            float k = i / (float)anillos;
            float r = (1f + 0.5f * Mathf.Sin(Mathf.PI * k * 0.75f)) * (1f - k * k * 0.85f);
            for (int s = 0; s <= segmentos; s++)
            {
                float a = s * Mathf.PI * 2f / segmentos;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(dir * r * 0.5f + Vector3.down * k);
                n.Add(dir);
                uv.Add(new Vector2(s / (float)segmentos, k));
            }
        }
        for (int i = 0; i < anillos; i++)
            for (int s = 0; s < segmentos; s++)
            {
                int a = i * (segmentos + 1) + s, b = a + segmentos + 1;
                t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        var m = new Mesh();
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>
    /// Llamas de un cohete: una por tobera (posición local y radio de la boca), hijas de la pieza que las lleva
    /// (así los propulsores se van con su fuego). Apagadas al empezar. Devuelve los renderers.
    /// </summary>
    static List<Renderer> ConstruirLlamas(IEnumerable<(Transform pieza, Vector3 posicion, float radio, float largo)> toberas)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Llama_Motor.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("OperacionEclipseRojo/Llama"));
            AssetDatabase.CreateAsset(mat, "Assets/Materiales/Llama_Motor.mat");
        }
        mat.shader = Shader.Find("OperacionEclipseRojo/Llama");
        mat.SetColor("_ColorNucleo", new Color(6f, 4.2f, 2f, 1f));
        mat.SetColor("_ColorBorde", new Color(5f, 1.5f, 0.22f, 1f));
        mat.SetFloat("_Diamantes", 0.7f);
        EditorUtility.SetDirty(mat);
        var malla = GuardarMalla(MallaLlama(), "Llama_Motor");
        var lista = new List<Renderer>();
        int i = 0;
        foreach (var (pieza, posicion, radio, largo) in toberas)
        {
            var go = Malla(malla, "LlamaMotor" + i++, pieza, Vector3.zero, Vector3.zero, Vector3.one, mat);
            // Tamaño en el mundo: boca «radio» y largo «largo», aunque la pieza esté escalada.
            go.transform.localPosition = posicion;
            go.transform.rotation = pieza.root.rotation;
            var l = pieza.lossyScale;
            // La boca de la llama es algo más ancha que la tobera (el chorro se abre al salir).
            go.transform.localScale = new Vector3(radio * 3.2f / l.x, largo / l.y, radio * 3.2f / l.z);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            lista.Add(r);
            // Resplandor en la boca del motor (sprite aditivo que mira a la cámara en el cuadro: billboard simple).
            var brillo = new GameObject("BrilloMotor" + i, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            brillo.transform.SetParent(pieza, false);
            brillo.transform.localPosition = posicion;
            brillo.transform.localScale = new Vector3(radio * 9f / l.x, radio * 9f / l.y, radio * 9f / l.z);
            brillo.sprite = ConstructorEstilo.Sprite("Resplandor");
            brillo.color = new Color(1f, 0.7f, 0.35f, 0.9f);
            brillo.sharedMaterial = MaterialAditivo("Brillo_Motor", null, new Color(2.5f, 1.6f, 0.8f, 1f));
            brillo.gameObject.AddComponent<MirarCamara>();
            brillo.gameObject.SetActive(false);
            lista.Add(brillo);
        }
        return lista;
    }

    /// <summary>
    /// Separa la etapa inferior del SLS en núcleo y dos propulsores laterales, por piezas conectadas (los
    /// propulsores tocan el núcleo en X, así que un corte recto no sirve). Cada propulsor queda en su propio
    /// objeto con el pivote en su centro (la cinemática los suelta). Devuelve (izquierdo, derecho).
    /// </summary>
    static (Transform izquierdo, Transform derecho) DividirSls(GameObject inferior)
    {
        var izq = new GameObject("PropulsorIzq").transform;
        var der = new GameObject("PropulsorDer").transform;
        izq.SetParent(inferior.transform, false);
        der.SetParent(inferior.transform, false);
        var centros = new Dictionary<Transform, (Vector3 suma, int n)> { [izq] = (Vector3.zero, 0), [der] = (Vector3.zero, 0) };
        var nuevos = new List<(Transform destino, Mesh malla, Material[] mats, Transform origen)>();

        foreach (var mf in inferior.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.transform.IsChildOf(izq) || mf.transform.IsChildOf(der)) continue;
            var malla = mf.sharedMesh;
            var vs = malla.vertices;
            // Unión de vértices por posición (el OBJ separa vértices por normal).
            var padres = new int[vs.Length];
            for (int i = 0; i < vs.Length; i++) padres[i] = i;
            int Raiz(int x) { while (padres[x] != x) { padres[x] = padres[padres[x]]; x = padres[x]; } return x; }
            void Unir(int a, int b) { a = Raiz(a); b = Raiz(b); if (a != b) padres[a] = b; }
            var porPosicion = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vs.Length; i++)
            {
                var clave = Vector3Int.RoundToInt(vs[i] * 20000f);
                if (porPosicion.TryGetValue(clave, out var otro)) Unir(i, otro); else porPosicion[clave] = i;
            }
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var tri = malla.GetTriangles(s);
                for (int k = 0; k < tri.Length; k += 3) { Unir(tri[k], tri[k + 1]); Unir(tri[k + 1], tri[k + 2]); }
            }
            // Centro de cada pieza conectada → lado (en el espacio de la etapa).
            var suma = new Dictionary<int, (Vector3 s, int n)>();
            for (int i = 0; i < vs.Length; i++)
            {
                int r = Raiz(i);
                suma.TryGetValue(r, out var acc);
                suma[r] = (acc.s + vs[i], acc.n + 1);
            }
            int Lado(int vertice)
            {
                var (s, n) = suma[Raiz(vertice)];
                var c = inferior.transform.InverseTransformPoint(mf.transform.TransformPoint(s / n));
                return c.x < -0.105f ? -1 : c.x > 0.105f ? 1 : 0;
            }
            var partes = new Dictionary<int, List<int>[]>();
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var tri = malla.GetTriangles(s);
                for (int k = 0; k < tri.Length; k += 3)
                {
                    int lado = Lado(tri[k]);
                    if (!partes.TryGetValue(lado, out var listas))
                    {
                        listas = new List<int>[malla.subMeshCount];
                        for (int q = 0; q < listas.Length; q++) listas[q] = new List<int>();
                        partes[lado] = listas;
                    }
                    listas[s].Add(tri[k]); listas[s].Add(tri[k + 1]); listas[s].Add(tri[k + 2]);
                }
            }
            if (!partes.ContainsKey(-1) && !partes.ContainsKey(1)) continue;
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            foreach (var par in partes)
            {
                // Solo los vértices que usa esta parte (la malla original tiene los del cohete entero).
                var normales = malla.normals; var uvs = malla.uv;
                var mapa = new Dictionary<int, int>();
                var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>();
                var listas = new List<int>[malla.subMeshCount];
                for (int s = 0; s < malla.subMeshCount; s++)
                {
                    listas[s] = new List<int>(par.Value[s].Count);
                    foreach (var idx in par.Value[s])
                    {
                        if (!mapa.TryGetValue(idx, out var nuevo))
                        {
                            nuevo = nv.Count;
                            mapa[idx] = nuevo;
                            nv.Add(vs[idx]);
                            if (normales.Length > 0) nn.Add(normales[idx]);
                            if (uvs.Length > 0) nu.Add(uvs[idx]);
                        }
                        listas[s].Add(nuevo);
                    }
                }
                var m = new Mesh { indexFormat = nv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                m.SetVertices(nv);
                if (nn.Count == nv.Count) m.SetNormals(nn);
                if (nu.Count == nv.Count) m.SetUVs(0, nu);
                m.subMeshCount = malla.subMeshCount;
                for (int s = 0; s < malla.subMeshCount; s++) m.SetTriangles(listas[s], s);
                m.RecalculateBounds();
                var destino = par.Key < 0 ? izq : par.Key > 0 ? der : null;
                nuevos.Add((destino, GuardarMalla(m, "SLS_" + mf.name + "_" + (par.Key < 0 ? "Izq" : par.Key > 0 ? "Der" : "Nucleo")), mats, mf.transform));
                if (destino != null)
                {
                    var b = m.bounds;
                    var c = inferior.transform.InverseTransformPoint(mf.transform.TransformPoint(b.center));
                    var acc = centros[destino];
                    centros[destino] = (acc.suma + c, acc.n + 1);
                }
            }
            Object.DestroyImmediate(mf.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(mf);
        }
        // Pivote de cada propulsor en su centro (la separación los gira alrededor de sí mismos).
        foreach (var lado in new[] { izq, der })
            if (centros[lado].n > 0) lado.localPosition = centros[lado].suma / centros[lado].n;
        foreach (var (destino, malla, mats, origen) in nuevos)
        {
            var go = new GameObject(malla.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(destino != null ? destino : origen, false);
            go.transform.SetPositionAndRotation(origen.position, origen.rotation);
            go.transform.localScale = Vector3.one;
            var l = go.transform.lossyScale; var o = origen.lossyScale;
            go.transform.localScale = new Vector3(o.x / l.x, o.y / l.y, o.z / l.z);
            go.GetComponent<MeshFilter>().sharedMesh = malla;
            go.GetComponent<MeshRenderer>().sharedMaterials = mats;
        }
        return (izq, der);
    }

    /// <summary>
    /// Nariz del SLS (de la sección del módulo de servicio hacia arriba, y ≥ 0.60 del OBJ) partida en dos mitades por
    /// el signo de X, cada una con su bisagra en el borde inferior y material de doble cara. Lo de abajo queda como
    /// etapa superior. La cinemática abre las mitades para liberar el satélite.
    /// </summary>
    static void DividirNarizSls(GameObject superior)
    {
        const float corte = 0.60f;   // en el espacio del OBJ (lado mayor = 2, centrado)
        var mitades = new Dictionary<int, Transform>();
        foreach (int lado in new[] { -1, 1 })
        {
            var t = new GameObject(lado < 0 ? "NarizIzq" : "NarizDer").transform;
            t.SetParent(superior.transform, false);
            mitades[lado] = t;
        }
        var bordes = new Dictionary<int, Bounds>();
        var nuevas = new List<(int lado, Mesh malla, Material[] mats, Transform origen)>();
        foreach (var mf in superior.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.transform.IsChildOf(mitades[-1]) || mf.transform.IsChildOf(mitades[1])) continue;
            var malla = mf.sharedMesh;
            var vs = malla.vertices;
            var partes = new Dictionary<int, List<int>[]>();
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                var tri = malla.GetTriangles(s);
                for (int k = 0; k < tri.Length; k += 3)
                {
                    var c = (vs[tri[k]] + vs[tri[k + 1]] + vs[tri[k + 2]]) / 3f;
                    var cs = superior.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    int lado = cs.y < corte ? 0 : cs.x < 0f ? -1 : 1;
                    if (!partes.TryGetValue(lado, out var listas))
                    {
                        listas = new List<int>[malla.subMeshCount];
                        for (int q = 0; q < listas.Length; q++) listas[q] = new List<int>();
                        partes[lado] = listas;
                    }
                    listas[s].Add(tri[k]); listas[s].Add(tri[k + 1]); listas[s].Add(tri[k + 2]);
                }
            }
            if (!partes.ContainsKey(-1) && !partes.ContainsKey(1)) continue;
            var mats = mf.GetComponent<MeshRenderer>().sharedMaterials;
            foreach (var par in partes)
            {
                var m = Compactar(malla, par.Value);
                var nombre = "SLS_Superior_" + mf.name + "_" + (par.Key < 0 ? "NarizIzq" : par.Key > 0 ? "NarizDer" : "Etapa");
                var guardada = GuardarMalla(m, nombre);
                var matsUsar = par.Key == 0 ? mats : System.Array.ConvertAll(mats, x => DobleCara(x, x.name + "_DobleCara"));
                nuevas.Add((par.Key, guardada, matsUsar, mf.transform));
            }
            Object.DestroyImmediate(mf.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(mf);
        }
        foreach (var (lado, malla, mats, origen) in nuevas)
        {
            var go = new GameObject(malla.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(lado == 0 ? origen : mitades[lado], false);
            go.transform.SetPositionAndRotation(origen.position, origen.rotation);
            go.transform.localScale = Vector3.one;
            var l = go.transform.lossyScale; var o = origen.lossyScale;
            go.transform.localScale = new Vector3(o.x / l.x, o.y / l.y, o.z / l.z);
            go.GetComponent<MeshFilter>().sharedMesh = malla;
            go.GetComponent<MeshRenderer>().sharedMaterials = mats;
        }
        // Bisagra de cada mitad en su borde inferior exterior (el pivote se mueve sin mover la malla).
        foreach (var par in mitades)
        {
            var hijos = new List<Transform>();
            foreach (Transform h in par.Value) hijos.Add(h);
            if (hijos.Count == 0) continue;
            var b = Limites(par.Value.gameObject);
            var bisagra = new Vector3(par.Key < 0 ? b.min.x : b.max.x, b.min.y, b.center.z);
            foreach (var h in hijos) h.SetParent(null, true);
            par.Value.position = bisagra;
            foreach (var h in hijos) h.SetParent(par.Value, true);
        }
    }

    /// <summary>Malla con solo los vértices que usan los triángulos dados (por submalla).</summary>
    static Mesh Compactar(Mesh malla, List<int>[] triangulos)
    {
        var vs = malla.vertices; var normales = malla.normals; var uvs = malla.uv;
        var mapa = new Dictionary<int, int>();
        var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>();
        var listas = new List<int>[triangulos.Length];
        for (int s = 0; s < triangulos.Length; s++)
        {
            listas[s] = new List<int>(triangulos[s].Count);
            foreach (var idx in triangulos[s])
            {
                if (!mapa.TryGetValue(idx, out var nuevo))
                {
                    nuevo = nv.Count;
                    mapa[idx] = nuevo;
                    nv.Add(vs[idx]);
                    if (normales.Length > 0) nn.Add(normales[idx]);
                    if (uvs.Length > 0) nu.Add(uvs[idx]);
                }
                listas[s].Add(nuevo);
            }
        }
        var m = new Mesh { indexFormat = nv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        m.SetVertices(nv);
        if (nn.Count == nv.Count) m.SetNormals(nn);
        if (nu.Count == nv.Count) m.SetUVs(0, nu);
        m.subMeshCount = triangulos.Length;
        for (int s = 0; s < triangulos.Length; s++) m.SetTriangles(listas[s], s);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Toberas (boca abajo) del SLS: piezas oscuras que llegan al pie del cohete, en el espacio de «pieza».</summary>
    static List<(Transform pieza, Vector3 posicion, float radio, float largo)> ToberasSls(Transform cohete, Transform nucleo, Transform izq, Transform der, float alto)
    {
        var lista = new List<(Transform, Vector3, float, float)>();
        // Propulsores: una tobera grande en el centro de la base de cada uno.
        foreach (var p in new[] { izq, der })
        {
            var b = Limites(p.gameObject);
            var abajo = new Vector3(b.center.x, b.min.y, b.center.z);
            lista.Add((p, p.InverseTransformPoint(abajo), 0.2f * alto / 11f, alto * 0.95f));
        }
        // Núcleo: cuatro motores alrededor del eje, en la base.
        var bn = Limites(nucleo.gameObject);
        float r = 0.17f * alto / 11f;
        foreach (var (x, z) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
        {
            var punto = new Vector3(cohete.position.x + x * r, bn.min.y + 0.05f, cohete.position.z + z * r);
            lista.Add((nucleo, nucleo.InverseTransformPoint(punto), 0.15f * alto / 11f, alto * 0.65f));
        }
        return lista;
    }
}
