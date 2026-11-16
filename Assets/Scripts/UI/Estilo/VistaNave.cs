using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SpaceMission
{
    /// <summary>
    /// «Tu nave» durante la Operación: una vista pequeña y fija de la nave en órbita (la misma que se ve en la
    /// cinemática y en los escaneos, con la carga elegida), sobre una esquina del mapa. Al encender o apagar un
    /// instrumento, la vista se acerca a ese instrumento unos segundos y lo nombra.
    /// - No tapa los clics del mapa (solo su cabecera es un botón, que la pliega).
    /// - Se aparta sola: mientras el cursor está sobre el mapa, la vista se desvanece y deja ver esa esquina
    ///   (el jugador va a elegir una región o a mirar una marca); al salir del mapa vuelve.
    /// - Se dibuja cuatro veces por segundo con una cámara propia, apagada el resto del tiempo (rendimiento).
    /// - Se oculta mientras la cinemática, un escaneo o el paso del mes usan la escena orbital.
    /// Solo presentación: lee la operación, no la cambia.
    /// </summary>
    public class VistaNave : MonoBehaviour
    {
        [SerializeField] Camera camara;
        [SerializeField] EscenarioOrbital escenario;
        [SerializeField] RawImage pantalla;
        [SerializeField] GameObject cuerpo;          // lo que se pliega (la pantalla y el rótulo)
        [SerializeField] TMP_Text rotulo;
        [SerializeField] Button cabecera;
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] DirectorCinematica director;
        [SerializeField] SecuenciaEscaneo escaneo;
        [SerializeField] TransicionMes transicion;
        [Tooltip("El mapa sobre el que está la vista: con el cursor dentro, la vista se desvanece.")]
        [SerializeField] RectTransform zonaMapa;
        [SerializeField] CanvasGroup grupoCuerpo;
        [SerializeField] float cadencia = 0.25f;
        [SerializeField] float segundosDeFoco = 3.5f;

        RenderTexture textura;
        readonly HashSet<string> encendidos = new HashSet<string>();
        bool hayEstado, plegada;
        string enFoco;
        float finFoco, proximo;
        Vector3 posicion;
        bool colocada;

        /// <summary>Instrumento que la vista está enfocando (null si muestra la nave entera).</summary>
        public string InstrumentoEnFoco => enFoco != null && Time.unscaledTime < finFoco ? enFoco : null;
        public bool Plegada => plegada;
        /// <summary>¿Está apartada porque el cursor está sobre el mapa?</summary>
        public bool Apartada { get; private set; }

        /// <summary>Aparta o devuelve la vista según dónde esté el cursor (en pantalla). Público para las pruebas.</summary>
        public void MirarCursor(Vector2? cursor)
        {
            Apartada = cursor.HasValue && zonaMapa != null && RectTransformUtility.RectangleContainsScreenPoint(zonaMapa, cursor.Value, null);
            if (grupoCuerpo == null) return;
            float destino = Apartada ? 0f : 1f;
            grupoCuerpo.alpha = Preferencias.ReducirMovimiento || !Application.isPlaying
                ? destino : Mathf.MoveTowards(grupoCuerpo.alpha, destino, Time.unscaledDeltaTime * 6f);
        }

        void Awake()
        {
            if (cabecera != null) cabecera.onClick.AddListener(Alternar);
            if (camara != null) camara.enabled = false;
        }

        void OnDestroy()
        {
            if (textura != null) { textura.Release(); Destroy(textura); }
        }

        public void Alternar()
        {
            plegada = !plegada;
            if (cuerpo != null) cuerpo.SetActive(!plegada);
        }

        /// <summary>Compara los instrumentos encendidos con los de la última vez y enfoca el que cambió.</summary>
        public void LeerOperacion(Operacion op)
        {
            if (op == null) { hayEstado = false; encendidos.Clear(); return; }
            if (hayEstado)
            {
                var cambio = op.Encendidos.FirstOrDefault(id => !encendidos.Contains(id)) ?? encendidos.FirstOrDefault(id => !op.Encendidos.Contains(id));
                if (cambio != null) Enfocar(cambio, op.Encendidos.Contains(cambio));
            }
            encendidos.Clear();
            foreach (var id in op.Encendidos) encendidos.Add(id);
            hayEstado = true;
        }

        void Enfocar(string id, bool encendido)
        {
            enFoco = id;
            finFoco = Time.unscaledTime + segundosDeFoco;
            proximo = 0f;
            if (rotulo != null && catalogo != null)
                rotulo.text = Iconos.DeInstrumento(id) + "  " + catalogo.Instrumento(id).Nombre.ToUpperInvariant() + " · " +
                              (encendido ? Idioma.T("SWITCHED ON") : Idioma.T("SWITCHED OFF"));
        }

        void LateUpdate()
        {
            if (camara == null || escenario == null || escenario.Satelite == null) return;
            LeerOperacion(EstadoJuego.Operacion);
            bool ocupada = (director != null && director.Reproduciendo) || (escaneo != null && escaneo.Activa) || (transicion != null && transicion.Activa);
            MirarCursor(Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2?)null);
            bool visible = !plegada && !ocupada && EstadoJuego.Operacion != null && (grupoCuerpo == null || grupoCuerpo.alpha > 0.01f);
            if (pantalla != null && pantalla.enabled != visible) pantalla.enabled = visible;
            camara.enabled = false;
            if (!visible) { colocada = false; return; }
            if (InstrumentoEnFoco == null && rotulo != null && enFoco != null) { enFoco = null; rotulo.text = ""; }
            if (Time.unscaledTime < proximo) return;
            proximo = Time.unscaledTime + cadencia;
            Encuadrar();
            if (textura == null)
            {
                textura = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32) { name = "VistaNave" };
                textura.Create();
            }
            camara.targetTexture = textura;
            camara.aspect = 1f;
            if (pantalla != null) pantalla.texture = textura;
            camara.enabled = true;   // dibuja este cuadro; el siguiente LateUpdate la apaga
        }

        void Encuadrar()
        {
            var sat = escenario.Satelite;
            var fuera = (sat.position - escenario.Centro).normalized;
            var lado = Vector3.Cross(fuera, Vector3.up).normalized;
            if (lado.sqrMagnitude < 0.01f) lado = Vector3.right;
            Vector3 mira = sat.position;
            float distancia = Mathf.Max(0.6f, Tamano(sat) * 1.15f);
            var foco = InstrumentoEnFoco != null ? sat.Find("Modulo_" + InstrumentoEnFoco) : null;
            Vector3 direccion = (fuera * 0.55f + lado * 0.8f + Vector3.up * 0.3f).normalized;
            if (foco != null && foco.gameObject.activeInHierarchy)
            {
                mira = Centro(foco);
                distancia = Mathf.Max(0.25f, Tamano(foco) * 2.6f);
                // Del lado del instrumento, para que no lo tape el cuerpo de la nave.
                direccion = ((mira - sat.position).normalized * 0.9f + direccion * 0.6f).normalized;
            }
            var destino = mira + direccion * distancia;
            // Cambio de encuadre suave (sin deriva: el destino solo depende de la nave y del foco).
            posicion = colocada && !Preferencias.ReducirMovimiento ? Vector3.Lerp(posicion, destino, 0.45f) : destino;
            colocada = true;
            camara.transform.position = posicion;
            camara.transform.rotation = Quaternion.LookRotation(mira - posicion, Vector3.up);
            camara.nearClipPlane = Mathf.Max(0.02f, distancia * 0.05f);
        }

        static float Tamano(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 1f;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return Mathf.Max(b.size.x, b.size.y, b.size.z);
        }

        static Vector3 Centro(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return t.position;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b.center;
        }
    }
}
