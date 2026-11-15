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
            Op("Use the backup battery", EfectoOpcion.UsarHerramienta),
            Op("Maneuver out of the shadow early (-4 fuel, lasts 1 month)", EfectoOpcion.Acortar, combustible: 4, duracion: 1),
            Op("Wait for it to pass (lasts 3 months)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "micrometeorito",
            Op("Use the power reconfiguration", EfectoOpcion.UsarHerramienta),
            Op("Reroute the current by hand (next month loses 10 days)", EfectoOpcion.Resolver, orbitas: 1),
            Op("Carry on with less power until the end", EfectoOpcion.Aguantar));
        Opciones(catalogo, "sobrecalentamiento",
            Op("Use the emergency cooling", EfectoOpcion.UsarHerramienta),
            Op("Turn the spacecraft toward the shade (-2 fuel, 1-month lockout)", EfectoOpcion.Acortar, combustible: 2, duracion: 1),
            Op("Wait for it to cool down (2-month lockout)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "orientacion",
            Op("Use the backup gyroscope", EfectoOpcion.UsarHerramienta),
            Op("Correct with the thrusters (-3 fuel)", EfectoOpcion.Resolver, combustible: 3),
            Op("Carry on (next month will give no readings)", EfectoOpcion.Aguantar));
        Opciones(catalogo, "ventana",
            Op("Use it to observe (+1 campaign month)", EfectoOpcion.GanarOrbitas, duracion: 1),
            Op("Open a contact window with Earth next month", EfectoOpcion.AbrirContacto));

        var peticion = Evento(catalogo, "peticion", "NASA request",
            "A rover team asks you to observe a nearby spot to plan its route. If you scan it, NASA will thank you.",
            EfectoEvento.Peticion, 150);
        var tormenta = Evento(catalogo, "tormenta", "Growing storm",
            "The dust storm is growing. From normal altitude the readings become confusing.",
            EfectoEvento.Tormenta, 0);
        Opciones(catalogo, "peticion",
            Op("Accept: observe the spot marked on the map (+150 points)", EfectoOpcion.AceptarObjetivo),
            Op("Decline and carry on with the mission", EfectoOpcion.Aguantar));
        Opciones(catalogo, "tormenta",
            Op("Descend without spending fuel (afterwards, drag does use fuel)", EfectoOpcion.BajarAltitud),
            Op("Carry on (next month's data quality drops)", EfectoOpcion.Aguantar));

        // Satélites (mecánicas aprobadas por Adrian el 2026-11-14). El MRO es el neutro.
        Satelite(catalogo, "mro", 0, 0, "Balanced: no advantages or disadvantages.", "None.");
        var magellan = Satelite(catalogo, "magellan", 30, 10,
            "The subsurface radar reads 30% farther. Aerobraking pioneer: changing altitude costs 2 fuel.",
            "Old recorder: 1 fewer memory block.");
        magellan.instrumentosConAlcance = new[] { "radar" }; magellan.factorAlcance = 1.3f;
        magellan.combustiblePorCambioAltitud = 2; magellan.memoriaExtra = -1;
        var chandra = Satelite(catalogo, "chandra", 30, 40,
            "Very precise pointing: loss of orientation does not affect it, and the thermal and high-resolution cameras read 20% farther.",
            "Very heavy (40 kg cost 4 fuel) and only 1.8 Gb on board, like the real Chandra: -1 memory block.");
        chandra.instrumentosConAlcance = new[] { "termica", "alta_res" }; chandra.factorAlcance = 1.2f;
        chandra.inmuneOrientacion = true;
        chandra.memoriaExtra = -1;   // versión 5: el Chandra real solo guardaba 1.8 Gb (el MRO, 160 Gb)
        var aquarius = Satelite(catalogo, "aquarius", 40, 20,
            "Microwave radiometer: +1 memory block, and the growing storm does not lower its data quality.",
            "The radiometer is always on: +50 W of power draw during the Operation.");
        aquarius.memoriaExtra = 1; aquarius.inmuneTormenta = true; aquarius.consumoExtraW = 50;
        var llcd = Satelite(catalogo, "llcd", 50, 10,
            "Laser communication: the transmitter uses 50 W and there is contact with Earth every month.",
            "Dust blocks the laser: if you stay in a growing storm, you cannot send the next month.");
        llcd.vatiosTransmisor = 50; llcd.cicloContacto = 1; llcd.tormentaBloqueaEnvio = true;
        var aztech = Satelite(catalogo, "aztechsat", 15, 0,
            "Link through the Globalstar constellation: it can send in any month, even without contact (without contact only 2 blocks fit).",
            "Tiny platform (1U CubeSat): 100 W lower power limit and 1 fewer memory block.");
        aztech.envioSinVentana = true; aztech.bloquesEnvioSinVentana = 2; aztech.limiteExtraW = -100; aztech.memoriaExtra = -1;
        foreach (var s in catalogo.satelites) EditorUtility.SetDirty(s);

        TextoEvento(catalogo, "sombra", "The spacecraft enters the planet's shadow and the panels stop receiving light. There is less power for a few months.");
        TextoEvento(catalogo, "orientacion", "The spacecraft loses its orientation: if it is not corrected, next month will give no readings.");
        TextoEvento(catalogo, "ventana", "The storm opens up over the region, leaving a clearing you can take advantage of.");
        TextoEvento(catalogo, "tormenta", "The dust storm is growing. From normal altitude, next month's data will be lower quality.");
        TextoEvento(catalogo, "peticion", "A rover team asks you to observe a nearby spot to plan its route. If you observe it, NASA will thank you.");

        catalogo.objetivos = new List<ObjetivoDatos>
        {
            Objetivo("rapido", "Get all the evidence in 5 observations or fewer", TipoObjetivo.EscaneosMaximos, 5, 150),
            Objetivo("sin_apagones", "No blackouts", TipoObjetivo.SinApagones, 0, 100),
            Objetivo("combustible", "Finish with 5 fuel or more", TipoObjetivo.CombustibleMinimo, 5, 100),
            Objetivo("enviar_pronto", "Send all the evidence before the last month", TipoObjetivo.EnviarAntesDeLaUltima, 0, 100),
            Objetivo("sin_herramientas", "Do not use any tools", TipoObjetivo.SinHerramientas, 0, 150),
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
