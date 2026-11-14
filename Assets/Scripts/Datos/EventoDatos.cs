using System.Collections.Generic;
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

    /// <summary>Qué pasa al elegir una respuesta a un evento (versión 3).</summary>
    public enum EfectoOpcion
    {
        UsarHerramienta,   // A: la herramienta del evento lo resuelve (solo si se cargó)
        Resolver,          // B: se resuelve pagando combustible u órbitas
        Acortar,           // B: el evento dura solo «duracion» órbitas
        Aguantar,          // C: se acepta el efecto tal cual (o se rechaza la petición)
        GanarOrbitas,      // ventana despejada: + «duracion» órbitas
        AbrirContacto,     // ventana despejada: abre ahora una ventana de contacto con la Tierra
        AceptarObjetivo,   // petición de la NASA: objetivo extra
        BajarAltitud       // tormenta: baja de altitud sin gastar combustible
    }

    /// <summary>Una respuesta posible a un evento, con su costo.</summary>
    [System.Serializable]
    public class OpcionEvento
    {
        public string texto;
        public EfectoOpcion efecto;
        public int combustible;   // costo en combustible
        public int orbitas;       // costo en órbitas
        public int duracion;      // Acortar: nueva duración; GanarOrbitas: órbitas ganadas
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

        [Header("Decisión (versión 3)")]
        public List<OpcionEvento> opciones = new List<OpcionEvento>();
    }
}
