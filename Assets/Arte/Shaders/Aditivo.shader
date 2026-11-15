// Brillo aditivo sin luces: textura × color × color de vértice, sumado a lo que hay detrás.
// Sirve para SpriteRenderer, LineRenderer, TrailRenderer y partículas (halo del Sol, estelas de órbita,
// chispas, núcleo de la llama). URP, una sola pasada, barato en WebGL.
Shader "OperacionEclipseRojo/Aditivo"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Salida { float4 posicion : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Salida vert(Entrada e)
            {
                Salida s;
                s.posicion = TransformObjectToHClip(e.posicion.xyz);
                s.uv = TRANSFORM_TEX(e.uv, _MainTex);
                s.color = e.color * _Color;
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, s.uv) * s.color;
            }
            ENDHLSL
        }
    }
}
