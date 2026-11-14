using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Aspecto de la nave según el tipo de satélite elegido (modelos de NASA 3D Resources):
    /// - Enciende el cuerpo del satélite elegido y apaga los demás.
    /// - Lleva cada módulo de instrumento (Modulo_*) a su montaje en ese satélite: sobre el casco, con un soporte
    ///   que llega hasta la superficie (el constructor calcula los montajes de cada modelo).
    /// Qué módulos se ven lo sigue decidiendo NaveUI; esto solo cambia el aspecto. Sin satélite elegido se usa
    /// el primero de la lista. Con «sateliteFijo» (Menú) no mira la configuración.
    /// </summary>
    public class NaveSatelite : MonoBehaviour
    {
        [SerializeField] string[] idsSatelite;
        [SerializeField] GameObject[] cuerpos;
        [Tooltip("Módulos de instrumento, en el mismo orden para todos los satélites.")]
        [SerializeField] Transform[] modulos;
        [Tooltip("Por satélite y módulo (satélite * módulos + módulo): posición y giro locales del montaje.")]
        [SerializeField] Vector3[] posiciones;
        [SerializeField] Quaternion[] rotaciones;
        [Tooltip("Largo del soporte de cada montaje (escala Y del hijo «Soporte» del módulo).")]
        [SerializeField] float[] largosSoporte;
        [SerializeField] string sateliteFijo;

        string aplicado;

        public string Actual => aplicado;

        void OnEnable() => Aplicar(Elegido());

        void Update()
        {
            var id = Elegido();
            if (id != aplicado) Aplicar(id);
        }

        string Elegido()
        {
            if (!string.IsNullOrEmpty(sateliteFijo)) return sateliteFijo;
            var id = EstadoJuego.Configuracion?.SateliteId;
            return string.IsNullOrEmpty(id) && idsSatelite != null && idsSatelite.Length > 0 ? idsSatelite[0] : id;
        }

        public void Aplicar(string id)
        {
            if (idsSatelite == null || cuerpos == null) return;
            int s = System.Array.IndexOf(idsSatelite, id);
            if (s < 0) s = 0;
            aplicado = idsSatelite.Length > 0 ? idsSatelite[s] : null;
            for (int i = 0; i < cuerpos.Length; i++)
                if (cuerpos[i] != null) cuerpos[i].SetActive(i == s);

            if (modulos == null || posiciones == null) return;
            for (int m = 0; m < modulos.Length; m++)
            {
                int k = s * modulos.Length + m;
                if (modulos[m] == null || k >= posiciones.Length) continue;
                modulos[m].localPosition = posiciones[k];
                modulos[m].localRotation = rotaciones[k];
                var soporte = modulos[m].Find("Soporte");
                if (soporte != null && largosSoporte != null && k < largosSoporte.Length)
                {
                    var e = soporte.localScale;
                    soporte.localScale = new Vector3(e.x, largosSoporte[k], e.z);
                    soporte.localPosition = new Vector3(0f, -largosSoporte[k], 0f);
                }
            }
        }
    }
}
