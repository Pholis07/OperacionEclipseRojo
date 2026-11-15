using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 5, bloque 5: dos botones «English» y «Español». El idioma elegido se resalta con el color de acento
    /// y se guarda (Idioma). Está en la pantalla de título y en Opciones.
    /// </summary>
    public class SelectorIdioma : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button botonIngles;
        [SerializeField] UnityEngine.UI.Button botonEspanol;

        static readonly Color Activo = new Color32(0xE2, 0x64, 0x3A, 0xFF);
        static readonly Color Inactivo = new Color32(0x1E, 0x1E, 0x23, 0xFF);

        void Awake()
        {
            botonIngles.onClick.AddListener(() => Idioma.Espanol = false);
            botonEspanol.onClick.AddListener(() => Idioma.Espanol = true);
        }

        void OnEnable()
        {
            Idioma.AlCambiar += Pintar;
            Pintar();
        }

        void OnDisable() => Idioma.AlCambiar -= Pintar;

        public void Pintar()
        {
            botonIngles.GetComponent<UnityEngine.UI.Image>().color = Idioma.Espanol ? Inactivo : Activo;
            botonEspanol.GetComponent<UnityEngine.UI.Image>().color = Idioma.Espanol ? Activo : Inactivo;
        }
    }
}
