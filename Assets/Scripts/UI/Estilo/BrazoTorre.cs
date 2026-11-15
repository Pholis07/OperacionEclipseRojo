using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Brazo de la torre de servicio. Su pivote está en una esquina de la torre y apunta al eje del cohete. Es
    /// telescópico: una celosía fija y una pasarela que sale lo justo para que el cabezal toque el costado del
    /// cohete elegido (antes el largo era fijo: atravesaba a los cohetes anchos y no llegaba a los delgados).
    /// Para soltar el cohete gira sobre el pivote alejándose de él. Solo presentación.
    /// </summary>
    public class BrazoTorre : MonoBehaviour
    {
        [SerializeField] Transform pasarela;
        [SerializeField] Transform cabezal;
        [SerializeField] Transform[] umbilicales = new Transform[0];
        [SerializeField] float distanciaAlEje = 2.66f;
        [SerializeField] float largoPasarela = 1.2f;
        [SerializeField] float alcanceMinimo = 1.3f;
        [SerializeField] float giroRetirada = 78f;

        public float DistanciaAlEje => distanciaAlEje;
        public float Alcance { get; private set; }
        public Transform Cabezal => cabezal;

        public void Fijar(Transform pasarelaBrazo, Transform cabezalBrazo, Transform[] cables, float distancia, float largo, float minimo)
        {
            pasarela = pasarelaBrazo; cabezal = cabezalBrazo; umbilicales = cables;
            distanciaAlEje = distancia; largoPasarela = largo; alcanceMinimo = minimo;
        }

        /// <summary>Deja el cabezal apoyado en la piel del cohete, que a esta altura llega a «radio» del eje.</summary>
        public void Ajustar(float radio, float holgura = 0.03f)
        {
            Alcance = Mathf.Max(alcanceMinimo, distanciaAlEje - radio - holgura);
            if (pasarela != null) pasarela.localPosition = new Vector3(0f, pasarela.localPosition.y, -(Alcance - largoPasarela * 0.5f));
            if (cabezal != null) cabezal.localPosition = new Vector3(0f, cabezal.localPosition.y, -Alcance);
            Retirar(0f);
        }

        /// <summary>0 = apoyado en el cohete; 1 = retirado. Primero se sueltan los umbilicales y luego gira el brazo.</summary>
        public void Retirar(float k)
        {
            k = Mathf.Clamp01(k);
            float suelta = Mathf.Clamp01(k / 0.25f);
            foreach (var u in umbilicales) if (u != null) u.localScale = new Vector3(1f, 1f, 1f - 0.85f * suelta);
            float giro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.12f) / 0.88f));
            transform.localRotation = Quaternion.Euler(0f, giroRetirada * giro, 0f);
        }
    }
}
