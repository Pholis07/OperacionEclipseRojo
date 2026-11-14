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

        [Header("Aspecto de la nave (sin reglas todavía)")]
        public List<SateliteDatos> satelites = new List<SateliteDatos>();

        [Header("Operación, niveles y puntos")]
        public ReglasOperacionDatos reglas;
        public List<MisionDatos> niveles = new List<MisionDatos>();   // 0 = nivel 1; «mision» es el nivel 2
        public List<EventoDatos> eventos = new List<EventoDatos>();
        public List<HerramientaDatos> herramientas = new List<HerramientaDatos>();

        public OrbitaDatos Orbita(string id) => orbitas.Find(o => o.id == id);
        public LanzadorDatos Lanzador(string id) => lanzadores.Find(l => l.id == id);
        public InstrumentoDatos Instrumento(string id) => instrumentos.Find(i => i.id == id);
        public TransmisionDatos Transmision(string id) => transmisiones.Find(t => t.id == id);
        public HerramientaDatos Herramienta(string id) => herramientas.Find(h => h.id == id);
        public SateliteDatos Satelite(string id) => satelites.Find(s => s.id == id);

        /// <summary>Datos del nivel (0, 1 o 2). Si no hay niveles cargados, la misión del diseño.</summary>
        public MisionDatos Nivel(int indice) =>
            indice >= 0 && indice < niveles.Count && niveles[indice] != null ? niveles[indice] : mision;
    }
}
