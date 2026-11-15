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
/// Versión 6 (alcance): masa que cada lanzador manda hacia Marte, altitudes reales de cada órbita, velocidad de
/// llegada e Isp de los motores. Con ellos Alcance.cs calcula el frenado (vis-viva y ecuación del cohete).
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

        Lanzador("explorador", 500, 1223,
            "Sends 1,223 kg toward Mars: the launch mass of ESA's Mars Express on a Soyuz-Fregat (2003), which also stayed in an elliptical orbit.",
            "Envía 1,223 kg hacia Marte: la masa de lanzamiento de la Mars Express de la ESA en un Soyuz-Fregat (2003), que también se quedó en una órbita elíptica.",
            "ESA");
        Lanzador("titan", 750, 1850,
            "Looks like NASA's SLS Block 1 (more than 27 t toward the Moon). The game uses 1,850 kg toward Mars, 1.5 times the Explorer; an Atlas V sent the real MRO with 2,180 kg.",
            "Aspecto del SLS Block 1 de la NASA (más de 27 t hacia la Luna). El juego usa 1,850 kg hacia Marte, 1.5 veces el Explorador; un Atlas V envió al MRO real con 2,180 kg.",
            "NASA");

        Orbita("polar", 255, 320,
            "Like the Mars Reconnaissance Orbiter: 255 × 320 km, 93°, about 112 minutes per orbit. Low and sun-synchronous, ideal for detail.",
            "Como el Mars Reconnaissance Orbiter: 255 × 320 km, 93°, unos 112 minutos por vuelta. Baja y heliosíncrona, ideal para el detalle.",
            "NASA/JPL (MRO)");
        Orbita("media", 400, 400,
            "Like ESA's Trace Gas Orbiter: about 400 km, 74°. Its plane drifts, so it sees each region at different times of day.",
            "Como el Trace Gas Orbiter de la ESA: unos 400 km, 74°. Su plano se desplaza y ve cada región a distintas horas.",
            "ESA");
        Orbita("eliptica", 150, 6200,
            "Like MAVEN: 150 × 6,200 km, 75°, 4.5 hours. It dips into the upper atmosphere on every pass, which is why it needs the atmospheric sensor.",
            "Como MAVEN: 150 × 6,200 km, 75°, 4.5 horas. Baja a la alta atmósfera en cada vuelta: por eso necesita el sensor atmosférico.",
            "NASA/LASP (MAVEN)");

        // Versión 7: dos órbitas nuevas (paquete de diseño del 15 de noviembre, valores aprobados por Diego).
        OrbitaNueva("ecuatorial", "Low equatorial",
            "Circles Mars near the equator at low altitude and passes over the anomaly region again and again.",
            100, 300, 300,
            "Repeated low passes over the anomaly (Gale crater, about 5° S)",
            "Never sees the poles",
            new[] { "termica", "radar" }, new[] { "alta_res" }, new string[0], "",
            "No Mars orbiter has flown a low equatorial orbit yet. An equatorial orbit only covers low latitudes; Gale crater, where Curiosity landed, is at about 5° S.",
            "Ningún orbitador de Marte ha volado todavía una órbita ecuatorial baja. Una órbita ecuatorial solo cubre latitudes bajas; el cráter Gale, donde aterrizó Curiosity, está a unos 5° S.",
            "NASA/JPL");
        OrbitaNueva("areoestacionaria", "Areostationary",
            "Stays fixed over one point of the equator, about 17,000 km up, watching the same hemisphere all the time.",
            160, 17031, 17031,
            "Continuous watch of the storm over the region",
            "Too far for the subsurface radar and the high-resolution camera: at most a partial result",
            new[] { "termica", "radar" }, new[] { "atmosferico" }, new[] { "radar", "alta_res" },
            "From 17,000 km the subsurface radar and the high-resolution camera cannot work: this orbit gives at most a partial result.",
            "Areostationary orbit: about 17,032 km above the equator (20,428 km from the center), one turn every 24 h 37 min, the same as Mars' rotation. Proposed for weather monitoring and relay; no spacecraft uses it yet.",
            "Órbita areoestacionaria: unos 17,032 km sobre el ecuador (20,428 km desde el centro), una vuelta cada 24 h 37 min, igual que la rotación de Marte. Propuesta para vigilar el clima y como repetidor; todavía no la usa ninguna nave.",
            "NASA NTRS (Montabone et al., 2020)");

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

        Reglas(2.7f, 230,
            "Mars: GM 42,828 km³/s², radius 3,389.5 km. Hydrazine engines like MRO's (Isp about 230 s), arriving at 2.7 km/s. Engines only: the real MRO braked about 1 km/s into a 400 × 35,000 km orbit and then needed six months of aerobraking to reach 255 × 320 km.",
            "Marte: GM 42,828 km³/s², radio 3,389.5 km. Motores de hidracina como los del MRO (Isp de unos 230 s), llegando a 2.7 km/s. Solo con motores: el MRO real frenó unos 1 km/s hasta una órbita de 400 × 35,000 km y después necesitó seis meses de aerofrenado para llegar a 255 × 320 km.",
            "NASA NSSDC");

        AssetDatabase.SaveAssets();
        Debug.Log("[DatosNasa] Datos de la versión 7 escritos.");
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

    static void Lanzador(string id, int capacidad, int masaInyectada, string en, string es, string fuente)
    {
        var l = Cargar<LanzadorDatos>("Lanzador_" + id);
        l.capacidadKg = capacidad;
        l.masaInyectadaKg = masaInyectada;
        l.datoNasa = en; l.datoNasaEs = es; l.fuenteNasa = fuente;
        EditorUtility.SetDirty(l);
    }

    static void Orbita(string id, int periapsis, int apoapsis, string en, string es, string fuente)
    {
        var o = Cargar<OrbitaDatos>("Orbita_" + id);
        o.periapsisKm = periapsis;
        o.apoapsisKm = apoapsis;
        o.datoNasa = en; o.datoNasaEs = es; o.fuenteNasa = fuente;
        EditorUtility.SetDirty(o);
    }

    /// <summary>Crea (o actualiza) una órbita que no estaba en el plan original y la añade al catálogo.</summary>
    static void OrbitaNueva(string id, string nombre, string descripcion, int costo, int periapsis, int apoapsis,
        string ventaja, string limitacion, string[] requisito, string[] revelaciones, string[] sinAlcance, string motivoSinAlcance,
        string en, string es, string fuente)
    {
        var ruta = Carpeta + "Orbita_" + id + ".asset";
        var o = AssetDatabase.LoadAssetAtPath<OrbitaDatos>(ruta);
        if (o == null)
        {
            o = ScriptableObject.CreateInstance<OrbitaDatos>();
            AssetDatabase.CreateAsset(o, ruta);
        }
        o.id = id; o.nombre = nombre; o.descripcionBreve = descripcion; o.costoCr = costo;
        o.periapsisKm = periapsis; o.apoapsisKm = apoapsis;
        o.ventaja = ventaja; o.limitacion = limitacion;
        o.requisitoDeVictoria = new System.Collections.Generic.List<string>(requisito);
        o.revelacionesDesbloqueadas = new System.Collections.Generic.List<string>(revelaciones);
        o.instrumentosSinAlcance = new System.Collections.Generic.List<string>(sinAlcance);
        o.motivoSinAlcance = motivoSinAlcance;
        o.datoNasa = en; o.datoNasaEs = es; o.fuenteNasa = fuente;
        EditorUtility.SetDirty(o);
        var catalogo = Cargar<CatalogoDatos>("Catalogo");
        if (!catalogo.orbitas.Contains(o)) { catalogo.orbitas.Add(o); EditorUtility.SetDirty(catalogo); }
    }

    static void Reglas(float velocidadLlegada, int isp, string en, string es, string fuente)
    {
        var r = Cargar<ReglasOperacionDatos>("ReglasOperacion");
        r.velocidadLlegadaKmS = velocidadLlegada;
        r.impulsoEspecificoS = isp;
        r.datoNasaAlcance = en; r.datoNasaAlcanceEs = es; r.fuenteNasaAlcance = fuente;
        EditorUtility.SetDirty(r);
    }

    static void Satelite(string id, string en, string es, string fuente)
    {
        var s = Cargar<SateliteDatos>("Satelite_" + id);
        s.datoNasa = en; s.datoNasaEs = es; s.fuenteNasa = fuente;
        EditorUtility.SetDirty(s);
    }
}
