using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Efecto de «máquina de escribir»: cuando el texto cambia, lo revela letra a letra.
    /// Solo cambia cuántas letras se ven (maxVisibleCharacters); el texto completo sigue en .text desde el principio,
    /// así que nada espera a la animación.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class Escritura : MonoBehaviour
    {
        [SerializeField] float letrasPorSegundo = 70f;

        TMP_Text texto;
        string ultimo;
        float inicio;

        void Awake() => texto = GetComponent<TMP_Text>();

        void LateUpdate()
        {
            if (texto.text != ultimo)
            {
                ultimo = texto.text;
                inicio = Time.unscaledTime;
            }
            if (Preferencias.ReducirMovimiento)
            {
                texto.maxVisibleCharacters = 99999;
                return;
            }
            int visibles = Mathf.FloorToInt((Time.unscaledTime - inicio) * letrasPorSegundo);
            texto.maxVisibleCharacters = visibles;
        }
    }
}
