namespace SpaceMission
{
    /// <summary>
    /// Guarda entre escenas lo que el jugador eligió. Es estática a propósito:
    /// sin singletons ni MonoBehaviours. El resultado se agrega en la fase 4.
    /// </summary>
    public static class EstadoJuego
    {
        /// <summary>La configuración elegida. «Modificar mi misión» la conserva.</summary>
        public static Configuracion Configuracion = new Configuracion();

        /// <summary>Deja todo como al empezar una misión nueva.</summary>
        public static void Reiniciar()
        {
            Configuracion = new Configuracion();
        }
    }
}
