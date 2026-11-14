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

        void Lanzar()
        {
            EstadoJuego.Carga = Carga;
            mision.MostrarInvestigacion();
        }

        void Refrescar()
        {
            foreach (var f in filas) { if (Application.isPlaying) Destroy(f); else DestroyImmediate(f); }
            filas.Clear();

            textoRecursos.text =
                "Elige qué herramientas subes. La bodega tiene " + catalogo.reglas.espaciosBodega + " espacios y solo pagas con lo que te sobra.\n" +
                "La capacidad del lanzador que quede libre se convierte en combustible (1 por cada " + catalogo.reglas.kgPorCombustible + " kg).\n\n" +
                "<b>Créditos disponibles:</b> " + TextosBriefing.Numero(Carga.CreditosDisponibles) +
                "   <b>Masa libre:</b> " + Carga.MasaLibreKg + " kg" +
                "   <b>Espacios libres:</b> " + Carga.EspaciosLibres +
                "\n<b>Combustible al salir:</b> " + Carga.Combustible;

            foreach (var h in catalogo.herramientas)
            {
                bool aBordo = Carga.Lleva(h.id);
                var boton = Instantiate(plantillaHerramienta, listaHerramientas);
                boton.gameObject.SetActive(true);
                boton.name = "Carga_" + h.id;
                boton.interactable = aBordo || Carga.PuedeCargar(h.id);
                boton.GetComponent<UnityEngine.UI.Image>().color = aBordo ? ColorABordo : ColorNormal;
                boton.GetComponentInChildren<TMP_Text>().text =
                    (aBordo ? "A BORDO  ·  " : "") + "<b>" + h.nombre + "</b>  ·  " + h.costoCr + " cr  ·  " + h.masaKg + " kg\n" +
                    "<size=80%>" + h.descripcion + "</size>";
                boton.onClick.AddListener(() => Alternar(h.id));
                filas.Add(boton.gameObject);
            }
        }
    }
}
