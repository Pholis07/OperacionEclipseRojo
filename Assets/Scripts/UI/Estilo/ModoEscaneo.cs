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
    /// Renovación visual: los cinco instrumentos tienen su lectura, distinta de las demás y siempre con el resultado
    /// real del juego (si la lectura no dio con la anomalía, el gráfico sale plano y la nota lo dice):
    /// - Cámara térmica: la misma franja en gris (imagen base) que un barrido convierte a falso color, con leyenda
    ///   de escala relativa (más frío / más cálido). No da temperaturas.
    /// - Radar de subsuelo: trazas que llegan una a una (perfil de ecos, esquemático); con hallazgo, un eco claro
    ///   bajo la superficie en el centro; sin él, solo ruido.
    /// - Sensor atmosférico: opacidad relativa del polvo a lo largo de la pasada.
    /// Son ilustraciones de los datos, no fenómenos que se verían desde fuera: cada título lo dice.
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

        /// <summary>Capa multiespectral resaltada (−1 si todavía no hay). SecuenciaEscaneo suena la conmutación al cambiar.</summary>
        public int CapaActiva { get; private set; } = -1;
        public string Titulo => titulo.text;
        public string Nota => nota.text;

        /// <summary>Instrumentos que tienen un modo propio en pantalla.</summary>
        public static bool Tiene(string instrumento) =>
            instrumento == "espectrometro" || instrumento == "alta_res" || instrumento == "termica" || instrumento == "radar" || instrumento == "atmosferico";

        static readonly string[] LeyendaTermica = { "COOLER", "RELATIVE SCALE", "WARMER" };
        static readonly string[] LeyendaRadar = { "SURFACE", "SCHEMATIC", "ECHO BELOW" };
        static readonly string[] LeyendaAtmosfera = { "CLEAR", "RELATIVE SCALE", "DUSTY" };

        static string[] Leyenda(string instrumento) =>
            instrumento == "termica" ? LeyendaTermica : instrumento == "radar" ? LeyendaRadar : instrumento == "atmosferico" ? LeyendaAtmosfera : null;

        public static string TituloDe(string instrumento)
        {
            switch (instrumento)
            {
                case "espectrometro": return Idioma.T("HYPERSPECTRAL · REFLECTANCE BY BAND");
                case "termica": return Idioma.T("THERMAL INFRARED · FALSE COLOR");
                case "radar": return Idioma.T("RADAR SOUNDER · ECHO PROFILE");
                case "atmosferico": return Idioma.T("ATMOSPHERIC SOUNDER · DUST OPACITY");
                default: return Idioma.T("HIGH RESOLUTION · OPTICAL CAPTURE");
            }
        }

        /// <summary>Altura (0 a 1) de la columna «i» en la lectura de este instrumento. Con «encontrada» aparece el rasgo.</summary>
        public static float Valor(string instrumento, int i, bool encontrada)
        {
            float x = i / (Bandas - 1f);
            float centro = Mathf.Exp(-Mathf.Pow((x - 0.5f) / 0.13f, 2f));
            switch (instrumento)
            {
                case "termica":      // contraste térmico: una mancha más cálida en el centro
                    return Mathf.Clamp01(0.3f + 0.06f * Mathf.Sin(x * 17f) + (encontrada ? 0.6f * centro : 0f));
                case "radar":        // eco bajo la superficie: ruido bajo y, con hallazgo, un reflector claro
                    return Mathf.Clamp01(0.12f + 0.07f * Mathf.Abs(Mathf.Sin(x * 31f)) + (encontrada ? 0.62f * Mathf.SmoothStep(0f, 1f, centro * 1.6f) : 0f));
                case "atmosferico":  // opacidad del polvo a lo largo de la pasada
                    return Mathf.Clamp01(0.35f + 0.18f * Mathf.Sin(x * 5.2f + 0.6f) + 0.05f * Mathf.Sin(x * 29f) + (encontrada ? 0.15f * centro : 0f));
                default: return 0f;
            }
        }

        static Color ColorDe(string instrumento, float v, float x)
        {
            switch (instrumento)
            {
                case "termica": return Color.HSVToRGB(Mathf.Lerp(0.62f, 0.04f, Mathf.InverseLerp(0.25f, 0.9f, v)), 0.85f, 1f);   // azul → naranja
                case "radar": return Color.Lerp(new Color(0.2f, 0.42f, 0.5f), new Color(0.55f, 0.95f, 1f), Mathf.InverseLerp(0.15f, 0.7f, v));
                default: return Color.Lerp(new Color(0.55f, 0.75f, 0.95f), new Color(0.9f, 0.68f, 0.4f), Mathf.InverseLerp(0.2f, 0.7f, v));
            }
        }

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
            CapaActiva = -1;
            bool espectro = instrumento == "espectrometro";
            titulo.text = TituloDe(instrumento);
            nota.text = "";
            var leyenda = Leyenda(instrumento);
            // Con leyenda a la derecha, el título se queda en la mitad izquierda (si no, se enciman).
            titulo.rectTransform.anchorMax = new Vector2(leyenda != null ? 0.49f : 0.97f, 0.97f);
            foreach (var b in barras) b.gameObject.SetActive(espectro || leyenda != null);
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i].gameObject.SetActive(leyenda != null);
                if (leyenda == null) continue;
                chips[i].text = Idioma.T(leyenda[i]);
                chips[i].color = i == 1 ? new Color(0.58f, 0.64f, 0.72f, 0.8f) : Color.white;
            }
            if (leyenda != null)
            {
                for (int i = 0; i < Bandas; i++)
                {
                    curva[i] = Valor(instrumento, i, encontrada);
                    barras[i].sizeDelta = new Vector2(barras[i].sizeDelta.x, 0f);
                }
                return;
            }
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
                CapaActiva = multi ? activa : -1;
                for (int i = 0; i < chips.Length; i++)
                {
                    chips[i].gameObject.SetActive(multi);
                    chips[i].text = Idioma.T(Capas[i]);
                    chips[i].color = i == activa ? new Color(1f, 1f, 1f, 1f) : new Color(0.58f, 0.64f, 0.72f, 0.7f);
                }
            }
            else if (Leyenda(instrumento) != null)
            {
                float altoZona = panel.rect.height * 0.42f;
                float avance = Mathf.Clamp01(t / 0.8f) * Bandas;
                for (int i = 0; i < Bandas; i++)
                {
                    float k = Mathf.Clamp01(avance - i);
                    if (instrumento == "termica")
                    {
                        // La franja entera está desde el principio en gris (imagen base); el barrido la pasa a falso color.
                        barras[i].sizeDelta = new Vector2(0f, altoZona * 0.8f);
                        float gris = 0.3f + 0.4f * curva[i];
                        imagenes[i].color = Color.Lerp(new Color(gris, gris, gris), ColorDe(instrumento, curva[i], i / (Bandas - 1f)), k);
                    }
                    else
                    {
                        // Radar: cada traza llega con su pulso. Atmósfera: el perfil se va trazando con la pasada.
                        barras[i].sizeDelta = new Vector2(0f, altoZona * curva[i] * k);
                        imagenes[i].color = ColorDe(instrumento, curva[i], i / (Bandas - 1f));
                    }
                }
            }
            else
            {
                // Cámara: destello blanco corto al capturar (cerca del final del enfoque).
                destello.color = new Color(1f, 1f, 1f, t > 0.85f && t < 0.93f ? 0.55f : 0f);
            }
        }

        /// <summary>
        /// Al terminar el escaneo: la nota prudente con lo que se puede decir. «aporta» es si esta lectura dio
        /// evidencia nueva; si vio la anomalía pero no aporta (repetida o con poca calidad), la nota lo dice.
        /// </summary>
        public void Concluir(string instrumento, bool encontrada, bool aporta = true)
        {
            if (!Visible) return;
            destello.color = new Color(1f, 1f, 1f, 0f);
            nota.text = encontrada && !aporta ? Idioma.T("The anomaly shows again, but this reading adds no new evidence.") : TextoNota(instrumento, encontrada);
        }

        public static string TextoNota(string instrumento, bool encontrada)
        {
            switch (instrumento)
            {
                case "espectrometro":
                    return encontrada ? Idioma.T("Signature consistent with minerals formed in water. It does not prove there is water today.")
                                      : Idioma.T("No clear mineral signature at this point.");
                case "termica":
                    return encontrada ? Idioma.T("Thermal contrast detected in the observed region. Illustrative view: relative scale, not temperatures.")
                                      : Idioma.T("No thermal contrast at this point.");
                case "radar":
                    return encontrada ? Idioma.T("Echo from below the surface, consistent with a buried layer. It does not identify what the layer is made of.")
                                      : Idioma.T("No clear echo from below the surface at this point.");
                case "atmosferico":
                    return encontrada ? Idioma.T("Dust profile recorded over the region: context for the other readings, not a finding by itself.")
                                      : Idioma.T("Dust profile recorded: nothing unusual at this point.");
                default:
                    return encontrada ? Idioma.T("Image captured: textures and fractures on the surface. An image alone does not show what lies below.")
                                      : Idioma.T("Image captured: nothing unusual at this point.");
            }
        }

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
