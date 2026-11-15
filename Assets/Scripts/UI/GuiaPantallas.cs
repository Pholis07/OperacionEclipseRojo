namespace SpaceMission
{
    /// <summary>Pantallas de la escena Mision, en el orden del juego.</summary>
    public enum PantallaMision { Briefing, Configuracion, Revision, Carga, Operacion }

    /// <summary>Un consejo de la guía: qué hacer ahora y el objeto que se resalta (por su nombre; null = nada).</summary>
    public struct ConsejoPantalla
    {
        public string Texto;
        public string Destino;
        public ConsejoPantalla(string texto, string destino) { Texto = texto; Destino = destino; }
    }

    /// <summary>
    /// Versión 6: la guía del modo normal (no solo la Misión 0). Para cada pantalla de la preparación dice en una frase
    /// qué hacer ahora y por qué, y señala el control que toca. También da la explicación completa de cada pantalla
    /// (botón «?»). Sin MonoBehaviours, para poder probarlo; la Operación ya tiene su guía (Guia.cs).
    /// </summary>
    public static class GuiaPantallas
    {
        // ---------- Consejo del momento ----------

        public static ConsejoPantalla Briefing() => new ConsejoPantalla(
            Idioma.T("Read the objective: it says which evidence you need. Then press «Continue». New here? Play Mission 0 first."),
            "BotonContinuar");

        public static ConsejoPantalla Configuracion(Configuracion c, ConfiguracionUI.Pestana pestana, Evaluacion e)
        {
            bool conLanzador = !string.IsNullOrEmpty(c.LanzadorId);
            bool conInstrumentos = c.InstrumentoIds.Count > 0;
            bool conOrbita = !string.IsNullOrEmpty(c.OrbitaId);
            var limites = e != null ? e.Calculo.LimitesSuperados : null;
            bool pasado = limites != null && (limites.Contains(Limite.Masa) || limites.Contains(Limite.Potencia) ||
                                              limites.Contains(Limite.Costo) || limites.Contains(Limite.Espacios));

            if (!conLanzador)
                return pestana == ConfiguracionUI.Pestana.Lanzador
                    ? new ConsejoPantalla(Idioma.T("Choose a rocket: it decides how much mass you can carry and how far it can take it."), null)
                    : new ConsejoPantalla(Idioma.T("Start with the rocket."), "PestanaLanzador");

            if (pasado)
                return new ConsejoPantalla(Idioma.T("A bar is in red: remove an instrument or the heavy satellite, or switch rockets."), "PanelDerecho");

            // Con la nave completa manda el semáforo (abajo), esté en la pestaña que esté.
            if (!(conInstrumentos && conOrbita))
            switch (pestana)
            {
                case ConfiguracionUI.Pestana.Lanzador:
                    return new ConsejoPantalla(Idioma.T("Good. Next: the satellite."), "BotonSiguiente");
                case ConfiguracionUI.Pestana.Satelite:
                    return string.IsNullOrEmpty(c.SateliteId)
                        ? new ConsejoPantalla(Idioma.T("Choose a satellite (optional): green arrows help, red ones cost. The MRO is neutral."), null)
                        : new ConsejoPantalla(Idioma.T("Next: the instruments."), "BotonSiguiente");
            }

            if (!conInstrumentos)
                return pestana == ConfiguracionUI.Pestana.Instrumentos
                    ? new ConsejoPantalla(Idioma.T("Install the instruments: the objective needs the thermal camera and the subsurface radar. Keep every bar out of the red."), null)
                    : new ConsejoPantalla(Idioma.T("Now install the instruments."), "PestanaInstrumentos");

            if (!conOrbita)
                return pestana == ConfiguracionUI.Pestana.Orbita
                    ? new ConsejoPantalla(Idioma.T("Choose an orbit you can reach: the locked ones need more braking than your spacecraft has."), null)
                    : pestana == ConfiguracionUI.Pestana.Instrumentos
                        ? new ConsejoPantalla(Idioma.T("Next: the orbit, the last step."), "BotonSiguiente")
                        : new ConsejoPantalla(Idioma.T("Last step: the orbit."), "PestanaOrbita");

            switch (e != null ? e.Estado : EstadoSemaforo.Gris)
            {
                case EstadoSemaforo.Verde:
                    return new ConsejoPantalla(Idioma.T("Green: everything fits and covers the objective. Press «Review mission»."), "BotonRevisar");
                case EstadoSemaforo.Amarillo:
                    return new ConsejoPantalla(Idioma.T("Yellow: you can launch, but some evidence will be missing (read the message on the right)."), "BotonRevisar");
                case EstadoSemaforo.Rojo:
                    return new ConsejoPantalla(Idioma.T("Red: your spacecraft cannot reach this orbit. Remove mass, switch rockets or choose another orbit."), "PanelDerecho");
                default:
                    return new ConsejoPantalla(Idioma.T("Something is still missing: look at the message on the right."), "PanelDerecho");
            }
        }

        public static ConsejoPantalla Revision(Evaluacion e) => e != null && e.SePuedeLanzar
            ? new ConsejoPantalla(Idioma.T("Check the summary. If it looks right, press «Load the spacecraft»."), "BotonLanzar")
            : new ConsejoPantalla(Idioma.T("This setup cannot launch: press «Fix» and change what is in red."), "BotonCorregir");

        public static ConsejoPantalla Carga(Carga carga) => carga == null || carga.PuedeLanzar
            ? new ConsejoPantalla(Idioma.T("Optional: add tools with what is left over; the free mass becomes fuel. Then press «Launch»."), "BotonLanzarCarga")
            : new ConsejoPantalla(Idioma.T("Not enough left over for this satellite: press «Use the MRO» or remove a tool."), "BotonUsarMro");

        // ---------- Explicación completa (botón «?») ----------

        /// <summary>
        /// La explicación con los datos del nivel: en el Briefing añade los límites y el semáforo, que ya no ocupan
        /// la pantalla (versión 6).
        /// </summary>
        public static string AyudaCompleta(PantallaMision p, CatalogoDatos catalogo, MisionDatos nivel)
        {
            var texto = Ayuda(p);
            if (p == PantallaMision.Operacion && catalogo != null && catalogo.reglas != null && catalogo.reglas.mesLimiteSinEvidencia > 0)
                return texto + string.Format(Idioma.T("\nIf no evidence has reached Earth by the end of month {0}, Mission Control cancels the mission."), catalogo.reglas.mesLimiteSinEvidencia);
            if (p != PantallaMision.Briefing || catalogo == null) return texto;
            return texto + Idioma.T("\n\n<b>LIMITS</b>\n") + TextosBriefing.TextoLimites(catalogo, nivel) +
                   Idioma.T("\n\n<b>THE STATUS LIGHT</b>\n") + TextosBriefing.ExplicacionColores;
        }

        public static string Ayuda(PantallaMision p)
        {
            switch (p)
            {
                case PantallaMision.Briefing: return Idioma.T(
                    "<b>BRIEFING</b>\nYour mission: find out what the thermal anomaly on Mars is.\n" +
                    "• <b>Objective and requirements</b>: the evidence you have to send to Earth.\n" +
                    "• <b>Limits</b>: the mass, power and money you have.\n" +
                    "• <b>Level</b>: level 1 has more budget, level 3 less.\n" +
                    "• <b>Mission 0</b> is a short tutorial: play it first if you are new.");
                case PantallaMision.Configuracion: return Idioma.T(
                    "<b>BUILD YOUR SPACECRAFT</b>, in this order:\n" +
                    "1. <b>Rocket</b>: carries the spacecraft. It limits the mass and the slots, and how much fuel you arrive with.\n" +
                    "2. <b>Satellite</b>: the platform. Green arrows help during the mission, red ones cost.\n" +
                    "3. <b>Instruments</b>: each one finds one kind of evidence. The objective needs the thermal camera and the subsurface radar; some orbits ask for more.\n" +
                    "4. <b>Orbit</b>: where you observe from. Low orbits need more braking: if the spacecraft is too heavy they are locked.\n" +
                    "<b>Bars</b>: if one turns red you cannot launch. <b>Light</b>: green = ready; yellow = you can launch but evidence will be missing; red = over a limit; grey = something left to choose.");
                case PantallaMision.Revision: return Idioma.T(
                    "<b>REVIEW</b>\nA summary of your spacecraft. With a green or yellow light you can go on to the cargo. " +
                    "With red, press «Fix» and change what is in red.");
                case PantallaMision.Carga: return Idioma.T(
                    "<b>CARGO</b>\nWith the credits and mass left over you can carry up to 2 tools. They help when something goes wrong " +
                    "(a battery for the shadow, a gyroscope if you lose orientation…).\n" +
                    "The mass you do not use becomes <b>fuel</b>: you need it to change altitude. Then press «Launch».");
                default: return Idioma.T(
                    "<b>OPERATION</b>: the mission lasts several months. Every month:\n" +
                    "1. <b>Turn on</b> an instrument (it uses power) and <b>click the map</b> where you want to observe.\n" +
                    "2. Press <b>«Resolve month»</b>. The reading tells you if you are cold, warm or hot (close to the anomaly).\n" +
                    "3. Good readings go to <b>memory</b>, but they only count when they reach Earth.\n" +
                    "4. Earth can only be reached some months (<b>contact</b>): then turn on <b>«Send at month end»</b>.\n" +
                    "5. When all the evidence has arrived, press <b>«View report»</b>.\n" +
                    "Events (storms, shadows) may ask you to decide something.");
            }
        }
    }
}
