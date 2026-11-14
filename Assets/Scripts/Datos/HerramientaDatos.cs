using UnityEngine;

namespace SpaceMission
{
    /// <summary>Herramienta de la nave. Se puede usar una vez por misión para resolver un evento.</summary>
    [CreateAssetMenu(fileName = "Herramienta", menuName = "SpaceMission/Herramienta")]
    public class HerramientaDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        [TextArea] public string descripcion;
    }
}
