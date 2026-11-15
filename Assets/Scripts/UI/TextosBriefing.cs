using System.Globalization;

namespace SpaceMission
{
    /// <summary>
    /// Textos del Briefing. Los requisitos son los del diseño (Plan.md, decisión 13): hablan de evidencias
    /// y no nombran instrumentos. Los números de los límites salen siempre del catálogo.
    /// </summary>
    public static class TextosBriefing
    {
        public static string[] Requisitos => new[]
        {
            Idioma.T("Obtain evidence of the thermal anomaly."),
            Idioma.T("Obtain evidence of what lies beneath the surface."),
            Idioma.T("With an elliptical orbit, also find out what the storm environment is like.")
        };

        public static string ExplicacionColores =>
            Idioma.T("<b><color=#9CA3AF>Grey</color></b>: you still need to choose an orbit, a launcher or at least one instrument.\n") +
            Idioma.T("<b><color=#EF4444>Red</color></b>: a limit is exceeded; you cannot launch.\n") +
            Idioma.T("<b><color=#EAB308>Yellow</color></b>: you can launch, but some evidence is missing.\n") +
            Idioma.T("<b><color=#22C55E>Green</color></b>: the configuration covers what the mission requires.");

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
            return Idioma.T("Budget: ") + Numero(m.presupuestoCr) + Idioma.T(" credits\n") +
                   Idioma.T("Available power: ") + Numero(m.potenciaDisponibleW) + " W\n" +
                   Idioma.T("Maximum mass by launcher: ") + explorador.Nombre + " " + Numero(explorador.capacidadKg) +
                   " kg, " + titan.Nombre + " " + Numero(titan.capacidadKg) + " kg\n" +
                   Idioma.T("Instrument slots: ") + explorador.espacios + Idioma.T(" with ") + explorador.Nombre +
                   ", " + titan.espacios + Idioma.T(" with ") + titan.Nombre + "\n" +
                   Idioma.T("At least one instrument is required to launch");
        }

        /// <summary>Números con coma de miles, como en los documentos del equipo (1,250).</summary>
        public static string Numero(int valor) => valor.ToString("N0", CultureInfo.InvariantCulture);
    }
}
