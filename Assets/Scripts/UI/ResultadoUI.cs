using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceMission
{
    /// <summary>
    /// Escena Resultado: conclusión, evidencias, revelación complementaria, lo que faltó y recursos usados.
    /// «Modificar mi misión» vuelve a Configuración conservando las elecciones. Sin puntos ni «Nueva misión».
    /// </summary>
    public class ResultadoUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] TMP_Text textoTitulo;
        [SerializeField] TMP_Text textoConclusion;
        [SerializeField] TMP_Text textoEvidencias;
        [SerializeField] TMP_Text textoFaltantes;
        [SerializeField] TMP_Text textoRecursos;
        [SerializeField] TMP_Text textoPuntos;
        [SerializeField] UnityEngine.UI.Button botonModificar;

        void Awake()
        {
            botonModificar.onClick.AddListener(ModificarMision);
            Mostrar(EstadoJuego.Resultado, EstadoJuego.Configuracion);
        }

        public void Mostrar(Evaluacion e, Configuracion c)
        {
            if (e == null || !e.SePuedeLanzar)
            {
                textoTitulo.text = Idioma.T("No mission launched");
                textoConclusion.text = Idioma.T("Go back to the builder to prepare and launch a mission.");
                textoEvidencias.text = textoFaltantes.text = textoRecursos.text = textoPuntos.text = "";
                return;
            }

            var op = EstadoJuego.Operacion;
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            var informe = InformeMision.Construir(e, c, catalogo, op, nivel);
            textoTitulo.text = informe.Titulo;
            textoConclusion.text = informe.Texto;

            var evidencias = Idioma.T("<b>Evidence obtained</b>\n") +
                             (informe.Evidencias.Count > 0 ? "• " + string.Join("\n• ", informe.Evidencias) : Idioma.T("No main clue."));
            if (informe.Revelaciones.Count > 0)
                evidencias += Idioma.T("\n\n<b>Supporting revelation</b>\n• ") + string.Join("\n• ", informe.Revelaciones);
            textoEvidencias.text = evidencias;

            textoFaltantes.text = informe.Faltantes.Count > 0
                ? Idioma.T("<b>What was missing</b>\n• ") + string.Join("\n• ", informe.Faltantes)
                : Idioma.T("<b>What was missing</b>\nNothing: the mission covered everything its orbit required.");
            textoRecursos.text = Idioma.T("<b>Resources used</b>\n") + informe.Recursos;
            textoPuntos.text = op != null ? TextoPuntos(e, op, nivel) : "";
        }

        string TextoPuntos(Evaluacion e, Operacion op, MisionDatos nivel)
        {
            var lineas = Puntuacion.Desglose(e, op, catalogo, nivel);
            int total = Puntuacion.Total(lineas);
            var texto = Idioma.T("<b>Points: ") + TextosBriefing.Numero(total) + "</b>  (" + nivel.NombreNivel + ")\n";
            foreach (var l in lineas)
                texto += l.Concepto + ": " + (l.Puntos >= 0 ? "+" : "") + TextosBriefing.Numero(l.Puntos) + "\n";
            texto += MejorPuntaje(total);
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

        public void ModificarMision()
        {
            EstadoJuego.Resultado = null;
            EstadoJuego.Operacion = null;
            EstadoJuego.CategoriaFinal = null;
            EstadoJuego.EntrarEnConfiguracion = true;
            if (Application.isPlaying) SceneManager.LoadScene("Mision");
        }
    }
}
