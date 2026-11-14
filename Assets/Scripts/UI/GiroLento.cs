using UnityEngine;

namespace SpaceMission
{
    /// <summary>Decoración del Menú: gira el objeto despacio sobre el eje vertical.</summary>
    public class GiroLento : MonoBehaviour
    {
        [SerializeField] float gradosPorSegundo = 6f;

        void Update() => transform.Rotate(0f, gradosPorSegundo * Time.deltaTime, 0f, Space.World);
    }
}
