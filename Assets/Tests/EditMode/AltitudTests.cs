using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 3, paso 2 (PROPUESTA-GAMEPLAY.md): combustible y altitud.</summary>
    public class AltitudTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 A015 = new Vector2(0.65f, 0.5f);   // a 0.15 de la anomalía
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        /// <summary>Operación sin eventos y con la anomalía en el centro; la carga sale de la configuración.</summary>
        Operacion Nueva(Configuracion c, MisionDatos nivel = null, params string[] herramientas)
        {
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var carga = new Carga(e, c, catalogo, nivel);
            foreach (var h in herramientas) Assert.IsTrue(carga.Cargar(h), h);
            var op = new Operacion(e, c, catalogo, nivel, 1, carga);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Encender("termica");
            return op;
        }

        Operacion Caso2(params string[] herramientas) =>
            Nueva(new Configuracion("media", "explorador", new[] { "termica", "radar" }), null, herramientas);   // 3 de combustible

        Operacion TitanNivel1() =>
            Nueva(new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" }), catalogo.Nivel(0));   // 8

        [Test]
        public void ReglasDeAltitud()
        {
            var r = catalogo.reglas;
            Assert.AreEqual(3, r.combustiblePorCambioAltitud);
            Assert.AreEqual(1.5f, r.factorAltitudBaja);
            Assert.AreEqual(1, r.combustiblePorOrbitaBaja);
            Assert.AreEqual(1, r.orbitasPerdidasSinCombustible);
            Assert.AreEqual(10, r.puntosPorCombustible);
        }

        [Test]
        public void SeEmpiezaEnNormalConElCombustibleDeLaCarga()
        {
            var op = Caso2();
            Assert.AreEqual(Altitud.Normal, op.Altitud);
            Assert.AreEqual(3, op.Combustible);
        }

        [Test]
        public void CambiarDeAltitudCuestaTresDeCombustible()
        {
            var op = TitanNivel1();
            Assert.IsTrue(op.CambiarAltitud(Altitud.Baja));
            Assert.AreEqual(Altitud.Baja, op.Altitud);
            Assert.AreEqual(5, op.Combustible);
            Assert.IsFalse(op.CambiarAltitud(Altitud.Baja), "ya está en baja");
            Assert.AreEqual(5, op.Combustible);
        }

        [Test]
        public void SinCombustibleNoSePuedeBajar()
        {
            var op = Caso2("bateria", "reconfiguracion");   // 0 kg libres
            Assert.AreEqual(0, op.Combustible);
            Assert.IsFalse(op.CambiarAltitud(Altitud.Baja));
            Assert.AreEqual(Altitud.Normal, op.Altitud);
        }

        [Test]
        public void EnBajaLasLecturasAlcanzanMasLejos()
        {
            var normal = TitanNivel1();
            Assert.AreEqual(Lectura.Caliente, normal.Escanear("termica", A015));

            var baja = TitanNivel1();
            baja.CambiarAltitud(Altitud.Baja);
            Assert.AreEqual(Lectura.Encontrada, baja.Escanear("termica", A015));
        }

        [Test]
        public void EnBajaElArrastreGastaUnoPorOrbita()
        {
            var op = TitanNivel1();
            op.CambiarAltitud(Altitud.Baja);   // 5
            for (int i = 0; i < 3; i++) op.Escanear("termica", Lejos);
            Assert.AreEqual(2, op.Combustible);
            Assert.AreEqual(9, op.Orbitas);
            Assert.IsFalse(op.CambiarAltitud(Altitud.Normal), "2 no alcanzan para subir");
        }

        [Test]
        public void SinCombustibleEnBajaLaNaveSubeSolaYPierdeUnaOrbita()
        {
            var op = Caso2();
            op.CambiarAltitud(Altitud.Baja);   // 3 − 3 = 0
            op.Escanear("termica", Lejos);
            Assert.AreEqual(Altitud.Normal, op.Altitud);
            Assert.AreEqual(1, op.SubidasForzadas);
            Assert.AreEqual(8, op.Orbitas, "1 del escaneo + 1 por subir");
            op.Escanear("termica", Lejos);
            Assert.AreEqual(7, op.Orbitas, "en normal ya no hay arrastre");
        }

        [Test]
        public void ElCombustibleSobranteDaDiezPuntosPorUnidad()
        {
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" });
            var op = TitanNivel1();
            op.Terminar();
            var linea = Puntuacion.Desglose(Evaluador.Evaluar(c, catalogo, catalogo.Nivel(0)), op, catalogo, catalogo.Nivel(0))
                .First(l => l.Concepto.StartsWith("Combustible"));
            Assert.AreEqual(80, linea.Puntos);
        }
    }
}
