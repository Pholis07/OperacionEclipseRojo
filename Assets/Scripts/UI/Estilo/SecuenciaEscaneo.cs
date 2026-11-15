using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Investigación orbital en 3D (storyboard B, «híbrida» con cámara que sigue a la nave). Tras cada escaneo,
    /// en el lugar del mapa aparece la vista 3D:
    ///   Objetivo fijado → Avance por la órbita → Escaneo (efecto del instrumento) → Datos → Regreso al encuadre.
    /// Al terminar vuelve el mapa. Todo es presentación: la lógica del escaneo ya se resolvió (OperacionUI) y el
    /// resultado ya está en pantalla, así que nada depende de esta secuencia.
    /// - Un clic sobre la vista la salta (y el mapa no recibe clics mientras se ve: no hay escaneos dobles).
    /// - Escaneos sin transmisión nueva usan una versión corta.
    /// - Con «Reducir movimiento» no hay secuencia: queda la marca en el mapa.
    /// - Siempre empieza y termina en la pose de presentación (sin deriva entre escaneos).
    /// </summary>
    public class SecuenciaEscaneo : MonoBehaviour
    {
        [SerializeField] OperacionUI operacion;
        [SerializeField] UnityEngine.UI.RawImage mapa;           // para saber qué zona de Marte se ve
        [SerializeField] EscenarioOrbital escenario;
        [SerializeField] Camera camara;
        [SerializeField] DirectorCinematica director;
        [SerializeField] TransicionMes transicion;      // el escaneo empieza cuando termina «pasa un mes»

        [Header("Vista (encima del mapa)")]
        [SerializeField] CanvasGroup vista;
        [SerializeField] UnityEngine.UI.RawImage pantalla;
        [SerializeField] UnityEngine.UI.Button saltar;
        [SerializeField] RectTransform reticula;
        [SerializeField] TMP_Text textoEstado;
        [SerializeField] TMP_Text textoInstrumento;

        [Header("Efecto del instrumento")]
        [SerializeField] Transform haz;                         // cono con el vértice en el satélite
        [SerializeField] Renderer rendererHaz;
        [SerializeField] SpriteRenderer mancha;                 // brillo sobre la superficie
        [SerializeField] AudioSource fuente;
        [SerializeField] CatalogoDatos catalogo;

        const float AvanceCompleto = 0.32f;   // radianes de órbita en la versión completa
        const float AvanceCorto = 0.16f;

        RenderTexture textura;
        Coroutine secuencia;
        bool saltando;
        Vector3 camPos, camVel;
        MaterialPropertyBlock bloque;
        ModoEscaneo modo;   // versión 7: modos del espectrómetro y de la cámara de alta resolución

        public bool Activa => secuencia != null;

        /// <summary>Duración nominal de la secuencia completa (storyboard B: de 9 a 11 s).</summary>
        public const float DuracionCompleta = 0.3f + TObjetivo + TAvance + TEscaneo + TDatos + TRegreso + 0.3f;
        const float TObjetivo = 0.9f, TAvance = 1.8f, TEscaneo = 3.0f, TDatos = 1.4f, TRegreso = 1.9f;

        void Awake()
        {
            if (vista != null) { vista.alpha = 0f; vista.blocksRaycasts = false; vista.gameObject.SetActive(false); }
            if (saltar != null) saltar.onClick.AddListener(() => saltando = true);
            if (haz != null) haz.gameObject.SetActive(false);
            if (mancha != null) mancha.gameObject.SetActive(false);
            bloque = new MaterialPropertyBlock();
        }

        void OnEnable() { if (operacion != null) operacion.AlEscanear += AlEscanear; }

        void OnDisable()
        {
            if (operacion != null) operacion.AlEscanear -= AlEscanear;
            Terminar();
        }

        void OnDestroy()
        {
            if (textura != null) { textura.Release(); Destroy(textura); }
        }

        void AlEscanear(string instrumento, Vector2 punto, Lectura lectura, bool nueva)
        {
            if (!Application.isPlaying || Preferencias.ReducirMovimiento || escenario == null || camara == null) return;
            if (director != null && director.Reproduciendo) return;   // la cinemática tiene la cámara
            if (secuencia != null) StopCoroutine(secuencia);
            secuencia = StartCoroutine(Secuencia(instrumento, punto, lectura, nueva));
        }

        Vector2 UvTextura(Vector2 punto)
        {
            // Dónde está en el globo la región que muestra el mapa (cráter Gale o la zona de la foto global).
            var region = mapa != null ? mapa.GetComponent<MapaMarteUI>() : null;
            var r = region != null ? region.RectGlobo : mapa != null ? mapa.uvRect : new Rect(0, 0, 1, 1);
            return new Vector2(r.x + punto.x * r.width, r.y + punto.y * r.height);
        }

        IEnumerator Secuencia(string instrumento, Vector2 punto, Lectura lectura, bool completa)
        {
            // Primero el paso del mes (si se está viendo); mientras tanto el resumen sigue esperando.
            while (transicion != null && transicion.Activa) yield return null;
            saltando = false;
            PrepararCamara();
            escenario.Preparar(EstadoJuego.Configuracion);
            escenario.MostrarOrbita(true, 0.55f);
            float f0 = escenario.FaseAncla;
            float f1 = f0 + (completa ? AvanceCompleto : AvanceCorto);
            var uv = UvTextura(punto);
            var objetivo = escenario.AlinearRegion(uv, f1);
            var (p0, r0) = escenario.PoseCamara(f0);
            camara.transform.SetPositionAndRotation(p0, r0);
            camara.fieldOfView = escenario.CampoAncla;
            camPos = p0; camVel = Vector3.zero;
            textoInstrumento.text = Iconos.DeInstrumento(instrumento) + "  " +
                                    (catalogo != null ? catalogo.Instrumento(instrumento).Nombre.ToUpperInvariant() : instrumento);
            var color = ColorInstrumento(instrumento);

            // Aparece la vista sobre el mapa.
            vista.gameObject.SetActive(true);
            vista.blocksRaycasts = true;
            yield return Fundir(0f, 1f, 0.3f);

            // 1) Objetivo fijado.
            Estado(Idioma.T("TARGET LOCKED"), Cian);
            Sonar(ArteDatos.Efecto.FijarObjetivo);
            if (completa) yield return Esperar(TObjetivo, objetivo, f0, null);

            // 2) Avance por la órbita, orientando los instrumentos hacia la región.
            Estado(Idioma.T("APPROACH"), Cian);
            Sonar(ArteDatos.Efecto.Servo, 0.7f);
            Sonar(ArteDatos.Efecto.Propulsores, 0.5f);
            float tAvance = completa ? TAvance : 0.9f;
            for (float t = 0f; t < 1f && !saltando; t += Time.deltaTime / tAvance)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                float fase = Mathf.Lerp(f0, f1, k);
                var mira = Vector3.Lerp(escenario.Centro, objetivo, Mathf.SmoothStep(0f, 1f, t * 1.4f));
                escenario.ColocarSatelite(fase, mira);
                Seguir(fase, objetivo, 0f);
                Reticula(objetivo, true);
                yield return null;
            }
            escenario.ColocarSatelite(f1, objetivo);

            // 3) Escaneo con el efecto del instrumento.
            Estado(Idioma.T("SCANNING"), color);
            Sonar(ArteDatos.EscaneoDe(instrumento));
            float tEscaneo = completa ? TEscaneo : 1.1f;
            Haz(true, objetivo, color);
            // Versión 7: el espectrómetro y la cámara muestran su modo de análisis (solo en la versión completa).
            bool encontrada = lectura == Lectura.Encontrada;
            if (completa && ModoEscaneo.Tiene(instrumento))
            {
                if (modo == null) modo = new ModoEscaneo((RectTransform)vista.transform, textoEstado);
                modo.Empezar(instrumento, encontrada);
            }
            for (float t = 0f; t < 1f && !saltando; t += Time.deltaTime / tEscaneo)
            {
                AnimarHaz(instrumento, t, objetivo, color);
                if (modo != null)
                {
                    int capaAntes = modo.CapaActiva;
                    modo.Animar(instrumento, t);
                    if (modo.CapaActiva != capaAntes && modo.CapaActiva >= 0) Sonar(ArteDatos.Efecto.ConmutacionEspectral, 0.7f);
                }
                Seguir(f1, objetivo, Mathf.Sin(t * Mathf.PI) * 0.12f);   // acercamiento sutil
                Reticula(objetivo, true);
                yield return null;
            }
            Haz(false, objetivo, color);

            // 4) Datos: el resultado ya está en el texto de lectura; aquí solo se resume.
            var (texto, colorTexto) = Resultado(lectura, completa);
            Estado(texto, colorTexto);
            if (completa) Sonar(ArteDatos.Efecto.DatosRecibidos);
            modo?.Concluir(instrumento, encontrada);
            // Con un modo en pantalla se da un poco más de tiempo para leer su nota.
            bool conModo = modo != null && modo.Visible;
            if (!saltando) yield return Esperar(completa ? TDatos + (conModo ? 1.2f : 0f) : 0.5f, objetivo, f1, null);
            modo?.Ocultar();

            // 5) Regreso: la cámara vuelve al encuadre de presentación; al fundirse, todo vuelve al ancla.
            Estado("", Cian);
            var (pa, ra) = escenario.PoseCamara(f1);
            var (pi, ri) = (camara.transform.position, camara.transform.rotation);
            float fovInicial = camara.fieldOfView;
            float tRegreso = saltando ? 0.15f : completa ? TRegreso : 0.8f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / tRegreso)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                camara.transform.SetPositionAndRotation(Vector3.Lerp(pi, pa, k), Quaternion.Slerp(ri, ra, k));
                camara.fieldOfView = Mathf.Lerp(fovInicial, escenario.CampoAncla, k);
                Reticula(objetivo, false);
                yield return null;
            }
            yield return Fundir(1f, 0f, saltando ? 0.12f : 0.3f);
            Terminar();
        }

        // ---------- Piezas ----------

        static readonly Color Cian = new Color32(0x69, 0xC6, 0xD2, 0xFF);

        static Color ColorInstrumento(string id)
        {
            switch (id)
            {
                case "termica": return new Color(1f, 0.45f, 0.18f);
                case "radar": return new Color(0.37f, 0.78f, 0.9f);
                case "alta_res": return new Color(0.92f, 0.95f, 1f);
                case "atmosferico": return new Color(0.85f, 0.7f, 0.45f);
                default: return new Color(0.62f, 0.5f, 1f);
            }
        }

        static (string, Color) Resultado(Lectura l, bool nueva)
        {
            switch (l)
            {
                case Lectura.Encontrada: return nueva ? (Idioma.T("DATA RECEIVED · ANOMALY OBSERVED"), new Color32(0x22, 0xC5, 0x5E, 0xFF))
                    : (Idioma.T("ANOMALY OBSERVED · NO NEW EVIDENCE"), new Color32(0xF4, 0xC4, 0x6A, 0xFF));   // ya estaba o le faltó calidad
                case Lectura.Caliente: return (Idioma.T("READING: HOT"), new Color32(0xE2, 0x64, 0x3A, 0xFF));
                case Lectura.Tibio: return (Idioma.T("READING: WARM"), new Color32(0xEA, 0xB3, 0x08, 0xFF));
                case Lectura.Frio: return (Idioma.T("READING: COLD"), new Color32(0x60, 0xA5, 0xFA, 0xFF));
                default: return (Idioma.T("NO DATA · SPACECRAFT DISORIENTED"), new Color32(0x9C, 0xA3, 0xAF, 0xFF));
            }
        }

        void PrepararCamara()
        {
            int lado = 1024;
            if (textura == null)
            {
                textura = new RenderTexture(lado, lado, 24, RenderTextureFormat.ARGB32) { name = "VistaOrbital", antiAliasing = 2 };
                textura.Create();
            }
            camara.targetTexture = textura;
            camara.aspect = 1f;
            camara.enabled = true;
            pantalla.texture = textura;
        }

        void Terminar()
        {
            saltando = false;
            modo?.Ocultar();
            if (haz != null) haz.gameObject.SetActive(false);
            if (mancha != null) mancha.gameObject.SetActive(false);
            if (fuente != null) fuente.Stop();
            if (vista != null) { vista.alpha = 0f; vista.blocksRaycasts = false; vista.gameObject.SetActive(false); }
            if (camara != null && (director == null || !director.Reproduciendo)) { camara.enabled = false; camara.targetTexture = null; }
            if (escenario != null && escenario.Orbita != null) escenario.ColocarSatelite(escenario.FaseAncla, null);
            secuencia = null;
        }

        IEnumerator Fundir(float de, float a, float duracion)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
            {
                vista.alpha = Mathf.Lerp(de, a, t);
                yield return null;
            }
            vista.alpha = a;
        }

        IEnumerator Esperar(float segundos, Vector3 objetivo, float fase, Vector3? apuntar)
        {
            for (float t = 0f; t < segundos && !saltando; t += Time.deltaTime)
            {
                Reticula(objetivo, true);
                yield return null;
            }
        }

        /// <summary>Cámara que sigue al satélite con amortiguación, sin perder de vista la región.</summary>
        void Seguir(float fase, Vector3 objetivo, float acercar)
        {
            var (pa, _) = escenario.PoseCamara(fase);
            var sat = escenario.Satelite.position;
            var deseada = Vector3.Lerp(pa, sat, acercar);
            camPos = Vector3.SmoothDamp(camPos, deseada, ref camVel, 0.25f);
            camara.transform.position = camPos;
            var mira = Vector3.Lerp(sat, objetivo, 0.35f);
            var (radial, _, _) = escenario.Marco(fase);
            camara.transform.rotation = Quaternion.Slerp(camara.transform.rotation,
                Quaternion.LookRotation(mira - camPos, radial), 1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        void Reticula(Vector3 objetivo, bool visible)
        {
            if (reticula == null) return;
            var vp = camara.WorldToViewportPoint(objetivo);
            bool enPantalla = visible && vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
            if (reticula.gameObject.activeSelf != enPantalla) reticula.gameObject.SetActive(enPantalla);
            if (!enPantalla) return;
            var rt = pantalla.rectTransform.rect;
            reticula.anchoredPosition = new Vector2((vp.x - 0.5f) * rt.width, (vp.y - 0.5f) * rt.height);
            reticula.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 40f);
        }

        void Estado(string texto, Color color)
        {
            textoEstado.text = texto;
            textoEstado.color = color;
        }

        void Haz(bool visible, Vector3 objetivo, Color color)
        {
            if (haz != null) haz.gameObject.SetActive(visible);
            if (mancha != null)
            {
                mancha.gameObject.SetActive(visible);
                var normal = (objetivo - escenario.Centro).normalized;
                mancha.transform.position = objetivo + normal * 0.05f;
                mancha.transform.rotation = Quaternion.LookRotation(-normal);
                mancha.color = new Color(color.r, color.g, color.b, 0.8f);
            }
        }

        /// <summary>Cada instrumento tiene su forma de escanear: barrido térmico, pulsos de radar, destello de cámara…</summary>
        void AnimarHaz(string instrumento, float t, Vector3 objetivo, Color color)
        {
            if (haz == null) return;
            var origen = escenario.Satelite.position;
            var dir = objetivo - origen;
            float largo = dir.magnitude;
            haz.position = origen;
            haz.rotation = Quaternion.LookRotation(dir.normalized);
            float ancho = 1.6f, alfa = 0.35f;
            switch (instrumento)
            {
                case "termica":   // barrido que va y viene
                    ancho = 1.2f + Mathf.Sin(t * Mathf.PI * 4f) * 0.6f; alfa = 0.35f; break;
                case "radar":     // pulsos
                    alfa = Mathf.Repeat(t * 6f, 1f) < 0.35f ? 0.5f : 0.08f; ancho = 2.2f; break;
                case "alta_res":  // enfoque fino y destello final
                    ancho = Mathf.Lerp(2.2f, 0.5f, t); alfa = t > 0.85f ? 0.8f : 0.25f; break;
                case "atmosferico": // cono amplio y suave
                    ancho = 3.2f; alfa = 0.18f + Mathf.Sin(t * Mathf.PI * 3f) * 0.06f; break;
                default:          // espectrómetro: bandas que cambian de color
                    color = Color.HSVToRGB(Mathf.Repeat(0.7f + t * 0.6f, 1f), 0.55f, 1f); alfa = 0.3f; ancho = 1.4f; break;
            }
            haz.localScale = new Vector3(ancho, ancho, largo);
            if (rendererHaz != null)
            {
                rendererHaz.GetPropertyBlock(bloque);
                var c = new Color(color.r, color.g, color.b, alfa);
                bloque.SetColor("_Color", c);       // Sprites/Default
                bloque.SetColor("_BaseColor", c);   // por si se cambia a un material URP
                rendererHaz.SetPropertyBlock(bloque);
            }
            if (mancha != null)
            {
                float s = ancho * 1.4f * (1f + Mathf.Sin(Time.time * 9f) * 0.08f);
                mancha.transform.localScale = Vector3.one * s;
                mancha.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alfa * 2f));
            }
        }

        void Sonar(ArteDatos.Efecto efecto, float volumen = 1f)
        {
            if (fuente == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(efecto);
            if (clip == null) return;
            fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }
    }
}
