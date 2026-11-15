using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Órbitas alrededor del Marte de Configuración. Cada una tiene su forma (Trayectoria) y su color:
    /// - En la pestaña Órbita se ven las tres para compararlas: la elegida gruesa, brillante y con un halo; las
    ///   otras finas y tenues. Sin ninguna elegida, las tres a media intensidad. Cada una lleva su nombre al lado
    ///   y la elíptica marca su punto más cercano y el más lejano.
    /// - En las demás pestañas solo queda la elegida (de fondo), sin etiquetas.
    /// El marcador recorre la elegida (más rápido cerca del planeta) con una estela que se desvanece detrás.
    /// Solo presentación: lee EstadoJuego.Configuracion.OrbitaId y la pestaña de ConfiguracionUI.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AnilloOrbita : MonoBehaviour
    {
        [SerializeField] float radio = 1.35f; // en radios del planeta (escala local de la esfera = 1)
        [SerializeField] Transform marcador;
        [SerializeField] float vueltaSegundos = 9f;
        [SerializeField] string[] idsOrbitas = { "polar", "media", "eliptica" };
        [SerializeField] LineRenderer[] lineas;        // una por órbita, en el orden de idsOrbitas
        [SerializeField] LineRenderer brillo;          // halo ancho de la elegida
        [SerializeField] LineRenderer estela;          // detrás del marcador
        [SerializeField] Transform[] etiquetas;        // nombre de cada órbita (TextMeshPro 3D)
        [SerializeField] Transform[] extremos;         // «más cerca» y «más lejos» de la elíptica
        [SerializeField] SpriteRenderer haloMarcador;
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] float anchoElegida = 0.16f, anchoOtra = 0.085f;
        const int Puntos = 128, PuntosEstela = 40;
        const float LargoEstela = 1.5f;

        LineRenderer linea;            // la del propio objeto: la elegida (las pruebas y capturas la leen)
        string dibujada = "-";
        bool todasVisibles;
        float aparicion;
        float angulo;
        string orbitaActual;

        void Awake()
        {
            linea = GetComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.loop = true;
            linea.positionCount = Puntos;
            if (lineas != null)
                for (int i = 0; i < lineas.Length && i < idsOrbitas.Length; i++)
                    DibujarEn(lineas[i], idsOrbitas[i], true);
            ColocarEtiquetas();
        }

        void Update()
        {
            // Versión 6: fuera de la Configuración (Briefing, Operación) no se dibuja ninguna órbita ni etiqueta;
            // antes se veían todas detrás del texto del Briefing.
            bool fuera = configuracion != null && !configuracion.gameObject.activeInHierarchy;
            var id = fuera ? null : EstadoJuego.Configuracion.OrbitaId;
            bool todas = !fuera && (configuracion == null || configuracion.PestanaActual == ConfiguracionUI.Pestana.Orbita);
            if (id != dibujada || todas != todasVisibles)
            {
                if (id != dibujada) aparicion = 0f;
                dibujada = id;
                todasVisibles = todas;
                Dibujar(id);
            }
            if (aparicion < 1f)
            {
                aparicion = Preferencias.ReducirMovimiento ? 1f : Mathf.Min(1f, aparicion + Time.unscaledDeltaTime * 2.5f);
                AplicarColores();
            }
            MoverMarcador();
            Orientar();
        }

        void MoverMarcador()
        {
            if (marcador == null) return;
            bool visible = linea.enabled;
            if (marcador.gameObject.activeSelf != visible) marcador.gameObject.SetActive(visible);
            if (estela != null) estela.enabled = visible;
            if (!visible) return;
            // Segunda ley de Kepler (aproximada): la velocidad angular crece al acercarse al planeta.
            if (!Preferencias.ReducirMovimiento)
                angulo += Time.unscaledDeltaTime * Mathf.PI * 2f / vueltaSegundos * Trayectoria.Ritmo(orbitaActual, angulo);
            marcador.localPosition = Trayectoria.Punto(orbitaActual, angulo, radio);
            if (estela != null)
            {
                estela.positionCount = PuntosEstela;
                for (int i = 0; i < PuntosEstela; i++)
                    estela.SetPosition(i, Trayectoria.Punto(orbitaActual, angulo - LargoEstela * (1f - i / (PuntosEstela - 1f)), radio));
            }
        }

        void Dibujar(string id)
        {
            linea.enabled = !string.IsNullOrEmpty(id);
            if (linea.enabled)
            {
                orbitaActual = id;
                // La forma de cada órbita está en Trayectoria (la misma en la cinemática y en la investigación).
                DibujarEn(linea, id, true);
                if (brillo != null) DibujarEn(brillo, id, true);
            }
            if (brillo != null) brillo.enabled = linea.enabled;
            AplicarColores();
        }

        void DibujarEn(LineRenderer l, string id, bool cerrada)
        {
            if (l == null) return;
            l.useWorldSpace = false;
            l.loop = cerrada;
            l.positionCount = Puntos;
            for (int i = 0; i < Puntos; i++)
                l.SetPosition(i, Trayectoria.Punto(id, i * Mathf.PI * 2f / Puntos, radio));
        }

        void AplicarColores()
        {
            float a = Animar.SalidaCubica(aparicion);
            bool hayElegida = !string.IsNullOrEmpty(dibujada);
            if (hayElegida)
            {
                var c = Trayectoria.Color(dibujada);
                linea.widthMultiplier = anchoElegida;
                linea.startColor = linea.endColor = new Color(c.r, c.g, c.b, 0.95f * a);
                if (brillo != null) brillo.startColor = brillo.endColor = new Color(c.r, c.g, c.b, 0.16f * a);
                if (estela != null)
                {
                    estela.startColor = new Color(c.r, c.g, c.b, 0f);
                    estela.endColor = new Color(Mathf.Lerp(c.r, 1f, 0.4f), Mathf.Lerp(c.g, 1f, 0.4f), Mathf.Lerp(c.b, 1f, 0.4f), 0.95f);
                }
                if (haloMarcador != null) haloMarcador.color = new Color(c.r, c.g, c.b, 0.8f);
                if (marcador != null)
                {
                    var r = marcador.GetComponent<Renderer>();
                    if (r != null)
                    {
                        var bloque = new MaterialPropertyBlock();
                        r.GetPropertyBlock(bloque);
                        bloque.SetColor("_BaseColor", c);
                        bloque.SetColor("_EmissionColor", c * 3f);
                        r.SetPropertyBlock(bloque);
                    }
                }
            }
            if (lineas != null)
                for (int i = 0; i < lineas.Length && i < idsOrbitas.Length; i++)
                {
                    var l = lineas[i];
                    if (l == null) continue;
                    // La elegida ya la dibuja la línea principal; las demás solo se ven en la pestaña Órbita.
                    l.enabled = todasVisibles && idsOrbitas[i] != dibujada;
                    var c = Trayectoria.Color(idsOrbitas[i]);
                    float alfa = hayElegida ? 0.42f : 0.7f;
                    l.widthMultiplier = hayElegida ? anchoOtra : anchoOtra * 1.4f;
                    l.startColor = l.endColor = new Color(c.r, c.g, c.b, alfa);
                }
            if (etiquetas != null)
                for (int i = 0; i < etiquetas.Length && i < idsOrbitas.Length; i++)
                {
                    if (etiquetas[i] == null) continue;
                    etiquetas[i].gameObject.SetActive(todasVisibles);
                    var texto = etiquetas[i].GetComponent<TMPro.TMP_Text>();
                    if (texto != null)
                    {
                        bool esta = idsOrbitas[i] == dibujada;
                        var c = Trayectoria.Color(idsOrbitas[i]);
                        texto.color = new Color(c.r, c.g, c.b, !hayElegida ? 0.9f : esta ? 1f : 0.45f);
                        texto.fontStyle = esta ? TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase : TMPro.FontStyles.UpperCase;
                    }
                }
            if (extremos != null)
                foreach (var e in extremos)
                    if (e != null) e.gameObject.SetActive(todasVisibles && (dibujada == "eliptica" || !hayElegida));
        }

        /// <summary>Etiquetas en el punto más alto de cada órbita, un poco hacia fuera; extremos de la elíptica.</summary>
        void ColocarEtiquetas()
        {
            if (etiquetas != null)
                for (int i = 0; i < etiquetas.Length && i < idsOrbitas.Length; i++)
                {
                    if (etiquetas[i] == null) continue;
                    float mejor = 0f, alto = float.MinValue;
                    for (int k = 0; k < 64; k++)
                    {
                        float a = k * Mathf.PI * 2f / 64f;
                        var p = Trayectoria.Punto(idsOrbitas[i], a, radio);
                        // Lejos del disco del planeta visto de frente (XY) y hacia la cámara (−Z): así la etiqueta no
                        // queda encima ni detrás de Marte. Cada órbita prefiere un lado para que no se encimen.
                        var lado = i == 0 ? new Vector2(-0.3f, 1f) : i == 1 ? new Vector2(1f, 0.6f) : new Vector2(-1f, -0.4f);
                        float puntaje = new Vector2(p.x, p.y).magnitude - p.z * 0.4f + Vector2.Dot(new Vector2(p.x, p.y).normalized, lado.normalized) * 0.25f;
                        if (puntaje > alto) { alto = puntaje; mejor = a; }
                    }
                    var punto = Trayectoria.Punto(idsOrbitas[i], mejor, radio);
                    var fuera = new Vector3(punto.x, punto.y, 0f).normalized;
                    etiquetas[i].localPosition = punto + fuera * 0.07f;
                }
            if (extremos != null && extremos.Length >= 2)
            {
                // En la elíptica el punto más cercano está en el ángulo 0 y el más lejano en π (Trayectoria.Radio).
                var cerca = Trayectoria.Punto("eliptica", 0f, radio);
                var lejos = Trayectoria.Punto("eliptica", Mathf.PI, radio);
                if (extremos[0] != null) extremos[0].localPosition = cerca;
                if (extremos[1] != null) extremos[1].localPosition = lejos;
            }
        }

        /// <summary>Las etiquetas miran siempre a la cámara.</summary>
        void Orientar()
        {
            var cam = Camera.main;
            if (cam == null) return;
            void Mirar(Transform t)
            {
                if (t != null && t.gameObject.activeSelf) t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, cam.transform.up);
            }
            if (etiquetas != null) foreach (var e in etiquetas) Mirar(e);
            if (extremos != null) foreach (var e in extremos) Mirar(e);
            if (haloMarcador != null) Mirar(haloMarcador.transform);
        }
    }
}
