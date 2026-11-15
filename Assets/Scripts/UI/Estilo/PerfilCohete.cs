using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Medidas de un cohete tomadas de su malla al construir la escena (ConstructorEscenas.MedirCohete). La
    /// cinemática las usa para que los brazos de la torre lleguen justo a su costado y para que el satélite quepa
    /// en la cofia, en lugar de proporciones fijas que solo valían para un modelo. Solo presentación.
    /// </summary>
    public class PerfilCohete : MonoBehaviour
    {
        [SerializeField] float alto = 1f;
        /// <summary>Por bandas de altura (de la base a la punta): hasta dónde llega el cohete hacia la torre.</summary>
        [SerializeField] float[] haciaTorre = new float[0];
        [SerializeField] float radioCarga = 0.1f;
        [SerializeField] float radioCorte = 0.2f;
        [SerializeField] float altoCofia = 0.5f;
        [SerializeField] float radioBase = 0.5f;

        public float Alto => alto;
        /// <summary>Radio libre dentro de la cofia: el satélite se escala para caber.</summary>
        public float RadioCarga => radioCarga;
        /// <summary>Radio del cohete en el corte entre la primera etapa y la superior.</summary>
        public float RadioCorte => radioCorte;

        /// <summary>Alto de la cofia: la carga va a un tercio de su altura, donde se midió el radio libre.</summary>
        public float AltoCofia => altoCofia;

        /// <summary>Radio del pie del cohete (con aletas y toberas): las sujeciones del pedestal se ponen justo fuera.</summary>
        public float RadioBase => radioBase;

        public void Fijar(float altoCohete, float[] perfil, float carga, float corte, float cofia, float pie)
        {
            alto = altoCohete; haciaTorre = perfil; radioCarga = carga; radioCorte = corte; altoCofia = cofia; radioBase = pie;
        }

        /// <summary>Distancia del eje a la piel del cohete, hacia la torre, entre dos alturas (la mayor de las bandas).</summary>
        public float HaciaTorre(float desde, float hasta)
        {
            if (haciaTorre == null || haciaTorre.Length == 0 || alto <= 0f) return 0.3f;
            int a = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(desde, hasta) / alto * haciaTorre.Length), 0, haciaTorre.Length - 1);
            int b = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(desde, hasta) / alto * haciaTorre.Length), 0, haciaTorre.Length - 1);
            float r = 0f;
            for (int i = a; i <= b; i++) r = Mathf.Max(r, haciaTorre[i]);
            return r;
        }
    }
}
