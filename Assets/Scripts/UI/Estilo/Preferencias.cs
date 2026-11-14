using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Opciones del jugador (volúmenes y «Reducir movimiento»), guardadas en PlayerPrefs.
    /// Si PlayerPrefs falla (navegador en modo privado, por ejemplo) se usan los valores por defecto.
    /// </summary>
    public static class Preferencias
    {
        const string ClaveMusica = "vol_musica";
        const string ClaveEfectos = "vol_efectos";
        const string ClaveVoz = "vol_voz";
        const string ClaveMovimiento = "reducir_movimiento";

        public static float VolumenMusica { get => Leer(ClaveMusica, 0.6f); set => Escribir(ClaveMusica, value); }
        public static float VolumenEfectos { get => Leer(ClaveEfectos, 0.8f); set => Escribir(ClaveEfectos, value); }
        public static float VolumenVoz { get => Leer(ClaveVoz, 1f); set => Escribir(ClaveVoz, value); }

        public static bool ReducirMovimiento
        {
            get => Leer(ClaveMovimiento, 0f) > 0.5f;
            set => Escribir(ClaveMovimiento, value ? 1f : 0f);
        }

        /// <summary>Se avisa a quien suene o se anime para que aplique el cambio al momento.</summary>
        public static event System.Action Cambiaron;

        static readonly System.Collections.Generic.Dictionary<string, float> cache =
            new System.Collections.Generic.Dictionary<string, float>();

        static float Leer(string clave, float porDefecto)
        {
            if (cache.TryGetValue(clave, out var valor)) return valor;
            try { valor = PlayerPrefs.GetFloat(clave, porDefecto); }
            catch { valor = porDefecto; }
            cache[clave] = valor;
            return valor;
        }

        static void Escribir(string clave, float valor)
        {
            cache[clave] = Mathf.Clamp01(valor);
            try
            {
                PlayerPrefs.SetFloat(clave, Mathf.Clamp01(valor));
                PlayerPrefs.Save();
            }
            catch { /* sin almacenamiento: el cambio dura solo esta sesión de juego */ }
            Cambiaron?.Invoke();
        }
    }
}
