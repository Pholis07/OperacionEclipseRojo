using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Aspecto de la nave según el tipo de satélite elegido (modelos de NASA 3D Resources):
    /// - Enciende el cuerpo del satélite elegido y apaga los demás.
    /// - Lleva cada módulo de instrumento (Modulo_*) a su montaje en ese satélite: sobre el casco, con un soporte
    ///   y una placa que llegan hasta la superficie (el constructor calcula los montajes de cada modelo), y con el
    ///   tamaño que cabe en ese satélite sin que los módulos se encimen.
    /// - Zócalos: un aro en cada montaje. Tenue y latiendo si está vacío; apagado cuando el módulo lo ocupa. Al
    ///   quitar un instrumento, el zócalo destella un momento.
    /// - Fantasmas: el holograma de cada instrumento en su montaje (lo enciende CamaraConfiguracion al pasar el
    ///   cursor por un instrumento sin instalar).
    /// Qué módulos se ven lo sigue decidiendo NaveUI (en el mismo cuadro); esto solo cambia el aspecto. Sin
    /// satélite elegido se usa el primero de la lista. Con «sateliteFijo» (Menú) no mira la configuración.
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
        [Tooltip("Largo del soporte de cada montaje, en el espacio de la nave.")]
        [SerializeField] float[] largosSoporte;
        [Tooltip("Escala de los módulos en cada satélite (para que quepan sin encimarse).")]
        [SerializeField] float[] escalas;
        [SerializeField] SpriteRenderer[] zocalos;      // uno por módulo (opcional)
        [SerializeField] Transform[] fantasmas;         // uno por módulo (opcional)
        [SerializeField] Transform[] tapas;             // tapa ciega de cada soporte vacío (opcional)
        [Tooltip("Mecanismos desplegados (en órbita y en el informe) o plegados (hangar de integración).")]
        [SerializeField] bool desplegada;
        [SerializeField] string sateliteFijo;
        [SerializeField] Color colorZocalo = new Color(0.37f, 0.78f, 0.9f, 0.55f);

        string aplicado;
        bool[] estaban;
        float[] destellos;

        public string Actual => aplicado;

        /// <summary>¿Los mecanismos van desplegados cuando la nave está en reposo?</summary>
        public bool Desplegada => desplegada;

        /// <summary>Despliega (0 a 1) los mecanismos de los instrumentos montados. La cinemática lo anima tras liberar la nave.</summary>
        public void Desplegar(float k)
        {
            if (modulos == null) return;
            foreach (var m in modulos)
            {
                if (m == null) continue;
                var mec = m.GetComponent<MecanismoInstrumento>();
                if (mec != null) mec.Desplegar(k);
            }
        }

        /// <summary>Deja los mecanismos en la pose de reposo de esta nave (plegados en el hangar, desplegados en órbita).</summary>
        public void PoseDeReposo() => Desplegar(desplegada ? 1f : 0f);

        void OnEnable() { Aplicar(Elegido()); PoseDeReposo(); }

        void Update()
        {
            var id = Elegido();
            if (id != aplicado) Aplicar(id);
            AnimarZocalos();
        }

        string Elegido()
        {
            if (!string.IsNullOrEmpty(sateliteFijo)) return sateliteFijo;
            var id = EstadoJuego.Configuracion?.SateliteId;
            return string.IsNullOrEmpty(id) && idsSatelite != null && idsSatelite.Length > 0 ? idsSatelite[0] : id;
        }

        /// <summary>Muestra u oculta el holograma del módulo (índice en el orden de los instrumentos).</summary>
        public void MostrarFantasma(int indice, bool visible)
        {
            if (fantasmas == null) return;
            for (int m = 0; m < fantasmas.Length; m++)
            {
                if (fantasmas[m] == null) continue;
                bool ver = visible && m == indice;
                if (fantasmas[m].gameObject.activeSelf != ver) fantasmas[m].gameObject.SetActive(ver);
            }
        }

        /// <summary>Muestra solo el holograma del instrumento con ese id (null: ninguno).</summary>
        public void MostrarFantasma(string idInstrumento)
        {
            if (fantasmas == null) return;
            foreach (var f in fantasmas)
            {
                if (f == null) continue;
                bool ver = idInstrumento != null && f.name == "Fantasma_" + idInstrumento;
                if (f.gameObject.activeSelf != ver) f.gameObject.SetActive(ver);
            }
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
            float escala = escalas != null && s < escalas.Length && escalas[s] > 0f ? escalas[s] : 1f;
            for (int m = 0; m < modulos.Length; m++)
            {
                int k = s * modulos.Length + m;
                if (k >= posiciones.Length) continue;
                float largo = largosSoporte != null && k < largosSoporte.Length ? largosSoporte[k] : 0.035f;
                if (modulos[m] != null)
                {
                    modulos[m].localPosition = posiciones[k];
                    modulos[m].localRotation = rotaciones[k];
                    modulos[m].localScale = Vector3.one * escala;
                    ColocarSoporte(modulos[m], largo / escala);
                }
                if (fantasmas != null && m < fantasmas.Length && fantasmas[m] != null)
                {
                    fantasmas[m].localPosition = posiciones[k];
                    fantasmas[m].localRotation = rotaciones[k];
                    fantasmas[m].localScale = Vector3.one * escala;
                }
                if (tapas != null && m < tapas.Length && tapas[m] != null)
                {
                    // La tapa ciega cierra el soporte vacío, a ras del casco.
                    tapas[m].localPosition = posiciones[k] - rotaciones[k] * Vector3.up * (largo * 2f - 0.012f);
                    tapas[m].localRotation = rotaciones[k];
                    tapas[m].localScale = Vector3.one * escala;
                    tapas[m].gameObject.SetActive(modulos[m] == null || !modulos[m].gameObject.activeSelf);
                }
                if (zocalos != null && m < zocalos.Length && zocalos[m] != null)
                {
                    // El aro va pegado al casco (debajo del módulo), del tamaño de la base del módulo.
                    var z = zocalos[m].transform;
                    z.localPosition = posiciones[k] - rotaciones[k] * Vector3.up * (largo * 2f - 0.01f);
                    z.localRotation = rotaciones[k] * Quaternion.Euler(90f, 0f, 0f);
                    z.localScale = Vector3.one * escala * 0.42f;
                }
            }
        }

        /// <summary>Soporte (cilindro) desde el casco hasta el módulo y placa base sobre el casco.</summary>
        static void ColocarSoporte(Transform modulo, float largo)
        {
            var soporte = modulo.Find("Soporte");
            if (soporte != null)
            {
                var e = soporte.localScale;
                soporte.localScale = new Vector3(e.x, largo, e.z);
                soporte.localPosition = new Vector3(0f, -largo, 0f);
            }
            var placa = modulo.Find("Placa");
            if (placa != null) placa.localPosition = new Vector3(0f, -largo * 2f + placa.localScale.y, 0f);
        }

        void AnimarZocalos()
        {
            if (zocalos == null || modulos == null) return;
            if (estaban == null || estaban.Length != modulos.Length)
            {
                estaban = new bool[modulos.Length];
                destellos = new float[modulos.Length];
                for (int m = 0; m < modulos.Length; m++) estaban[m] = modulos[m] != null && modulos[m].gameObject.activeSelf;
            }
            float latido = Preferencias.ReducirMovimiento ? 0.75f : 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 2.4f);
            for (int m = 0; m < zocalos.Length && m < modulos.Length; m++)
            {
                var z = zocalos[m];
                if (z == null || modulos[m] == null) continue;
                bool ocupado = modulos[m].gameObject.activeSelf;
                if (estaban[m] && !ocupado)
                {
                    destellos[m] = 1f;   // se acaba de quitar
                    Retirada.Empezar(modulos[m]);
                }
                if (tapas != null && m < tapas.Length && tapas[m] != null && tapas[m].gameObject.activeSelf == ocupado)
                    tapas[m].gameObject.SetActive(!ocupado);
                estaban[m] = ocupado;
                destellos[m] = Mathf.Max(0f, destellos[m] - Time.unscaledDeltaTime * 2.2f);
                var c = colorZocalo;
                if (ocupado) c.a = 0f;
                else c.a *= latido;
                if (destellos[m] > 0f) c = Color.Lerp(c, new Color(1f, 0.55f, 0.3f, 1f), destellos[m]);
                z.color = c;
            }
        }
    }
}
