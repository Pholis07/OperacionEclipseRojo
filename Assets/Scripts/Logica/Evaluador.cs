using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Decide el semáforo final, el resultado y las transmisiones. Lógica pura.
    /// Las reglas vienen del diseño (Plan.md, decisiones 3, 4, 5 y 12; Transmisiones.md);
    /// los ids, requisitos y revelaciones vienen del CatalogoDatos.
    /// </summary>
    public static class Evaluador
    {
        /// <param name="nivel">Datos del nivel; si es null, la misión del diseño (nivel 2).</param>
        public static Evaluacion Evaluar(Configuracion config, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            return Evaluar(config, Calculador.Calcular(config, catalogo, nivel), catalogo, nivel);
        }

        public static Evaluacion Evaluar(Configuracion config, ResultadoCalculo calculo, CatalogoDatos catalogo,
            MisionDatos nivel = null)
        {
            var e = new Evaluacion { Calculo = calculo, Estado = calculo.Estado };

            if (calculo.Estado == EstadoSemaforo.Gris)
                e.Mensaje = MensajeGris(calculo);
            else if (calculo.Estado == EstadoSemaforo.Rojo)
                e.Mensaje = MensajeRojo(calculo, config, catalogo, nivel != null ? nivel : catalogo.mision);
            else
                EvaluarValida(e, config, catalogo);

            e.LineaRegistro = Calculador.LineaRegistro(calculo, e.Estado,
                e.Categoria.HasValue ? e.Categoria.Value.ToString().ToUpperInvariant() : "-");
            Debug.Log(e.LineaRegistro);
            return e;
        }

        static void EvaluarValida(Evaluacion e, Configuracion config, CatalogoDatos catalogo)
        {
            var orbita = catalogo.Orbita(config.OrbitaId);
            // Versión 7: desde algunas órbitas hay instrumentos que no pueden trabajar (OrbitaDatos.instrumentosSinAlcance).
            // Siguen a bordo (pesan y cuestan), pero no dan evidencia.
            var elegidos = config.InstrumentoIds.Where(id => !orbita.instrumentosSinAlcance.Contains(id)).ToList();

            e.SePuedeLanzar = true;
            e.PistasPrincipales.AddRange(elegidos.Where(id => EsPrincipal(id, catalogo)));
            e.Requeridos.AddRange(orbita.requisitoDeVictoria);
            e.EvidenciasFaltantes.AddRange(orbita.requisitoDeVictoria.Where(id => !elegidos.Contains(id)));

            if (e.EvidenciasFaltantes.Count == 0) e.Categoria = CategoriaResultado.Victoria;
            else if (e.PistasPrincipales.Count > 0) e.Categoria = CategoriaResultado.Parcial;
            else e.Categoria = CategoriaResultado.Fracaso;

            e.Estado = e.Categoria == CategoriaResultado.Victoria ? EstadoSemaforo.Verde : EstadoSemaforo.Amarillo;
            e.Mensaje = e.Estado == EstadoSemaforo.Verde
                ? Idioma.T("Setup ready: it covers all the evidence the orbit requires.")
                : MensajeAmarillo(e, catalogo);
            if (orbita.instrumentosSinAlcance.Count > 0 && !string.IsNullOrEmpty(orbita.motivoSinAlcance))
                e.Mensaje += " " + orbita.MotivoSinAlcance;

            ElegirTransmisiones(e, elegidos, orbita, catalogo);
        }

        /// <summary>
        /// Presentes: TX-00 siempre; la de cada instrumento principal elegido; la de cada complementario
        /// elegido solo si la órbita desbloquea su revelación. Orden: campo «orden» del asset y después id.
        /// Ausentes: cada principal que falta; un complementario que falta solo si la órbita lo exige para
        /// la victoria (TX-A3 en elíptica), o si la órbita desbloquea su revelación y ninguna órbita lo exige
        /// para la victoria (TX-A4 y TX-A5 en polar baja). Por eso el sensor atmosférico que falta en media
        /// inclinada no se anuncia, como pide Transmisiones.md.
        /// </summary>
        static void ElegirTransmisiones(Evaluacion e, List<string> elegidos, OrbitaDatos orbita, CatalogoDatos catalogo)
        {
            var presentes = new List<TransmisionDatos>();
            var ausentes = new List<TransmisionDatos>();
            var exigidosEnAlgunaOrbita = catalogo.orbitas.SelectMany(o => o.requisitoDeVictoria).ToList();

            foreach (var t in catalogo.transmisiones)
            {
                bool esAusente = t.id.StartsWith("TX-A");
                if (string.IsNullOrEmpty(t.instrumentoId))
                {
                    presentes.Add(t); // TX-00, la de la misión
                    continue;
                }

                bool elegido = elegidos.Contains(t.instrumentoId);
                bool principal = t.tipo == TipoTransmision.Principal;
                bool desbloqueado = orbita.revelacionesDesbloqueadas.Contains(t.instrumentoId);

                if (!esAusente && elegido && (principal || desbloqueado))
                    presentes.Add(t);

                if (esAusente && !elegido)
                {
                    bool exigido = orbita.requisitoDeVictoria.Contains(t.instrumentoId);
                    bool revelacionPerdida = desbloqueado && !exigidosEnAlgunaOrbita.Contains(t.instrumentoId);
                    if (principal || exigido || revelacionPerdida)
                        ausentes.Add(t);
                }
            }

            e.Transmisiones.AddRange(presentes.OrderBy(t => t.orden).ThenBy(t => t.id).Select(t => t.id));
            e.Ausentes.AddRange(ausentes.OrderBy(t => t.id).Select(t => t.id));
        }

        /// <summary>Un instrumento es principal si su transmisión es de tipo principal (térmica y radar).</summary>
        public static bool EsPrincipal(string instrumentoId, CatalogoDatos catalogo)
        {
            return catalogo.transmisiones.Any(t => t.instrumentoId == instrumentoId && t.tipo == TipoTransmision.Principal);
        }

        static string MensajeGris(ResultadoCalculo r)
        {
            var partes = new List<string>();
            if (r.Faltantes.Contains(Faltante.Orbita)) partes.Add(Idioma.T("an orbit"));
            if (r.Faltantes.Contains(Faltante.Lanzador)) partes.Add(Idioma.T("a launcher"));
            if (r.Faltantes.Contains(Faltante.Instrumentos)) partes.Add(Idioma.T("at least one instrument"));
            return Idioma.T("Incomplete setup: you still need to choose ") + Unir(partes) + ".";
        }

        static string MensajeRojo(ResultadoCalculo r, Configuracion config, CatalogoDatos catalogo, MisionDatos mision)
        {
            var lanzador = catalogo.Lanzador(config.LanzadorId);
            var partes = new List<string>();
            if (r.LimitesSuperados.Contains(Limite.Masa))
                partes.Add(Idioma.T("the mass (") + r.MasaKg + Idioma.T(" of ") + lanzador.capacidadKg + " kg)");
            if (r.LimitesSuperados.Contains(Limite.Potencia))
                partes.Add(Idioma.T("the power (") + r.PotenciaW + Idioma.T(" of ") + mision.potenciaDisponibleW + " W)");
            if (r.LimitesSuperados.Contains(Limite.Costo))
                partes.Add(Idioma.T("the budget (") + r.CostoCr + Idioma.T(" of ") + mision.presupuestoCr + Idioma.T(" credits)"));
            if (r.LimitesSuperados.Contains(Limite.Espacios))
                partes.Add(Idioma.T("the slots (") + r.EspaciosUsados + Idioma.T(" of ") + lanzador.espacios + ")");
            var mensaje = partes.Count == 0 ? Idioma.T("Cannot launch.")
                : Idioma.T("Cannot launch: ") + Unir(partes) + (partes.Count > 1 ? Idioma.T(" exceed the limit.") : Idioma.T(" exceeds the limit."));
            if (r.LimitesSuperados.Contains(Limite.Alcance))
            {
                // Versión 6: no es «superar un límite» sino que falta frenado. Se dice cuánto y a qué órbitas sí llega.
                mensaje += Idioma.T(" Not enough braking for this orbit: it needs ") + TextosBriefing.Numero(r.DeltaVNecesarioMs) +
                           Idioma.T(" m/s and the spacecraft has ") + TextosBriefing.Numero(r.DeltaVDisponibleMs) + " m/s.";
                var alcanzables = Alcance.Alcanzables(config, catalogo, mision);
                mensaje += alcanzables.Count > 0
                    ? Idioma.T(" Within reach: ") + Unir(alcanzables.Select(o => o.Nombre).ToList()) + "."
                    : Idioma.T(" No orbit is within reach: remove mass or use a bigger launcher.");
            }
            return mensaje;
        }

        static string MensajeAmarillo(Evaluacion e, CatalogoDatos catalogo)
        {
            var faltan = e.EvidenciasFaltantes.Select(id => catalogo.Instrumento(id).PistaAsociada).ToList();
            var mensaje = Idioma.T("Missing ") + Unir(faltan) + ".";
            if (e.PistasPrincipales.Count == 0)
                mensaje += Idioma.T(" Warning: certain failure, none of the chosen instruments can observe the main anomaly.");
            return mensaje;
        }

        static string Unir(List<string> partes)
        {
            if (partes.Count <= 1) return string.Join("", partes);
            return string.Join(", ", partes.Take(partes.Count - 1)) + Idioma.T(" and ") + partes[partes.Count - 1];
        }
    }
}
