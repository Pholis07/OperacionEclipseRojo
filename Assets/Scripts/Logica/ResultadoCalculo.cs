using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>
    /// Lo que devuelve el Calculador. Versión 6: aunque falte algo (Gris) se calculan los parciales con lo elegido,
    /// para que las barras se muevan desde el primer paso.
    /// </summary>
    public class ResultadoCalculo
    {
        public int MasaKg;            // masa seca: base + satélite + instrumentos
        public int MasaSateliteKg;    // la parte del satélite (la Carga la descuenta si se cambia por el MRO)
        public int DeltaVNecesarioMs; // frenado que pide la órbita (0 sin órbita o sin datos)
        public int DeltaVDisponibleMs;// frenado que tiene la nave (0 sin lanzador o sin datos)
        public bool AlcanceCalculado; // hay lanzador y órbita con datos
        public int PotenciaW;
        public int CostoCr;
        public int EspaciosUsados;
        public EstadoSemaforo Estado;
        public readonly List<Limite> LimitesSuperados = new List<Limite>();
        public readonly List<Faltante> Faltantes = new List<Faltante>();
    }
}
