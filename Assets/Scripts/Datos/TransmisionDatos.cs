using UnityEngine;

namespace SpaceMission
{
    /// <summary>Una transmisión de la fase Investigación (una por instrumento presente o ausente).</summary>
    [CreateAssetMenu(fileName = "Transmision", menuName = "SpaceMission/Transmisión")]
    public class TransmisionDatos : ScriptableObject
    {
        public string id;

        /// <summary>Id del instrumento al que corresponde; vacío en la transmisión de la misión.</summary>
        public string instrumentoId;
        public int orden;
        public TipoTransmision tipo;
        [TextArea] public string texto;

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Texto => Idioma.T(texto);
    }
}
