using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Datos de una órbita. Los valores viven en los assets, no en el código.</summary>
    [CreateAssetMenu(fileName = "Orbita", menuName = "SpaceMission/Órbita")]
    public class OrbitaDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        [TextArea] public string descripcionBreve;
        public int costoCr;
        [TextArea] public string ventaja;
        [TextArea] public string limitacion;

        [Header("Dato real de la NASA en que se basa (versión 5)")]
        [TextArea] public string datoNasa;     // inglés
        [TextArea] public string datoNasaEs;   // español
        public string fuenteNasa;              // fuente corta, p. ej. «NASA/JPL (MRO)»

        /// <summary>Ids de instrumento que hacen falta para la victoria completa.</summary>
        public List<string> requisitoDeVictoria = new List<string>();

        /// <summary>Ids de instrumento complementario cuya revelación desbloquea esta órbita.</summary>
        public List<string> revelacionesDesbloqueadas = new List<string>();
    }
}
