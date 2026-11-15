using System.Collections;
using TMPro;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Entrada de la escena Resultado (unos 5 s, un clic la salta), según lo que pasó en la misión:
    /// - Victoria: pulsos de datos viajan del satélite a la Tierra y cae el sello «MISIÓN CUMPLIDA».
    /// - Parcial: el enlace parpadea, algunos pulsos se pierden por el camino y cae el sello ámbar.
    /// - Fracaso: la tormenta cubre Marte, el satélite se apaga, estática de señal perdida y sello rojo.
    /// Detalles de la partida: si hubo apagones el satélite parpadea; los datos que se quedaron en la memoria
    /// orbitan junto a la nave como bloques; sin combustible, la nave deriva girando.
    /// Después los paneles del informe entran escalonados y el total de puntos cuenta hacia arriba.
    /// Solo presentación: lee EstadoJuego. Los textos del informe (Titulo, BotonModificar…) existen completos
    /// desde el primer cuadro; aquí solo cambian alfa, posición y escala.
    /// </summary>
    public class FinalAnimado : MonoBehaviour
    {
        [SerializeField] CatalogoDatos catalogo;
        [SerializeField] Camera camara;
        [SerializeField] NaveUI nave;
        [SerializeField] Transform tierra;
        [SerializeField] Renderer polvo;                 // capa de la tormenta de Marte
        [SerializeField] SpriteRenderer[] pulsos;        // pulsos de datos (aditivos, apagados al empezar)
        [SerializeField] Transform[] bloquesMemoria;     // datos que no se enviaron
        [SerializeField] CanvasGroup[] paneles;          // bloques del informe, en orden de entrada
        [SerializeField] CanvasGroup veloInforme;        // oscurece la escena 3D detrás del informe
        [SerializeField] RectTransform sello;
        [SerializeField] TMP_Text textoSello;
        [SerializeField] UnityEngine.UI.Graphic aroSello;
        [SerializeField] TMP_Text totalPuntos;
        [SerializeField] CanvasGroup estatica;
        [SerializeField] UnityEngine.UI.Button saltar;
        [SerializeField] AudioSource fuente;

        static readonly Color Verde = new Color(0.13f, 0.77f, 0.37f);
        static readonly Color Ambar = new Color(0.92f, 0.7f, 0.03f);
        static readonly Color Rojo = new Color(0.94f, 0.27f, 0.27f);

        CategoriaResultado categoria;
        bool saltando, terminado;
        int total;
        Vector3 poseCamara;
        Quaternion giroCamara;
        Vector3 posNave;
        Quaternion giroNave;
        bool deriva;

        void Start()
        {
            if (saltar != null) saltar.onClick.AddListener(() => saltando = true);
            var e = EstadoJuego.Resultado;
            var op = EstadoJuego.Operacion;
            categoria = EstadoJuego.CategoriaFinal ?? e?.Categoria ?? CategoriaResultado.Fracaso;
            if (nave != null && EstadoJuego.Configuracion != null) nave.Mostrar(EstadoJuego.Configuracion);
            if (nave != null) { posNave = nave.transform.position; giroNave = nave.transform.rotation; }
            if (camara != null) { poseCamara = camara.transform.position; giroCamara = camara.transform.rotation; }
            total = 0;
            if (op != null && e != null && catalogo != null)
                total = Puntuacion.Total(Puntuacion.Desglose(e, op, catalogo, catalogo.Nivel(EstadoJuego.NivelIndice)));
            deriva = op != null && op.Combustible <= 0;
            PrepararSello();
            if (bloquesMemoria != null)
                for (int i = 0; i < bloquesMemoria.Length; i++)
                    if (bloquesMemoria[i] != null) bloquesMemoria[i].gameObject.SetActive(op != null && i < op.Memoria.Count);

            if (e == null || !e.SePuedeLanzar || !Application.isPlaying || Preferencias.ReducirMovimiento)
            {
                EstadoFinal();
                return;
            }
            foreach (var p in paneles) if (p != null) p.alpha = 0f;
            if (veloInforme != null) veloInforme.alpha = 0f;
            if (sello != null) sello.gameObject.SetActive(false);
            if (totalPuntos != null) totalPuntos.alpha = 0f;
            if (saltar != null) saltar.gameObject.SetActive(true);
            StartCoroutine(Entrada(op));
        }

        void Update()
        {
            // Detalles que siguen después de la entrada: bloques de memoria en órbita y la deriva sin combustible.
            if (bloquesMemoria != null && nave != null)
                for (int i = 0; i < bloquesMemoria.Length; i++)
                {
                    var b = bloquesMemoria[i];
                    if (b == null || !b.gameObject.activeSelf) continue;
                    float a = Time.time * 0.8f + i * Mathf.PI * 2f / bloquesMemoria.Length;
                    b.position = nave.transform.position + new Vector3(Mathf.Cos(a) * 1.1f, Mathf.Sin(a * 1.3f) * 0.25f, Mathf.Sin(a) * 1.1f);
                    b.rotation = Quaternion.Euler(Time.time * 40f + i * 30f, Time.time * 60f, 0f);
                }
            if (deriva && nave != null && !Preferencias.ReducirMovimiento)
                nave.transform.rotation = giroNave * Quaternion.Euler(Time.time * 9f, Time.time * 13f, Time.time * 5f);
        }

        IEnumerator Entrada(Operacion op)
        {
            var satelite = nave != null ? nave.transform : null;
            // 1) La cámara se acerca a la nave en órbita (y los apagones la hacen parpadear).
            float t0 = Time.time;
            var desde = poseCamara + new Vector3(-1.5f, 0.8f, -3f);
            while (Time.time - t0 < 1.4f && !saltando)
            {
                float k = Animar.SalidaCubica((Time.time - t0) / 1.4f);
                camara.transform.position = Vector3.Lerp(desde, poseCamara, k);
                camara.transform.rotation = giroCamara;
                if (op != null && op.Apagones > 0 && satelite != null)
                    satelite.gameObject.SetActive(Mathf.PerlinNoise(Time.time * 9f, 0.3f) > 0.28f);
                yield return null;
            }
            if (satelite != null) satelite.gameObject.SetActive(true);

            // 2) Lo que pasó con la señal.
            if (categoria == CategoriaResultado.Fracaso) yield return Fracaso(satelite);
            else yield return Envio(satelite, categoria == CategoriaResultado.Victoria);

            // 3) Sello.
            yield return CaerSello();

            // 4) El informe entra escalonado y el total de puntos cuenta.
            yield return EntrarInforme();
            EstadoFinal();
        }

        IEnumerator Envio(Transform satelite, bool completo)
        {
            if (pulsos == null || satelite == null || tierra == null) yield break;
            Sonar(ArteDatos.Efecto.DatosRecibidos, 0.7f);
            float duracion = 2.2f;
            float t0 = Time.time;
            while (Time.time - t0 < duracion && !saltando)
            {
                float t = Time.time - t0;
                for (int i = 0; i < pulsos.Length; i++)
                {
                    var p = pulsos[i];
                    if (p == null) continue;
                    float k = (t * 0.9f - i * 0.22f) % 1.2f;
                    bool vivo = k >= 0f && k <= 1f;
                    // Parcial: el enlace se corta a ratos y algunos pulsos se pierden a mitad de camino.
                    if (!completo && (i % 2 == 1 && k > 0.5f || Mathf.PerlinNoise(t * 4f, i) < 0.3f)) vivo = false;
                    p.gameObject.SetActive(vivo);
                    if (!vivo) continue;
                    p.transform.position = Vector3.Lerp(satelite.position, tierra.position, k);
                    var c = completo ? new Color(0.45f, 0.95f, 1f, 1f) : new Color(1f, 0.82f, 0.35f, 1f);
                    c.a = Mathf.Sin(k * Mathf.PI);
                    p.color = c;
                }
                yield return null;
            }
            foreach (var p in pulsos) if (p != null) p.gameObject.SetActive(false);
        }

        IEnumerator Fracaso(Transform satelite)
        {
            Sonar(ArteDatos.Efecto.SenalPerdida);
            var bloque = new MaterialPropertyBlock();
            float opacidad = polvo != null && polvo.sharedMaterial.HasProperty("_Opacidad") ? polvo.sharedMaterial.GetFloat("_Opacidad") : 0.5f;
            float t0 = Time.time;
            while (Time.time - t0 < 2.2f && !saltando)
            {
                float k = (Time.time - t0) / 2.2f;
                if (polvo != null)
                {
                    polvo.GetPropertyBlock(bloque);
                    bloque.SetFloat("_Opacidad", Mathf.Lerp(opacidad, 0.95f, k));
                    polvo.SetPropertyBlock(bloque);
                }
                if (estatica != null) estatica.alpha = Mathf.Clamp01(k * 1.4f) * (0.35f + 0.25f * Mathf.PerlinNoise(Time.time * 30f, 0f));
                // La nave se apaga: parpadea y queda a oscuras.
                if (satelite != null) satelite.gameObject.SetActive(k < 0.7f && Mathf.PerlinNoise(Time.time * 12f, 1f) > 0.35f);
                yield return null;
            }
            if (satelite != null) satelite.gameObject.SetActive(true);
            if (estatica != null) estatica.alpha = 0.18f;
        }

        void PrepararSello()
        {
            if (textoSello == null) return;
            switch (categoria)
            {
                case CategoriaResultado.Victoria: textoSello.text = Idioma.T("MISSION\nACCOMPLISHED"); break;
                case CategoriaResultado.Parcial: textoSello.text = Idioma.T("INCOMPLETE\nSIGNAL"); break;
                default: textoSello.text = Idioma.T("INVESTIGATION\nFAILED"); break;
            }
            var color = categoria == CategoriaResultado.Victoria ? Verde : categoria == CategoriaResultado.Parcial ? Ambar : Rojo;
            textoSello.color = color;
            if (aroSello != null) aroSello.color = color;
        }

        IEnumerator CaerSello()
        {
            if (sello == null) yield break;
            sello.gameObject.SetActive(true);
            var grupo = sello.GetComponent<CanvasGroup>();
            Sonar(ArteDatos.Efecto.Sello);
            Sonar(categoria == CategoriaResultado.Victoria ? ArteDatos.Efecto.RemateVictoria
                : categoria == CategoriaResultado.Parcial ? ArteDatos.Efecto.RemateParcial : ArteDatos.Efecto.RemateFracaso, 0.9f);
            yield return Animar.Durante(saltando ? 0f : 0.35f, t =>
            {
                sello.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, t);
                sello.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-18f, -8f, t));
                if (grupo != null) grupo.alpha = t;
            });
            // Pequeño rebote al «golpear» el papel.
            yield return Animar.Durante(saltando ? 0f : 0.2f, t => sello.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, Animar.SalidaRebote(t)));
            float espera = Time.time + 0.7f;
            while (Time.time < espera && !saltando) yield return null;
        }

        IEnumerator EntrarInforme()
        {
            if (saltar != null) saltar.gameObject.SetActive(false);
            if (veloInforme != null) StartCoroutine(Animar.Durante(0.5f, t => veloInforme.alpha = t));
            foreach (var p in paneles)
            {
                if (p == null) continue;
                var rt = (RectTransform)p.transform;
                var destino = rt.anchoredPosition;
                StartCoroutine(Animar.Durante(0.45f, t => { p.alpha = t; rt.anchoredPosition = destino + new Vector2(0f, -30f * (1f - t)); }));
                yield return new WaitForSeconds(0.12f);
            }
            if (totalPuntos == null || total <= 0) yield break;
            totalPuntos.alpha = 1f;
            float pasoSonido = 0f;
            yield return Animar.Durante(1.2f, t =>
            {
                totalPuntos.text = TextosBriefing.Numero(Mathf.RoundToInt(total * t)) + Idioma.T(" <size=45%>POINTS</size>");
                pasoSonido += Time.deltaTime;
                if (pasoSonido > 0.07f && t < 1f) { pasoSonido = 0f; Sonar(ArteDatos.Efecto.ConteoPuntos, 0.5f); }
            });
        }

        /// <summary>Todo en su sitio (al terminar, al saltar o con «Reducir movimiento»).</summary>
        void EstadoFinal()
        {
            if (terminado) return;
            terminado = true;
            foreach (var p in paneles) if (p != null) p.alpha = 1f;
            if (veloInforme != null) veloInforme.alpha = 1f;
            if (sello != null)
            {
                sello.gameObject.SetActive(EstadoJuego.Resultado != null && EstadoJuego.Resultado.SePuedeLanzar);
                sello.localScale = Vector3.one;
                sello.localRotation = Quaternion.Euler(0f, 0f, -8f);
                var g = sello.GetComponent<CanvasGroup>();
                if (g != null) g.alpha = 1f;
            }
            if (totalPuntos != null)
            {
                totalPuntos.alpha = total > 0 ? 1f : 0f;
                totalPuntos.text = TextosBriefing.Numero(total) + Idioma.T(" <size=45%>POINTS</size>");
            }
            if (pulsos != null) foreach (var p in pulsos) if (p != null) p.gameObject.SetActive(false);
            if (saltar != null) saltar.gameObject.SetActive(false);
            if (nave != null) nave.gameObject.SetActive(true);
            if (camara != null) camara.transform.SetPositionAndRotation(poseCamara, giroCamara);
        }

        void Sonar(ArteDatos.Efecto efecto, float volumen = 1f)
        {
            if (fuente == null || ReproductorUI.Actual == null || ReproductorUI.Actual.Arte == null) return;
            var clip = ReproductorUI.Actual.Arte.Clip(efecto);
            if (clip != null) fuente.PlayOneShot(clip, volumen * Preferencias.VolumenEfectos);
        }
    }
}
