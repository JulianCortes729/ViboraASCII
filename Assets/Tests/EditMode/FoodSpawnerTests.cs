using System;
using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Core.Tests
{
    /// <summary>Azar de mentira: devuelve los números que le digamos, en orden.</summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly int[] _values;
        private int _index;

        public ScriptedRandom(params int[] values) => _values = values;

        public int Next(int maxExclusive)
        {
            if (_index >= _values.Length)
                throw new InvalidOperationException("El test pidió más números de los que guionó.");

            return _values[_index++];
        }
    }

    [TestFixture]
    public sealed class FoodSpawnerTests
    {
        // Víbora horizontal en (3,5)(4,5)(5,5). En el barrido por filas esas son
        // las celdas 53, 54 y 55: las primeras 53 libres son las celdas 0 a 52.
        private static SnakeModel Snake(GridModel grid)
            => new SnakeModel(grid, new GridPos(5, 5), Direction.Right, 3);

        [Test]
        public void SorteaEntreLasLibres_NoEntreTodas()
        {
            var grid = new GridModel(10, 10);
            var snake = Snake(grid);

            // La libre número 0 es la celda (0,0).
            var spawner = new FoodSpawner(grid, new ScriptedRandom(0));

            Assert.IsTrue(spawner.TryPlace(snake, out GridPos position));
            Assert.AreEqual(new GridPos(0, 0), position);
        }

        [Test]
        public void SaltaLasCeldasOcupadas()
        {
            var grid = new GridModel(10, 10);
            var snake = Snake(grid);

            // La libre número 53 NO es la celda 53 (ocupada por la cola): es la 56.
            var spawner = new FoodSpawner(grid, new ScriptedRandom(53));

            Assert.IsTrue(spawner.TryPlace(snake, out GridPos position));
            Assert.AreEqual(new GridPos(6, 5), position, "saltó las tres celdas de la víbora");
        }

        [Test]
        public void NingunSorteoCaeNuncaSobreLaVibora()
        {
            var grid = new GridModel(10, 10);
            var snake = Snake(grid);
            const int libres = 100 - 3;

            for (int k = 0; k < libres; k++)
            {
                var spawner = new FoodSpawner(grid, new ScriptedRandom(k));

                Assert.IsTrue(spawner.TryPlace(snake, out GridPos position), $"sorteo {k}");
                Assert.IsFalse(snake.Occupies(position), $"el sorteo {k} cayó sobre la víbora en {position}");
                Assert.IsTrue(grid.Contains(position), $"el sorteo {k} cayó fuera del tablero");
            }
        }

        [Test]
        public void CadaSorteoDaUnaCeldaDistinta()
        {
            var grid = new GridModel(10, 10);
            var snake = Snake(grid);
            const int libres = 100 - 3;

            var vistas = new bool[grid.CellCount];

            for (int k = 0; k < libres; k++)
            {
                new FoodSpawner(grid, new ScriptedRandom(k)).TryPlace(snake, out GridPos position);

                int index = grid.ToIndex(position);
                Assert.IsFalse(vistas[index], $"la celda {position} salió dos veces");
                vistas[index] = true;
            }
        }

        [Test]
        public void TableroLleno_NoHayDondePonerla()
        {
            // Tablero 1x3 con una víbora de 3: no queda ni una celda.
            var grid = new GridModel(1, 3);
            var snake = new SnakeModel(grid, new GridPos(0, 2), Direction.Down, 3);
            var spawner = new FoodSpawner(grid, new ScriptedRandom());

            Assert.IsFalse(spawner.TryPlace(snake, out _), "sin celdas libres no hay manzana");
        }

        [Test]
        public void DependenciasNulas_Explotan()
        {
            var grid = new GridModel(10, 10);

            Assert.Throws<ArgumentNullException>(() => new FoodSpawner(null!, new ScriptedRandom(0)));
            Assert.Throws<ArgumentNullException>(() => new FoodSpawner(grid, null!));
            Assert.Throws<ArgumentNullException>(
                () => new FoodSpawner(grid, new ScriptedRandom(0)).TryPlace(null!, out _));
        }
    }

    [TestFixture]
    public sealed class SnakeModelPeekTests
    {
        private static SnakeModel Snake(int length)
            => new SnakeModel(new GridModel(10, 10), new GridPos(5, 5), Direction.Right, length);

        [Test]
        public void PeekNext_DevuelveLaCeldaDeAdelante()
        {
            Assert.AreEqual(new GridPos(6, 5), Snake(3).PeekNext(Direction.Right));
        }

        [Test]
        public void PeekNext_AplicaLaMismaReglaDe180QueTryAdvance()
        {
            var snake = Snake(3);

            Assert.AreEqual(new GridPos(6, 5), snake.PeekNext(Direction.Left),
                "el giro de 180° se ignora también acá");
        }

        [Test]
        public void PeekNext_ConUnSoloSegmento_SiPermiteDarseVuelta()
        {
            Assert.AreEqual(new GridPos(4, 5), Snake(1).PeekNext(Direction.Left));
        }

        [Test]
        public void PeekNext_NoMueveNada()
        {
            var snake = Snake(3);

            snake.PeekNext(Direction.Down);
            snake.PeekNext(Direction.Up);

            Assert.AreEqual(new GridPos(5, 5), snake.Head, "mirar no mueve");
            Assert.AreEqual(Direction.Right, snake.CurrentDirection);
        }

        [Test]
        public void PeekNext_CoincideConDondeTerminaTryAdvance()
        {
            foreach (Direction pedida in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var snake = Snake(3);
                GridPos previsto = snake.PeekNext(pedida);

                Assert.AreEqual(StepOutcome.Moved, snake.TryAdvance(pedida, grow: false), $"dirección {pedida}");
                Assert.AreEqual(previsto, snake.Head, $"PeekNext y TryAdvance discrepan en {pedida}");
            }
        }
    }
}
