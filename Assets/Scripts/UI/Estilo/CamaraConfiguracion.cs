using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Cámara de la vista Configuración: se acerca a lo que el jugador está eligiendo.
    /// - Pestaña Órbita: el planeta con el anillo de la órbita.
    /// - Pestaña Lanzador: la plataforma con los dos cohetes; el elegido se ilumina y gira.
    /// - Pestaña Instrumentos: la nave; al instalar un instrumento, un acercamiento breve a su módulo.
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

        [Header("Encuadres")]
        [SerializeField] Encuadre nave;
        [SerializeField] Encuadre planeta;
        [SerializeField] Encuadre plataforma;
        [SerializeField] float suavizado = 0.45f;
        [Tooltip("Campo de visión en la pestaña Satélite (más cerca que en Instrumentos).")]
        [SerializeField] float campoSatelite = 44f;

        [Header("Objetos")]
        [SerializeField] Transform raizNave;
        [SerializeField] GameObject coheteExplorador;
        [SerializeField] GameObject coheteTitan;
        [SerializeField] GameObject resaltadoExplorador;
        [SerializeField] GameObject resaltadoTitan;

        [Header("Acercamiento a un instrumento")]
        [SerializeField] float distanciaModulo = 3.6f;
        [SerializeField] float duracionModulo = 2.2f;
        [SerializeField] CatalogoDatos catalogo;
        [Tooltip("Etiqueta flotante (dentro del Canvas) que señala el módulo instalado.")]
        [SerializeField] RectTransform etiquetaFoco;
        [SerializeField] TMPro.TMP_Text textoFoco;

        Camera camara;
        Vector3 posicion, mirar, velPosicion, velMirar;
        float campo, velCampo;
        readonly List<string> instrumentosVistos = new List<string>();
        Transform moduloEnFoco;
        float finFoco;
        Renderer[] renderersNave;

        void Awake()
        {
            camara = GetComponent<Camera>();
            posicion = transform.position;
            mirar = transform.position + transform.forward * 10f;
            campo = camara.fieldOfView;
            if (raizNave != null) renderersNave = raizNave.GetComponentsInChildren<Renderer>(true);
        }

        void LateUpdate()
        {
            var config = EstadoJuego.Configuracion;
            bool enConfiguracion = vistaConfiguracion != null && vistaConfiguracion.activeInHierarchy && configuracion != null;
            var pestana = enConfiguracion ? configuracion.PestanaActual : ConfiguracionUI.Pestana.Instrumentos;

            DetectarInstalacion(config, enConfiguracion && pestana == ConfiguracionUI.Pestana.Instrumentos);

            Encuadre objetivo = nave;
            if (pestana == ConfiguracionUI.Pestana.Orbita) objetivo = planeta;
            else if (pestana == ConfiguracionUI.Pestana.Lanzador) objetivo = plataforma;
            else if (pestana == ConfiguracionUI.Pestana.Satelite) { objetivo = nave; objetivo.campo = campoSatelite; }
            else if (moduloEnFoco != null && Time.unscaledTime < finFoco) objetivo = EncuadreModulo(moduloEnFoco);
            else moduloEnFoco = null;

            // La nave no se ve mientras se mira el planeta (estaría delante, flotando sin sentido).
            MostrarNave(pestana != ConfiguracionUI.Pestana.Orbita);
            GirarSatelite(enConfiguracion && pestana == ConfiguracionUI.Pestana.Satelite);
            ResaltarCohete(config.LanzadorId);

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

        /// <summary>Etiqueta «INSTALADO» sobre el módulo enfocado, siguiendo su posición en pantalla.</summary>
        void ColocarEtiqueta()
        {
            if (etiquetaFoco == null) return;
            bool visible = moduloEnFoco != null && Time.unscaledTime < finFoco;
            if (etiquetaFoco.gameObject.activeSelf != visible) etiquetaFoco.gameObject.SetActive(visible);
            if (!visible) return;
            var pantalla = camara.WorldToScreenPoint(CentroVisual(moduloEnFoco));
            if (pantalla.z <= 0f) return;
            var padre = (RectTransform)etiquetaFoco.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, pantalla, null, out var local))
                etiquetaFoco.anchoredPosition = local + new Vector2(0f, 90f);
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
                textoFoco.text = Iconos.DeInstrumento(nuevo) + "  " + catalogo.Instrumento(nuevo).nombre.ToUpperInvariant() +
                                 "\n<size=75%><color=#94A3B8>INSTALADO AQUÍ</color></size>";
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

        void ResaltarCohete(string lanzadorId)
        {
            Activar(resaltadoExplorador, lanzadorId == "explorador");
            Activar(resaltadoTitan, lanzadorId == "titan");
            Girar(coheteExplorador, lanzadorId == "explorador");
            Girar(coheteTitan, lanzadorId == "titan");
        }

        static void Activar(GameObject go, bool valor)
        {
            if (go != null && go.activeSelf != valor) go.SetActive(valor);
        }

        static void Girar(GameObject cohete, bool elegido)
        {
            if (cohete == null) return;
            var giro = cohete.GetComponent<GiroLento>();
            if (giro != null) giro.enabled = elegido;
        }
    }
}
