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
        [Header("Modo seguro (un evento deja el consumo por encima del límite)")]
        public int creditosPorReinicio;
        public int diasPerdidosPorApagon;   // días útiles que pierde el mes siguiente

        [Header("Meses (versión 4)")]
        public int diasPorMes;
        public int creditosPorManiobra;     // cambiar de altitud: créditos, combustible y días de ese mes
        public int diasPorManiobra;
        public int diasPerdidosPorOpcion;       // por cada «órbita» que costaba una respuesta a un evento
        public int diasPerdidosSinCombustible;  // la nave sube sola desde la órbita baja
        public float calidadBase;           // calidad de un registro con el mes completo (0 a 100)
        public float calidadMinima;         // por debajo, lo observado no es evidencia válida
        public float factorCalidadTormenta; // tormenta creciendo, si se sigue
        public float factorCalidadBaja;     // en órbita baja

        [Header("Observación (distancias en un mapa de 0 a 1)")]
        public float radioDeteccion;   // a esta distancia o menos, el instrumento encuentra la anomalía
        public float distanciaCaliente;
        public float distanciaTibio;   // más lejos de esto: frío

        [Header("Puntos")]
        public int puntosVictoria;
        public int puntosParcial;
        public int puntosFracaso;
        public int creditosPorPunto;   // +1 punto por cada N créditos sobrantes
        public int puntosPorRevelacion;
        public int puntosPorOrbitaSobrante;   // por cada mes que sobra al terminar
        public int puntosPorApagon;        // se restan

        [Header("Carga y objetivos (versión 3)")]
        public int espaciosBodega;         // herramientas que caben en la nave
        public int kgPorCombustible;       // 1 unidad de combustible por cada N kg libres del lanzador
        public int combustibleMaximo;
        public int objetivosPorMision;

        [Header("Combustible y altitud (versión 3)")]
        public int combustiblePorCambioAltitud;
        public float factorAltitudBaja;          // en baja, radio y distancias de lectura se multiplican por esto
        public int combustiblePorOrbitaBaja;     // arrastre, por mes
        public int puntosPorCombustible;

        [Header("Alcance de las órbitas (versión 6): frenado real al llegar a Marte")]
        public float velocidadLlegadaKmS;   // velocidad con que la nave llega a Marte (v∞)
        public int impulsoEspecificoS;      // Isp de los motores de la nave
        [TextArea] public string datoNasaAlcance;     // inglés
        [TextArea] public string datoNasaAlcanceEs;   // español
        public string fuenteNasaAlcance;
        public string DatoNasaAlcance => Idioma.Espanol && !string.IsNullOrEmpty(datoNasaAlcanceEs) ? datoNasaAlcanceEs : datoNasaAlcance;

        [Header("Memoria y envío a Tierra (versión 3)")]
        public int cicloContacto;      // la ventana de contacto se abre 1 mes de cada N (con 2: los meses pares)
        public int vatiosTransmisor;   // consumo extra mientras se envía
    }
}
