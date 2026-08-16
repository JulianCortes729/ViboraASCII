
namespace Vibora.Core
{
    public sealed class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.MainMenu;

        public StateChange Confirm()
        {
            switch (Current)
            {
                case GameState.MainMenu:
                    Current = GameState.Playing;
                    return StateChange.GameStarted;
                case GameState.Paused:
                    Current = GameState.Playing;
                    return StateChange.Resumed;
                case GameState.GameOver:
                    Current = GameState.Playing;
                    return StateChange.GameStarted;
                default:
                    return StateChange.None;
            }
        }

        public StateChange TogglePause()
        {
            switch (Current)
            {
                case GameState.Playing:
                    Current = GameState.Paused;
                    return StateChange.Paused;
                case GameState.Paused:
                    Current = GameState.Playing;
                    return StateChange.Resumed;
                default:
                    return StateChange.None;
            }
        }   
            

        public StateChange NotifyGameOver()
        {
            switch (Current)
            {
                case GameState.Playing:
                    Current = GameState.GameOver;
                    return StateChange.Ended;
                default:
                    return StateChange.None;
            }
        }
    }
}