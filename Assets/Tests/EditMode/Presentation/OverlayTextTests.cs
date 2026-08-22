using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Presentation.Tests
{
    [TestFixture]
    public sealed class OverlayTextTests
    {
        private const int Altura = 9;
        private const int Lineas = 3;

        private const int PrimerLinea = 3;

        [Test]
        public void ElCartelDelMenu_QuedaCentradoVertical()
        {
            // Las tres líneas del menú caen en las filas 3, 4, 5
            Assert.AreEqual("V I B O R A S C I I", OverlayText.LineFor(3, GameState.MainMenu, Altura));
            Assert.AreEqual("", OverlayText.LineFor(4, GameState.MainMenu, Altura));
            Assert.AreEqual("ENTER PARA JUGAR", OverlayText.LineFor(5, GameState.MainMenu, Altura));
        }

        [Test]
        public void FueraDelBloque_NoHayCartel()
        {
            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea-1), GameState.MainMenu, Altura));
            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea+Lineas), GameState.MainMenu, Altura));

            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea - 1), GameState.Paused, Altura));
            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea + Lineas), GameState.Paused, Altura));
        }

        [Test]
        public void JugandoYGameOver_NuncaHayCartel()
        {
            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea), GameState.Playing, Altura));
            Assert.AreEqual(null, OverlayText.LineFor((PrimerLinea + 1), GameState.GameOver, Altura));
        }

        [Test]
        public void LaLineaDelMedio_EsVaciaPeroNoNull()
        {
            Assert.AreEqual("", OverlayText.LineFor((PrimerLinea+1), GameState.MainMenu, Altura));
            Assert.AreEqual("", OverlayText.LineFor((PrimerLinea + 1), GameState.Paused, Altura));

            Assert.IsNotNull(OverlayText.LineFor((PrimerLinea+1), GameState.MainMenu, Altura));
            Assert.IsNotNull(OverlayText.LineFor((PrimerLinea + 1), GameState.Paused, Altura));
        }

        [Test]
        public void Pausa_MuestraSuPropioCartel()
        {
            Assert.AreEqual("P A U S A", OverlayText.LineFor(3, GameState.Paused, Altura));
            Assert.AreEqual("", OverlayText.LineFor(4, GameState.Paused, Altura));
            Assert.AreEqual("ENTER PARA SEGUIR", OverlayText.LineFor(5, GameState.Paused, Altura));
        }

        [Test]
        public void GrillaMasBajaQueElCartel_NoExplota()
        {
            // 📖 Dos líneas de alto para un cartel de tres: no entra, y el centrado
            //    tiene que degradarse sin romperse. Nadie juega en una grilla así, pero
            //    es la fila que un bug de índice elige para explotar.
            const int AlturaChica = 2;

            // 📖 first = (2 - 3) / 2 = -1 / 2. C# trunca hacia CERO, no hacia abajo:
            //    da 0, no -1. Por eso el cartel arranca en la fila 0 y se recorta por
            //    abajo. Con la convención de Python (-1) se recortaría por arriba, y
            //    estas dos filas dirían otra cosa.
            Assert.AreEqual("V I B O R A S C I I", OverlayText.LineFor(0, GameState.MainMenu, AlturaChica));
            Assert.AreEqual("", OverlayText.LineFor(1, GameState.MainMenu, AlturaChica));

            // 📖 Con una sola fila, first = (1 - 3) / 2 = -1 y el índice se corre: la
            //    única fila visible es la del medio, que está en blanco. Feo, pero no
            //    rompe nada — que es todo lo que este test le pide al caso degenerado.
            Assert.AreEqual("", OverlayText.LineFor(0, GameState.Paused, 1));

            // 📖 Lo que de verdad sostiene el test: ninguna fila de una grilla enana
            //    sale por un índice fuera del array. La guarda es index >= 0 && index
            //    < lines.Length; si alguien la simplifica a una sola mitad, esto avisa.
            Assert.DoesNotThrow(() =>
            {
                foreach (GameState state in new[] { GameState.MainMenu, GameState.Paused })
                    for (int altura = 1; altura <= 4; altura++)
                        for (int y = 0; y < altura; y++)
                            OverlayText.LineFor(y, state, altura);
            }, "alguna fila de una grilla más baja que el cartel se fue del array");
        }
    }
}
