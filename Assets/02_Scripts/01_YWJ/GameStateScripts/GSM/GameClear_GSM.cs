using UnityEngine;



namespace YWJ.GameStateMachine
{
    public class GameClear_GSM : Root_GSM
    {
        protected override GameState GSMState => GameState.GAME_CLEAR;

        public GameClear_GSM()
        {

        }

        public override void EnterState()
        {
            Debug.Log("Enter GameClear State");
            GameStateEventBus.PublishGameClearStateEntered();
        }

        public override void UpdateState()
        {
            // 업데이트 로직을 여기에 작성
        }

        public override void ExitState()
        {
            Debug.Log("Exit GameClear State");
            GameStateEventBus.PublishGameClearStateExited();
        }
    }
}