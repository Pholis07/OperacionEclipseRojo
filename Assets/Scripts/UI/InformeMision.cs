using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Arma el informe final a partir de la evaluación: conclusión, qué instrumento dio cada evidencia,
    /// revelaciones complementarias, qué faltó y por qué, y recursos usados. Sin puntos ni calificación.
    /// </summary>
    public static class InformeMision
    {
        public class Informe
        {
            public string Titulo;
            public string Texto;
            public readonly List<string> Evidencias = new List<string>();   // principales obtenidas
            public readonly List<string> Revelaciones = new List<string>(); // complementarias desbloqueadas
            public readonly List<string> Faltantes = new List<string>();    // textos TX-A
            public string Recursos;
        }

        public static Informe Construir(Evaluacion e, Configuracion c, CatalogoDatos catalogo)
        {
            var categoria = e.Categoria ?? CategoriaResultado.Fracaso;
            var informe = new Informe
            {
                Titulo = TextosResultado.Titulo(categoria),
                Texto = TextosResultado.Texto(categoria),
                Recursos = Recursos(e, c, catalogo)
            };

            foreach (var id in e.Transmisiones)
            {
                var t = catalogo.Transmision(id);
                if (string.IsNullOrEmpty(t.instrumentoId)) continue; // TX-00 es de la misión
                var instrumento = catalogo.Instrumento(t.instrumentoId);
                var linea = instrumento.nombre + ": " + instrumento.pistaAsociada + ".";
                if (t.tipo == TipoTransmision.Principal) informe.Evidencias.Add(linea);
                else informe.Revelaciones.Add(linea);
            }

            foreach (var id in e.Ausentes)
                informe.Faltantes.Add(catalogo.Transmision(id).texto);

            return informe;
        }

        public static string Recursos(Evaluacion e, Configuracion c, CatalogoDatos catalogo)
        {
            var r = e.Calculo;
            var m = catalogo.mision;
            var lanzador = catalogo.Lanzador(c.LanzadorId);
            return "Usaste " + TextosBriefing.Numero(r.CostoCr) + " de " + TextosBriefing.Numero(m.presupuestoCr) + " créditos, " +
                   TextosBriefing.Numero(r.MasaKg) + " de " + TextosBriefing.Numero(lanzador.capacidadKg) + " kg, " +
                   TextosBriefing.Numero(r.PotenciaW) + " de " + TextosBriefing.Numero(m.potenciaDisponibleW) + " W y " +
                   r.EspaciosUsados + " de " + lanzador.espacios + " espacios.";
        }

        /// <summary>Encabezado de una transmisión en la fase Investigación.</summary>
        public static string Encabezado(string transmisionId, int indice, int total, CatalogoDatos catalogo)
        {
            var t = catalogo.Transmision(transmisionId);
            var origen = string.IsNullOrEmpty(t.instrumentoId) ? "Inicio de la observación" : catalogo.Instrumento(t.instrumentoId).nombre;
            return "Transmisión " + (indice + 1) + " de " + total + "  ·  " + origen;
        }
    }
}
