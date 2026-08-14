namespace Vibora.Core
{
    /// <summary>
    /// Lo único que el juego necesita saber del input: "¿hay un giro pendiente?".
    /// </summary>
    /// <remarks>
    /// 🧩 Esto es un <b>puerto</b>: el Core declara qué necesita, sin decir de dónde sale.
    /// El Input System, un bot de IA o un test que devuelve teclas de mentira son todos
    /// implementaciones válidas. Sin esta interfaz, probar el movimiento exigiría un
    /// teclado y un humano apretando teclas.
    /// </remarks>
    public interface IInputReader
    {
        /// <summary>
        /// Saca el próximo giro de la cola. Devuelve false si no hay ninguno pendiente
        /// (en ese caso <paramref name="direction"/> no significa nada).
        /// </summary>
        bool TryConsumeDirection(out Direction direction);

        /// <summary>Descarta lo pendiente. Para usar al reiniciar la partida.</summary>
        void Clear();
    }
}
