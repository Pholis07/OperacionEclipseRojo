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
            botonAbrir.onClick.AddListener(() => Mostrar(true));
            botonCerrar.onClick.AddListener(() => Mostrar(false));
        }

        UnityEngine.UI.Toggle pantallaCompleta;

        /// <summary>
        /// Versión 7: interruptor «Pantalla completa». La escena Menu se edita a mano, así que se crea aquí copiando
        /// la casilla de «Reducir movimiento» y poniéndola justo debajo.
        /// </summary>
        void CrearPantallaCompleta()
        {
            if (reducirMovimiento == null || pantallaCompleta != null) return;
            pantallaCompleta = Instantiate(reducirMovimiento, reducirMovimiento.transform.parent);
            pantallaCompleta.name = "PantallaCompleta";
            pantallaCompleta.transform.SetSiblingIndex(reducirMovimiento.transform.GetSiblingIndex() + 1);
            pantallaCompleta.onValueChanged = new UnityEngine.UI.Toggle.ToggleEvent();
            var etiqueta = pantallaCompleta.GetComponentInChildren<TMPro.TMP_Text>();
            if (etiqueta != null) etiqueta.text = "Full screen";   // TraductorEscena lo pasa al español
            pantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
            pantallaCompleta.onValueChanged.AddListener(v => Screen.fullScreen = v);
        }

        void Mostrar(bool visible)
        {
            if (visible && pantallaCompleta != null) pantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);   // pudo cambiar con Esc o F11
            panel.SetActive(visible);
            var foco = visible ? (GameObject)musica.gameObject : botonAbrir.gameObject;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(foco);
        }
    }
}
