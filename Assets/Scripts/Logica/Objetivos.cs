using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>Objetivos secundarios (PROPUESTA-GAMEPLAY.md v3): elegirlos al azar y saber si se cumplieron.</summary>
    public static class Objetivos
    {
        public static List<ObjetivoDatos> Elegir(CatalogoDatos catalogo, System.Random azar) =>
            catalogo.objetivos.OrderBy(_ => azar.Next())
                .Take(System.Math.Min(catalogo.reglas.objetivosPorMision, catalogo.objetivos.Count)).ToList();

        /// <summary>«Objetivos extra: Ningún apagón (cumplido), … (no cumplido).» Vacío si la misión no tiene objetivos.</summary>
        public static string Resumen(Operacion op) =>
            op.Objetivos.Count == 0 ? ""
                : "Objetivos extra: " + string.Join(", ", op.Objetivos.Select(o =>
                    o.texto + (Cumplido(o, op) ? " (cumplido)" : " (no cumplido)"))) + ".";

        /// <summary>Se evalúa al terminar la misión.</summary>
        public static bool Cumplido(ObjetivoDatos o, Operacion op)
        {
            switch (o.tipo)
            {
                case TipoObjetivo.EscaneosMaximos: return op.BajaCalidad.Count == 0 && op.EscaneosAlCompletar > 0 && op.EscaneosAlCompletar <= o.valor;
                case TipoObjetivo.SinApagones: return op.Apagones == 0;
                case TipoObjetivo.CombustibleMinimo: return op.Combustible >= o.valor;
                case TipoObjetivo.EnviarAntesDeLaUltima: return op.BajaCalidad.Count == 0 && op.OrbitasAlCompletar > 1;
                case TipoObjetivo.SinHerramientas: return op.HerramientasUsadas.Count == 0;
            }
            return false;
        }
    }
}
