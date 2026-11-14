using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>Lo que devuelve el Calculador. En Gris no se calculan masa, potencia ni costo (quedan en 0).</summary>
    public class ResultadoCalculo
    {
        public int MasaKg;
        public int PotenciaW;
        public int CostoCr;
        public int EspaciosUsados;
        public EstadoSemaforo Estado;
        public readonly List<Limite> LimitesSuperados = new List<Limite>();
        public readonly List<Faltante> Faltantes = new List<Faltante>();
    }
}
