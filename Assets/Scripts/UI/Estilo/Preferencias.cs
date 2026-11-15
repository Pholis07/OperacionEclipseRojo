using UnityEngine;

namespace SpaceMission
{
    /// <summary>Idioma de los subtítulos de las voces (el juego está en inglés).</summary>
    public enum IdiomaSubtitulos { English, Espanol, Off }

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
        const string ClaveAmbiente = "vol_ambiente";
        const string ClaveSilencio = "silencio";
        const string ClaveSubtitulos = "subtitulos";

        /// <summary>Subtítulos de las voces: English (por defecto), Español u Off. Se guarda como 0, 0.5 o 1.</summary>
        public static IdiomaSubtitulos Subtitulos
        {
            get
            {
                float v = Leer(ClaveSubtitulos, 0f);
                return v < 0.25f ? IdiomaSubtitulos.English : v < 0.75f ? IdiomaSubtitulos.Espanol : IdiomaSubtitulos.Off;
            }
            set => Escribir(ClaveSubtitulos, value == IdiomaSubtitulos.English ? 0f : value == IdiomaSubtitulos.Espanol ? 0.5f : 1f);
        }

        public static float VolumenMusica { get => Leer(ClaveMusica, 0.6f); set => Escribir(ClaveMusica, value); }
        public static float VolumenEfectos { get => Leer(ClaveEfectos, 0.8f); set => Escribir(ClaveEfectos, value); }
        public static float VolumenVoz { get => Leer(ClaveVoz, 1f); set => Escribir(ClaveVoz, value); }
        public static float VolumenAmbiente { get => Leer(ClaveAmbiente, 0.7f); set => Escribir(ClaveAmbiente, value); }

        /// <summary>Silencio total (AudioListener): funciona igual en el editor y en el navegador.</summary>
        public static bool Silencio
        {
            get => Leer(ClaveSilencio, 0f) > 0.5f;
            set { Escribir(ClaveSilencio, value ? 1f : 0f); AplicarSilencio(); }
        }

        public static void AplicarSilencio() => AudioListener.volume = Silencio ? 0f : 1f;

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
