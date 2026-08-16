

namespace Vibora.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public enum StateChange
    {
        None,        // la tecla no aplica en este estado
        GameStarted, // arrancar partida nueva y soltar el metrónomo
        Paused,      // frenar el metrónomo
        Resumed,     // soltarlo sin tocar la partida
        Ended        // frenar, guardar récord, sonido
    }
}
