namespace Vibora.Core
{

    /// <summary>
    /// Repositorio de puntuaciones. Existe para poder apagarlo en los tests.
    /// </summary>
    /// <remarks>
    /// 🧩 Un test que depende de un repositorio real no prueba nada: pasa o falla según el estado del disco. 
    ///    Inyectando el repositorio, el test decide exactamente qué puntuación hay y el resultado es siempre el mismo. 
    /// </remarks>
    
    public interface IScoreRepository
    {

        /// <summary>
        /// Lee la puntuación más alta guardada. Si no hay ninguna, devuelve 0.
        /// </summary>
        /// <returns>La puntuación más alta guardada o 0 si no hay ninguna.</returns>
        int Load();


        /// <summary>
        /// Guarda la puntuación más alta. Si ya había una, la reemplaza.
        /// </summary>
        /// <param name="score"></param>
        void Save(int score);

    }
}
