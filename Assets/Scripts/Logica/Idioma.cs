using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 5, bloque 5: idioma del juego (inglés o español). Se guarda en el navegador; por defecto, inglés.
    /// Los textos del código pasan por <see cref="T"/>; los de los datos, por las propiedades traducidas de cada
    /// clase (Nombre, Texto…); los fijos de las escenas, por <see cref="TraductorEscena"/>.
    /// </summary>
    public static class Idioma
    {
        public const string Clave = "idioma";
        static bool? espanol;

        public static event System.Action AlCambiar;

        public static bool Espanol
        {
            get
            {
                if (espanol == null)
                {
                    try { espanol = PlayerPrefs.GetString(Clave, "en") == "es"; } catch { espanol = false; }
                }
                return espanol.Value;
            }
            set
            {
                if (espanol == value) return;
                espanol = value;
                try { PlayerPrefs.SetString(Clave, value ? "es" : "en"); PlayerPrefs.Save(); } catch { }
                AlCambiar?.Invoke();
            }
        }

        /// <summary>El texto en el idioma actual. Si no hay traducción, el original (inglés).</summary>
        public static string T(string en)
        {
            if (!Espanol || string.IsNullOrEmpty(en)) return en;
            return Traducciones.Es.TryGetValue(en, out var es) ? es : en;
        }

        /// <summary>
        /// Como <see cref="T"/>, pero si el texto lleva un icono u otros signos delante (p. ej. «  LATEST SIGNAL»)
        /// o detrás («Continue  →»), traduce el resto. Lo usa el traductor de los textos fijos de las escenas.
        /// </summary>
        public static string TCompuesto(string en) => Espanol ? EspanolDe(en) ?? en : en;

        /// <summary>La traducción al español, o null si no hay (sin mirar el idioma actual).</summary>
        public static string EspanolDe(string en)
        {
            if (string.IsNullOrEmpty(en)) return null;
            if (Traducciones.Es.TryGetValue(en, out var es)) return es;
            // Varias líneas en un mismo texto («Cómo jugar»): se traduce cada una; si alguna no tiene par, nada.
            if (en.IndexOf('\n') >= 0)
            {
                var lineas = en.Split('\n');
                for (int n = 0; n < lineas.Length; n++)
                {
                    if (lineas[n].Length == 0) continue;
                    var linea = EspanolDe(lineas[n]);
                    if (linea == null) return null;
                    lineas[n] = linea;
                }
                return string.Join("\n", lineas);
            }
            // Icono con color delante («<color=#5BC0EB>●</color>  Choose an orbit…»).
            var icono = System.Text.RegularExpressions.Regex.Match(en, @"^<color=[^>]+>[^<]*</color>\s*");
            if (icono.Success && icono.Length < en.Length)
            {
                var tras = EspanolDe(en.Substring(icono.Length));
                return tras == null ? null : icono.Value + tras;
            }
            int i = 0;
            while (i < en.Length && !char.IsLetterOrDigit(en[i]) && en[i] != '<') i++;
            if (i > 0 && i < en.Length)
            {
                // El icono puede ir pegado a la clave o con sus espacios («  LATEST SIGNAL»).
                for (int k = 1; k < i; k++)
                    if (Traducciones.Es.TryGetValue(en.Substring(k), out es)) return en.Substring(0, k) + es;
                var resto = en.Substring(i);
                if (Traducciones.Es.TryGetValue(resto, out es)) return en.Substring(0, i) + es;
                var recortado = resto.TrimEnd();
                if (Traducciones.Es.TryGetValue(recortado, out es)) return en.Substring(0, i) + es + resto.Substring(recortado.Length);
            }
            int j = en.Length;
            while (j > 0 && !char.IsLetterOrDigit(en[j - 1])) j--;
            if (j > 0 && j < en.Length)
            {
                // La clave puede guardar sus espacios antes de la flecha («Review mission  »).
                for (int k = en.Length - 1; k > j; k--)
                    if (Traducciones.Es.TryGetValue(en.Substring(0, k), out es)) return es + en.Substring(k);
                var delante = en.Substring(0, j);
                if (Traducciones.Es.TryGetValue(delante, out es)) return es + en.Substring(j);
                if (Traducciones.Es.TryGetValue(delante.TrimEnd(), out es)) return es + en.Substring(delante.TrimEnd().Length);
            }
            // Icono delante y flecha detrás a la vez («🚀   Start mission  →»).
            if (i > 0 && j > i && Traducciones.Es.TryGetValue(en.Substring(i, j - i), out es))
                return en.Substring(0, i) + es + en.Substring(j);
            return null;
        }

        /// <summary>Para las pruebas: vuelve a leer la preferencia.</summary>
        public static void Recargar() => espanol = null;
    }
}
