using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>Abre y cierra un panel (por ejemplo, Créditos) con sus botones; Escape también lo cierra.</summary>
    public class AbrirPanel : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button abrir;
        [SerializeField] UnityEngine.UI.Button cerrar;
        [SerializeField] GameObject panel;

        void Awake()
        {
            if (abrir != null) abrir.onClick.AddListener(() => Mostrar(true));
            if (cerrar != null) cerrar.onClick.AddListener(() => Mostrar(false));
        }

        void Update()
        {
            if (panel != null && panel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Mostrar(false);
        }

        public void Mostrar(bool visible)
        {
            if (panel == null) return;
            panel.SetActive(visible);
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            if (visible && cerrar != null) es.SetSelectedGameObject(cerrar.gameObject);
            else if (!visible && abrir != null) es.SetSelectedGameObject(abrir.gameObject);
        }
    }
}
