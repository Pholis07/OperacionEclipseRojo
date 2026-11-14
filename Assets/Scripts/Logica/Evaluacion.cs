using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>Lo que devuelve el Evaluador: semáforo final, mensaje, resultado y transmisiones.</summary>
    public class Evaluacion
    {
        public ResultadoCalculo Calculo;

        /// <summary>Gris, Rojo, Amarillo o Verde (nunca Valida).</summary>
        public EstadoSemaforo Estado;

        /// <summary>Texto del semáforo para el jugador.</summary>
        public string Mensaje;

        /// <summary>Solo Amarillo y Verde se lanzan.</summary>
        public bool SePuedeLanzar;

        /// <summary>Resultado de la misión; null si no se puede lanzar.</summary>
        public CategoriaResultado? Categoria;

        /// <summary>Ids de los instrumentos principales elegidos (térmica, radar).</summary>
        public readonly List<string> PistasPrincipales = new List<string>();

        /// <summary>Ids de los instrumentos que pide la órbita para la victoria y faltan.</summary>
        public readonly List<string> EvidenciasFaltantes = new List<string>();

        /// <summary>Ids de transmisión que se muestran en Investigación, en orden.</summary>
        public readonly List<string> Transmisiones = new List<string>();

        /// <summary>Ids de transmisión de instrumentos ausentes que se listan en el informe.</summary>
        public readonly List<string> Ausentes = new List<string>();

        /// <summary>[Misión] masa=… potencia=… costo=… semáforo=… resultado=…</summary>
        public string LineaRegistro;
    }
}
