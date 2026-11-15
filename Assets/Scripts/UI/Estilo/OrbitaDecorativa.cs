using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Órbita de adorno (Menú): una elipse cian fina alrededor del planeta y un satélite que la recorre,
    /// más rápido cerca del planeta. Sin lógica del juego. Con «Reducir movimiento» el satélite se queda quieto.
    /// Opcional: una estela (otra línea, en el mismo espacio local) que sigue al satélite y se desvanece detrás.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class OrbitaDecorativa : MonoBehaviour
    {
        [SerializeField] Transform satelite;
        [SerializeField] float semiejeMayor = 9f;
        [SerializeField, Range(0f, 0.8f)] float excentricidad = 0.25f;
        [SerializeField] Vector3 inclinacion = new Vector3(72f, 0f, 18f);
        [SerializeField] float vueltaSegundos = 40f;
        [SerializeField] float anguloInicial = 2.2f;
        [SerializeField] LineRenderer estela;
        [SerializeField] LineRenderer brillo;         // halo ancho y tenue con la misma forma (opcional)
        [SerializeField] float largoEstela = 1.6f;   // radianes de órbita detrás del satélite
        const int Puntos = 160, PuntosEstela = 48;

        LineRenderer linea;
        float angulo;

        void Awake() => Dibujar();

        /// <summary>Dibuja la elipse y coloca el satélite al principio (también lo llama el constructor de escenas).</summary>
        public void Dibujar()
        {
            linea = GetComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.loop = true;
            linea.positionCount = Puntos;
            for (int i = 0; i < Puntos; i++) linea.SetPosition(i, Punto(i * Mathf.PI * 2f / Puntos));
            if (brillo != null)
            {
                brillo.useWorldSpace = false;
                brillo.loop = true;
                brillo.positionCount = Puntos;
                for (int i = 0; i < Puntos; i++) brillo.SetPosition(i, Punto(i * Mathf.PI * 2f / Puntos));
            }
            angulo = anguloInicial;
            Colocar();
        }

        /// <summary>Punto de la elipse (con el planeta en un foco) para el ángulo dado, en coordenadas locales.</summary>
        public Vector3 Punto(float a)
        {
            float p = semiejeMayor * (1f - excentricidad * excentricidad);
            float r = p / (1f + excentricidad * Mathf.Cos(a));
            return Quaternion.Euler(inclinacion) * new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        void Update()
        {
            if (!Preferencias.ReducirMovimiento)
            {
                float r = Punto(angulo).magnitude;
                angulo += Time.unscaledDeltaTime * Mathf.PI * 2f / vueltaSegundos * (semiejeMayor * semiejeMayor) / (r * r);
            }
            Colocar();
        }

        void Colocar()
        {
            if (estela != null)
            {
                estela.useWorldSpace = false;
                estela.loop = false;
                estela.positionCount = PuntosEstela;
                for (int i = 0; i < PuntosEstela; i++)
                    estela.SetPosition(i, Punto(angulo - largoEstela * (1f - i / (PuntosEstela - 1f))));
            }
            if (satelite == null) return;
            var aqui = transform.TransformPoint(Punto(angulo));
            var luego = transform.TransformPoint(Punto(angulo + 0.01f));
            satelite.position = aqui;
            // Avanza por la órbita con la cara frontal (los instrumentos) hacia el planeta.
            var haciaPlaneta = (transform.position - aqui).normalized;
            satelite.rotation = Quaternion.LookRotation(-haciaPlaneta, (luego - aqui).normalized);
        }
    }
}
