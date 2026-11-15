using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceMission
{
    /// <summary>
    /// Respuesta de los botones: crecen un poco con el cursor o el foco del teclado, se hunden al pulsar
    /// y suenan al pulsar (pasar el cursor o recibir el foco es silencioso). No cambia lo que hace el botón.
    /// </summary>
    public class BotonVivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [SerializeField] float escalaEncima = 1.03f;
        [SerializeField] float escalaPulsado = 0.97f;
        [SerializeField] ArteDatos.Sonido sonidoPulsar = ArteDatos.Sonido.Click;

        /// <summary>
        /// Botón bajo el cursor o con el foco del teclado (null al salir). Lo usa, por ejemplo, la cámara de
        /// Configuración para mostrar dónde se instalaría un instrumento antes de elegirlo.
        /// </summary>
        public static event System.Action<GameObject> AlCambiarEncima;
        public static GameObject Encimado { get; private set; }

        Vector3 escalaBase = Vector3.one;
        Coroutine animacion;
        bool encima;

        void Awake() => escalaBase = transform.localScale;

        void OnDisable()
        {
            encima = false;
            transform.localScale = escalaBase;
            if (Encimado == gameObject) Avisar(null);
        }

        static void Avisar(GameObject go)
        {
            if (Encimado == go) return;
            Encimado = go;
            AlCambiarEncima?.Invoke(go);
        }

        bool Activo
        {
            get
            {
                var boton = GetComponent<UnityEngine.UI.Selectable>();
                return boton == null || boton.IsInteractable();
            }
        }

        public void OnPointerEnter(PointerEventData e) => Encima(true);
        public void OnPointerExit(PointerEventData e) => Encima(false);
        public void OnSelect(BaseEventData e) => Encima(true);
        public void OnDeselect(BaseEventData e) => Encima(false);

        public void OnPointerDown(PointerEventData e)
        {
            if (Activo) Escalar(escalaPulsado, 0.08f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!Activo) return;
            ReproductorUI.Sonar(sonidoPulsar);
            Escalar(encima ? escalaEncima : 1f, 0.12f);
        }

        public void OnSubmit(BaseEventData e)
        {
            if (Activo) ReproductorUI.Sonar(sonidoPulsar);
        }

        void Encima(bool valor)
        {
            if (valor) Avisar(gameObject);
            else if (Encimado == gameObject) Avisar(null);
            if (encima == valor) return;
            encima = valor;
            // Versión 7: sonido muy suave al pasar el cursor por un botón activo (ui_hover existía sin usarse).
            if (valor && Activo && Application.isPlaying) ReproductorUI.Sonar(ArteDatos.Sonido.Hover, 0.35f);
            Escalar(valor && Activo ? escalaEncima : 1f, 0.12f);
        }

        void Escalar(float objetivo, float duracion)
        {
            var desde = transform.localScale;
            var hasta = escalaBase * objetivo;
            Animar.Reemplazar(this, ref animacion,
                Animar.Durante(duracion, t => transform.localScale = Vector3.LerpUnclamped(desde, hasta, t)));
        }
    }
}
