using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Versión 5, bloque 5: traduce los textos fijos de cada escena (los que puso el constructor). Al cargar una
    /// escena guarda el texto en inglés de cada TMP_Text; al cambiar de idioma solo toca los que siguen mostrando
    /// ese texto (o su traducción): los que un script ya reescribió se dejan, porque ese script escribe en el
    /// idioma actual. No necesita cambios en las escenas.
    /// </summary>
    public static class TraductorEscena
    {
        static readonly Dictionary<TMP_Text, string> originales = new Dictionary<TMP_Text, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            SceneManager.sceneLoaded += (escena, modo) => Traducir();
            Idioma.AlCambiar += Traducir;
            Traducir();
        }

        /// <summary>Traduce todos los textos fijos de las escenas abiertas al idioma actual.</summary>
        public static void Traducir()
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!originales.TryGetValue(t, out var en))
                {
                    en = t.text;
                    originales[t] = en;
                }
                var es = Idioma.EspanolDe(en);
                if (es != null && (t.text == en || t.text == es))
                    t.text = Idioma.Espanol ? es : en;
            }
            // Las entradas de objetos destruidos (escenas anteriores) se limpian.
            var muertos = new List<TMP_Text>();
            foreach (var k in originales.Keys) if (k == null) muertos.Add(k);
            foreach (var k in muertos) originales.Remove(k);
        }
    }
}
