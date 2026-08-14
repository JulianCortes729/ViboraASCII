using System;

namespace Vibora.Core
{
    /// <summary>
    /// Una coordenada de celda. Reemplaza a Vector2Int, que vive en UnityEngine
    /// y por lo tanto no puede entrar en esta capa.
    /// </summary>
    /// <remarks>
    /// Convención de ejes: <b>Y = 0 es la fila de ARRIBA</b> y crece hacia abajo,
    /// igual que las líneas de un texto. Así el renderer recorre la grilla en el
    /// mismo orden en que escribe los caracteres, sin invertir nada.
    /// </remarks>
    // 📖 readonly struct: dato sin identidad, se copia por valor y no aloca en el heap.
    // 📖 IEquatable<GridPos>: sin esto, cada comparación pasaría por el Equals(object)
    //    de la clase base, que hace boxing -> basura para el GC en pleno tick. 🔴GC
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is GridPos other && Equals(other);

        // 📖 397 es un primo: mezcla los bits de X e Y para que (1,2) y (2,1) no colisionen.
        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);

        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.X + b.X, a.Y + b.Y);

        public override string ToString() => $"({X},{Y})";
    }
}
