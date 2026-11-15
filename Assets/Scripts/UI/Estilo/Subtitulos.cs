using System.Collections.Generic;

namespace SpaceMission
{
    /// <summary>
    /// Subtítulos de las voces (en inglés): las seis transmisiones de la Operación y las cuatro frases de control de
    /// la cinemática, en inglés y en español. El jugador elige en Opciones: English, Español u Off.
    /// El juego está en inglés; esta tabla es lo único que existe en dos idiomas.
    /// </summary>
    public static class Subtitulos
    {
        static readonly Dictionary<string, (string en, string es)> Textos = new Dictionary<string, (string, string)>
        {
            ["TX-00"] = ("The spacecraft enters orbit above the anomaly region. The dust storm covers much of the terrain, but the instruments are starting to receive data. Observation begins.",
                         "La nave entra en órbita sobre la región de la anomalía. La tormenta de polvo cubre buena parte del terreno, pero los instrumentos empiezan a recibir datos. Comienza la observación."),
            ["TX-01"] = ("The infrared thermal camera detects a temperature anomaly beneath the dust: an area warmer than its surroundings that stays stable over several passes. The pattern is hard to attribute to sunlight alone. Its source is still unknown.",
                         "La cámara térmica infrarroja detecta una anomalía de temperatura bajo el polvo: una zona más cálida que su entorno que se mantiene estable durante varias pasadas. El patrón es difícil de atribuir solo a la luz del Sol. Todavía no se sabe qué lo origina."),
            ["TX-02"] = ("The subsurface radar penetrates the dust and the surface. Beneath the anomaly, reflections appear that are consistent with underground structures and possible ice deposits. These are clues, not confirmation: interpreting them requires more observations.",
                         "El radar de subsuelo atraviesa el polvo y la superficie. Bajo la anomalía aparecen reflexiones compatibles con estructuras subterráneas y con posibles depósitos de hielo. Son indicios, no una confirmación: interpretarlos exige más observaciones."),
            ["TX-03"] = ("The atmospheric sensor measures the storm: the amount of dust and the air temperature over the region. This data helps separate the effect of the storm from the signal being studied.",
                         "El sensor atmosférico mide la tormenta: la cantidad de polvo y la temperatura del aire sobre la región. Estos datos permiten separar mejor el efecto de la tormenta de la señal que se está estudiando."),
            ["TX-04"] = ("The high-resolution camera captures sharp images between the dust swirls. Over the anomaly area, terrain textures and fractures can be seen that may be related to the detection, although a single image is not enough to confirm it.",
                         "La cámara de alta resolución consigue imágenes nítidas entre los remolinos de polvo. Sobre la zona de la anomalía se distinguen texturas y fracturas del terreno que podrían relacionarse con lo detectado, aunque una imagen por sí sola no basta para afirmarlo."),
            ["TX-05"] = ("The mineralogical spectrometer analyzes the light reflected by the surface. Minerals appear that may have formed in the presence of water in the past, which is consistent with the ice hypothesis but does not prove it. The exact composition remains an open question.",
                         "El espectrómetro mineralógico analiza la luz que refleja la superficie. Aparecen minerales que pudieron formarse en presencia de agua en el pasado, un dato coherente con la hipótesis del hielo pero que no la demuestra. La composición exacta sigue abierta."),
            ["VozEncendido"] = ("Ignition sequence started. Main engines ignited.", "Secuencia de encendido iniciada. Motores principales, encendidos."),
            ["VozDespegue"] = ("Liftoff! The rocket has cleared the tower.", "¡Despegue! El cohete dejó la torre."),
            ["VozSeparacion"] = ("Separation confirmed. The upper stage continues its ascent.", "Separación confirmada. La etapa superior continúa el ascenso."),
            ["VozOrbita"] = ("Orbit insertion complete. We are in orbit around Mars.", "Inserción orbital completada. Estamos en órbita de Marte."),
        };

        /// <summary>Se avisa al empezar una voz: texto en el idioma elegido y duración en segundos.</summary>
        public static event System.Action<string, float> AlHablar;

        /// <summary>Texto del subtítulo en el idioma elegido; null con los subtítulos apagados o sin texto.</summary>
        public static string Texto(string id)
        {
            if (id == null || !Textos.TryGetValue(id, out var t)) return null;
            switch (Preferencias.Subtitulos)
            {
                case IdiomaSubtitulos.Espanol: return t.es;
                case IdiomaSubtitulos.Off: return null;
                default: return t.en;
            }
        }

        public static bool Existe(string id) => id != null && Textos.ContainsKey(id);

        public static void Mostrar(string id, float duracion)
        {
            var texto = Texto(id);
            if (texto != null) AlHablar?.Invoke(texto, duracion);
        }
    }
}
