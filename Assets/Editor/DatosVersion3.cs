using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SpaceMission;

/// <summary>
/// Escribe en los assets los números de la versión 3 del gameplay (PROPUESTA-GAMEPLAY.md): costo y masa de
/// las herramientas, bodega y combustible, memoria por nivel, respuestas a los eventos y objetivos.
/// Se puede correr más de una vez: deja siempre los mismos valores.
/// Batch: -executeMethod DatosVersion3.Aplicar
/// </summary>
public static class DatosVersion3
{
    const string Carpeta = "Assets/Datos";

    [MenuItem("Herramientas/Datos de la versión 3")]
    public static void Aplicar()
    {
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>(Carpeta + "/Catalogo.asset");

        Herramienta(catalogo, "bateria", 40, 20);
        Herramienta(catalogo, "reconfiguracion", 30, 10);
        Herramienta(catalogo, "refrigeracion", 30, 15);
        Herramienta(catalogo, "giroscopio", 40, 15);

        var r = catalogo.reglas;
        r.espaciosBodega = 2;
        r.kgPorCombustible = 10;
        r.combustibleMaximo = 20;
        r.objetivosPorMision = 2;
        r.combustiblePorCambioAltitud = 3;
        r.factorAltitudBaja = 1.5f;
        r.combustiblePorOrbitaBaja = 1;
        r.orbitasPerdidasSinCombustible = 1;
        r.puntosPorCombustible = 10;
        r.cicloContacto = 3;
        r.vatiosTransmisor = 150;
        EditorUtility.SetDirty(r);

        Memoria(catalogo.Nivel(0), 4);
        Memoria(catalogo.Nivel(1), 3);
        Memoria(catalogo.Nivel(2), 3);

        Opciones(catalogo, "sombra",
            Op("Usar la batería de respaldo", EfectoOpcion.UsarHerramienta),
            Op("Maniobrar para salir antes de la sombra (-4 de combustible, dura 1 órbita)", EfectoOpcion.Acortar, combustible: 4, duracion: 1),
            Op("Esperar a que pase (dura 3 órbitas)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "micrometeorito",
            Op("Usar la reconfiguración de energía", EfectoOpcion.UsarHerramienta),
            Op("Desviar la corriente a mano (-1 órbita)", EfectoOpcion.Resolver, orbitas: 1),
            Op("Seguir con menos energía hasta el final", EfectoOpcion.Aguantar));
        Opciones(catalogo, "sobrecalentamiento",
            Op("Usar la refrigeración de emergencia", EfectoOpcion.UsarHerramienta),
            Op("Girar la nave hacia la sombra (-2 de combustible, bloqueo de 1 órbita)", EfectoOpcion.Acortar, combustible: 2, duracion: 1),
            Op("Esperar a que se enfríe (bloqueo de 2 órbitas)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "orientacion",
            Op("Usar el giroscopio de respaldo", EfectoOpcion.UsarHerramienta),
            Op("Corregir con los propulsores (-3 de combustible)", EfectoOpcion.Resolver, combustible: 3),
            Op("Seguir así (el próximo escaneo no dará lectura)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "ventana",
            Op("Aprovechar para observar (+1 órbita)", EfectoOpcion.GanarOrbitas, duracion: 1),
            Op("Abrir ahora una ventana de contacto con la Tierra", EfectoOpcion.AbrirContacto));

        catalogo.objetivos = new List<ObjetivoDatos>
        {
            Objetivo("rapido", "Encontrar todo en 5 escaneos o menos", TipoObjetivo.EscaneosMaximos, 5, 150),
            Objetivo("sin_apagones", "Ningún apagón", TipoObjetivo.SinApagones, 0, 100),
            Objetivo("combustible", "Terminar con 5 de combustible o más", TipoObjetivo.CombustibleMinimo, 5, 100),
            Objetivo("enviar_pronto", "Enviar todo antes de la última órbita", TipoObjetivo.EnviarAntesDeLaUltima, 0, 100),
            Objetivo("sin_herramientas", "No usar ninguna herramienta", TipoObjetivo.SinHerramientas, 0, 150),
        };
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        Debug.Log("[DatosVersion3] Datos de la versión 3 escritos.");
    }

    static void Herramienta(CatalogoDatos c, string id, int costo, int masa)
    {
        var h = c.Herramienta(id);
        h.costoCr = costo;
        h.masaKg = masa;
        EditorUtility.SetDirty(h);
    }

    static void Memoria(MisionDatos m, int bloques)
    {
        m.memoriaBloques = bloques;
        EditorUtility.SetDirty(m);
    }

    static OpcionEvento Op(string texto, EfectoOpcion efecto, int combustible = 0, int orbitas = 0, int duracion = 0) =>
        new OpcionEvento { texto = texto, efecto = efecto, combustible = combustible, orbitas = orbitas, duracion = duracion };

    static void Opciones(CatalogoDatos c, string eventoId, params OpcionEvento[] opciones)
    {
        var e = c.eventos.Find(x => x.id == eventoId);
        e.opciones = new List<OpcionEvento>(opciones);
        EditorUtility.SetDirty(e);
    }

    static ObjetivoDatos Objetivo(string id, string texto, TipoObjetivo tipo, int valor, int puntos)
    {
        string ruta = Carpeta + "/Objetivo_" + id + ".asset";
        var o = AssetDatabase.LoadAssetAtPath<ObjetivoDatos>(ruta);
        bool nuevo = o == null;
        if (nuevo) o = ScriptableObject.CreateInstance<ObjetivoDatos>();
        o.id = id;
        o.texto = texto;
        o.tipo = tipo;
        o.valor = valor;
        o.puntos = puntos;
        if (nuevo) AssetDatabase.CreateAsset(o, ruta);
        else EditorUtility.SetDirty(o);
        return o;
    }
}
