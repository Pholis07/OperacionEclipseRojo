using SpaceMission;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renovación visual: lo que hace físico el montaje de cada instrumento. Geometría original con primitivas, en el
/// espacio del módulo (Y sale del casco; la base del instrumento en y = 0; su frente hacia +Z):
/// - para todos: dos fijaciones que se cierran sobre la base, un conector con su cable y un indicador de conexión;
/// - para cada uno, su mecanismo con pivote propio (MecanismoInstrumento): plegado en el hangar, desplegado en órbita.
///   Radar: dos botalones de dos tramos. Alta resolución: parasol que se extiende y tapa. Térmica: visera y tapa.
///   Atmosférico: mástil con cabezal. Espectrómetro: radiador lateral y obturador.
/// Los modelos del kit son mallas de una pieza: no se finge moverlos; lo que se mueve son estas piezas añadidas.
/// </summary>
public static partial class ConstructorEscenas
{
    static MecanismoInstrumento.Pieza PiezaMovil(Transform pivote, Vector3 posPlegada, Vector3 posAbierta, Vector3 giroPlegado, Vector3 giroAbierto, float desde, float hasta) =>
        new MecanismoInstrumento.Pieza
        {
            pivote = pivote, posicionPlegada = posPlegada, posicionAbierta = posAbierta, giroPlegado = giroPlegado, giroAbierto = giroAbierto, desde = desde, hasta = hasta,
        };

