using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Eclipse Ink (versión 8): qué materiales llevan el shader Tinta (cel shading de 3 bandas y contorno) y con qué
/// ajustes. La lista es explícita: los planetas, el Sol, las órbitas, las partículas, los logos y la interfaz
/// siguen como estaban.
///
/// Para volver atrás: poner <see cref="EstiloTinta"/> en false y ejecutar «Herramientas/Construir escenas (sin Menu)»
/// o «Herramientas/Eclipse Ink/Quitar». Los materiales vuelven a URP/Lit con sus colores y texturas (los dos shaders
/// usan los mismos nombres de propiedades).
/// </summary>
public static partial class ConstructorEscenas
{
    /// <summary>Interruptor del estilo. Con false, CrearMaterial devuelve cada material a URP/Lit.</summary>
    const bool EstiloTinta = true;

    const string ShaderTinta = "OperacionEclipseRojo/Tinta";
    const string ShaderLit = "Universal Render Pipeline/Lit";

    /// <summary>Ajustes del estilo para un grupo de materiales.</summary>
    struct AjusteTinta
    {
        public float contorno;       // px a 1080p; 0 = sin contorno
        public float maximo;         // tope del contorno en unidades del mundo
        public float borde;          // fuerza de la luz de borde
        public float brillo;         // fuerza del brillo técnico
    }

    /// <summary>
    /// Piloto (entrega 1): el satélite y los instrumentos. Devuelve false para todo lo demás.
    /// Los vidrios y las piezas que emiten luz llevan bandas pero no contorno.
    /// </summary>
    static bool AjusteDe(string nombre, bool emite, out AjusteTinta ajuste)
    {
        ajuste = new AjusteTinta { contorno = 1.5f, maximo = 0.02f, borde = 0.22f, brillo = 0.6f };
        if (nombre.StartsWith("NASA_Sat_") || nombre.StartsWith("CubeSat_"))
        {
            // Láminas y paneles finos: un contorno más delgado para que no se empasten.
            if (nombre.Contains("panel") || nombre.Contains("Celdas") || nombre.Contains("foil")) ajuste.contorno = 1.2f;
        }
        else if (nombre.StartsWith("Instrumento_"))
        {
            ajuste.contorno = 1.2f;
            if (nombre.Contains("Vidrio")) { ajuste.contorno = 0f; ajuste.borde = 0.1f; }
        }
        else if (nombre == "Nave_Plata" || nombre == "Nave_Placa")
            ajuste.contorno = 1.2f;
        else
            return false;
        if (emite) ajuste.contorno = 0f;
        return true;
    }

    /// <summary>Pone (o quita) el estilo en un material, según la lista. Lo llama CrearMaterial.</summary>
    static void Entintar(Material mat)
    {
        if (mat == null) return;
        bool emite = mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > 0.01f;
        if (!AjusteDe(mat.name, emite, out var ajuste)) return;
        var deseado = Shader.Find(EstiloTinta ? ShaderTinta : ShaderLit);
        if (deseado == null) { Debug.LogError("Falta el shader " + (EstiloTinta ? ShaderTinta : ShaderLit)); return; }
        if (mat.shader != deseado) mat.shader = deseado;
        if (EstiloTinta)
        {
            mat.SetFloat("_Contorno", ajuste.contorno);
            mat.SetFloat("_ContornoMaximo", ajuste.maximo);
            mat.SetFloat("_FuerzaBorde", ajuste.borde);
            mat.SetFloat("_FuerzaBrillo", ajuste.brillo);
            mat.SetColor("_ColorSombra", Hex("#324A57"));
            mat.SetColor("_ColorBorde", Hex("#60D7E4"));
            // Las piezas casi negras (paneles, mantas térmicas) llevan la línea en azul pizarra: la tinta
            // profunda no se vería sobre ellas ni contra el espacio.
            var c = mat.GetColor("_BaseColor");
            var bajo = mat.name.ToLowerInvariant();
            bool oscuro = bajo.Contains("panel") || bajo.Contains("celdas") || bajo.Contains("black") || bajo.Contains("krinkle") ||
                          bajo.Contains("oscuro") || (mat.GetTexture("_BaseMap") == null && c.maxColorComponent < 0.25f);
            mat.SetColor("_ColorContorno", Hex(oscuro ? "#3F5F70" : "#121A25"));
        }
        EditorUtility.SetDirty(mat);
    }

    /// <summary>¿El material se dibuja con contorno de tinta? (Sus mallas necesitan la normal suavizada.)</summary>
    static bool LlevaContorno(Material mat) =>
        mat != null && mat.shader != null && mat.shader.name == ShaderTinta && mat.GetFloat("_Contorno") > 0.001f;

    static readonly Dictionary<PrimitiveType, Mesh> primitivasTinta = new Dictionary<PrimitiveType, Mesh>();

    /// <summary>
    /// Copia de una primitiva de Unity (cubo, cilindro…) con la normal suavizada del contorno. Las primitivas de
    /// Unity no se pueden modificar, así que las piezas con contorno usan esta copia (Assets/Arte/Mallas/Tinta_*).
    /// </summary>
    static Mesh PrimitivaTinta(PrimitiveType tipo, Mesh original)
    {
        if (primitivasTinta.TryGetValue(tipo, out var lista) && lista != null) return lista;
        var copia = Object.Instantiate(original);
        return primitivasTinta[tipo] = GuardarMalla(copia, "Tinta_" + tipo, true);
    }

    /// <summary>Aplica el estilo a los materiales de la lista que ya existen (sin reconstruir las escenas).</summary>
    [MenuItem("Herramientas/Eclipse Ink/Aplicar a los materiales")]
    public static void AplicarTinta()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materiales" }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            var antes = mat.shader;
            Entintar(mat);
            if (mat.shader != antes) n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Eclipse Ink: " + n + " materiales cambiaron de shader");
    }

    /// <summary>Devuelve a URP/Lit todos los materiales que usan el shader Tinta (vuelta atrás completa).</summary>
    [MenuItem("Herramientas/Eclipse Ink/Quitar")]
    public static void QuitarTinta()
    {
        var lit = Shader.Find(ShaderLit);
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materiales" }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat.shader == null || mat.shader.name != ShaderTinta) continue;
            mat.shader = lit;
            EditorUtility.SetDirty(mat);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Eclipse Ink: " + n + " materiales devueltos a URP/Lit");
    }
}
