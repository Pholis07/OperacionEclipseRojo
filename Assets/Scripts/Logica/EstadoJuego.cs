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

        /// <summary>Nivel elegido en el Briefing: 0, 1 o 2 (por defecto el 2, «Misión estándar», que es el del diseño).</summary>
        public static int NivelIndice = 1;

        /// <summary>Las herramientas y el combustible que se cargaron antes de lanzar (gameplay v3).</summary>
        public static Carga Carga;

        /// <summary>La operación de la última misión lanzada (para el informe y los puntos).</summary>
        public static Operacion Operacion;

        /// <summary>Resultado final de la misión (ResultadoCientifico). Lo fija la Operación al terminar; null si no se jugó.</summary>
        public static CategoriaResultado? CategoriaFinal;

        /// <summary>«Modificar mi misión» lo pone en true para que Mision abra en Configuración y no en el Briefing.</summary>
        public static bool EntrarEnConfiguracion;

        /// <summary>Deja todo como al empezar una misión nueva.</summary>
        public static void Reiniciar()
        {
            Configuracion = new Configuracion();
            Resultado = null;
            Carga = null;
            Operacion = null;
            CategoriaFinal = null;
            EntrarEnConfiguracion = false;
            NivelIndice = 1;
        }
    }
}
