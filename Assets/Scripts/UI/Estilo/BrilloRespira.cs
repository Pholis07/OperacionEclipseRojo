using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Pulso lento de un brillo (alfa y un poco de escala), por ejemplo el Sol asomando tras Marte en el Menú.
    /// Solo adorno. Con «Reducir movimiento» se queda fijo.
    /// </summary>
    public class BrilloRespira : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] float periodo = 7f;
        [SerializeField, Range(0f, 1f)] float variacion = 0.25f;

        float alfaBase;
        Vector3 escalaBase;

        void Awake()
        {
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) return;
            alfaBase = sprite.color.a;
            escalaBase = sprite.transform.localScale;
        }

        void Update()
        {
            if (sprite == null || Preferencias.ReducirMovimiento) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / periodo);
            var c = sprite.color;
            c.a = alfaBase * (1f - variacion + variacion * k);
            sprite.color = c;
            sprite.transform.localScale = escalaBase * (1f + 0.04f * k);
        }
    }
}
