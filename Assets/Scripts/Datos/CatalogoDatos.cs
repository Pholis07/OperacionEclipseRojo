using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>Reúne todos los datos del juego y los entrega por id.</summary>
    [CreateAssetMenu(fileName = "Catalogo", menuName = "SpaceMission/Catálogo")]
    public class CatalogoDatos : ScriptableObject
    {
        public MisionDatos mision;
        public List<OrbitaDatos> orbitas = new List<OrbitaDatos>();
        public List<LanzadorDatos> lanzadores = new List<LanzadorDatos>();
        public List<InstrumentoDatos> instrumentos = new List<InstrumentoDatos>();
        public List<TransmisionDatos> transmisiones = new List<TransmisionDatos>();

        public OrbitaDatos Orbita(string id) => orbitas.Find(o => o.id == id);
        public LanzadorDatos Lanzador(string id) => lanzadores.Find(l => l.id == id);
        public InstrumentoDatos Instrumento(string id) => instrumentos.Find(i => i.id == id);
        public TransmisionDatos Transmision(string id) => transmisiones.Find(t => t.id == id);
    }
}
