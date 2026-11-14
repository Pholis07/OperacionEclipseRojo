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

            var categoria = e.Categoria ?? CategoriaResultado.Fracaso;
            int puntosResultado = categoria == CategoriaResultado.Victoria ? reglas.puntosVictoria
                : categoria == CategoriaResultado.Parcial ? reglas.puntosParcial : reglas.puntosFracaso;
            lineas.Add(new Linea { Concepto = TextosResultado.Titulo(categoria), Puntos = puntosResultado });

            int sobrante = nivel.presupuestoCr - e.Calculo.CostoCr - op.CreditosPerdidos - op.CreditosCarga;
            if (sobrante > 0)
                lineas.Add(new Linea
                {
                    Concepto = "Presupuesto sobrante (" + TextosBriefing.Numero(sobrante) + " créditos)",
                    Puntos = sobrante / reglas.creditosPorPunto
                });

            int revelaciones = op.Recibidas.Count(id =>
            {
                var t = catalogo.Transmision(id);
                return !string.IsNullOrEmpty(t.instrumentoId) && t.tipo == TipoTransmision.Complementaria;
            });
            if (revelaciones > 0)
                lineas.Add(new Linea { Concepto = "Revelaciones complementarias (" + revelaciones + ")", Puntos = revelaciones * reglas.puntosPorRevelacion });

            if (op.Orbitas > 0)
                lineas.Add(new Linea { Concepto = "Órbitas sobrantes (" + op.Orbitas + ")", Puntos = op.Orbitas * reglas.puntosPorOrbitaSobrante });

            if (op.Combustible > 0)
                lineas.Add(new Linea { Concepto = "Combustible sobrante (" + op.Combustible + ")", Puntos = op.Combustible * reglas.puntosPorCombustible });

            if (op.BajaCalidad.Count > 0)
                lineas.Add(new Linea { Concepto = "Datos de baja calidad (" + op.BajaCalidad.Count + ")", Puntos = -op.BajaCalidad.Count * reglas.puntosPorBajaCalidad });

            if (op.Apagones > 0)
                lineas.Add(new Linea { Concepto = "Apagones de la nave (" + op.Apagones + ")", Puntos = -op.Apagones * reglas.puntosPorApagon });

            return lineas;
        }

        public static int Total(IEnumerable<Linea> lineas) => System.Math.Max(0, lineas.Sum(l => l.Puntos));
    }
}
