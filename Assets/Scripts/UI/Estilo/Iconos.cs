namespace SpaceMission
{
    /// <summary>
    /// Iconos de Lucide (licencia ISC) como caracteres de la fuente «Iconos SDF». Esa fuente es fallback de las
    /// fuentes de texto, así que basta con escribir el carácter dentro de cualquier texto.
    /// Los códigos salen de lucide-static 1.73.0 (font/codepoints.json).
    /// </summary>
    public static class Iconos
    {
        public const string Masa = "";        // weight
        public const string Potencia = "";    // zap
        public const string Costo = "";       // coins
        public const string Espacios = "";    // grid-2x2
        public const string Orbita = "";      // orbit
        public const string Lanzador = "";    // rocket
        public const string Instrumentos = ""; // satellite
        public const string Elegido = "";     // square-check
        public const string SinElegir = "";   // square
        public const string Gris = "";        // circle
        public const string Rojo = "";        // circle-alert
        public const string Amarillo = "";    // triangle-alert
        public const string Verde = "";       // circle-check
        public const string Senal = "";       // satellite-dish
        public const string Objetivo = "";    // target
        public const string Informe = "";     // clipboard-list
        public const string Ajustes = "";     // settings
        public const string Volumen = "";     // volume-2
        public const string Siguiente = "";   // arrow-right
        public const string Atras = "";       // arrow-left

        /// <summary>Todos los iconos, para generar el atlas de la fuente.</summary>
        public static readonly string Todos = Masa + Potencia + Costo + Espacios + Orbita + Lanzador + Instrumentos +
            Elegido + SinElegir + Gris + Rojo + Amarillo + Verde + Senal + Objetivo + Informe + Ajustes + Volumen +
            Siguiente + Atras + "";

        public static string DeInstrumento(string id)
        {
            switch (id)
            {
                case "termica": return "";       // thermometer
                case "radar": return "";         // radar
                case "alta_res": return "";      // camera
                case "espectrometro": return ""; // flask-conical
                case "atmosferico": return "";   // wind
                default: return Instrumentos;
            }
        }

        public static string DeEstado(EstadoSemaforo estado)
        {
            switch (estado)
            {
                case EstadoSemaforo.Rojo: return Rojo;
                case EstadoSemaforo.Amarillo: return Amarillo;
                case EstadoSemaforo.Verde: return Verde;
                default: return Gris;
            }
        }
    }
}
