using UnityEngine;

namespace SpaceMission
{
    /// <summary>
    /// Eclipse Ink (versión 8): enciende o apaga los contornos de tinta del shader Tinta según la preferencia del
    /// jugador (Opciones → «Contornos de tinta»). Apagados es el modo ligero: las bandas de luz se conservan y la
    /// pasada del contorno no dibuja nada. Se aplica al arrancar y cada vez que cambian las preferencias.
    /// </summary>
    public static class ContornosTinta
    {
        static readonly int SinContorno = Shader.PropertyToID("_TintaSinContorno");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Iniciar()
        {
            Preferencias.Cambiaron -= Aplicar;
            Preferencias.Cambiaron += Aplicar;
            Aplicar();
        }

        public static void Aplicar() => Shader.SetGlobalFloat(SinContorno, Preferencias.ContornosTinta ? 0f : 1f);
    }
}
