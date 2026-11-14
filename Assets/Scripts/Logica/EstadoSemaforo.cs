namespace SpaceMission
{
    /// <summary>
    /// Estado del semáforo. Se calcula, nunca se guarda. El Calculador decide Gris, Rojo o Valida;
    /// el Evaluador (fase 4) convierte Valida en Amarillo o Verde.
    /// </summary>
    public enum EstadoSemaforo
    {
        Gris,
        Rojo,
        Valida,
        Amarillo,
        Verde
    }

    /// <summary>Límites que puede superar una configuración.</summary>
    public enum Limite
    {
        Masa,
        Potencia,
        Costo,
        Espacios
    }

    /// <summary>Lo que le falta a una configuración para poder lanzarse.</summary>
    public enum Faltante
    {
        Orbita,
        Lanzador,
        Instrumentos
    }
}
