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
            botonComoJugar.onClick.AddListener(() => panelComoJugar.SetActive(true));
            botonCerrarComoJugar.onClick.AddListener(() => panelComoJugar.SetActive(false));
            panelComoJugar.SetActive(false);
        }

        public void IniciarMision()
        {
            EstadoJuego.Reiniciar();
            SceneManager.LoadScene("Mision");
        }
    }
}
