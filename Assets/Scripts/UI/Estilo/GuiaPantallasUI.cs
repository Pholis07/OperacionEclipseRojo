using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 6: ayuda del modo normal, encima de las vistas de la escena Mision (sin cambiar su lógica).
    /// - Tarjeta con bombilla: el siguiente paso de la pantalla (GuiaPantallas) y un contorno que late en el control
    ///   que toca. Cambia de sitio según la pantalla. Se oculta con «×» y se vuelve a mostrar desde la ayuda.
    /// - Botón «?» siempre visible: abre la explicación completa de la pantalla.
    /// En la Operación no hay tarjeta (ya tiene su guía encima del mapa), solo el «?». Durante la cinemática, nada.
    /// </summary>
    public class GuiaPantallasUI : MonoBehaviour
    {
        [SerializeField] MisionUI mision;
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] CargaUI carga;
        [SerializeField] DirectorCinematica cinematica;
        [SerializeField] RectTransform raiz;          // tarjeta + «?», se mueve según la pantalla
        [SerializeField] GameObject tarjeta;
        [SerializeField] TMP_Text texto;
        [SerializeField] UnityEngine.UI.Button botonOcultar;
        [SerializeField] UnityEngine.UI.Button botonAyuda;
        [SerializeField] GameObject panelAyuda;
        [SerializeField] TMP_Text textoAyuda;
        [SerializeField] UnityEngine.UI.Button botonAlternarGuia;
        [SerializeField] UnityEngine.UI.Button botonCerrarAyuda;
        [SerializeField] GameObject pistaBriefing;    // la pista fija del Briefing: la tarjeta ocupa su sitio

        ResaltarGuia resaltado;
        string firma;

        public ConsejoPantalla Consejo { get; private set; }
        public bool TarjetaVisible => tarjeta != null && tarjeta.activeSelf && raiz.gameObject.activeSelf;

        void Awake()
        {
            botonOcultar.onClick.AddListener(() => { Preferencias.GuiaActiva = false; firma = null; });
            botonAyuda.onClick.AddListener(AbrirAyuda);
            botonCerrarAyuda.onClick.AddListener(() => panelAyuda.SetActive(false));
            botonAlternarGuia.onClick.AddListener(() =>
            {
                Preferencias.GuiaActiva = !Preferencias.GuiaActiva;
                firma = null;
                panelAyuda.SetActive(false);
            });
            panelAyuda.SetActive(false);
            Idioma.AlCambiar += Olvidar;
        }

        void OnDestroy() => Idioma.AlCambiar -= Olvidar;

        void Olvidar() => firma = null;

        public void AbrirAyuda()
        {
            textoAyuda.text = GuiaPantallas.Ayuda(mision.PantallaActual);
            botonAlternarGuia.GetComponentInChildren<TMP_Text>().text =
                Preferencias.GuiaActiva ? Idioma.T("Hide tips") : Idioma.T("Show tips");
            panelAyuda.SetActive(true);
        }

        void Update() => Actualizar();

        /// <summary>Recalcula el consejo y lo dibuja si cambió. Público para las pruebas en modo edición.</summary>
        public void Actualizar()
        {
            bool enCine = cinematica != null && cinematica.Reproduciendo;
            if (raiz.gameObject.activeSelf == enCine) raiz.gameObject.SetActive(!enCine);
            if (enCine) { panelAyuda.SetActive(false); ResaltarGuia.Mover(ref resaltado, null); return; }

            var pantalla = mision.PantallaActual;
            var consejo = Calcular(pantalla);
            bool conTarjeta = Preferencias.GuiaActiva && pantalla != PantallaMision.Operacion && !EstadoJuego.Tutorial;
            var nuevaFirma = pantalla + "|" + conTarjeta + "|" + consejo.Texto + "|" + consejo.Destino;
            if (nuevaFirma == firma && (resaltado != null || consejo.Destino == null || !conTarjeta)) return;
            firma = nuevaFirma;
            Consejo = consejo;

            Colocar(pantalla);
            tarjeta.SetActive(conTarjeta);
            if (pistaBriefing != null) pistaBriefing.SetActive(!conTarjeta);
            texto.text = "<color=#75D6DF>" + Iconos.Pista + "</color>  " + consejo.Texto;
            ResaltarGuia.Mover(ref resaltado, conTarjeta ? Buscar(mision.Vista(pantalla), consejo.Destino) : null);
        }

        ConsejoPantalla Calcular(PantallaMision pantalla)
        {
            switch (pantalla)
            {
                case PantallaMision.Briefing: return GuiaPantallas.Briefing();
                case PantallaMision.Configuracion:
                    return GuiaPantallas.Configuracion(EstadoJuego.Configuracion, configuracion.PestanaActual, configuracion.Ultima);
                case PantallaMision.Revision: return GuiaPantallas.Revision(configuracion.Ultima);
                case PantallaMision.Carga: return GuiaPantallas.Carga(carga != null ? carga.Carga : null);
                default: return new ConsejoPantalla("", null);
            }
        }

        /// <summary>La tarjeta va donde no tapa nada: abajo a la izquierda del Briefing, bajo el título de la
        /// Configuración, a la derecha de las tarjetas de Revisión y Carga; en la Operación solo el «?».</summary>
        void Colocar(PantallaMision p)
        {
            switch (p)
            {
                case PantallaMision.Briefing: Anclar(0.05f, 0.03f, 0.42f, 0.13f); break;
                case PantallaMision.Configuracion: Anclar(0.37f, 0.7f, 0.63f, 0.81f); break;
                case PantallaMision.Revision: Anclar(0.765f, 0.42f, 0.985f, 0.62f); break;
                case PantallaMision.Carga: Anclar(0.79f, 0.42f, 0.99f, 0.62f); break;
                default: Anclar(0.66f, 0.925f, 0.69f, 0.975f); break;   // Operación: solo el «?», junto a su guía
            }
        }

        void Anclar(float x0, float y0, float x1, float y1)
        {
            raiz.anchorMin = new Vector2(x0, y0);
            raiz.anchorMax = new Vector2(x1, y1);
            raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        }

        /// <summary>El objeto activo con ese nombre dentro de la vista (con imagen, para que el contorno se vea).</summary>
        static GameObject Buscar(GameObject vista, string nombre)
        {
            if (vista == null || string.IsNullOrEmpty(nombre)) return null;
            foreach (var t in vista.GetComponentsInChildren<RectTransform>(false))
                if (t.name == nombre && t.GetComponent<UnityEngine.UI.Graphic>() != null) return t.gameObject;
            return null;
        }
    }
}
