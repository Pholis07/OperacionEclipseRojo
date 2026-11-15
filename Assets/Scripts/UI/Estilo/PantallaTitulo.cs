using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>
    /// Velo de título del Menú: «PULSA CUALQUIER TECLA». Sirve además para que el navegador permita el audio
    /// (necesita un gesto del jugador). Se muestra una sola vez por sesión. Es solo un velo encima: los botones
    /// del Menú existen y funcionan debajo desde el principio.
    ///
    /// Entrada: la línea del evento, el título letra a letra con el espaciado cerrándose, una línea que se dibuja,
    /// el lema, el aviso y la franja del patrocinador. Al pulsar: destello, el velo se abre y los bloques del Menú
    /// (<see cref="entradas"/>) entran escalonados. Con «Reducir movimiento» todo aparece al instante.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PantallaTitulo : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text aviso;
        [SerializeField] GameObject focoAlCerrar;
        [SerializeField] TMPro.TMP_Text etiqueta;
        [SerializeField] TMPro.TMP_Text titulo;
        [SerializeField] RectTransform linea;
        [SerializeField] TMPro.TMP_Text lema;
        [SerializeField] CanvasGroup patrocinio;
        [SerializeField] RectTransform contenido;
        [SerializeField] UnityEngine.UI.Graphic destello;
        [SerializeField] CanvasGroup[] entradas;
        [SerializeField] AudioClip sonidoCerrar;

        static bool yaMostrada;
        CanvasGroup grupo;
        bool cerrando, avisoListo;
        float espaciadoFinal;

        void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
            if (yaMostrada)
            {
                gameObject.SetActive(false);
                return;
            }
            // Los bloques del Menú esperan escondidos bajo el velo y entran al cerrarlo (siguen siendo pulsables).
            foreach (var e in entradas) if (e != null) e.alpha = 0f;
            if (titulo != null) espaciadoFinal = titulo.characterSpacing;
        }

        void Start()
        {
            if (!yaMostrada) StartCoroutine(Entrada());
        }

        void Update()
        {
            if (cerrando) return;
            // Sin foco mientras está el velo: Enter cierra el título y no pulsa a la vez «Iniciar misión».
            var sistema = UnityEngine.EventSystems.EventSystem.current;
            if (sistema != null && sistema.currentSelectedGameObject != null) sistema.SetSelectedGameObject(null);
            // El aviso respira para indicar que espera al jugador.
            if (aviso != null && avisoListo) aviso.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
            if (AlgoPulsado())
            {
                StopAllCoroutines(); // corta la entrada si aún no terminó
                StartCoroutine(Cerrar());
            }
        }

        IEnumerator Entrada()
        {
            if (aviso != null) aviso.alpha = 0f;
            if (patrocinio != null) patrocinio.alpha = 0f;
            if (lema != null) lema.alpha = 0f;
            if (linea != null) linea.localScale = new Vector3(0f, 1f, 1f);
            if (etiqueta != null) etiqueta.alpha = 0f;
            int letras = 0;
            if (titulo != null)
            {
                titulo.ForceMeshUpdate();
                letras = titulo.textInfo.characterCount;
                titulo.maxVisibleCharacters = 0;
            }

            yield return Esperar(0.25f);
            if (etiqueta != null) StartCoroutine(Animar.Durante(0.7f, t => etiqueta.alpha = t));
            yield return Esperar(0.35f);
            if (titulo != null)
                StartCoroutine(Animar.Durante(1.3f, t =>
                {
                    titulo.maxVisibleCharacters = Mathf.CeilToInt(letras * Mathf.Clamp01(t * 1.6f));
                    titulo.characterSpacing = Mathf.Lerp(espaciadoFinal + 40f, espaciadoFinal, t);
                }));
            yield return Esperar(1.0f);
            if (linea != null) StartCoroutine(Animar.Durante(0.6f, t => linea.localScale = new Vector3(t, 1f, 1f)));
            if (lema != null) StartCoroutine(Animar.Durante(0.8f, t => lema.alpha = t));
            yield return Esperar(0.5f);
            if (patrocinio != null) StartCoroutine(Animar.Durante(0.8f, t => patrocinio.alpha = t));
            avisoListo = true;
        }

        static IEnumerator Esperar(float segundos)
        {
            if (Preferencias.ReducirMovimiento) yield break;
            float fin = Time.unscaledTime + segundos;
            while (Time.unscaledTime < fin) yield return null;
        }

        static bool AlgoPulsado()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        }

        IEnumerator Cerrar()
        {
            cerrando = true;
            yaMostrada = true;
            var fuente = GetComponent<AudioSource>();
            if (sonidoCerrar != null && fuente != null) fuente.PlayOneShot(sonidoCerrar, Preferencias.VolumenEfectos);
            else ReproductorUI.Sonar(ArteDatos.Sonido.Click);
            grupo.blocksRaycasts = false;

            // Lo que no terminó de entrar se completa de golpe.
            if (titulo != null) { titulo.maxVisibleCharacters = 99999; titulo.characterSpacing = espaciadoFinal; }
            if (linea != null) linea.localScale = Vector3.one;
            if (etiqueta != null) etiqueta.alpha = 1f;
            if (lema != null) lema.alpha = 1f;
            if (patrocinio != null) patrocinio.alpha = 1f;

            if (destello != null)
                StartCoroutine(Animar.Durante(0.45f, t => { var c = destello.color; c.a = 0.35f * (1f - t); destello.color = c; }));
            var escalaInicial = contenido != null ? contenido.localScale : Vector3.one;
            yield return Animar.Durante(0.55f, t =>
            {
                grupo.alpha = 1f - t;
                if (contenido != null) contenido.localScale = escalaInicial * (1f + 0.08f * t);
            });
            if (focoAlCerrar != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focoAlCerrar);
            // Los bloques del Menú entran escalonados. El velo ya no tapa nada: se apaga al terminar.
            for (int i = 0; i < entradas.Length; i++)
            {
                var e = entradas[i];
                if (e == null) continue;
                var rt = (RectTransform)e.transform;
                var destino = rt.anchoredPosition;
                StartCoroutine(Animar.Durante(0.45f, t =>
                {
                    e.alpha = t;
                    rt.anchoredPosition = destino + new Vector2(0f, -26f * (1f - t));
                }));
                yield return Esperar(0.08f);
            }
            yield return Esperar(0.5f);
            foreach (var e in entradas) if (e != null) e.alpha = 1f;
            gameObject.SetActive(false);
        }

        void OnDisable()
        {
            // Si el objeto se apaga a mitad (cambio de escena), el Menú no se queda invisible.
            if (cerrando) foreach (var e in entradas) if (e != null) e.alpha = 1f;
        }
    }
}
