using System;
using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>
    /// Versión 6: hasta qué órbita llega la nave, con física real (sin aerofrenado).
    ///
    /// Al llegar a Marte la nave viene rápido (v∞) y tiene que frenar con sus motores para quedarse en órbita.
    /// - Frenado que pide la órbita (vis-viva, en el punto más bajo):
    ///     √(v∞² + 2μ/rp) − √(μ (2/rp − 1/a))
    ///   Una órbita baja y redonda pide más frenado que una elíptica alta.
    /// - Frenado que tiene la nave (ecuación del cohete de Tsiolkovsky):
    ///     Isp × g0 × ln(masa inyectada ÷ masa seca)
    ///   La masa inyectada es lo que el lanzador manda hacia Marte; la seca, la nave sin combustible
    ///   (base + satélite + instrumentos). Más masa seca = menos frenado.
    /// Constantes de Marte: NASA NSSDC (Mars Fact Sheet). Todo se compara en m/s enteros.
    /// Sin datos (lanzador u órbita con valores en 0) no se calcula y todo se considera alcanzable.
    /// </summary>
    public static class Alcance
    {
        public const double MuMarte = 42828.0;     // km³/s²
        public const double RadioMarte = 3389.5;   // km (radio medio)
        public const double G0 = 9.80665;          // m/s²

        public struct Resultado
        {
            public bool Calculable;       // hay lanzador y órbita con datos
            public int MasaSecaKg;
            public int NecesarioMs;       // frenado que pide la órbita
            public int DisponibleMs;      // frenado que tiene la nave
            public int MasaMaximaKg;      // masa seca máxima con que se llega a esta órbita
            public bool Alcanza => !Calculable || DisponibleMs >= NecesarioMs;
        }

        public static bool Activo(LanzadorDatos l, ReglasOperacionDatos r) =>
            l != null && r != null && l.masaInyectadaKg > 0 && r.impulsoEspecificoS > 0 && r.velocidadLlegadaKmS > 0f;

        public static bool TieneDatos(OrbitaDatos o) => o != null && o.periapsisKm > 0 && o.apoapsisKm >= o.periapsisKm;

        /// <summary>Frenado (m/s) para pasar de la llegada a esta órbita, en su punto más bajo.</summary>
        public static int DeltaVInsercionMs(OrbitaDatos o, ReglasOperacionDatos r)
        {
            double rp = RadioMarte + o.periapsisKm;
            double ra = RadioMarte + o.apoapsisKm;
            double a = (rp + ra) / 2.0;
            double vinf = r.velocidadLlegadaKmS;
            double llegada = Math.Sqrt(vinf * vinf + 2.0 * MuMarte / rp);
            double enOrbita = Math.Sqrt(MuMarte * (2.0 / rp - 1.0 / a));
            return (int)Math.Round(1000.0 * (llegada - enOrbita));
        }

        /// <summary>Frenado (m/s) que puede dar la nave con su combustible. 0 si no le queda combustible.</summary>
        public static int DeltaVDisponibleMs(LanzadorDatos l, int masaSecaKg, ReglasOperacionDatos r)
        {
            if (masaSecaKg <= 0) masaSecaKg = 1;
            if (masaSecaKg >= l.masaInyectadaKg) return 0;
            return (int)Math.Floor(r.impulsoEspecificoS * G0 * Math.Log((double)l.masaInyectadaKg / masaSecaKg));
        }

        /// <summary>La masa seca más grande con la que el lanzador todavía llega a esta órbita.</summary>
        public static int MasaMaximaKg(LanzadorDatos l, OrbitaDatos o, ReglasOperacionDatos r)
        {
            int necesario = DeltaVInsercionMs(o, r);
            int m = (int)Math.Floor(l.masaInyectadaKg / Math.Exp(necesario / (r.impulsoEspecificoS * G0)));
            while (m > 1 && DeltaVDisponibleMs(l, m, r) < necesario) m--;
            while (DeltaVDisponibleMs(l, m + 1, r) >= necesario) m++;
            return m;
        }

        /// <summary>El alcance de una nave con esta masa seca a esta órbita.</summary>
        public static Resultado Evaluar(LanzadorDatos l, OrbitaDatos o, int masaSecaKg, ReglasOperacionDatos r)
        {
            var res = new Resultado { MasaSecaKg = masaSecaKg };
            if (!Activo(l, r) || !TieneDatos(o)) return res;
            res.Calculable = true;
            res.NecesarioMs = DeltaVInsercionMs(o, r);
            res.DisponibleMs = DeltaVDisponibleMs(l, masaSecaKg, r);
            res.MasaMaximaKg = MasaMaximaKg(l, o, r);
            return res;
        }

        /// <summary>El alcance de la configuración (con su lanzador y su masa) a una órbita cualquiera.</summary>
        public static Resultado Evaluar(Configuracion c, OrbitaDatos o, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var l = string.IsNullOrEmpty(c.LanzadorId) ? null : catalogo.Lanzador(c.LanzadorId);
            return Evaluar(l, o, Calculador.MasaSecaKg(c, catalogo, nivel), catalogo.reglas);
        }

        /// <summary>Las órbitas a las que llega la configuración con su lanzador y su masa.</summary>
        public static List<OrbitaDatos> Alcanzables(Configuracion c, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var lista = new List<OrbitaDatos>();
            foreach (var o in catalogo.orbitas)
                if (Evaluar(c, o, catalogo, nivel).Alcanza) lista.Add(o);
            return lista;
        }
    }
}
