using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMission
{
    /// <summary>
    /// Velo de título del Menú: «PULSA CUALQUIER TECLA». Sirve además para que el navegador permita el audio
    /// (necesita un gesto del jugador). Se muestra una sola vez por sesión. Es solo un velo encima: los botones
    /// del Menú existen y funcionan debajo desde el principio.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PantallaTitulo : MonoBehaviour
    {
        [SerializeField] TMPro.TMP_Text aviso;
        [SerializeField] GameObject focoAlCerrar;

        static bool yaMostrada;
        CanvasGroup grupo;
        bool cerrando;

        void Awake()
        {
            grupo = GetComponent<CanvasGroup>();
            if (yaMostrada) gameObject.SetActive(false);
        }

        void Update()
        {
            if (cerrando) return;
            // El aviso respira para indicar que espera al jugador.
            if (aviso != null) aviso.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
            if (AlgoPulsado()) StartCoroutine(Cerrar());
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
            ReproductorUI.Sonar(ArteDatos.Sonido.Click);
            grupo.blocksRaycasts = false;
            yield return Animar.Durante(0.5f, t => grupo.alpha = 1f - t);
            if (focoAlCerrar != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(focoAlCerrar);
            gameObject.SetActive(false);
        }
    }
}
