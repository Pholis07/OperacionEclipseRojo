// Borde de atmósfera para el planeta: brillo aditivo que crece hacia el contorno (efecto Fresnel).
// Va en una esfera un poco más grande que el planeta. URP, sin luces, barato en WebGL.
Shader "OperacionEclipseRojo/Atmosfera"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.0, 0.55, 0.35, 1)
        _Potencia ("Potencia del borde", Range(0.5, 8)) = 3
        _Intensidad ("Intensidad", Range(0, 4)) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Potencia;
                half _Intensidad;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; float3 normal : NORMAL; };
            struct Salida { float4 posicion : SV_POSITION; float3 normal : TEXCOORD0; float3 vista : TEXCOORD1; };

            Salida vert(Entrada e)
            {
                Salida s;
                float3 mundo = TransformObjectToWorld(e.posicion.xyz);
                s.posicion = TransformWorldToHClip(mundo);
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.vista = GetWorldSpaceViewDir(mundo);
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                half borde = 1.0h - saturate(dot(normalize(s.normal), normalize(s.vista)));
                half brillo = pow(borde, _Potencia) * _Intensidad;
                return half4(_Color.rgb * brillo, 1.0h);
            }
            ENDHLSL
        }
    }
}
