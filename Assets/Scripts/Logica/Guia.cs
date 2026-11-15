using System.Linq;

namespace SpaceMission
{
    /// <summary>Qué parte de la pantalla señala la guía.</summary>
    public enum ObjetivoGuia { Ninguno, Instrumento, Mapa, Enviar, Resolver, Decision, Informe }

    /// <summary>El siguiente paso que la guía le propone al jugador.</summary>
    public struct PasoGuia
    {
        public ObjetivoGuia Objetivo;
        public string InstrumentoId;   // si el objetivo es un instrumento o su región en el mapa
        public string Texto;
        public bool Aviso;             // algo va mal (memoria, envío): se pinta en ámbar
    }

    /// <summary>
    /// Versión 5, bloque 3: guía de la Operación. Mira el estado (no cambia nada) y dice qué conviene hacer ahora,
    /// en orden de importancia: fin, decisión pendiente, instrumentos, regiones, envío, memoria, lecturas y resolver.
    /// </summary>
    public static class Guia
    {
        public static PasoGuia Siguiente(Operacion op, CatalogoDatos catalogo)
        {
            var r = catalogo.reglas;
            string Nombre(string id) => catalogo.Instrumento(id).Nombre;

            if (op.Completa)
                return Paso(ObjetivoGuia.Informe, Idioma.T("All the evidence reached Earth! Press \u00abView report\u00bb to see the result."));
            if (op.Terminada)
                return Paso(ObjetivoGuia.Informe, Idioma.T("The campaign is over: open the report."));
            if (op.EsperandoDecision)
                return Paso(ObjetivoGuia.Decision, Idioma.T("Mission Control needs a decision about the event."));

            // Enviar va antes que todo lo demás: la ventana de contacto no espera. Solo se pide si hay evidencia que
            // contar o si la memoria se va a llenar; enviar datos sin evidencia no corre prisa.
            bool evidenciaEnMemoria = op.Memoria.Any(x => x.Evidencias.Count > 0);
            bool seLlenara = op.MemoriaUsada + op.BloquesPrevistos > op.MemoriaCapacidad;
            if (op.Memoria.Count > 0 && op.MotivoSinEnvio == null && !op.TransmitirPlaneado && (evidenciaEnMemoria || seLlenara))
                return Paso(ObjetivoGuia.Enviar, evidenciaEnMemoria
                    ? Idioma.T("Contact with Earth this month: turn on \u00abSend at month end\u00bb so your evidence counts.")
                    : Idioma.T("Contact with Earth this month: send to free memory, it will not fit otherwise."));

            // Instrumentos que aún tienen que encontrar su evidencia y no están bloqueados.
            var faltan = op.Instrumentos.Where(id => op.Pendientes.ContainsValue(id) && !op.Bloqueados.ContainsKey(id)).ToList();
            var apagado = faltan.FirstOrDefault(id => !op.Encendidos.Contains(id));
            if (apagado != null && !faltan.Any(op.Encendidos.Contains))
                return Paso(ObjetivoGuia.Instrumento, Idioma.T("Turn on the ") + Nombre(apagado) + Idioma.T(": it still has to find its evidence."), apagado);

            var sinRegion = op.Encendidos.FirstOrDefault(id => !op.Regiones.ContainsKey(id));
            if (sinRegion != null)
                return Paso(ObjetivoGuia.Mapa, Idioma.T("Click the map to choose where the ") + Nombre(sinRegion) + Idioma.T(" will look."), sinRegion);

            if (seLlenara && !op.TransmitirPlaneado)
                return Paso(ObjetivoGuia.Resolver, Idioma.T("Memory will be full: new data will not fit until you send it") +
                    (op.ContactoAbierto ? "." : Idioma.T(" (contact in ") + op.MesesParaContacto + (op.MesesParaContacto == 1 ? Idioma.T(" month).") : Idioma.T(" months)."))), aviso: true);

            if (evidenciaEnMemoria && op.MesesRestantes == 1 && op.MotivoSinEnvio != null)
                return Paso(ObjetivoGuia.Resolver, Idioma.T("Last month and your evidence cannot be sent: ") + op.MotivoSinEnvio + ".", aviso: true);

            // Pista de búsqueda: la última lectura del instrumento que sigue buscando, si su región no se movió.
            foreach (var id in faltan.Where(op.Encendidos.Contains))
            {
                var ultima = op.Historial.LastOrDefault(x => x.Instrumento == id);
                if (ultima == null || !op.Regiones.TryGetValue(id, out var region) || region != ultima.Region) continue;
                string pista = ultima.Lectura == Lectura.Frio ? Idioma.T("was cold: try a different part of the map.")
                    : ultima.Lectura == Lectura.Tibio ? Idioma.T("was warm: you are getting closer, move toward the warmer marks.")
                    : ultima.Lectura == Lectura.Caliente ? Idioma.T("was hot: the anomaly is very close, move the region just a little.")
                    : ultima.Lectura == Lectura.SinLectura ? Idioma.T("gave no reading (orientation): try again.")
                    : ultima.Calidad < r.calidadMinima ? Idioma.T("found the anomaly, but quality ") + (int)ultima.Calidad + Idioma.T(" is below ") + (int)r.calidadMinima +
                      Idioma.T(": observe it again with more useful days or from low orbit.")
                    : null;
                if (pista != null)
                    return Paso(ObjetivoGuia.Mapa, Idioma.T("The last ") + Nombre(id) + Idioma.T(" reading ") + pista +
                        Idioma.T(" (Map: blue cold, yellow warm, orange hot, green found.)"), id);
            }

            return Paso(ObjetivoGuia.Resolver, Idioma.T("Plan ready: press \u00abResolve month ") + op.Mes + "\u00bb.");
        }

        static PasoGuia Paso(ObjetivoGuia objetivo, string texto, string instrumento = null, bool aviso = false) =>
            new PasoGuia { Objetivo = objetivo, Texto = texto, InstrumentoId = instrumento, Aviso = aviso };
    }
}
