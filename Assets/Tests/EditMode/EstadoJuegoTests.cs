using NUnit.Framework;

namespace SpaceMission.Tests
{
    public class EstadoJuegoTests
    {
        [Test]
        public void Reiniciar_DejaElEstadoVacio()
        {
            EstadoJuego.Configuracion = new Configuracion("polar", "titan", new[] { "termica" });

            EstadoJuego.Reiniciar();

            Assert.IsNull(EstadoJuego.Configuracion.OrbitaId);
            Assert.IsNull(EstadoJuego.Configuracion.LanzadorId);
            Assert.IsEmpty(EstadoJuego.Configuracion.InstrumentoIds);
        }
    }
}
