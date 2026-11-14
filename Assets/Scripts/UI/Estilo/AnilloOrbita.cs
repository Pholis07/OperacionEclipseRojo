using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Anillo de la órbita elegida alrededor del Marte del fondo: inclinación según la órbita
    /// (polar, media inclinada o elíptica). Sin órbita elegida, no se ve.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AnilloOrbita : MonoBehaviour
    {
        [SerializeField] float radio = 1.35f; // en radios del planeta (escala local de la esfera = 1)
        const int Puntos = 128;

        LineRenderer linea;
        string dibujada = "-";
        float aparicion;

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
        }

        void Dibujar(string id)
        {
            linea.enabled = !string.IsNullOrEmpty(id);
            if (!linea.enabled) return;
            float inclinacion = id == "polar" ? 88f : id == "media" ? 45f : 25f;
            float excentricidad = id == "eliptica" ? 0.55f : 0f;
            var rot = Quaternion.Euler(inclinacion, 0f, 20f);
            for (int i = 0; i < Puntos; i++)
            {
                float a = i * Mathf.PI * 2f / Puntos;
                // Elipse con el planeta en un foco.
                float r = radio * (1f - excentricidad * excentricidad) / (1f + excentricidad * Mathf.Cos(a));
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                linea.SetPosition(i, rot * p);
            }
        }
    }
}
