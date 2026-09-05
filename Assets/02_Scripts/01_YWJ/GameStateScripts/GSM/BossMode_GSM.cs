using UnityEngine;



namespace YWJ.GameStateMachine
{
    public class BossMode_GSM : Root_GSM
    {
        protected override GameState GSMState => GameState.BOSS_MODE;

        public BossMode_GSM()
        {

        }

        public override void EnterState()
        {
            Debug.Log("Enter BossMode State");
            GameStateEventBus.PublishBossModeStateEntered();
        }

        public override void UpdateState()
        {
            // 업데이트 로직을 여기에 작성
        }

        public override void ExitState()
        {
            Debug.Log("Exit BossMode State");
            GameStateEventBus.PublishBossModeStateExited();
        }
    }
}