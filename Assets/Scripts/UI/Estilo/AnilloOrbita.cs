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
        float angulo;
        string orbitaActual;

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
            // Segunda ley de Kepler (aproximada): la velocidad angular crece al acercarse al planeta.
            if (!Preferencias.ReducirMovimiento)
                angulo += Time.unscaledDeltaTime * Mathf.PI * 2f / vueltaSegundos * Trayectoria.Ritmo(orbitaActual, angulo);
            marcador.localPosition = Trayectoria.Punto(orbitaActual, angulo, radio);
        }

        void Dibujar(string id)
        {
            linea.enabled = !string.IsNullOrEmpty(id);
            if (!linea.enabled) return;
            orbitaActual = id;
            // La forma de cada órbita está en Trayectoria (la misma en la cinemática y en la investigación).
            for (int i = 0; i < Puntos; i++)
                linea.SetPosition(i, Trayectoria.Punto(id, i * Mathf.PI * 2f / Puntos, radio));
        }
    }
}
