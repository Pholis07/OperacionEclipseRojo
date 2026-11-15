// Llama de un motor: cono con UV (v = 0 en la tobera, 1 en la punta). Núcleo blanco, envoltura naranja que se
// apaga hacia la punta, «diamantes» de choque cerca de la tobera y ruido que sube y parpadea. Aditivo, sin
// luces; el Bloom la hace brillar. _Potencia (0–1) la enciende o apaga (la anima DirectorCinematica).
Shader "OperacionEclipseRojo/Llama"
{
    Properties
    {
        [HDR] _ColorNucleo ("Núcleo", Color) = (6, 4.2, 2, 1)
        [HDR] _ColorBorde ("Borde", Color) = (5, 1.5, 0.22, 1)
        _Potencia ("Potencia", Range(0, 1)) = 1
        _Diamantes ("Diamantes de choque", Range(0, 1)) = 0.6
        _Velocidad ("Velocidad del ruido", Float) = 9
        _Semilla ("Semilla", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorNucleo;
                half4 _ColorBorde;
                half _Potencia;
                half _Diamantes;
                float _Velocidad;
                float _Semilla;
            CBUFFER_END

            struct Entrada { float4 posicion : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Salida { float4 posicion : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 vista : TEXCOORD2; float semilla : TEXCOORD3; };

            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Ruido(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Salida vert(Entrada e)
            {
                Salida s;
                float3 p = e.posicion.xyz;
                // Ondula un poco el contorno (más hacia la punta).
                float3 origen = TransformObjectToWorld(float3(0, 0, 0));
                s.semilla = _Semilla + origen.x * 3.7 + origen.z * 1.3;
                float t = _Time.y * _Velocidad + s.semilla * 13.0;
                p.xz *= 1.0 + (Ruido(float2(e.uv.x * 6.0, e.uv.y * 4.0 - t)) - 0.5) * 0.35 * e.uv.y;
                float3 mundo = TransformObjectToWorld(p);
                s.posicion = TransformWorldToHClip(mundo);
                s.uv = e.uv;
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.vista = GetWorldSpaceViewDir(mundo);
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                float v = s.uv.y;
                float t = _Time.y * _Velocidad + s.semilla * 13.0;
                half n = Ruido(float2(s.uv.x * 8.0, v * 6.0 - t)) * 0.6 + Ruido(float2(s.uv.x * 19.0, v * 14.0 - t * 1.7)) * 0.4;
                // De frente (centro del cono) el núcleo; en el contorno, el borde naranja.
                half frente = saturate(dot(normalize(s.normal), normalize(s.vista)));
                frente = abs(frente);
                half largo = saturate(1.0h - v);
                half3 color = lerp(_ColorBorde.rgb, _ColorNucleo.rgb, pow(frente, 1.5h) * pow(largo, 1.5h));
                half diamantes = (0.5h + 0.5h * cos(v * 38.0h)) * smoothstep(0.55h, 0.0h, v) * _Diamantes;
                half alfa = pow(largo, 1.4h) * (0.55h + 0.65h * n) * (0.35h + 0.65h * frente) + diamantes * 0.35h * frente;
                half parpadeo = 0.88h + 0.12h * Ruido(float2(t * 2.3, s.semilla));
                return half4(color * alfa * _Potencia * parpadeo, 1.0h);
            }
            ENDHLSL
        }
    }
}
