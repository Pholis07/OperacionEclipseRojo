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

        [Header("Dato real de la NASA en que se basa (versión 5)")]
        [TextArea] public string datoNasa;     // inglés
        [TextArea] public string datoNasaEs;   // español
        public string fuenteNasa;              // fuente corta, p. ej. «NASA/JPL (MRO)»

        /// <summary>Consumo total. Desde la versión 5 la tormenta no suma potencia (extraTormentaW = 0): baja la de los paneles.</summary>
        public int ConsumoTotalW => potenciaNormalW + extraTormentaW;

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Nombre => Idioma.T(nombre);
        public string PistaAsociada => Idioma.T(pistaAsociada);
        public string TextoAyuda => Idioma.T(textoAyuda);
        public string DatoNasa => Idioma.Espanol && !string.IsNullOrEmpty(datoNasaEs) ? datoNasaEs : datoNasa;
    }
}
