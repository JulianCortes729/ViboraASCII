using NUnit.Framework;
using Vibora.Core;
namespace Vibora.Presentation.Tests
{
    [TestFixture]
    public sealed class RayEffectTests
    {
        private RayEffect RayoDisparado()
        {
            RayEffect rayEffect = new RayEffect();
            GridPos gridPos = new GridPos(10, 10);
            int distance = 12;
            rayEffect.Trigger(gridPos, distance);

            return rayEffect;
        }

        [Test]
        public void RayRecienCreado_Inactivo()
        {
            RayEffect rayEffect = new RayEffect();
            Assert.IsFalse(rayEffect.IsActive);
        }

        [Test]
        public void RayDespuesDeTrigger_Activo()
        {
            Assert.IsTrue(RayoDisparado().IsActive);
        }

        [Test]
        public void RayInactivoAvanza_QuedaInactivo()
        {
            RayEffect rayEffect = new RayEffect();
            rayEffect.Advance(5f);
            Assert.IsFalse(rayEffect.IsActive);
        }

        [Test]
        public void RayInactivo_NoCubreCeldas()
        {
            RayEffect rayEffect = new RayEffect();

            GridPos origen  = new GridPos(0, 0);
            GridPos traslado1 = new GridPos(0, 1);
            GridPos traslado2 = new GridPos(0, 2);
            GridPos traslado3 = new GridPos(1, 0);
            GridPos traslado4 = new GridPos(2, 0);

            // 📖 (0,0) es el default de GridPos: el único punto que un rayo sin
            //    disparar cubriría si se cayera la guarda de IsActive. Las otras
            //    cuatro son de compañía; esta es la que hace fallar el test.
            Assert.IsFalse(rayEffect.Covers(origen), "Un rayo inactivo no debería cubrir celda (0, 0)");
            Assert.IsFalse(rayEffect.Covers(traslado1), "Un rayo inactivo no debería cubrir celda (0, 1)");
            Assert.IsFalse(rayEffect.Covers(traslado2), "Un rayo inactivo no debería cubrir celda (0, 2)");
            Assert.IsFalse(rayEffect.Covers(traslado3), "Un rayo inactivo no debería cubrir celda (1, 0)");
            Assert.IsFalse(rayEffect.Covers(traslado4), "Un rayo inactivo no debería cubrir celda (2, 0)");
        }

        [Test]
        public void RayActivo_NoCubreCeldasFueraDeLaCruz()
        {
            RayEffect rayEffect = RayoDisparado();

            GridPos posInvalida1 = new GridPos(9, 9);
            GridPos posInvalida2 = new GridPos(11, 9);
            GridPos posInvalida3 = new GridPos(9, 11);
            GridPos posInvalida4 = new GridPos(11, 11);

            Assert.IsFalse(rayEffect.Covers(posInvalida1), "El rayo en (10, 10) no cubre diagonal (9, 9)");
            Assert.IsFalse(rayEffect.Covers(posInvalida2), "El rayo en (10, 10) no cubre diagonal (11, 9)");
            Assert.IsFalse(rayEffect.Covers(posInvalida3), "El rayo en (10, 10) no cubre diagonal (9, 11)");
            Assert.IsFalse(rayEffect.Covers(posInvalida4), "El rayo en (10, 10) no cubre diagonal (11, 11)");
        }
    }
}
