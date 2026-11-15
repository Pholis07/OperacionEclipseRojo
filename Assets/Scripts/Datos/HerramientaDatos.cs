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

        [Header("Carga (versión 3): se paga con lo que sobra de la configuración")]
        public int costoCr;
        public int masaKg;

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Nombre => Idioma.T(nombre);
        public string Descripcion => Idioma.T(descripcion);
    }
}
