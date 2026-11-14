namespace SpaceMission
{
    /// <summary>
    /// Guarda entre escenas lo que el jugador eligió. Es estática a propósito:
    /// sin singletons ni MonoBehaviours.
    /// </summary>
    public static class EstadoJuego
    {
        /// <summary>La configuración elegida. «Modificar mi misión» la conserva.</summary>
        public static Configuracion Configuracion = new Configuracion();

        /// <summary>La evaluación de la misión lanzada, o null si todavía no se lanzó.</summary>
        public static Evaluacion Resultado;

        /// <summary>Deja todo como al empezar una misión nueva.</summary>
        public static void Reiniciar()
        {
            Configuracion = new Configuracion();
            Resultado = null;
        }
    }
}
