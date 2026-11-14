using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Forma visual de cada órbita, en un solo sitio: la usan el anillo de Configuración, la cinemática y la
    /// investigación orbital (así las tres muestran la misma órbita). Son parámetros VISUALES, no astrodinámica:
    /// - Polar baja: casi de polo a polo (80°), la más cercana.
    /// - Media inclinada: 45°, circular y algo más alta.
    /// - Elíptica: 22°, excentricidad 0.42 con el planeta en un foco; el periapsis queda siempre fuera del planeta.
    /// Las distancias van en «radios de órbita»: radio = 1 es la órbita circular de referencia.
    /// </summary>
    public static class Trayectoria
    {
        public static float Inclinacion(string orbitaId) => orbitaId == "polar" ? 80f : orbitaId == "media" ? 45f : 22f;

        public static float Excentricidad(string orbitaId) => orbitaId == "eliptica" ? 0.42f : 0f;

        /// <summary>Radio relativo de la órbita circular (la polar va más baja que la media).</summary>
        public static float Escala(string orbitaId) => orbitaId == "polar" ? 0.92f : 1f;

        public static Quaternion Plano(string orbitaId) => Quaternion.Euler(Inclinacion(orbitaId), 0f, 20f);

        /// <summary>
        /// Distancia al centro para el ángulo dado. Circular: «radio». Elíptica: el punto más cercano queda al 88 %
        /// de «radio» (fuera del planeta) y el planeta está en un foco.
        /// </summary>
        public static float Radio(float angulo, float excentricidad, float radio)
        {
            if (excentricidad <= 0f) return radio;
            float semilatus = radio * 0.88f * (1f + excentricidad);
            return semilatus / (1f + excentricidad * Mathf.Cos(angulo));
        }

        /// <summary>Punto de la órbita (relativo al centro del planeta, sin escala del planeta).</summary>
        public static Vector3 Punto(string orbitaId, float angulo, float radio)
        {
            float r = Radio(angulo, Excentricidad(orbitaId), radio * Escala(orbitaId));
            return Plano(orbitaId) * new Vector3(Mathf.Cos(angulo) * r, 0f, Mathf.Sin(angulo) * r);
        }

        /// <summary>Velocidad angular relativa (segunda ley de Kepler aproximada): más rápido cerca del planeta.</summary>
        public static float Ritmo(string orbitaId, float angulo)
        {
            float r = Radio(angulo, Excentricidad(orbitaId), 1f);
            return 1f / (r * r);
        }
    }
}
