using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// «Pasa un mes»: al pulsar «Resolver mes», antes del resumen, unos 2.4 s de tiempo acelerado:
    /// - Detrás, Marte gira rápido bajo la tormenta y el satélite da vueltas en su órbita (cámara de la cinemática).
    /// - Delante, una tarjeta de calendario: «MES 3» deja paso a «MES 4», la tira de los meses se llena y los días
    ///   corren; debajo, lo más importante que pasó (tormenta, evento, datos enviados, contacto con la Tierra).
    /// Solo lee el ResultadoMes que avisa OperacionUI (la lógica ya terminó en ese mismo cuadro; el resumen ya está
    /// marcado como pendiente y solo se dibuja al terminar esto). Un clic la salta. No se reproduce con «Reducir
    /// movimiento», en modo edición ni durante la cinemática de lanzamiento.
    /// </summary>
    public class TransicionMes : MonoBehaviour
    {
        [SerializeField] OperacionUI operacion;
        [SerializeField] DirectorCinematica director;
        [SerializeField] EscenarioOrbital escenario;
        [SerializeField] Camera camara;
        [SerializeField] CanvasGroup grupo;
        [SerializeField] UnityEngine.UI.RawImage pantalla;
        [SerializeField] UnityEngine.UI.Button saltar;
        [SerializeField] RectTransform tarjeta;
        [SerializeField] TMP_Text textoMesAnterior;
        [SerializeField] TMP_Text textoMesNuevo;
        [SerializeField] TMP_Text textoDias;
        [SerializeField] TMP_Text textoSuceso;
        [SerializeField] UnityEngine.UI.Image[] segmentos;    // tira de los meses de la campaña
        [SerializeField] AudioSource fuente;
        [SerializeField] float duracion = 2.4f;
        [SerializeField] Color colorLleno = new Color(0.89f, 0.39f, 0.23f);
        [SerializeField] Color colorVacio = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] Color colorNuevo = new Color(0.37f, 0.78f, 0.9f);

        Coroutine rutina;
        RenderTexture textura;
        bool saltando;

        public bool Activa => rutina != null;

        void Awake()
        {
            if (saltar != null) saltar.onClick.AddListener(() => saltando = true);
            Ocultar();
        }

        void OnEnable() { if (operacion != null) operacion.AlResolverMes += AlResolverMes; }

        void OnDisable()
        {
            if (operacion != null) operacion.AlResolverMes -= AlResolverMes;
            Terminar();
        }

        void OnDestroy()
        {
            if (textura != null) { textura.Release(); Destroy(textura); }
        }

        void AlResolverMes(ResultadoMes r)
        {
            if (r == null || !r.Valido) return;
            if (!Application.isPlaying || Preferencias.ReducirMovimiento) return;
            if (director != null && director.Reproduciendo) return;   // la cinemática tiene la cámara
            if (rutina != null) StopCoroutine(rutina);
            rutina = StartCoroutine(Reproducir(r));
        }

        IEnumerator Reproducir(ResultadoMes r)
        {
            saltando = false;
            var op = operacion.Op;
            int total = op.MesesResueltos + op.MesesRestantes;
            textoMesAnterior.text = "MES " + r.Mes;
            textoMesNuevo.text = r.Termino ? "FIN" : "MES " + (r.Mes + 1);
            textoSuceso.text = Suceso(r, op);
            textoDias.text = "DÍA 0";
            PintarSegmentos(total, r.Mes - 1, -1f);
            PrepararCamara();
            escenario.Preparar(EstadoJuego.Configuracion);
            escenario.MostrarOrbita(true, 0.5f);
            Sonar();

            grupo.gameObject.SetActive(true);
            grupo.blocksRaycasts = true;
            float fase = escenario.FaseAncla;
            var anterior = (RectTransform)textoMesAnterior.transform;
            var nuevo = (RectTransform)textoMesNuevo.transform;
            float salto = anterior.rect.height * 1.1f;

            for (float t = 0f; t < duracion && !saltando; t += Time.unscaledDeltaTime)
            {
                float k = t / duracion;
                grupo.alpha = Mathf.Clamp01(Mathf.Min(t / 0.25f, (duracion - t) / 0.3f));
                // Tiempo acelerado: el satélite da vueltas y Marte gira (lo devuelve Preparar al terminar).
                fase += Time.unscaledDeltaTime * 5.5f;
                escenario.ColocarSatelite(fase, null);
                escenario.GirarPlaneta(Time.unscaledDeltaTime * 140f);
                var centro = escenario.Centro;
                camara.transform.position = centro + new Vector3(-6f, 16f, -58f);
                camara.transform.rotation = Quaternion.LookRotation(centro - camara.transform.position, Vector3.up);
                camara.fieldOfView = 34f;

                // Calendario: el mes viejo sube y se va; el nuevo entra desde abajo; los días corren.
                float cambio = Animar.SalidaCubica(Mathf.Clamp01((k - 0.35f) / 0.3f));
                anterior.anchoredPosition = new Vector2(0f, salto * cambio);
                textoMesAnterior.alpha = 1f - cambio;
                nuevo.anchoredPosition = new Vector2(0f, -salto * (1f - cambio));
                textoMesNuevo.alpha = cambio;
                textoDias.text = "DÍA " + Mathf.RoundToInt(Mathf.Clamp01(k / 0.8f) * Mathf.Max(0, r.DiasUtiles));
                PintarSegmentos(total, r.Mes - 1, Mathf.Clamp01((k - 0.2f) / 0.5f));
                if (tarjeta != null) tarjeta.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Mathf.Clamp01(k / 0.3f) * Mathf.PI));
                textoSuceso.alpha = Mathf.Clamp01((k - 0.45f) / 0.2f);
                yield return null;
            }
            Terminar();
        }

        /// <summary>La línea más importante del mes (solo de lo que ya calculó la lógica).</summary>
        static string Suceso(ResultadoMes r, Operacion op)
        {
            if (r.Termino) return "FIN DE LA CAMPAÑA · PREPARA EL INFORME";
            if (r.Evento != null && r.Evento.Datos != null)
            {
                if (r.Evento.Datos.efecto == EfectoEvento.Tormenta) return "LA TORMENTA DE POLVO CRECE";
                return "EVENTO A BORDO · " + r.Evento.Datos.nombre.ToUpperInvariant();
            }
            if (r.Enviadas.Count > 0) return "DATOS RECIBIDOS EN LA TIERRA";
            if (!string.IsNullOrEmpty(r.EnvioFallido)) return "EL ENVÍO NO SALIÓ ESTE MES";
            if (r.Observaciones.Count > 0) return "NUEVAS OBSERVACIONES EN MEMORIA";
            return op.ContactoAbierto ? "PRÓXIMO MES · CONTACTO CON LA TIERRA" : "PRÓXIMO MES · SIN CONTACTO CON LA TIERRA";
        }

        void PintarSegmentos(int total, int llenos, float avance)
        {
            if (segmentos == null) return;
            for (int i = 0; i < segmentos.Length; i++)
            {
                var s = segmentos[i];
                if (s == null) continue;
                bool existe = i < total;
                if (s.gameObject.activeSelf != existe) s.gameObject.SetActive(existe);
                if (!existe) continue;
                if (i < llenos) s.color = colorLleno;
                else if (i == llenos && avance >= 0f) s.color = Color.Lerp(colorVacio, colorNuevo, avance);
                else s.color = colorVacio;
            }
        }

        void PrepararCamara()
        {
            int ancho = Mathf.Max(640, Screen.width), alto = Mathf.Max(360, Screen.height);
            if (textura == null || textura.width != ancho || textura.height != alto)
            {
                if (textura != null) { textura.Release(); Destroy(textura); }
                textura = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32) { name = "TransicionMes", antiAliasing = 2 };
                textura.Create();
            }
            camara.targetTexture = textura;
            camara.aspect = (float)ancho / alto;
            camara.farClipPlane = 600f;
            camara.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            camara.enabled = true;
            pantalla.texture = textura;
        }

        void Sonar()
        {
            if (fuente == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(ArteDatos.Efecto.PasoMes);
            if (clip != null) fuente.PlayOneShot(clip, Preferencias.VolumenEfectos);
        }

        void Terminar()
        {
            if (rutina != null) { StopCoroutine(rutina); rutina = null; }
            if (escenario != null && EstadoJuego.Configuracion != null && Application.isPlaying)
            {
                // Mismo encuadre de siempre para el escaneo que viene después (sin deriva).
                escenario.Preparar(EstadoJuego.Configuracion);
                escenario.MostrarOrbita(true, 0.55f);
            }
            Ocultar();
        }

        void Ocultar()
        {
            if (grupo != null)
            {
                grupo.alpha = 0f;
                grupo.blocksRaycasts = false;
                grupo.gameObject.SetActive(false);
            }
            if (camara != null && (director == null || !director.Reproduciendo))
            {
                camara.enabled = false;
                camara.targetTexture = null;
            }
        }
    }
}
