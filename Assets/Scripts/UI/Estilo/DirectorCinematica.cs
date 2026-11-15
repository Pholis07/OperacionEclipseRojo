using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>
    /// Cinemática Tierra → Marte (~36 s, 10 tomas con cortes) al pulsar «Lanzar» con la misión válida:
    /// complejo de lanzamiento con árboles, mar y torre; encendido con chispas, una llama por motor, luz, humo,
    /// vapor y aves; despegue con sacudida; ascenso entre nubes hasta el negro del espacio; separación propia de
    /// cada cohete (Titán: propulsores laterales y después la etapa central; Explorador: los cuatro propulsores se
    /// abren en cruz) y las tomas de transferencia, inserción y órbita. Se ve encima de todo; la Operación ya está activa debajo desde el mismo cuadro (las pruebas y la
    /// lógica no esperan a la cinemática). Usa el lanzador, el satélite, los instrumentos y la órbita elegidos.
    /// «Omitir» (o Escape) lleva exactamente al mismo final: satélite en el ancla de su órbita, audio de la
    /// investigación y la vista de la Operación. El final se avisa una sola vez (AlTerminar).
    /// Con «Reducir movimiento» no se reproduce: se pasa directo al final.
    /// El sonido de los motores es dramatización: fuera de la atmósfera solo se oye como vibración de la nave.
    /// </summary>
    public class DirectorCinematica : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button botonLanzar;
        [SerializeField] Camera camara;
        [SerializeField] MusicaEscena musica;
        [SerializeField] AudioSource fuente;

        [Header("Pantalla")]
        [SerializeField] CanvasGroup velo;
        [SerializeField] UnityEngine.UI.RawImage pantalla;
        [SerializeField] CanvasGroup corte;
        [SerializeField] TMP_Text textoHud;
        [SerializeField] TMP_Text textoFase;
        [SerializeField] TMP_Text textoCuenta;
        [SerializeField] UnityEngine.UI.Button botonOmitir;

        [Header("Lanzamiento")]
        [SerializeField] Transform coheteExplorador;
        [SerializeField] Transform coheteTitan;
        [SerializeField] Transform superiorExplorador;
        [SerializeField] Transform superiorTitan;
        [SerializeField] GameObject[] soloEnTierra;
        [SerializeField] GameObject espacio;          // Tierra, estrellas: solo en las tomas en órbita terrestre
        [SerializeField] Transform puntoEspacio;

        [Header("Complejo de lanzamiento")]
        [SerializeField] Renderer cielo;                 // shader Cielo (_Noche: día → espacio)
        [SerializeField] Transform[] brazos;             // brazos de la torre (giran para retirarse)
        [SerializeField] Light luzMotores;
        [SerializeField] ParticleSystem chispas;
        [SerializeField] ParticleSystem[] humoLados;     // salen por los extremos del foso de llamas
        [SerializeField] ParticleSystem vapor;
        [SerializeField] ParticleSystem estela;          // columna de humo que deja el cohete al subir
        [SerializeField] ParticleSystem aves;
        [SerializeField] Renderer[] llamasExplorador;
        [SerializeField] Renderer[] llamasTitan;
        [SerializeField] Renderer[] llamaSuperiorExplorador;   // motor de la etapa superior (en el vacío, sin sonido)
        [SerializeField] Renderer[] llamaSuperiorTitan;
        [SerializeField] Transform[] propulsoresExplorador;    // los cuatro bloques laterales (se abren en cruz)
        [SerializeField] Transform[] propulsoresTitan;         // los dos propulsores laterales del SLS
        [SerializeField] AudioSource fuenteBucle;              // ambiente de la costa y rugido del motor (en bucle)
        [SerializeField] Transform[] cofiaExplorador;          // dos mitades con bisagra (se abren en órbita)
        [SerializeField] Transform[] cofiaTitan;
        [SerializeField] ParticleSystem pirotecnia;             // destello de los pernos al soltar la cofia
        [SerializeField] ParticleSystem gasFrio;                // pulsos de los propulsores de control (sin sonido)
        [SerializeField] CanvasGroup destello;                  // destello blanco al encender
        [SerializeField] Color colorNiebla = new Color(0.72f, 0.79f, 0.86f);

        [Header("Transferencia")]
        [SerializeField] GameObject mapaTransferencia;
        [SerializeField] Transform centroMapa;
        [SerializeField] Transform tierraMapa;
        [SerializeField] Transform marteMapa;
        [SerializeField] Transform marcadorMapa;
        [SerializeField] LineRenderer arco;
        [SerializeField] float radioTierraMapa = 10f;
        [SerializeField] float radioMarteMapa = 15.2f;

        [Header("Llegada")]
        [SerializeField] EscenarioOrbital escenario;
        [SerializeField] SpriteRenderer pulsoZona;
        [SerializeField] UnityEngine.UI.RawImage mapaOperacion;

        static readonly Color CieloDia = new Color(0.36f, 0.56f, 0.82f);
        static readonly Color Negro = new Color(0.01f, 0.012f, 0.02f);
        const int Tomas = 10;

        RenderTexture textura;
        Coroutine rutina;
        bool omitir, terminada;
        Transform cohete, superior;
        Vector3 posCohete, posSuperior;
        Quaternion rotCohete;
        Renderer[] llamas, llamaSuperior;
        float sacudida, potencia;
        Vector3 escalaSatelite = Vector3.zero;   // la toma 7 lo encoge para que quepa en la cofia
        MaterialPropertyBlock bloque;
        readonly System.Collections.Generic.List<(Transform t, Vector3 p, Quaternion r, bool activo)> poses =
            new System.Collections.Generic.List<(Transform, Vector3, Quaternion, bool)>();

        public bool Reproduciendo => rutina != null;

        /// <summary>Se avisa una sola vez por lanzamiento, al terminar u omitir.</summary>
        public event System.Action AlTerminar;

        void Awake()
        {
            Ocultar();
            if (botonOmitir != null) botonOmitir.onClick.AddListener(Omitir);
            if (botonLanzar != null)
                // «Lanzar» solo está habilitado con la misión válida (RevisionUI). Este aviso puede llegar antes que
                // el de RevisionUI (que se conecta al abrir la Revisión), así que no se mira EstadoJuego.Resultado.
                botonLanzar.onClick.AddListener(() => { if (botonLanzar.interactable) Reproducir(); });
        }

        void Update()
        {
            if (Reproduciendo && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Omitir();
            // Voces de control de misión en fila: una no empieza hasta que termina la anterior (no se enciman).
            if (fuenteVoz != null && colaVoz.Count > 0 && !fuenteVoz.isPlaying)
            {
                fuenteVoz.clip = colaVoz.Dequeue();
                fuenteVoz.volume = Preferencias.VolumenVoz;
                fuenteVoz.Play();
            }
        }

        static readonly ArteDatos.Efecto[] Voces =
            { ArteDatos.Efecto.VozEncendido, ArteDatos.Efecto.VozDespegue, ArteDatos.Efecto.VozSeparacion, ArteDatos.Efecto.VozOrbita };
        readonly System.Collections.Generic.Queue<AudioClip> colaVoz = new System.Collections.Generic.Queue<AudioClip>();
        AudioSource fuenteVoz;

        void CallarVoz()
        {
            colaVoz.Clear();
            if (fuenteVoz != null) fuenteVoz.Stop();
        }

        void OnDisable()
        {
            if (Reproduciendo) Final();
        }

        void OnDestroy()
        {
            if (textura != null) { textura.Release(); Destroy(textura); }
        }

        /// <summary>
        /// Saltar: va directo al final (que apaga sonidos, motor y voces). Antes la rutina recorría en un cuadro las
        /// tomas que faltaban y disparaba todos sus sonidos a la vez, que seguían oyéndose en la Operación.
        /// </summary>
        public void Omitir()
        {
            omitir = true;
            if (Reproduciendo) Final();
        }

        public void Reproducir()
        {
            if (Reproduciendo) return;
            terminada = false;
            omitir = false;
            // La escena de Configuración ya no se ve: su cámara deja de dibujar (rendimiento en el navegador).
            // Sigue encendida pero sin capas: así nunca queda la pantalla sin cámara («No cameras rendering» en el editor).
            if (Camera.main != null && Camera.main != camara)
            {
                Camera.main.cullingMask = 0;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }
            if (Preferencias.ReducirMovimiento || !Application.isPlaying) { Final(); return; }
            rutina = StartCoroutine(Secuencia());
        }

        // ---------- Secuencia ----------

        IEnumerator Secuencia()
        {
            var config = EstadoJuego.Configuracion;
            bool titan = config.LanzadorId == "titan";
            cohete = titan ? coheteTitan : coheteExplorador;
            superior = titan ? superiorTitan : superiorExplorador;
            llamas = titan ? llamasTitan : llamasExplorador;
            llamaSuperior = titan ? llamaSuperiorTitan : llamaSuperiorExplorador;
            var propulsores = titan ? propulsoresTitan : propulsoresExplorador;
            GuardarPoses(propulsores);
            if (escenario != null && escenario.Satelite != null) escalaSatelite = escenario.Satelite.localScale;
            if (coheteExplorador != null) coheteExplorador.gameObject.SetActive(!titan);
            if (coheteTitan != null) coheteTitan.gameObject.SetActive(titan);
            // El elegido, en el centro de la plataforma (bajo los brazos de la torre).
            cohete.localPosition = new Vector3(0f, cohete.localPosition.y, 0f);
            posCohete = cohete.position; rotCohete = cohete.rotation; posSuperior = superior.localPosition;
            float alto = titan ? 11f : 6.1f;
            escenario.Preparar(config);
            escenario.MostrarOrbita(false);
            PrepararComplejo(alto);

            PrepararCamara();
            velo.gameObject.SetActive(true);
            velo.alpha = 1f;
            velo.blocksRaycasts = true;
            corte.alpha = 1f;
            if (musica != null) musica.Cambiar(ArteDatos.Musica.Lanzamiento);
            Activar(true, false, false);
            Bucle(ArteDatos.Efecto.AmbienteCosta, 0.55f);
            var basePos = posCohete;

            // 1) Plano general del complejo: árboles delante, el mar detrás, cuenta atrás.
            Toma(1, "LAUNCH COMPLEX");
            Sonar(ArteDatos.Efecto.CuentaAtras);
            yield return Cortar(false);
            for (float t = 0f; t < 3.6f && !omitir; t += Time.deltaTime)
            {
                float k = t / 3.6f;
                textoCuenta.text = t < 1.2f ? "T-05" : t < 2.4f ? "T-04" : "T-03";
                var cam = basePos + Vector3.Lerp(new Vector3(-58f, 13f, -80f), new Vector3(-50f, 11.5f, -70f), k);
                Mirar(cam, basePos + Vector3.up * alto * 0.5f, 30f);
                yield return null;
            }

            // 2) Los motores de cerca: chispas, voz de control y encendido.
            yield return Cortar(true);
            Toma(2, "IGNITION SEQUENCE");
            Sonar(ArteDatos.Efecto.VozEncendido);
            bool chispazo = false, encendido = false;
            for (float t = 0f; t < 3.6f && !omitir; t += Time.deltaTime)
            {
                textoCuenta.text = t < 1.2f ? "T-02" : t < 2.4f ? "T-01" : "";
                if (!chispazo && t > 0.5f)
                {
                    chispazo = true;
                    if (chispas != null) { chispas.transform.position = basePos + Vector3.up * 0.2f; chispas.Play(); }
                    Sonar(ArteDatos.Efecto.Chispas);
                }
                if (!encendido && t > 1.7f)
                {
                    encendido = true;
                    Encender(true);
                    Sonar(ArteDatos.Efecto.Ignicion);
                    Bucle(ArteDatos.Efecto.MotorCercano, 0.8f);
                    sacudida = 0.55f;
                    StartCoroutine(Destellar());
                    foreach (var h in humoLados) if (h != null) h.Play();
                    if (vapor != null) vapor.Play();
                    if (aves != null) aves.Play();
                }
                potencia = encendido ? Mathf.MoveTowards(potencia, 1f, Time.deltaTime * 1.6f) : 0f;
                var cam = basePos + new Vector3(-alto * 0.62f, alto * 0.09f, -alto * 0.95f) + Vector3.forward * t * 0.08f * alto;
                Mirar(cam, basePos + Vector3.up * alto * 0.16f, 42f);
                yield return null;
            }
            textoCuenta.text = "";

            // 3) Los brazos de la torre se retiran y el cohete despega, visto desde abajo.
            yield return Cortar(false);
            Toma(3, "LIFTOFF!");
            Sonar(ArteDatos.Efecto.BrazosTorre);
            bool voz = false;
            float subida = 0f;
            var camSuelo = basePos + new Vector3(alto * 0.75f, 0.5f, -alto * 1.45f);
            for (float t = 0f; t < 4f && !omitir; t += Time.deltaTime)
            {
                float giro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.1f));
                for (int i = 0; i < brazos.Length; i++) if (brazos[i] != null) brazos[i].localRotation = Quaternion.Euler(0f, -75f * giro, 0f);
                if (t > 1.1f)
                {
                    float u = t - 1.1f;
                    subida = 0.5f * 0.55f * alto * u * u;
                    if (!voz) { voz = true; Sonar(ArteDatos.Efecto.VozDespegue); sacudida = Mathf.Max(sacudida, 0.4f); }
                }
                cohete.position = basePos + Vector3.up * subida;
                Mirar(camSuelo, cohete.position + Vector3.up * alto * 0.55f, 52f);
                yield return null;
            }

            // 4) Desde detrás de los árboles: sube dejando su columna de humo.
            yield return Cortar(true);
            Toma(4, "ASCENT");
            if (estela != null) estela.Play();
            var camArboles = basePos + new Vector3(31f, 4.2f, -52f);
            float inicio4 = subida;
            for (float t = 0f; t < 3.6f && !omitir; t += Time.deltaTime)
            {
                subida = inicio4 + alto * (1.2f * t + 0.5f * t * t);
                cohete.position = basePos + Vector3.up * subida;
                Mirar(camArboles, Vector3.Lerp(basePos + Vector3.up * alto * 2.5f, cohete.position + Vector3.up * alto * 0.5f, 0.7f), 44f);
                yield return null;
            }

            // 5) Entre las nubes: el cielo pasa de azul al negro del espacio; el cohete empieza a inclinarse.
            yield return Cortar(true);
            Toma(5, "THROUGH THE CLOUDS");
            for (float t = 0f; t < 3.6f && !omitir; t += Time.deltaTime)
            {
                float k = t / 3.6f;
                cohete.position = basePos + Vector3.up * Mathf.Lerp(55f, 175f, k * (0.6f + 0.4f * k));
                cohete.rotation = rotCohete * Quaternion.Euler(0f, 0f, -14f * k);
                Noche(Mathf.SmoothStep(0f, 0.85f, k));
                var cam = cohete.position + new Vector3(-alto * 1.7f, -alto * 1.3f, -alto * 1.5f);
                Mirar(cam, cohete.position + cohete.up * alto * 0.7f, 50f);
                yield return null;
            }

            // 6) En órbita terrestre: separación propia de cada cohete (en el vacío no suena el motor).
            yield return Cortar(true);
            Toma(6, "STAGE SEPARATION");
            Encender(false);
            DetenerBucle();
            Activar(false, true, false);
            camara.backgroundColor = Negro;
            cohete.position = puntoEspacio.position;
            cohete.rotation = puntoEspacio.rotation;   // eje del cohete (arriba) = dirección de avance
            var adelante = cohete.up;
            var lado = cohete.right;
            var desfaseSuperior = superior.position - cohete.position;
            var rotSuperior = superior.rotation;
            var desfases = new Vector3[propulsores.Length];
            var giros = new Quaternion[propulsores.Length];
            for (int i = 0; i < propulsores.Length; i++)
            {
                desfases[i] = propulsores[i].position - cohete.position;
                giros[i] = propulsores[i].rotation;
            }
            bool fuera = false, separado = false;
            for (float t = 0f; t < 3.8f && !omitir; t += Time.deltaTime)
            {
                var avance = puntoEspacio.position + adelante * (t * alto * 0.22f);
                // Primero se sueltan los propulsores laterales y se alejan girando.
                if (!fuera && t > 0.5f) { fuera = true; Sonar(ArteDatos.Efecto.SeparacionPropulsores); }
                float sp = fuera ? t - 0.5f : 0f;
                // Después la etapa central se queda atrás y la superior sigue con su motor (sin sonido).
                if (!separado && t > 1.9f)
                {
                    separado = true;
                    Sonar(ArteDatos.Efecto.Separacion);
                    Sonar(ArteDatos.Efecto.VozSeparacion);
                    EncenderSuperior(true);
                }
                float s2 = separado ? t - 1.9f : 0f;
                cohete.position = avance - adelante * s2 * s2 * alto * 0.35f;
                cohete.rotation = puntoEspacio.rotation * Quaternion.Euler(s2 * 9f, 0f, s2 * 6f);
                superior.position = avance + desfaseSuperior + adelante * s2 * alto * 0.14f;
                superior.rotation = rotSuperior;
                // Los propulsores son hijos del cohete: su pose se fija después de mover el cohete.
                for (int i = 0; i < propulsores.Length; i++)
                {
                    var p = propulsores[i];
                    if (p == null) continue;
                    var radial = Vector3.ProjectOnPlane(desfases[i], adelante);
                    radial = radial.sqrMagnitude < 0.0001f ? lado : radial.normalized;
                    if (titan)
                        p.position = avance + desfases[i] + radial * sp * sp * alto * 0.12f - adelante * sp * sp * alto * 0.18f;
                    else   // Explorador: se abren en cruz, girando hacia fuera desde la base
                        p.position = avance + desfases[i] + radial * sp * alto * 0.22f - adelante * sp * sp * alto * 0.12f;
                    p.rotation = Quaternion.AngleAxis((titan ? 18f : 55f) * sp, Vector3.Cross(adelante, radial)) * giros[i];
                }
                var centro = avance + adelante * alto * 0.5f;
                Mirar(centro + Vector3.up * alto * 0.35f + Vector3.back * alto * 1.5f, centro + Vector3.down * alto * 0.1f, 46f);
                yield return null;
            }

            // 7) La cofia se abre y el satélite se separa: pernos, mitades que se alejan girando, resortes que
            //    empujan la carga y la etapa superior que se aparta con pulsos de gas frío (en el vacío, sin motor).
            Toma(7, "FAIRING SEPARATION · PAYLOAD RELEASE");
            EncenderSuperior(false);
            var cofia = titan ? cofiaTitan : cofiaExplorador;
            var sat = escenario.Satelite;
            var escalaSat = sat.localScale;
            var nariz = Vector3.zero;
            foreach (var c in cofia) nariz += CentroVisual(c) / cofia.Length;
            var radioNariz = alto * (titan ? 0.028f : 0.04f);
            // El satélite viaja dentro de la cofia, a una escala que cabe en ella.
            sat.localScale = escalaSat;
            float tamano = Mathf.Max(0.01f, TamanoVisual(sat));
            sat.localScale = escalaSat * (radioNariz * 1.5f / tamano);
            var poseSuperior = superior.position;
            var giroSuperior = superior.rotation;
            var desfaseNariz = nariz - poseSuperior;
            var giroBisagra = new Quaternion[cofia.Length];
            for (int i = 0; i < cofia.Length; i++) giroBisagra[i] = cofia[i].rotation;
            var ejes = new Vector3[cofia.Length];
            var haciaFuera = new Vector3[cofia.Length];
            for (int i = 0; i < cofia.Length; i++)
            {
                haciaFuera[i] = Vector3.ProjectOnPlane(CentroVisual(cofia[i]) - nariz, adelante).normalized;
                if (haciaFuera[i].sqrMagnitude < 0.01f) haciaFuera[i] = i == 0 ? lado : -lado;
                ejes[i] = Vector3.Cross(adelante, haciaFuera[i]);
            }
            var bisagras = new Vector3[cofia.Length];
            for (int i = 0; i < cofia.Length; i++) bisagras[i] = cofia[i].position;
            bool pernos = false, resortes = false, gas = false, estable = false;
            // Cámara del lado iluminado por el Sol (si no, la nariz se ve azul, solo con la luz de contorno).
            var haciaSol = RenderSettings.sun != null ? -RenderSettings.sun.transform.forward : Vector3.back;
            var lateral = Vector3.ProjectOnPlane(haciaSol, adelante).normalized;
            if (lateral.sqrMagnitude < 0.01f) lateral = Vector3.Cross(adelante, Vector3.up).normalized;
            for (float t = 0f; t < 5.4f && !omitir; t += Time.deltaTime)
            {
                // La etapa (con el satélite dentro) sigue avanzando despacio por inercia.
                var avance = adelante * t * alto * 0.05f;
                float frena = resortes ? (t - 2.1f) : 0f;
                superior.position = poseSuperior + avance - adelante * frena * frena * alto * 0.035f;
                superior.rotation = Quaternion.AngleAxis(frena * 7f, lateral) * giroSuperior;
                var centroNariz = poseSuperior + avance + desfaseNariz;

                if (!pernos && t > 0.7f)
                {
                    pernos = true;
                    Sonar(ArteDatos.Efecto.Separacion, 0.6f);
                    if (pirotecnia != null) { pirotecnia.transform.position = centroNariz; pirotecnia.Play(); }
                }
                // Mitades de la cofia: giran sobre su bisagra y, ya abiertas, se alejan dando vueltas.
                float abre = pernos ? Mathf.Clamp01((t - 0.7f) / 1.1f) : 0f;
                float suelta = pernos ? Mathf.Max(0f, t - 1.5f) : 0f;
                for (int i = 0; i < cofia.Length; i++)
                {
                    var c = cofia[i];
                    if (c == null) continue;
                    var posMitad = bisagras[i] + avance + haciaFuera[i] * suelta * alto * 0.09f - adelante * suelta * suelta * alto * 0.01f;
                    c.position = posMitad;
                    c.rotation = Quaternion.AngleAxis(Animar.SalidaCubica(abre) * 38f + suelta * 30f, ejes[i]) * giroBisagra[i];
                }
                // El satélite: dentro, quieto respecto a la etapa; luego los resortes lo empujan hacia delante.
                if (!resortes && t > 2.1f) { resortes = true; Sonar(ArteDatos.Efecto.Servo, 0.5f); }
                float empuje = resortes ? t - 2.1f : 0f;
                sat.position = centroNariz + adelante * (empuje * alto * 0.11f);
                sat.rotation = Quaternion.LookRotation(adelante, Vector3.up) * Quaternion.Euler(0f, 0f, empuje * 9f);
                if (!gas && t > 2.7f)
                {
                    gas = true;   // la etapa se aparta con pulsos de gas frío
                    if (gasFrio != null) { gasFrio.transform.position = centroNariz - adelante * alto * 0.15f; gasFrio.Play(); }
                }
                if (!estable && t > 3.8f)
                {
                    estable = true;   // el satélite se estabiliza con sus propios propulsores
                    Sonar(ArteDatos.Efecto.Despliegue);
                    textoHud.text = "PAYLOAD RELEASED";
                    if (gasFrio != null) { gasFrio.transform.position = sat.position; gasFrio.Play(); }
                }
                // Cámara de lado, acercándose a la nariz y siguiendo luego al satélite.
                var mira = Vector3.Lerp(centroNariz, sat.position, Mathf.Clamp01((t - 2f) / 2.5f));
                // Cerca, a la escala de la nariz: el satélite (pequeño, cabía en la cofia) llena buena parte del cuadro.
                var dist = radioNariz * Mathf.Lerp(10f, 4.2f, Mathf.Clamp01(t / 5.4f));
                Mirar(mira + lateral * dist + Vector3.up * dist * 0.35f - adelante * dist * 0.4f, mira, 40f);
                yield return null;
            }
            sat.localScale = escalaSat;

            // 8) Transferencia Tierra → Marte en un mapa (tiempo acelerado, sin cifras).
            yield return Cortar(true);
            Toma(8, "INTERPLANETARY TRANSFER · TIME LAPSE");
            Activar(false, false, true);
            Sonar(ArteDatos.Efecto.Transferencia);
            yield return Transferencia(4.5f);

            // 9) Aproximación a Marte e inserción orbital.
            yield return Cortar(true);
            Toma(9, "ORBIT INSERTION");
            Activar(false, false, false);
            sat.localScale = escalaSat;
            float fFinal = escenario.FaseAncla;
            float fEntrada = fFinal - 1.1f;
            var (rad, tan, _) = escenario.Marco(fEntrada);
            var entrada = escenario.PosicionEnOrbita(fEntrada);
            var lejos = entrada - tan * 70f + rad * 25f;
            Sonar(ArteDatos.Efecto.Propulsores);
            for (float t = 0f; t < 3.5f && !omitir; t += Time.deltaTime)
            {
                float k = Animar.SalidaCubica(t / 3.5f);
                sat.position = Vector3.Lerp(lejos, entrada, k);
                sat.rotation = Quaternion.LookRotation((sat.position - escenario.Centro).normalized, tan);
                if (t > 2.6f) escenario.MostrarOrbita(true, Mathf.Clamp01((t - 2.6f) / 0.9f) * 0.85f);
                var cam = sat.position - tan * 9f + rad * 3.5f;
                Mirar(cam, Vector3.Lerp(sat.position, escenario.Centro, 0.25f), Mathf.Lerp(55f, 48f, k));
                yield return null;
            }

            // 10) Plano héroe en la órbita elegida; la cámara llega al encuadre de la investigación.
            Toma(10, "ORBIT ESTABLISHED · ANOMALY LOCATED");
            escenario.MostrarOrbita(true, 0.85f);
            Sonar(ArteDatos.Efecto.DatosRecibidos, 0.8f);
            Sonar(ArteDatos.Efecto.VozOrbita);
            MostrarZona(true);
            var (pa, ra) = escenario.PoseCamara(fFinal);
            var inicioCam = camara.transform.position;
            var inicioRot = camara.transform.rotation;
            float inicioFov = camara.fieldOfView;
            for (float t = 0f; t < 3f && !omitir; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 3f);
                escenario.ColocarSatelite(Mathf.Lerp(fEntrada, fFinal, Animar.SalidaCubica(t / 3f)), null);
                camara.transform.SetPositionAndRotation(Vector3.Lerp(inicioCam, pa, k), Quaternion.Slerp(inicioRot, ra, k));
                camara.fieldOfView = Mathf.Lerp(inicioFov, escenario.CampoAncla, k);
                yield return null;
            }

            // Fundido a la Operación.
            textoHud.text = "";
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
            {
                velo.alpha = 1f - t;
                yield return null;
            }
            Final();
        }

        IEnumerator Transferencia(float duracion)
        {
            const int Puntos = 64;
            float a = (radioTierraMapa + radioMarteMapa) / 2f;
            float e = (radioMarteMapa - radioTierraMapa) / (radioMarteMapa + radioTierraMapa);
            Vector3 Elipse(float th) => centroMapa.position + new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th)) * (a * (1 - e * e) / (1 + e * Mathf.Cos(th)));
            Vector3 Circulo(float r, float th) => centroMapa.position + new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th)) * r;
            arco.positionCount = 0;
            var vista = centroMapa.position + new Vector3(-6f, 26f, -22f);
            for (float t = 0f; t < duracion && !omitir; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duracion);
                float th = Mathf.PI * k;
                // Transferencia de Hohmann (orientativa): la Tierra avanza ~255° y Marte ~136° durante el viaje.
                tierraMapa.position = Circulo(radioTierraMapa, k * 255f * Mathf.Deg2Rad);
                marteMapa.position = Circulo(radioMarteMapa, (44f + k * 136f) * Mathf.Deg2Rad);
                marcadorMapa.position = Elipse(th);
                int n = Mathf.Max(2, Mathf.CeilToInt(Puntos * k));
                arco.positionCount = n;
                for (int i = 0; i < n; i++) arco.SetPosition(i, Elipse(th * i / (n - 1)));
                Mirar(Vector3.Lerp(vista, vista + new Vector3(8f, -4f, 4f), k), centroMapa.position + new Vector3(2f, 0f, 2f), 45f);
                yield return null;
            }
        }

        // ---------- Piezas ----------

        /// <summary>Lado mayor de lo que se ve del objeto (en el mundo).</summary>
        static float TamanoVisual(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 1f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        }

        static Vector3 CentroVisual(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return t.position;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.center;
        }

        void Toma(int numero, string hud)
        {
            textoFase.text = "MISSION PHASE  " + numero + " / " + Tomas;
            textoHud.text = hud;
        }

        void Mirar(Vector3 posicion, Vector3 objetivo, float campo)
        {
            // Sacudida de la cámara al encender y al despegar (se apaga sola; nada con «Reducir movimiento»).
            if (sacudida > 0f)
            {
                float tt = Time.time * 28f;
                var ruido = new Vector3(Mathf.PerlinNoise(tt, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, tt) - 0.5f, Mathf.PerlinNoise(tt, tt * 0.5f) - 0.5f);
                posicion += ruido * sacudida * 0.9f;
                sacudida = Mathf.MoveTowards(sacudida, 0f, Time.deltaTime * 0.25f);
            }
            AnimarFuego();
            camara.transform.position = posicion;
            camara.transform.rotation = Quaternion.LookRotation(objetivo - posicion, Vector3.up);
            camara.fieldOfView = campo;
        }

        IEnumerator Cortar(bool fundirANegro)
        {
            if (fundirANegro)
                for (float t = 0f; t < 1f && !omitir; t += Time.deltaTime / 0.18f) { corte.alpha = t; yield return null; }
            corte.alpha = 1f;
            yield return null;
            for (float t = 0f; t < 1f && !omitir; t += Time.deltaTime / 0.22f) { corte.alpha = 1f - t; yield return null; }
            corte.alpha = 0f;
        }

        void Activar(bool tierraFirme, bool orbitaTerrestre, bool mapa)
        {
            if (soloEnTierra != null) foreach (var go in soloEnTierra) if (go != null) go.SetActive(tierraFirme);
            // Bruma lejana solo en el complejo de lanzamiento (colinas y mar); en el espacio no hay niebla.
            RenderSettings.fog = tierraFirme;
            // Sombras del cohete solo sobre la plataforma: en órbita serían manchas enormes sobre la Tierra.
            foreach (var c in new[] { coheteExplorador, coheteTitan })
                if (c != null)
                    foreach (var r in c.GetComponentsInChildren<MeshRenderer>(true))
                        if (!r.name.StartsWith("LlamaMotor") && !r.name.StartsWith("Logo"))
                            r.shadowCastingMode = tierraFirme ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (tierraFirme)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = colorNiebla;
                RenderSettings.fogStartDistance = 110f;
                RenderSettings.fogEndDistance = 620f;
            }
            if (espacio != null) espacio.SetActive(orbitaTerrestre);
            if (mapaTransferencia != null) mapaTransferencia.SetActive(mapa);
            if (camara == null) return;
            if (tierraFirme) camara.backgroundColor = CieloDia;
            else if (!orbitaTerrestre) camara.backgroundColor = Negro;
            // Cada escenario solo ve lo suyo (los demás quedan más allá del plano lejano o tras su cielo).
            camara.farClipPlane = tierraFirme ? 700f : mapa ? 140f : 600f;
        }

        void Encender(bool encendido)
        {
            potencia = 0f;
            Mostrar(llamas, encendido);
            if (luzMotores != null) luzMotores.enabled = encendido;
            if (!encendido)
            {
                foreach (var h in humoLados) if (h != null) h.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (vapor != null) vapor.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (estela != null) estela.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void EncenderSuperior(bool encendido) => Mostrar(llamaSuperior, encendido);

        static void Mostrar(Renderer[] rs, bool visible)
        {
            if (rs == null) return;
            foreach (var r in rs) if (r != null && r.gameObject.activeSelf != visible) r.gameObject.SetActive(visible);
        }

        /// <summary>Potencia de las llamas, parpadeo de la luz y la columna de humo que sigue al cohete.</summary>
        void AnimarFuego()
        {
            if (llamas == null || cohete == null) return;
            bloque ??= new MaterialPropertyBlock();
            bloque.SetFloat("_Potencia", potencia);
            foreach (var r in llamas) if (r != null && r.gameObject.activeSelf) r.SetPropertyBlock(bloque);
            if (luzMotores != null && luzMotores.enabled)
            {
                luzMotores.transform.position = cohete.position + Vector3.up * 0.5f;
                luzMotores.intensity = potencia * (9f + 4f * Mathf.PerlinNoise(Time.time * 14f, 0.5f));
            }
            if (estela != null) estela.transform.position = cohete.position;
        }

        IEnumerator Destellar()
        {
            if (destello == null) yield break;
            destello.alpha = 0.55f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) { destello.alpha = 0.55f * (1f - t); yield return null; }
            destello.alpha = 0f;
        }

        void Noche(float k)
        {
            if (cielo == null) return;
            bloque ??= new MaterialPropertyBlock();
            cielo.GetPropertyBlock(bloque);
            bloque.SetFloat("_Noche", k);
            cielo.SetPropertyBlock(bloque);
            // Al cambiar de escena (OnDisable → Final) la cámara puede estar ya destruida.
            if (camara != null) camara.backgroundColor = Color.Lerp(CieloDia, Negro, k);
        }

        void PrepararComplejo(float alto)
        {
            Noche(0f);
            sacudida = 0f;
            potencia = 0f;
            Mostrar(llamasExplorador, false);
            Mostrar(llamasTitan, false);
            Mostrar(llamaSuperiorExplorador, false);
            Mostrar(llamaSuperiorTitan, false);
            if (luzMotores != null) luzMotores.enabled = false;
            if (destello != null) destello.alpha = 0f;
            // Los brazos de la torre, a la altura del cohete elegido y pegados a él.
            for (int i = 0; i < brazos.Length; i++)
            {
                if (brazos[i] == null) continue;
                var p = brazos[i].localPosition;
                brazos[i].localPosition = new Vector3(p.x, alto * (0.3f + 0.22f * i), p.z);
                brazos[i].localRotation = Quaternion.identity;
            }
            foreach (var ps in new[] { chispas, vapor, estela, aves, pirotecnia, gasFrio })
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var h in humoLados) if (h != null) h.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Guarda la pose de todo lo que la cinemática mueve, para dejarlo igual al terminar u omitir.</summary>
        void GuardarPoses(Transform[] propulsores)
        {
            poses.Clear();
            void Guardar(Transform t) { if (t != null) poses.Add((t, t.localPosition, t.localRotation, t.gameObject.activeSelf)); }
            Guardar(coheteExplorador); Guardar(coheteTitan); Guardar(superiorExplorador); Guardar(superiorTitan);
            if (propulsoresExplorador != null) foreach (var p in propulsoresExplorador) Guardar(p);
            if (propulsoresTitan != null) foreach (var p in propulsoresTitan) Guardar(p);
            if (brazos != null) foreach (var b in brazos) Guardar(b);
            if (cofiaExplorador != null) foreach (var c in cofiaExplorador) Guardar(c);
            if (cofiaTitan != null) foreach (var c in cofiaTitan) Guardar(c);
        }

        void RestaurarPoses()
        {
            foreach (var (t, p, r, activo) in poses)
            {
                if (t == null) continue;
                t.localPosition = p;
                t.localRotation = r;
                t.gameObject.SetActive(activo);
            }
            poses.Clear();
        }

        void Bucle(ArteDatos.Efecto efecto, float volumen)
        {
            if (fuenteBucle == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(efecto);
            if (clip == null) return;
            fuenteBucle.clip = clip;
            fuenteBucle.loop = true;
            fuenteBucle.volume = volumen * Preferencias.VolumenEfectos;
            fuenteBucle.Play();
        }

        void DetenerBucle()
        {
            if (fuenteBucle != null) fuenteBucle.Stop();
        }

        void MostrarZona(bool visible)
        {
            if (pulsoZona == null) return;
            pulsoZona.gameObject.SetActive(visible);
            if (!visible) return;
            // Centro de la zona del mapa de la Operación (la región, no el punto exacto de la anomalía).
            var region = mapaOperacion != null ? mapaOperacion.GetComponent<MapaMarteUI>() : null;
            var r = region != null ? region.RectGlobo : mapaOperacion != null ? mapaOperacion.uvRect : new Rect(0.4f, 0.4f, 0.2f, 0.2f);
            var punto = escenario.AlinearRegion(r.center, escenario.FaseAncla + 0.35f);
            var normal = (punto - escenario.Centro).normalized;
            pulsoZona.transform.position = punto + normal * 0.05f;
            pulsoZona.transform.rotation = Quaternion.LookRotation(-normal);
        }

        void PrepararCamara()
        {
            int ancho = Mathf.Max(640, Screen.width), alto = Mathf.Max(360, Screen.height);
            if (textura == null || textura.width != ancho || textura.height != alto)
            {
                if (textura != null) { textura.Release(); Destroy(textura); }
                textura = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32) { name = "Cinematica", antiAliasing = 2 };
                textura.Create();
            }
            camara.targetTexture = textura;
            camara.aspect = (float)ancho / alto;
            camara.enabled = true;
            pantalla.texture = textura;
        }

        void Sonar(ArteDatos.Efecto efecto, float volumen = 1f)
        {
            if (omitir) return;
            if (fuente == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(efecto);
            if (clip == null) return;
            if (System.Array.IndexOf(Voces, efecto) >= 0)
            {
                if (fuenteVoz == null)
                {
                    fuenteVoz = fuente.gameObject.AddComponent<AudioSource>();
                    fuenteVoz.playOnAwake = false;
                    fuenteVoz.spatialBlend = 0f;
                    fuenteVoz.outputAudioMixerGroup = fuente.outputAudioMixerGroup;
                }
                colaVoz.Enqueue(clip);
                return;
            }
            fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }

        /// <summary>El mismo final al terminar o al omitir: satélite en su órbita, audio y vista de la Operación.</summary>
        void Final()
        {
            if (rutina != null) { StopCoroutine(rutina); rutina = null; }
            if (fuente != null) fuente.Stop();
            CallarVoz();
            DetenerBucle();
            if (llamas != null) Encender(false);
            EncenderSuperior(false);
            sacudida = 0f;
            if (destello != null) destello.alpha = 0f;
            foreach (var ps in new[] { chispas, vapor, estela, aves, pirotecnia, gasFrio })
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (humoLados != null) foreach (var h in humoLados) if (h != null) h.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RestaurarPoses();
            if (cielo != null) Noche(0f);
            if (escalaSatelite != Vector3.zero && escenario != null && escenario.Satelite != null)
                escenario.Satelite.localScale = escalaSatelite;
            MostrarZona(false);
            if (escenario != null)
            {
                escenario.Preparar(EstadoJuego.Configuracion);
                escenario.MostrarOrbita(true, 0.55f);
            }
            if (musica != null)
            {
                musica.Cambiar(ArteDatos.Musica.Investigacion);
                musica.CambiarAmbiente(ArteDatos.Ambiente.Orbital);   // interior de la nave en órbita
            }
            Ocultar();
            if (!terminada)
            {
                terminada = true;
                AlTerminar?.Invoke();
            }
        }

        void Ocultar()
        {
            if (velo != null) { velo.alpha = 0f; velo.blocksRaycasts = false; velo.gameObject.SetActive(false); }
            if (textoCuenta != null) textoCuenta.text = "";
            if (camara != null) { camara.enabled = false; camara.targetTexture = null; }
            Activar(false, false, false);
        }
    }
}
