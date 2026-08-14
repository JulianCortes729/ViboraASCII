using System;

namespace Vibora.Core
{
    /// <summary>Decide en qué celda libre aparece la próxima manzana.</summary>
    public interface IFoodSpawner
    {
        /// <summary>
        /// Busca una celda libre. Devuelve false si no queda ninguna
        /// (la víbora llenó el tablero: ganaste).
        /// </summary>
        bool TryPlace(SnakeModel snake, out GridPos position);
    }

    /// <summary>
    /// Contar y elegir: cuenta las celdas libres, sortea un índice entre ellas
    /// y recorre hasta llegar a esa.
    /// </summary>
    /// <remarks>
    /// 🧩 La alternativa obvia sería tirar celdas al azar hasta pegarle a una libre.
    /// Anda bien con el tablero vacío y empeora justo cuando la víbora es larga: con el
    /// 95% ocupado, cada manzana cuesta ~20 intentos, y encima con costo impredecible.
    /// Este método siempre hace dos pasadas: más caro cuando el tablero está vacío,
    /// pero nunca sorprende. En un juego, un costo constante y aburrido vale más que
    /// un promedio bajo con picos.
    /// </remarks>
    public sealed class FoodSpawner : IFoodSpawner
    {
        private readonly GridModel _grid;
        private readonly IRandom _random;

        public FoodSpawner(GridModel grid, IRandom random)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool TryPlace(SnakeModel snake, out GridPos position)
        {
            if (snake == null)
                throw new ArgumentNullException(nameof(snake));

            // --- Pasada 1: cuántas celdas libres hay ---
            int freeCells = 0;

            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    if (!snake.Occupies(new GridPos(x, y)))
                        freeCells++;
                }
            }

            if (freeCells == 0)
            {
                position = default;
                return false;
            }

            // 📖 Se sortea entre las LIBRES, no entre todas. Por eso no hace falta reintentar:
            //    cualquier número que salga corresponde sí o sí a una celda válida.
            int target = _random.Next(freeCells);

            // --- Pasada 2: llegar a la libre número 'target' ---
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    var candidate = new GridPos(x, y);

                    if (snake.Occupies(candidate))
                        continue;

                    if (target == 0)
                    {
                        position = candidate;
                        return true;
                    }

                    target--;
                }
            }

            // 📖 Inalcanzable: la pasada 2 recorre exactamente las mismas celdas que
            //    contó la pasada 1. Está por si alguien rompe esa invariante en el futuro.
            position = default;
            return false;
        }
    }
}
