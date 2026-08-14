using System;

namespace Vibora.Core
{
    /// <summary>
    /// Lleva la cuenta del récord de puntuación. Se inyecta un repositorio para poder apagarlo en los tests.
    /// </summary>
    public sealed class HighScoreTracker
    {
        private readonly IScoreRepository _scoreRepository;

        // 📖 Sin campo separado: la propiedad guarda el valor y solo esta clase puede escribirlo.
        public int Best { get; private set; }

        /// <summary>
        /// Constructor. Inyecta el repositorio de puntuaciones.
        /// </summary>
        /// <param name="scoreRepository">El repositorio de puntuaciones.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public HighScoreTracker(IScoreRepository scoreRepository)
        {
            if (scoreRepository == null)
                throw new ArgumentNullException(nameof(scoreRepository));

            _scoreRepository = scoreRepository;

           

            // 📖 Lo que viene de afuera del proceso no es confiable: el JSON lo puede editar
            //    cualquiera. Un récord negativo haría que un score de 0 pareciera un récord.
            Best = Math.Max(0, _scoreRepository.Load());
        }

        /// <summary>
        /// Intenta guardar un score. Devuelve true si es récord, false si no.
        /// </summary>
        /// <param name="score">La puntuación a intentar guardar.</param>
        /// <returns>True si es récord, false si no.</returns>
        public bool Submit(int score)
        {
            if (score > Best)
            {
                _scoreRepository.Save(score);
                Best = score;
                return true;
            }
            return false;
        }
    }
}
