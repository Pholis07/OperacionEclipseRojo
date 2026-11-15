// Borde de atmósfera para el planeta: brillo aditivo que crece hacia el contorno (efecto Fresnel).
// Va en una esfera un poco más grande que el planeta. Más intenso del lado del Sol. URP, barato en WebGL.
Shader "OperacionEclipseRojo/Atmosfera"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.0, 0.55, 0.35, 1)
        _Potencia ("Potencia del borde", Range(0.5, 8)) = 3
        _Intensidad ("Intensidad", Range(0, 4)) = 1.2
        _DependeSol ("Más brillo del lado del Sol", Range(0, 1)) = 0.75
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Potencia;
                half _Intensidad;
                half _DependeSol;
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
                // Del lado de día el borde brilla más; del de noche queda un hilo tenue.
                half sol = saturate(dot(normalize(s.normal), GetMainLight().direction) * 0.8h + 0.35h);
                brillo *= lerp(1.0h, sol * 1.1h, _DependeSol);
                return half4(_Color.rgb * brillo, 1.0h);
            }
            ENDHLSL
        }
    }
}
