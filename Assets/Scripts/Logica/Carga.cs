using System.Collections.Generic;
using System.Linq;

namespace SpaceMission
{
    /// <summary>
    /// Carga de la nave (PROPUESTA-GAMEPLAY.md v3, paso 1): entre Revisión y Lanzar se eligen las herramientas
    /// que suben, pagando solo con lo que sobra de la configuración (créditos y capacidad del lanzador).
    /// La capacidad que queda libre se convierte en combustible. No cambia la evaluación ni la categoría.
    /// </summary>
    public class Carga
    {
        readonly CatalogoDatos catalogo;
        readonly ReglasOperacionDatos reglas;
        readonly int creditosSobrantes;
        readonly int masaLibreKg;
        readonly Configuracion config;
        readonly MisionDatos nivel;

        public const string IdMro = "mro";

        /// <summary>Satélite elegido en la Configuración; sin elegir, el MRO (el neutro). Null si el catálogo no tiene satélites.</summary>
        public SateliteDatos Satelite { get; private set; }

        public readonly List<string> Herramientas = new List<string>();

        public Carga(Evaluacion evaluacion, Configuracion config, CatalogoDatos catalogo, MisionDatos nivel = null)
        {
            this.catalogo = catalogo;
            this.config = config;
            reglas = catalogo.reglas;
            Satelite = (!string.IsNullOrEmpty(config.SateliteId) ? catalogo.Satelite(config.SateliteId) : null)
                       ?? catalogo.Satelite(IdMro);
            nivel = nivel != null ? nivel : catalogo.mision;
            this.nivel = nivel;
            var lanzador = catalogo.Lanzador(config.LanzadorId);
            creditosSobrantes = System.Math.Max(0, nivel.presupuestoCr - evaluacion.Calculo.CostoCr);
            // La masa del cálculo ya incluye el satélite (versión 6); aquí se descuenta aparte (MasaLibreKg),
            // para que cambiarlo por el MRO en la Carga libere su masa sin contarla dos veces.
            masaLibreKg = lanzador == null ? 0
                : System.Math.Max(0, lanzador.capacidadKg - (evaluacion.Calculo.MasaKg - evaluacion.Calculo.MasaSateliteKg));
        }

        int CostoSatelite => Satelite != null ? Satelite.costoCr : 0;
        int MasaSatelite => Satelite != null ? Satelite.masaKg : 0;

        /// <summary>Lo que cuesta la carga: el satélite y las herramientas.</summary>
        public int CreditosGastados => CostoSatelite + Herramientas.Sum(id => catalogo.Herramienta(id).costoCr);
        public int CreditosDisponibles => creditosSobrantes - CreditosGastados;
        public int MasaLibreKg => masaLibreKg - MasaSatelite - Herramientas.Sum(id => catalogo.Herramienta(id).masaKg);

        /// <summary>«Lanzar» se bloquea si lo que sobra no alcanza para el satélite y las herramientas.</summary>
        public bool PuedeLanzar => CreditosDisponibles >= 0 && MasaLibreKg >= 0;

        /// <summary>Cambia al MRO (no cuesta ni pesa). Lo usa el botón «Usar el MRO» de la Carga.</summary>
        public void UsarMro()
        {
            var mro = catalogo.Satelite(IdMro);
            if (mro == null) return;
            Satelite = mro;
            config.SateliteId = IdMro;
        }
        public int EspaciosLibres => reglas.espaciosBodega - Herramientas.Count;

        /// <summary>Combustible con el que sale la nave: 1 unidad por cada N kg libres, con tope.</summary>
        public int Combustible => System.Math.Max(0, System.Math.Min(reglas.combustibleMaximo, MasaLibreKg / reglas.kgPorCombustible));

        public bool Lleva(string id) => Herramientas.Contains(id);

        public bool PuedeCargar(string id)
        {
            var h = catalogo.Herramienta(id);
            return h != null && !Lleva(id) && EspaciosLibres > 0
                   && h.costoCr <= CreditosDisponibles && h.masaKg <= MasaLibreKg && AlcanzaCon(h.masaKg);
        }

        /// <summary>
        /// ¿La nave sigue llegando a su órbita con «extraKg» más? La masa seca es la de la Configuración (base,
        /// satélite actual e instrumentos) más las herramientas: cada herramienta resta frenado (versión 6).
        /// </summary>
        public bool AlcanzaCon(int extraKg = 0)
        {
            var orbita = string.IsNullOrEmpty(config.OrbitaId) ? null : catalogo.Orbita(config.OrbitaId);
            var lanzador = string.IsNullOrEmpty(config.LanzadorId) ? null : catalogo.Lanzador(config.LanzadorId);
            int masaSeca = Calculador.MasaSecaKg(config, catalogo, nivel) + Herramientas.Sum(h => catalogo.Herramienta(h).masaKg) + extraKg;
            return Alcance.Evaluar(lanzador, orbita, masaSeca, reglas).Alcanza;
        }

        public bool Cargar(string id)
        {
            if (!PuedeCargar(id)) return false;
            Herramientas.Add(id);
            return true;
        }

        public void Quitar(string id) => Herramientas.Remove(id);
    }
}
