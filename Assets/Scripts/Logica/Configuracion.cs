using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>
    /// Lo que eligió el jugador, solo con ids. Cualquier parte puede faltar (null o sin instrumentos).
    /// Cambiar de lanzador u órbita no borra los instrumentos.
    /// </summary>
    public class Configuracion
    {
        public string OrbitaId;
        public string LanzadorId;

        /// <summary>Ids de instrumentos sin repetir, en el orden en que se eligieron.</summary>
        public readonly List<string> InstrumentoIds = new List<string>();

        public Configuracion() { }

        public Configuracion(string orbitaId, string lanzadorId, IEnumerable<string> instrumentoIds)
        {
            OrbitaId = orbitaId;
            LanzadorId = lanzadorId;
            foreach (var id in instrumentoIds)
                AgregarInstrumento(id);
        }

        public void AgregarInstrumento(string id)
        {
            if (!InstrumentoIds.Contains(id))
                InstrumentoIds.Add(id);
        }

        public void QuitarInstrumento(string id) => InstrumentoIds.Remove(id);
    }
}
