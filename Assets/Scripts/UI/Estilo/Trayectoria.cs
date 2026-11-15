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
        /// <summary>Forma y color con que se dibuja una órbita (no son sus datos físicos: esos están en OrbitaDatos).</summary>
        public struct Forma
        {
            public float Inclinacion, Excentricidad, Escala;
            public string Color;
            public bool NoAEscala;   // se dibuja más cerca de lo real y se avisa («Esquema no a escala»)
            public Forma(float inclinacion, float excentricidad, float escala, string color, bool noAEscala = false)
            { Inclinacion = inclinacion; Excentricidad = excentricidad; Escala = escala; Color = color; NoAEscala = noAEscala; }
        }

        /// <summary>
        /// Versión 7: las cinco órbitas en una tabla. Las dos nuevas: ecuatorial baja (casi plana, baja) y
        /// areoestacionaria (ecuatorial y muy lejana: a 6 radios de Marte no cabría, se dibuja a ~2.3 y se avisa).
        /// </summary>
        static readonly System.Collections.Generic.Dictionary<string, Forma> Formas = new System.Collections.Generic.Dictionary<string, Forma>
        {
            { "polar", new Forma(80f, 0f, 0.92f, "#5EC8E5") },
            { "media", new Forma(45f, 0f, 1f, "#6EE7A0") },
            { "eliptica", new Forma(22f, 0.42f, 1f, "#F5B342") },
            { "ecuatorial", new Forma(6f, 0f, 0.9f, "#C4A1FF") },
            { "areoestacionaria", new Forma(8f, 0f, 1.75f, "#FF8FB8", true) },
        };

        static readonly Forma PorDefecto = new Forma(22f, 0f, 1f, "#5EC8E5");

        public static Forma De(string orbitaId) =>
            orbitaId != null && Formas.TryGetValue(orbitaId, out var f) ? f : PorDefecto;

        /// <summary>Ids de las órbitas que se saben dibujar.</summary>
        public static System.Collections.Generic.IEnumerable<string> Ids => Formas.Keys;

        public static float Inclinacion(string orbitaId) => De(orbitaId).Inclinacion;

        public static float Excentricidad(string orbitaId) => De(orbitaId).Excentricidad;

        /// <summary>Radio relativo de la órbita circular (la polar va más baja que la media).</summary>
        public static float Escala(string orbitaId) => De(orbitaId).Escala;

        /// <summary>La órbita se dibuja más cerca de lo real (areoestacionaria): hay que avisarlo.</summary>
        public static bool NoAEscala(string orbitaId) => De(orbitaId).NoAEscala;

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

        /// <summary>
        /// Color propio de cada órbita (para distinguirlas a simple vista en Configuración, la cinemática y la
        /// investigación): polar cian, media verde, elíptica ámbar.
        /// </summary>
        public static Color Color(string orbitaId)
        {
            ColorUtility.TryParseHtmlString(ColorHex(orbitaId), out var c);
            return c;
        }

        public static string ColorHex(string orbitaId) => De(orbitaId).Color;

        /// <summary>Velocidad angular relativa (segunda ley de Kepler aproximada): más rápido cerca del planeta.</summary>
        public static float Ritmo(string orbitaId, float angulo)
        {
            float r = Radio(angulo, Excentricidad(orbitaId), 1f);
            return 1f / (r * r);
        }
    }
}
