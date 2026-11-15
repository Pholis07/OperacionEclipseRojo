using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>
    /// Cinemática Tierra → Marte (storyboard A, ~24 s, 8 tomas con cortes) al pulsar «Lanzar» con la misión
    /// válida. Se ve encima de todo; la Operación ya está activa debajo desde el mismo cuadro (las pruebas y la
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
        [SerializeField] ParticleSystem humo;
        [SerializeField] Transform llama;
        [SerializeField] GameObject espacio;          // Tierra, estrellas: solo en las tomas en órbita terrestre
        [SerializeField] Transform puntoEspacio;

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
        static readonly Color CieloAlto = new Color(0.05f, 0.08f, 0.2f);
        static readonly Color Negro = new Color(0.01f, 0.012f, 0.02f);
        const int Tomas = 8;

        RenderTexture textura;
        Coroutine rutina;
        bool omitir, terminada;
        Transform cohete, superior;
        Vector3 posCohete, posSuperior;
        Quaternion rotCohete;

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
        }

        void OnDisable()
        {
            if (Reproduciendo) Final();
        }

        void OnDestroy()
        {
            if (textura != null) { textura.Release(); Destroy(textura); }
        }

        public void Omitir() => omitir = true;

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
            if (coheteExplorador != null) coheteExplorador.gameObject.SetActive(!titan);
            if (coheteTitan != null) coheteTitan.gameObject.SetActive(titan);
            posCohete = cohete.position; rotCohete = cohete.rotation; posSuperior = superior.localPosition;
            float alto = titan ? 11f : 6.1f;
            escenario.Preparar(config);
            escenario.MostrarOrbita(false);

            PrepararCamara();
            velo.gameObject.SetActive(true);
            velo.alpha = 1f;
            velo.blocksRaycasts = true;
            corte.alpha = 1f;
            if (musica != null) musica.Cambiar(ArteDatos.Musica.Lanzamiento);
            Activar(true, false, false);

            // 1) 0–2.5 s: detalle de las toberas, cuenta atrás.
            Toma(1, "SISTEMAS NOMINALES");
            Sonar(ArteDatos.Efecto.CuentaAtras);
            yield return Cortar(false);
            var basePos = posCohete;
            for (float t = 0f; t < 2.5f && !omitir; t += Time.deltaTime)
            {
                textoCuenta.text = t < 0.8f ? "T−03" : t < 1.6f ? "T−02" : "T−01";
                var cam = basePos + new Vector3(-alto * 0.32f, alto * 0.06f, -alto * 0.42f) + Vector3.up * t * 0.08f * alto;
                Mirar(cam, basePos + Vector3.up * alto * 0.35f, 50f);
                yield return null;
            }
            textoCuenta.text = "";

            // 2) 2.5–5.8 s: ignición y despegue, contrapicado.
            Toma(2, "LANZAMIENTO CONFIRMADO");
            Encender(true, alto);
            Sonar(ArteDatos.Efecto.Despegue);
            var camSuelo = basePos + new Vector3(-alto * 0.9f, alto * 0.05f, -alto * 1.4f);
            for (float t = 0f; t < 3.3f && !omitir; t += Time.deltaTime)
            {
                float subida = t < 0.6f ? 0f : 0.5f * 2.2f * alto * 0.12f * (t - 0.6f) * (t - 0.6f);
                cohete.position = basePos + Vector3.up * subida;
                Mirar(camSuelo, cohete.position + Vector3.up * alto * 0.5f, 55f);
                yield return null;
            }

            // 3) 5.8–8.5 s: plano lateral largo, el cielo pasa de azul a negro.
            yield return Cortar(true);
            Toma(3, "ASCENSO");
            Activar(false, false, false);
            for (float t = 0f; t < 2.7f && !omitir; t += Time.deltaTime)
            {
                float k = t / 2.7f;
                cohete.position = basePos + Vector3.up * (alto * 6f + alto * 8f * k * k);
                cohete.rotation = rotCohete * Quaternion.Euler(0f, 0f, -12f * k);   // empieza a inclinarse
                camara.backgroundColor = Color.Lerp(CieloDia, Color.Lerp(CieloAlto, Negro, k), Mathf.SmoothStep(0f, 1f, k));
                var cam = cohete.position + new Vector3(-alto * 2.6f, -alto * 0.4f, -alto * 1.2f);
                Mirar(cam, cohete.position + cohete.up * alto * 0.5f, 42f);
                yield return null;
            }

            // 4) 8.5–11 s: en órbita terrestre, separación de etapas con la Tierra de fondo.
            yield return Cortar(true);
            Toma(4, "SEPARACIÓN COMPLETADA");
            Encender(false, alto);
            Activar(false, true, false);
            camara.backgroundColor = Negro;
            cohete.position = puntoEspacio.position;
            cohete.rotation = puntoEspacio.rotation;   // eje del cohete (arriba) = dirección de avance
            bool separado = false;
            var adelante = cohete.up;
            var desfaseSuperior = superior.position - cohete.position;   // donde va la etapa superior unida
            var rotSuperior = superior.rotation;
            for (float t = 0f; t < 2.5f && !omitir; t += Time.deltaTime)
            {
                if (!separado && t > 0.6f) { separado = true; Sonar(ArteDatos.Efecto.Separacion); }
                float s = separado ? (t - 0.6f) : 0f;
                var avance = puntoEspacio.position + adelante * (t * alto * 0.25f);
                // La etapa inferior se queda atrás y gira despacio; la superior sigue de frente.
                cohete.position = avance - adelante * s * s * alto * 0.35f;
                cohete.rotation = puntoEspacio.rotation * Quaternion.Euler(s * 9f, 0f, s * 6f);
                superior.position = avance + desfaseSuperior + adelante * s * alto * 0.12f;
                superior.rotation = rotSuperior;
                var centro = avance + adelante * alto * 0.55f;
                // De lado y algo por encima: el cohete cruza la pantalla con la Tierra debajo.
                Mirar(centro + Vector3.up * alto * 0.3f + Vector3.back * alto * 1.35f, centro + Vector3.down * alto * 0.1f, 45f);
                yield return null;
            }

            // 5) 11–13 s: se libera la carga científica (el satélite elegido, con sus instrumentos).
            Toma(5, "CARGA CIENTÍFICA LIBERADA");
            Sonar(ArteDatos.Efecto.Despliegue);
            var sat = escenario.Satelite;
            var escalaSat = sat.localScale;
            var inicioSat = CentroVisual(superior) + adelante * alto * 0.25f;
            for (float t = 0f; t < 2f && !omitir; t += Time.deltaTime)
            {
                sat.position = inicioSat + adelante * (t * alto * 0.12f);
                // Los instrumentos (cara −Z de la nave) hacia la cámara, girando despacio.
                sat.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(0f, 25f + t * 10f, 0f);
                superior.position -= adelante * Time.deltaTime * alto * 0.05f;
                Mirar(sat.position + Vector3.up * 1.2f + Vector3.back * 5.5f - adelante * 1.5f, sat.position, 42f);
                yield return null;
            }

            // 6) 13–17.5 s: transferencia Tierra → Marte en un mapa (tiempo acelerado, sin cifras).
            yield return Cortar(true);
            Toma(6, "TRANSFERENCIA INTERPLANETARIA · TIEMPO ACELERADO");
            Activar(false, false, true);
            Sonar(ArteDatos.Efecto.Transferencia);
            yield return Transferencia(4.5f);

            // 7) 17.5–21 s: aproximación a Marte e inserción orbital.
            yield return Cortar(true);
            Toma(7, "INSERCIÓN ORBITAL");
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

            // 8) 21–24 s: plano héroe en la órbita elegida; la cámara llega al encuadre de la investigación.
            Toma(8, "ÓRBITA ESTABLECIDA · ANOMALÍA LOCALIZADA");
            escenario.MostrarOrbita(true, 0.85f);
            Sonar(ArteDatos.Efecto.DatosRecibidos, 0.8f);
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
            textoFase.text = "FASE DE MISIÓN  " + numero + " / " + Tomas;
            textoHud.text = hud;
        }

        void Mirar(Vector3 posicion, Vector3 objetivo, float campo)
        {
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
            if (espacio != null) espacio.SetActive(orbitaTerrestre);
            if (mapaTransferencia != null) mapaTransferencia.SetActive(mapa);
            if (camara == null) return;
            if (tierraFirme) camara.backgroundColor = CieloDia;
            else if (!orbitaTerrestre) camara.backgroundColor = Negro;
            // Cada escenario solo ve lo suyo (los demás quedan más allá del plano lejano o tras su cielo).
            camara.farClipPlane = tierraFirme ? 320f : mapa ? 140f : 600f;
        }

        void Encender(bool encendido, float alto)
        {
            if (llama != null)
            {
                llama.gameObject.SetActive(encendido);
                if (encendido)
                {
                    llama.SetParent(cohete, false);
                    llama.localPosition = Vector3.zero;
                    llama.localRotation = Quaternion.identity;
                    llama.localScale = Vector3.one * alto * 0.12f;
                }
            }
            if (humo != null)
            {
                if (encendido) { humo.transform.position = posCohete; humo.Play(); }
                else humo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void MostrarZona(bool visible)
        {
            if (pulsoZona == null) return;
            pulsoZona.gameObject.SetActive(visible);
            if (!visible) return;
            // Centro de la zona del mapa de la Operación (la región, no el punto exacto de la anomalía).
            var r = mapaOperacion != null ? mapaOperacion.uvRect : new Rect(0.4f, 0.4f, 0.2f, 0.2f);
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
            if (fuente == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(efecto);
            if (clip != null) fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }

        /// <summary>El mismo final al terminar o al omitir: satélite en su órbita, audio y vista de la Operación.</summary>
        void Final()
        {
            if (rutina != null) { StopCoroutine(rutina); rutina = null; }
            if (fuente != null) fuente.Stop();
            Encender(false, 1f);
            if (llama != null) llama.SetParent(transform, false);
            if (cohete != null) { cohete.position = posCohete; cohete.rotation = rotCohete; superior.localPosition = posSuperior; superior.localRotation = Quaternion.identity; }
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
