using UnityEngine;

namespace SpaceMission
{
    /// <summary>Sprite que siempre mira a la cámara que lo dibuja (resplandor de los motores en la cinemática).</summary>
    public class MirarCamara : MonoBehaviour
    {
        void OnWillRenderObject()
        {
            var cam = Camera.current;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
