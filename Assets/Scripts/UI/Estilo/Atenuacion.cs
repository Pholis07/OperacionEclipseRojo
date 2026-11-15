using System.Collections.Generic;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Mientras habla una voz (control de misión o una transmisión), la música, el ambiente y el motor bajan
    /// para que se entienda, y vuelven con un fundido al terminar. Las fuentes de voz se registran aquí y cada
    /// fuente de fondo sigue el factor con <see cref="Seguir"/>.
    /// </summary>
    public static class Atenuacion
    {
        /// <summary>Volumen del fondo mientras suena una voz (1 = sin cambio).</summary>
        public const float Fondo = 0.35f;

        /// <summary>Velocidad del fundido (unidades de volumen por segundo).</summary>
        const float Velocidad = 2.5f;

        static readonly List<AudioSource> voces = new List<AudioSource>();

        public static void Registrar(AudioSource voz)
        {
            if (voz != null && !voces.Contains(voz)) voces.Add(voz);
        }

        public static bool HablaAlguien
        {
            get
            {
                voces.RemoveAll(v => v == null);
                foreach (var v in voces) if (v.isActiveAndEnabled && v.isPlaying) return true;
                return false;
            }
        }

        /// <summary>Cuántas voces suenan ahora mismo (las pruebas comprueban que nunca son dos a la vez).</summary>
        public static int VocesSonando
        {
            get
            {
                voces.RemoveAll(v => v == null);
                int n = 0;
                foreach (var v in voces) if (v.isActiveAndEnabled && v.isPlaying) n++;
                return n;
            }
        }

        /// <summary>El factor de volumen del fondo, un paso más cerca de su objetivo.</summary>
        public static float Seguir(float actual) =>
            Mathf.MoveTowards(actual, HablaAlguien ? Fondo : 1f, Time.unscaledDeltaTime * Velocidad);
    }
}
