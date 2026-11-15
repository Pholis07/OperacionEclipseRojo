using UnityEditor;
using UnityEngine;

using SpaceMission;

/// <summary>
/// Versión 5 (aprobada por Adrian el 2026-11-15): valores basados en datos reales de la NASA, escalados.
/// - Instrumentos: masa = real × 3 y potencia = real × 5 (redondeo a 5), así se respetan las proporciones reales.
///   Los créditos siguen siendo moneda de juego (la NASA no publica el costo de cada instrumento); solo la cámara de
///   alta resolución sube a 200 para conservar el resultado de los 19 casos.
/// - La tormenta ya no suma potencia a cada instrumento (extraTormentaW = 0): en la realidad baja la de los paneles.
/// - Explorador: 500 kg (antes 650) para que la masa siga contando. Los lanzadores conservan valores de juego.
/// Cada opción guarda el dato real en inglés y en español, con su fuente (PROPUESTA-GAMEPLAY.md, versión 5).
/// Se puede correr más de una vez. Batch: -executeMethod DatosNasa.Aplicar
/// </summary>
public static class DatosNasa
{
    const string Carpeta = "Assets/Datos/";

    [MenuItem("Herramientas/Aplicar datos de la NASA (versión 5)")]
    public static void Aplicar()
    {
        Instrumento("termica", 35, 70, 180,
            "Measures the ground temperature and provides evidence of the thermal anomaly.",
            "Based on THEMIS (Mars Odyssey): 11.2 kg, 14 W, thermal images at 100 m per pixel.",
            "Basada en THEMIS (Mars Odyssey): 11.2 kg, 14 W, imágenes térmicas de 100 m por píxel.",
            "ASU / NASA");
        Instrumento("radar", 45, 50, 260,
            "Penetrates the dust and the surface and provides evidence of what lies beneath the surface.",
            "Based on SHARAD (MRO): 15 kg, 10 W of transmitted power, sees down to about 1 km below the surface.",
            "Basado en SHARAD (MRO): 15 kg, 10 W de potencia emitida, ve hasta ~1 km bajo la superficie.",
            "NASA/JPL (MRO)");
        Instrumento("alta_res", 195, 300, 200,
            "Takes sharp images through the dust and provides detail images of the surface.",
            "Based on HiRISE (MRO): about 65 kg, 60 W, resolves objects about 1 m across. The heaviest camera ever sent to Mars.",
            "Basada en HiRISE (MRO): unos 65 kg, 60 W, distingue objetos de ~1 m. La cámara más pesada enviada a Marte.",
            "NASA/JPL (MRO)");
        Instrumento("atmosferico", 25, 55, 90,
            "Measures the dust and the air temperature and provides information about the storm's environment.",
            "Based on the Mars Climate Sounder (MRO): 9 kg, 11 W, 9 channels from 0.3 to 45 µm, one reading every 5 km in height.",
            "Basado en el Mars Climate Sounder (MRO): 9 kg, 11 W, 9 canales de 0.3 a 45 µm, un dato cada 5 km de altura.",
            "NASA/JPL (MRO)");
        Instrumento("espectrometro", 100, 225, 220,
            "Analyzes the light reflected by the surface and provides information about its composition.",
            "Based on CRISM (MRO): 33 kg, about 45 W, 362–3920 nm at 18 m per pixel.",
            "Basado en CRISM (MRO): 33 kg, unos 45 W, de 362 a 3920 nm a 18 m por píxel.",
            "NASA/JPL (MRO)");

        Lanzador("explorador", 500,
            "Inspired by Soyuz-Fregat, which sent ESA's Mars Express (about 1.2 t) to Mars. The game keeps its own capacities.",
            "Inspirado en el Soyuz-Fregat, que envió a Marte la Mars Express de la ESA (~1.2 t). El juego usa capacidades propias.",
            "ESA");
        Lanzador("titan", 750,
            "Inspired by NASA's SLS Block 1: more than 27 t to the Moon. The real gap with Soyuz is over 20 times; the game keeps its own capacities.",
            "Inspirado en el SLS Block 1 de la NASA: más de 27 t hacia la Luna. La diferencia real con el Soyuz es de más de 20 veces; el juego usa capacidades propias.",
            "NASA");

        Orbita("polar",
            "Like the Mars Reconnaissance Orbiter: 255 × 320 km, 93°, about 112 minutes per orbit. Low and sun-synchronous, ideal for detail.",
            "Como el Mars Reconnaissance Orbiter: 255 × 320 km, 93°, unos 112 minutos por vuelta. Baja y heliosíncrona, ideal para el detalle.",
            "NASA/JPL (MRO)");
        Orbita("media",
            "Like ESA's Trace Gas Orbiter: about 400 km, 74°. Its plane drifts, so it sees each region at different times of day.",
            "Como el Trace Gas Orbiter de la ESA: unos 400 km, 74°. Su plano se desplaza y ve cada región a distintas horas.",
            "ESA");
        Orbita("eliptica",
            "Like MAVEN: 150 × 6,200 km, 75°, 4.5 hours. It dips into the upper atmosphere on every pass, which is why it needs the atmospheric sensor.",
            "Como MAVEN: 150 × 6,200 km, 75°, 4.5 horas. Baja a la alta atmósfera en cada vuelta: por eso necesita el sensor atmosférico.",
            "NASA/LASP (MAVEN)");

        Satelite("mro",
            "Reference spacecraft: 1,031 kg dry, about 1,000 W at Mars, 160 Gb of storage, up to 6 Mbps to Earth.",
            "Nave de referencia: 1,031 kg en seco, unos 1,000 W en Marte, 160 Gb de memoria, hasta 6 Mbps hacia la Tierra.",
            "NASA NSSDC");
        Satelite("magellan",
            "Mapped 98% of Venus with radar (120–300 m resolution) and was the first planetary spacecraft to use aerobraking.",
            "Cartografió el 98% de Venus con radar (resolución de 120 a 300 m) y fue la primera nave planetaria en usar aerofrenado.",
            "NASA");
        Satelite("chandra",
            "4,790 kg and very precise pointing, but only 1.8 Gb of onboard storage (MRO has 160 Gb).",
            "4,790 kg y un apunte muy preciso, pero solo 1.8 Gb de memoria a bordo (el MRO tiene 160 Gb).",
            "NASA NSSDC / Harvard-Smithsonian");
        Satelite("aquarius",
            "Its microwave radiometer measured ocean salinity through clouds; the platform generated about 1,360 W.",
            "Su radiómetro de microondas medía la salinidad del océano a través de las nubes; la plataforma generaba unos 1,360 W.",
            "NASA/JPL");
        Satelite("llcd",
            "The LLCD laser terminal on LADEE sent data from the Moon at 622 Mbps, about 100 times MRO's radio link.",
            "El terminal láser LLCD de LADEE envió datos desde la Luna a 622 Mbps, unas 100 veces el enlace de radio del MRO.",
            "NASA");
        Satelite("aztechsat",
            "1U CubeSat of about 1 kg built by UPAEP (Puebla); in 2020 it tested a link through the Globalstar constellation.",
            "CubeSat 1U de ~1 kg de la UPAEP (Puebla); en 2020 probó un enlace a través de la constelación Globalstar.",
            "NASA Ames");

        AssetDatabase.SaveAssets();
        Debug.Log("[DatosNasa] Datos de la versión 5 escritos.");
    }

