using SpaceMission;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Explorador con el aspecto del Ares 1 (B) de NASA 3D Resources (ares1b.fbx). El FBX trae casi todo el cohete en
/// una sola malla, así que se parte por altura en las piezas que la cinemática mueve:
/// - el propio cohete (padre): primera etapa con su interestadio y los cuatro motores de separación;
/// - «EtapaSuperior»: la etapa superior, que sigue sola tras la separación (motor encendido en el vacío);
/// - «CofiaFrente» y «CofiaDetras», dentro de la etapa superior: el Orion y la torre de escape partidos en dos
///   mitades con bisagra, que se abren para liberar el satélite (el Ares no lleva cofia; es la adaptación).
/// No tiene propulsores laterales: en la separación cae la primera etapa entera.
/// Colores planos del modelo, con el estilo de tinta de los cohetes (materiales NASA_Cohete_Ares_*).
/// </summary>
public static partial class ConstructorEscenas
{
    /// <summary>Alto del Explorador en la escena (el Titán mide 11 y la torre 17). Igual en DirectorCinematica.</summary>
    public const float AltoExplorador = 7.5f;

    // Cortes en fracción del alto, medidos en el modelo (alto 9.66): el interestadio termina en 5.55 y el Orion
    // empieza en 7.6.
    const float CorteEtapaAres = 0.575f, CorteNarizAres = 0.787f;

    /// <summary>
    /// Versión 9: los lanzadores que se suman al Explorador y al Titán, todos de NASA 3D Resources sin banderas ni
    /// «USA» (ver CREDITOS.md). Cortes en fracción del alto, medidos en cada modelo:
    /// - Jupiter-C: la primera etapa termina en 0.76; la «cofia» es el tambor de etapas superiores con el satélite
    ///   (desde 0.845), que es donde iba el Explorer 1.
    /// - Ares 1 (A): mismo perfil que el Ares 1 (B).
    /// - Saturn V: el S-II termina en 0.62 (el radio pasa de 0.62 a 0.43) y el adaptador con el Apollo empieza en 0.79.
    /// El alto también está en PlataformaCohetes.Alto (la cinemática y la cámara lo usan).
    /// </summary>
    static readonly (string id, string ruta, string prefijo, float etapa, float nariz, Color? tinte, int motores, float x)[] CohetesExtra =
    {
        ("pionero", "NASA/jupiterc.fbx", "JupiterC", 0.76f, 0.845f, null, 1, -4.4f),
        ("horizonte", "NASA/ares1a.fbx", "AresA", 0.575f, 0.787f, null, 1, -2.2f),   // colores propios: ColoresHorizonte
        ("goliat", "NASA/saturnv.fbx", "Goliat", 0.62f, 0.79f, new Color(0.42f, 0.44f, 0.5f), 5, 1.4f),       // Saturn V en gris pizarra
        ("coloso", "NASA/saturnv.fbx", "SaturnV", 0.62f, 0.79f, null, 5, 3.6f),
    };

    /// <summary>Monta el Ares en «cohete» (base en su origen) y devuelve la etapa superior.</summary>
    static Transform ConstruirAres(Transform cohete) =>
        ConstruirPorEtapas(cohete, "NASA/ares1b.fbx", "Ares", AltoExplorador, CorteEtapaAres, CorteNarizAres);

    /// <summary>Llamas del Explorador (una tobera), medidas como las de la tabla.</summary>
    static (List<Renderer> pie, List<Renderer> superior) LlamasExplorador(Transform cohete) => LlamasDe(cohete, AltoExplorador, CorteEtapaAres, 1);

    /// <summary>
    /// Un lanzador de la tabla: el cohete «Cohete_id» en la plataforma, con su logo y su resalte. Lo usan la pestaña
    /// Lanzador y la cinemática (que le añade las llamas con LlamasExtra).
    /// </summary>
    static (string id, Transform cohete, GameObject resalte) ConstruirCoheteExtra(Transform raiz, int i, System.Func<string, Vector3, GameObject> resalte)
    {
        var d = CohetesExtra[i];
        float alto = PlataformaCohetes.Alto(d.id);
        var cohete = new GameObject("Cohete_" + d.id).transform;
        cohete.SetParent(raiz, false);
        cohete.localPosition = new Vector3(d.x, 0, 0);
        var superior = ConstruirPorEtapas(cohete, d.ruta, d.prefijo, alto, d.etapa, d.nariz, d.tinte, d.id == "horizonte" ? ColoresHorizonte : null,
            d.id == "pionero" ? FranjasPionero : (System.Func<string, Vector3, string>)null);
        // El logo va en la etapa superior, salvo en el Pionero: su etapa superior es un cono, así que va en el cuerpo.
        bool enCuerpo = d.id == "pionero";
        float alturaLogo = alto * (enCuerpo ? 0.6f : Mathf.Lerp(d.etapa, d.nariz, 0.45f));
        float ladoLogo = alto * (enCuerpo ? 0.06f : 0.044f);
        LogoPatrocinio(enCuerpo ? cohete : superior, "Fence", false, RadioNucleo(cohete, alturaLogo, ladoLogo) + 0.012f, alturaLogo, ladoLogo, cohete);
        cohete.gameObject.AddComponent<GiroLento>().enabled = false;
        return (d.id, cohete, resalte("Elegido_" + d.id, cohete.localPosition));
    }

