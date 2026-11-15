namespace SpaceMission
{
    /// <summary>
    /// Títulos y textos de las tres conclusiones, copiados del diseño (prompt 07 y decisión 6) con un script.
    /// En el documento el punto final va fuera de las comillas; aquí se muestra como final de la frase.
    /// </summary>
    public static class TextosResultado
    {
        public const string TituloVictoria = "Misión cumplida";
        public const string TextoVictoria =
            "Contra las dificultades de la tormenta marciana, tu nave reunió evidencias decisivas: una anomalía térmica y estructuras subterráneas compatibles con posibles depósitos de hielo. La investigación abre nuevas preguntas sobre Marte.";

        public const string TituloParcial = "Señal incompleta";
        public const string TextoParcial =
            "La misión recopiló información valiosa, pero faltan pruebas para explicar la anomalía. Una configuración científica distinta podría completar la investigación.";

        public const string TituloFracaso = "Investigación fallida";
        public const string TextoFracaso =
            "La nave ejecutó la misión, pero ninguno de sus instrumentos observó la anomalía principal. Los datos complementarios no bastan para explicarla. Revisa el equipo científico antes de volver a intentarlo.";

        public static string Titulo(CategoriaResultado c) =>
            c == CategoriaResultado.Victoria ? TituloVictoria : c == CategoriaResultado.Parcial ? TituloParcial : TituloFracaso;

        /// <summary>Versión 4: la nave podía conseguir más, pero la evidencia no llegó a la Tierra (no es culpa de la configuración).</summary>
        public const string TextoSinEnviar =
            "Tu nave podía conseguir más, pero no toda la evidencia llegó a la Tierra. Observa la anomalía con calidad suficiente y envía los datos antes de terminar la misión.";

        public static string Texto(CategoriaResultado c) =>
            c == CategoriaResultado.Victoria ? TextoVictoria : c == CategoriaResultado.Parcial ? TextoParcial : TextoFracaso;
    }
}
