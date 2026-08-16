using System.IO;
using UnityEngine;
using Vibora.Core;
using Vibora.Presentation;

namespace Vibora.Infrastructure
{
    /// <summary>
    /// El composition root: el único lugar del juego que sabe quién usa a quién.
    /// </summary>
    /// <remarks>
    /// 🧩 Todas las demás clases reciben lo que necesitan por constructor y no salen a
    /// buscar nada (nada de FindObjectOfType, nada de singletons). El precio es que
    /// alguien tiene que armar el rompecabezas: ese alguien es este archivo. La ventaja:
    /// para cambiar cómo está cableado el juego, hay un solo lugar donde mirar.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Tablero (en celdas)")]
        [SerializeField, Range(8, 120)] private int _gridWidth = 40;
        [SerializeField, Range(8, 80)] private int _gridHeight = 25;

        [Header("Víbora inicial")]
        [SerializeField, Range(1, 10)] private int _initialLength = 3;
        [SerializeField] private Direction _initialDirection = Direction.Right;

        [Header("Velocidad")]
        [Tooltip("Pasos por segundo al arrancar.")]
        [SerializeField, Range(1f, 20f)] private float _baseTicksPerSecond = 8f;

        [Tooltip("Cuánto acelera por manzana comida.")]
        [SerializeField, Range(0f, 2f)] private float _speedPerFood = 0.25f;

        [Tooltip("Techo de velocidad. Sin esto, a las 40 manzanas es injugable.")]
        [SerializeField, Range(1f, 30f)] private float _maxTicksPerSecond = 18f;

