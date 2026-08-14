using System;

namespace Vibora.Core
{
    /// <summary>
    /// Cola FIFO de giros pendientes, de tamaño fijo.
    /// </summary>
    /// <remarks>
    /// 🧩 El problema que resuelve: el juego avanza ~8 veces por segundo, pero el teclado
    /// se lee ~60. Entre dos pasos de la víbora entran varias teclas. Sin cola, la última
    /// pisa a la anterior y una esquina en L rápida sale mal — el jugador siente que el
    /// juego "no le tomó" la tecla. Con cola de 2 podés dejar la esquina pre-programada:
    /// el primer giro entra en este paso y el segundo en el siguiente.
    ///
    /// No es una <c>Queue&lt;T&gt;</c> porque el array de tamaño fijo garantiza cero
    /// allocations pase lo que pase, incluso si el jugador aporrea el teclado. 🔴GC
    /// </remarks>
    public sealed class DirectionBuffer
    {
        private readonly Direction[] _slots;

        private int _writeSlot;
        private int _readSlot;
        private int _count;

        public int Capacity => _slots.Length;

        public int Count => _count;

        public bool IsFull => _count == _slots.Length;

        public DirectionBuffer(int capacity = 2)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "La capacidad debe ser >= 1.");

            _slots = new Direction[capacity];
        }

        /// <summary>Encola un giro. Devuelve false si se descartó.</summary>
        public bool TryEnqueue(Direction direction)
        {
            // 📖 Repetir el último encolado no aporta nada y ocuparía un lugar que sirve
            //    para un giro de verdad. Mantener Right apretado no debe llenar la cola.
            if (_count > 0 && LastQueued() == direction)
                return false;

            // 📖 Cola llena: se descarta el nuevo, no el viejo. Lo que el jugador pidió
            //    primero es lo que quiere que pase primero.
            if (IsFull)
                return false;

            _slots[_writeSlot] = direction;
            _writeSlot = NextSlot(_writeSlot);
            _count++;
            return true;
        }

        public bool TryDequeue(out Direction direction)
        {
            if (_count == 0)
            {
                direction = default;
                return false;
            }

            direction = _slots[_readSlot];
            _readSlot = NextSlot(_readSlot);
            _count--;
            return true;
        }

        /// <summary>Mira el próximo a salir sin sacarlo.</summary>
        public bool TryPeek(out Direction direction)
        {
            if (_count == 0)
            {
                direction = default;
                return false;
            }

            direction = _slots[_readSlot];
            return true;
        }

        public void Clear()
        {
            _writeSlot = 0;
            _readSlot = 0;
            _count = 0;
        }

        private Direction LastQueued() => _slots[(_writeSlot - 1 + Capacity) % Capacity];

        private int NextSlot(int slot) => (slot + 1) % Capacity;
    }
}
