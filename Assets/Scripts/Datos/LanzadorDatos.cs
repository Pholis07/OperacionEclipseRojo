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

        [Header("Dato real de la NASA en que se basa (versión 5)")]
        [TextArea] public string datoNasa;     // inglés
        [TextArea] public string datoNasaEs;   // español
        public string fuenteNasa;              // fuente corta, p. ej. «NASA/JPL (MRO)»

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Nombre => Idioma.T(nombre);
        public string Descripcion => Idioma.T(descripcion);
        public string DatoNasa => Idioma.Espanol && !string.IsNullOrEmpty(datoNasaEs) ? datoNasaEs : datoNasa;
    }
}
