using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Suena los efectos de la interfaz en la escena actual. Hay uno por escena; «Actual» apunta al de la escena
    /// abierta solo para que los botones lo encuentren sin referencias (no guarda estado del juego).
    /// Sin clips asignados no suena nada y no da errores.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ReproductorUI : MonoBehaviour
    {
        [SerializeField] ArteDatos arte;

        AudioSource fuente;

        public static ReproductorUI Actual { get; private set; }
        public ArteDatos Arte => arte;

        void Awake()
        {
            fuente = GetComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
        }

        void OnEnable()
        {
            Actual = this;
            ultimoTiempo = -1f;   // sin recargar el dominio (editor), el reloj vuelve a 0 y el enfriamiento callaría todo
        }

        void OnDisable()
        {
            colaVoz.Clear();
            if (Actual == this) Actual = null;
        }

        /// <summary>Peticiones de sonido desde que arrancó (para las pruebas: reconstruir una vista no debe sonar).</summary>
        public static int Peticiones { get; private set; }

        const float Enfriamiento = 0.12f;   // una acción = un sonido: dentro de este tiempo solo gana el más importante
        static float ultimoTiempo = -1f;
        static ArteDatos.Sonido ultimoSonido;

        /// <summary>Navegar (0) &lt; montar o quitar (1) &lt; avisos y semáforo (2) &lt; lanzamiento y señal (3).</summary>
        static int Prioridad(ArteDatos.Sonido s) =>
            s == ArteDatos.Sonido.Hover || s == ArteDatos.Sonido.Click || s == ArteDatos.Sonido.Pestana ? 0
            : s == ArteDatos.Sonido.Elegir || s == ArteDatos.Sonido.Quitar ? 1
            : s == ArteDatos.Sonido.Lanzamiento || s == ArteDatos.Sonido.Senal ? 3 : 2;

        public static void Sonar(ArteDatos.Sonido sonido, float volumen = 1f)
        {
            Peticiones++;
            if (Actual == null || Actual.arte == null) return;
            var clip = Actual.arte.Clip(sonido);
            if (clip == null) return;
            if (Time.unscaledTime - ultimoTiempo < Enfriamiento)
            {
                if (Prioridad(sonido) <= Prioridad(ultimoSonido)) return;   // repetido o menos importante: se calla
                Actual.fuente.Stop();                                        // más importante: sustituye al anterior
            }
            ultimoTiempo = Time.unscaledTime;
            ultimoSonido = sonido;
            Actual.fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }

        /// <summary>
        /// Pone una voz (transmisiones) en fila: no corta la que está sonando, empieza cuando termina.
        /// Así, si llegan varias transmisiones juntas, se oyen todas y sin encimarse.
        /// </summary>
        public static void Hablar(AudioClip clip) => Actual?.Encolar(clip);

        /// <summary>Corta la voz que suena y las que esperan (al salir de la Operación o al saltar).</summary>
        public static void Callar() => Actual?.CallarVoces();

        /// <summary>La voz que está sonando, o null.</summary>
        public static AudioClip VozSonando =>
            Actual != null && Actual.vozFuente != null && Actual.vozFuente.isPlaying ? Actual.vozFuente.clip : null;

        /// <summary>Voces de esta escena que esperan turno (sin contar la que suena).</summary>
        public int VocesEnFila => colaVoz.Count;

        public void Encolar(AudioClip clip)
        {
            if (clip == null) return;
            VozFuente();
            colaVoz.Enqueue(clip);
        }

        public void CallarVoces()
        {
            colaVoz.Clear();
            if (vozFuente != null) vozFuente.Stop();
        }

        readonly System.Collections.Generic.Queue<AudioClip> colaVoz = new System.Collections.Generic.Queue<AudioClip>();
        AudioSource vozFuente;

        void Update() => SiguienteVoz();

        /// <summary>Si no suena ninguna voz, empieza la siguiente de la fila.</summary>
        public void SiguienteVoz()
        {
            if (colaVoz.Count == 0 || vozFuente == null || vozFuente.isPlaying) return;
            vozFuente.clip = colaVoz.Dequeue();
            vozFuente.volume = Preferencias.VolumenVoz;
            vozFuente.Play();
        }

        AudioSource VozFuente()
        {
            if (vozFuente == null)
            {
                vozFuente = gameObject.AddComponent<AudioSource>();
                vozFuente.playOnAwake = false;
                vozFuente.spatialBlend = 0f;
                Atenuacion.Registrar(vozFuente);
            }
            return vozFuente;
        }
    }
}
