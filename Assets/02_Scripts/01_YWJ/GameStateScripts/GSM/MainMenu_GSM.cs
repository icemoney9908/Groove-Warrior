using UnityEngine;



namespace YWJ.GameStateMachine
{
    public class MainMenu_GSM : Root_GSM
    {
        protected override GameState GSMState => GameState.MAIN_MENU;

        public MainMenu_GSM()
        {

        }

        public override void EnterState()
        {
            Debug.Log("Enter MainMenu State");
            GameStateEventBus.PublishMainMenuStateEntered();
        }

        public override void UpdateState()
        {
            // 업데이트 로직을 여기에 작성
        }

        public override void ExitState()
        {
            Debug.Log("Exit MainMenu State");
            GameStateEventBus.PublishMainMenuStateExited();
        }
    }
}