    static Transform Pivote(string nombre, Transform padre, Vector3 posicion)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = posicion;
        return t;
    }

    static GameObject SinSombra(GameObject go)
    {
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    /// <summary>Fijaciones, conector, indicador y mecanismo del módulo «m» (su instrumento es «equipo»).</summary>
    static void MontajeModulo(Transform m, GameObject equipo, string id, Instalacion instalacion, bool animar)
    {
        var plata = CrearMaterial("Nave_Plata", Hex("#C9CED6"), 0.85f, 0.7f);
        var oscuro = CrearMaterial("Nave_Placa", Hex("#3A3F47"), 0.75f, 0.55f);
        var claro = CrearMaterial("Instrumento_Proteccion", Hex("#D9DFDC"), 0.2f, 0.45f);
        var ambar = CrearMaterial("Instrumento_Manta", Hex("#BC7B36"), 0.7f, 0.5f);
        var luz = CrearMaterial("Instrumento_Indicador", Hex("#0E2A2E"), 0f, 0.6f, null, Vector2.one, Hex("#61C7D2") * 3.2f);
        var b = Limites(equipo);
        float hx = b.extents.x, hz = b.extents.z, alto = b.size.y;

        // Fijaciones: una a cada lado de la base; se abren hacia fuera (Instalacion.Abrir).
        var garras = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            int lado = i == 0 ? -1 : 1;
            garras[i] = Pivote("Fijacion_" + i, m, new Vector3(lado * (hx + 0.035f), 0f, 0f));
            SinSombra(Pieza(PrimitiveType.Cube, "Garra", garras[i], new Vector3(0f, 0.09f, 0f), Vector3.zero, new Vector3(0.05f, 0.18f, Mathf.Min(0.34f, hz * 1.2f)), oscuro));
            SinSombra(Pieza(PrimitiveType.Cube, "Una", garras[i], new Vector3(-lado * 0.035f, 0.165f, 0f), Vector3.zero, new Vector3(0.09f, 0.035f, Mathf.Min(0.3f, hz * 1.1f)), plata));
        }
        // Conector al bus y su cable hasta el casco; indicador de conexión al frente.
        SinSombra(Pieza(PrimitiveType.Cube, "Conector", m, new Vector3(-hx * 0.45f, 0.045f, -hz - 0.035f), Vector3.zero, new Vector3(0.16f, 0.09f, 0.07f), oscuro));
        SinSombra(Pieza(PrimitiveType.Cylinder, "Cable", m, new Vector3(-hx * 0.45f, -0.04f, -hz - 0.05f), Vector3.zero, new Vector3(0.035f, 0.07f, 0.035f), ambar));
        var indicador = SinSombra(Pieza(PrimitiveType.Sphere, "Indicador", m, new Vector3(hx * 0.6f, 0.05f, hz + 0.02f), Vector3.zero, Vector3.one * 0.07f, luz));
        instalacion.Fijar(garras, indicador, animar);

        var piezas = new List<MecanismoInstrumento.Pieza>();
        switch (id)
        {
            case "radar":
                // Dos botalones de dos tramos: plegados hacia arriba junto al equipo; abiertos, una antena horizontal larga.
                for (int i = 0; i < 2; i++)
                {
                    int lado = i == 0 ? -1 : 1;
                    var raiz = new Vector3(lado * (hx + 0.03f), alto * 0.5f, -hz * 0.3f);
                    var interior = Pivote("Botalon_" + i, m, raiz);
                    SinSombra(Pieza(PrimitiveType.Cube, "Tramo", interior, new Vector3(lado * 0.36f, 0f, 0f), Vector3.zero, new Vector3(0.72f, 0.035f, 0.035f), plata));
                    SinSombra(Pieza(PrimitiveType.Cube, "Bisagra", interior, Vector3.zero, Vector3.zero, new Vector3(0.07f, 0.08f, 0.08f), oscuro));
                    var codo = new Vector3(lado * 0.72f, 0f, 0f);
                    var exterior = Pivote("Extremo", interior, codo);
                    SinSombra(Pieza(PrimitiveType.Cube, "Tramo", exterior, new Vector3(lado * 0.34f, 0f, 0f), Vector3.zero, new Vector3(0.68f, 0.028f, 0.028f), plata));
                    SinSombra(Pieza(PrimitiveType.Cube, "Punta", exterior, new Vector3(lado * 0.68f, 0f, 0f), Vector3.zero, new Vector3(0.04f, 0.06f, 0.12f), ambar));
                    float t0 = i * 0.06f;
                    piezas.Add(PiezaMovil(interior, raiz, raiz, new Vector3(0f, 0f, lado * 86f), Vector3.zero, t0, t0 + 0.42f));
                    piezas.Add(PiezaMovil(exterior, codo, codo, new Vector3(0f, 0f, -lado * 172f), Vector3.zero, 0.52f + t0, 0.94f + t0));
                }
                break;
            case "alta_res":
            {
                // Parasol que se extiende por delante del tubo óptico y tapa que se abre hacia arriba.
                float eje = alto * 0.55f, radio = Mathf.Min(hx, alto * 0.5f) * 0.52f;
                var dentro = new Vector3(0f, eje, hz - 0.2f); var afuera = new Vector3(0f, eje, hz + 0.08f);
                var parasol = Pivote("Parasol", m, dentro);
                SinSombra(Pieza(PrimitiveType.Cylinder, "Tubo", parasol, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(radio * 2f, 0.15f, radio * 2f), oscuro));
                SinSombra(Pieza(PrimitiveType.Cylinder, "Aro", parasol, new Vector3(0f, 0f, 0.15f), new Vector3(90f, 0f, 0f), new Vector3(radio * 2.16f, 0.018f, radio * 2.16f), plata));
                var bisagra = new Vector3(0f, radio * 1.05f, 0.17f);
                var tapa = Pivote("Tapa", parasol, bisagra);
                SinSombra(Pieza(PrimitiveType.Cylinder, "Disco", tapa, new Vector3(0f, -radio * 1.05f, 0f), new Vector3(90f, 0f, 0f), new Vector3(radio * 2.1f, 0.012f, radio * 2.1f), claro));
                piezas.Add(PiezaMovil(parasol, dentro, afuera, Vector3.zero, Vector3.zero, 0f, 0.5f));
                piezas.Add(PiezaMovil(tapa, bisagra, bisagra, Vector3.zero, new Vector3(-112f, 0f, 0f), 0.6f, 1f));
                break;
            }
            case "termica":
            {
                // Visera térmica corta que avanza sobre la óptica y tapa que se aparta hacia un lado.
                float eje = alto * 0.5f, radio = Mathf.Min(hx, alto * 0.5f) * 0.45f;
                var atras = new Vector3(0f, alto + 0.02f, hz - 0.3f); var delante = new Vector3(0f, alto + 0.02f, hz + 0.02f);
                var visera = Pivote("Visera", m, atras);
                SinSombra(Pieza(PrimitiveType.Cube, "Placa", visera, Vector3.zero, Vector3.zero, new Vector3(hx * 1.7f, 0.03f, 0.34f), ambar));
                foreach (int lado in new[] { -1, 1 })
                    SinSombra(Pieza(PrimitiveType.Cube, "Aleta", visera, new Vector3(lado * hx * 0.85f, -0.07f, 0.02f), Vector3.zero, new Vector3(0.03f, 0.14f, 0.3f), ambar));
                var bisagra = new Vector3(-radio * 1.1f, eje, hz + 0.02f);
                var tapa = Pivote("Tapa", m, bisagra);
                SinSombra(Pieza(PrimitiveType.Cylinder, "Disco", tapa, new Vector3(radio * 1.1f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(radio * 2f, 0.012f, radio * 2f), claro));
                piezas.Add(PiezaMovil(visera, atras, delante, Vector3.zero, Vector3.zero, 0f, 0.5f));
                piezas.Add(PiezaMovil(tapa, bisagra, bisagra, Vector3.zero, new Vector3(0f, 118f, 0f), 0.55f, 1f));
                break;
            }
            case "atmosferico":
            {
                // Mástil corto que sale del módulo con el cabezal de teledetección, que gira hacia el limbo.
                var dentro = new Vector3(-hx * 0.55f, alto * 0.35f, hz * 0.4f); var afuera = new Vector3(-hx * 0.55f, alto * 0.9f, hz * 0.4f);
                var mastil = Pivote("Mastil", m, dentro);
                SinSombra(Pieza(PrimitiveType.Cylinder, "Tubo", mastil, new Vector3(0f, 0.2f, 0f), Vector3.zero, new Vector3(0.04f, 0.2f, 0.04f), plata));
                var punta = new Vector3(0f, 0.42f, 0f);
                var cabezal = Pivote("Cabezal", mastil, punta);
                SinSombra(Pieza(PrimitiveType.Cube, "Caja", cabezal, Vector3.zero, Vector3.zero, new Vector3(0.14f, 0.09f, 0.18f), claro));
                SinSombra(Pieza(PrimitiveType.Cylinder, "Ojo", cabezal, new Vector3(0f, 0f, 0.1f), new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.02f, 0.07f), oscuro));
                piezas.Add(PiezaMovil(mastil, dentro, afuera, Vector3.zero, Vector3.zero, 0f, 0.6f));
                piezas.Add(PiezaMovil(cabezal, punta, punta, new Vector3(0f, -90f, 0f), new Vector3(28f, 0f, 0f), 0.65f, 1f));
                break;
            }
            case "espectrometro":
            {
                // Radiador lateral que se abre desde el costado y obturador que descubre la rendija.
                var bisagra = new Vector3(hx + 0.02f, alto * 0.12f, 0f);
                var radiador = Pivote("Radiador", m, bisagra);
                SinSombra(Pieza(PrimitiveType.Cube, "Panel", radiador, new Vector3(0.015f, alto * 0.4f, 0f), Vector3.zero, new Vector3(0.025f, alto * 0.8f, hz * 1.5f), claro));
                for (int k = -1; k <= 1; k++)
                    SinSombra(Pieza(PrimitiveType.Cube, "Lama", radiador, new Vector3(0.035f, alto * (0.4f + 0.24f * k), 0f), Vector3.zero, new Vector3(0.02f, 0.03f, hz * 1.4f), plata));
                var cerrado = new Vector3(0f, alto * 0.5f, hz + 0.015f); var abierto = new Vector3(-hx * 0.75f, alto * 0.5f, hz + 0.015f);
                var obturador = Pivote("Obturador", m, cerrado);
                SinSombra(Pieza(PrimitiveType.Cube, "Hoja", obturador, Vector3.zero, Vector3.zero, new Vector3(hx * 0.8f, alto * 0.22f, 0.02f), oscuro));
                piezas.Add(PiezaMovil(radiador, bisagra, bisagra, Vector3.zero, new Vector3(0f, 0f, -82f), 0f, 0.55f));
                piezas.Add(PiezaMovil(obturador, cerrado, abierto, Vector3.zero, Vector3.zero, 0.62f, 1f));
                break;
            }
        }
        var mecanismo = m.gameObject.AddComponent<MecanismoInstrumento>();
        mecanismo.Fijar(piezas.ToArray());
        EditorUtility.SetDirty(mecanismo);
    }

    /// <summary>Tapa ciega del soporte vacío: una placa atornillada a ras del casco.</summary>
    static Transform TapaCiega(Transform nave, string id)
    {
        var oscuro = CrearMaterial("Nave_Placa", Hex("#3A3F47"), 0.75f, 0.55f);
        var plata = CrearMaterial("Nave_Plata", Hex("#C9CED6"), 0.85f, 0.7f);
        var t = Pivote("Tapa_" + id, nave, Vector3.zero);
        SinSombra(Pieza(PrimitiveType.Cylinder, "Placa", t, Vector3.zero, Vector3.zero, new Vector3(0.3f, 0.012f, 0.3f), oscuro));
        for (int k = 0; k < 4; k++)
        {
            var p = Quaternion.Euler(0f, 45f + 90f * k, 0f) * Vector3.forward * 0.115f;
            SinSombra(Pieza(PrimitiveType.Cylinder, "Perno", t, p + Vector3.up * 0.012f, Vector3.zero, new Vector3(0.035f, 0.008f, 0.035f), plata));
        }
        return t;
    }
}
