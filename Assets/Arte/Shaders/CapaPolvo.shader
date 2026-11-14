// Capa de polvo (tormenta) sobre el planeta: textura con transparencia iluminada por la luz principal.
// Va en una esfera un poco más grande que el planeta y gira algo más rápido que él. URP, barato en WebGL.
Shader "OperacionEclipseRojo/CapaPolvo"
{
    Properties
    {
        _BaseMap ("Polvo (RGBA)", 2D) = "white" {}
        _Opacidad ("Opacidad", Range(0, 1)) = 0.55
        _Ambiente ("Luz ambiente", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Opacidad;
                half _Ambiente;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Salida { float4 posicion : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };

            Salida vert(Entrada e)
            {
                Salida s;
                s.posicion = TransformObjectToHClip(e.posicion.xyz);
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.uv = TRANSFORM_TEX(e.uv, _BaseMap);
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, s.uv);
                Light sol = GetMainLight();
                half luz = saturate(dot(normalize(s.normal), sol.direction)) + _Ambiente;
                return half4(c.rgb * sol.color * luz, c.a * _Opacidad);
            }
            ENDHLSL
        }
    }
}
