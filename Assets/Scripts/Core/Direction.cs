namespace Vibora.Core
{
    /// <summary>Las cuatro direcciones de movimiento.</summary>
    /// <remarks>
    /// El orden NO es arbitrario: están en sentido horario, así que la dirección
    /// opuesta siempre está a distancia 2. Eso convierte "¿es un giro de 180°?"
    /// en una cuenta, sin switch ni tabla.
    /// </remarks>
    public enum Direction : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class DirectionExtensions
    {
        /// <summary>Cuánto se desplaza la cabeza en un paso.</summary>
        // 📖 Up resta en Y porque Y=0 es la fila de arriba (ver GridPos).
        public static GridPos Delta(this Direction direction) => direction switch
        {
            Direction.Up => new GridPos(0, -1),
            Direction.Right => new GridPos(1, 0),
            Direction.Down => new GridPos(0, 1),
            Direction.Left => new GridPos(-1, 0),
            _ => new GridPos(0, 0)
        };

        /// <summary>¿Son direcciones opuestas? (Up/Down o Left/Right)</summary>
        // 📖 +2 y vuelta al principio: gracias al orden horario del enum.
        public static bool IsOpposite(this Direction a, Direction b)
            => (byte)a == ((byte)b + 2) % 4;
    }
}
