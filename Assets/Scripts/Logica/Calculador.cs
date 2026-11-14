using System;

namespace SpaceMission
{
    /// <summary>
    /// Lógica pura: masa, potencia, costo, espacios y bloqueo (Gris o Rojo) de una configuración.
    /// Todos los números salen del CatalogoDatos. Un límite se supera solo si el valor es MAYOR que el límite.
    /// No escribe la línea de registro: la escribe el Evaluador, que ya conoce el semáforo final y el resultado.
    /// </summary>
    public static class Calculador
    {
        public static ResultadoCalculo Calcular(Configuracion config, CatalogoDatos catalogo)
        {
            var r = new ResultadoCalculo();

            if (string.IsNullOrEmpty(config.OrbitaId)) r.Faltantes.Add(Faltante.Orbita);
            if (string.IsNullOrEmpty(config.LanzadorId)) r.Faltantes.Add(Faltante.Lanzador);
            if (config.InstrumentoIds.Count == 0) r.Faltantes.Add(Faltante.Instrumentos);

            if (r.Faltantes.Count > 0)
            {
                r.Estado = EstadoSemaforo.Gris;
                return r;
            }

            var mision = catalogo.mision;
            var orbita = Buscar(catalogo.Orbita(config.OrbitaId), "órbita", config.OrbitaId);
            var lanzador = Buscar(catalogo.Lanzador(config.LanzadorId), "lanzador", config.LanzadorId);

            r.MasaKg = mision.masaBaseKg;
            r.PotenciaW = mision.consumoBaseW;
            r.CostoCr = mision.costoBaseCr + lanzador.costoCr + orbita.costoCr;
            foreach (var id in config.InstrumentoIds)
            {
                var instrumento = Buscar(catalogo.Instrumento(id), "instrumento", id);
                r.MasaKg += instrumento.masaKg;
                r.PotenciaW += instrumento.ConsumoTotalW;
                r.CostoCr += instrumento.costoCr;
            }
            r.EspaciosUsados = config.InstrumentoIds.Count;

            if (r.MasaKg > lanzador.capacidadKg) r.LimitesSuperados.Add(Limite.Masa);
            if (r.PotenciaW > mision.potenciaDisponibleW) r.LimitesSuperados.Add(Limite.Potencia);
            if (r.CostoCr > mision.presupuestoCr) r.LimitesSuperados.Add(Limite.Costo);
            if (r.EspaciosUsados > lanzador.espacios) r.LimitesSuperados.Add(Limite.Espacios);

            r.Estado = r.LimitesSuperados.Count > 0 ? EstadoSemaforo.Rojo : EstadoSemaforo.Valida;
            return r;
        }

        /// <summary>
        /// Línea de registro: [Misión] masa=620 potencia=430 costo=850 semáforo=VERDE resultado=VICTORIA.
        /// El Evaluador (fase 4) pasa el semáforo final y el resultado; si no se conocen, van el estado del cálculo y un guion.
        /// </summary>
        public static string LineaRegistro(ResultadoCalculo r, EstadoSemaforo? semaforo = null, string resultado = "-")
        {
            bool gris = r.Estado == EstadoSemaforo.Gris;
            string Valor(int v) => gris ? "-" : v.ToString();
            var estado = (semaforo ?? r.Estado).ToString().ToUpperInvariant();
            return "[Misión] masa=" + Valor(r.MasaKg) + " potencia=" + Valor(r.PotenciaW) + " costo=" + Valor(r.CostoCr)
                + " semáforo=" + estado + " resultado=" + resultado;
        }

        static T Buscar<T>(T dato, string tipo, string id) where T : class
        {
            if (dato == null)
                throw new ArgumentException("No existe " + tipo + " con id '" + id + "' en el catálogo");
            return dato;
        }
    }
}
