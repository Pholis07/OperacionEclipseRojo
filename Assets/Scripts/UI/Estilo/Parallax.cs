using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>Mueve un objeto 3D un poco según la posición del ratón (profundidad, como en los menús de Starfield).</summary>
    public class Parallax : MonoBehaviour
    {
        [SerializeField] Vector2 amplitud = new Vector2(0.4f, 0.25f);

        Vector3 origen;

        void Awake() => origen = transform.localPosition;

        void Update()
        {
            if (Preferencias.ReducirMovimiento || Mouse.current == null)
            {
                transform.localPosition = origen;
                return;
            }
            var p = Mouse.current.position.ReadValue();
            var n = new Vector2(p.x / Mathf.Max(1, Screen.width) - 0.5f, p.y / Mathf.Max(1, Screen.height) - 0.5f);
            var destino = origen + new Vector3(-n.x * amplitud.x, -n.y * amplitud.y, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, destino, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        }
    }
}
