using UnityEngine;

namespace SpaceMission
{
    /// <summary>Recursos globales y textos de la misión (es una sola y fija).</summary>
    [CreateAssetMenu(fileName = "Mision", menuName = "SpaceMission/Misión")]
    public class MisionDatos : ScriptableObject
    {
        public int presupuestoCr;
        public int potenciaDisponibleW;
        public int masaBaseKg;
        public int costoBaseCr;
        public int consumoBaseW;

        [Header("Nivel y fase de Operación")]
        public string nombreNivel;
        public int orbitasNavegacion;
        public int eventosPorMision;
        public int memoriaBloques;   // versión 3: cuántos escaneos caben en la memoria antes de enviar
        [TextArea] public string objetivo;
        [TextArea] public string narrativa;
    }
}
