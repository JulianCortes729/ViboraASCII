using System;
using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class SnakeModelTests
    {
        // Tablero chico a propósito: los bordes quedan cerca y los casos límite
        // se escriben en dos líneas en vez de en veinte.
        private static GridModel Grid(int w = 10, int h = 10) => new GridModel(w, h);

        private static SnakeModel Snake(
            GridModel grid,
            int x = 5, int y = 5,
            Direction direction = Direction.Right,
            int length = 3)
            => new SnakeModel(grid, new GridPos(x, y), direction, length);

        // ---------- construcción ----------

        [Test]
        public void Nace_ConLaCabezaEnLaPosicionPedida()
        {
            var snake = Snake(Grid());

            Assert.AreEqual(new GridPos(5, 5), snake.Head);
            Assert.AreEqual(3, snake.Length);
        }

        [Test]
        public void Nace_ConElCuerpoExtendidoHaciaAtras()
        {
            var snake = Snake(Grid(), direction: Direction.Right, length: 3);

            Assert.AreEqual(new GridPos(5, 5), snake.GetSegment(0), "cabeza");
            Assert.AreEqual(new GridPos(4, 5), snake.GetSegment(1), "medio");
            Assert.AreEqual(new GridPos(3, 5), snake.GetSegment(2), "cola");
        }

        [Test]
        public void Nace_MarcandoTodasSusCeldasComoOcupadas()
        {
            var snake = Snake(Grid(), direction: Direction.Right, length: 3);

            Assert.IsTrue(snake.Occupies(new GridPos(5, 5)));
            Assert.IsTrue(snake.Occupies(new GridPos(4, 5)));
            Assert.IsTrue(snake.Occupies(new GridPos(3, 5)));
            Assert.IsFalse(snake.Occupies(new GridPos(2, 5)), "una celda antes de la cola");
        }

        [Test]
        public void Nace_ExplotaSiElCuerpoNoEntraEnElTablero()
        {
            // Cabeza pegada al borde izquierdo mirando a la derecha => la cola cae afuera.
            Assert.Throws<ArgumentException>(
                () => new SnakeModel(Grid(), new GridPos(0, 5), Direction.Right, 3));
        }

        // ---------- ejes ----------

        [Test]
        public void Up_RestaEnY_PorqueLaFilaCeroEsLaDeArriba()
        {
            var snake = Snake(Grid(), direction: Direction.Up, length: 1);

            Assert.AreEqual(StepOutcome.Moved, snake.TryAdvance(Direction.Up, grow: false));
            Assert.AreEqual(new GridPos(5, 4), snake.Head);
        }

        [Test]
        public void Down_SumaEnY()
        {
            var snake = Snake(Grid(), direction: Direction.Down, length: 1);

            snake.TryAdvance(Direction.Down, grow: false);

            Assert.AreEqual(new GridPos(5, 6), snake.Head);
        }

        // ---------- avanzar ----------

        [Test]
        public void Avanzar_SinComer_MantieneElLargo()
        {
            var snake = Snake(Grid());

            var outcome = snake.TryAdvance(Direction.Right, grow: false);

            Assert.AreEqual(StepOutcome.Moved, outcome);
            Assert.AreEqual(3, snake.Length);
            Assert.AreEqual(new GridPos(6, 5), snake.Head);
        }

        [Test]
        public void Avanzar_SinComer_LiberaLaCeldaDeLaCola()
        {
            var snake = Snake(Grid());

            snake.TryAdvance(Direction.Right, grow: false);

            Assert.IsFalse(snake.Occupies(new GridPos(3, 5)), "la cola vieja debe quedar libre");
            Assert.IsTrue(snake.Occupies(new GridPos(6, 5)), "la cabeza nueva debe estar ocupada");
        }

        [Test]
        public void Avanzar_Comiendo_CreceYLaColaSeQueda()
        {
            var snake = Snake(Grid());

            snake.TryAdvance(Direction.Right, grow: true);

            Assert.AreEqual(4, snake.Length);
            Assert.IsTrue(snake.Occupies(new GridPos(3, 5)), "la cola NO se mueve cuando come");
            Assert.AreEqual(new GridPos(3, 5), snake.GetSegment(3));
        }

        [Test]
        public void Avanzar_ElCuerpoSigueALaCabeza()
        {
            var snake = Snake(Grid());

            snake.TryAdvance(Direction.Right, grow: false);

            Assert.AreEqual(new GridPos(6, 5), snake.GetSegment(0));
            Assert.AreEqual(new GridPos(5, 5), snake.GetSegment(1), "el segmento 1 es la cabeza anterior");
            Assert.AreEqual(new GridPos(4, 5), snake.GetSegment(2));
        }

        // ---------- paredes ----------

        [Test]
        public void Pared_DerechaIzquierdaArribaAbajo_DevuelvenHitWall()
        {
            var grid = Grid();

            Assert.AreEqual(StepOutcome.HitWall,
                Snake(grid, x: 9, y: 5, Direction.Right, 3).TryAdvance(Direction.Right, false), "derecha");
            Assert.AreEqual(StepOutcome.HitWall,
                Snake(grid, x: 0, y: 5, Direction.Left, 3).TryAdvance(Direction.Left, false), "izquierda");
            Assert.AreEqual(StepOutcome.HitWall,
                Snake(grid, x: 5, y: 0, Direction.Up, 3).TryAdvance(Direction.Up, false), "arriba");
            Assert.AreEqual(StepOutcome.HitWall,
                Snake(grid, x: 5, y: 9, Direction.Down, 3).TryAdvance(Direction.Down, false), "abajo");
        }

        [Test]
        public void Pared_NoDejaRastro_LaViboraQuedaIntacta()
        {
            var snake = Snake(Grid(), x: 9, y: 5, direction: Direction.Right, length: 3);

            snake.TryAdvance(Direction.Right, grow: false);

            Assert.AreEqual(new GridPos(9, 5), snake.Head, "la cabeza no se movió");
            Assert.AreEqual(3, snake.Length);
            Assert.IsTrue(snake.Occupies(new GridPos(7, 5)), "la cola sigue en su lugar");
        }

        // ---------- morderse ----------

        [Test]
        public void Morderse_DevuelveHitSelf()
        {
            // Víbora de 5 mirando a la derecha: cuerpo en (5,5)(4,5)(3,5)(2,5)(1,5).
            // Baja, va a la izquierda, y sube: la cabeza vuelve sobre el cuerpo.
            var snake = Snake(Grid(), x: 5, y: 5, direction: Direction.Right, length: 5);

            Assert.AreEqual(StepOutcome.Moved, snake.TryAdvance(Direction.Down, grow: true));  // (5,6)
            Assert.AreEqual(StepOutcome.Moved, snake.TryAdvance(Direction.Left, grow: true));  // (4,6)
            Assert.AreEqual(StepOutcome.HitSelf, snake.TryAdvance(Direction.Up, grow: false)); // (4,5) = cuerpo
        }

        [Test]
        public void Morderse_NoDejaRastro_LaViboraQuedaIntacta()
        {
            var snake = Snake(Grid(), x: 5, y: 5, direction: Direction.Right, length: 5);
            snake.TryAdvance(Direction.Down, grow: true);
            snake.TryAdvance(Direction.Left, grow: true);

            int largoAntes = snake.Length;
            var cabezaAntes = snake.Head;

            snake.TryAdvance(Direction.Up, grow: false);

            Assert.AreEqual(largoAntes, snake.Length);
            Assert.AreEqual(cabezaAntes, snake.Head);
            // Y el cuerpo que casi mordía sigue marcado como ocupado.
            Assert.IsTrue(snake.Occupies(new GridPos(4, 5)));
        }

        /// <summary>
        /// EL test que justifica todo el cuidado con el orden de las operaciones:
        /// la cola se va en el mismo tick en que la cabeza llega. Perseguirse la
        /// punta de la cola es legal, no es morderse.
        /// </summary>
        [Test]
        public void PerseguirseLaCola_EsLegal_PorqueLaColaSeVaEnElMismoTick()
        {
            // Cuadrado de 4: cabeza (5,5), cuerpo (4,5)(4,6)(5,6). La cola está en (5,6).
            var grid = Grid();
            var snake = new SnakeModel(grid, new GridPos(5, 5), Direction.Up, 1);
            snake.TryAdvance(Direction.Left, grow: true);  // (4,5) largo 2
            snake.TryAdvance(Direction.Down, grow: true);  // (4,6) largo 3
            snake.TryAdvance(Direction.Right, grow: true); // (5,6) largo 4
            Assert.AreEqual(new GridPos(5, 5), snake.GetSegment(3), "la cola quedó en (5,5)");

            // La cabeza está en (5,6) y sube a (5,5): justo la celda que la cola libera ahora.
            var outcome = snake.TryAdvance(Direction.Up, grow: false);

            Assert.AreEqual(StepOutcome.Moved, outcome, "esto NO es morderse");
            Assert.AreEqual(new GridPos(5, 5), snake.Head);
        }

        [Test]
        public void PerseguirseLaCola_SiComeEnEseTick_SiEsMorderse()
        {
            // Mismo caso, pero creciendo: la cola NO se va, así que la celda sigue ocupada.
            var grid = Grid();
            var snake = new SnakeModel(grid, new GridPos(5, 5), Direction.Up, 1);
            snake.TryAdvance(Direction.Left, grow: true);
            snake.TryAdvance(Direction.Down, grow: true);
            snake.TryAdvance(Direction.Right, grow: true);

            var outcome = snake.TryAdvance(Direction.Up, grow: true);

            Assert.AreEqual(StepOutcome.HitSelf, outcome);
        }

        // ---------- giro de 180° ----------

        [Test]
        public void Giro180_SeIgnora_YSigueDerecho()
        {
            var snake = Snake(Grid(), direction: Direction.Right, length: 3);

            var outcome = snake.TryAdvance(Direction.Left, grow: false);

            Assert.AreEqual(StepOutcome.Moved, outcome);
            Assert.AreEqual(new GridPos(6, 5), snake.Head, "siguió a la derecha, no se dio vuelta");
            Assert.AreEqual(Direction.Right, snake.CurrentDirection);
        }

        [Test]
        public void Giro180_ConUnSoloSegmento_SiEstaPermitido()
        {
            var snake = Snake(Grid(), direction: Direction.Right, length: 1);

            var outcome = snake.TryAdvance(Direction.Left, grow: false);

            Assert.AreEqual(StepOutcome.Moved, outcome);
            Assert.AreEqual(new GridPos(4, 5), snake.Head, "sin cuello, darse vuelta es legal");
            Assert.AreEqual(Direction.Left, snake.CurrentDirection);
        }

        [Test]
        public void Giro90_CambiaLaDireccionActual()
        {
            var snake = Snake(Grid(), direction: Direction.Right, length: 3);

            snake.TryAdvance(Direction.Down, grow: false);

            Assert.AreEqual(Direction.Down, snake.CurrentDirection);
            Assert.AreEqual(new GridPos(5, 6), snake.Head);
        }

        // ---------- direcciones ----------

        [Test]
        public void IsOpposite_SoloParaLosParesReales()
        {
            Assert.IsTrue(Direction.Up.IsOpposite(Direction.Down));
            Assert.IsTrue(Direction.Down.IsOpposite(Direction.Up));
            Assert.IsTrue(Direction.Left.IsOpposite(Direction.Right));
            Assert.IsTrue(Direction.Right.IsOpposite(Direction.Left));

            Assert.IsFalse(Direction.Up.IsOpposite(Direction.Left));
            Assert.IsFalse(Direction.Up.IsOpposite(Direction.Right));
            Assert.IsFalse(Direction.Up.IsOpposite(Direction.Up));
        }

        // ---------- grilla ----------

        [Test]
        public void Grid_IndiceYCoordenadaSonElMismoDatoEnDosFormatos()
        {
            var grid = Grid(7, 4);

            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(i, grid.ToIndex(grid.FromIndex(i)), $"índice {i}");
        }

        [Test]
        public void Grid_ContainsRechazaLoQueEstaAfuera()
        {
            var grid = Grid(10, 10);

            Assert.IsTrue(grid.Contains(new GridPos(0, 0)));
            Assert.IsTrue(grid.Contains(new GridPos(9, 9)));
            Assert.IsFalse(grid.Contains(new GridPos(-1, 0)));
            Assert.IsFalse(grid.Contains(new GridPos(10, 0)));
            Assert.IsFalse(grid.Contains(new GridPos(0, 10)));
        }

        // ---------- caso extremo ----------

        [Test]
        public void LlenarElTableroEntero_NoRompeElBufferCircular()
        {
            // Tablero 1xN: la víbora avanza en línea recta comiendo siempre,
            // hasta ocupar cada celda. Es el caso que revienta un buffer mal indexado.
            var grid = new GridModel(1, 8);
            var snake = new SnakeModel(grid, new GridPos(0, 0), Direction.Down, 1);

            for (int step = 1; step < 8; step++)
                Assert.AreEqual(StepOutcome.Moved, snake.TryAdvance(Direction.Down, grow: true), $"paso {step}");

            Assert.AreEqual(8, snake.Length);
            Assert.AreEqual(snake.Capacity, snake.Length, "la víbora llena el tablero");

            for (int y = 0; y < 8; y++)
                Assert.IsTrue(snake.Occupies(new GridPos(0, y)), $"celda (0,{y})");

            Assert.AreEqual(StepOutcome.HitWall, snake.TryAdvance(Direction.Down, grow: false));
        }
    }
}
