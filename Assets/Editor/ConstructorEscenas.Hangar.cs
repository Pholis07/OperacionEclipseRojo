using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renovación visual: hangar de integración, el escenario del constructor. Tres planos de profundidad:
/// 1) la máquina, en su cuna; 2) el equipo de servicio (riel con carro y brazo, carro de piezas, escalera);
/// 3) la pared con su estructura, pasarela y luces de trabajo. Geometría original con primitivas y estilo de
/// tinta (materiales Hangar_*). Es un decorado: no se recorre y no cambia nada del juego.
/// </summary>
public static partial class ConstructorEscenas
{
    static GameObject ConstruirHangar(Vector3 centroNave)
    {
        var raiz = new GameObject("HangarIntegracion");
        raiz.transform.position = centroNave;
        var t = raiz.transform;
        var acero = CrearMaterial("Hangar_Acero", Hex("#2A333D"), 0.5f, 0.35f);
        var pared = CrearMaterial("Hangar_Pared", Hex("#1B222A"), 0.2f, 0.25f);
        var suelo = CrearMaterial("Hangar_Suelo", Hex("#232B33"), 0.3f, 0.4f);
        var claro = CrearMaterial("Hangar_Proteccion", Hex("#9AA6AD"), 0.4f, 0.45f);
        var marca = CrearMaterial("Hangar_Marca", Hex("#C99A2E"), 0f, 0.3f);
        var lampara = CrearMaterial("Hangar_Lampara", Hex("#3A4650"), 0f, 0.5f, null, Vector2.one, Hex("#CFE8F2") * 2.4f);
        GameObject Caja(string nombre, Vector3 p, Vector3 e, Material m, Vector3? giro = null)
        {
            var go = Pieza(PrimitiveType.Cube, nombre, t, p, giro ?? Vector3.zero, e, m);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        const float piso = -2.3f, fondo = 6.5f;

        // ----- Plano 3: pared, costillas, pasarela y luces de trabajo -----
        Caja("Pared", new Vector3(0f, 2.2f, fondo), new Vector3(34f, 12f, 0.4f), pared);
        Caja("Techo", new Vector3(0f, 6.6f, 2f), new Vector3(34f, 0.4f, 12f), pared);
        for (int i = -5; i <= 5; i++)
        {
            Caja("Costilla", new Vector3(i * 3f, 2.2f, fondo - 0.35f), new Vector3(0.28f, 12f, 0.35f), acero);
            if (i < 5) Caja("Riostra", new Vector3(i * 3f + 1.5f, 3.4f, fondo - 0.3f), new Vector3(3.3f, 0.12f, 0.12f), acero, new Vector3(0f, 0f, i % 2 == 0 ? 24f : -24f));
        }
        Caja("Pasarela", new Vector3(0f, 1.9f, fondo - 0.9f), new Vector3(34f, 0.12f, 1.1f), acero);
        Caja("Baranda", new Vector3(0f, 2.6f, fondo - 1.4f), new Vector3(34f, 0.06f, 0.06f), claro);
        for (int i = -8; i <= 8; i++) Caja("Poste", new Vector3(i * 2f, 2.25f, fondo - 1.4f), new Vector3(0.05f, 0.7f, 0.05f), claro);
        Caja("Franja", new Vector3(0f, -0.4f, fondo - 0.22f), new Vector3(34f, 0.5f, 0.05f), marca);
        foreach (float x in new[] { -7.5f, -2.5f, 2.5f, 7.5f })
        {
            Caja("Lampara", new Vector3(x, 6.25f, 2.4f), new Vector3(2.2f, 0.12f, 0.5f), lampara);
            Caja("Carcasa", new Vector3(x, 6.36f, 2.4f), new Vector3(2.5f, 0.14f, 0.8f), acero);
        }

        // ----- Suelo con marcas de escala alrededor de la cuna -----
        Caja("Suelo", new Vector3(0f, piso - 0.2f, 1.5f), new Vector3(34f, 0.4f, 16f), suelo);
        foreach (float x in new[] { -3.2f, 3.2f }) Caja("MarcaLado", new Vector3(x, piso + 0.012f, 0f), new Vector3(0.12f, 0.02f, 6.4f), marca);
        foreach (float z in new[] { -3.2f, 3.2f }) Caja("MarcaFrente", new Vector3(0f, piso + 0.012f, z), new Vector3(6.5f, 0.02f, 0.12f), marca);
        for (int i = -6; i <= 6; i++)
            Caja("Regla", new Vector3(i * 0.5f, piso + 0.012f, -3.55f), new Vector3(0.04f, 0.02f, i % 2 == 0 ? 0.5f : 0.25f), claro);

        // ----- Plano 1: la cuna que sostiene la máquina -----
        foreach (int lado in new[] { -1, 1 })
        {
            Caja("Pilar", new Vector3(lado * 1.7f, piso + 0.55f, 0.4f), new Vector3(0.3f, 1.1f, 0.3f), acero);
            Caja("Zapata", new Vector3(lado * 1.7f, piso + 0.04f, 0.4f), new Vector3(0.9f, 0.08f, 0.9f), acero);
            Caja("Brazo", new Vector3(lado * 1.25f, piso + 1.25f, 0.4f), new Vector3(1.1f, 0.16f, 0.2f), acero, new Vector3(0f, 0f, lado * 28f));
            Caja("Apoyo", new Vector3(lado * 0.72f, piso + 1.62f, 0.4f), new Vector3(0.28f, 0.1f, 0.5f), marca);
        }
        Caja("Travesano", new Vector3(0f, piso + 0.22f, 0.4f), new Vector3(3.4f, 0.14f, 0.2f), acero);

        // ----- Plano 2: riel con carro y brazo, carro de piezas y escalera de servicio -----
        Caja("Riel", new Vector3(0f, 4.6f, 1.2f), new Vector3(24f, 0.3f, 0.3f), acero);
        Caja("RielBorde", new Vector3(0f, 4.42f, 1.2f), new Vector3(24f, 0.06f, 0.5f), marca);
        Caja("Polipasto", new Vector3(2.4f, 4.3f, 1.2f), new Vector3(0.8f, 0.4f, 0.6f), claro);
        Caja("Mastil", new Vector3(2.4f, 3.5f, 1.2f), new Vector3(0.12f, 1.4f, 0.12f), acero);
        Caja("Pinza", new Vector3(2.4f, 2.75f, 1.2f), new Vector3(0.5f, 0.12f, 0.3f), marca);
        Caja("Carro", new Vector3(-4.6f, piso + 0.5f, -0.6f), new Vector3(1.6f, 0.12f, 1f), acero);
        foreach (var (x, z) in new[] { (-0.7f, -0.4f), (0.7f, -0.4f), (-0.7f, 0.4f), (0.7f, 0.4f) })
            Caja("Pata", new Vector3(-4.6f + x, piso + 0.24f, -0.6f + z), new Vector3(0.08f, 0.48f, 0.08f), acero);
        Caja("Caja1", new Vector3(-4.95f, piso + 0.76f, -0.6f), new Vector3(0.6f, 0.4f, 0.6f), claro);
        Caja("Caja2", new Vector3(-4.2f, piso + 0.7f, -0.75f), new Vector3(0.45f, 0.28f, 0.4f), marca);
        Caja("Escalera", new Vector3(4.9f, piso + 0.9f, 0.9f), new Vector3(0.9f, 0.1f, 2.4f), acero, new Vector3(-38f, 0f, 0f));
        Caja("Plataforma", new Vector3(4.9f, piso + 1.65f, 2.2f), new Vector3(1.3f, 0.1f, 1.1f), acero);

        // Luz de trabajo: fría y cercana, para que la nave se lea contra la pared oscura.
        var foco = new GameObject("LuzTrabajo", typeof(Light)).GetComponent<Light>();
        foco.transform.SetParent(t, false);
        foco.transform.localPosition = new Vector3(-2.2f, 4.2f, -3.2f);
        foco.type = LightType.Point;
        foco.range = 16f;
        foco.intensity = 16f;
        foco.color = new Color(0.82f, 0.92f, 1f);
        foco.shadows = LightShadows.None;
        var relleno = new GameObject("LuzRelleno", typeof(Light)).GetComponent<Light>();
        relleno.transform.SetParent(t, false);
        relleno.transform.localPosition = new Vector3(3.4f, 1.2f, -3.6f);
        relleno.type = LightType.Point;
        relleno.range = 14f;
        relleno.intensity = 7f;
        relleno.color = new Color(1f, 0.86f, 0.7f);
        relleno.shadows = LightShadows.None;
        raiz.SetActive(false);   // lo enciende CamaraConfiguracion con la nave
        return raiz;
    }
}
