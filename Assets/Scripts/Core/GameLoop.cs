using System;

namespace Vibora.Core
{
    /// <summary>
    /// Una partida: leer el input, mover la víbora, resolver si comió y si perdió.
    /// </summary>
    /// <remarks>
    /// Recibe todas sus dependencias por constructor, así que una partida entera se
    /// simula en un test con teclas y azar de mentira, sin abrir Unity.
    /// Se expone hacia afuera como <see cref="IBoardView"/>: quien dibuja solo mira.
    /// </remarks>
    public sealed class GameLoop : IBoardView
    {
        private readonly SnakeModel _snake;
        private readonly IInputReader _input;
        private readonly IFoodSpawner _spawner;

        private GridPos _food;
        private bool _hasFood;

        public GameLoop(SnakeModel snake, IInputReader input, IFoodSpawner spawner)
        {
            _snake = snake ?? throw new ArgumentNullException(nameof(snake));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));

            SpawnFood();
        }

        public int Score { get; private set; }

        public bool IsOver { get; private set; }

        /// <summary>Ganar es llenar el tablero: no queda una sola celda libre.</summary>
        public bool IsWon { get; private set; }

        public StepOutcome LastOutcome { get; private set; } = StepOutcome.Moved;

        public int StepCount { get; private set; }

        // ---- IBoardView: solo lectura ----

        public GridModel Grid => _snake.Grid;

        public GridPos SnakeHead => _snake.Head;

        public int SnakeLength => _snake.Length;

        public bool SnakeOccupies(GridPos position) => _snake.Occupies(position);

        public bool TryGetFood(out GridPos food)
        {
            food = _food;
            return _hasFood;
        }

        // ---- La partida ----

        /// <summary>Avanza un paso. Después de terminar, no hace nada más.</summary>
        public StepOutcome Step()
        {
            if (IsOver)
                return LastOutcome;

            // 📖 Sin input pendiente se sigue derecho. El juego nunca se queda quieto.
            Direction direction = _snake.CurrentDirection;

            if (_input.TryConsumeDirection(out Direction requested))
                direction = requested;

            // 📖 Hay que mirar el destino ANTES de mover, porque 'grow' se decide antes
            //    del paso. PeekNext aplica la misma regla de giro que TryAdvance.
            bool eats = _hasFood && _snake.PeekNext(direction) == _food;

            LastOutcome = _snake.TryAdvance(direction, grow: eats);
            StepCount++;

            if (LastOutcome != StepOutcome.Moved)
            {
                IsOver = true;
                return LastOutcome;
            }

            if (!eats)
                return LastOutcome;

            Score++;

            // 📖 Respawn DESPUÉS de crecer, nunca antes: si no, la manzana nueva podría
            //    caer en la celda que la víbora acaba de ocupar y quedar inalcanzable.
            SpawnFood();

            if (!_hasFood)
            {
                // No quedó una sola celda libre: la víbora es el tablero.
                IsWon = true;
                IsOver = true;
            }

            return LastOutcome;
        }

        private void SpawnFood() => _hasFood = _spawner.TryPlace(_snake, out _food);
    }
}
