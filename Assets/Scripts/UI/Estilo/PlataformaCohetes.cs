using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Plataforma de la pestaña Lanzador:
    /// - Sin lanzador elegido: los tres cohetes en fila, para comparar.
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
        [SerializeField] Transform coloso;           // versión 9 (Saturn V)
        [SerializeField] Transform resalteColoso;
        [SerializeField] float subida = 1.2f;

        (string id, Transform cohete, Transform resalte, Vector3 pos, Quaternion rot)[] cohetes;
        string mostrado = "-";
        Coroutine entrada;

        public static bool EsTitan(string lanzadorId) => lanzadorId == "titan";
        public static bool EsColoso(string lanzadorId) => lanzadorId == "coloso";

        void Awake()
        {
            cohetes = new[] { ("explorador", explorador, resalteExplorador), ("titan", titan, resalteTitan), ("coloso", coloso, resalteColoso) }
                .Where(c => c.Item2 != null)
                .Select(c => (c.Item1, c.Item2, c.Item3, c.Item2.localPosition, c.Item2.localRotation)).ToArray();
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
            // Sin elección se ven todos; un lanzador sin modelo propio se muestra con el Explorador.
            string visto = ninguno || cohetes.Any(c => c.id == id) ? id : "explorador";
            Transform elegido = null;
            foreach (var c in cohetes)
            {
                bool esEste = !ninguno && c.id == visto;
                Preparar(c.cohete, c.pos, c.rot, c.resalte, ninguno || esEste, esEste);
                if (esEste) elegido = c.cohete;
            }
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
