using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// La órbita elegida alrededor del Marte de Configuración, con su forma (Trayectoria) y su color.
    /// Versión 7 (requisito del paquete de diseño): en el visor se ve UNA sola trayectoria, la elegida, con su halo,
    /// su marcador y su estela; las demás no se dibujan. Sin elección, Marte queda sin trayectorias. La lista de
    /// opciones (ConfiguracionUI) sí muestra todas. En la pestaña Órbita, una órbita excéntrica marca además su
    /// punto más cercano y el más lejano. El nombre va en el rótulo fijo del visor (GuiaConfiguracion).
    /// El marcador recorre la elegida (más rápido cerca del planeta) con una estela que se desvanece detrás.
    /// Solo presentación: lee EstadoJuego.Configuracion.OrbitaId y la pestaña de ConfiguracionUI.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AnilloOrbita : MonoBehaviour
    {
        [SerializeField] float radio = 1.35f; // en radios del planeta (escala local de la esfera = 1)
        [SerializeField] Transform marcador;
        [SerializeField] float vueltaSegundos = 9f;
        [SerializeField] LineRenderer[] lineas;        // (escenas anteriores) líneas de las otras órbitas: siempre apagadas
        [SerializeField] LineRenderer brillo;          // halo ancho de la elegida
        [SerializeField] LineRenderer estela;          // detrás del marcador
        [SerializeField] Transform[] etiquetas;        // (escenas anteriores) etiquetas flotantes: siempre apagadas
        [SerializeField] Transform[] extremos;         // «más cerca» y «más lejos» de la órbita excéntrica elegida
        [SerializeField] SpriteRenderer haloMarcador;
        [SerializeField] ConfiguracionUI configuracion;
        [SerializeField] float anchoElegida = 0.16f;
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
            if (lineas != null) foreach (var l in lineas) if (l != null) l.enabled = false;
            ColocarEtiquetas();
        }

        void Update()
        {
            // Versión 7: en el visor se ve SOLO la órbita elegida (una línea, un marcador). Sin elección, ninguna.
            // Fuera de la Configuración (Briefing, Operación) tampoco se dibuja nada.
            bool fuera = configuracion != null && !configuracion.gameObject.activeInHierarchy;
            var id = fuera ? null : EstadoJuego.Configuracion.OrbitaId;
            // «todas» ya solo significa «estoy en la pestaña Órbita»: allí se ven además los extremos de la elíptica.
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
            // Las líneas y etiquetas de «las otras» órbitas ya no se muestran nunca (escenas viejas las traen).
            if (lineas != null) foreach (var l in lineas) if (l != null) l.enabled = false;
            if (etiquetas != null) foreach (var e in etiquetas) if (e != null) e.gameObject.SetActive(false);
            // Extremos (más cerca / más lejos): solo de la órbita elegida, si es excéntrica, y en su pestaña.
            bool conExtremos = todasVisibles && hayElegida && Trayectoria.Excentricidad(dibujada) > 0f;
            if (extremos != null)
                foreach (var e in extremos)
                    if (e != null) e.gameObject.SetActive(conExtremos);
            if (conExtremos && extremos != null && extremos.Length >= 2)
            {
                if (extremos[0] != null) extremos[0].localPosition = Trayectoria.Punto(dibujada, 0f, radio);
                if (extremos[1] != null) extremos[1].localPosition = Trayectoria.Punto(dibujada, Mathf.PI, radio);
            }
        }

        // ---------- Para la cámara y las pruebas ----------

        /// <summary>Trayectorias que se ven ahora mismo: 0 (sin elección) o 1 (la elegida). Nunca más.</summary>
        public int TrayectoriasVisibles
        {
            get
            {
                int n = linea != null && linea.enabled ? 1 : 0;
                if (lineas != null) foreach (var l in lineas) if (l != null && l.enabled) n++;
                return n;
            }
        }

        /// <summary>Marcadores de la nave visibles: 0 o 1.</summary>
        public int MarcadoresVisibles => marcador != null && marcador.gameObject.activeSelf ? 1 : 0;

        /// <summary>La órbita que se está dibujando (null si ninguna).</summary>
        public string Dibujada => string.IsNullOrEmpty(dibujada) || dibujada == "-" ? null : dibujada;

        public Vector3 Centro => transform.position;

        /// <summary>Radio del planeta en el mundo (la superficie está a 0.5 en unidades locales).</summary>
        public float RadioPlaneta => transform.lossyScale.x * 0.5f;

        /// <summary>Puntos de una órbita en el mundo (para que la cámara la encuadre entera).</summary>
        public Vector3[] PuntosMundo(string id, int cuantos = 48)
        {
            var puntos = new Vector3[cuantos];
            for (int i = 0; i < cuantos; i++)
                puntos[i] = transform.TransformPoint(Trayectoria.Punto(id, i * Mathf.PI * 2f / cuantos, radio));
            return puntos;
        }

        /// <summary>Recalcula ya lo que se ve (las pruebas en modo edición no tienen Update).</summary>
        public void Actualizar()
        {
            if (linea == null) Awake();
            Update();
        }

        /// <summary>(Versión 7) Ya no hay etiquetas flotantes por órbita: el nombre va en el rótulo fijo del visor.</summary>
        void ColocarEtiquetas() { }

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
