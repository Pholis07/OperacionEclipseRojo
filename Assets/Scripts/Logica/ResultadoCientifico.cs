using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Resultado científico de la misión (versión 4): la configuración da la capacidad y la Operación da el
    /// resultado. Solo cuenta la evidencia que se observó con calidad suficiente y llegó a la Tierra.
    /// - Victoria: la configuración podía ganar y se recibió la evidencia de todos los instrumentos que exige la órbita.
    /// - Parcial: se recibió al menos una evidencia principal (térmica o radar).
    /// - Fracaso: ninguna evidencia principal recibida.
    /// </summary>
    public static class ResultadoCientifico
    {
        /// <param name="op">La Operación jugada. Con null (misión sin operar) nada se envió: fracaso.</param>
        public static CategoriaResultado Categoria(Evaluacion e, Operacion op, CatalogoDatos catalogo)
        {
            if (op == null || e.Categoria == null) return CategoriaResultado.Fracaso;

            bool Recibida(string instrumentoId) => op.Recibidas.Any(id => catalogo.Transmision(id).instrumentoId == instrumentoId);

            if (e.Categoria == CategoriaResultado.Victoria && e.Requeridos.All(Recibida)) return CategoriaResultado.Victoria;
            return e.PistasPrincipales.Any(Recibida) ? CategoriaResultado.Parcial : CategoriaResultado.Fracaso;
        }
    }
}
