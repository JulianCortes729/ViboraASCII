using System;

namespace Vibora.Core
{
    /// <summary>
    /// El tablero: sus dimensiones y las cuentas para moverse entre coordenada e índice.
    /// No guarda qué hay en cada celda — eso es de quien ocupa el tablero, no del tablero.
    /// </summary>
    public sealed class GridModel
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>Cantidad total de celdas. Es el techo absoluto del largo de la víbora.</summary>
        public int CellCount { get; }

        public GridModel(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), width, "El ancho debe ser >= 1.");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), height, "El alto debe ser >= 1.");

            Width = width;
            Height = height;
            CellCount = width * height; // 📖 cacheado: se consulta seguido y nunca cambia.
        }

        public bool Contains(GridPos position)
            => position.X >= 0 && position.X < Width
            && position.Y >= 0 && position.Y < Height;

        /// <summary>Coordenada -> índice plano. Sin validar: los callers ya usaron Contains.</summary>
        // 📖 Aplanar la grilla a un solo array es más rápido que un array de arrays:
        //    una sola reserva de memoria y todo contiguo (mejor uso de la caché del CPU).
        public int ToIndex(GridPos position) => (position.Y * Width) + position.X;

        /// <summary>Índice plano -> coordenada.</summary>
        public GridPos FromIndex(int index) => new GridPos(index % Width, index / Width);
    }
}
