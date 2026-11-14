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
        [TextArea] public string objetivo;
        [TextArea] public string narrativa;
    }
}
