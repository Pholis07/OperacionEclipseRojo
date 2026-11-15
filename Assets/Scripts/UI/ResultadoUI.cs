using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Escena Resultado, en dos pantallas para no saturar:
    /// 1) Veredicto (póster): título, conclusión, las evidencias de un vistazo, los puntos y el sello, con
    ///    «Ver detalles». Marte y la nave se ven a la derecha.
    /// 2) Detalle (tablero, versión 8): barras de recursos, fichas de la operación, objetivos extra, una fila por
    ///    evidencia (llegó o faltó, y por qué) y la tabla de puntos. Con «Modificar mi misión» (vuelve a
    ///    Configuración conservando las elecciones), «Volver al veredicto» y «← Menú».
    /// </summary>
    public class ResultadoUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] TMP_Text textoTitulo;
        [SerializeField] TMP_Text textoConclusion;
        [SerializeField] TMP_Text textoEvidencias;
        [SerializeField] TMP_Text textoPuntos;
        [SerializeField] TMP_Text textoResumen;       // veredicto: evidencias de un vistazo
        [SerializeField] TMP_Text textoNivel;         // veredicto: nivel jugado, junto a los puntos
        [SerializeField] TMP_Text[] textosBarra;      // detalle: créditos, masa, potencia y espacios
        [SerializeField] RectTransform[] barras;       // el relleno de cada barra: su ancla derecha es la fracción usada
        [SerializeField] TMP_Text[] fichas;           // detalle: meses, combustible, memoria y apagones
        [SerializeField] TMP_Text textoObjetivos;
        [SerializeField] UnityEngine.UI.Button botonModificar;
        [SerializeField] UnityEngine.UI.Button botonDetalle;
        [SerializeField] UnityEngine.UI.Button botonVeredicto;
        [SerializeField] UnityEngine.UI.Button botonMenu;
        [SerializeField] GameObject vistaVeredicto;
        [SerializeField] GameObject vistaDetalle;
        [SerializeField] UnityEngine.UI.Button botonDesglose;   // abre o cierra el desglose (recursos, fichas, objetivos, puntos)
        [SerializeField] GameObject desglose;

        public const string ColorBien = "#22C55E";
        public const string ColorMal = "#EF4444";
        const string ColorTenue = "#94A3B8";

        public bool DetalleVisible => vistaDetalle != null && vistaDetalle.activeSelf;
        public InformeMision.Informe Informe { get; private set; }
        public System.Collections.Generic.IReadOnlyList<RectTransform> Barras => barras;
        public System.Collections.Generic.IReadOnlyList<TMP_Text> TextosBarra => textosBarra;
        public string TextoEvidencias => textoEvidencias.text;
        public string TextoPuntosActual => textoPuntos.text;

        void Awake()
        {
            botonModificar.onClick.AddListener(ModificarMision);
            if (botonDetalle != null) botonDetalle.onClick.AddListener(VerDetalle);
            if (botonDesglose != null) botonDesglose.onClick.AddListener(AlternarDesglose);
            if (botonVeredicto != null) botonVeredicto.onClick.AddListener(VerVeredicto);
            if (botonMenu != null) botonMenu.onClick.AddListener(VolverAlMenu);
            Mostrar(EstadoJuego.Resultado, EstadoJuego.Configuracion);
        }

        public void Mostrar(Evaluacion e, Configuracion c)
        {
            if (e == null || !e.SePuedeLanzar)
            {
                Informe = null;
                textoTitulo.text = Idioma.T("No mission launched");
                textoConclusion.text = Idioma.T("Go back to the builder to prepare and launch a mission.");
                textoEvidencias.text = textoPuntos.text = "";
                Poner(textoResumen, ""); Poner(textoNivel, ""); Poner(textoObjetivos, "");
                LlenarBarras(null);
                LlenarFichas(null);
                return;
            }

            var op = EstadoJuego.Operacion;
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            var informe = Informe = InformeMision.Construir(e, c, catalogo, op, nivel);
            textoTitulo.text = informe.Titulo;
            textoConclusion.text = informe.Texto;
            Poner(textoResumen, string.Join("\n", informe.Filas.Select(f => Marca(f.Llego) + "  " + Iconos.DeInstrumento(f.InstrumentoId) + " " + f.Nombre)));
            Poner(textoNivel, nivel != null ? nivel.NombreNivel : "");

            textoEvidencias.text = informe.Filas.Count > 0
                ? string.Join("\n", informe.Filas.Select(FilaEvidencia))
                : Idioma.T("Nothing: the mission covered everything its orbit required.");
            LlenarBarras(informe);
            LlenarFichas(informe);
            Poner(textoObjetivos, informe.ObjetivosExtra.Count > 0
                ? string.Join("\n", informe.ObjetivosExtra.Select(o => Marca(o.Cumplido) + "<indent=7%>" + o.Texto + "</indent>"))
                : "");
            if (textoObjetivos != null) textoObjetivos.transform.parent.gameObject.SetActive(informe.ObjetivosExtra.Count > 0);
            evaluacion = e;
            lineaMejor = null;
            PonerDesglose(false);
        }

        Evaluacion evaluacion;
        string lineaMejor;

        /// <summary>¿Se ve el desglose? Cerrado, el detalle muestra solo lo esencial: evidencias y total de puntos.</summary>
        public bool DesgloseAbierto { get; private set; }

        /// <summary>Abre o cierra el desglose: recursos, fichas, objetivos extra y cada línea de los puntos.</summary>
        public void AlternarDesglose()
        {
            PonerDesglose(!DesgloseAbierto);
            ReproductorUI.Sonar(ArteDatos.Sonido.Click);
        }

        void PonerDesglose(bool abierto)
        {
            DesgloseAbierto = abierto;
            if (desglose != null) desglose.SetActive(abierto);
            if (botonDesglose != null)
                botonDesglose.GetComponentInChildren<TMP_Text>().text =
                    (abierto ? Idioma.T("Hide breakdown") : Idioma.T("Show breakdown")) + "  " + (abierto ? Iconos.Sube : Iconos.Baja);
            var op = EstadoJuego.Operacion;
            textoPuntos.text = op != null && evaluacion != null ? TextoPuntos(evaluacion, op, catalogo.Nivel(EstadoJuego.NivelIndice), abierto) : "";
        }

        static void Poner(TMP_Text t, string texto) { if (t != null) t.text = texto; }

        /// <summary>✓ en verde o aviso en rojo: el estado se lee por la forma del icono, no solo por el color.</summary>
        public static string Marca(bool bien) =>
            "<color=" + (bien ? ColorBien : ColorMal) + ">" + (bien ? Iconos.Verde : Iconos.Rojo) + "</color>";

        static string FilaEvidencia(InformeMision.FilaEvidencia f) =>
            Marca(f.Llego) + "<indent=7%><b>" + Iconos.DeInstrumento(f.InstrumentoId) + " " + f.Nombre + "</b>  <size=80%><color=" + ColorTenue + ">" +
            (f.Llego ? Idioma.T("Reached Earth") : Idioma.T("Missing")) + (f.Principal ? "" : Idioma.T(" · supporting")) +
            "</color></size>\n<size=86%><color=" + ColorTenue + ">" + f.Detalle + "</color></size></indent>";

        void LlenarBarras(InformeMision.Informe informe)
        {
            if (barras == null || textosBarra == null) return;
            for (int i = 0; i < barras.Length && i < textosBarra.Length; i++)
            {
                var b = informe != null && i < informe.Barras.Count ? informe.Barras[i] : null;
                if (textosBarra[i] != null) textosBarra[i].text = b != null ? b.Texto : "";
                if (barras[i] != null) barras[i].anchorMax = new Vector2(b != null ? b.Fraccion : 0f, 1f);
            }
        }

        void LlenarFichas(InformeMision.Informe informe)
        {
            if (fichas == null) return;
            for (int i = 0; i < fichas.Length; i++)
            {
                if (fichas[i] == null) continue;
                var f = informe != null && i < informe.Fichas.Count ? informe.Fichas[i] : null;
                fichas[i].text = f != null ? "<size=170%><b>" + f.Valor + "</b></size>\n<color=" + ColorTenue + ">" + f.Etiqueta + "</color>" : "";
            }
        }

        /// <summary>Tabla de puntos: los puntos a la izquierda y el concepto al lado, y el total al final.</summary>
        string TextoPuntos(Evaluacion e, Operacion op, MisionDatos nivel, bool completo)
        {
            var lineas = Puntuacion.Desglose(e, op, catalogo, nivel);
            int total = Puntuacion.Total(lineas);
            var texto = "";
            if (completo) foreach (var l in lineas)
                texto += "<b>" + (l.Puntos >= 0 ? "+" : "") + TextosBriefing.Numero(l.Puntos) + "</b><indent=17%>" + l.Concepto + "</indent>\n";
            texto += "<size=120%><b>" + TextosBriefing.Numero(total) + "</b><indent=17%><b>" + Idioma.T("Total") + "</b>  <size=70%><color=" + ColorTenue + ">" +
                     nivel.NombreNivel + "</color></size></indent></size>\n";
            texto += lineaMejor ??= MejorPuntaje(total);   // una vez por informe: guarda el récord
            return texto;
        }

        /// <summary>Guarda en el navegador el mejor puntaje de cada nivel.</summary>
        string MejorPuntaje(int total)
        {
            var clave = "mejor_puntaje_nivel_" + EstadoJuego.NivelIndice;
            int mejor = PlayerPrefs.GetInt(clave, 0);
            if (!Application.isPlaying) return Idioma.T("Best score on this level: ") + TextosBriefing.Numero(System.Math.Max(mejor, total));
            if (total > mejor)
            {
                PlayerPrefs.SetInt(clave, total);
                PlayerPrefs.Save();
                return Idioma.T("<color=#22C55E>New best score on this level!</color>");
            }
            return Idioma.T("Best score on this level: ") + TextosBriefing.Numero(mejor);
        }

        /// <summary>Pasa del veredicto al detalle (evidencias, puntos y recursos).</summary>
        public void VerDetalle() => Cambiar(true);

        /// <summary>Vuelve del detalle al veredicto.</summary>
        public void VerVeredicto() => Cambiar(false);

        void Cambiar(bool detalle)
        {
            if (vistaVeredicto != null) vistaVeredicto.SetActive(!detalle);
            if (vistaDetalle != null) vistaDetalle.SetActive(detalle);
            ReproductorUI.Sonar(ArteDatos.Sonido.Click);
        }

        public void ModificarMision()
        {
            EstadoJuego.Resultado = null;
            EstadoJuego.Operacion = null;
            EstadoJuego.CategoriaFinal = null;
            EstadoJuego.EntrarEnConfiguracion = true;
            if (Application.isPlaying) SceneManager.LoadScene("Mision");
        }

        /// <summary>Versión 8: del informe al menú principal (la configuración se conserva por si vuelve a jugar).</summary>
        public void VolverAlMenu()
        {
            ReproductorUI.Callar();
            if (Application.isPlaying) SceneManager.LoadScene("Menu");
        }
    }
}