    /// <summary>
    /// El Ares 1 (A) llega sin colores (todos sus materiales son el mismo gris). Se pinta por zonas con los
    /// materiales del modelo, que van en el mismo orden que los del Ares 1 (B): cuerpo blanco con bandas negras y
    /// acento naranja, para que se distinga del Explorador (etapa superior color espuma).
    /// </summary>
    static Color? ColoresHorizonte(string material)
    {
        var m = System.Text.RegularExpressions.Regex.Match(material, @"blinn(\d+)SG");
        if (!m.Success) return null;
        switch (int.Parse(m.Groups[1].Value))
        {
            case 1: return Hex("#F1F2F2");   // cuerpo
            case 2: return Hex("#E4E7EB");   // etapa superior (en el Ares 1 B es color espuma)
            case 3: return Hex("#6C6E7C");
            case 4: return Hex("#15171B");   // bandas
            case 5: return Hex("#E0782F");   // acento
            case 6: return Hex("#D1D1D1");
            case 7: return Hex("#C8372D");
            case 8: return Hex("#15171B");
            default: return null;
        }
    }

    /// <summary>
    /// El Jupiter-C llevaba el cuerpo pintado a cuadros blancos y negros (para seguir su giro en las películas del
    /// lanzamiento). El modelo llega sin ese patrón: aquí se pinta por cuadrantes, alternados entre el tramo de
    /// abajo y el de arriba. Los cortes caen en las aristas del cilindro (20 caras de 18°).
    /// </summary>
    static string FranjasPionero(string material, Vector3 centro)
    {
        bool arriba = material.StartsWith("jupiterc_blinn6S"), abajo = material.StartsWith("jupiterc_blinn7S");
        if (!arriba && !abajo) return null;
        float angulo = Mathf.Repeat(Mathf.Atan2(centro.z, centro.x) * Mathf.Rad2Deg, 360f);
        bool par = Mathf.FloorToInt(angulo / 90f) % 2 == 0;
        return par == arriba ? "jupiterc_blinn4S" : null;
    }

    /// <summary>
    /// Llamas de un lanzador: una por tobera al pie y la de la etapa superior, que sale de su tobera (bajo el corte).
    /// Las toberas se miden en el modelo (ToberasMedidas); si no se encuentran, se usa la proporción de antes.
    /// </summary>
    static (List<Renderer> pie, List<Renderer> superior) LlamasExtra(Transform cohete, string id)
    {
        var d = System.Array.Find(CohetesExtra, c => c.id == id);
        return LlamasDe(cohete, PlataformaCohetes.Alto(id), d.etapa, d.motores);
    }

    static (List<Renderer> pie, List<Renderer> superior) LlamasDe(Transform cohete, float alto, float fraccionEtapa, int motores)
    {
        var toberas = new List<(Transform, Vector3, float, float)>();
        var medidas = ToberasMedidas(cohete, alto, motores);
        if (medidas.Count == motores)
            foreach (var (pos, radio) in medidas) toberas.Add((cohete, pos, radio, alto * (motores == 1 ? 0.44f : 0.4f)));
        else
        {
            toberas.Add((cohete, new Vector3(0f, 0.05f, 0f), alto * 0.013f, alto * 0.44f));
            if (motores == 5)   // Saturn V: cinco F-1, uno central y cuatro en cruz
                for (int k = 0; k < 4; k++)
                    toberas.Add((cohete, Quaternion.Euler(0, 45f + 90f * k, 0) * Vector3.forward * alto * 0.024f + Vector3.up * 0.05f, alto * 0.011f, alto * 0.4f));
        }
        var superior = cohete.Find("EtapaSuperior");
        var perfil = cohete.GetComponent<PerfilCohete>();
        float corte = perfil != null ? perfil.RadioCorte : alto * 0.03f;
        // La boca de la tobera de la etapa superior (ConstruirPorEtapas): radio 0.5 × corte, 1.1 × corte bajo el plano.
        return (ConstruirLlamas(toberas), ConstruirLlamas(new[] { (superior, new Vector3(0f, fraccionEtapa * alto - corte * 1.1f, 0f), corte * 0.3f, alto * 0.16f) }));
    }

    /// <summary>Multiplica el color del material (recién puesto por CrearMaterial, así que no se acumula).</summary>
    static Material Tenir(Material m, Color? tinte)
    {
        if (m == null || !tinte.HasValue) return m;
        var c = m.GetColor("_BaseColor") * tinte.Value;
        m.SetColor("_BaseColor", new Color(Mathf.Min(c.r, 1f), Mathf.Min(c.g, 1f), Mathf.Min(c.b, 1f), 1f));
        EditorUtility.SetDirty(m);
        return m;
    }
}
