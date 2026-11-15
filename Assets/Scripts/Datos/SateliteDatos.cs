using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Tipo de satélite (aspecto de la nave), con un modelo 3D de NASA 3D Resources.
    /// Mecánicas (PROPUESTA-GAMEPLAY.md, satélites): no entra en las reglas ni en el semáforo. Se paga en la
    /// Carga con lo que sobra y sus efectos solo existen en la Operación. El MRO es el neutro y el de por defecto.
    /// </summary>
    [CreateAssetMenu(fileName = "Satelite", menuName = "SpaceMission/Satélite")]
    public class SateliteDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        [Tooltip("Misión real de NASA en la que se basa el modelo 3D.")]
        public string inspiradoEn;
        [TextArea] public string descripcion;
        [Tooltip("Crédito del modelo 3D.")]
        public string credito = "Modelo 3D: NASA 3D Resources";

        [Header("Mecánicas: se pagan en la Carga")]
        public int costoCr;
        public int masaKg;
        [TextArea] public string ventaja;
        [TextArea] public string desventaja;

        [Header("Efectos en la Operación (los valores neutros no cambian nada)")]
        [Tooltip("Instrumentos que leen más lejos con este satélite.")]
        public string[] instrumentosConAlcance = new string[0];
        public float factorAlcance = 1f;
        [Tooltip("-1 = el de las reglas.")]
        public int combustiblePorCambioAltitud = -1;
        public int memoriaExtra;                 // bloques (puede ser negativo)
        public bool inmuneOrientacion;           // la pérdida de orientación no le afecta
        public bool inmuneTormenta;              // la tormenta creciendo no le da lecturas falsas
        public int consumoExtraW;                // consumo propio durante la Operación
        [Tooltip("-1 = el de las reglas.")]
        public int vatiosTransmisor = -1;
        [Tooltip("-1 = el de las reglas.")]
        public int cicloContacto = -1;
        public bool tormentaBloqueaEnvio;        // si sigue en la tormenta, no puede enviar esa órbita
        public int limiteExtraW;                 // cambia el límite de energía (negativo: paneles pequeños)
        public bool envioSinVentana;             // puede enviar con la ventana cerrada (relé por constelación)
        public int bloquesEnvioSinVentana;       // cuántos bloques caben por mes fuera de la ventana
    }
}
