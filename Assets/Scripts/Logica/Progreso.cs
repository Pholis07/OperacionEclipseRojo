using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 9 (Adrian, 2026-11-15): lo que se desbloquea al ganar. Se guarda en el navegador cuántos niveles se han
    /// ganado seguidos desde el primero:
    /// - Al empezar: nivel 1, Explorador, MRO, cámara térmica, radar y órbitas media y polar (bastan para ganar).
    /// - Al ganar el nivel 1: nivel 2, Titán, Magellan, Chandra, Aquarius, alta resolución, sensor atmosférico y elíptica.
    /// - Al ganar el nivel 2: nivel 3, Coloso, LLCD, AztechSat-1, espectrómetro, ecuatorial y areoestacionaria.
    /// Solo cambia lo que se puede elegir en la interfaz: el cálculo, la evaluación y la Operación no lo miran.
    /// </summary>
    public static class Progreso
    {
        const string Clave = "niveles_ganados";

        /// <summary>Niveles que hay que haber ganado para usar cada cosa (lo que no está, desde el principio).</summary>
        static readonly Dictionary<string, int> Requisito = new Dictionary<string, int>
        {
            { "titan", 1 }, { "magellan", 1 }, { "chandra", 1 }, { "aquarius", 1 },
            { "alta_res", 1 }, { "atmosferico", 1 }, { "eliptica", 1 },
            { "coloso", 2 }, { "llcd", 2 }, { "aztechsat", 2 },
            { "espectrometro", 2 }, { "ecuatorial", 2 }, { "areoestacionaria", 2 },
        };

        static int? ganados;

        /// <summary>Cuántos niveles se han ganado (0 a 3). Sin acceso al almacenamiento, 0.</summary>
        public static int NivelesGanados
        {
            get
            {
                if (!ganados.HasValue)
                    try { ganados = PlayerPrefs.GetInt(Clave, 0); } catch { ganados = 0; }
                return ganados.Value;
            }
            private set
            {
                ganados = value;
                try { PlayerPrefs.SetInt(Clave, value); PlayerPrefs.Save(); } catch { }
            }
        }

        /// <summary>Nivel que hay que ganar para usar «id» (1 o 2); 0 si está desde el principio.</summary>
        public static int NivelParaDesbloquear(string id) => id != null && Requisito.TryGetValue(id, out var n) ? n : 0;

        public static bool Desbloqueado(string id) => NivelParaDesbloquear(id) <= NivelesGanados;

        /// <summary>Los niveles se abren en orden: el 2 al ganar el 1 y el 3 al ganar el 2.</summary>
        public static bool NivelDisponible(int indice) => indice <= NivelesGanados;

        /// <summary>
        /// Anota una victoria en el nivel «indice» (0, 1 o 2). Devuelve los ids que se acaban de desbloquear
        /// (vacío si ese nivel ya se había ganado o si se ganó uno que aún no tocaba).
        /// </summary>
        public static List<string> RegistrarVictoria(int indice)
        {
            int antes = NivelesGanados;
            if (indice != antes) return new List<string>();
            NivelesGanados = antes + 1;
            return Requisito.Where(p => p.Value == NivelesGanados).Select(p => p.Key).ToList();
        }

        /// <summary>Para Opciones: todo abierto (jurado, demostraciones) o empezar de cero.</summary>
        public static void DesbloquearTodo() => NivelesGanados = 3;
        public static void Reiniciar() => NivelesGanados = 0;

        /// <summary>Para las pruebas: deja el valor que había.</summary>
        public static void Poner(int niveles) => NivelesGanados = niveles;
    }
}
