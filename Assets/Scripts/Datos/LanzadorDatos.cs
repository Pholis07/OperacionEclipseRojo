using UnityEngine;

namespace SpaceMission
{
    /// <summary>Datos de un lanzador.</summary>
    [CreateAssetMenu(fileName = "Lanzador", menuName = "SpaceMission/Lanzador")]
    public class LanzadorDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        public int costoCr;
        public int capacidadKg;
        public int espacios;
        [TextArea] public string descripcion;
    }
}
