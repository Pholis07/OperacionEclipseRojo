using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Reglas de la fase de Operación que no cambian por nivel: apagón, escaneo y puntos.
    /// Diseño aprobado en PROPUESTA-GAMEPLAY.md (versión 2). Los números viven en el asset, no en el código.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglasOperacion", menuName = "SpaceMission/Reglas de operación")]
    public class ReglasOperacionDatos : ScriptableObject
    {
        [Header("Apagón (consumo mayor que el límite)")]
        public int orbitasPerdidasPorApagon;
        public int creditosPorReinicio;

        [Header("Escaneo (distancias en un mapa de 0 a 1)")]
        public int orbitasPorEscaneo;
        public float radioDeteccion;   // a esta distancia o menos, el instrumento encuentra la anomalía
        public float distanciaCaliente;
        public float distanciaTibio;   // más lejos de esto: frío

        [Header("Puntos")]
        public int puntosVictoria;
        public int puntosParcial;
        public int puntosFracaso;
        public int creditosPorPunto;   // +1 punto por cada N créditos sobrantes
        public int puntosPorRevelacion;
        public int puntosPorOrbitaSobrante;
        public int puntosPorBajaCalidad;   // se restan
        public int puntosPorApagon;        // se restan
    }
}
