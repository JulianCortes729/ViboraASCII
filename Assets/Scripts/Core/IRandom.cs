using System;

namespace Vibora.Core
{
    /// <summary>Fuente de azar. Existe para poder apagarla en los tests.</summary>
    /// <remarks>
    /// 🧩 Un test que depende del azar real no prueba nada: pasa o falla según el humor
    /// del generador. Inyectando el azar, el test decide exactamente qué número sale y
    /// el resultado es siempre el mismo.
    /// </remarks>
    public interface IRandom
    {
        /// <summary>Un entero en el rango [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }

    /// <summary>El azar de verdad, envolviendo <see cref="System.Random"/>.</summary>
    // 📖 System.Random es de la biblioteca base de C#, no de UnityEngine: por eso puede
    //    vivir en el Core. UnityEngine.Random no podría.
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom() : this(Environment.TickCount) { }

        /// <summary>Con semilla fija, la partida entera es reproducible.</summary>
        public SystemRandom(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
