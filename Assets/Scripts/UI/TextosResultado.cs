namespace SpaceMission
{
    /// <summary>
    /// Títulos y textos de las tres conclusiones, copiados del diseño (prompt 07 y decisión 6) con un script.
    /// En el documento el punto final va fuera de las comillas; aquí se muestra como final de la frase.
    /// </summary>
    public static class TextosResultado
    {
        public static string TituloVictoria => Idioma.T("Mission accomplished");
        public static string TextoVictoria =>
            Idioma.T("Against the challenges of the Martian storm, your spacecraft gathered decisive evidence: a thermal anomaly and underground structures consistent with possible ice deposits. The investigation opens new questions about Mars.");

        public static string TituloParcial => Idioma.T("Incomplete signal");
        public static string TextoParcial =>
            Idioma.T("The mission gathered valuable information, but more evidence is needed to explain the anomaly. A different science configuration could complete the investigation.");

        public static string TituloFracaso => Idioma.T("Investigation failed");
        public static string TextoFracaso =>
            Idioma.T("The spacecraft carried out the mission, but none of its instruments observed the main anomaly. The supporting data is not enough to explain it. Review the science payload before trying again.");

        public static string Titulo(CategoriaResultado c) =>
            c == CategoriaResultado.Victoria ? TituloVictoria : c == CategoriaResultado.Parcial ? TituloParcial : TituloFracaso;

        /// <summary>Versión 4: la nave podía conseguir más, pero la evidencia no llegó a la Tierra (no es culpa de la configuración).</summary>
        public static string TextoSinEnviar =>
            Idioma.T("Your spacecraft could have achieved more, but not all the evidence reached Earth. Observe the anomaly with enough quality and send the data before ending the mission.");

        public static string Texto(CategoriaResultado c) =>
            c == CategoriaResultado.Victoria ? TextoVictoria : c == CategoriaResultado.Parcial ? TextoParcial : TextoFracaso;
    }
}
