using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Escena Menu: «Iniciar misión» y el panel «Cómo jugar». Versión 6: los pasos de «Cómo jugar» los pone el código
    /// (la escena Menu se edita a mano y no se regenera), con el orden real del juego: armar la nave, el alcance, operar
    /// por meses, enviar en los meses de contacto y el informe.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button botonIniciar;
        [SerializeField] UnityEngine.UI.Button botonComoJugar;
        [SerializeField] UnityEngine.UI.Button botonCerrarComoJugar;
        [SerializeField] GameObject panelComoJugar;

        void Awake()
        {
            botonIniciar.onClick.AddListener(IniciarMision);
            botonComoJugar.onClick.AddListener(() => MostrarComoJugar(true));
            botonCerrarComoJugar.onClick.AddListener(() => MostrarComoJugar(false));
            panelComoJugar.SetActive(false);
        }

        /// <summary>Abre o cierra «Cómo jugar» y mueve el foco del teclado al botón que queda a la vista.</summary>
        void MostrarComoJugar(bool visible)
        {
            if (visible) LlenarComoJugar();
            panelComoJugar.SetActive(visible);
            var foco = visible ? botonCerrarComoJugar : botonComoJugar;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(foco.gameObject);
        }

        /// <summary>Los pasos de «Cómo jugar», en el idioma actual. Cada uno con su icono en color de datos.</summary>
        public static string TextoComoJugar()
        {
            string Paso(string icono, string texto) => "<color=#5EC8E5>" + icono + "</color>  " + Idioma.T(texto);
            return string.Join("\n", new[]
            {
                Paso(Iconos.Lanzador, "<b>Build your spacecraft</b>: rocket, satellite, instruments and orbit. Keep every bar out of the red."),
                Paso(Iconos.Orbita, "<b>How far can you go?</b> A heavier spacecraft brakes less, so low orbits may be locked. Real NASA physics."),
                Paso(Iconos.Verde, "<b>STATUS light</b>: green = ready, yellow = evidence will be missing, red = over a limit."),
                Paso(Iconos.Mira, "<b>Operate month by month</b>: turn on an instrument, click the map and press «Resolve month»."),
                Paso(Iconos.Senal, "<b>Send to Earth</b> in the contact months: evidence only counts when it arrives."),
                Paso(Iconos.Informe, "<b>Read the report</b>: it says what went well and what to change. Press «?» on any screen for help."),
            });
        }

        /// <summary>Escribe los pasos en el texto «Pasos» del panel (si la escena lo tiene).</summary>
        public void LlenarComoJugar()
        {
            foreach (var t in panelComoJugar.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (t.name != "Pasos") continue;
                t.text = TextoComoJugar();
                t.enableAutoSizing = true;
                t.fontSizeMin = 18;
                t.fontSizeMax = 32;
                t.lineSpacing = 18;
            }
        }

        public void IniciarMision()
        {
            EstadoJuego.Reiniciar();
            SceneManager.LoadScene("Mision");
        }
    }
}
