using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Plataforma de la pestaña Lanzador:
    /// - Sin lanzador elegido: los dos cohetes, uno a cada lado, para comparar.
    /// - Con uno elegido: solo ese, en el centro de la plataforma, con una entrada corta (sube desde el foso y se
    ///   asienta) y girando despacio. El otro se oculta y vuelve a su giro inicial.
    /// Solo presentación: lee EstadoJuego.Configuracion.LanzadorId.
    /// </summary>
    public class PlataformaCohetes : MonoBehaviour
    {
        [SerializeField] Transform explorador;
        [SerializeField] Transform titan;
        [SerializeField] Transform resalteExplorador;
        [SerializeField] Transform resalteTitan;
        [SerializeField] float subida = 1.2f;

        Vector3 posExplorador, posTitan;
        Quaternion rotExplorador, rotTitan;
        string mostrado = "-";
        Coroutine entrada;

        public static bool EsTitan(string lanzadorId) => lanzadorId == "titan";

        void Awake()
        {
            if (explorador != null) { posExplorador = explorador.localPosition; rotExplorador = explorador.localRotation; }
            if (titan != null) { posTitan = titan.localPosition; rotTitan = titan.localRotation; }
        }

        void Update()
        {
            var id = EstadoJuego.Configuracion?.LanzadorId;
            if (id == mostrado) return;
            mostrado = id;
            Mostrar(id);
        }

        void Mostrar(string id)
        {
            bool ninguno = string.IsNullOrEmpty(id);
            bool esTitan = EsTitan(id);
            Preparar(explorador, posExplorador, rotExplorador, resalteExplorador, ninguno || !esTitan, !ninguno && !esTitan);
            Preparar(titan, posTitan, rotTitan, resalteTitan, ninguno || esTitan, !ninguno && esTitan);
            if (ninguno) return;
            var elegido = esTitan ? titan : explorador;
            if (elegido == null) return;
            if (entrada != null) StopCoroutine(entrada);
            if (isActiveAndEnabled) entrada = StartCoroutine(Animar.Durante(0.6f, t =>
                elegido.localPosition = new Vector3(0f, -subida * (1f - t), 0f), Animar.SalidaRebote));
        }

        static void Preparar(Transform cohete, Vector3 pos, Quaternion rot, Transform resalte, bool visible, bool elegido)
        {
            if (cohete == null) return;
            if (cohete.gameObject.activeSelf != visible) cohete.gameObject.SetActive(visible);
            cohete.localRotation = rot;
            cohete.localPosition = elegido ? Vector3.zero : pos;
            var giro = cohete.GetComponent<GiroLento>();
            if (giro != null) giro.enabled = elegido;
            if (resalte != null)
            {
                resalte.gameObject.SetActive(elegido);
                resalte.localPosition = new Vector3(elegido ? 0f : pos.x, resalte.localPosition.y, resalte.localPosition.z);
            }
        }
    }
}
