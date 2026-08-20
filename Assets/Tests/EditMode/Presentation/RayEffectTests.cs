using System;
using NUnit.Framework;
using Vibora.Core;
namespace Vibora.Presentation.Tests
{
    [TestFixture]
    public sealed class RayEffectTests
    {
        private const int OriginX = 10;
        private const int OriginY = 10;
        private const int DistanciaMaxima = 12;

        /// <summary>Cuántos frames espera un test antes de darse por vencido. Mil son ~16 segundos de juego.</summary>
        private const int MaxFrames = 1000;

        private RayEffect RayoDisparado()
        {
            RayEffect rayEffect = new RayEffect();
            GridPos gridPos = new GridPos(OriginX, OriginY);
            rayEffect.Trigger(gridPos, DistanciaMaxima);
            return rayEffect;
        }

        private RayEffect RayoDisparado(RayEffect rayEffect,GridPos gridPos)
        {
            rayEffect.Trigger(gridPos, DistanciaMaxima);

            return rayEffect;
        }

        private void AvanzarElRayoUnFrame(RayEffect ray)
        {
            float timeFrame = 1f / 60f;
            ray.Advance(timeFrame);
        }

        /// <summary>
        /// Avanza el rayo de a un frame hasta que <paramref name="condicion"/> se cumpla.
        /// Si no se cumple nunca, falla con <paramref name="queFaltaba"/> en vez de colgarse.
        /// </summary>
        // 📖 El tope vive acá adentro y no copiado en cada test: el patrón estaba escrito
        //    cinco veces, y en tres de ellas el contador no se incrementaba. Un límite que
        //    hay que acordarse de escribir bien cinco veces no es un límite.
        private void AvanzarHasta(RayEffect ray, Func<bool> condicion, string queFaltaba)
        {
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                if (condicion())
                    return;

                AvanzarElRayoUnFrame(ray);
            }

            Assert.Fail($"{queFaltaba} (después de {MaxFrames} frames)");
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

            GridPos origen = new GridPos(0, 0);
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

        [Test]
        public void ElBordeSeIlumina_AntesDeApagarse()
        {
            RayEffect rayEffect = RayoDisparado();
            GridPos bordeDelBrazo = new GridPos(OriginX, OriginY + DistanciaMaxima);

            // 📖 El "antes de apagarse" del nombre no necesita assert propio: Covers ya
            //    devuelve false en un rayo apagado, así que encontrar la celda encendida
            //    prueba las dos mitades de la frase de una sola vez.
            // 📖 Este es el test que caza el signo invertido: con Front + Thickness el
            //    rayo muere antes de que la punta llegue, y el borde no se enciende nunca.
            AvanzarHasta(rayEffect, () => rayEffect.Covers(bordeDelBrazo),
                "el borde del brazo nunca se iluminó");
        }

        [Test]
        public void LaEstelaPasa_LaCeldaSeApagaAntesQueElRayo()
        {
            RayEffect rayEffect = RayoDisparado();
            GridPos celdaCercaDelOrigen = new GridPos(OriginX, OriginY + 2);

            AvanzarHasta(rayEffect, () => rayEffect.Covers(celdaCercaDelOrigen),
                "la celda cerca del origen nunca se encendió");
            AvanzarHasta(rayEffect, () => !rayEffect.Covers(celdaCercaDelOrigen),
                "la celda quedó encendida para siempre: el frente crece en vez de viajar");

            // 📖 El segundo AvanzarHasta también sale si el rayo entero se apagó, porque
            //    un rayo inactivo no cubre nada. Este assert es lo único que separa
            //    "la estela le pasó por encima" de "se apagó todo".
            Assert.IsTrue(rayEffect.IsActive,
                "el rayo se apagó antes de que la estela terminara de pasar");
        }

        [Test]
        public void ElRayoSeApagaSolo()
        {
            RayEffect rayEffect = RayoDisparado();

            AvanzarHasta(rayEffect, () => !rayEffect.IsActive,
                "el rayo siguió activo: no se apaga solo");
        }

        [Test]
        public void TriggerPisaAlAnterior()
        {
            RayEffect rayEffect = RayoDisparado();
            GridPos bordeDelBrazo = new GridPos(OriginX, OriginY + DistanciaMaxima);
            GridPos nuevoOrigen = new GridPos(OriginX + 5, OriginY + 5);

            // 📖 Re-disparar recién salido no probaría nada: hay que dejar que el frente
            //    se aleje para que "el reloj volvió a cero" y "el reloj siguió corriendo"
            //    den resultados distintos.
            AvanzarHasta(rayEffect, () => rayEffect.Covers(bordeDelBrazo),
                "el frente nunca llegó al borde");
            Assert.IsTrue(rayEffect.IsActive, "el rayo tiene que seguir vivo al re-dispararlo");

            RayoDisparado(rayEffect, nuevoOrigen);

            // 📖 Sin avanzar un solo frame: con el reloj en cero el frente está parado en
            //    el origen nuevo y lo cubre. Si Trigger no reseteara _timer, el frente
            //    seguiría lejos y la estela ya habría dejado esta celda atrás.
            Assert.IsTrue(rayEffect.Covers(nuevoOrigen),
                "después de re-disparar, el frente no arrancó de cero en el origen nuevo");
        }
    }
}
