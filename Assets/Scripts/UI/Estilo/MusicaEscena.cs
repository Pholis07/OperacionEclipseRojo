using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Música en bucle de la escena (y una capa de ambiente opcional) con entrada suave.
    /// En WebGL el navegador solo deja sonar audio después del primer clic o tecla; Unity lo reanuda solo.
    /// </summary>
    public class MusicaEscena : MonoBehaviour
    {
        [SerializeField] ArteDatos arte;
        [SerializeField] ArteDatos.Musica musica = ArteDatos.Musica.Menu;
        [SerializeField] bool conAmbiente = true;
        [Tooltip("En Resultado: tema de victoria, parcial o fracaso según la misión lanzada.")]
        [SerializeField] bool segunResultado;
        [SerializeField] float entrada = 2f;

        AudioSource fuenteMusica;
        AudioSource fuenteAmbiente;
        float factor;

        void Start()
        {
            if (arte == null) return;
            if (segunResultado && EstadoJuego.Resultado?.Categoria != null)
            {
                var c = EstadoJuego.Resultado.Categoria.Value;
                musica = c == CategoriaResultado.Victoria ? ArteDatos.Musica.Victoria
                    : c == CategoriaResultado.Parcial ? ArteDatos.Musica.Parcial : ArteDatos.Musica.Fracaso;
                conAmbiente = false;
            }
            fuenteMusica = Crear(arte.Clip(musica));
            if (conAmbiente) fuenteAmbiente = Crear(arte.ambienteTormenta);
            Preferencias.Cambiaron += Aplicar;
            StartCoroutine(Entrar());
        }

        void OnDestroy() => Preferencias.Cambiaron -= Aplicar;

        /// <summary>Cambia de tema con un fundido corto (por ejemplo, al pasar a la Operación).</summary>
        public void Cambiar(ArteDatos.Musica nueva)
        {
            if (arte == null || fuenteMusica == null || nueva == musica) return;
            musica = nueva;
            StopAllCoroutines();
            StartCoroutine(Cruzar(arte.Clip(nueva)));
        }

        AudioSource Crear(AudioClip clip)
        {
            if (clip == null) return null;
            var f = gameObject.AddComponent<AudioSource>();
            f.clip = clip;
            f.loop = true;
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            f.volume = 0f;
            f.Play();
            return f;
        }

        IEnumerator Entrar()
        {
            float inicio = Time.unscaledTime;
            while (factor < 1f)
            {
                factor = Mathf.Clamp01((Time.unscaledTime - inicio) / entrada);
                Aplicar();
                yield return null;
            }
        }

        IEnumerator Cruzar(AudioClip clip)
        {
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime * 2f) { factor = t; Aplicar(); yield return null; }
            fuenteMusica.clip = clip;
            if (clip != null) fuenteMusica.Play();
            yield return Entrar();
        }

        void Aplicar()
        {
            if (fuenteMusica != null) fuenteMusica.volume = factor * Preferencias.VolumenMusica;
            if (fuenteAmbiente != null) fuenteAmbiente.volume = factor * Preferencias.VolumenMusica * 0.5f;
        }
    }
}
