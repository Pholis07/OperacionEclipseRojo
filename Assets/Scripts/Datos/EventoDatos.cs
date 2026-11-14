using UnityEngine;

namespace SpaceMission
{
    /// <summary>Qué hace un evento de la nave durante la Operación.</summary>
    public enum EfectoEvento
    {
        ReducirLimiteTemporal,   // polvo sobre los paneles: baja el límite unas órbitas
        ReducirLimite,           // panel dañado: baja el límite hasta repararlo
        BloquearInstrumento,     // sobrecalentamiento: apaga y bloquea el instrumento encendido de mayor consumo
        AnularLectura,           // pérdida de orientación: el próximo escaneo no da lectura
        GanarOrbitas             // ventana despejada
    }

    /// <summary>Evento aleatorio de la nave y la herramienta que lo resuelve.</summary>
    [CreateAssetMenu(fileName = "Evento", menuName = "SpaceMission/Evento")]
    public class EventoDatos : ScriptableObject
    {
        public string id;
        public string nombre;
        [TextArea] public string texto;
        public EfectoEvento efecto;
        public int vatios;      // cuánto baja el límite (ReducirLimite y ReducirLimiteTemporal)
        public int orbitas;     // duración (temporal o bloqueo) u órbitas ganadas
        public string herramientaId;   // vacío si no necesita herramienta
    }
}
