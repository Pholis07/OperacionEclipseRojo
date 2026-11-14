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
        public static Evaluacion Evaluar(Configuracion config, CatalogoDatos catalogo)
        {
            return Evaluar(config, Calculador.Calcular(config, catalogo), catalogo);
        }

        public static Evaluacion Evaluar(Configuracion config, ResultadoCalculo calculo, CatalogoDatos catalogo)
        {
            var e = new Evaluacion { Calculo = calculo, Estado = calculo.Estado };

            if (calculo.Estado == EstadoSemaforo.Gris)
                e.Mensaje = MensajeGris(calculo);
            else if (calculo.Estado == EstadoSemaforo.Rojo)
                e.Mensaje = MensajeRojo(calculo, config, catalogo);
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
            var elegidos = config.InstrumentoIds;

            e.SePuedeLanzar = true;
            e.PistasPrincipales.AddRange(elegidos.Where(id => EsPrincipal(id, catalogo)));
            e.EvidenciasFaltantes.AddRange(orbita.requisitoDeVictoria.Where(id => !elegidos.Contains(id)));

            if (e.EvidenciasFaltantes.Count == 0) e.Categoria = CategoriaResultado.Victoria;
            else if (e.PistasPrincipales.Count > 0) e.Categoria = CategoriaResultado.Parcial;
            else e.Categoria = CategoriaResultado.Fracaso;

            e.Estado = e.Categoria == CategoriaResultado.Victoria ? EstadoSemaforo.Verde : EstadoSemaforo.Amarillo;
            e.Mensaje = e.Estado == EstadoSemaforo.Verde
                ? "Configuración lista: cubre todas las evidencias que pide la órbita."
                : MensajeAmarillo(e, catalogo);

            ElegirTransmisiones(e, config, orbita, catalogo);
        }

        /// <summary>
        /// Presentes: TX-00 siempre; la de cada instrumento principal elegido; la de cada complementario
        /// elegido solo si la órbita desbloquea su revelación. Orden: campo «orden» del asset y después id.
        /// Ausentes: cada principal que falta; un complementario que falta solo si la órbita lo exige para
        /// la victoria (TX-A3 en elíptica), o si la órbita desbloquea su revelación y ninguna órbita lo exige
        /// para la victoria (TX-A4 y TX-A5 en polar baja). Por eso el sensor atmosférico que falta en media
        /// inclinada no se anuncia, como pide Transmisiones.md.
        /// </summary>
        static void ElegirTransmisiones(Evaluacion e, Configuracion config, OrbitaDatos orbita, CatalogoDatos catalogo)
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

                bool elegido = config.InstrumentoIds.Contains(t.instrumentoId);
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
            if (r.Faltantes.Contains(Faltante.Orbita)) partes.Add("una órbita");
            if (r.Faltantes.Contains(Faltante.Lanzador)) partes.Add("un lanzador");
            if (r.Faltantes.Contains(Faltante.Instrumentos)) partes.Add("al menos un instrumento");
            return "Configuración incompleta: falta elegir " + Unir(partes) + ".";
        }

        static string MensajeRojo(ResultadoCalculo r, Configuracion config, CatalogoDatos catalogo)
        {
            var mision = catalogo.mision;
            var lanzador = catalogo.Lanzador(config.LanzadorId);
            var partes = new List<string>();
            if (r.LimitesSuperados.Contains(Limite.Masa))
                partes.Add("la masa (" + r.MasaKg + " de " + lanzador.capacidadKg + " kg)");
            if (r.LimitesSuperados.Contains(Limite.Potencia))
                partes.Add("la potencia (" + r.PotenciaW + " de " + mision.potenciaDisponibleW + " W)");
            if (r.LimitesSuperados.Contains(Limite.Costo))
                partes.Add("el presupuesto (" + r.CostoCr + " de " + mision.presupuestoCr + " créditos)");
            if (r.LimitesSuperados.Contains(Limite.Espacios))
                partes.Add("los espacios (" + r.EspaciosUsados + " de " + lanzador.espacios + ")");
            return "No se puede lanzar: se supera " + Unir(partes) + ".";
        }

        static string MensajeAmarillo(Evaluacion e, CatalogoDatos catalogo)
        {
            var faltan = e.EvidenciasFaltantes.Select(id => catalogo.Instrumento(id).pistaAsociada).ToList();
            var mensaje = "Falta " + Unir(faltan) + ".";
            if (e.PistasPrincipales.Count == 0)
                mensaje += " Aviso de fracaso seguro: ningún instrumento elegido puede observar la anomalía principal.";
            return mensaje;
        }

        static string Unir(List<string> partes)
        {
            if (partes.Count <= 1) return string.Join("", partes);
            return string.Join(", ", partes.Take(partes.Count - 1)) + " y " + partes[partes.Count - 1];
        }
    }
}
