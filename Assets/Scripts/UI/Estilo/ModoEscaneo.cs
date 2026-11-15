using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Versión 7 (paquete de diseño): modos de análisis que se ven durante el escaneo, encima de la vista orbital.
    /// Son vistas de los instrumentos que ya existen; no añaden instrumentos, costos ni evidencias.
    /// - Espectrómetro: curva de reflectancia por bandas que se va trazando (modo hiperespectral) y después tres capas
    ///   que se alternan, Visible / Infrarrojo / Composición (modo multiespectral).
    /// - Cámara de alta resolución: destello de captura y una nota prudente.
    /// Los textos son prudentes: «firma compatible con…», sin afirmar agua actual ni vida, y una imagen no muestra
    /// lo que hay bajo la superficie.
    /// El panel se crea por código la primera vez (hereda la fuente de un texto de la vista).
    /// </summary>
    public class ModoEscaneo
    {
        const int Bandas = 28;
        static readonly string[] Capas = { "VISIBLE", "INFRARED", "COMPOSITE" };

        readonly RectTransform panel;
        readonly TMP_Text titulo, nota;
        readonly RectTransform[] barras = new RectTransform[Bandas];
        readonly UnityEngine.UI.Image[] imagenes = new UnityEngine.UI.Image[Bandas];
        readonly TMP_Text[] chips = new TMP_Text[Capas.Length];
        readonly UnityEngine.UI.Image destello;
        readonly float[] curva = new float[Bandas];

        public bool Visible => panel != null && panel.gameObject.activeSelf;
        public string Titulo => titulo.text;
        public string Nota => nota.text;

        /// <summary>Instrumentos que tienen un modo propio en pantalla.</summary>
        public static bool Tiene(string instrumento) => instrumento == "espectrometro" || instrumento == "alta_res";

        public ModoEscaneo(RectTransform vista, TMP_Text plantilla)
        {
            destello = Imagen(vista, "DestelloCaptura", new Color(1f, 1f, 1f, 0f));
            Estirar(destello.rectTransform);

            panel = Imagen(vista, "PanelModo", new Color(0.03f, 0.04f, 0.07f, 0.82f)).rectTransform;
            panel.anchorMin = new Vector2(0.04f, 0.14f);
            panel.anchorMax = new Vector2(0.96f, 0.42f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            titulo = Texto(plantilla, panel, "TituloModo", 0.03f, 0.8f, 0.97f, 0.97f, TextAlignmentOptions.Left);
            nota = Texto(plantilla, panel, "NotaModo", 0.03f, 0.03f, 0.97f, 0.3f, TextAlignmentOptions.TopLeft);
            nota.fontStyle = FontStyles.Normal;
            nota.textWrappingMode = TextWrappingModes.Normal;
            for (int i = 0; i < Capas.Length; i++)
                chips[i] = Texto(plantilla, panel, "Capa" + i, 0.5f + i * 0.165f, 0.8f, 0.5f + (i + 1) * 0.165f - 0.01f, 0.97f, TextAlignmentOptions.Center);

            // Zona de la curva: barras finas, una por banda.
            for (int i = 0; i < Bandas; i++)
            {
                imagenes[i] = Imagen(panel, "Banda" + i, Color.white);
                barras[i] = imagenes[i].rectTransform;
                float x0 = 0.03f + i * (0.94f / Bandas);
                barras[i].anchorMin = new Vector2(x0, 0.34f);
                barras[i].anchorMax = new Vector2(x0 + 0.94f / Bandas * 0.7f, 0.34f);
                barras[i].pivot = new Vector2(0.5f, 0f);
                barras[i].offsetMin = barras[i].offsetMax = Vector2.zero;
            }
            Ocultar();
        }

        /// <summary>Prepara el modo del instrumento. «encontrada»: la lectura dio con la anomalía.</summary>
        public void Empezar(string instrumento, bool encontrada)
        {
            if (!Tiene(instrumento)) { Ocultar(); return; }
            panel.gameObject.SetActive(true);
            bool espectro = instrumento == "espectrometro";
            titulo.text = espectro ? Idioma.T("HYPERSPECTRAL · REFLECTANCE BY BAND") : Idioma.T("HIGH RESOLUTION · OPTICAL CAPTURE");
            nota.text = "";
            foreach (var b in barras) b.gameObject.SetActive(espectro);
            foreach (var c in chips) c.gameObject.SetActive(false);
            if (!espectro) return;
            // Curva de reflectancia: sube hacia el infrarrojo, con un valle de absorción si hay firma.
            for (int i = 0; i < Bandas; i++)
            {
                float x = i / (Bandas - 1f);
                float v = 0.25f + 0.55f * Mathf.SmoothStep(0f, 1f, x) + 0.05f * Mathf.Sin(x * 23f);
                if (encontrada) v -= 0.32f * Mathf.Exp(-Mathf.Pow((x - 0.68f) / 0.07f, 2f));   // absorción cerca de 1.9 µm
                curva[i] = Mathf.Clamp01(v);
                barras[i].sizeDelta = new Vector2(barras[i].sizeDelta.x, 0f);
            }
        }

        /// <summary>Avance del escaneo, de 0 a 1.</summary>
        public void Animar(string instrumento, float t)
        {
            if (!Visible) return;
            if (instrumento == "espectrometro")
            {
                float altoZona = panel.rect.height * 0.42f;
                // Primera mitad: la curva se traza de izquierda a derecha (hiperespectral: muchas bandas).
                float trazado = Mathf.Clamp01(t / 0.55f) * Bandas;
                for (int i = 0; i < Bandas; i++)
                {
                    float k = Mathf.Clamp01(trazado - i);
                    barras[i].sizeDelta = new Vector2(0f, altoZona * curva[i] * k);
                    imagenes[i].color = Color.HSVToRGB(Mathf.Lerp(0.72f, 0.02f, i / (Bandas - 1f)), 0.6f, 1f);
                }
                // Segunda mitad: tres capas que se alternan (multiespectral: pocas bandas elegidas).
                bool multi = t >= 0.55f;
                if (multi) titulo.text = Idioma.T("MULTISPECTRAL · LAYERS");
                int activa = Mathf.Min(Capas.Length - 1, (int)((t - 0.55f) / 0.15f));
                for (int i = 0; i < chips.Length; i++)
                {
                    chips[i].gameObject.SetActive(multi);
                    chips[i].text = Idioma.T(Capas[i]);
                    chips[i].color = i == activa ? new Color(1f, 1f, 1f, 1f) : new Color(0.58f, 0.64f, 0.72f, 0.7f);
                }
            }
            else
            {
                // Cámara: destello blanco corto al capturar (cerca del final del enfoque).
                destello.color = new Color(1f, 1f, 1f, t > 0.85f && t < 0.93f ? 0.55f : 0f);
            }
        }

        /// <summary>Al terminar el escaneo: la nota prudente con lo que se puede decir.</summary>
        public void Concluir(string instrumento, bool encontrada)
        {
            if (!Visible) return;
            destello.color = new Color(1f, 1f, 1f, 0f);
            nota.text = TextoNota(instrumento, encontrada);
        }

        public static string TextoNota(string instrumento, bool encontrada) =>
            instrumento == "espectrometro"
                ? (encontrada ? Idioma.T("Signature consistent with minerals formed in water. It does not prove there is water today.")
                              : Idioma.T("No clear mineral signature at this point."))
                : (encontrada ? Idioma.T("Image captured: textures and fractures on the surface. An image alone does not show what lies below.")
                              : Idioma.T("Image captured: nothing unusual at this point."));

        public void Ocultar()
        {
            if (panel != null) panel.gameObject.SetActive(false);
            if (destello != null) destello.color = new Color(1f, 1f, 1f, 0f);
        }

        // ---------- Piezas ----------

        static UnityEngine.UI.Image Imagen(Transform padre, string nombre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(padre, false);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TMP_Text Texto(TMP_Text plantilla, Transform padre, string nombre, float x0, float y0, float x1, float y1, TextAlignmentOptions alineacion)
        {
            var t = Object.Instantiate(plantilla, padre);
            t.name = nombre;
            t.text = "";
            t.alignment = alineacion;
            t.raycastTarget = false;
            t.enableAutoSizing = true;
            t.fontSizeMin = 10; t.fontSizeMax = 22;
            t.color = new Color(0.89f, 0.92f, 0.96f);
            t.gameObject.SetActive(true);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            return t;
        }
    }
}
