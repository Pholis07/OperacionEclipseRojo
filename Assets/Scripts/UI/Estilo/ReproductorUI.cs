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

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;
        }

        public static void Sonar(ArteDatos.Sonido sonido, float volumen = 1f)
        {
            if (Actual == null || Actual.arte == null) return;
            var clip = Actual.arte.Clip(sonido);
            if (clip != null) Actual.fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }

        /// <summary>Reproduce una voz (transmisiones) cortando la anterior.</summary>
        public static void Hablar(AudioClip clip)
        {
            if (Actual == null) return;
            var voz = Actual.VozFuente();
            voz.Stop();
            if (clip == null) return;
            voz.clip = clip;
            voz.volume = Preferencias.VolumenVoz;
            voz.Play();
        }

        AudioSource vozFuente;

        AudioSource VozFuente()
        {
            if (vozFuente == null)
            {
                vozFuente = gameObject.AddComponent<AudioSource>();
                vozFuente.playOnAwake = false;
                vozFuente.spatialBlend = 0f;
            }
            return vozFuente;
        }
    }
}
