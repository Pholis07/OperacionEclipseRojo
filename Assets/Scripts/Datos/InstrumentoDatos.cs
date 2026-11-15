using UnityEngine;

namespace SpaceMission
{
    /// <summary>Datos de un instrumento.</summary>
    [CreateAssetMenu(fileName = "Instrumento", menuName = "SpaceMission/Instrumento")]
    public class InstrumentoDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        public int masaKg;
        public int potenciaNormalW;
        public int extraTormentaW;
        public int costoCr;
        public int bloquesDatos;   // memoria que ocupa cada registro mensual (versión 4)

        /// <summary>La evidencia que aporta, con las palabras de los requisitos del Briefing.</summary>
        [TextArea] public string pistaAsociada;

        /// <summary>Ayuda contextual: qué evidencia aporta y el desglose del consumo.</summary>
        [TextArea] public string textoAyuda;

        /// <summary>Consumo de diseño total: normal más extra de tormenta.</summary>
        public int ConsumoTotalW => potenciaNormalW + extraTormentaW;
    }
}
