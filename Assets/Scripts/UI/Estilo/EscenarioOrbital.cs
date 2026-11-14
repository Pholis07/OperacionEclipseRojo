using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Escenario 3D de Marte en órbita, compartido por la cinemática de llegada y la investigación orbital:
    /// el planeta, la línea de la órbita elegida (la misma forma que en Configuración, ver Trayectoria) y el
    /// satélite con los instrumentos elegidos. Solo presentación: no cambia nada de la lógica.
    ///
    /// «Pose de presentación»: el satélite tiene una fase fija en la órbita (el ancla) y la cámara un encuadre
    /// fijo respecto a él. Cada escaneo parte de ahí y vuelve ahí; así dos investigaciones seguidas empiezan
    /// con el mismo encuadre exacto, sin deriva.
    /// </summary>
    public class EscenarioOrbital : MonoBehaviour
    {
        [SerializeField] Transform marte;               // malla del planeta (gira para poner la región bajo la órbita)
        [SerializeField] float radioPlaneta = 13f;      // en unidades del mundo
        [SerializeField] float radioOrbita = 17.5f;     // órbita circular de referencia (Trayectoria radio = 1)
        [SerializeField] LineRenderer lineaOrbita;
        [SerializeField] Transform satelite;            // raíz de la nave (NaveUI + NaveSatelite)
        [SerializeField] NaveUI nave;
        [SerializeField] float faseAncla = 3.6f;        // radianes
        [SerializeField] Vector3 camaraRelativa = new Vector3(-1.6f, 1.5f, -4.6f); // en el marco de la órbita
        [SerializeField] float campoAncla = 46f;
        const int Puntos = 160;

        Quaternion giroBaseMarte;
        string orbita;

        public Transform Satelite => satelite;
        public Vector3 Centro => transform.position;
        public float RadioPlaneta => radioPlaneta;
        public float FaseAncla => faseAncla;
        public float CampoAncla => campoAncla;
        public string Orbita => orbita;

        void Awake()
        {
            if (marte != null) giroBaseMarte = marte.rotation;
        }

        /// <summary>Dibuja la órbita elegida, monta los instrumentos elegidos y deja el satélite en el ancla.</summary>
        public void Preparar(Configuracion config)
        {
            orbita = string.IsNullOrEmpty(config?.OrbitaId) ? "media" : config.OrbitaId;
            if (lineaOrbita != null)
            {
                lineaOrbita.useWorldSpace = false;
                lineaOrbita.loop = true;
                lineaOrbita.positionCount = Puntos;
                for (int i = 0; i < Puntos; i++)
                    lineaOrbita.SetPosition(i, Trayectoria.Punto(orbita, i * Mathf.PI * 2f / Puntos, radioOrbita / transform.lossyScale.x));
            }
            if (nave != null && config != null) nave.Mostrar(config);
            if (marte != null) marte.rotation = giroBaseMarte;
            ColocarSatelite(faseAncla, null);
        }

        public Vector3 PosicionEnOrbita(float fase) => Centro + Trayectoria.Punto(orbita ?? "media", fase, radioOrbita);

        /// <summary>Radial, tangente (sentido de avance) y normal del plano de la órbita en esa fase.</summary>
        public (Vector3 radial, Vector3 tangente, Vector3 normal) Marco(float fase)
        {
            var p = PosicionEnOrbita(fase);
            var radial = (p - Centro).normalized;
            var tangente = (PosicionEnOrbita(fase + 0.01f) - p).normalized;
            var normal = Vector3.Cross(radial, tangente).normalized;
            return (radial, tangente, normal);
        }

        /// <summary>
        /// Pone el satélite en la fase dada. Con «apuntarA», su cara de instrumentos (−Z) mira a ese punto;
        /// sin él, mira al centro del planeta (nadir).
        /// </summary>
        public void ColocarSatelite(float fase, Vector3? apuntarA)
        {
            if (satelite == null) return;
            var p = PosicionEnOrbita(fase);
            var (_, tangente, _) = Marco(fase);
            var objetivo = apuntarA ?? Centro;
            satelite.position = p;
            satelite.rotation = Quaternion.LookRotation((p - objetivo).normalized, tangente);
        }

        /// <summary>Pose de la cámara de presentación para el satélite en la fase dada.</summary>
        public (Vector3 posicion, Quaternion rotacion) PoseCamara(float fase)
        {
            var p = PosicionEnOrbita(fase);
            var (radial, tangente, normal) = Marco(fase);
            // Detrás y algo por encima del satélite, de lado: el satélite delante y el borde de Marte de fondo.
            var pos = p + tangente * camaraRelativa.z + radial * camaraRelativa.y + normal * camaraRelativa.x;
            var mira = p - radial * (radioPlaneta * 0.22f) + tangente * 2.5f;
            return (pos, Quaternion.LookRotation(mira - pos, radial));
        }

        /// <summary>
        /// Gira el planeta sobre su eje para que el punto (u, v) de la textura de Marte quede bajo la trayectoria
        /// del satélite en la fase dada (la latitud no se cambia). Devuelve la posición del punto en el mundo.
        /// </summary>
        public Vector3 AlinearRegion(Vector2 uv, float fase)
        {
            if (marte == null) return Centro;
            var local = DireccionTextura(uv);
            var deseada = (PosicionEnOrbita(fase) - Centro).normalized;
            var q = Quaternion.Inverse(giroBaseMarte) * deseada;
            float angLocal = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
            float angDeseado = Mathf.Atan2(q.z, q.x) * Mathf.Rad2Deg;
            // Girar θ sobre Y lleva el ángulo atan2(z, x) de a a a − θ.
            marte.rotation = giroBaseMarte * Quaternion.Euler(0f, angLocal - angDeseado, 0f);
            return PuntoEnSuperficie(uv);
        }

        public Vector3 PuntoEnSuperficie(Vector2 uv) =>
            marte == null ? Centro : Centro + marte.rotation * DireccionTextura(uv) * radioPlaneta;

        /// <summary>Dirección (radio 1) del punto (u, v) de la textura en la malla del planeta (Esfera_Planeta).</summary>
        public static Vector3 DireccionTextura(Vector2 uv)
        {
            float fi = Mathf.PI * Mathf.Clamp01(uv.y), la = 2f * Mathf.PI * uv.x;
            return new Vector3(-Mathf.Sin(fi) * Mathf.Cos(la), -Mathf.Cos(fi), -Mathf.Sin(fi) * Mathf.Sin(la));
        }

        public void MostrarOrbita(bool visible, float alfa = 0.85f)
        {
            if (lineaOrbita == null) return;
            lineaOrbita.enabled = visible;
            var c = lineaOrbita.startColor; c.a = alfa;
            lineaOrbita.startColor = lineaOrbita.endColor = c;
        }
    }
}
