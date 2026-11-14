using NUnit.Framework;

namespace SpaceMission.Tests
{
    public class EstadoJuegoTests
    {
        [Test]
        public void Reiniciar_DejaElEstadoVacio()
        {
            EstadoJuego.OrbitaId = "polar";
            EstadoJuego.LanzadorId = "titan";
            EstadoJuego.InstrumentoIds.Add("termica");

            EstadoJuego.Reiniciar();

            Assert.IsNull(EstadoJuego.OrbitaId);
            Assert.IsNull(EstadoJuego.LanzadorId);
            Assert.IsEmpty(EstadoJuego.InstrumentoIds);
        }
    }
}
