using UnityEngine;

namespace SpaceMission
{
    /// <summary>Nave 3D fija: muestra el módulo de cada instrumento elegido. Sin rotación ni zoom.</summary>
    public class NaveUI : MonoBehaviour
    {
        [SerializeField] string[] idsInstrumento;
        [SerializeField] GameObject[] modulos;

        public void Mostrar(Configuracion config)
        {
            for (int i = 0; i < modulos.Length; i++)
                modulos[i].SetActive(config.InstrumentoIds.Contains(idsInstrumento[i]));
        }
    }
}
