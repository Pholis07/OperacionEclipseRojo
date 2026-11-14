using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Carga de la nave (PROPUESTA-GAMEPLAY.md v3, paso 1): entre Revisión y Lanzar se eligen las herramientas
    /// que suben, pagando solo con lo que sobra de la configuración (créditos y capacidad del lanzador).
    /// La capacidad que queda libre se convierte en combustible. No cambia la evaluación ni la categoría.
    /// </summary>
    public class Carga
    {
        readonly CatalogoDatos catalogo;
        readonly ReglasOperacionDatos reglas;
        readonly int creditosSobrantes;
        readonly int masaLibreKg;

        public readonly List<string> Herramientas = new List<string>();

        public Carga(Evaluacion evaluacion, Configuracion config, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            this.catalogo = catalogo;
            reglas = catalogo.reglas;
            nivel = nivel != null ? nivel : catalogo.mision;
            var lanzador = catalogo.Lanzador(config.LanzadorId);
            creditosSobrantes = System.Math.Max(0, nivel.presupuestoCr - evaluacion.Calculo.CostoCr);
            masaLibreKg = lanzador == null ? 0 : System.Math.Max(0, lanzador.capacidadKg - evaluacion.Calculo.MasaKg);
        }

        public int CreditosGastados => Herramientas.Sum(id => catalogo.Herramienta(id).costoCr);
        public int CreditosDisponibles => creditosSobrantes - CreditosGastados;
        public int MasaLibreKg => masaLibreKg - Herramientas.Sum(id => catalogo.Herramienta(id).masaKg);
        public int EspaciosLibres => reglas.espaciosBodega - Herramientas.Count;

        /// <summary>Combustible con el que sale la nave: 1 unidad por cada N kg libres, con tope.</summary>
        public int Combustible => System.Math.Min(reglas.combustibleMaximo, MasaLibreKg / reglas.kgPorCombustible);

        public bool Lleva(string id) => Herramientas.Contains(id);

        public bool PuedeCargar(string id)
        {
            var h = catalogo.Herramienta(id);
            return h != null && !Lleva(id) && EspaciosLibres > 0
                   && h.costoCr <= CreditosDisponibles && h.masaKg <= MasaLibreKg;
        }

        public bool Cargar(string id)
        {
            if (!PuedeCargar(id)) return false;
            Herramientas.Add(id);
            return true;
        }

        public void Quitar(string id) => Herramientas.Remove(id);
    }
}
