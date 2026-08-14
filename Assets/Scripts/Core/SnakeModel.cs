using System;

namespace Vibora.Core
{
    /// <summary>Qué pasó al intentar avanzar un paso.</summary>
    public enum StepOutcome : byte
    {
        Moved,
        HitWall,
        HitSelf
    }

    /// <summary>
    /// El cuerpo de la víbora y su única operación: avanzar un paso.
    /// No sabe de manzanas, ni de score, ni de tiempo. Solo dónde está y cómo se mueve.
    /// </summary>
    /// <remarks>
    /// 🧩 Buffer circular: el cuerpo vive en un array de tamaño fijo (una celda por
    /// casillero del tablero, el máximo que la víbora puede llegar a medir). Avanzar
    /// no mueve ningún dato: solo corre un índice. Es como una cinta transportadora —
    /// la cinta no se agranda, se corre. Sin esto habría que desplazar N posiciones
    /// por tick, o pedirle memoria nueva al sistema constantemente. 🔴GC
    ///
    /// El array <c>_occupied</c> es el que hace que "¿choqué conmigo?" sea mirar
    /// un casillero (O(1)) en vez de recorrer todo el cuerpo (O(n)).
    /// </remarks>
    public sealed class SnakeModel
    {
        private readonly GridModel _grid;
        private readonly GridPos[] _body;
        private readonly bool[] _occupied;

        private int _headSlot;
        private int _length;

        /// <summary>El tablero donde se mueve.</summary>
        public GridModel Grid => _grid;

        /// <summary>Largo máximo posible: la víbora llenando el tablero entero.</summary>
        public int Capacity => _body.Length;

        public int Length => _length;

        public Direction CurrentDirection { get; private set; }

        public GridPos Head => _body[_headSlot];

        /// <param name="start">Dónde nace la cabeza.</param>
        /// <param name="direction">Hacia dónde mira. El cuerpo se arma hacia atrás.</param>
        /// <param name="initialLength">Cabeza incluida.</param>
        public SnakeModel(GridModel grid, GridPos start, Direction direction, int initialLength)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));

            if (!_grid.Contains(start))
                throw new ArgumentOutOfRangeException(nameof(start), start, "La cabeza nace fuera del tablero.");
            if (initialLength < 1)
                throw new ArgumentOutOfRangeException(nameof(initialLength), initialLength, "El largo inicial debe ser >= 1.");
            if (initialLength > _grid.CellCount)
                throw new ArgumentOutOfRangeException(nameof(initialLength), initialLength, "El largo inicial no entra en el tablero.");

            _body = new GridPos[_grid.CellCount];
            _occupied = new bool[_grid.CellCount];

            _headSlot = 0;
            _length = initialLength;
            CurrentDirection = direction;

            // 📖 El cuerpo se dibuja hacia atrás desde la cabeza: si mira a la derecha,
            //    los segmentos quedan a su izquierda.
            GridPos backwards = direction.Delta();
            backwards = new GridPos(-backwards.X, -backwards.Y);

            GridPos cursor = start;
            for (int segment = 0; segment < initialLength; segment++)
            {
                if (!_grid.Contains(cursor))
                    throw new ArgumentException(
                        $"La víbora de largo {initialLength} naciendo en {start} mirando {direction} se sale del tablero en {cursor}.",
                        nameof(initialLength));

                _body[SlotOf(segment)] = cursor;
                _occupied[_grid.ToIndex(cursor)] = true;
                cursor += backwards;
            }
        }

        /// <summary>Segmento por posición: 0 es la cabeza, Length-1 es la punta de la cola.</summary>
        public GridPos GetSegment(int segment)
        {
            if (segment < 0 || segment >= _length)
                throw new ArgumentOutOfRangeException(nameof(segment), segment, $"La víbora tiene {_length} segmentos.");

            return _body[SlotOf(segment)];
        }

        /// <summary>¿La víbora ocupa esta celda? O(1). Lo va a usar el spawner de manzanas.</summary>
        public bool Occupies(GridPos position)
            => _grid.Contains(position) && _occupied[_grid.ToIndex(position)];

        /// <summary>
        /// Avanza un paso. Si el resultado no es <see cref="StepOutcome.Moved"/>,
        /// la víbora queda exactamente como estaba.
        /// </summary>
        /// <param name="requested">Dirección pedida. Un giro de 180° se ignora.</param>
        /// <param name="grow">true = comió: la cola se queda y el cuerpo crece.</param>
        public StepOutcome TryAdvance(Direction requested, bool grow)
        {
            Direction direction = Resolve(requested);
            GridPos next = Head + direction.Delta();

            if (!_grid.Contains(next))
                return StepOutcome.HitWall;

            // 📖 Si el tablero ya está lleno no hay dónde crecer (ganaste).
            bool growsNow = grow && _length < Capacity;
            bool tailVacates = !growsNow;

            // 📖 LA SUTILEZA QUE ROMPE LA MAYORÍA DE LOS CLONES DE SNAKE:
            //    la cola se va en el MISMO tick en que la cabeza avanza. Entrar a la celda
            //    que la cola está dejando es legal. Si liberás la cola después de chequear
            //    la colisión, la víbora se mata sola cada vez que se muerde la punta.
            int tailIndex = -1;
            if (tailVacates)
            {
                tailIndex = _grid.ToIndex(GetSegment(_length - 1));
                _occupied[tailIndex] = false;
            }

            int nextIndex = _grid.ToIndex(next);

            if (_occupied[nextIndex])
            {
                // 📖 Deshacer: prometimos no dejar rastro si el paso no se concreta.
                if (tailVacates)
                    _occupied[tailIndex] = true;

                return StepOutcome.HitSelf;
            }

            _headSlot = NextSlot(_headSlot);
            _body[_headSlot] = next;
            _occupied[nextIndex] = true;

            if (growsNow)
                _length++;

            CurrentDirection = direction;
            return StepOutcome.Moved;
        }

        /// <summary>
        /// A qué celda iría la cabeza con esta dirección, sin moverla.
        /// </summary>
        /// <remarks>
        /// Lo necesita el juego para saber si la víbora está por comer: <c>grow</c> se
        /// decide ANTES del paso. Es importante que use el mismo <see cref="Resolve"/>
        /// que <see cref="TryAdvance"/>: si cada uno resolviera el giro de 180° por su
        /// cuenta, tarde o temprano darían destinos distintos y la manzana se comería
        /// sola o dejaría de comerse. ⚠️SOLID
        /// </remarks>
        public GridPos PeekNext(Direction requested) => Head + Resolve(requested).Delta();

        // 📖 Girar 180° sería meter la cabeza en el propio cuello: muerte instantánea
        //    por apretar una tecla. Se ignora y la víbora sigue derecho.
        //    Con un solo segmento no hay cuello, así que ahí sí vale darse vuelta.
        private Direction Resolve(Direction requested)
            => (_length > 1 && requested.IsOpposite(CurrentDirection))
                ? CurrentDirection
                : requested;

        // 📖 El +Capacity evita un índice negativo: en C# el % de un negativo da negativo.
        private int SlotOf(int segment) => (_headSlot - segment + Capacity) % Capacity;

        private int NextSlot(int slot) => (slot + 1) % Capacity;
    }
}
