using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Arma el informe final: conclusión según lo que llegó a la Tierra (ResultadoCientifico), qué instrumento
    /// dio cada evidencia recibida, revelaciones complementarias, qué faltó y por qué, y recursos usados.
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

        /// <param name="op">La Operación jugada; si es null, no se envió nada.</param>
        public static Informe Construir(Evaluacion e, Configuracion c, CatalogoDatos catalogo,
            Operacion op = null, MisionDatos nivel = null)
        {
            var categoria = ResultadoCientifico.Categoria(e, op, catalogo);
            var informe = new Informe
            {
                Titulo = TextosResultado.Titulo(categoria),
                Texto = categoria == e.Categoria ? TextosResultado.Texto(categoria) : TextosResultado.TextoSinEnviar,
                Recursos = Recursos(e, c, catalogo, nivel) + TextoOperacion(op)
            };

            foreach (var id in e.Transmisiones)
            {
                var t = catalogo.Transmision(id);
                if (string.IsNullOrEmpty(t.instrumentoId)) continue; // TX-00 es de la misión
                var instrumento = catalogo.Instrumento(t.instrumentoId);
                if (op == null || !op.Recibidas.Contains(id))
                {
                    // Lo que no se observó con calidad suficiente o no se envió no cuenta como evidencia.
                    informe.Faltantes.Add(instrumento.Nombre + Idioma.T(": no valid evidence reached Earth."));
                    continue;
                }
                var linea = instrumento.Nombre + ": " + instrumento.PistaAsociada + ".";
                if (t.tipo == TipoTransmision.Principal) informe.Evidencias.Add(linea);
                else informe.Revelaciones.Add(linea);
            }

            foreach (var id in e.Ausentes)
                informe.Faltantes.Add(catalogo.Transmision(id).Texto);

            return informe;
        }

        public static string Recursos(Evaluacion e, Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var r = e.Calculo;
            var m = nivel != null ? nivel : catalogo.mision;
            var lanzador = catalogo.Lanzador(c.LanzadorId);
            return Idioma.T("You used ") + TextosBriefing.Numero(r.CostoCr) + Idioma.T(" of ") + TextosBriefing.Numero(m.presupuestoCr) + Idioma.T(" credits, ") +
                   TextosBriefing.Numero(r.MasaKg) + Idioma.T(" of ") + TextosBriefing.Numero(lanzador.capacidadKg) + " kg, " +
                   TextosBriefing.Numero(r.PotenciaW) + Idioma.T(" of ") + TextosBriefing.Numero(m.potenciaDisponibleW) + Idioma.T(" W and ") +
                   r.EspaciosUsados + Idioma.T(" of ") + lanzador.espacios + Idioma.T(" slots.");
        }

        static string TextoOperacion(Operacion op)
        {
            if (op == null) return "";
            var texto = Idioma.T(" The mission lasted ") + op.MesesResueltos + (op.MesesResueltos == 1 ? Idioma.T(" month") : Idioma.T(" months")) + Idioma.T(", with ") + op.MesesRestantes + Idioma.T(" left over.");
            if (op.Apagones > 0)
                texto += Idioma.T(" The spacecraft shut down ") + (op.Apagones == 1 ? Idioma.T("once") : op.Apagones + Idioma.T(" times")) +
                         Idioma.T(" and the restarts cost ") + TextosBriefing.Numero(op.CreditosPerdidos) + Idioma.T(" credits.");
            if (op.Satelite != null) texto += Idioma.T(" Satellite: ") + op.Satelite.Nombre + ".";
            if (op.CreditosCarga > 0)
                texto += Idioma.T(" The cargo cost ") + TextosBriefing.Numero(op.CreditosCarga) + Idioma.T(" credits.");
            if (op.CreditosManiobras > 0)
                texto += Idioma.T(" The maneuvers cost ") + TextosBriefing.Numero(op.CreditosManiobras) + Idioma.T(" credits.");
            texto += Idioma.T(" You had ") + op.Combustible + Idioma.T(" fuel left.");
            if (op.Memoria.Count > 0)
                texto += " " + op.Memoria.Count + (op.Memoria.Count == 1 ? Idioma.T(" record was") : Idioma.T(" records were")) + Idioma.T(" left unsent in memory.");
            if (op.PeticionAceptada)
                texto += op.PeticionCumplida ? Idioma.T(" You fulfilled the NASA request.") : Idioma.T(" You did not fulfill the NASA request.");
            var objetivos = Objetivos.Resumen(op);
            if (objetivos.Length > 0) texto += " " + objetivos;
            return texto;
        }

        /// <summary>Encabezado de una transmisión en la fase Investigación.</summary>
        public static string Encabezado(string transmisionId, int indice, int total, CatalogoDatos catalogo)
        {
            var t = catalogo.Transmision(transmisionId);
            var origen = string.IsNullOrEmpty(t.instrumentoId) ? Idioma.T("Start of observation") : catalogo.Instrumento(t.instrumentoId).Nombre;
            return Idioma.T("Transmission ") + (indice + 1) + Idioma.T(" of ") + total + "  ·  " + origen;
        }
    }
}
