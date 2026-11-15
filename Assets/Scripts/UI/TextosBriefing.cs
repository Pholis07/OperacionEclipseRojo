using System.Globalization;

namespace SpaceMission
{
    /// <summary>
    /// Textos del Briefing. Los requisitos son los del diseño (Plan.md, decisión 13): hablan de evidencias
    /// y no nombran instrumentos. Los números de los límites salen siempre del catálogo.
    /// </summary>
    public static class TextosBriefing
    {
        public static readonly string[] Requisitos =
        {
            "Obtain evidence of the thermal anomaly.",
            "Obtain evidence of what lies beneath the surface.",
            "With an elliptical orbit, also find out what the storm environment is like."
        };

        public const string ExplicacionColores =
            "<b><color=#9CA3AF>Grey</color></b>: you still need to choose an orbit, a launcher or at least one instrument.\n" +
            "<b><color=#EF4444>Red</color></b>: a limit is exceeded; you cannot launch.\n" +
            "<b><color=#EAB308>Yellow</color></b>: you can launch, but some evidence is missing.\n" +
            "<b><color=#22C55E>Green</color></b>: the configuration covers what the mission requires.";

        public static string TextoRequisitos()
        {
            var texto = "";
            for (int i = 0; i < Requisitos.Length; i++)
                texto += (i + 1) + ". " + Requisitos[i] + (i < Requisitos.Length - 1 ? "\n" : "");
            return texto;
        }

        public static string TextoLimites(CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var m = nivel != null ? nivel : catalogo.mision;
            var explorador = catalogo.Lanzador("explorador");
            var titan = catalogo.Lanzador("titan");
            return "Budget: " + Numero(m.presupuestoCr) + " credits\n" +
                   "Available power: " + Numero(m.potenciaDisponibleW) + " W\n" +
                   "Maximum mass by launcher: " + explorador.nombre + " " + Numero(explorador.capacidadKg) +
                   " kg, " + titan.nombre + " " + Numero(titan.capacidadKg) + " kg\n" +
                   "Instrument slots: " + explorador.espacios + " with " + explorador.nombre +
                   ", " + titan.espacios + " with " + titan.nombre + "\n" +
                   "At least one instrument is required to launch";
        }

        /// <summary>Números con coma de miles, como en los documentos del equipo (1,250).</summary>
        public static string Numero(int valor) => valor.ToString("N0", CultureInfo.InvariantCulture);
    }
}
