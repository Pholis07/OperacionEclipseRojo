using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Convierte una evaluación en lo que se ve: textos de las barras, colores del semáforo, ayudas y resumen.
    /// Sin MonoBehaviours, para poder probarlo. Todos los números salen de los datos o del cálculo.
    /// </summary>
    public static class PresentacionMision
    {
        public struct Barra
        {
            public string Nombre;
            public string Texto;     // por ejemplo «620 / 650 kg»
            public float Fraccion;   // 0 a 1 (se recorta en 1 si se supera)
            public bool Excedida;
        }

        public const string SinValor = "—";

        public static Barra[] Barras(Evaluacion e, Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var r = e.Calculo;
            var m = nivel != null ? nivel : catalogo.mision;
            var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            bool sinCalculo = r.Estado == EstadoSemaforo.Gris; // en Gris no se calcula (diseño, casos B1, B7 y B8)
            return new[]
            {
                Hacer("Mass", r.MasaKg, lanzador != null ? lanzador.capacidadKg : (int?)null, "kg", sinCalculo, r.LimitesSuperados.Contains(Limite.Masa)),
                Hacer("Power", r.PotenciaW, m.potenciaDisponibleW, "W", sinCalculo, r.LimitesSuperados.Contains(Limite.Potencia)),
                Hacer("Cost", r.CostoCr, m.presupuestoCr, "cr", sinCalculo, r.LimitesSuperados.Contains(Limite.Costo)),
                Hacer("Slots", r.EspaciosUsados, lanzador != null ? lanzador.espacios : (int?)null, "", sinCalculo, r.LimitesSuperados.Contains(Limite.Espacios)),
            };
        }

        static Barra Hacer(string nombre, int valor, int? limite, string unidad, bool sinCalculo, bool excedida)
        {
            var texto = (sinCalculo ? SinValor : TextosBriefing.Numero(valor)) + " / " +
                        (limite.HasValue ? TextosBriefing.Numero(limite.Value) : SinValor) +
                        (unidad == "" ? "" : " " + unidad);
            float fraccion = sinCalculo || !limite.HasValue || limite.Value <= 0 ? 0f : Mathf.Min(1f, (float)valor / limite.Value);
            return new Barra { Nombre = nombre, Texto = texto, Fraccion = fraccion, Excedida = excedida };
        }

        public static Color ColorSemaforo(EstadoSemaforo estado)
        {
            switch (estado)
            {
                case EstadoSemaforo.Rojo: return Hex("#EF4444");
                case EstadoSemaforo.Amarillo: return Hex("#EAB308");
                case EstadoSemaforo.Verde: return Hex("#22C55E");
                default: return Hex("#9CA3AF");
            }
        }

        public static string NombreEstado(EstadoSemaforo estado)
        {
            switch (estado)
            {
                case EstadoSemaforo.Verde: return "GREEN";
                case EstadoSemaforo.Amarillo: return "YELLOW";
                case EstadoSemaforo.Rojo: return "RED";
                default: return "GREY";
            }
        }

        // ---------- Opciones y ayuda ----------

        // Las opciones son tarjetas de dos líneas: nombre arriba y «chips» de datos con icono abajo.
        // La casilla (elegida o no) la antepone ConfiguracionUI.
        const string Cian = "#5EC8E5";

        static string Chip(string icono, string valor) => "<color=" + Cian + ">" + icono + "</color> " + valor;

        static string Tarjeta(string icono, string nombre, params string[] chips) =>
            "<indent=1.5em><color=" + Cian + ">" + icono + "</color> <b>" + nombre + "</b>\n" +
            "<size=89%><color=#94A3B8>" + string.Join("     ", chips) + "</color></size></indent>";

        // El icono de cada órbita va en su color (el mismo de su línea en el planeta).
        public static string TextoOpcion(OrbitaDatos o) =>
            Tarjeta("<color=" + Trayectoria.ColorHex(o.id) + ">" + Iconos.Orbita + "</color>", o.nombre,
                Chip(Iconos.Costo, TextosBriefing.Numero(o.costoCr) + " cr"));

        public static string TextoOpcion(LanzadorDatos l) =>
            Tarjeta(Iconos.Lanzador, l.nombre, Chip(Iconos.Costo, TextosBriefing.Numero(l.costoCr) + " cr"),
                Chip(Iconos.Masa, TextosBriefing.Numero(l.capacidadKg) + " kg"), Chip(Iconos.Espacios, l.espacios + " slots"));

        public static string TextoOpcion(InstrumentoDatos i) =>
            Tarjeta(Iconos.DeInstrumento(i.id), i.nombre, Chip(Iconos.Masa, i.masaKg + " kg"),
                Chip(Iconos.Potencia, i.ConsumoTotalW + " W"), Chip(Iconos.Costo, i.costoCr + " cr"));

        public static string TextoOpcion(SateliteDatos s) =>
            Tarjeta(Iconos.Satelite, s.nombre, Chip(Iconos.Informe, s.inspiradoEn));

        static string Dato(string icono, string nombre, string valor) =>
            "\n<color=" + Cian + ">" + icono + "</color> <b>" + nombre + ":</b> " + valor;

        public static string Ayuda(OrbitaDatos o) =>
            "<b>" + o.nombre + "</b>\n" + o.descripcionBreve + "\n<b>Advantage:</b> " + o.ventaja +
            "\n<b>Limitation:</b> " + o.limitacion + Dato(Iconos.Costo, "Cost", o.costoCr + " credits") + Nasa(o.datoNasa, o.fuenteNasa);

        public static string Ayuda(LanzadorDatos l) =>
            "<b>" + l.nombre + "</b>\n" + l.descripcion + Dato(Iconos.Costo, "Cost", l.costoCr + " credits") +
            Dato(Iconos.Masa, "Maximum mass", TextosBriefing.Numero(l.capacidadKg) + " kg") + Dato(Iconos.Espacios, "Slots", l.espacios.ToString()) +
            Nasa(l.datoNasa, l.fuenteNasa);

        public static string Ayuda(InstrumentoDatos i) =>
            "<b>" + i.nombre + "</b>\n" + i.textoAyuda + Dato(Iconos.Masa, "Mass", i.masaKg + " kg") +
            Dato(Iconos.Potencia, "Power", i.ConsumoTotalW + " W") + Dato(Iconos.Costo, "Cost", i.costoCr + " credits") +
            Nasa(i.datoNasa, i.fuenteNasa);

        public static string Ayuda(SateliteDatos s) =>
            "<b>" + s.nombre + "</b>\n" + s.descripcion + Dato(Iconos.Informe, "Based on", s.inspiradoEn) + Nasa(s.datoNasa, s.fuenteNasa) +
            "\n<size=85%><color=#94A3B8>" + s.credito + ". Advantage and drawback: see Cargo.</color></size>";

        /// <summary>El dato real de la NASA en que se basa la opción, en color de datos y con su fuente.</summary>
        public static string Nasa(string dato, string fuente) =>
            string.IsNullOrEmpty(dato) ? "" :
            "\n<color=#75D6DF><b>NASA data:</b> " + dato + "</color>" +
            (string.IsNullOrEmpty(fuente) ? "" : " <size=80%><color=#94A3B8>(" + fuente + ")</color></size>");

        // ---------- Revisión ----------

        public static string Resumen(Evaluacion e, Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var orbita = string.IsNullOrEmpty(c.OrbitaId) ? null : catalogo.Orbita(c.OrbitaId);
            var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            var instrumentos = c.InstrumentoIds.Select(id => catalogo.Instrumento(id).nombre).ToList();

            var lineas = new List<string>
            {
                "<b>Orbit:</b> " + (orbita != null ? orbita.nombre : "not chosen"),
                "<b>Launcher:</b> " + (lanzador != null ? lanzador.nombre : "not chosen"),
                "<b>Instruments:</b> " + (instrumentos.Count > 0 ? string.Join(", ", instrumentos) : "none"),
                ""
            };
            foreach (var b in Barras(e, c, catalogo, nivel))
                lineas.Add("<b>" + b.Nombre + ":</b> " + (b.Excedida ? "<color=#EF4444>" + b.Texto + "</color>" : b.Texto));
            lineas.Add("");
            lineas.Add("<b>STATUS:</b> <color=#" + ColorUtility.ToHtmlStringRGB(ColorSemaforo(e.Estado)) + ">" +
                       NombreEstado(e.Estado) + "</color>");
            lineas.Add(e.Mensaje);
            return string.Join("\n", lineas);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
