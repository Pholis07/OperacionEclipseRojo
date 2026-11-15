using UnityEngine;

namespace SpaceMission
{
    /// <summary>Panel «Opciones» del Menú: volumen de música, ambiente, efectos y voz, «Silenciar todo» y «Reducir movimiento».</summary>
    public class OpcionesUI : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button botonAbrir;
        [SerializeField] UnityEngine.UI.Button botonCerrar;
        [SerializeField] GameObject panel;
        [SerializeField] UnityEngine.UI.Slider musica;
        [SerializeField] UnityEngine.UI.Slider efectos;
        [SerializeField] UnityEngine.UI.Slider voz;
        [SerializeField] UnityEngine.UI.Toggle reducirMovimiento;
        [SerializeField] UnityEngine.UI.Slider ambiente;
        [SerializeField] UnityEngine.UI.Toggle silencio;
        // Versión 9: el constructor ya las crea en su columna. Si faltan (escena vieja), se clonan como antes.
        [SerializeField] UnityEngine.UI.Toggle pantallaCompleta;
        [SerializeField] UnityEngine.UI.Toggle contornos;
        [SerializeField] UnityEngine.UI.Toggle consejos;

        void Awake()
        {
            panel.SetActive(false);
            musica.SetValueWithoutNotify(Preferencias.VolumenMusica);
            efectos.SetValueWithoutNotify(Preferencias.VolumenEfectos);
            voz.SetValueWithoutNotify(Preferencias.VolumenVoz);
            reducirMovimiento.SetIsOnWithoutNotify(Preferencias.ReducirMovimiento);

            musica.onValueChanged.AddListener(v => Preferencias.VolumenMusica = v);
            efectos.onValueChanged.AddListener(v => Preferencias.VolumenEfectos = v);
            // Una sola preescucha al soltar el deslizador, no una por cada paso.
            var alSoltar = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            alSoltar.callback.AddListener(_ => ReproductorUI.Sonar(ArteDatos.Sonido.Click));
            efectos.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>().triggers.Add(alSoltar);
            voz.onValueChanged.AddListener(v => Preferencias.VolumenVoz = v);
            reducirMovimiento.onValueChanged.AddListener(v => Preferencias.ReducirMovimiento = v);
            if (ambiente != null)
            {
                ambiente.SetValueWithoutNotify(Preferencias.VolumenAmbiente);
                ambiente.onValueChanged.AddListener(v => Preferencias.VolumenAmbiente = v);
            }
            if (silencio != null)
            {
                silencio.SetIsOnWithoutNotify(Preferencias.Silencio);
                silencio.onValueChanged.AddListener(v => Preferencias.Silencio = v);
            }

            CrearPantallaCompleta();
            CrearContornos();
            CrearConsejos();
            foreach (var casilla in panel.GetComponentsInChildren<UnityEngine.UI.Toggle>(true)) ArreglarMarca(casilla);
            botonAbrir.onClick.AddListener(() => Mostrar(true));
            botonCerrar.onClick.AddListener(() => Mostrar(false));
        }

        /// <summary>
        /// Versión 7: interruptor «Pantalla completa». La escena Menu se edita a mano, así que se crea aquí copiando
        /// la casilla de «Reducir movimiento» y poniéndola justo debajo.
        /// </summary>
        void CrearPantallaCompleta()
        {
            if (pantallaCompleta == null)
            {
                if (reducirMovimiento == null) return;
                pantallaCompleta = Clonar("PantallaCompleta", "Full screen", reducirMovimiento);
            }
            pantallaCompleta.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
            pantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
            pantallaCompleta.onValueChanged.AddListener(v => Screen.fullScreen = v);
        }

        /// <summary>
        /// Versión 8: la marca de las casillas tenía tamaño negativo (anclada a un punto con −12 de tamaño) y no se
        /// veía nunca, estuviera marcada o no. Se estira dentro de su caja con un margen.
        /// </summary>
        /// <summary>Copia de «Reducir movimiento» justo después de «despuesDe» (solo con escenas sin la casilla).</summary>
        UnityEngine.UI.Toggle Clonar(string nombre, string etiqueta, UnityEngine.UI.Toggle despuesDe)
        {
            var copia = Instantiate(reducirMovimiento, reducirMovimiento.transform.parent);
            copia.name = nombre;
            copia.transform.SetSiblingIndex(despuesDe.transform.GetSiblingIndex() + 1);
            var texto = copia.GetComponentInChildren<TMPro.TMP_Text>();
            if (texto != null) texto.text = etiqueta;   // TraductorEscena lo pasa al español
            return copia;
        }

        static void ArreglarMarca(UnityEngine.UI.Toggle casilla)
        {
            if (casilla == null || casilla.graphic == null || casilla.graphic is TMPro.TMP_Text) return;   // el ✓ (versión 9) ya tiene su tamaño
            var marca = casilla.graphic.rectTransform;
            marca.anchorMin = Vector2.zero;
            marca.anchorMax = Vector2.one;
            marca.offsetMin = new Vector2(7f, 7f);
            marca.offsetMax = new Vector2(-7f, -7f);
        }

        /// <summary>
        /// Versión 8 (Eclipse Ink): interruptor «Contornos de tinta». Apagado es el modo ligero (sin la pasada de
        /// contorno). Se crea por código, como «Pantalla completa», porque la escena Menu se edita a mano.
        /// </summary>
        void CrearContornos()
        {
            if (contornos == null)
            {
                if (reducirMovimiento == null) return;
                contornos = Clonar("ContornosTinta", "Ink outlines", pantallaCompleta != null ? pantallaCompleta : reducirMovimiento);
            }
            contornos.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
            contornos.SetIsOnWithoutNotify(Preferencias.ContornosTinta);
            contornos.onValueChanged.AddListener(v => Preferencias.ContornosTinta = v);
        }

        /// <summary>
        /// Interruptor «Show tips»: los consejos de cada pantalla, la guía paso a paso de la Operación y el resaltado.
        /// Apagados, el juego no guía a quien no quiere; el botón «?» y la Misión 0 siguen disponibles.
        /// </summary>
        void CrearConsejos()
        {
            if (consejos == null)
            {
                if (reducirMovimiento == null) return;
                consejos = Clonar("Consejos", "Show tips", contornos != null ? contornos : reducirMovimiento);
            }
            consejos.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
            consejos.SetIsOnWithoutNotify(Preferencias.GuiaActiva);
            consejos.onValueChanged.AddListener(v => Preferencias.GuiaActiva = v);
        }

        void Mostrar(bool visible)
        {
            if (visible && consejos != null) consejos.SetIsOnWithoutNotify(Preferencias.GuiaActiva);   // pudo cambiar en el juego
            if (visible && pantallaCompleta != null) pantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);   // pudo cambiar con Esc o F11
            panel.SetActive(visible);
            var foco = visible ? (GameObject)musica.gameObject : botonAbrir.gameObject;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(foco);
        }
    }
}
