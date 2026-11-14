using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>Escena Menu: «Iniciar misión» y el panel «Cómo jugar».</summary>
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
            panelComoJugar.SetActive(visible);
            var foco = visible ? botonCerrarComoJugar : botonComoJugar;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(foco.gameObject);
        }

        public void IniciarMision()
        {
            EstadoJuego.Reiniciar();
            SceneManager.LoadScene("Mision");
        }
    }
}
