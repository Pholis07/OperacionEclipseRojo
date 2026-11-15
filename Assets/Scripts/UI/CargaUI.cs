using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Vista Carga (gameplay v3): entre Revisión y Lanzar se eligen las herramientas que suben a la nave.
    /// Toda la lógica está en Carga; aquí solo se dibuja. Interfaz básica: Diego la estiliza.
    /// </summary>
    public class CargaUI : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] MisionUI mision;
        [SerializeField] TMP_Text textoRecursos;
        [SerializeField] Transform listaHerramientas;
        [SerializeField] UnityEngine.UI.Button plantillaHerramienta;
        [SerializeField] UnityEngine.UI.Button botonVolver;
        [SerializeField] UnityEngine.UI.Button botonLanzar;
        [SerializeField] UnityEngine.UI.Button botonUsarMro;

        static readonly Color ColorNormal = new Color32(0x2A, 0x2A, 0x31, 0xFF);
        static readonly Color ColorABordo = new Color32(0x14, 0x53, 0x2D, 0xFF);

        readonly List<GameObject> filas = new List<GameObject>();
        bool inicializada;

        public Carga Carga { get; private set; }
        public UnityEngine.UI.Button BotonLanzar => botonLanzar;

        void Awake() => Inicializar();

        public void Inicializar()
        {
            if (inicializada) return;
            inicializada = true;
            plantillaHerramienta.gameObject.SetActive(false);
            botonVolver.onClick.AddListener(() => mision.MostrarRevision(EstadoJuego.Resultado));
            botonLanzar.onClick.AddListener(Lanzar);
            if (botonUsarMro != null) botonUsarMro.onClick.AddListener(UsarMro);
        }

        /// <summary>Empieza una carga vacía para la misión evaluada.</summary>
        public void Mostrar(Evaluacion e)
        {
            Inicializar();
            Carga = new Carga(e, EstadoJuego.Configuracion, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice));
            Refrescar();
        }

        public void Alternar(string herramientaId)
        {
            if (Carga.Lleva(herramientaId)) Carga.Quitar(herramientaId);
            else Carga.Cargar(herramientaId);
            Refrescar();
        }

        /// <summary>Cambia el satélite elegido por el MRO, que no cuesta ni pesa.</summary>
        public void UsarMro()
        {
            Carga.UsarMro();
            // Versión 6: el satélite cuenta en la masa. La evaluación guardada se rehace con el MRO para que el
            // informe y las barras (al volver a la Configuración) digan la masa real.
            var nivel = catalogo.Nivel(EstadoJuego.NivelIndice);
            EstadoJuego.Resultado = Evaluador.Evaluar(EstadoJuego.Configuracion, catalogo, nivel);
            var configuracion = FindFirstObjectByType<ConfiguracionUI>(FindObjectsInactive.Include);
            if (configuracion != null) configuracion.Recalcular();
            Refrescar();
        }

        void Lanzar()
        {
            if (!Carga.PuedeLanzar) return;
            EstadoJuego.Carga = Carga;
            mision.MostrarInvestigacion();
        }

        void Refrescar()
        {
            foreach (var f in filas) { if (Application.isPlaying) Destroy(f); else DestroyImmediate(f); }
            filas.Clear();

            textoRecursos.text =
                Idioma.T("Cargo hold with ") + catalogo.reglas.espaciosBodega + Idioma.T(" slots. You pay with what you have left over, and free mass becomes fuel (1 per ") +
                catalogo.reglas.kgPorCombustible + " kg).\n" +
                Idioma.T("<b>Available credits:</b> ") + TextosBriefing.Numero(Carga.CreditosDisponibles) +
                Idioma.T("   <b>Free mass:</b> ") + Carga.MasaLibreKg + " kg" +
                Idioma.T("   <b>Free slots:</b> ") + Carga.EspaciosLibres +
                Idioma.T("\n<b>Fuel at departure:</b> ") + Carga.Combustible;
            var s = Carga.Satelite;
            if (s != null)
                textoRecursos.text += Idioma.T("\n<b>Satellite:</b> ") + s.Nombre + "<size=85%>" +
                                      PresentacionMision.Modificadores(Modificadores.DeSatelite(s, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice))) + "</size>";
            if (!Carga.PuedeLanzar)
                textoRecursos.text += Idioma.T("\n<color=#EF4444><b>What you have left is not enough.</b></color> Remove tools or use the MRO to launch.");
            botonLanzar.interactable = Carga.PuedeLanzar;
            if (botonUsarMro != null) botonUsarMro.gameObject.SetActive(!Carga.PuedeLanzar && s != null && s.id != Carga.IdMro);

            foreach (var h in catalogo.herramientas)
            {
                bool aBordo = Carga.Lleva(h.id);
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Carga_" + h.id;
                boton.interactable = aBordo || Carga.PuedeCargar(h.id);
                boton.GetComponent<UnityEngine.UI.Image>().color = aBordo ? ColorABordo : ColorNormal;
                boton.GetComponentInChildren<TMP_Text>().text =
                    (aBordo ? Idioma.T("ON BOARD  ·  ") : "") + "<b>" + h.Nombre + "</b>  ·  " + h.costoCr + " cr  ·  " + h.masaKg + " kg\n" +
                    "<size=80%>" + h.Descripcion + "</size>";
                boton.onClick.AddListener(() => Alternar(h.id));
                filas.Add(boton.gameObject);
            }
        }
    }
}
