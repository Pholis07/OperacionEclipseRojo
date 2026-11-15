using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>Un efecto que ayuda (buff) o perjudica (debuff), con el texto que ve el jugador.</summary>
    public struct Modificador
    {
        public bool Positivo;
        public string Texto;
        public Modificador(bool positivo, string texto) { Positivo = positivo; Texto = texto; }
    }

    /// <summary>
    /// Versión 5, bloque 2: los buffs y debuffs salen de los números de los datos, no de un texto escrito a mano,
    /// así que lo que se muestra siempre coincide con lo que hace el juego.
    /// </summary>
    public static class Modificadores
    {
        /// <summary>Lo que cambia un satélite respecto al MRO (la nave de referencia, sin modificadores).</summary>
        public static List<Modificador> DeSatelite(SateliteDatos s, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var r = catalogo.reglas;
            nivel = nivel != null ? nivel : catalogo.mision;
            var l = new List<Modificador>();
            if (s == null) return l;

            if (s.instrumentosConAlcance != null && s.instrumentosConAlcance.Length > 0 && s.factorAlcance != 1f)
                l.Add(new Modificador(s.factorAlcance > 1f, string.Join(Idioma.T(" and "), s.instrumentosConAlcance.Select(id => catalogo.Instrumento(id).Nombre)) +
                    Idioma.T(" read ") + Porcentaje(s.factorAlcance) + Idioma.T(" farther")));
            if (s.combustiblePorCambioAltitud >= 0 && s.combustiblePorCambioAltitud != r.combustiblePorCambioAltitud)
                l.Add(new Modificador(s.combustiblePorCambioAltitud < r.combustiblePorCambioAltitud,
                    Idioma.T("Altitude change: ") + s.combustiblePorCambioAltitud + Idioma.T(" fuel instead of ") + r.combustiblePorCambioAltitud));
            if (s.memoriaExtra != 0)
                l.Add(new Modificador(s.memoriaExtra > 0, Idioma.T("Memory ") + nivel.memoriaBloques + " → " + (nivel.memoriaBloques + s.memoriaExtra) + Idioma.T(" blocks")));
            if (s.limiteExtraW != 0)
                l.Add(new Modificador(s.limiteExtraW > 0, Idioma.T("Power limit ") + nivel.potenciaDisponibleW + " → " + (nivel.potenciaDisponibleW + s.limiteExtraW) + " W"));
            if (s.consumoExtraW != 0)
                l.Add(new Modificador(s.consumoExtraW < 0, Idioma.T("Always-on draw ") + Signo(s.consumoExtraW) + " W"));
            if (s.vatiosTransmisor >= 0 && s.vatiosTransmisor != r.vatiosTransmisor)
                l.Add(new Modificador(s.vatiosTransmisor < r.vatiosTransmisor, Idioma.T("Transmitter ") + s.vatiosTransmisor + Idioma.T(" W instead of ") + r.vatiosTransmisor + " W"));
            if (s.cicloContacto > 0 && s.cicloContacto != r.cicloContacto)
                l.Add(new Modificador(s.cicloContacto < r.cicloContacto, s.cicloContacto == 1
                    ? Idioma.T("Contact with Earth every month") : Idioma.T("Contact with Earth every ") + s.cicloContacto + Idioma.T(" months")));
            if (s.envioSinVentana)
                l.Add(new Modificador(true, Idioma.T("Can send without contact (") + s.bloquesEnvioSinVentana + Idioma.T(" blocks a month)")));
            if (s.inmuneOrientacion) l.Add(new Modificador(true, Idioma.T("Loss of orientation does not affect it")));
            if (s.inmuneTormenta) l.Add(new Modificador(true, Idioma.T("A growing storm does not lower its data quality")));
            if (s.tormentaBloqueaEnvio) l.Add(new Modificador(false, Idioma.T("Carrying on in a storm blocks sending next month")));
            if (s.masaKg > 0)
                l.Add(new Modificador(false, s.masaKg + " kg: " + Signo(-s.masaKg / r.kgPorCombustible) + Idioma.T(" fuel")));
            if (s.costoCr > 0) l.Add(new Modificador(false, Idioma.T("Costs ") + s.costoCr + Idioma.T(" credits in the Cargo")));
            return l;
        }

        /// <summary>Todo lo que afecta a la Operación ahora mismo: satélite, altitud, eventos, bloqueos y lo que viene el mes siguiente.</summary>
        public static List<Modificador> Activos(Operacion op, CatalogoDatos catalogo)
        {
            var r = catalogo.reglas;
            var l = new List<Modificador>();
            if (op.Satelite != null)
                l.AddRange(DeSatelite(op.Satelite, catalogo).Where(m => !m.Texto.StartsWith(Idioma.T("Costs ")) && !m.Texto.Contains(" kg: ")));

            if (op.Altitud == Altitud.Baja)
            {
                l.Add(new Modificador(true, Idioma.T("Low orbit: readings reach ×") + r.factorAltitudBaja.ToString("0.#") + Idioma.T(" farther, quality ×") + r.factorCalidadBaja.ToString("0.#")));
                l.Add(new Modificador(false, Idioma.T("Low orbit drag: ") + Signo(-r.combustiblePorOrbitaBaja) + Idioma.T(" fuel a month")));
            }
            foreach (var e in op.Eventos.Where(e => !e.Resuelto))
            {
                if (e.Datos.efecto == EfectoEvento.ReducirLimiteTemporal)
                    l.Add(new Modificador(false, e.Datos.Nombre + ": " + Signo(-e.Datos.vatios) + " W (" + Meses(e.OrbitasRestantes) + Idioma.T(" left)")));
                else if (e.Datos.efecto == EfectoEvento.ReducirLimite)
                    l.Add(new Modificador(false, e.Datos.Nombre + ": " + Signo(-e.Datos.vatios) + Idioma.T(" W until it is fixed")));
            }
            foreach (var b in op.Bloqueados)
                l.Add(new Modificador(false, catalogo.Instrumento(b.Key).Nombre + Idioma.T(" locked (") + Meses(b.Value) + ")"));
            if (op.SinLecturaProxima) l.Add(new Modificador(false, Idioma.T("Next month gives no readings (orientation)")));
            if (op.CalidadReducidaProxima) l.Add(new Modificador(false, Idioma.T("Next month's quality ×") + r.factorCalidadTormenta.ToString("0.#") + Idioma.T(" (storm)")));
            if (op.DiasPerdidosProximos > 0) l.Add(new Modificador(false, Idioma.T("Next month loses ") + op.DiasPerdidosProximos + Idioma.T(" days")));
            if (op.EnvioBloqueado) l.Add(new Modificador(false, Idioma.T("Dust blocks the laser this month")));
            foreach (var h in catalogo.herramientas.Where(h => op.Carga != null && op.Carga.Lleva(h.id) && !op.HerramientasUsadas.Contains(h.id)))
                l.Add(new Modificador(true, h.Nombre + Idioma.T(" on board")));
            return l;
        }

        static string Meses(int n) => n + (n == 1 ? Idioma.T(" month") : Idioma.T(" months"));
        static string Signo(int n) => (n > 0 ? "+" : n < 0 ? "-" : "") + System.Math.Abs(n);
        static string Porcentaje(float f) => Signo((int)System.Math.Round((f - 1f) * 100f)) + "%";
    }
}