        [Header("Azar")]
        [Tooltip("Con semilla fija la partida es siempre igual: sirve para reproducir un bug.")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed = 12345;

        [Header("Piezas de la escena")]
        [SerializeField] private TickDriver? _tickDriver;
        [SerializeField] private InputSystemReader? _inputReader;
        [SerializeField] private AsciiRenderer? _renderer;
        [SerializeField] private SfxPlayer? _sfx;

        private GameLoop? _loop;
        private SpeedCurve? _speed;
        private GridModel? _grid;
        private HighScoreTracker? _highScoreTracker;
        private bool _needsRedraw;
        private const string ScoreFileName = "highscore.json";
        private readonly GameStateMachine _states = new GameStateMachine();


        // 📖 Awake SOLO valida. Unity no garantiza en qué orden corre el Awake de
        //    GameObjects distintos: si acá tocáramos al renderer podríamos llegar antes
        //    que su propio Awake. Lo garantizado es que TODOS los Awake terminan antes
        //    de que empiece cualquier Start. Por eso el cableado va en Start.
        private void Awake()
        {
            if (_tickDriver == null || _inputReader == null || _renderer == null)
            {
                Debug.LogError($"[{nameof(GameBootstrap)}] Faltan referencias en el Inspector. El juego no arranca.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_tickDriver != null)
                _tickDriver.Ticked += OnTick;
        }

        private void OnDisable()
        {
            if (_tickDriver != null)
                _tickDriver.Ticked -= OnTick;
        }

        private void Start()
        {
            if (_tickDriver == null || _inputReader == null || _renderer == null)
                return;

            _speed = new SpeedCurve(_baseTicksPerSecond, _speedPerFood, _maxTicksPerSecond);

            // 📖 El tablero se crea una sola vez: sus dimensiones no cambian entre
            //    partidas, y recrearlo obligaría a recalcular el fontSize al pedo.
            _grid = new GridModel(_gridWidth, _gridHeight);
            _renderer.Initialize(_grid);

            string scorePath = Path.Combine(Application.persistentDataPath, ScoreFileName);
            _highScoreTracker = new HighScoreTracker(new JsonScoreRepository(scorePath));

            BuildNewGame();
            _tickDriver.Pause();
        }

        private void Update()
        {
            if (_inputReader == null)
                return;

            if (_inputReader.ConsumeConfirm())
                Apply(_states.Confirm());

            if (_inputReader.ConsumePause())
                Apply(_states.TogglePause());

         
        }

        /// <summary>
        /// Recrea todos los modelos y el GameLoop para empezar una partida nueva.
        /// </summary>
        /// <remarks>
        /// Recrea los modelos en vez de resetearlos in-place. Son ~9 KB de allocations
        /// por muerte, no por frame: no vale la pena el código extra de un Reset(). 🟡PERF
        /// </remarks>
        private void BuildNewGame()
        {
            if (_grid == null || _speed == null || _tickDriver == null || _inputReader == null || _renderer == null || _highScoreTracker == null)
                return;

            var start = new GridPos(_gridWidth / 2, _gridHeight / 2);
            var snake = new SnakeModel(_grid, start, _initialDirection, _initialLength);

            IRandom random = _useFixedSeed ? new SystemRandom(_seed) : new SystemRandom();
            var spawner = new FoodSpawner(_grid, random);

            // 📖 Acá se ve la inyección: GameLoop recibe un IInputReader y un IFoodSpawner,
            //    no las clases concretas. No sabe que atrás hay un teclado ni de dónde
            //    sale el azar.
            _loop = new GameLoop(snake, _inputReader, spawner);

            // 📖 Limpiar el input pendiente: los giros que quedaron encolados mientras
            //    la víbora agonizaba no tienen por qué aplicarse a la partida nueva.
            _inputReader.Clear();

            _tickDriver.TicksPerSecond = _speed.For(0);


            _needsRedraw = true;
        }

        private void OnTick()
        {
            if (_loop == null || _renderer == null || _speed == null || _tickDriver == null || _highScoreTracker == null)
                return;

            int scoreAntes = _loop.Score;
            StepOutcome outcome = _loop.Step();
            bool comio = _loop.Score > scoreAntes;

            // 📖 La velocidad se recalcula desde el score, no se incrementa al comer.
            //    Así no hay estado que se pueda desincronizar: mismo score, misma velocidad,
            //    siempre — incluso después de reiniciar.
            _tickDriver.TicksPerSecond = _speed.For(_loop.Score);

            if (comio)
                _sfx?.PlayEat();

            // 📖 El récord se actualiza ANTES de dibujar: así el último frame —el que queda
            //    congelado en pantalla— muestra el récord nuevo y no el anterior.
            //    El && corta a la izquierda: si la partida sigue, Submit ni se llama.
            bool esRecord = _loop.IsOver && _highScoreTracker.Submit(_loop.Score);

            _needsRedraw = true;

            if (!_loop.IsOver)
                return;

            // 📖 El tick se frena acá y no adentro del GameLoop: el Core no sabe que
            //    existe un metrónomo, y no tiene por qué saberlo.
            Apply(_states.NotifyGameOver());

            if (esRecord)
                _sfx?.PlayRecord();
            else
                _sfx?.PlayDead();

            Debug.Log(esRecord
                    ? $"[{nameof(GameBootstrap)}] ¡Nuevo récord! {_loop.Score}"
                    : $"[{nameof(GameBootstrap)}] Récord actual: {_highScoreTracker.Best}");

            string motivo = _loop.IsWon ? "GANASTE" : outcome.ToString();
            Debug.Log($"[{nameof(GameBootstrap)}] Fin: {motivo} · score {_loop.Score} · {_loop.StepCount} pasos. Enter para reiniciar.");
            
        }

        private void Apply(StateChange change)
        {
            switch (change)
            {
                case StateChange.GameStarted:
                    BuildNewGame();
                    _tickDriver?.Resume();
                    break;
                case StateChange.Paused:
                    _tickDriver?.Pause();
                    _needsRedraw = true;
                    break;
                case StateChange.Resumed:
                    // 📖 Durante la pausa el lector siguió encolando giros: el player loop de Unity
                    //    no se entera de que el metrónomo está frenado. Se descartan al reanudar.
                    _inputReader?.Clear();
                    _tickDriver?.Resume();
                    _needsRedraw = true;
                    break;
                case StateChange.Ended:
                    _tickDriver?.Pause();
                    break;
                case StateChange.None:
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!_needsRedraw) return;
            
            _needsRedraw = false;
            Draw();
            
        }

        private void Draw()
        {
            if (_loop == null || _renderer == null || _highScoreTracker == null)
                return;

            _renderer.Render(_loop, _highScoreTracker.Best, _states.Current);
        }
    }
}
