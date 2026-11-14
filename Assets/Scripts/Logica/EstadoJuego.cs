using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>
    /// Guarda entre escenas lo que el jugador eligió. Es estática a propósito:
    /// sin singletons ni MonoBehaviours. Solo guarda ids (los datos viven en los
    /// ScriptableObjects). El resultado se agrega en la fase 4, cuando exista su tipo.
    /// </summary>
    public static class EstadoJuego
    {
        /// <summary>Id de la órbita elegida, o null si no hay.</summary>
        public static string OrbitaId;

        /// <summary>Id del lanzador elegido, o null si no hay.</summary>
        public static string LanzadorId;

        /// <summary>Ids de los instrumentos elegidos. Cambiar de lanzador no los borra.</summary>
        public static readonly List<string> InstrumentoIds = new List<string>();

        /// <summary>Deja todo como al empezar una misión nueva.</summary>
        public static void Reiniciar()
        {
            OrbitaId = null;
            LanzadorId = null;
            InstrumentoIds.Clear();
        }
    }
}
