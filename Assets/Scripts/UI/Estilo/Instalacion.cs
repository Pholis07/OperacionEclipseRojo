using System.Collections;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Montaje físico de un módulo de instrumento en la nave del constructor. Al aparecer el módulo (NaveUI lo
    /// enciende en el mismo cuadro en que se elige), se ve en seis tiempos:
    /// 1) llega desde un lateral, lejos del casco; 2) se orienta a su soporte; 3) se aproxima frenando;
    /// 4) asienta; 5) cierran las dos fijaciones; 6) se enciende el indicador de conexión (y un destello).
    /// La lógica no espera: el módulo ya está activo y los recursos ya están calculados. Si se quita o se
    /// desactiva a mitad, queda en su pose final. Con «Reducir movimiento», pose final directa.
    /// En la nave de órbita y en la del informe («animar» apagado) el módulo aparece ya montado.
    /// </summary>
    public class Instalacion : MonoBehaviour
    {
        [SerializeField] SpriteRenderer destello;
        [SerializeField] Transform[] fijaciones = new Transform[0];
        [SerializeField] GameObject indicador;
        [SerializeField] bool animar = true;
        [SerializeField] float distanciaLlegada = 1.1f;
        [SerializeField] float giroFijacionAbierta = 70f;

        public const float Duracion = 1.25f;

        Vector3 escalaFinal, posicionFinal;
        Quaternion giroFinal;
        bool animando;

        /// <summary>¿Está a mitad del montaje? (La cámara espera a que termine para devolver el control.)</summary>
        public bool Montando => animando;
        public Transform[] Fijaciones => fijaciones;
        public GameObject Indicador => indicador;

        public void Fijar(Transform[] garras, GameObject luz, bool conAnimacion)
        {
            fijaciones = garras; indicador = luz; animar = conAnimacion;
        }

        void OnEnable()
        {
            if (!Application.isPlaying) { PoseFinal(); return; }
            StopAllCoroutines();
            // La pose final es la que dejó NaveSatelite (posición, giro y escala del satélite elegido).
            escalaFinal = transform.localScale;
            posicionFinal = transform.localPosition;
            giroFinal = transform.localRotation;
            // Si todavía se estaba retirando una copia de este mismo módulo, desaparece: no hay dos a la vez.
            var saliendo = transform.parent != null ? transform.parent.Find(Retirada.Prefijo + name) : null;
            if (saliendo != null) Destroy(saliendo.gameObject);
            if (!animar || Preferencias.ReducirMovimiento) { PoseFinal(); return; }
            StartCoroutine(Montar());
        }

        /// <summary>Montado del todo: fijaciones cerradas e indicador encendido.</summary>
        public void PoseFinal()
        {
            Abrir(0f);
            if (indicador != null) indicador.SetActive(true);
            if (destello != null) destello.gameObject.SetActive(false);
        }

        /// <summary>1 = fijaciones abiertas; 0 = cerradas sobre la base del instrumento.</summary>
        public void Abrir(float k)
        {
            for (int i = 0; i < fijaciones.Length; i++)
                if (fijaciones[i] != null)
                    fijaciones[i].localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * giroFijacionAbierta * k);
        }

        /// <summary>Hacia dónde queda «el lateral seguro»: perpendicular al casco y alejándose del centro de la nave.</summary>
        public static Vector3 LateralSeguro(Vector3 posicion, Vector3 fuera)
        {
            var lado = Vector3.ProjectOnPlane(posicion, fuera);
            if (lado.sqrMagnitude < 1e-4f) lado = Vector3.Cross(fuera, Vector3.forward);
            if (lado.sqrMagnitude < 1e-4f) lado = Vector3.Cross(fuera, Vector3.right);
            return lado.normalized;
        }

        IEnumerator Montar()
        {
            animando = true;
            var fuera = giroFinal * Vector3.up;   // la normal del casco, en el espacio de la nave
            var lado = LateralSeguro(posicionFinal, fuera);
            var sobre = posicionFinal + fuera * distanciaLlegada * 0.38f;        // encima del soporte, ya alineado
            var lejos = sobre + lado * distanciaLlegada + fuera * distanciaLlegada * 0.2f;
            var giroLlegada = Quaternion.AngleAxis(75f, fuera) * Quaternion.AngleAxis(24f, lado) * giroFinal;
            Abrir(1f);
            if (indicador != null) indicador.SetActive(false);
            if (destello != null) destello.gameObject.SetActive(false);
            transform.localScale = escalaFinal;

            // 1 y 2) Llega desde el lateral y se orienta al soporte.
            yield return Animar.Durante(0.46f, t =>
            {
                transform.localPosition = Vector3.Lerp(lejos, sobre, t);
                transform.localRotation = Quaternion.Slerp(giroLlegada, giroFinal, Mathf.SmoothStep(0f, 1f, t));
            }, Animar.SalidaCubica);
            // 3) Aproximación con frenado.
            yield return Animar.Durante(0.34f, t => transform.localPosition = Vector3.Lerp(sobre, posicionFinal, t), Animar.SalidaCubica);
            // 4) Asienta: baja un punto más y vuelve (peso, sin rebote de juguete).
            yield return Animar.Durante(0.12f, t => transform.localPosition = posicionFinal - fuera * 0.012f * Mathf.Sin(t * Mathf.PI), x => x);
            transform.localPosition = posicionFinal;
            transform.localRotation = giroFinal;
            // 5) Cierran las fijaciones.
            ReproductorUI.SonarEfecto(ArteDatos.Efecto.Servo, 0.35f);
            yield return Animar.Durante(0.2f, t => Abrir(1f - t), Animar.SalidaCubica);
            // 6) Conexión: indicador encendido y destello en el soporte.
            if (indicador != null) indicador.SetActive(true);
            animando = false;
            if (destello != null) yield return Destellar();
        }

        IEnumerator Destellar()
        {
            destello.gameObject.SetActive(true);
            var escala = destello.transform.localScale;
            yield return Animar.Durante(0.3f, t =>
            {
                destello.transform.localScale = escala * Mathf.Lerp(0.6f, 1.8f, t);
                var c = destello.color; c.a = 1f - t; destello.color = c;
            });
            destello.transform.localScale = escala;
            destello.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (!animando) return;
            // Si se quita a mitad del montaje, queda en su pose final para la próxima vez.
            transform.localPosition = posicionFinal;
            transform.localRotation = giroFinal;
            transform.localScale = escalaFinal;
            animando = false;
            PoseFinal();
        }
    }
}
