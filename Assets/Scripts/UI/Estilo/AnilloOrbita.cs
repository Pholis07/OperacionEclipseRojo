using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Anillo de la órbita elegida alrededor del Marte del fondo: inclinación según la órbita
    /// (polar, media inclinada o elíptica), con un marcador que la recorre como la nave (más rápido cerca del
    /// planeta, como en una órbita real). Sin órbita elegida, no se ve.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AnilloOrbita : MonoBehaviour
    {
        [SerializeField] float radio = 1.35f; // en radios del planeta (escala local de la esfera = 1)
        [SerializeField] Transform marcador;
        [SerializeField] float vueltaSegundos = 9f;
        const int Puntos = 128;

        LineRenderer linea;
        string dibujada = "-";
        float aparicion;
        float angulo, excentricidadActual;
        Quaternion rotacionActual = Quaternion.identity;

        void Awake()
        {
            linea = GetComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.loop = true;
            linea.positionCount = Puntos;
        }

        void Update()
        {
            var id = EstadoJuego.Configuracion.OrbitaId;
            if (id != dibujada)
            {
                dibujada = id;
                Dibujar(id);
                aparicion = 0f;
            }
            if (aparicion < 1f)
            {
                aparicion = Preferencias.ReducirMovimiento ? 1f : Mathf.Min(1f, aparicion + Time.unscaledDeltaTime * 2.5f);
                var c = linea.startColor;
                c.a = Animar.SalidaCubica(aparicion) * 0.9f;
                linea.startColor = linea.endColor = c;
            }
            MoverMarcador();
        }

        void MoverMarcador()
        {
            if (marcador == null) return;
            bool visible = linea.enabled;
            if (marcador.gameObject.activeSelf != visible) marcador.gameObject.SetActive(visible);
            if (!visible) return;
            float r = Radio(angulo, excentricidadActual);
            // Segunda ley de Kepler (aproximada): la velocidad angular crece al acercarse al planeta.
            if (!Preferencias.ReducirMovimiento)
                angulo += Time.unscaledDeltaTime * Mathf.PI * 2f / vueltaSegundos * (radio * radio) / (r * r);
            marcador.localPosition = rotacionActual * new Vector3(Mathf.Cos(angulo) * r, 0f, Mathf.Sin(angulo) * r);
        }

        /// <summary>
        /// Distancia al centro del planeta (radio de la esfera = 0.5). Circular: «radio». Elíptica: el punto más
        /// cercano queda al 88 % de «radio», siempre fuera del planeta, con el planeta en un foco.
        /// </summary>
        float Radio(float a, float e)
        {
            if (e <= 0f) return radio;
            float semilatus = radio * 0.88f * (1f + e);
            return semilatus / (1f + e * Mathf.Cos(a));
        }

        void Dibujar(string id)
        {
            linea.enabled = !string.IsNullOrEmpty(id);
            if (!linea.enabled) return;
            float inclinacion = id == "polar" ? 80f : id == "media" ? 45f : 22f;
            float excentricidad = id == "eliptica" ? 0.42f : 0f;
            var rot = Quaternion.Euler(inclinacion, 0f, 20f);
            rotacionActual = rot;
            excentricidadActual = excentricidad;
            for (int i = 0; i < Puntos; i++)
            {
                float a = i * Mathf.PI * 2f / Puntos;
                // Elipse con el planeta en un foco.
                float r = Radio(a, excentricidad);
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                linea.SetPosition(i, rot * p);
            }
        }
    }
}
