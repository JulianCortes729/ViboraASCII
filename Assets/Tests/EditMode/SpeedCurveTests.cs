using System;
using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class SpeedCurveTests
    {
        // Arranca en 8, sube 0.5 por manzana, techo en 12: llega al tope con 8 manzanas.
        private static SpeedCurve Curve() => new SpeedCurve(8f, 0.5f, 12f);

        [Test]
        public void SinComer_EsLaVelocidadInicial()
        {
            Assert.AreEqual(8f, Curve().For(0), 1e-5f);
        }

        [Test]
        public void CadaManzanaAcelera()
        {
            var curve = Curve();

            Assert.AreEqual(8.5f, curve.For(1), 1e-5f);
            Assert.AreEqual(10f, curve.For(4), 1e-5f);
        }

        [Test]
        public void NuncaPasaElTecho()
        {
            var curve = Curve();

            Assert.AreEqual(12f, curve.For(8), 1e-5f, "justo en el tope");
            Assert.AreEqual(12f, curve.For(50), 1e-5f, "muy por encima");
            Assert.AreEqual(12f, curve.For(int.MaxValue), 1e-5f, "el caso absurdo tampoco lo rompe");
        }

        [Test]
        public void NuncaBajaDeLaVelocidadInicial()
        {
            Assert.AreEqual(8f, Curve().For(-5), 1e-5f, "un score negativo no frena el juego");
        }

        [Test]
        public void EsMonotona_NuncaSeFrenaAlComer()
        {
            var curve = Curve();
            float anterior = curve.For(0);

            for (int score = 1; score <= 30; score++)
            {
                float actual = curve.For(score);
                Assert.GreaterOrEqual(actual, anterior, $"la velocidad bajó al pasar a {score} manzanas");
                anterior = actual;
            }
        }

        [Test]
        public void ScoreAtMaxSpeed_DiceCuandoSeLlegaAlTope()
        {
            var curve = Curve();
            int score = curve.ScoreAtMaxSpeed;

            Assert.AreEqual(8, score);
            Assert.AreEqual(curve.MaxTicksPerSecond, curve.For(score), 1e-5f, "en ese score ya está al tope");
            Assert.Less(curve.For(score - 1), curve.MaxTicksPerSecond, "y uno antes todavía no");
        }

        [Test]
        public void SinIncremento_LaVelocidadNoCambia()
        {
            var plana = new SpeedCurve(8f, 0f, 12f);

            Assert.AreEqual(8f, plana.For(0), 1e-5f);
            Assert.AreEqual(8f, plana.For(100), 1e-5f);
            Assert.AreEqual(0, plana.ScoreAtMaxSpeed, "nunca llega al tope, y no divide por cero");
        }

        [Test]
        public void ParametrosInvalidos_Explotan()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedCurve(0f, 0.5f, 12f), "velocidad inicial cero");
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedCurve(-1f, 0.5f, 12f), "velocidad inicial negativa");
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedCurve(8f, -0.5f, 12f), "incremento negativo");
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpeedCurve(8f, 0.5f, 4f), "techo por debajo del piso");
        }

        [Test]
        public void TechoIgualAlPiso_EsValido()
        {
            var fija = new SpeedCurve(8f, 0.5f, 8f);

            Assert.AreEqual(8f, fija.For(0), 1e-5f);
            Assert.AreEqual(8f, fija.For(20), 1e-5f);
        }
    }
}
