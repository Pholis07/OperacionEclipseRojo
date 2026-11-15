using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>Objetivos secundarios (PROPUESTA-GAMEPLAY.md v3): elegirlos al azar y saber si se cumplieron.</summary>
    public static class Objetivos
    {
        /// <param name="combustibleInicial">Sin él no salen objetivos de combustible imposibles (no se puede ganar combustible).</param>
        public static List<ObjetivoDatos> Elegir(CatalogoDatos catalogo, System.Random azar, int combustibleInicial = int.MaxValue) =>
            catalogo.objetivos.Where(o => o.tipo != TipoObjetivo.CombustibleMinimo || combustibleInicial >= o.valor)
                .OrderBy(_ => azar.Next())
                .Take(System.Math.Min(catalogo.reglas.objetivosPorMision, catalogo.objetivos.Count)).ToList();

        /// <summary>«Objetivos extra: Ningún apagón (cumplido), … (no cumplido).» Vacío si la misión no tiene objetivos.</summary>
        public static string Resumen(Operacion op) =>
            op.Objetivos.Count == 0 ? ""
                : "Bonus objectives: " + string.Join(", ", op.Objetivos.Select(o =>
                    o.texto + (Cumplido(o, op) ? " (done)" : " (not done)"))) + ".";

        /// <summary>Se evalúa al terminar la misión.</summary>
        public static bool Cumplido(ObjetivoDatos o, Operacion op)
        {
            switch (o.tipo)
            {
                case TipoObjetivo.EscaneosMaximos: return op.ObservacionesAlCompletar > 0 && op.ObservacionesAlCompletar <= o.valor;
                case TipoObjetivo.SinApagones: return op.Apagones == 0;
                case TipoObjetivo.CombustibleMinimo: return op.Combustible >= o.valor;
                case TipoObjetivo.EnviarAntesDeLaUltima: return op.MesesAlCompletar >= 1;   // no en el último mes
                case TipoObjetivo.SinHerramientas: return op.HerramientasUsadas.Count == 0;
            }
            return false;
        }
    }
}