    static T Cargar<T>(string nombre) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(Carpeta + nombre + ".asset");
        if (a == null) throw new System.Exception("Falta el asset " + nombre);
        return a;
    }

    static void Instrumento(string id, int masa, int potencia, int costo, string ayuda, string en, string es, string fuente)
    {
        var i = Cargar<InstrumentoDatos>("Instrumento_" + id);
        i.masaKg = masa;
        i.potenciaNormalW = potencia;
        i.extraTormentaW = 0;
        i.costoCr = costo;
        i.textoAyuda = ayuda;
        i.datoNasa = en; i.datoNasaEs = es; i.fuenteNasa = fuente;
        EditorUtility.SetDirty(i);
    }

    static void Lanzador(string id, int capacidad, string en, string es, string fuente)
    {
        var l = Cargar<LanzadorDatos>("Lanzador_" + id);
        l.capacidadKg = capacidad;
        l.datoNasa = en; l.datoNasaEs = es; l.fuenteNasa = fuente;
        EditorUtility.SetDirty(l);
    }

    static void Orbita(string id, string en, string es, string fuente)
    {
        var o = Cargar<OrbitaDatos>("Orbita_" + id);
        o.datoNasa = en; o.datoNasaEs = es; o.fuenteNasa = fuente;
        EditorUtility.SetDirty(o);
    }

    static void Satelite(string id, string en, string es, string fuente)
    {
        var s = Cargar<SateliteDatos>("Satelite_" + id);
        s.datoNasa = en; s.datoNasaEs = es; s.fuenteNasa = fuente;
        EditorUtility.SetDirty(s);
    }
}
