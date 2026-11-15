// Cielo del complejo de lanzamiento: esfera grande vista desde dentro. Degradado de horizonte a cenit, disco
// del Sol con halo y nubes que se mueven (textura que se repite, proyectada sobre un plano alto). _Noche lleva
// el cielo de día al negro del espacio durante el ascenso (lo anima DirectorCinematica). URP, sin luces.
Shader "OperacionEclipseRojo/Cielo"
{
    Properties
    {
        _ColorHorizonte ("Horizonte", Color) = (0.78, 0.84, 0.9, 1)
        _ColorCenit ("Cenit", Color) = (0.22, 0.45, 0.8, 1)
        _ColorEspacio ("Espacio", Color) = (0.01, 0.012, 0.03, 1)
        _DireccionSol ("Dirección al Sol", Vector) = (0.5, 0.35, 0.8, 0)
        [HDR] _ColorSol ("Sol", Color) = (4, 3.4, 2.6, 1)
        _Nubes ("Nubes", 2D) = "black" {}
        _EscalaNubes ("Escala de las nubes", Float) = 0.6
        _VelocidadNubes ("Velocidad de las nubes", Float) = 0.004
        _Noche ("Noche (0 día, 1 espacio)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Cull Front
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Nubes); SAMPLER(sampler_Nubes);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorHorizonte;
                half4 _ColorCenit;
                half4 _ColorEspacio;
                float4 _DireccionSol;
                half4 _ColorSol;
                float4 _Nubes_ST;
                float _EscalaNubes;
                float _VelocidadNubes;
                half _Noche;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; };
            struct Salida { float4 posicion : SV_POSITION; float3 mundo : TEXCOORD0; };

            Salida vert(Entrada e)
            {
                Salida s;
                s.mundo = TransformObjectToWorld(e.posicion.xyz);
                s.posicion = TransformWorldToHClip(s.mundo);
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                float3 dir = normalize(s.mundo - _WorldSpaceCameraPos);
                half alto = saturate(dir.y);
                half3 cielo = lerp(_ColorHorizonte.rgb, _ColorCenit.rgb, pow(alto, 0.55h));
                // Bajo el horizonte, un tono algo más oscuro (bruma sobre el mar y las colinas).
                cielo = lerp(cielo, _ColorHorizonte.rgb * 0.85h, saturate(-dir.y * 6.0h));

                // Sol: disco y halo.
                float3 sol = normalize(_DireccionSol.xyz);
                half d = saturate(dot(dir, sol));
                cielo += _ColorSol.rgb * (pow(d, 900.0h) * 1.2h + pow(d, 24.0h) * 0.18h) * (1.0h - _Noche * 0.6h);

                // Nubes en un plano alto: más grandes encima, se pierden hacia el horizonte.
                float2 uv = dir.xz / (dir.y + 0.12) * _EscalaNubes + _Time.y * _VelocidadNubes;
                half nube = SAMPLE_TEXTURE2D(_Nubes, sampler_Nubes, uv).a * smoothstep(0.02h, 0.25h, dir.y);
                half3 colorNube = lerp(half3(0.82h, 0.85h, 0.9h), half3(1.0h, 0.97h, 0.92h), pow(d, 4.0h));
                cielo = lerp(cielo, colorNube, nube * 0.85h * (1.0h - _Noche));

                cielo = lerp(cielo, _ColorEspacio.rgb, _Noche);
                return half4(cielo, 1.0h);
            }
            ENDHLSL
        }
    }
}
