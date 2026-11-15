using UnityEngine;

namespace SpaceMission
{
    /// <summary>Panel «Opciones» del Menú: volúmenes, «Silenciar todo», «Reducir movimiento» y el idioma de los subtítulos.</summary>
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
        [Tooltip("Subtítulos: English, Español y Off, en ese orden.")]
        [SerializeField] UnityEngine.UI.Button[] botonesSubtitulos;
        [SerializeField] Color colorElegido = new Color(0.89f, 0.39f, 0.23f);
        [SerializeField] Color colorNormal = new Color(0.118f, 0.118f, 0.137f);

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

            if (botonesSubtitulos != null)
                for (int i = 0; i < botonesSubtitulos.Length; i++)
                {
                    var idioma = (IdiomaSubtitulos)i;
                    if (botonesSubtitulos[i] == null) continue;
                    botonesSubtitulos[i].onClick.AddListener(() => { Preferencias.Subtitulos = idioma; PintarSubtitulos(); });
                }
            PintarSubtitulos();

            botonAbrir.onClick.AddListener(() => Mostrar(true));
            botonCerrar.onClick.AddListener(() => Mostrar(false));
        }

        /// <summary>El idioma elegido se ve con el color de acento.</summary>
        void PintarSubtitulos()
        {
            if (botonesSubtitulos == null) return;
            for (int i = 0; i < botonesSubtitulos.Length; i++)
            {
                var b = botonesSubtitulos[i];
                if (b == null) continue;
                bool elegido = (int)Preferencias.Subtitulos == i;
                var img = b.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = elegido ? colorElegido : colorNormal;
                var t = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (t != null) t.color = elegido ? new Color(0.043f, 0.043f, 0.063f) : new Color(0.97f, 0.98f, 0.99f);
            }
        }

        void Mostrar(bool visible)
        {
            panel.SetActive(visible);
            var foco = visible ? (GameObject)musica.gameObject : botonAbrir.gameObject;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(foco);
        }
    }
}
