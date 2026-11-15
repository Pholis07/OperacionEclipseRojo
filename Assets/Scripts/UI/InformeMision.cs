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

        /// <param name="op">La Operación jugada; si es null (pruebas antiguas), todo cuenta como recibido bien.</param>
        public static Informe Construir(Evaluacion e, Configuracion c, CatalogoDatos catalogo,
            Operacion op = null, MisionDatos nivel = null)
        {
            var categoria = e.Categoria ?? CategoriaResultado.Fracaso;
            var informe = new Informe
            {
                Titulo = TextosResultado.Titulo(categoria),
                Texto = TextosResultado.Texto(categoria),
                Recursos = Recursos(e, c, catalogo, nivel) + TextoOperacion(op)
            };

            foreach (var id in e.Transmisiones)
            {
                var t = catalogo.Transmision(id);
                if (string.IsNullOrEmpty(t.instrumentoId)) continue; // TX-00 es de la misión
                var instrumento = catalogo.Instrumento(t.instrumentoId);
                var linea = instrumento.nombre + ": " + instrumento.pistaAsociada + "." +
                            (op != null && op.BajaCalidad.Contains(id) ? " (datos de baja calidad)" : "");
                if (t.tipo == TipoTransmision.Principal) informe.Evidencias.Add(linea);
                else informe.Revelaciones.Add(linea);
            }

            foreach (var id in e.Ausentes)
                informe.Faltantes.Add(catalogo.Transmision(id).texto);

            return informe;
        }

        public static string Recursos(Evaluacion e, Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var r = e.Calculo;
            var m = nivel != null ? nivel : catalogo.mision;
            var lanzador = catalogo.Lanzador(c.LanzadorId);
            return "Usaste " + TextosBriefing.Numero(r.CostoCr) + " de " + TextosBriefing.Numero(m.presupuestoCr) + " créditos, " +
                   TextosBriefing.Numero(r.MasaKg) + " de " + TextosBriefing.Numero(lanzador.capacidadKg) + " kg, " +
                   TextosBriefing.Numero(r.PotenciaW) + " de " + TextosBriefing.Numero(m.potenciaDisponibleW) + " W y " +
                   r.EspaciosUsados + " de " + lanzador.espacios + " espacios.";
        }

        static string TextoOperacion(Operacion op)
        {
            if (op == null) return "";
            var texto = " Te quedaron " + op.Orbitas + " órbitas de navegación.";
            if (op.Apagones > 0)
                texto += " La nave se apagó " + op.Apagones + (op.Apagones == 1 ? " vez" : " veces") +
                         " y los reinicios costaron " + TextosBriefing.Numero(op.CreditosPerdidos) + " créditos.";
            if (op.ConMemoria)
            {
                if (op.Satelite != null) texto += " Satélite: " + op.Satelite.nombre + ".";
                if (op.CreditosCarga > 0)
                    texto += " La carga de herramientas costó " + TextosBriefing.Numero(op.CreditosCarga) + " créditos.";
                texto += " Te quedaron " + op.Combustible + " de combustible.";
                if (op.PeticionAceptada)
                    texto += op.PeticionCumplida ? " Cumpliste la petición de la NASA." : " No llegaste a cumplir la petición de la NASA.";
                var objetivos = Objetivos.Resumen(op);
                if (objetivos.Length > 0) texto += " " + objetivos;
            }
            return texto;
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
