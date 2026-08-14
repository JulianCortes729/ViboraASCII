using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Core.Tests
{
    /// <summary>Teclado de mentira: entrega las teclas que le digamos, en orden.</summary>
    internal sealed class ScriptedInputReader : IInputReader
    {
        private readonly Queue<Direction> _scripted = new Queue<Direction>();

        public ScriptedInputReader(params Direction[] directions)
        {
            foreach (Direction direction in directions)
                _scripted.Enqueue(direction);
        }

        public bool TryConsumeDirection(out Direction direction)
        {
            if (_scripted.Count == 0)
            {
                direction = default;
                return false;
            }

            direction = _scripted.Dequeue();
            return true;
        }

        public void Clear() => _scripted.Clear();
    }

    /// <summary>Tablero sin manzanas: para probar el movimiento sin que la comida moleste.</summary>
    internal sealed class NoFoodSpawner : IFoodSpawner
    {
        public bool TryPlace(SnakeModel snake, out GridPos position)
        {
            position = default;
            return false;
        }
    }

    /// <summary>Pone las manzanas exactamente donde le decimos, en orden.</summary>
    internal sealed class ScriptedFoodSpawner : IFoodSpawner
    {
        private readonly Queue<GridPos> _positions = new Queue<GridPos>();

        public ScriptedFoodSpawner(params GridPos[] positions)
        {
            foreach (GridPos position in positions)
                _positions.Enqueue(position);
        }

        public bool TryPlace(SnakeModel snake, out GridPos position)
        {
            if (_positions.Count == 0)
            {
                position = default;
                return false;
            }

            position = _positions.Dequeue();
            return true;
        }
    }

    [TestFixture]
    public sealed class GameLoopTests
    {
        private static GameLoop Loop(IInputReader input, IFoodSpawner? spawner = null, int length = 3)
        {
            var grid = new GridModel(10, 10);
            var snake = new SnakeModel(grid, new GridPos(5, 5), Direction.Right, length);
            return new GameLoop(snake, input, spawner ?? new NoFoodSpawner());
        }

        // ---------- movimiento ----------

        [Test]
        public void SinInput_SigueDerecho()
        {
            var loop = Loop(new ScriptedInputReader());

            loop.Step();
            loop.Step();

            Assert.AreEqual(new GridPos(7, 5), loop.SnakeHead);
        }

        [Test]
        public void ConInput_AplicaUnGiroPorPaso()
        {
            var loop = Loop(new ScriptedInputReader(Direction.Down, Direction.Left));

            loop.Step();
            Assert.AreEqual(new GridPos(5, 6), loop.SnakeHead);

            loop.Step();
            Assert.AreEqual(new GridPos(4, 6), loop.SnakeHead, "la esquina en L salió entera");
        }

        // ---------- comer ----------

        [Test]
        public void ComerLaManzana_CreceYSuma()
        {
            // Manzana justo enfrente, y otra lejos para el respawn.
            var loop = Loop(
                new ScriptedInputReader(),
                new ScriptedFoodSpawner(new GridPos(6, 5), new GridPos(0, 0)));

            Assert.AreEqual(3, loop.SnakeLength);
            Assert.AreEqual(0, loop.Score);

            loop.Step();

            Assert.AreEqual(4, loop.SnakeLength, "la víbora creció");
            Assert.AreEqual(1, loop.Score);
        }

        [Test]
        public void ComerLaManzana_ApareceUnaNueva()
        {
            var loop = Loop(
                new ScriptedInputReader(),
                new ScriptedFoodSpawner(new GridPos(6, 5), new GridPos(0, 0)));

            loop.Step();

            Assert.IsTrue(loop.TryGetFood(out GridPos nueva));
            Assert.AreEqual(new GridPos(0, 0), nueva, "la manzana se mudó");
        }

        [Test]
        public void PasarAlLado_NoEsComer()
        {
            // Manzana en (6,6): la víbora pasa por (6,5), no la toca.
            var loop = Loop(new ScriptedInputReader(), new ScriptedFoodSpawner(new GridPos(6, 6)));

            loop.Step();

            Assert.AreEqual(3, loop.SnakeLength);
            Assert.AreEqual(0, loop.Score);
            Assert.IsTrue(loop.TryGetFood(out GridPos sigue));
            Assert.AreEqual(new GridPos(6, 6), sigue, "la manzana sigue donde estaba");
        }

        [Test]
        public void ComerVariasSeguidas_AcumulaScore()
        {
            var loop = Loop(
                new ScriptedInputReader(),
                new ScriptedFoodSpawner(new GridPos(6, 5), new GridPos(7, 5), new GridPos(8, 5), new GridPos(0, 0)));

            loop.Step();
            loop.Step();
            loop.Step();

            Assert.AreEqual(3, loop.Score);
            Assert.AreEqual(6, loop.SnakeLength, "3 iniciales + 3 comidas");
        }

        [Test]
        public void GiroDe180_IgnoradoTambienParaDecidirSiCome()
        {
            // La víbora va a la derecha, se pide un giro de 180° (que se ignora) y la
            // manzana está adelante. Si GameLoop calculara el destino con la dirección
            // PEDIDA en vez de la resuelta, miraría la celda de atrás y no comería.
            var loop = Loop(
                new ScriptedInputReader(Direction.Left),
                new ScriptedFoodSpawner(new GridPos(6, 5), new GridPos(0, 0)));

            loop.Step();

            Assert.AreEqual(new GridPos(6, 5), loop.SnakeHead, "siguió a la derecha");
            Assert.AreEqual(1, loop.Score, "comió la manzana que tenía adelante");
            Assert.AreEqual(4, loop.SnakeLength);
        }

        // ---------- fin de partida ----------

        [Test]
        public void ChocarLaPared_TerminaLaPartida()
        {
            var loop = Loop(new ScriptedInputReader());

            for (int i = 0; i < 4; i++)
                Assert.AreEqual(StepOutcome.Moved, loop.Step(), $"paso {i}");

            Assert.AreEqual(StepOutcome.HitWall, loop.Step());
            Assert.IsTrue(loop.IsOver);
            Assert.IsFalse(loop.IsWon, "chocar no es ganar");
        }

        [Test]
        public void DespuesDePerder_LosPasosNoHacenNada()
        {
            var loop = Loop(new ScriptedInputReader());

            while (!loop.IsOver)
                loop.Step();

            int pasos = loop.StepCount;
            var cabeza = loop.SnakeHead;

            loop.Step();
            loop.Step();

            Assert.AreEqual(pasos, loop.StepCount);
            Assert.AreEqual(cabeza, loop.SnakeHead);
        }

        [Test]
        public void LlenarElTablero_EsGanar()
        {
            // Tablero de 1x2 con el spawner de verdad: la víbora nace en (0,0),
            // la única celda libre es (0,1), se la come y no queda lugar para otra.
            var grid = new GridModel(1, 2);
            var snake = new SnakeModel(grid, new GridPos(0, 0), Direction.Down, 1);
            var loop = new GameLoop(snake, new ScriptedInputReader(), new FoodSpawner(grid, new SystemRandom(1)));

            Assert.IsTrue(loop.TryGetFood(out GridPos manzana));
            Assert.AreEqual(new GridPos(0, 1), manzana, "la única celda libre");

            Assert.AreEqual(StepOutcome.Moved, loop.Step());

            Assert.IsTrue(loop.IsWon, "llenó el tablero");
            Assert.IsTrue(loop.IsOver);
            Assert.AreEqual(2, loop.SnakeLength);
            Assert.IsFalse(loop.TryGetFood(out _), "ya no hay dónde poner una manzana");
        }

        // ---------- construcción ----------

        [Test]
        public void DependenciasNulas_Explotan()
        {
            var grid = new GridModel(10, 10);
            var snake = new SnakeModel(grid, new GridPos(5, 5), Direction.Right, 3);

            Assert.Throws<ArgumentNullException>(() => new GameLoop(snake, null!, new NoFoodSpawner()));
            Assert.Throws<ArgumentNullException>(() => new GameLoop(null!, new ScriptedInputReader(), new NoFoodSpawner()));
            Assert.Throws<ArgumentNullException>(() => new GameLoop(snake, new ScriptedInputReader(), null!));
        }

        [Test]
        public void AlEmpezar_YaHayUnaManzana()
        {
            var grid = new GridModel(10, 10);
            var snake = new SnakeModel(grid, new GridPos(5, 5), Direction.Right, 3);
            var loop = new GameLoop(snake, new ScriptedInputReader(), new FoodSpawner(grid, new SystemRandom(7)));

            Assert.IsTrue(loop.TryGetFood(out GridPos manzana), "la partida arranca con comida en el tablero");
            Assert.IsFalse(loop.SnakeOccupies(manzana), "y no debajo de la víbora");
        }
    }
}
