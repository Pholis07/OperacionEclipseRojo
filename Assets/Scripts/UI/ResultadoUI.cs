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
                textoTitulo.text = "Sin misión lanzada";
                textoConclusion.text = "Vuelve al constructor para preparar y lanzar una misión.";
                textoEvidencias.text = textoFaltantes.text = textoRecursos.text = textoPuntos.text = "";
                return;
            }

            var op = EstadoJuego.Operacion;
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            var informe = InformeMision.Construir(e, c, catalogo, op, nivel);
            textoTitulo.text = informe.Titulo;
            textoConclusion.text = informe.Texto;

            var evidencias = "<b>Evidencias obtenidas</b>\n" +
                             (informe.Evidencias.Count > 0 ? "• " + string.Join("\n• ", informe.Evidencias) : "Ninguna pista principal.");
            if (informe.Revelaciones.Count > 0)
                evidencias += "\n\n<b>Revelación complementaria</b>\n• " + string.Join("\n• ", informe.Revelaciones);
            textoEvidencias.text = evidencias;

            textoFaltantes.text = informe.Faltantes.Count > 0
                ? "<b>Lo que faltó</b>\n• " + string.Join("\n• ", informe.Faltantes)
                : "<b>Lo que faltó</b>\nNada: la misión cubrió todo lo que pedía su órbita.";
            textoRecursos.text = "<b>Recursos usados</b>\n" + informe.Recursos;
            textoPuntos.text = op != null ? TextoPuntos(e, op, nivel) : "";
        }

        string TextoPuntos(Evaluacion e, Operacion op, MisionDatos nivel)
        {
            var lineas = Puntuacion.Desglose(e, op, catalogo, nivel);
            int total = Puntuacion.Total(lineas);
            var texto = "<b>Puntos: " + TextosBriefing.Numero(total) + "</b>  (" + nivel.nombreNivel + ")\n";
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
            if (!Application.isPlaying) return "Mejor puntaje en este nivel: " + TextosBriefing.Numero(System.Math.Max(mejor, total));
            if (total > mejor)
            {
                PlayerPrefs.SetInt(clave, total);
                PlayerPrefs.Save();
                return "<color=#22C55E>¡Nuevo mejor puntaje en este nivel!</color>";
            }
            return "Mejor puntaje en este nivel: " + TextosBriefing.Numero(mejor);
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
