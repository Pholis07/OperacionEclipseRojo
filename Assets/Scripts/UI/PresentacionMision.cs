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
            // Versión 6: los valores se ven desde el primer paso (parciales con lo elegido), aunque el semáforo siga gris.
            bool conFrenado = Alcance.Activo(lanzador, catalogo.reglas);
            return new[]
            {
                Hacer(Idioma.T("Mass"), r.MasaKg, lanzador != null ? lanzador.capacidadKg : (int?)null, "kg", false, r.LimitesSuperados.Contains(Limite.Masa)),
                Hacer(Idioma.T("Power"), r.PotenciaW, m.potenciaDisponibleW, "W", false, r.LimitesSuperados.Contains(Limite.Potencia)),
                Hacer(Idioma.T("Cost"), r.CostoCr, m.presupuestoCr, "cr", false, r.LimitesSuperados.Contains(Limite.Costo)),
                Hacer(Idioma.T("Slots"), r.EspaciosUsados, lanzador != null ? lanzador.espacios : (int?)null, "", false, r.LimitesSuperados.Contains(Limite.Espacios)),
                // Frenado: el que pide la órbita sobre el que tiene la nave (Alcance). Sin órbita, solo lo que tiene.
                Hacer(Idioma.T("Braking"), r.DeltaVNecesarioMs, conFrenado ? r.DeltaVDisponibleMs : (int?)null, "m/s",
                    !r.AlcanceCalculado, r.LimitesSuperados.Contains(Limite.Alcance)),
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
                case EstadoSemaforo.Verde: return Idioma.T("GREEN");
                case EstadoSemaforo.Amarillo: return Idioma.T("YELLOW");
                case EstadoSemaforo.Rojo: return Idioma.T("RED");
                default: return Idioma.T("GREY");
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
            Tarjeta("<color=" + Trayectoria.ColorHex(o.id) + ">" + Iconos.Orbita + "</color>", o.Nombre,
                Chip(Iconos.Costo, TextosBriefing.Numero(o.costoCr) + " cr"));

        /// <summary>
        /// Versión 6: tarjeta de la órbita con su frenado y la masa con la que se llega; si no se llega, el motivo en rojo.
        /// «capacidadKg» es lo que carga el lanzador: la masa con la que se llega nunca se muestra por encima.
        /// </summary>
        public static string TextoOpcion(OrbitaDatos o, Alcance.Resultado a, int capacidadKg = int.MaxValue)
        {
            var icono = "<color=" + Trayectoria.ColorHex(o.id) + ">" + Iconos.Orbita + "</color>";
            var costo = Chip(Iconos.Costo, TextosBriefing.Numero(o.costoCr) + " cr");
            if (!a.Calculable) return Tarjeta(icono, o.Nombre, costo);
            var frenado = Chip(Iconos.Lanzador, TextosBriefing.Numero(a.NecesarioMs) + " m/s");
            if (a.Alcanza)
                return Tarjeta(icono, o.Nombre, costo, frenado,
                    "<color=" + ColorBuff + ">" + Iconos.Sube + Idioma.T(" up to ") + TextosBriefing.Numero(System.Math.Min(a.MasaMaximaKg, capacidadKg)) + " kg</color>");
            return Tarjeta(icono, "<color=#94A3B8>" + o.Nombre + "</color>", costo, frenado,
                "<color=" + ColorDebuff + ">" + Iconos.Baja + Idioma.T(" max ") + TextosBriefing.Numero(a.MasaMaximaKg) + " kg</color>");
        }

        public static string TextoOpcion(LanzadorDatos l) =>
            Tarjeta(Iconos.Lanzador, l.Nombre, Chip(Iconos.Costo, TextosBriefing.Numero(l.costoCr) + " cr"),
                Chip(Iconos.Masa, TextosBriefing.Numero(l.capacidadKg) + " kg"), Chip(Iconos.Espacios, l.espacios + Idioma.T(" slots")));

        public static string TextoOpcion(InstrumentoDatos i) =>
            Tarjeta(Iconos.DeInstrumento(i.id), i.Nombre, Chip(Iconos.Masa, i.masaKg + " kg"),
                Chip(Iconos.Potencia, i.ConsumoTotalW + " W"), Chip(Iconos.Costo, i.costoCr + " cr"));

        public static string TextoOpcion(SateliteDatos s) =>
            Tarjeta(Iconos.Satelite, s.Nombre, Chip(Iconos.Informe, s.inspiradoEn));

        static string Dato(string icono, string nombre, string valor) =>
            "\n<color=" + Cian + ">" + icono + "</color> <b>" + nombre + ":</b> " + valor;

        public static string Ayuda(OrbitaDatos o) =>
            "<b>" + o.Nombre + "</b>\n" + o.DescripcionBreve +
            Modificador(new SpaceMission.Modificador(true, o.Ventaja)) + Modificador(new SpaceMission.Modificador(false, o.Limitacion)) + Dato(Iconos.Costo, Idioma.T("Cost"), o.costoCr + Idioma.T(" credits"));

        public static string Ayuda(LanzadorDatos l) =>
            "<b>" + l.Nombre + "</b>\n" + l.Descripcion + Dato(Iconos.Costo, Idioma.T("Cost"), l.costoCr + Idioma.T(" credits")) +
            Dato(Iconos.Masa, Idioma.T("Maximum mass"), TextosBriefing.Numero(l.capacidadKg) + " kg") + Dato(Iconos.Espacios, Idioma.T("Slots"), l.espacios.ToString()) +
            (l.masaInyectadaKg > 0 ? Dato(Iconos.Lanzador, Idioma.T("Sends toward Mars"), TextosBriefing.Numero(l.masaInyectadaKg) + Idioma.T(" kg (spacecraft + fuel)")) : "");

        // Versión 6: el dato real de la NASA no va en la ayuda; se abre con el botón «+ Dato NASA» (ConfiguracionUI).
        public static string DatoNasa(OrbitaDatos o) => "\n" + o.DescripcionBreve + Nasa(o.DatoNasa, o.fuenteNasa);
        public static string DatoNasa(LanzadorDatos l) => Nasa(l.DatoNasa, l.fuenteNasa);
        public static string DatoNasa(InstrumentoDatos i) => Nasa(i.DatoNasa, i.fuenteNasa);
        public static string DatoNasa(SateliteDatos s) => Nasa(s.DatoNasa, s.fuenteNasa);

        /// <summary>Versión 6: ayuda de la órbita con su alcance (flecha verde si se llega, roja si no).</summary>
        public static string Ayuda(OrbitaDatos o, Alcance.Resultado a)
        {
            var texto = Ayuda(o);
            if (!a.Calculable) return texto;
            var linea = a.Alcanza
                ? Idioma.T("Within reach: needs ") + TextosBriefing.Numero(a.NecesarioMs) + Idioma.T(" m/s of braking, you have ") + TextosBriefing.Numero(a.DisponibleMs) + " m/s"
                : Idioma.T("Out of reach: needs ") + TextosBriefing.Numero(a.NecesarioMs) + Idioma.T(" m/s of braking, you have ") + TextosBriefing.Numero(a.DisponibleMs) +
                  Idioma.T(" m/s. Remove mass (satellite or instruments) or use a bigger launcher.");
            // Versión 7: solo lo esencial para que se lea (nombre, alcance, ventaja y límite). La descripción va con
            // el dato de la NASA en el botón «+ Dato NASA»; el costo ya está en la tarjeta.
            return "<b>" + o.Nombre + "</b>" + Modificador(new SpaceMission.Modificador(a.Alcanza, linea)) +
                   Modificador(new SpaceMission.Modificador(true, o.Ventaja)) + Modificador(new SpaceMission.Modificador(false, o.Limitacion));
        }

        /// <summary>
        /// Versión 6: nota corta al final de la lista de órbitas: el frenado de la nave en una línea y el aviso de que
        /// al pulsarla se explica cómo se calcula (ExplicacionAlcance, en la caja de ayuda).
        /// </summary>
        public static string NotaAlcance(Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var reglas = catalogo.reglas;
            var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            var frenado = Alcance.Activo(lanzador, reglas)
                ? Idioma.T("Your braking: ") + "<b>" + TextosBriefing.Numero(Alcance.DeltaVDisponibleMs(lanzador, Calculador.MasaSecaKg(c, catalogo, nivel), reglas)) + " m/s</b>"
                : Idioma.T("Choose a launcher to see your braking.");
            return "<color=" + Cian + ">" + Iconos.Lanzador + "</color>  " + frenado +
                   "\n<size=85%><color=#94A3B8>" + Idioma.T("How is it calculated?") + "  " + Iconos.Siguiente + "</color></size>";
        }

        /// <summary>Versión 6: la explicación completa del alcance, con la fórmula, los números de la nave y la fuente.</summary>
        public static string ExplicacionAlcance(Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var reglas = catalogo.reglas;
            var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            var texto = Idioma.T("<b>HOW FAR CAN YOU GO?</b>") +
                        Idioma.T("\nTo stay in orbit the spacecraft must brake with its engines when it arrives. A lower, rounder orbit needs more braking; more mass means less braking.");
            if (Alcance.Activo(lanzador, reglas))
            {
                int masa = Calculador.MasaSecaKg(c, catalogo, nivel);
                texto += Idioma.T("\nYour braking = ") + reglas.impulsoEspecificoS + " s × 9.807 × ln(" +
                         TextosBriefing.Numero(lanzador.masaInyectadaKg) + " kg ÷ " + TextosBriefing.Numero(masa) + " kg) = <b>" +
                         TextosBriefing.Numero(Alcance.DeltaVDisponibleMs(lanzador, masa, reglas)) + " m/s</b>";
            }
            else texto += Idioma.T("\nChoose a launcher to see your braking.");
            return texto;
        }

        /// <summary>Los datos reales del cálculo del alcance (Marte, motores, el MRO real), para el botón «+ Dato NASA».</summary>
        public static string DatoNasaAlcance(CatalogoDatos catalogo) =>
            Nasa(catalogo.reglas.DatoNasaAlcance, catalogo.reglas.fuenteNasaAlcance);

        public static string Ayuda(InstrumentoDatos i) =>
            "<b>" + i.Nombre + "</b>\n" + i.TextoAyuda + Dato(Iconos.Masa, Idioma.T("Mass"), i.masaKg + " kg") +
            Dato(Iconos.Potencia, Idioma.T("Power"), i.ConsumoTotalW + " W") + Dato(Iconos.Costo, Idioma.T("Cost"), i.costoCr + Idioma.T(" credits"));

        public static string Ayuda(SateliteDatos s) =>
            // Lo que cambia en el juego (flechas); el dato real va en el botón «+ Dato NASA» (versión 6).
            "<b>" + s.Nombre + "</b>" +
            (catalogoActual != null ? Modificadores(SpaceMission.Modificadores.DeSatelite(s, catalogoActual)) : "") +
            "\n<size=80%><color=#94A3B8>" + s.Credito + ".</color></size>";

        /// <summary>Catálogo para calcular los modificadores del satélite en su ayuda. Lo fija ConfiguracionUI.</summary>
        public static CatalogoDatos catalogoActual;

        public const string ColorBuff = "#22C55E", ColorDebuff = "#EF4444";

        /// <summary>Un buff (flecha arriba, verde) o un debuff (flecha abajo, rojo): se distinguen por icono y color.</summary>
        public static string Modificador(SpaceMission.Modificador m) =>
            "\n<color=" + (m.Positivo ? ColorBuff : ColorDebuff) + ">" + (m.Positivo ? Iconos.Sube : Iconos.Baja) + " " + m.Texto + "</color>";

        /// <summary>Lista de modificadores; los buffs primero. Sin ninguno: «sin ventajas ni desventajas».</summary>
        public static string Modificadores(System.Collections.Generic.List<SpaceMission.Modificador> l)
        {
            if (l == null) return "";
            if (l.Count == 0) return Idioma.T("\n<color=#94A3B8>No advantages or drawbacks: the reference spacecraft.</color>");
            return string.Concat(l.OrderBy(m => m.Positivo ? 0 : 1).Select(Modificador));
        }

        /// <summary>El dato real de la NASA en que se basa la opción, en color de datos y con su fuente.</summary>
        public static string Nasa(string dato, string fuente) =>
            string.IsNullOrEmpty(dato) ? "" :
            Idioma.T("\n<color=#75D6DF><b>NASA data:</b> ") + dato + "</color>" +
            (string.IsNullOrEmpty(fuente) ? "" : " <size=80%><color=#94A3B8>(" + fuente + ")</color></size>");

        // ---------- Revisión ----------

        public static string Resumen(Evaluacion e, Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var orbita = string.IsNullOrEmpty(c.OrbitaId) ? null : catalogo.Orbita(c.OrbitaId);
            var lanzador = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            var instrumentos = c.InstrumentoIds.Select(id => catalogo.Instrumento(id).Nombre).ToList();
            var satelite = string.IsNullOrEmpty(c.SateliteId) ? null : catalogo.Satelite(c.SateliteId);

            var lineas = new List<string>
            {
                // Versión 6: en el orden de la configuración (cohete, satélite, instrumentos y órbita).
                Idioma.T("<b>Launcher:</b> ") + (lanzador != null ? lanzador.Nombre : Idioma.T("not chosen")),
                Idioma.T("<b>Satellite:</b> ") + (satelite != null ? satelite.Nombre : Idioma.T("MRO Orbiter (default)")),
                Idioma.T("<b>Instruments:</b> ") + (instrumentos.Count > 0 ? string.Join(", ", instrumentos) : Idioma.T("none")),
                Idioma.T("<b>Orbit:</b> ") + (orbita != null ? orbita.Nombre : Idioma.T("not chosen")),
                ""
            };
            foreach (var b in Barras(e, c, catalogo, nivel))
                lineas.Add("<b>" + b.Nombre + ":</b> " + (b.Excedida ? "<color=#EF4444>" + b.Texto + "</color>" : b.Texto));
            lineas.Add("");
            lineas.Add(Idioma.T("<b>STATUS:</b> <color=#") + ColorUtility.ToHtmlStringRGB(ColorSemaforo(e.Estado)) + ">" +
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
