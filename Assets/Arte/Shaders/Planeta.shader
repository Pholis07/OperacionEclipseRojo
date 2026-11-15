// Superficie de Marte en alta resolución: el mapa de 8K va en dos mitades de 4096×4096 (el límite de
// WebGL) y aquí se elige la mitad según la longitud. Añade relieve (mapa de normales), una textura de detalle
// que solo se nota de cerca, un terminador suave (la noche no corta en seco) y un borde de polvo anaranjado
// en el lado iluminado. Luz principal + luces adicionales (la de contorno) + ambiente. URP, sin sombras.
// Versión 6: capa de nubes de hielo de agua que se mueve (textura propia), escarcha con brillo en los casquetes
// polares (realza el hielo que ya trae el mapa) y crepúsculo azulado en el terminador, como los atardeceres de Marte.
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
        _Nubes ("Nubes (alfa, equirectangular)", 2D) = "black" {}
        _FuerzaNubes ("Fuerza de las nubes", Range(0, 1)) = 0
        _VelocidadNubes ("Velocidad de las nubes (vueltas por segundo)", Float) = 0.002
        _ColorNubes ("Color de las nubes", Color) = (0.93, 0.95, 1.0, 1)
        _Escarcha ("Escarcha de los casquetes", Range(0, 1)) = 0
        _BrilloHielo ("Brillo del hielo", Range(0, 2)) = 0
        [HDR] _ColorCrepusculo ("Crepúsculo (terminador)", Color) = (0.25, 0.45, 0.9, 1)
        _FuerzaCrepusculo ("Fuerza del crepúsculo", Range(0, 1)) = 0
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
            TEXTURE2D(_Nubes); SAMPLER(sampler_Nubes);

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
                float4 _Nubes_ST;
                half _FuerzaNubes;
                float _VelocidadNubes;
                half4 _ColorNubes;
                half _Escarcha;
                half _BrilloHielo;
                half4 _ColorCrepusculo;
                half _FuerzaCrepusculo;
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

                // Escarcha: en latitudes altas, lo que ya es claro en el mapa (el hielo de los casquetes) se vuelve
                // blanco azulado. No inventa casquetes: solo realza los que trae la textura.
                half latitud = abs(s.uv.y - 0.5h) * 2.0h;
                half luma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                half hielo = smoothstep(0.78h, 0.9h, latitud) * smoothstep(0.32h, 0.6h, luma) * _Escarcha;
                albedo = lerp(albedo, half3(0.92h, 0.95h, 1.0h), hielo);

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

                half3 vista = normalize(GetWorldSpaceViewDir(s.mundo));
                // Brillo del hielo: reflejo especular suave solo en la escarcha y del lado del Sol.
                half3 medio = normalize(principal.direction + vista);
                color += principal.color * pow(saturate(dot(n, medio)), 48.0h) * hielo * _BrilloHielo * smoothstep(0.0h, 0.3h, luzGeo);

                // Nubes de hielo de agua: finas, iluminadas por el Sol, se mueven despacio de oeste a este.
                float2 uvNubes = float2(s.uv.x + _Time.y * _VelocidadNubes, s.uv.y);
                half nube = SAMPLE_TEXTURE2D(_Nubes, sampler_Nubes, uvNubes).a * _FuerzaNubes;
                half3 colorNube = _ColorNubes.rgb * (principal.color * luzGeo + _Ambiente.rgb);
                // Solo del lado iluminado: en la sombra se veían como vetas grises.
                color = lerp(color, colorNube, nube * smoothstep(0.1h, 0.5h, luzGeo));

                // Borde de polvo del lado iluminado (la atmósfera fina dispersa la luz en el contorno).
                half borde = pow(1.0h - saturate(dot(nGeo, vista)), _PotenciaBorde);
                color += _ColorBorde.rgb * borde * saturate(luzGeo * 1.4h) * 0.35h;
                // Crepúsculo azulado: en Marte el polvo dispersa el azul hacia delante, cerca del terminador.
                half terminador = smoothstep(0.02h, 0.2h, luzGeo) * (1.0h - smoothstep(0.2h, 0.55h, luzGeo));
                color += _ColorCrepusculo.rgb * terminador * (0.35h + borde) * _FuerzaCrepusculo;

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
