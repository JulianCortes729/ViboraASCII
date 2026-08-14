namespace Vibora.Core
{
    /// <summary>
    /// El tablero visto por quien solo tiene derecho a mirarlo.
    /// </summary>
    /// <remarks>
    /// 🧩 Es una <b>interfaz de solo lectura</b>. El renderer recibe esto y no el
    /// <see cref="GameLoop"/> entero: así no tiene forma de llamar a Step() ni de
    /// modificar la víbora, ni por error ni por atajo apurado. La capa que dibuja
    /// queda incapaz de cambiar el juego — garantizado por el compilador, no por
    /// buena voluntad. ⚠️SOLID
    /// </remarks>
    public interface IBoardView
    {
        GridModel Grid { get; }

        GridPos SnakeHead { get; }

        int SnakeLength { get; }

        bool SnakeOccupies(GridPos position);

        /// <summary>False si no hay manzana en el tablero (se ganó la partida).</summary>
        bool TryGetFood(out GridPos food);

        int Score { get; }

        bool IsOver { get; }

        bool IsWon { get; }
    }
}
