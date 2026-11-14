using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceMission
{
    /// <summary>
    /// Mapa de la región para la fase de Operación: una textura generada por código (suelo marciano con
    /// manchas de polvo) que avisa dónde pulsó el jugador, en coordenadas de 0 a 1, y dibuja las marcas.
    /// </summary>
    public class MapaMarteUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] UnityEngine.UI.RawImage imagen;
        [SerializeField] RectTransform capaMarcas;

        public event Action<Vector2> AlPulsar;
        readonly List<GameObject> marcas = new List<GameObject>();
        Texture2D textura;

        /// <summary>Genera un terreno distinto según la semilla.</summary>
        public void Generar(int semilla)
        {
            const int lado = 256;
            if (textura == null)
            {
                textura = new Texture2D(lado, lado, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp };
                imagen.texture = textura;
            }
            var azar = new System.Random(semilla);
            float ox = azar.Next(0, 1000), oy = azar.Next(0, 1000);
            var suelo = new Color(0.62f, 0.30f, 0.18f);
            var oscuro = new Color(0.36f, 0.17f, 0.11f);
            var polvo = new Color(0.80f, 0.62f, 0.45f);
            var pixeles = new Color[lado * lado];
            for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float u = (float)x / lado, v = (float)y / lado;
                float relieve = Mathf.PerlinNoise(ox + u * 6f, oy + v * 6f) * 0.7f + Mathf.PerlinNoise(ox + u * 18f, oy + v * 18f) * 0.3f;
                float nube = Mathf.Clamp01((Mathf.PerlinNoise(oy + u * 3f, ox + v * 3f) - 0.45f) * 2.2f);
                var c = Color.Lerp(oscuro, suelo, relieve);
                pixeles[y * lado + x] = Color.Lerp(c, polvo, nube * 0.75f);
            }
            textura.SetPixels(pixeles);
            textura.Apply();
        }

        public void Limpiar()
        {
            foreach (var m in marcas) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
            marcas.Clear();
        }

        /// <summary>Dibuja una marca redonda en el punto (0 a 1).</summary>
        public void Marcar(Vector2 punto, Color color)
        {
            var go = new GameObject("Marca", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(capaMarcas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = punto;
            rt.sizeDelta = new Vector2(28, 28);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = color;
            img.raycastTarget = false;
            marcas.Add(go);
        }

        public void OnPointerClick(PointerEventData datos)
        {
            var rt = (RectTransform)imagen.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, datos.position, datos.pressEventCamera, out var local))
                return;
            var r = rt.rect;
            var punto = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
            if (punto.x < 0 || punto.x > 1 || punto.y < 0 || punto.y > 1) return;
            AlPulsar?.Invoke(punto);
        }
    }
}
