using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Cámara de la vista Configuración: se acerca a lo que el jugador está eligiendo.
    /// - Pestaña Órbita: el planeta con el anillo de la órbita.
    /// - Pestaña Lanzador: la plataforma (PlataformaCohetes deja solo el elegido); el encuadre se ajusta a su altura.
    /// - Pestaña Instrumentos: la nave; al instalar un instrumento, un acercamiento breve a su módulo con una
    ///   etiqueta hacia fuera y una línea guía. Al pasar el cursor por un instrumento sin instalar, su holograma
    ///   aparece en el montaje donde iría.
    /// Solo lee el estado (pestaña y EstadoJuego.Configuracion): no cambia nada de la lógica.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraConfiguracion : MonoBehaviour
    {
        [System.Serializable]
        public struct Encuadre
        {
            public Vector3 posicion;
            public Vector3 mirarA;
            public float campo;
        }

        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] GameObject vistaConfiguracion;
        [Tooltip("Versión 6: en el Briefing se ve el Marte 3D detrás del expediente (sin la nave).")]
        [SerializeField] GameObject vistaBriefing;

        [Header("Encuadres")]
        [SerializeField] Encuadre nave;
        [SerializeField] Encuadre planeta;
        [SerializeField] Encuadre briefing;
        [SerializeField] Encuadre plataforma;
        [SerializeField] Encuadre plataformaExplorador;
        [SerializeField] Encuadre plataformaTitan;
        [SerializeField] float suavizado = 0.45f;
        [Tooltip("Campo de visión en la pestaña Satélite (más cerca que en Instrumentos).")]
        [SerializeField] float campoSatelite = 44f;

        [Header("Objetos")]
        [SerializeField] Transform raizNave;

        [Header("Acercamiento a un instrumento")]
        [SerializeField] float distanciaModulo = 3.6f;
        [SerializeField] float duracionModulo = 2.2f;
        [SerializeField] CatalogoDatos catalogo;
        [Tooltip("Etiqueta flotante (dentro del Canvas) que señala el módulo instalado.")]
        [SerializeField] RectTransform etiquetaFoco;
        [SerializeField] TMPro.TMP_Text textoFoco;
        [SerializeField] RectTransform guiaFoco;     // línea del módulo a la etiqueta
        [SerializeField] RectTransform puntoFoco;    // punto sobre el módulo
        [SerializeField] float separacionEtiqueta = 170f;

        Camera camara;
        Vector3 posicion, mirar, velPosicion, velMirar;
        float campo, velCampo;
        readonly List<string> instrumentosVistos = new List<string>();
        Transform moduloEnFoco;
        float finFoco;
        Renderer[] renderersNave;
        NaveSatelite aspectoNave;

        void Awake()
        {
            camara = GetComponent<Camera>();
            posicion = transform.position;
            mirar = transform.position + transform.forward * 10f;
            campo = camara.fieldOfView;
            if (raizNave != null)
            {
                renderersNave = raizNave.GetComponentsInChildren<Renderer>(true);
                aspectoNave = raizNave.GetComponent<NaveSatelite>();
            }
        }

        void LateUpdate()
        {
            var config = EstadoJuego.Configuracion;
            bool enConfiguracion = vistaConfiguracion != null && vistaConfiguracion.activeInHierarchy && configuracion != null;
            var pestana = enConfiguracion ? configuracion.PestanaActual : ConfiguracionUI.Pestana.Instrumentos;

            DetectarInstalacion(config, enConfiguracion && pestana == ConfiguracionUI.Pestana.Instrumentos);

            Encuadre objetivo = nave;
            if (pestana == ConfiguracionUI.Pestana.Orbita) objetivo = planeta;
            else if (pestana == ConfiguracionUI.Pestana.Lanzador)
                objetivo = string.IsNullOrEmpty(config.LanzadorId) || plataformaExplorador.campo <= 1f ? plataforma
                         : PlataformaCohetes.EsTitan(config.LanzadorId) ? plataformaTitan : plataformaExplorador;
            else if (pestana == ConfiguracionUI.Pestana.Satelite) { objetivo = nave; objetivo.campo = campoSatelite; }
            else if (moduloEnFoco != null && Time.unscaledTime < finFoco) objetivo = EncuadreModulo(moduloEnFoco);
            else moduloEnFoco = null;

            bool enBriefing = vistaBriefing != null && vistaBriefing.activeInHierarchy && briefing.campo > 1f;
            if (enBriefing) objetivo = briefing;

            // La nave no se ve mientras se mira el planeta (estaría delante, flotando sin sentido).
            MostrarNave(pestana != ConfiguracionUI.Pestana.Orbita && !enBriefing);
            GirarSatelite(enConfiguracion && pestana == ConfiguracionUI.Pestana.Satelite);
            MostrarFantasma(config, enConfiguracion && pestana == ConfiguracionUI.Pestana.Instrumentos);

            float t = Preferencias.ReducirMovimiento ? 0f : suavizado;
            float dt = Time.unscaledDeltaTime;
            posicion = Vector3.SmoothDamp(posicion, objetivo.posicion, ref velPosicion, t, Mathf.Infinity, dt);
            mirar = Vector3.SmoothDamp(mirar, objetivo.mirarA, ref velMirar, t, Mathf.Infinity, dt);
            campo = Mathf.SmoothDamp(campo, objetivo.campo > 1f ? objetivo.campo : 60f, ref velCampo, t, Mathf.Infinity, dt);
            if (t <= 0f) { posicion = objetivo.posicion; mirar = objetivo.mirarA; campo = objetivo.campo > 1f ? objetivo.campo : 60f; }

            transform.position = posicion;
            transform.rotation = Quaternion.LookRotation(mirar - posicion, Vector3.up);
            camara.fieldOfView = campo;
            ColocarEtiqueta();
        }

        /// <summary>
        /// Etiqueta «INSTALADO AQUÍ» del módulo enfocado: hacia fuera de la nave (no la tapa), dentro de la zona
        /// central entre los paneles, con una línea guía hasta un punto sobre el módulo.
        /// </summary>
        void ColocarEtiqueta()
        {
            if (etiquetaFoco == null) return;
            bool visible = moduloEnFoco != null && Time.unscaledTime < finFoco;
            if (etiquetaFoco.gameObject.activeSelf != visible) etiquetaFoco.gameObject.SetActive(visible);
            if (guiaFoco != null && guiaFoco.gameObject.activeSelf != visible) guiaFoco.gameObject.SetActive(visible);
            if (puntoFoco != null && puntoFoco.gameObject.activeSelf != visible) puntoFoco.gameObject.SetActive(visible);
            if (!visible) return;
            var padre = (RectTransform)etiquetaFoco.parent;
            var pantalla = camara.WorldToScreenPoint(CentroVisual(moduloEnFoco));
            var pantallaNave = camara.WorldToScreenPoint(raizNave.position);
            if (pantalla.z <= 0f) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, pantalla, null, out var punto)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, pantallaNave, null, out var centro);
            var dir = punto - centro;
            dir = dir.sqrMagnitude < 1f ? Vector2.up : dir.normalized;
            var deseado = punto + dir * separacionEtiqueta;
            // Zona central libre entre los dos paneles de Configuración (en proporción del lienzo).
            var r = padre.rect;
            var medio = etiquetaFoco.rect.size * 0.5f;
            float minX = r.xMin + r.width * 0.375f + medio.x, maxX = r.xMin + r.width * 0.645f - medio.x;
            float minY = r.yMin + r.height * 0.16f + medio.y, maxY = r.yMin + r.height * 0.84f - medio.y;
            deseado = new Vector2(Mathf.Clamp(deseado.x, minX, Mathf.Max(minX, maxX)), Mathf.Clamp(deseado.y, minY, maxY));
            etiquetaFoco.anchoredPosition = deseado;
            if (puntoFoco != null) puntoFoco.anchoredPosition = punto;
            if (guiaFoco != null)
            {
                var d = deseado - punto;
                guiaFoco.anchoredPosition = punto + d * 0.5f;
                guiaFoco.sizeDelta = new Vector2(d.magnitude, guiaFoco.sizeDelta.y);
                guiaFoco.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        /// <summary>Holograma del instrumento bajo el cursor (si aún no está instalado) en su montaje.</summary>
        void MostrarFantasma(Configuracion config, bool permitido)
        {
            if (aspectoNave == null) return;
            string id = null;
            var encima = BotonVivo.Encimado;
            if (permitido && encima != null && catalogo != null && encima.transform.IsChildOf(vistaConfiguracion.transform))
            {
                var texto = encima.GetComponentInChildren<TMPro.TMP_Text>();
                if (texto != null)
                    foreach (var instrumento in catalogo.instrumentos)
                        if (texto.text.Contains(instrumento.Nombre) && !config.InstrumentoIds.Contains(instrumento.id))
                        {
                            id = instrumento.id;
                            break;
                        }
            }
            aspectoNave.MostrarFantasma(id);
        }

        /// <summary>Si se añadió un instrumento, enfoca su módulo un momento (solo en la pestaña Instrumentos).</summary>
        void DetectarInstalacion(Configuracion config, bool permitido)
        {
            string nuevo = null;
            foreach (var id in config.InstrumentoIds)
                if (!instrumentosVistos.Contains(id)) nuevo = id;
            instrumentosVistos.Clear();
            instrumentosVistos.AddRange(config.InstrumentoIds);
            if (nuevo == null || !permitido || raizNave == null) return;
            var modulo = raizNave.Find("Modulo_" + nuevo);
            if (modulo == null) return;
            moduloEnFoco = modulo;
            finFoco = Time.unscaledTime + duracionModulo;
            if (textoFoco != null && catalogo != null)
                textoFoco.text = Iconos.DeInstrumento(nuevo) + "  " + catalogo.Instrumento(nuevo).Nombre.ToUpperInvariant() +
                                 Idioma.T("\n<size=75%><color=#94A3B8>INSTALLED HERE</color></size>");
        }

        Encuadre EncuadreModulo(Transform modulo)
        {
            var centro = raizNave.position;
            var punto = CentroVisual(modulo);
            // Desde fuera de la nave, del lado del módulo (también por arriba o por abajo) y algo de frente,
            // mirando a un punto entre el módulo y el centro para que se vea dónde está en la nave.
            var fuera = punto - centro;
            if (fuera.sqrMagnitude < 0.01f) fuera = Vector3.back;
            var dir = (fuera.normalized + Vector3.back * 1.1f).normalized;
            var mira = Vector3.Lerp(centro, punto, 0.65f);
            return new Encuadre { posicion = mira + dir * distanciaModulo, mirarA = mira, campo = 42f };
        }

        static Vector3 CentroVisual(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return t.position;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b.center;
        }

        /// <summary>En la pestaña Satélite la nave gira despacio para verla entera; al salir vuelve a su pose.</summary>
        void GirarSatelite(bool girar)
        {
            if (raizNave == null) return;
            if (!poseGuardada) { poseNave = raizNave.localRotation; poseGuardada = true; }
            if (girar && !Preferencias.ReducirMovimiento) giroSatelite += Time.unscaledDeltaTime * 14f;
            else giroSatelite = Mathf.MoveTowardsAngle(giroSatelite, 0f, Time.unscaledDeltaTime * 120f);
            giroSatelite = Mathf.Repeat(giroSatelite + 180f, 360f) - 180f;
            raizNave.localRotation = poseNave * Quaternion.Euler(0f, giroSatelite, 0f);
        }

        float giroSatelite;
        Quaternion poseNave;
        bool poseGuardada;

        void MostrarNave(bool visible)
        {
            if (renderersNave == null) return;
            foreach (var r in renderersNave)
                if (r != null) r.enabled = visible;
        }
    }
}
