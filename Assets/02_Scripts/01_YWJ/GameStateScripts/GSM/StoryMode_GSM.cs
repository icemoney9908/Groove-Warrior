using UnityEngine;



namespace YWJ.GameStateMachine
{
    public class StoryMode_GSM : Root_GSM
    {
        protected override GameState GSMState => GameState.STORY_MODE;

        public StoryMode_GSM()
        {

        }

        public override void EnterState()
        {
            Debug.Log("Enter StoryMode State");
            GameStateEventBus.PublishStoryModeStateEntered();
        }

        public override void UpdateState()
        {
            // 업데이트 로직을 여기에 작성
        }

        public override void ExitState()
        {
            Debug.Log("Exit StoryMode State");
            GameStateEventBus.PublishStoryModeStateExited();
        }
    }
}