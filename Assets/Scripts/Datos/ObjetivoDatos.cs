using UnityEngine;

namespace SpaceMission
{
    /// <summary>Qué hay que cumplir para un objetivo secundario (versión 3).</summary>
    public enum TipoObjetivo
    {
        EscaneosMaximos,          // encontrar todo en «valor» escaneos o menos
        SinApagones,
        CombustibleMinimo,        // terminar con «valor» de combustible o más
        EnviarAntesDeLaUltima,    // todo enviado antes de la última órbita
        SinHerramientas
    }

    /// <summary>Objetivo secundario opcional. Cada misión saca algunos al azar y dan puntos si se cumplen.</summary>
    [CreateAssetMenu(fileName = "Objetivo", menuName = "SpaceMission/Objetivo")]
    public class ObjetivoDatos : ScriptableObject
    {
        public string id;
        [TextArea] public string texto;
        public TipoObjetivo tipo;
        public int valor;
        public int puntos;

        // Versión 5: textos en el idioma actual (Idioma). Los campos guardan el inglés.
        public string Texto => Idioma.T(texto);
    }
}
