using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SpaceMission;

/// <summary>
/// Escribe en los assets de Assets/Datos (y los créditos de Assets/Arte/Arte.asset) los textos en inglés que ve
/// el jugador. Solo toca textos: ids, números y efectos quedan igual. Se puede correr más de una vez.
/// Los mismos textos están en ConstructorEscenas.Nasa.cs, DatosVersion3.cs y ConstructorEstilo.cs, que los
/// reescriben al construir las escenas.
/// Batch: -executeMethod TraduccionIngles.Aplicar
/// </summary>
public static class TraduccionIngles
{
    const string Carpeta = "Assets/Datos/";

    [MenuItem("Herramientas/Aplicar textos en inglés")]
    public static void Aplicar()
    {
        {
            var a = Cargar<EventoDatos>("Evento_micrometeorito");
            a.nombre = "Micrometeorite impact";
            a.texto = "A micrometeorite damages a solar panel circuit. The spacecraft produces less power until the current is rerouted.";
            Opciones(a, "Use the power reconfiguration", "Reroute the current by hand (next month loses 10 days)", "Carry on with less power until the end");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_orientacion");
            a.nombre = "Loss of orientation";
            a.texto = "The spacecraft loses its orientation: if it is not corrected, next month will give no readings.";
            Opciones(a, "Use the backup gyroscope", "Correct with the thrusters (-3 fuel)", "Carry on (next month will give no readings)");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_peticion");
            a.nombre = "NASA request";
            a.texto = "A rover team asks you to observe a nearby spot to plan its route. If you observe it, NASA will thank you.";
            Opciones(a, "Accept: observe the spot marked on the map (+150 points)", "Decline and carry on with the mission");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_sobrecalentamiento");
            a.nombre = "Overheating";
            a.texto = "The active instrument that uses the most power gets too hot and shuts down to protect itself.";
            Opciones(a, "Use the emergency cooling", "Turn the spacecraft toward the shade (-2 fuel, 1-month lockout)", "Wait for it to cool down (2-month lockout)");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_sombra");
            a.nombre = "Passing through Mars' shadow";
            a.texto = "The spacecraft enters the planet's shadow and the panels stop receiving light. There is less power for a few months.";
            Opciones(a, "Use the backup battery", "Maneuver out of the shadow early (-4 fuel, lasts 1 month)", "Wait for it to pass (lasts 3 months)");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_tormenta");
            a.nombre = "Growing storm";
            a.texto = "The dust storm is growing. From normal altitude, next month's data will be lower quality.";
            Opciones(a, "Descend without spending fuel (afterwards, drag does use fuel)", "Carry on (next month's data quality drops)");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<EventoDatos>("Evento_ventana");
            a.nombre = "Clear window";
            a.texto = "The storm opens up over the region, leaving a clearing you can take advantage of.";
            Opciones(a, "Use it to observe (+1 campaign month)", "Open a contact window with Earth next month");
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<HerramientaDatos>("Herramienta_bateria");
            a.nombre = "Backup battery";
            a.descripcion = "Powers the spacecraft while it is in Mars' shadow.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<HerramientaDatos>("Herramienta_giroscopio");
            a.nombre = "Backup gyroscope";
            a.descripcion = "Restores the spacecraft's orientation.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<HerramientaDatos>("Herramienta_reconfiguracion");
            a.nombre = "Power reconfiguration";
            a.descripcion = "Reroutes power through a healthy panel circuit.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<HerramientaDatos>("Herramienta_refrigeracion");
            a.nombre = "Emergency cooling";
            a.descripcion = "Cools an overheated instrument so it can be turned back on.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<InstrumentoDatos>("Instrumento_alta_res");
            a.nombre = "High-resolution camera";
            a.pistaAsociada = "detail images of the surface";
            a.textoAyuda = "Takes sharp images through the dust and provides detail images of the surface. Power: 150 W: 90 W normal + 60 W due to the storm.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<InstrumentoDatos>("Instrumento_atmosferico");
            a.nombre = "Atmospheric sensor";
            a.pistaAsociada = "information about the storm's environment";
            a.textoAyuda = "Measures the dust and the air temperature and provides information about the storm's environment. Power: 70 W: 60 W normal + 10 W due to the storm.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<InstrumentoDatos>("Instrumento_espectrometro");
            a.nombre = "Mineralogical spectrometer";
            a.pistaAsociada = "information about the surface composition";
            a.textoAyuda = "Analyzes the light reflected by the surface and provides information about its composition. Power: 260 W: 130 W normal + 130 W due to the storm.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<InstrumentoDatos>("Instrumento_radar");
            a.nombre = "Subsurface radar";
            a.pistaAsociada = "evidence of what lies beneath the surface";
            a.textoAyuda = "Penetrates the dust and the surface and provides evidence of what lies beneath the surface. Power: 200 W: 180 W normal + 20 W due to the storm.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<InstrumentoDatos>("Instrumento_termica");
            a.nombre = "Infrared thermal camera";
            a.pistaAsociada = "evidence of the thermal anomaly";
            a.textoAyuda = "Measures the ground temperature and provides evidence of the thermal anomaly. Power: 130 W: 100 W normal + 30 W due to the storm.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<LanzadorDatos>("Lanzador_explorador");
            a.nombre = "Explorer";
            a.descripcion = "Light and affordable";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<LanzadorDatos>("Lanzador_titan");
            a.nombre = "Titan";
            a.descripcion = "Larger payload and higher cost";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<MisionDatos>("Mision");
            a.nombreNivel = "2. Standard mission";
            a.objetivo = "Explain the thermal anomaly that appeared in a region of Mars while a dust storm hampers observation.";
            a.narrativa = "A strange thermal signal appears in a region of Mars, and a dust storm hampers the investigation. The clues in the game are not proof of present-day liquid water or of life.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<MisionDatos>("Mision_Nivel1");
            a.nombreNivel = "1. Training";
            a.objetivo = "Explain the thermal anomaly that appeared in a region of Mars while a dust storm hampers observation.";
            a.narrativa = "A strange thermal signal appears in a region of Mars, and a dust storm hampers the investigation. The clues in the game are not proof of present-day liquid water or of life.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<MisionDatos>("Mision_Nivel3");
            a.nombreNivel = "3. Severe storm";
            a.objetivo = "Explain the thermal anomaly that appeared in a region of Mars while a dust storm hampers observation.";
            a.narrativa = "A strange thermal signal appears in a region of Mars, and a dust storm hampers the investigation. The clues in the game are not proof of present-day liquid water or of life.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<ObjetivoDatos>("Objetivo_combustible");
            a.texto = "Finish with 5 fuel or more";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<ObjetivoDatos>("Objetivo_enviar_pronto");
            a.texto = "Send all the evidence before the last month";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<ObjetivoDatos>("Objetivo_rapido");
            a.texto = "Get all the evidence in 5 observations or fewer";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<ObjetivoDatos>("Objetivo_sin_apagones");
            a.texto = "No blackouts";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<ObjetivoDatos>("Objetivo_sin_herramientas");
            a.texto = "Do not use any tools";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<OrbitaDatos>("Orbita_eliptica");
            a.nombre = "Elliptical";
            a.descripcionBreve = "Elongated orbit that gives a panoramic view and measures environmental conditions.";
            a.ventaja = "Panoramic context and environmental conditions";
            a.limitacion = "Full victory also requires the atmospheric sensor";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<OrbitaDatos>("Orbita_media");
            a.nombre = "Mid inclined";
            a.descripcionBreve = "Inclined orbit at intermediate altitude that keeps passing over the anomaly.";
            a.ventaja = "Repeated tracking of the anomaly";
            a.limitacion = "Less surface detail in the game's simplified model";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<OrbitaDatos>("Orbita_polar");
            a.nombre = "Low polar";
            a.descripcionBreve = "Passes over the poles of Mars and flies over a different region on each orbit.";
            a.ventaja = "Detailed observation of different regions";
            a.limitacion = "Less continuous observation of any single region";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_aquarius");
            a.nombre = "Aquarius Platform";
            a.inspiradoEn = "Aquarius / SAC-D (2011)";
            a.descripcion = "Measured ocean salinity from Earth orbit, with a large antenna reflector.";
            a.credito = "3D model: NASA 3D Resources";
            a.ventaja = "Microwave radiometer: +1 memory block, and the growing storm does not lower its data quality.";
            a.desventaja = "The radiometer is always on: +50 W of power draw during the Operation.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_aztechsat");
            a.nombre = "AztechSat-1 Nanosatellite";
            a.inspiradoEn = "AztechSat-1 (UPAEP, 2019)";
            a.descripcion = "1U CubeSat from UPAEP (Puebla, Mexico). Released from the Space Station in 2020, it tested communicating through the Globalstar constellation.";
            a.credito = "3D model: made by the team (built in code)";
            a.ventaja = "Link through the Globalstar constellation: it can send in any month, even without contact (without contact only 2 blocks fit).";
            a.desventaja = "Tiny platform (1U CubeSat): 100 W lower power limit and 1 fewer memory block.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_chandra");
            a.nombre = "Chandra Observatory";
            a.inspiradoEn = "Chandra (1999)";
            a.descripcion = "X-ray space telescope: a very long optical tube with two wings of solar panels.";
            a.credito = "3D model: NASA 3D Resources";
            a.ventaja = "Very precise pointing: loss of orientation does not affect it, and the thermal and high-resolution cameras read 20% farther.";
            a.desventaja = "Very heavy: its 40 kg cost 4 fuel.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_llcd");
            a.nombre = "LLCD Laser Relay";
            a.inspiradoEn = "LLCD (2013)";
            a.descripcion = "Demonstrated laser communications from lunar orbit. Platform with large solar panels.";
            a.credito = "3D model: NASA 3D Resources";
            a.ventaja = "Laser communication: the transmitter uses 50 W and there is contact with Earth every month.";
            a.desventaja = "Dust blocks the laser: if you stay in a growing storm, you cannot send the next month.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_magellan");
            a.nombre = "Magellan Mapper";
            a.inspiradoEn = "Magellan (1989)";
            a.descripcion = "Mapped almost the entire surface of Venus by radar. Its large antenna also served as the radar.";
            a.credito = "3D model: NASA 3D Resources";
            a.ventaja = "The subsurface radar reads 30% farther. Aerobraking pioneer: changing altitude costs 2 fuel.";
            a.desventaja = "Old recorder: 1 fewer memory block.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<SateliteDatos>("Satelite_mro");
            a.nombre = "MRO Orbiter";
            a.inspiradoEn = "Mars Reconnaissance Orbiter (2005)";
            a.descripcion = "Has studied Mars since 2006 with cameras, radar and a spectrometer. Large antenna and two wide solar panels.";
            a.credito = "3D model: NASA 3D Resources";
            a.ventaja = "Balanced: no advantages or disadvantages.";
            a.desventaja = "None.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-00");
            a.texto = "The spacecraft enters orbit above the anomaly region. The dust storm covers much of the terrain, but the instruments are starting to receive data. Observation begins.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-01");
            a.texto = "The infrared thermal camera detects a temperature anomaly beneath the dust: an area warmer than its surroundings that stays stable over several passes. The pattern is hard to attribute to sunlight alone. Its source is still unknown.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-02");
            a.texto = "The subsurface radar penetrates the dust and the surface. Beneath the anomaly, reflections appear that are consistent with underground structures and possible ice deposits. These are clues, not confirmation: interpreting them requires more observations.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-03");
            a.texto = "The atmospheric sensor measures the storm: the amount of dust and the air temperature over the region. This data helps separate the effect of the storm from the signal being studied.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-04");
            a.texto = "The high-resolution camera captures sharp images between the dust swirls. Over the anomaly area, terrain textures and fractures can be seen that may be related to the detection, although a single image is not enough to confirm it.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-05");
            a.texto = "The mineralogical spectrometer analyzes the light reflected by the surface. Minerals appear that may have formed in the presence of water in the past, which is consistent with the ice hypothesis but does not prove it. The exact composition remains an open question.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-A1");
            a.texto = "Without the thermal camera, the mission did not record the anomaly that started the investigation. Without that evidence, the area of interest cannot be pinpointed.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-A2");
            a.texto = "Without the subsurface radar, there was no evidence of what happens beneath the surface. The thermal anomaly, if it was detected, is left without a structural explanation.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-A3");
            a.texto = "Without the atmospheric sensor, the effect of the storm could not be separated from the measurements. In elliptical orbit, this context is needed for a complete conclusion.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-A4");
            a.texto = "Without the high-resolution camera, no detail images of the region's surface were obtained.";
            EditorUtility.SetDirty(a);
        }
        {
            var a = Cargar<TransmisionDatos>("Transmision_TX-A5");
            a.texto = "Without the mineralogical spectrometer, the composition of the region's surface was not analyzed.";
            EditorUtility.SetDirty(a);
        }

        var arte = AssetDatabase.LoadAssetAtPath<ArteDatos>("Assets/Arte/Arte.asset");
        if (arte != null)
        {
            var creditos = new Dictionary<string, string>
            {
                ["termica"] = "NASA/JPL/Arizona State University (THEMIS, Mars Odyssey). Nighttime infrared; colors added.",
                ["radar"] = "NASA/JPL-Caltech/ASI/University of Rome/PSI/Smithsonian Institution (SHARAD, MRO).",
                ["alta_res"] = "NASA/JPL-Caltech/Univ. of Arizona (HiRISE, MRO).",
                ["espectrometro"] = "NASA/JPL-Caltech/UA/JHUAPL (CRISM and HiRISE, MRO).",
                ["atmosferico"] = "NASA/JPL-Caltech/MSSS (MARCI, MRO).",
            };
            foreach (var e in arte.evidencias)
                if (e != null && creditos.TryGetValue(e.instrumentoId, out var credito)) e.credito = credito;
            arte.creditoMision = "NASA/JPL-Caltech/MSSS (MARCI, MRO). 2018 global dust storm.";
            EditorUtility.SetDirty(arte);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("TraduccionIngles: listo");
    }

    static T Cargar<T>(string nombre) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(Carpeta + nombre + ".asset");
        if (a == null) throw new System.Exception("TraduccionIngles: falta " + Carpeta + nombre + ".asset");
        return a;
    }

    /// <summary>Cambia solo el texto de cada respuesta, en orden; el efecto y los costos quedan igual.</summary>
    static void Opciones(EventoDatos e, params string[] textos)
    {
        if (e.opciones.Count != textos.Length)
            throw new System.Exception("TraduccionIngles: el evento " + e.id + " tiene " + e.opciones.Count + " respuestas, no " + textos.Length);
        for (int i = 0; i < textos.Length; i++) e.opciones[i].texto = textos[i];
    }
}
