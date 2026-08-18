using NUnit.Framework;
using Vibora.Presentation;
namespace Vibora.Presentation.Tests
{
    [TestFixture]
    public sealed class RayEffectTests
    {

        [Test]
        public void RayRecienCreado_Inactivo()
        {
            RayEffect rayEffect = new RayEffect();
            Assert.IsFalse(rayEffect.IsActive);
        }
    }
}
