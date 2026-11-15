using NUnit.Framework;

namespace SpaceMission.PlayTests
{
    /// <summary>
    /// Versión 9: las pruebas eligen cualquier cohete, satélite, órbita o nivel, así que corren con todo desbloqueado
    /// (Progreso). Al terminar se deja el progreso que había. Las pruebas del desbloqueo ponen el suyo.
    /// </summary>
    [SetUpFixture]
    public class ProgresoAbierto
    {
        int antes;

        [OneTimeSetUp]
        public void Abrir() { antes = SpaceMission.Progreso.NivelesGanados; SpaceMission.Progreso.DesbloquearTodo(); }

        [OneTimeTearDown]
        public void Restaurar() => SpaceMission.Progreso.Poner(antes);
    }
}
