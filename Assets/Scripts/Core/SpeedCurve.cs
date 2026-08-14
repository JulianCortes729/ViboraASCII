using System;

namespace Vibora.Core
{
    /// <summary>
    /// A qué velocidad corre el juego según cuántas manzanas se comieron.
    /// </summary>
    /// <remarks>
    /// Vive en el Core y no adentro del TickDriver porque "el juego acelera al comer"
    /// es una regla de diseño, no un detalle del reloj de Unity. Acá se puede probar
    /// la curva entera en un test; adentro del MonoBehaviour habría que jugar 30
    /// partidas para saber si el tope funciona.
    /// </remarks>
    public sealed class SpeedCurve
    {
        public float BaseTicksPerSecond { get; }

        /// <summary>Cuánto sube la velocidad por cada manzana.</summary>
        public float IncrementPerFood { get; }

        /// <summary>Techo. Sin esto, a las 40 manzanas el juego es injugable.</summary>
        public float MaxTicksPerSecond { get; }

        public SpeedCurve(float baseTicksPerSecond, float incrementPerFood, float maxTicksPerSecond)
        {
            if (baseTicksPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(baseTicksPerSecond), baseTicksPerSecond, "Debe ser > 0.");
            if (incrementPerFood < 0f)
                throw new ArgumentOutOfRangeException(nameof(incrementPerFood), incrementPerFood, "No puede ser negativo: el juego no se frena al comer.");
            if (maxTicksPerSecond < baseTicksPerSecond)
                throw new ArgumentOutOfRangeException(nameof(maxTicksPerSecond), maxTicksPerSecond, "El techo no puede estar debajo de la velocidad inicial.");

            BaseTicksPerSecond = baseTicksPerSecond;
            IncrementPerFood = incrementPerFood;
            MaxTicksPerSecond = maxTicksPerSecond;
        }

        /// <summary>Velocidad para un score dado, ya recortada al techo.</summary>
        public float For(int score)
        {
            // 📖 Un score negativo no debería existir, pero si aparece no tiene por qué
            //    frenar el juego por debajo de la velocidad inicial.
            if (score < 0)
                score = 0;

            float raw = BaseTicksPerSecond + (score * IncrementPerFood);

            return raw > MaxTicksPerSecond ? MaxTicksPerSecond : raw;
        }

        /// <summary>Con cuántas manzanas se llega al techo. Sirve para tunear la curva.</summary>
        public int ScoreAtMaxSpeed
        {
            get
            {
                if (IncrementPerFood <= 0f)
                    return 0;

                return (int)Math.Ceiling((MaxTicksPerSecond - BaseTicksPerSecond) / IncrementPerFood);
            }
        }
    }
}
