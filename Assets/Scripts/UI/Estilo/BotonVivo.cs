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

        Vector3 escalaBase = Vector3.one;
        Coroutine animacion;
        bool encima;

        void Awake() => escalaBase = transform.localScale;

        void OnDisable()
        {
            encima = false;
            transform.localScale = escalaBase;
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
            if (encima == valor) return;
            encima = valor;
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
