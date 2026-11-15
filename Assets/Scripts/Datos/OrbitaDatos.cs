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

        [Tooltip("Altitudes reales de la órbita (km sobre Marte). Con ellas se calcula el frenado que pide (Alcance).")]
        public int periapsisKm;
        public int apoapsisKm;
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

        /// <summary>
        /// Versión 7: instrumentos que desde esta órbita no pueden trabajar (la areoestacionaria queda demasiado lejos
        /// para el radar y la cámara de detalle). Aunque se instalen, no dan evidencia ni cuentan para el resultado.
        /// </summary>
        public List<string> instrumentosSinAlcance = new List<string>();
        [TextArea] public string motivoSinAlcance;   // inglés; se explica en el semáforo y en la ficha
        public string MotivoSinAlcance => Idioma.T(motivoSinAlcance);

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Nombre => Idioma.T(nombre);
        public string DescripcionBreve => Idioma.T(descripcionBreve);
        public string Ventaja => Idioma.T(ventaja);
        public string Limitacion => Idioma.T(limitacion);
        public string DatoNasa => Idioma.Espanol && !string.IsNullOrEmpty(datoNasaEs) ? datoNasaEs : datoNasa;
    }
}
