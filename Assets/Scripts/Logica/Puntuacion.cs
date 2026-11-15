using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Puntos de la misión con su desglose (diseño aprobado, PROPUESTA-GAMEPLAY.md v2; revierte la decisión 7).
    /// Los valores salen de ReglasOperacionDatos.
    /// </summary>
    public static class Puntuacion
    {
        public struct Linea
        {
            public string Concepto;
            public int Puntos;
        }

        public static List<Linea> Desglose(Evaluacion e, Operacion op, CatalogoDatos catalogo, MisionDatos nivel)
        {
            var reglas = catalogo.reglas;
            nivel = nivel != null ? nivel : catalogo.mision;
            var lineas = new List<Linea>();

            var categoria = ResultadoCientifico.Categoria(e, op, catalogo);
            int puntosResultado = categoria == CategoriaResultado.Victoria ? reglas.puntosVictoria
                : categoria == CategoriaResultado.Parcial ? reglas.puntosParcial : reglas.puntosFracaso;
            lineas.Add(new Linea { Concepto = TextosResultado.Titulo(categoria), Puntos = puntosResultado });

            // Lo que sobra (presupuesto, meses y combustible) solo premia si llegó alguna evidencia: abandonar pronto no debe dar puntos.
            bool envioEvidencia = op.Recibidas.Any(id => !string.IsNullOrEmpty(catalogo.Transmision(id).instrumentoId));

            int sobrante = op.CreditosDisponibles;
            if (envioEvidencia && sobrante > 0)
                lineas.Add(new Linea
                {
                    Concepto = "Budget left over (" + TextosBriefing.Numero(sobrante) + " credits)",
                    Puntos = sobrante / reglas.creditosPorPunto
                });

            int revelaciones = op.Recibidas.Count(id =>
            {
                var t = catalogo.Transmision(id);
                return !string.IsNullOrEmpty(t.instrumentoId) && t.tipo == TipoTransmision.Complementaria;
            });
            if (revelaciones > 0)
                lineas.Add(new Linea { Concepto = "Complementary findings (" + revelaciones + ")", Puntos = revelaciones * reglas.puntosPorRevelacion });

            if (envioEvidencia && op.MesesRestantes > 0)
                lineas.Add(new Linea { Concepto = "Months left over (" + op.MesesRestantes + ")", Puntos = op.MesesRestantes * reglas.puntosPorOrbitaSobrante });

            foreach (var o in op.Objetivos.Where(o => Objetivos.Cumplido(o, op)))
                lineas.Add(new Linea { Concepto = "Bonus objective: " + o.texto, Puntos = o.puntos });

            if (op.PeticionCumplida)
                lineas.Add(new Linea { Concepto = "NASA request fulfilled", Puntos = op.PuntosPeticion });

            if (envioEvidencia && op.Combustible > 0)
                lineas.Add(new Linea { Concepto = "Fuel left over (" + op.Combustible + ")", Puntos = op.Combustible * reglas.puntosPorCombustible });

            if (op.Apagones > 0)
                lineas.Add(new Linea { Concepto = "Spacecraft blackouts (" + op.Apagones + ")", Puntos = -op.Apagones * reglas.puntosPorApagon });

            return lineas;
        }

        public static int Total(IEnumerable<Linea> lineas) => System.Math.Max(0, lineas.Sum(l => l.Puntos));
    }
}
