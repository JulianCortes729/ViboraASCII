using System;
using NUnit.Framework;
using UnityEngine;
using Vibora.Core;
using Vibora.Infrastructure;

namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class DirectionBufferTests
    {
        [Test]
        public void Vacia_NoEntregaNada()
        {
            var buffer = new DirectionBuffer(2);

            Assert.IsFalse(buffer.TryDequeue(out _));
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void SaleEnElOrdenEnQueEntro()
        {
            var buffer = new DirectionBuffer(2);
            buffer.TryEnqueue(Direction.Up);
            buffer.TryEnqueue(Direction.Left);

            Assert.IsTrue(buffer.TryDequeue(out Direction primero));
            Assert.IsTrue(buffer.TryDequeue(out Direction segundo));

            Assert.AreEqual(Direction.Up, primero, "primero el que apretó primero");
            Assert.AreEqual(Direction.Left, segundo);
            Assert.IsFalse(buffer.TryDequeue(out _), "y nada más");
        }

        [Test]
        public void Llena_DescartaElNuevoYConservaLosViejos()
        {
            var buffer = new DirectionBuffer(2);
            buffer.TryEnqueue(Direction.Up);
            buffer.TryEnqueue(Direction.Left);

            Assert.IsFalse(buffer.TryEnqueue(Direction.Down), "el tercero no entra");

            buffer.TryDequeue(out Direction primero);
            Assert.AreEqual(Direction.Up, primero, "los que ya estaban no se pisaron");
        }

        [Test]
        public void DescartaElRepetidoConsecutivo()
        {
            var buffer = new DirectionBuffer(2);

            Assert.IsTrue(buffer.TryEnqueue(Direction.Right));
            Assert.IsFalse(buffer.TryEnqueue(Direction.Right), "mantener la tecla no llena la cola");
            Assert.AreEqual(1, buffer.Count);
        }

        [Test]
        public void PermiteRepetirDespuesDeOtraDireccion()
        {
            var buffer = new DirectionBuffer(2);
            buffer.TryEnqueue(Direction.Up);
            buffer.TryEnqueue(Direction.Left);
            buffer.TryDequeue(out _);

            Assert.IsTrue(buffer.TryEnqueue(Direction.Up), "Up de nuevo es un giro real, no una repetición");
        }

        [Test]
        public void DaLaVueltaAlBufferSinCorromperse()
        {
            var buffer = new DirectionBuffer(2);

            // Diez vueltas completas: si los índices circulares están mal, acá se nota.
            for (int i = 0; i < 10; i++)
            {
                buffer.TryEnqueue(Direction.Up);
                buffer.TryEnqueue(Direction.Left);

                Assert.IsTrue(buffer.TryDequeue(out Direction a));
                Assert.IsTrue(buffer.TryDequeue(out Direction b));
                Assert.AreEqual(Direction.Up, a, $"vuelta {i}");
                Assert.AreEqual(Direction.Left, b, $"vuelta {i}");
            }

            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void PeekNoConsume()
        {
            var buffer = new DirectionBuffer(2);
            buffer.TryEnqueue(Direction.Down);

            Assert.IsTrue(buffer.TryPeek(out Direction espiada));
            Assert.AreEqual(Direction.Down, espiada);
            Assert.AreEqual(1, buffer.Count, "espiar no saca");
        }

        [Test]
        public void Clear_VaciaTodo()
        {
            var buffer = new DirectionBuffer(2);
            buffer.TryEnqueue(Direction.Up);
            buffer.TryEnqueue(Direction.Left);

            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.IsFalse(buffer.TryDequeue(out _));
            Assert.IsTrue(buffer.TryEnqueue(Direction.Up), "y queda usable");
        }

        [Test]
        public void CapacidadInvalida_Explota()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DirectionBuffer(0));
        }
    }

    [TestFixture]
    public sealed class TickAccumulatorTests
    {
        // 8 pasos por segundo => 0.125 s por paso, que en float es exacto.
        private static TickAccumulator Acc(float tps = 8f, int max = 4) => new TickAccumulator(tps, max);

        [Test]
        public void IntervaloEsLaInversaDeLaFrecuencia()
        {
            Assert.AreEqual(0.125f, Acc().Interval, 1e-6f);
        }

        [Test]
        public void TiempoJusto_DaUnPaso()
        {
            Assert.AreEqual(1, Acc().Advance(0.125f));
        }

        [Test]
        public void TiempoInsuficiente_NoDaPasosPeroNoSePierde()
        {
            var acc = Acc();

            Assert.AreEqual(0, acc.Advance(0.06f), "todavía no alcanza");
            Assert.AreEqual(1, acc.Advance(0.07f), "0.06 + 0.07 sí alcanza: el sobrante se guardó");
        }

        [Test]
        public void FrameLargo_DaVariosPasos()
        {
            Assert.AreEqual(3, Acc().Advance(0.375f), "0.375 / 0.125 = 3");
        }

        [Test]
        public void EspiralDeLaMuerte_SeCortaEnElTopeYSeTiraElAtraso()
        {
            var acc = Acc(max: 4);

            // Un freeze de 2 segundos son 16 pasos pendientes.
            Assert.AreEqual(4, acc.Advance(2f), "no ejecuta más que el tope");
            Assert.AreEqual(0f, acc.Pending, 1e-6f, "el atraso se descarta, no se arrastra");
            Assert.AreEqual(0, acc.Advance(0.01f), "y el frame siguiente arranca limpio");
        }

        [Test]
        public void CambiarVelocidad_ConservaLoAcumulado()
        {
            var acc = Acc(tps: 8f);
            acc.Advance(0.06f); // guardado, sin pasos

            acc.SetTicksPerSecond(16f); // ahora el intervalo es 0.0625

            Assert.AreEqual(1, acc.Advance(0.01f), "0.06 + 0.01 ya supera 0.0625");
        }

        [Test]
        public void DeltaTimeNoPositivo_NoHaceNada()
        {
            var acc = Acc();

            Assert.AreEqual(0, acc.Advance(0f));
            Assert.AreEqual(0, acc.Advance(-1f), "un delta negativo no debe romper el acumulador");
        }

        [Test]
        public void Reset_TiraLoAcumulado()
        {
            var acc = Acc();
            acc.Advance(0.1f);

            acc.Reset();

            Assert.AreEqual(0f, acc.Pending, 1e-6f);
            Assert.AreEqual(0, acc.Advance(0.05f), "arranca de cero");
        }

        [Test]
        public void ParametrosInvalidos_Explotan()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickAccumulator(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickAccumulator(-8f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TickAccumulator(8f, 0));
        }
    }

    [TestFixture]
    public sealed class Vector2ADireccionTests
    {
        private const float DeadZone = 0.5f;

        private static Direction Map(Vector2 raw, Vector2 previous = default)
        {
            Assert.IsTrue(
                InputSystemReader.TryToDirection(raw, previous, DeadZone, out Direction direction),
                $"esperaba que {raw} (previo {previous}) diera una dirección");
            return direction;
        }

        private static void NoMapea(Vector2 raw, Vector2 previous = default)
        {
            Assert.IsFalse(
                InputSystemReader.TryToDirection(raw, previous, DeadZone, out _),
                $"esperaba que {raw} (previo {previous}) NO diera dirección");
        }

        [Test]
        public void EjesPuros()
        {
            Assert.AreEqual(Direction.Right, Map(new Vector2(1f, 0f)));
            Assert.AreEqual(Direction.Left, Map(new Vector2(-1f, 0f)));
            Assert.AreEqual(Direction.Up, Map(new Vector2(0f, 1f)), "Y+ en pantalla es arriba");
            Assert.AreEqual(Direction.Down, Map(new Vector2(0f, -1f)));
        }

        [Test]
        public void SoltarTodo_NoEsUnGiro()
        {
            NoMapea(Vector2.zero, previous: new Vector2(1f, 0f));
        }

        [Test]
        public void PorDebajoDeLaZonaMuerta_SeIgnora()
        {
            // Stick apenas rozado: no es una intención de girar.
            NoMapea(new Vector2(0.3f, 0f));
        }

        [Test]
        public void Diagonal_GanaElEjeQueSeAcabaDeApretar()
        {
            // Venía yendo para arriba y ahora suma Izquierda sin soltar Arriba.
            Assert.AreEqual(Direction.Left, Map(new Vector2(-1f, 1f), previous: new Vector2(0f, 1f)),
                "el giro nuevo es el horizontal");

            // Al revés: venía yendo a la izquierda y suma Arriba.
            Assert.AreEqual(Direction.Up, Map(new Vector2(-1f, 1f), previous: new Vector2(-1f, 0f)),
                "el giro nuevo es el vertical");
        }

        [Test]
        public void Diagonal_ExactamenteSimultanea_EsAmbiguaYSeDescarta()
        {
            NoMapea(new Vector2(1f, 1f), previous: Vector2.zero);
        }

        [Test]
        public void Diagonal_SinNadaNuevo_SeDescarta()
        {
            // Ya tenía las dos apretadas y el valor cambió por otra razón: no hay giro nuevo.
            NoMapea(new Vector2(1f, 1f), previous: new Vector2(1f, 1f));
        }
    }
}
