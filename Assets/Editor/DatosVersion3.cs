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
        r.puntosPorCombustible = 10;
        r.cicloContacto = 2;          // versión 4: contacto en los meses pares (y siempre en el último)
        r.vatiosTransmisor = 150;
        // Versión 4 (meses): valores propuestos por el paquete v1.0, ajustables aquí.
        r.diasPorMes = 30;
        r.creditosPorManiobra = 40;
        r.diasPorManiobra = 2;
        r.diasPerdidosPorOpcion = 10;
        r.diasPerdidosSinCombustible = 10;
        r.diasPerdidosPorApagon = 15;
        r.calidadBase = 70f;
        r.calidadMinima = 60f;
        r.factorCalidadTormenta = 0.8f;
        r.factorCalidadBaja = 1.2f;
        EditorUtility.SetDirty(r);

        // Campaña de 12 meses en los tres niveles; la dificultad va en el presupuesto, la potencia y los eventos.
        Campana(catalogo.Nivel(0), 12, 0);
        Campana(catalogo.Nivel(1), 12, 3);
        Campana(catalogo.Nivel(2), 12, 5);
        Bloques(catalogo, "termica", 1);
        Bloques(catalogo, "radar", 2);
        Bloques(catalogo, "alta_res", 1);
        Bloques(catalogo, "atmosferico", 1);
        Bloques(catalogo, "espectrometro", 2);

        Memoria(catalogo.Nivel(0), 6);
        Memoria(catalogo.Nivel(1), 6);
        Memoria(catalogo.Nivel(2), 6);

        Opciones(catalogo, "sombra",
            Op("Usar la batería de respaldo", EfectoOpcion.UsarHerramienta),
            Op("Maniobrar para salir antes de la sombra (-4 de combustible, dura 1 mes)", EfectoOpcion.Acortar, combustible: 4, duracion: 1),
            Op("Esperar a que pase (dura 3 meses)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "micrometeorito",
            Op("Usar la reconfiguración de energía", EfectoOpcion.UsarHerramienta),
            Op("Desviar la corriente a mano (el mes siguiente pierde 10 días)", EfectoOpcion.Resolver, orbitas: 1),
            Op("Seguir con menos energía hasta el final", EfectoOpcion.Aguantar));
        Opciones(catalogo, "sobrecalentamiento",
            Op("Usar la refrigeración de emergencia", EfectoOpcion.UsarHerramienta),
            Op("Girar la nave hacia la sombra (-2 de combustible, bloqueo de 1 mes)", EfectoOpcion.Acortar, combustible: 2, duracion: 1),
            Op("Esperar a que se enfríe (bloqueo de 2 meses)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "orientacion",
            Op("Usar el giroscopio de respaldo", EfectoOpcion.UsarHerramienta),
            Op("Corregir con los propulsores (-3 de combustible)", EfectoOpcion.Resolver, combustible: 3),
            Op("Seguir así (el mes siguiente no dará lecturas)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "ventana",
            Op("Aprovechar para observar (+1 mes de campaña)", EfectoOpcion.GanarOrbitas, duracion: 1),
            Op("Abrir una ventana de contacto con la Tierra el mes siguiente", EfectoOpcion.AbrirContacto));

        var peticion = Evento(catalogo, "peticion", "Petición de la NASA",
            "El equipo de un rover pide observar un punto cercano para planear su ruta. Si lo escaneas, la NASA te lo agradece.",
            EfectoEvento.Peticion, 150);
        var tormenta = Evento(catalogo, "tormenta", "Tormenta creciendo",
            "La tormenta de polvo crece. Desde la altitud normal las lecturas se vuelven confusas.",
            EfectoEvento.Tormenta, 0);
        Opciones(catalogo, "peticion",
            Op("Aceptar: observar el punto marcado en el mapa (+150 puntos)", EfectoOpcion.AceptarObjetivo),
            Op("Rechazar y seguir con la misión", EfectoOpcion.Aguantar));
        Opciones(catalogo, "tormenta",
            Op("Bajar de altitud sin gastar combustible (después el arrastre sí gasta)", EfectoOpcion.BajarAltitud),
            Op("Seguir así (el mes siguiente la calidad de los datos baja)", EfectoOpcion.Aguantar));

        // Satélites (mecánicas aprobadas por Adrian el 2026-11-14). El MRO es el neutro.
        Satelite(catalogo, "mro", 0, 0, "Equilibrado: sin ventajas ni desventajas.", "Ninguna.");
        var magellan = Satelite(catalogo, "magellan", 30, 10,
            "El radar de subsuelo lee un 30 % más lejos. Pionero del aerofrenado: cambiar de altitud cuesta 2 de combustible.",
            "Grabadora antigua: 1 bloque menos de memoria.");
        magellan.instrumentosConAlcance = new[] { "radar" }; magellan.factorAlcance = 1.3f;
        magellan.combustiblePorCambioAltitud = 2; magellan.memoriaExtra = -1;
        var chandra = Satelite(catalogo, "chandra", 30, 40,
            "Apunte muy preciso: no le afecta la pérdida de orientación, y las cámaras térmica y de alta resolución leen un 20 % más lejos.",
            "Muy pesado: sus 40 kg restan 4 de combustible.");
        chandra.instrumentosConAlcance = new[] { "termica", "alta_res" }; chandra.factorAlcance = 1.2f;
        chandra.inmuneOrientacion = true;
        var aquarius = Satelite(catalogo, "aquarius", 40, 20,
            "Radiómetro de microondas: +1 bloque de memoria, y la tormenta creciendo no le baja la calidad de los datos.",
            "El radiómetro va siempre encendido: +50 W de consumo en la Operación.");
        aquarius.memoriaExtra = 1; aquarius.inmuneTormenta = true; aquarius.consumoExtraW = 50;
        var llcd = Satelite(catalogo, "llcd", 50, 10,
            "Comunicación láser: el transmisor gasta 50 W y hay contacto con la Tierra todos los meses.",
            "El polvo bloquea el láser: si sigues en una tormenta creciendo, el mes siguiente no puedes enviar.");
        llcd.vatiosTransmisor = 50; llcd.cicloContacto = 1; llcd.tormentaBloqueaEnvio = true;
        var aztech = Satelite(catalogo, "aztechsat", 15, 0,
            "Enlace por la constelación Globalstar: puede enviar cualquier mes, aunque no haya contacto (sin contacto solo caben 2 bloques).",
            "Plataforma diminuta (CubeSat 1U): 100 W menos de límite de energía y 1 bloque menos de memoria.");
        aztech.envioSinVentana = true; aztech.bloquesEnvioSinVentana = 2; aztech.limiteExtraW = -100; aztech.memoriaExtra = -1;
        foreach (var s in catalogo.satelites) EditorUtility.SetDirty(s);

        TextoEvento(catalogo, "sombra", "La nave entra en la sombra del planeta y los paneles dejan de recibir luz. Hay menos potencia durante unos meses.");
        TextoEvento(catalogo, "orientacion", "La nave pierde la orientación: si no se corrige, el mes siguiente no dará lecturas.");
        TextoEvento(catalogo, "ventana", "La tormenta se abre sobre la región y deja un claro que se puede aprovechar.");
        TextoEvento(catalogo, "tormenta", "La tormenta de polvo crece. Desde la altitud normal, los datos del mes siguiente tendrán peor calidad.");
        TextoEvento(catalogo, "peticion", "El equipo de un rover pide observar un punto cercano para planear su ruta. Si lo observas, la NASA te lo agradece.");

        catalogo.objetivos = new List<ObjetivoDatos>
        {
            Objetivo("rapido", "Conseguir toda la evidencia en 5 observaciones o menos", TipoObjetivo.EscaneosMaximos, 5, 150),
            Objetivo("sin_apagones", "Ningún apagón", TipoObjetivo.SinApagones, 0, 100),
            Objetivo("combustible", "Terminar con 5 de combustible o más", TipoObjetivo.CombustibleMinimo, 5, 100),
            Objetivo("enviar_pronto", "Enviar toda la evidencia antes del último mes", TipoObjetivo.EnviarAntesDeLaUltima, 0, 100),
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

    static void Campana(MisionDatos m, int meses, int eventos)
    {
        m.orbitasNavegacion = meses;
        m.eventosPorMision = eventos;
        EditorUtility.SetDirty(m);
    }

    static void Bloques(CatalogoDatos c, string instrumentoId, int bloques)
    {
        var i = c.Instrumento(instrumentoId);
        i.bloquesDatos = bloques;
        EditorUtility.SetDirty(i);
    }

    /// <summary>Texto del evento en meses (antes hablaban de órbitas y escaneos).</summary>
    static void TextoEvento(CatalogoDatos c, string id, string texto)
    {
        var e = c.eventos.Find(x => x.id == id);
        e.texto = texto;
        EditorUtility.SetDirty(e);
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

    static EventoDatos Evento(CatalogoDatos c, string id, string nombre, string texto, EfectoEvento efecto, int puntos)
    {
        string ruta = Carpeta + "/Evento_" + id + ".asset";
        var e = AssetDatabase.LoadAssetAtPath<EventoDatos>(ruta);
        bool nuevo = e == null;
        if (nuevo) e = ScriptableObject.CreateInstance<EventoDatos>();
        e.id = id;
        e.nombre = nombre;
        e.texto = texto;
        e.efecto = efecto;
        e.puntos = puntos;
        e.herramientaId = "";
        if (nuevo) AssetDatabase.CreateAsset(e, ruta);
        else EditorUtility.SetDirty(e);
        if (!c.eventos.Contains(e)) c.eventos.Add(e);
        return e;
    }

    /// <summary>Deja el satélite con sus valores neutros y le pone costo, masa y textos. Los efectos se ponen después.</summary>
    static SateliteDatos Satelite(CatalogoDatos c, string id, int costo, int masa, string ventaja, string desventaja)
    {
        var s = c.Satelite(id);
        if (s == null)   // lo crea ConstructorEscenas.CrearDatosSatelites; por si se corre esto antes
        {
            s = ScriptableObject.CreateInstance<SateliteDatos>();
            s.id = id;
            AssetDatabase.CreateAsset(s, Carpeta + "/Satelite_" + id + ".asset");
            c.satelites.Add(s);
        }
        s.costoCr = costo; s.masaKg = masa; s.ventaja = ventaja; s.desventaja = desventaja;
        s.instrumentosConAlcance = new string[0]; s.factorAlcance = 1f; s.combustiblePorCambioAltitud = -1;
        s.memoriaExtra = 0; s.inmuneOrientacion = false; s.inmuneTormenta = false; s.consumoExtraW = 0;
        s.vatiosTransmisor = -1; s.cicloContacto = -1; s.tormentaBloqueaEnvio = false;
        s.limiteExtraW = 0; s.envioSinVentana = false; s.bloquesEnvioSinVentana = 0;
        return s;
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
