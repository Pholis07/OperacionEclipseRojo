using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Tipo de satélite (aspecto de la nave), con un modelo 3D de NASA 3D Resources.
    /// Por ahora solo cambia el aspecto: no tiene masa, potencia ni costo, y no entra en las reglas ni en el
    /// semáforo. Las mecánicas de cada tipo las define el equipo de lógica (Adrian) más adelante.
    /// </summary>
    [CreateAssetMenu(fileName = "Satelite", menuName = "SpaceMission/Satélite")]
    public class SateliteDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        [Tooltip("Misión real de NASA en la que se basa el modelo 3D.")]
        public string inspiradoEn;
        [TextArea] public string descripcion;
        [Tooltip("Crédito del modelo 3D.")]
        public string credito = "Modelo 3D: NASA 3D Resources";
    }
}
