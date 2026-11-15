using UnityEngine;

namespace SpaceMission
{
    /// <summary>Mueve despacio la zona visible de una RawImage (polvo de la tormenta sobre el mapa). Solo adorno.</summary>
    [RequireComponent(typeof(UnityEngine.UI.RawImage))]
    public class DesplazarUV : MonoBehaviour
    {
        [SerializeField] Vector2 velocidad = new Vector2(0.01f, 0f);

        UnityEngine.UI.RawImage imagen;

        void Awake() => imagen = GetComponent<UnityEngine.UI.RawImage>();

        void Update()
        {
            if (Preferencias.ReducirMovimiento) return;
            var r = imagen.uvRect;
            r.position += velocidad * Time.unscaledDeltaTime;
            imagen.uvRect = r;
        }
    }
}
