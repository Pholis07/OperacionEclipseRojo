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
            "Conseguir evidencia de la anomalía térmica.",
            "Conseguir evidencia de lo que hay bajo la superficie.",
            "Con órbita elíptica, además, conocer cómo es el ambiente de la tormenta."
        };

        public const string ExplicacionColores =
            "<b><color=#9CA3AF>Gris</color></b>: falta elegir órbita, lanzador o al menos un instrumento.\n" +
            "<b><color=#EF4444>Rojo</color></b>: se supera un límite; no se puede lanzar.\n" +
            "<b><color=#EAB308>Amarillo</color></b>: se puede lanzar, pero falta alguna evidencia.\n" +
            "<b><color=#22C55E>Verde</color></b>: la configuración cubre lo que pide la misión.";

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
            return "Presupuesto: " + Numero(m.presupuestoCr) + " créditos\n" +
                   "Potencia disponible: " + Numero(m.potenciaDisponibleW) + " W\n" +
                   "Masa máxima según el lanzador: " + explorador.nombre + " " + Numero(explorador.capacidadKg) +
                   " kg, " + titan.nombre + " " + Numero(titan.capacidadKg) + " kg\n" +
                   "Espacios para instrumentos: " + explorador.espacios + " con " + explorador.nombre +
                   ", " + titan.espacios + " con " + titan.nombre + "\n" +
                   "Para lanzar hace falta al menos un instrumento";
        }

        /// <summary>Números con coma de miles, como en los documentos del equipo (1,250).</summary>
        public static string Numero(int valor) => valor.ToString("N0", CultureInfo.InvariantCulture);
    }
}
