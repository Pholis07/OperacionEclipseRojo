// Eclipse Ink (versión 8): cel shading selectivo para la nave, los instrumentos, el cohete y la plataforma.
// - Luz en 3 bandas (sombra, medio tono, luz) con una transición mínima para que no haya dientes.
// - Contorno de tinta por malla invertida: una pasada extra que dibuja las caras de atrás un poco más grandes.
//   No necesita textura de profundidad ni renderer features (el WebGL usa la calidad «Mobile», que no los tiene).
// Los planetas, las órbitas, las partículas y la interfaz NO usan este shader.
// Mismos nombres de propiedades que URP/Lit (_BaseMap, _BaseColor, _BumpMap, _EmissionColor, _Cull…): cambiar un
// material de URP/Lit a Tinta, o al revés, conserva sus colores y texturas.
Shader "OperacionEclipseRojo/Tinta"
{
    Properties
    {
        [MainTexture] _BaseMap ("Textura", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Recorte alfa", Range(0, 1)) = 0.5
        [Normal] _BumpMap ("Normales", 2D) = "bump" {}
        _BumpScale ("Fuerza del relieve", Float) = 1
        [HDR] _EmissionColor ("Emisión", Color) = (0, 0, 0, 1)
        _Metallic ("Metálico (tiñe el brillo)", Range(0, 1)) = 0
        _Smoothness ("Suavidad (tamaño del brillo)", Range(0, 1)) = 0.5

        [Header(Bandas)]
        _ColorSombra ("Relleno frío de la sombra", Color) = (0.196, 0.29, 0.341, 1)
        _Relleno ("Fuerza del relleno", Range(0, 2)) = 0.6
        _NivelSombra ("Nivel de la sombra", Range(0, 1)) = 0.3
        _NivelMedio ("Nivel del medio tono", Range(0, 1)) = 0.62
        _UmbralSombra ("Umbral sombra / medio", Range(-1, 1)) = 0.02
        _UmbralLuz ("Umbral medio / luz", Range(-1, 1)) = 0.48
        _Transicion ("Transición entre bandas", Range(0.001, 0.3)) = 0.025
        _FuerzaBrillo ("Fuerza del brillo técnico", Range(0, 2)) = 0.6
        [HDR] _ColorBorde ("Luz de borde", Color) = (0.38, 0.84, 0.9, 1)
        _FuerzaBorde ("Fuerza de la luz de borde", Range(0, 1)) = 0.22

        [Header(Contorno)]
        _Contorno ("Grosor (px a 1080p; 0 = sin contorno)", Range(0, 6)) = 1.5
        _ContornoMaximo ("Grosor máximo (unidades del mundo)", Range(0, 2)) = 0.02
        _ColorContorno ("Tinta", Color) = (0.07, 0.1, 0.145, 1)

        [HideInInspector] _Cull ("Caras", Float) = 2
        [HideInInspector] _AlphaClip ("Recorte", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _BumpScale;
            half4 _EmissionColor;
            half _Metallic;
            half _Smoothness;
            half4 _ColorSombra;
            half _Relleno;
            half _NivelSombra;
            half _NivelMedio;
            half _UmbralSombra;
            half _UmbralLuz;
            half _Transicion;
            half _FuerzaBrillo;
            half4 _ColorBorde;
            half _FuerzaBorde;
            half _Contorno;
            half _ContornoMaximo;
            half4 _ColorContorno;
            half _Cull;
            half _AlphaClip;
        CBUFFER_END

        // Global: 1 apaga todos los contornos (modo ligero, Opciones). Por defecto vale 0: contornos encendidos.
        float _TintaSinContorno;

        void Recortar(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a - _Cutoff);
            #endif
        }
        ENDHLSL

        // ---------- Luz en bandas ----------
        Pass
        {
            Name "Tinta"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                half niebla : TEXCOORD4;
            };

            Salida vert(Entrada e)
            {
                Salida s;
                s.mundo = TransformObjectToWorld(e.posicion.xyz);
                s.posicion = TransformWorldToHClip(s.mundo);
                s.uv = TRANSFORM_TEX(e.uv, _BaseMap);
                s.normal = TransformObjectToWorldNormal(e.normal);
                s.tangente = float4(TransformObjectToWorldDir(e.tangente.xyz), e.tangente.w * GetOddNegativeScale());
                s.niebla = ComputeFogFactor(s.posicion.z);
                return s;
            }

            // Escalón suave: 0 por debajo del umbral, 1 por encima.
            half Banda(half valor, half umbral)
            {
                return smoothstep(umbral - _Transicion, umbral + _Transicion, valor);
            }

            half4 frag(Salida s, FRONT_FACE_TYPE frente : FRONT_FACE_SEMANTIC) : SV_Target
            {
                Recortar(s.uv);
                half4 textura = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, s.uv);
                half3 albedo = textura.rgb * _BaseColor.rgb;

                half3 n = normalize(s.normal);
                #if defined(_NORMALMAP)
                half3 nt = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, s.uv), _BumpScale);
                half3 t = normalize(s.tangente.xyz);
                half3 b = cross(n, t) * s.tangente.w;
                n = normalize(nt.x * t + nt.y * b + nt.z * n);
                #endif
                // Piezas de dos caras (cofias): la cara de dentro se ilumina con la normal dada la vuelta.
                n = IS_FRONT_VFACE(frente, n, -n);

                half3 vista = normalize(GetWorldSpaceViewDir(s.mundo));

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                Light principal = GetMainLight(TransformWorldToShadowCoord(s.mundo));
                #else
                Light principal = GetMainLight();
                #endif

                // Tres bandas: la sombra proyectada manda la superficie a la banda de sombra.
                half nl = dot(n, principal.direction);
                half valor = min(nl, lerp(-1.0h, 1.0h, smoothstep(0.35h, 0.65h, principal.shadowAttenuation)));
                half medio = Banda(valor, _UmbralSombra);
                half luz = Banda(valor, _UmbralLuz);
                half tono = lerp(lerp(_NivelSombra, _NivelMedio, medio), 1.0h, luz);
                half3 color = albedo * principal.color * tono;
                // Relleno frío en la sombra: las piezas negras no se pierden contra el espacio.
                color += _ColorSombra.rgb * _Relleno * (1.0h - 0.6h * medio) * (1.0h - luz);
                // Lo que emite luz (lentes, pilotos) se ve por su emisión, no quemado por la luz del Sol.
                color *= 1.0h - 0.7h * saturate(max(_EmissionColor.r, max(_EmissionColor.g, _EmissionColor.b)));

                // Brillo técnico: una mancha pequeña y dura, solo en la zona de luz.
                half3 mitad = normalize(principal.direction + vista);
                half exponente = exp2(3.0h + _Smoothness * 7.0h);
                half brillo = Banda(pow(saturate(dot(n, mitad)), exponente), 0.5h) * luz;
                color += principal.color * lerp(1.0h, albedo, _Metallic) * brillo * _FuerzaBrillo * (0.15h + 0.85h * _Metallic) * 0.5h;

                // Luces extra (la luz de contorno azul, el fuego de los motores): una banda cada una.
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = s.mundo;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(s.posicion);
                uint cuantas = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                for (uint d = 0u; d < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); d++)
                {
                    Light direccional = GetAdditionalLight(d, s.mundo);
                    color += albedo * direccional.color * Banda(dot(n, direccional.direction), 0.3h) * 0.45h;
                }
                #endif
                LIGHT_LOOP_BEGIN(cuantas)
                    Light extra = GetAdditionalLight(lightIndex, s.mundo);
                    color += albedo * extra.color * Banda(dot(n, extra.direction) * saturate(extra.distanceAttenuation * 4.0h), 0.3h)
                             * min(1.0h, extra.distanceAttenuation) * 0.45h;
                LIGHT_LOOP_END
                #endif

                // Luz de borde: fina, en el lado iluminado y en el contraluz; nunca un neón continuo.
                half fresnel = 1.0h - saturate(dot(n, vista));
                half borde = smoothstep(0.62h, 0.8h, fresnel) * (0.35h + 0.65h * medio);
                color += _ColorBorde.rgb * borde * _FuerzaBorde;

                color += _EmissionColor.rgb;
                color = MixFog(color, s.niebla);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ---------- Contorno de tinta (malla invertida) ----------
        Pass
        {
            Name "Contorno"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fog

            struct Entrada
            {
                float4 posicion : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 suave : TEXCOORD2;   // normal suavizada en octaedro (ImportadorContornos / GuardarMalla)
            };

            struct Salida
            {
                float4 posicion : SV_POSITION;
                float2 uv : TEXCOORD0;
                half niebla : TEXCOORD1;
            };

            float3 NormalSuave(float2 uv, float3 normal)
            {
                // Sin el canal (malla sin preparar) llega (0, 0): se usa la normal de la malla.
                if (uv.x + uv.y < 0.02) return normal;
                float2 f = (uv - 0.05) / 0.9 * 2.0 - 1.0;
                float3 n = float3(f.x, f.y, 1.0 - abs(f.x) - abs(f.y));
                float t = saturate(-n.z);
                n.x += n.x >= 0.0 ? -t : t;
                n.y += n.y >= 0.0 ? -t : t;
                return normalize(n);
            }

            Salida vert(Entrada e)
            {
                Salida s;
                s.uv = TRANSFORM_TEX(e.uv, _BaseMap);
                float4 clip = TransformObjectToHClip(e.posicion.xyz);
                s.niebla = ComputeFogFactor(clip.z);
                float grosor = _Contorno * (1.0 - saturate(_TintaSinContorno));
                if (grosor <= 0.001)
                {
                    // Sin contorno: todos los vértices al mismo punto, no se dibuja nada.
                    s.posicion = float4(0.0, 0.0, 0.0, 0.0);
                    return s;
                }
                float3 normalMundo = TransformObjectToWorldNormal(NormalSuave(e.suave, e.normal));
                float2 direccion = mul((float3x3)UNITY_MATRIX_VP, normalMundo).xy;
                float largo = length(direccion);
                direccion = largo > 1e-5 ? direccion / largo : float2(0.0, 0.0);
                // Grosor constante en píxeles (a 1080 de alto), con tope para que de lejos no se coma el objeto.
                float2 pixel = 2.0 / _ScreenParams.xy;
                float px = grosor * _ScreenParams.y / 1080.0;
                float mundoPorPixel = clip.w * 2.0 / (abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y);
                px = min(px, _ContornoMaximo / max(mundoPorPixel, 1e-6));
                clip.xy += direccion * pixel * px * clip.w;
                s.posicion = clip;
                return s;
            }

            half4 frag(Salida s) : SV_Target
            {
                Recortar(s.uv);
                return half4(MixFog(_ColorContorno.rgb, s.niebla), 1.0h);
            }
            ENDHLSL
        }

        // ---------- Sombras y profundidad ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Salida { float4 posicion : SV_POSITION; float2 uv : TEXCOORD0; };

            Salida vert(float4 posicion : POSITION, float3 normal : NORMAL, float2 uv : TEXCOORD0)
            {
                Salida s;
                float3 mundo = TransformObjectToWorld(posicion.xyz);
                float3 normalMundo = TransformObjectToWorldNormal(normal);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 luz = normalize(_LightPosition - mundo);
                #else
                float3 luz = _LightDirection;
                #endif
                s.posicion = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(mundo, normalMundo, luz)));
                s.uv = TRANSFORM_TEX(uv, _BaseMap);
                return s;
            }

            half4 frag(Salida s) : SV_Target { Recortar(s.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            struct Salida { float4 posicion : SV_POSITION; float2 uv : TEXCOORD0; };

            Salida vert(float4 posicion : POSITION, float2 uv : TEXCOORD0)
            {
                Salida s;
                s.posicion = TransformObjectToHClip(posicion.xyz);
                s.uv = TRANSFORM_TEX(uv, _BaseMap);
                return s;
            }

            half4 frag(Salida s) : SV_Target { Recortar(s.uv); return s.posicion.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            struct Salida { float4 posicion : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };

            Salida vert(float4 posicion : POSITION, float3 normal : NORMAL, float2 uv : TEXCOORD0)
            {
                Salida s;
                s.posicion = TransformObjectToHClip(posicion.xyz);
                s.uv = TRANSFORM_TEX(uv, _BaseMap);
                s.normal = TransformObjectToWorldNormal(normal);
                return s;
            }

            half4 frag(Salida s) : SV_Target { Recortar(s.uv); return half4(normalize(s.normal), 0.0h); }
            ENDHLSL
        }
    }
    FallBack Off
}
