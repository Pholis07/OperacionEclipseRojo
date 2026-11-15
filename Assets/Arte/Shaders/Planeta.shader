// Superficie de Marte en alta resolución: el mapa de 8K va en dos mitades de 4096×4096 (el límite de
// WebGL) y aquí se elige la mitad según la longitud. Añade relieve (mapa de normales), una textura de detalle
// que solo se nota de cerca, un terminador suave (la noche no corta en seco) y un borde de polvo anaranjado
// en el lado iluminado. Luz principal + luces adicionales (la de contorno) + ambiente. URP, sin sombras.
Shader "OperacionEclipseRojo/Planeta"
{
    Properties
    {
        _MapaOeste ("Mapa (longitud 0-180)", 2D) = "white" {}
        _MapaEste ("Mapa (longitud 180-360)", 2D) = "white" {}
        [Normal] _Normales ("Normales", 2D) = "bump" {}
        _FuerzaNormales ("Fuerza del relieve", Range(0, 2)) = 1
        _Detalle ("Detalle (gris medio)", 2D) = "gray" {}
        _RepeticionDetalle ("Repetición del detalle", Float) = 48
        _FuerzaDetalle ("Fuerza del detalle", Range(0, 1)) = 0.35
        _Suavidad ("Terminador suave", Range(0, 1)) = 0.35
        [HDR] _ColorBorde ("Borde de polvo", Color) = (1.0, 0.5, 0.25, 1)
        _PotenciaBorde ("Potencia del borde", Range(0.5, 8)) = 3.5
        _Brillo ("Brillo", Range(0.5, 2)) = 1.08
        _Ambiente ("Luz ambiente", Color) = (0.05, 0.055, 0.075, 1)
        _Saturacion ("Saturación", Range(0.5, 2)) = 1.15
        _Contraste ("Contraste", Range(0.5, 2)) = 1.15
        _Gamma ("Gamma del color", Range(0.6, 1.6)) = 1.12
        [Normal] _ParcheNormal ("Relieve de la región (Gale)", 2D) = "bump" {}
        _RectParche ("Región en el mapa (x, y, ancho, alto)", Vector) = (0.8728, 0.452, 0.0205, 0.041)
        _FuerzaParche ("Fuerza del relieve de la región", Range(0, 3)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Planeta"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MapaOeste); SAMPLER(sampler_MapaOeste);
            TEXTURE2D(_MapaEste); SAMPLER(sampler_MapaEste);
            TEXTURE2D(_Normales); SAMPLER(sampler_Normales);
            TEXTURE2D(_Detalle); SAMPLER(sampler_Detalle);
            TEXTURE2D(_ParcheNormal); SAMPLER(sampler_ParcheNormal);

            CBUFFER_START(UnityPerMaterial)
                float4 _MapaOeste_ST;
                float4 _MapaEste_ST;
                float4 _Normales_ST;
                float4 _Detalle_ST;
                half _FuerzaNormales;
                float _RepeticionDetalle;
                half _FuerzaDetalle;
                half _Suavidad;
                half4 _ColorBorde;
                half _PotenciaBorde;
                half _Brillo;
                half4 _Ambiente;
                half _Saturacion;
                half _Contraste;
                half _Gamma;
                float4 _ParcheNormal_ST;
                float4 _RectParche;
                half _FuerzaParche;
            CBUFFER_END

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                float4 tangente : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 tangente : TEXCOORD2;
                float3 mundo : TEXCOORD3;
            };

            Salida vert(Entrada e)
            {
                Salida s;
                s.mundo = TransformObjectToWorld(e.posicion.xyz);
                s.posicion = TransformWorldToHClip(s.mundo);
                s.uv = e.uv;
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.tangente = float4(TransformObjectToWorldDir(e.tangente.xyz), e.tangente.w * GetOddNegativeScale());
                return s;
            }

            // Lambert con «envoltura»: la luz pasa un poco al lado de noche y el terminador no corta en seco.
            half Difusa(half3 n, half3 l)
            {
                half nl = dot(n, l);
                return saturate((nl + _Suavidad) / (1.0h + _Suavidad));
            }

            half4 frag(Salida s) : SV_Target
            {
                // Las dos mitades se muestrean siempre (derivadas continuas en la unión) y se elige con step.
                float2 uvOeste = float2(s.uv.x * 2.0, s.uv.y);
                float2 uvEste = float2(s.uv.x * 2.0 - 1.0, s.uv.y);
                half3 oeste = SAMPLE_TEXTURE2D(_MapaOeste, sampler_MapaOeste, uvOeste).rgb;
                half3 este = SAMPLE_TEXTURE2D(_MapaEste, sampler_MapaEste, uvEste).rgb;
                half3 albedo = lerp(oeste, este, step(0.5, s.uv.x));
                half detalle = SAMPLE_TEXTURE2D(_Detalle, sampler_Detalle, s.uv * float2(2.0, 1.0) * _RepeticionDetalle).r;
                albedo *= lerp(1.0h, detalle * 2.0h, _FuerzaDetalle);
                half gris = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = max(0.0h, lerp(gris.xxx, albedo, _Saturacion));
                // Menos lavado: gamma (tonos medios más profundos) y contraste alrededor del gris medio.
                albedo = pow(albedo, _Gamma);
                albedo = saturate((albedo - 0.18h) * _Contraste + 0.18h);

                half3 nt = UnpackNormalScale(SAMPLE_TEXTURE2D(_Normales, sampler_Normales, s.uv), _FuerzaNormales);
                // Relieve del cráter Gale (modelo de NASA) en su sitio, con el borde fundido.
                float2 uvParche = (s.uv - _RectParche.xy) / _RectParche.zw;
                half3 np = UnpackNormalScale(SAMPLE_TEXTURE2D(_ParcheNormal, sampler_ParcheNormal, saturate(uvParche)), _FuerzaParche);
                float2 bordeParche = smoothstep(0.0, 0.12, uvParche) * smoothstep(0.0, 0.12, 1.0 - uvParche);
                half pesoParche = bordeParche.x * bordeParche.y;
                nt = normalize(lerp(nt, half3(nt.xy + np.xy, nt.z), pesoParche));
                half3 nGeo = normalize(s.normal);
                half3 t = normalize(s.tangente.xyz);
                half3 b = cross(nGeo, t) * s.tangente.w;
                half3 n = normalize(nt.x * t + nt.y * b + nt.z * nGeo);

                Light principal = GetMainLight();
                half luzGeo = Difusa(nGeo, principal.direction);
                // El relieve solo modula donde hay luz: de noche no aparecen cráteres iluminados.
                half3 color = albedo * principal.color * Difusa(n, principal.direction) * smoothstep(0.0h, 0.25h, luzGeo);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                uint cuantas = GetAdditionalLightsCount();
                for (uint i = 0u; i < cuantas; ++i)
                {
                    Light extra = GetAdditionalLight(i, s.mundo);
                    color += albedo * extra.color * extra.distanceAttenuation * saturate(dot(n, extra.direction)) * 0.6h;
                }
                #endif

                color += albedo * _Ambiente.rgb;   // ambiente fijo: igual en el editor, en batch y en el navegador

                // Borde de polvo del lado iluminado (la atmósfera fina dispersa la luz en el contorno).
                half3 vista = normalize(GetWorldSpaceViewDir(s.mundo));
                half borde = pow(1.0h - saturate(dot(nGeo, vista)), _PotenciaBorde);
                color += _ColorBorde.rgb * borde * saturate(luzGeo * 1.4h) * 0.35h;

                return half4(color * _Brillo, 1.0h);
            }
            ENDHLSL
        }

        // Sin sombras propias pero sí profundidad (el posprocesado y la cámara la usan).
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertProfundidad
            #pragma fragment fragProfundidad
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vertProfundidad(float4 posicion : POSITION) : SV_POSITION { return TransformObjectToHClip(posicion.xyz); }
            half4 fragProfundidad() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
