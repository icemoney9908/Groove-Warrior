using KKH.Gameplay;
using UnityEngine;



namespace YWJ.GameStateMachine
{
    public class MusicMode_GSM : Root_GSM
    {
        protected override GameState GSMState => GameState.MUSIC_MODE;

        protected MusicConductor _musicConductor;

        public MusicMode_GSM()
        {

        }

        public override void EnterState()
        {
            Debug.Log("Enter MusicMode State");
            GameStateEventBus.PublishMusicModeStateEntered();
        }

        public override void UpdateState()
        {
            // 업데이트 로직을 여기에 작성
        }

        public override void ExitState()
        {
            Debug.Log("Exit MusicMode State");
            GameStateEventBus.PublishMusicModeStateExited();
        }
    }
}