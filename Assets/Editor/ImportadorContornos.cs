using System.Collections.Generic;
using SpaceMission;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Eclipse Ink (versión 8): prepara las mallas para el contorno de tinta del shader Tinta.
/// El contorno se dibuja empujando cada vértice a lo largo de su normal. En las mallas facetadas (cubos, modelos
/// de la NASA con aristas duras) cada cara tiene su propia normal y el contorno se abre en las aristas. Para que
/// quede cerrado, aquí se calcula una normal suavizada (la media de las caras que comparten ese punto) y se guarda
/// en el canal UV 3 de la malla (TEXCOORD2), codificada en octaedro. El shader usa la normal de siempre para la
/// luz y esta solo para el contorno.
/// Es un importador aparte de ImportadorArte para que subir la versión de aquel no reimporte todas las texturas.
/// </summary>
public class ImportadorContornos : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    public override int GetPostprocessOrder() => 10;   // después de ImportadorArte (UV de caja y tangentes)

    void OnPostprocessModel(GameObject raiz)
    {
        if (!assetPath.StartsWith("Assets/Arte/Modelos/")) return;
        foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
            if (filtro.sharedMesh != null) NormalesContorno.Preparar(filtro.sharedMesh);
    }
}
