using System;

namespace SpaceMission
{
    /// <summary>
    /// Lógica pura: masa, potencia, costo, espacios, alcance y bloqueo (Gris o Rojo) de una configuración.
    /// Todos los números salen del CatalogoDatos. Un límite se supera solo si el valor es MAYOR que el límite.
    /// No escribe la línea de registro: la escribe el Evaluador, que ya conoce el semáforo final y el resultado.
    /// </summary>
    public static class Calculador
    {
        /// <param name="nivel">Datos del nivel; si es null, la misión del diseño (nivel 2).</param>
        public static ResultadoCalculo Calcular(Configuracion config, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var r = new ResultadoCalculo();

            if (string.IsNullOrEmpty(config.OrbitaId)) r.Faltantes.Add(Faltante.Orbita);
            if (string.IsNullOrEmpty(config.LanzadorId)) r.Faltantes.Add(Faltante.Lanzador);
            if (config.InstrumentoIds.Count == 0) r.Faltantes.Add(Faltante.Instrumentos);

            var mision = nivel != null ? nivel : catalogo.mision;
            var orbita = string.IsNullOrEmpty(config.OrbitaId) ? null : Buscar(catalogo.Orbita(config.OrbitaId), "órbita", config.OrbitaId);
            var lanzador = string.IsNullOrEmpty(config.LanzadorId) ? null : Buscar(catalogo.Lanzador(config.LanzadorId), "lanzador", config.LanzadorId);

            // Parciales con lo que ya se eligió (versión 6: la órbita se elige al final y las barras se mueven antes).
            r.MasaSateliteKg = MasaSatelite(config, catalogo);
            r.MasaKg = mision.masaBaseKg + r.MasaSateliteKg;
            r.PotenciaW = mision.consumoBaseW;
            r.CostoCr = mision.costoBaseCr + (lanzador != null ? lanzador.costoCr : 0) + (orbita != null ? orbita.costoCr : 0);
            foreach (var id in config.InstrumentoIds)
            {
                var instrumento = Buscar(catalogo.Instrumento(id), "instrumento", id);
                r.MasaKg += instrumento.masaKg;
                r.PotenciaW += instrumento.ConsumoTotalW;
                r.CostoCr += instrumento.costoCr;
            }
            r.EspaciosUsados = config.InstrumentoIds.Count;

            // Alcance: frenado que tiene la nave con su lanzador y el que pide la órbita (Alcance.cs).
            if (Alcance.Activo(lanzador, catalogo.reglas))
            {
                r.DeltaVDisponibleMs = Alcance.DeltaVDisponibleMs(lanzador, r.MasaKg, catalogo.reglas);
                if (Alcance.TieneDatos(orbita))
                {
                    r.AlcanceCalculado = true;
                    r.DeltaVNecesarioMs = Alcance.DeltaVInsercionMs(orbita, catalogo.reglas);
                }
            }

            if (lanzador != null && r.MasaKg > lanzador.capacidadKg) r.LimitesSuperados.Add(Limite.Masa);
            if (r.PotenciaW > mision.potenciaDisponibleW) r.LimitesSuperados.Add(Limite.Potencia);
            if (r.CostoCr > mision.presupuestoCr) r.LimitesSuperados.Add(Limite.Costo);
            if (lanzador != null && r.EspaciosUsados > lanzador.espacios) r.LimitesSuperados.Add(Limite.Espacios);
            if (r.AlcanceCalculado && r.DeltaVDisponibleMs < r.DeltaVNecesarioMs) r.LimitesSuperados.Add(Limite.Alcance);

            r.Estado = r.Faltantes.Count > 0 ? EstadoSemaforo.Gris
                : r.LimitesSuperados.Count > 0 ? EstadoSemaforo.Rojo : EstadoSemaforo.Valida;
            return r;
        }

        /// <summary>
        /// Masa seca de la nave (base + satélite + instrumentos), la que cuenta para el alcance.
        /// Ids desconocidos cuentan 0 (la masa también se muestra con configuraciones a medias).
        /// </summary>
        public static int MasaSecaKg(Configuracion config, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            var mision = nivel != null ? nivel : catalogo.mision;
            int masa = mision.masaBaseKg + MasaSatelite(config, catalogo);
            foreach (var id in config.InstrumentoIds)
            {
                var i = catalogo.Instrumento(id);
                if (i != null) masa += i.masaKg;
            }
            return masa;
        }

        /// <summary>Versión 6: el satélite elegido pesa (sin elegir, 0: el MRO de referencia no suma).</summary>
        static int MasaSatelite(Configuracion config, CatalogoDatos catalogo)
        {
            if (string.IsNullOrEmpty(config.SateliteId)) return 0;
            var s = catalogo.Satelite(config.SateliteId);
            return s != null ? s.masaKg : 0;
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
