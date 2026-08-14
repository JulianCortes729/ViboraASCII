using System;

namespace Vibora.Core
{
    /// <summary>
    /// Convierte tiempo real (que llega en pedazos irregulares) en una cantidad
    /// entera de pasos de juego.
    /// </summary>
    /// <remarks>
    /// 🧩 Los frames no duran lo mismo: uno puede tardar 8 ms y el siguiente 40. Si la
    /// víbora avanzara "un paso por frame", correría distinto en cada máquina. El
    /// acumulador es como una alcancía: le vas metiendo el tiempo que pasó y cada vez
    /// que junta un intervalo completo, te devuelve un paso. El sobrante queda guardado
    /// para el próximo frame, así no se pierde ni un milisegundo.
    ///
    /// Es C# puro a propósito: la parte que se puede equivocar es la cuenta, y así se
    /// testea sin abrir Unity.
    /// </remarks>
    public sealed class TickAccumulator
    {
        private float _accumulated;
        private float _interval;

        /// <summary>Tope de pasos por llamada a <see cref="Advance"/>.</summary>
        public int MaxStepsPerAdvance { get; }

        /// <summary>Segundos entre paso y paso.</summary>
        public float Interval => _interval;

        public float TicksPerSecond => 1f / _interval;

        /// <summary>Tiempo guardado que todavía no alcanzó para un paso.</summary>
        public float Pending => _accumulated;

        public TickAccumulator(float ticksPerSecond, int maxStepsPerAdvance = 4)
        {
            if (maxStepsPerAdvance < 1)
                throw new ArgumentOutOfRangeException(nameof(maxStepsPerAdvance), maxStepsPerAdvance, "Debe permitir al menos un paso.");

            MaxStepsPerAdvance = maxStepsPerAdvance;
            SetTicksPerSecond(ticksPerSecond);
        }

        /// <summary>Cambia la velocidad sin perder el tiempo ya acumulado.</summary>
        // 📖 Esto es lo que va a usar la velocidad progresiva (P2): subir la dificultad
        //    es cambiar un número, no reconstruir nada.
        public void SetTicksPerSecond(float ticksPerSecond)
        {
            if (ticksPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Debe ser > 0.");

            _interval = 1f / ticksPerSecond;
        }

        /// <summary>Mete tiempo y devuelve cuántos pasos hay que ejecutar ahora.</summary>
        public int Advance(float deltaTime)
        {
            // 📖 deltaTime negativo o cero no debería pasar, pero si pasa (Time.timeScale = 0,
            //    o un frame raro) el acumulador no tiene por qué romperse.
            if (deltaTime <= 0f)
                return 0;

            _accumulated += deltaTime;

            int steps = 0;
            while (_accumulated >= _interval && steps < MaxStepsPerAdvance)
            {
                _accumulated -= _interval;
                steps++;
            }

            // 📖 ESPIRAL DE LA MUERTE: si un freeze de 2 s dejó 20 pasos pendientes,
            //    ejecutarlos todos hace el frame aún más largo, que acumula más pasos...
            //    Cortamos por lo sano: se tira el atraso. El juego pierde un pedacito de
            //    tiempo (mejor) en vez de congelarse para siempre (peor).
            if (steps == MaxStepsPerAdvance && _accumulated >= _interval)
                _accumulated = 0f;

            return steps;
        }

        /// <summary>Vuelve a cero. Para usar al reiniciar o al despausar.</summary>
        public void Reset() => _accumulated = 0f;
    }
}
