using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceMission.Tests
{
    /// <summary>Versión 3, paso 4 (PROPUESTA-GAMEPLAY.md): eventos con decisiones y los eventos nuevos.</summary>
    public class DecisionesTests
    {
        static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Lejos = new Vector2(0.95f, 0.95f);

        CatalogoDatos catalogo;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDatos>("Assets/Datos/Catalogo.asset");
        }

        /// <summary>Titán en elíptica, nivel 1 (8 de combustible sin herramientas). Anomalía en el centro, térmica encendida.</summary>
        Operacion Nueva(params string[] herramientas)
        {
            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" });
            var nivel = catalogo.Nivel(0);
            var e = Evaluador.Evaluar(c, catalogo, nivel);
            var carga = new Carga(e, c, catalogo, nivel);
            foreach (var h in herramientas) Assert.IsTrue(carga.Cargar(h), h);
            var op = new Operacion(e, c, catalogo, nivel, 1, carga);
            op.ProgramarEventos(new EventoDatos[0], new int[0]);
            op.PonerAnomalia(Centro);
            op.Encender("termica");
            return op;
        }

        EventoDatos Evento(string id) => catalogo.eventos.First(e => e.id == id);
        static OpcionEvento Con(Operacion op, EfectoOpcion efecto) => op.Opciones.First(o => o.efecto == efecto);

        [Test]
        public void UnEventoPausaLaOperacionHastaDecidir()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("sombra"));
            Assert.IsTrue(op.EsperandoDecision);
            Assert.AreEqual(800, op.LimiteW, "el efecto espera a la decisión");
            Assert.IsNull(op.Escanear("termica", Lejos));
            Assert.IsFalse(op.CambiarAltitud(Altitud.Baja));
            Assert.IsTrue(op.Decidir(Con(op, EfectoOpcion.Aguantar)));
            Assert.IsFalse(op.EsperandoDecision);
            Assert.AreEqual(650, op.LimiteW);
            Assert.IsNotNull(op.Escanear("termica", Lejos));
        }

        [Test]
        public void LosEventosProgramadosPidenDecisionTrasSuEscaneo()
        {
            var op = Nueva();
            op.ProgramarEventos(new[] { Evento("orientacion") }, new[] { 1 });
            op.Escanear("termica", Lejos);
            Assert.IsTrue(op.EsperandoDecision);
            Assert.AreEqual("orientacion", op.UltimoEvento.Datos.id);
        }

        [Test]
        public void LaRespuestaDeHerramientaSoloApareceSiSeCargo()
        {
            var sin = Nueva();
            sin.AplicarEvento(Evento("sombra"));
            Assert.AreEqual(2, sin.Opciones.Count());
            Assert.IsFalse(sin.Opciones.Any(o => o.efecto == EfectoOpcion.UsarHerramienta));

            var con = Nueva("bateria");
            con.AplicarEvento(Evento("sombra"));
            Assert.AreEqual(3, con.Opciones.Count());
            Assert.IsTrue(con.Decidir(Con(con, EfectoOpcion.UsarHerramienta)));
            Assert.AreEqual(800, con.LimiteW);
            CollectionAssert.Contains(con.HerramientasUsadas.ToList(), "bateria");

            con.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(con.PuedeElegir(Con(con, EfectoOpcion.UsarHerramienta)), "ya se usó");
        }

        [Test]
        public void ManiobrarAcortaLaSombraYGastaCombustible()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("sombra"));
            Assert.IsTrue(op.Decidir(Con(op, EfectoOpcion.Acortar)));
            Assert.AreEqual(4, op.Combustible);
            Assert.AreEqual(650, op.LimiteW);
            op.Escanear("termica", Lejos);
            Assert.AreEqual(800, op.LimiteW, "dura 1 órbita");
        }

        [Test]
        public void SinCombustibleNoSePuedeManiobrar()
        {
            var op = Nueva();
            op.CambiarAltitud(Altitud.Baja);   // 8 − 3 = 5
            op.Escanear("termica", Lejos);     // arrastre: 4
            op.Escanear("termica", Lejos);     // 3
            op.AplicarEvento(Evento("sombra"));
            Assert.IsFalse(op.PuedeElegir(Con(op, EfectoOpcion.Acortar)), "pide 4");
            Assert.IsFalse(op.Decidir(Con(op, EfectoOpcion.Acortar)));
            Assert.IsTrue(op.EsperandoDecision);
        }

        [Test]
        public void DesviarLaCorrienteResuelveElMicrometeoritoPorUnaOrbita()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("micrometeorito"));
            op.Decidir(Con(op, EfectoOpcion.Resolver));
            Assert.AreEqual(800, op.LimiteW);
            Assert.AreEqual(11, op.Orbitas);
        }

        [Test]
        public void GirarLaNaveAcortaElBloqueoDelSobrecalentamiento()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("sobrecalentamiento"));
            op.Decidir(Con(op, EfectoOpcion.Acortar));
            Assert.AreEqual(6, op.Combustible);
            Assert.IsFalse(op.Encender("termica"));
            op.Encender("radar");
            op.Escanear("radar", Lejos);
            Assert.IsTrue(op.Encender("termica"), "el bloqueo duró 1 órbita");
        }

        [Test]
        public void CorregirConPropulsoresEvitaLaPerdidaDeLectura()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("orientacion"));
            op.Decidir(Con(op, EfectoOpcion.Resolver));
            Assert.AreEqual(5, op.Combustible);
            Assert.AreEqual(Lectura.Encontrada, op.Escanear("termica", Centro));
        }

        [Test]
        public void LaVentanaDespejadaDaUnaOrbitaOAbreElContacto()
        {
            var a = Nueva();
            a.AplicarEvento(Evento("ventana"));
            a.Decidir(Con(a, EfectoOpcion.GanarOrbitas));
            Assert.AreEqual(13, a.Orbitas);

            var b = Nueva();
            b.Escanear("termica", Centro);
            b.AplicarEvento(Evento("ventana"));
            b.Decidir(Con(b, EfectoOpcion.AbrirContacto));
            Assert.IsTrue(b.ContactoAbierto);
            Assert.IsTrue(b.Enviar());
        }

        [Test]
        public void LaPeticionDeLaNasaAceptadaDaPuntosAlEscanearSuPunto()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("peticion"));
            op.Decidir(Con(op, EfectoOpcion.AceptarObjetivo));
            Assert.IsTrue(op.PeticionAceptada);
            op.PonerPuntoPeticion(new Vector2(0.2f, 0.2f));
            op.Escanear("termica", Lejos);
            Assert.IsFalse(op.PeticionCumplida);
            op.Escanear("termica", new Vector2(0.22f, 0.2f));
            Assert.IsTrue(op.PeticionCumplida);

            var c = new Configuracion("eliptica", "titan", new[] { "termica", "radar", "atmosferico" });
            var linea = Puntuacion.Desglose(Evaluador.Evaluar(c, catalogo, catalogo.Nivel(0)), op, catalogo, catalogo.Nivel(0))
                .First(l => l.Concepto.StartsWith("Petición"));
            Assert.AreEqual(150, linea.Puntos);
        }

        [Test]
        public void RechazarLaPeticionNoCambiaNada()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("peticion"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            Assert.IsFalse(op.PeticionAceptada);
            Assert.AreEqual(12, op.Orbitas);
            Assert.AreEqual(8, op.Combustible);
        }

        [Test]
        public void EnLaTormentaSePuedeBajarGratis()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(Con(op, EfectoOpcion.BajarAltitud));
            Assert.AreEqual(Altitud.Baja, op.Altitud);
            Assert.AreEqual(8, op.Combustible);
        }

        [Test]
        public void SiSeSigueEnLaTormentaElProximoFrioSeLeeTibio()
        {
            var op = Nueva();
            op.AplicarEvento(Evento("tormenta"));
            op.Decidir(Con(op, EfectoOpcion.Aguantar));
            Assert.AreEqual(Lectura.Tibio, op.Escanear("termica", Lejos), "lectura falsa");
            Assert.AreEqual(Lectura.Frio, op.Escanear("termica", Lejos), "solo el próximo");
        }

        [Test]
        public void SinCargaLosEventosNuevosNoSalenAlAzarYLosDemasSeAplicanAlMomento()
        {
            var c = new Configuracion("media", "explorador", new[] { "termica", "radar" });
            var e = Evaluador.Evaluar(c, catalogo, catalogo.Nivel(2));
            for (int semilla = 0; semilla < 30; semilla++)
            {
                var op = new Operacion(e, c, catalogo, catalogo.Nivel(2), semilla);
                op.PonerAnomalia(new Vector2(-5, -5));
                op.Encender("termica");
                for (int i = 0; i < 8 && !op.Terminada; i++)
                {
                    if (!op.Encender("termica")) op.UsarHerramienta("refrigeracion");
                    op.Escanear("termica", Lejos);
                    Assert.IsFalse(op.EsperandoDecision);
                }
                Assert.IsFalse(op.Eventos.Any(x => x.Datos.efecto == EfectoEvento.Peticion || x.Datos.efecto == EfectoEvento.Tormenta));
            }
        }
    }
}
