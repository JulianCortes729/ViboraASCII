using NUnit.Framework;
namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class GameStateMachineTests
    {

        [Test]
        public void Ciclo_Completo_De_Transiciones()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            // From MainMenu to Playing
            var result1 = stateMachine.Confirm();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.GameStarted, result1);
            // From Playing to Paused
            var result2 = stateMachine.TogglePause();
            Assert.AreEqual(GameState.Paused, stateMachine.Current);
            Assert.AreEqual(StateChange.Paused, result2);
            // From Paused to Playing
            var result3 = stateMachine.TogglePause();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.Resumed, result3);
            // From Playing to GameOver
            var result4 = stateMachine.NotifyGameOver();
            Assert.AreEqual(GameState.GameOver, stateMachine.Current);
            Assert.AreEqual(StateChange.Ended, result4);
        }


        [Test]
        public void Confirm_Desde_MainMenu_Transiciona_A_Playing()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            var result = stateMachine.Confirm();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.GameStarted, result);
        }

        [Test]
        public void Confirm_Desde_Paused_Transiciona_A_Playing()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            stateMachine.Confirm(); // Transition to Playing
            stateMachine.TogglePause(); // Transition to Paused
            var result = stateMachine.Confirm();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.Resumed, result);
        }

        [Test]
        public void Confirm_Desde_GameOver_Transiciona_A_Playing()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            stateMachine.Confirm(); // Transition to Playing
            stateMachine.NotifyGameOver(); // Transition to GameOver
            var result = stateMachine.Confirm();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.GameStarted, result);
        }

        [Test]
        public void Confirm_Desde_Playing_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            stateMachine.Confirm(); // Transition to Playing
            var result = stateMachine.Confirm();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void TogglePause_Desde_Playing_Transiciona_A_Paused()
        {
            var stateMachine = MachineIn(GameState.Playing);
            var result = stateMachine.TogglePause();
            Assert.AreEqual(GameState.Paused, stateMachine.Current);
            Assert.AreEqual(StateChange.Paused, result);
        }

        [Test]
        public void TogglePause_Desde_Paused_Transiciona_A_Playing()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            stateMachine.Confirm(); // Transition to Playing
            stateMachine.TogglePause(); // Transition to Paused
            var result = stateMachine.TogglePause();
            Assert.AreEqual(GameState.Playing, stateMachine.Current);
            Assert.AreEqual(StateChange.Resumed, result);
        }

        [Test]
        public void TogglePause_Desde_MainMenu_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            var result = stateMachine.TogglePause();
            Assert.AreEqual(GameState.MainMenu, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void TogglePause_Desde_GameOver_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.GameOver);
            var result = stateMachine.TogglePause();
            Assert.AreEqual(GameState.GameOver, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void NotifyGameOver_Desde_Playing_Transiciona_A_GameOver()
        {
            var stateMachine = MachineIn(GameState.Playing);
            var result = stateMachine.NotifyGameOver();
            Assert.AreEqual(GameState.GameOver, stateMachine.Current);
            Assert.AreEqual(StateChange.Ended, result);
        }

        [Test]
        public void NotifyGameOver_Desde_Paused_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.Paused);
            var result = stateMachine.NotifyGameOver();
            Assert.AreEqual(GameState.Paused, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void NotifyGameOver_Desde_MainMenu_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            var result = stateMachine.NotifyGameOver();
            Assert.AreEqual(GameState.MainMenu, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void NotifyGameOver_Desde_GameOver_No_Cambia_Estado()
        {
            var stateMachine = MachineIn(GameState.GameOver);
            stateMachine.NotifyGameOver(); // Transition to GameOver
            var result = stateMachine.NotifyGameOver();
            Assert.AreEqual(GameState.GameOver, stateMachine.Current);
            Assert.AreEqual(StateChange.None, result);
        }

        [Test]
        public void NotifyGameOver_DosVeces_Desde_Playing_Transiciona_A_GameOver_Una_Vez()
        {
            var stateMachine = MachineIn(GameState.Playing);
            var result1 = stateMachine.NotifyGameOver();
            var result2 = stateMachine.NotifyGameOver();

            Assert.AreEqual(GameState.GameOver, stateMachine.Current);
            Assert.AreEqual(StateChange.Ended, result1);
            Assert.AreEqual(StateChange.None, result2);
        }

        [Test]
        public void Estado_Inicial_Es_MainMenu()
        {
            var stateMachine = MachineIn(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, stateMachine.Current);
        }

        private static GameStateMachine MachineIn(GameState state)
        {
            var machine = new GameStateMachine();

            switch (state)
            {
                case GameState.Playing:
                    machine.Confirm();
                    break;
                case GameState.Paused:
                    machine.Confirm();
                    machine.TogglePause();
                    break;
                case GameState.GameOver:
                    machine.Confirm();
                    machine.NotifyGameOver();
                    break;
            }

            return machine;
        }
    }
}
