using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Pulso discreto sobre la región de la anomalía (Menú): un anillo que crece y se desvanece cada pocos segundos,
    /// y un punto fijo en el centro. Funciona con un SpriteRenderer o con una Image de la interfaz.
    /// </summary>
    public class PulsoAnomalia : MonoBehaviour
    {
        [SerializeField] SpriteRenderer anillo;
        [SerializeField] UnityEngine.UI.Graphic anilloUI;
        [SerializeField] float periodo = 6f;
        [SerializeField] float duracion = 1.8f;
        [SerializeField] float escalaMaxima = 3f;
        [SerializeField] float alfa = 0.85f;

        Vector3 escalaBase;
        float desfase;

        void Awake()
        {
            var t = anillo != null ? anillo.transform : anilloUI != null ? anilloUI.transform : null;
            escalaBase = t != null ? t.localScale : Vector3.one;
            desfase = Random.value;
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.unscaledTime + desfase * periodo, periodo);
            float k = Preferencias.ReducirMovimiento ? 0f : Mathf.Clamp01(t / duracion);
            float a = Preferencias.ReducirMovimiento ? alfa * 0.6f : (1f - k) * alfa;
            float escala = Mathf.Lerp(1f, escalaMaxima, Animar.SalidaCubica(k));
            if (anillo != null)
            {
                anillo.transform.localScale = escalaBase * escala;
                var c = anillo.color; c.a = a; anillo.color = c;
            }
            if (anilloUI != null)
            {
                anilloUI.transform.localScale = escalaBase * escala;
                var c = anilloUI.color; c.a = a; anilloUI.color = c;
            }
        }
    }
}
