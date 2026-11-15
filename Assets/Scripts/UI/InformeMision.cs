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

            // Versión 8: los mismos datos, estructurados, para el tablero de detalles (barras, filas y fichas).
            public readonly List<Barra> Barras = new List<Barra>();
            public readonly List<FilaEvidencia> Filas = new List<FilaEvidencia>();
            public readonly List<Ficha> Fichas = new List<Ficha>();
            public readonly List<FilaObjetivo> ObjetivosExtra = new List<FilaObjetivo>();
        }

        /// <summary>Un recurso de la nave: cuánto se usó de su límite.</summary>
        public class Barra
        {
            public string Icono, Nombre, Unidad;
            public int Usado, Limite;
            public float Fraccion => Limite > 0 ? UnityEngine.Mathf.Clamp01((float)Usado / Limite) : 0f;
            public string Texto => Icono + "  " + Nombre + "  <b>" + TextosBriefing.Numero(Usado) + " / " + TextosBriefing.Numero(Limite) +
                                   (string.IsNullOrEmpty(Unidad) ? "" : " " + Unidad) + "</b>";
        }

        /// <summary>Una evidencia que la misión podía conseguir: si llegó a la Tierra y qué dice (o por qué faltó).</summary>
        public class FilaEvidencia
        {
            public string InstrumentoId, Nombre, Detalle;
            public bool Llego;
            public bool Principal;
        }

        /// <summary>Un dato corto de la operación (meses, combustible…).</summary>
        public class Ficha { public string Valor, Etiqueta; }

        public class FilaObjetivo { public string Texto; public bool Cumplido; public int Puntos; }

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
                bool principal = t.tipo == TipoTransmision.Principal;
                if (op == null || !op.Recibidas.Contains(id))
                {
                    // Lo que no se observó con calidad suficiente o no se envió no cuenta como evidencia.
                    informe.Faltantes.Add(instrumento.Nombre + Idioma.T(": no valid evidence reached Earth."));
                    informe.Filas.Add(new FilaEvidencia { InstrumentoId = instrumento.id, Nombre = instrumento.Nombre, Principal = principal,
                        Detalle = Idioma.T("No valid evidence reached Earth.") });
                    continue;
                }
                informe.Filas.Add(new FilaEvidencia { InstrumentoId = instrumento.id, Nombre = instrumento.Nombre, Principal = principal,
                    Llego = true, Detalle = Mayuscula(instrumento.PistaAsociada) + "." });
                var linea = instrumento.Nombre + ": " + instrumento.PistaAsociada + ".";
                if (t.tipo == TipoTransmision.Principal) informe.Evidencias.Add(linea);
                else informe.Revelaciones.Add(linea);
            }

            foreach (var id in e.Ausentes)
            {
                var t = catalogo.Transmision(id);
                informe.Faltantes.Add(t.Texto);
                var instrumento = string.IsNullOrEmpty(t.instrumentoId) ? null : catalogo.Instrumento(t.instrumentoId);
                informe.Filas.Add(new FilaEvidencia { InstrumentoId = t.instrumentoId, Nombre = instrumento != null ? instrumento.Nombre : Idioma.T("Not on board"),
                    Principal = t.tipo == TipoTransmision.Principal, Detalle = t.Texto });
            }

            Tablero(informe, e, c, catalogo, op, nivel);
            return informe;
        }

        static string Mayuscula(string t) => string.IsNullOrEmpty(t) ? t : char.ToUpper(t[0]) + t.Substring(1);

        /// <summary>Versión 8: barras de recursos, fichas de la operación y objetivos extra para el tablero.</summary>
        static void Tablero(Informe informe, Evaluacion e, Configuracion c, CatalogoDatos catalogo, Operacion op, MisionDatos nivel)
        {
            var r = e.Calculo;
            var m = nivel != null ? nivel : catalogo.mision;
            var lanzador = catalogo.Lanzador(c.LanzadorId);
            if (lanzador != null)
            {
                informe.Barras.Add(new Barra { Icono = Iconos.Costo, Nombre = Idioma.T("Credits"), Usado = r.CostoCr, Limite = m.presupuestoCr });
                informe.Barras.Add(new Barra { Icono = Iconos.Masa, Nombre = Idioma.T("Mass"), Usado = r.MasaKg, Limite = lanzador.capacidadKg, Unidad = "kg" });
                informe.Barras.Add(new Barra { Icono = Iconos.Potencia, Nombre = Idioma.T("Power"), Usado = r.PotenciaW, Limite = m.potenciaDisponibleW, Unidad = "W" });
                informe.Barras.Add(new Barra { Icono = Iconos.Espacios, Nombre = Idioma.T("Slots"), Usado = r.EspaciosUsados, Limite = lanzador.espacios });
            }
            if (op == null) return;
            informe.Fichas.Add(new Ficha { Valor = op.MesesResueltos + " / " + (op.MesesResueltos + op.MesesRestantes), Etiqueta = Idioma.T("Months used") });
            informe.Fichas.Add(new Ficha { Valor = op.Combustible.ToString(), Etiqueta = Idioma.T("Fuel left") });
            informe.Fichas.Add(new Ficha { Valor = op.Memoria.Count.ToString(), Etiqueta = Idioma.T("Records unsent") });
            informe.Fichas.Add(new Ficha { Valor = op.Apagones.ToString(), Etiqueta = Idioma.T("Blackouts") });
            foreach (var o in op.Objetivos)
                informe.ObjetivosExtra.Add(new FilaObjetivo { Texto = o.Texto, Cumplido = Objetivos.Cumplido(o, op), Puntos = o.puntos });
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
