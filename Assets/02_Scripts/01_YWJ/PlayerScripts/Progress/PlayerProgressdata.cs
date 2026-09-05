using System;


namespace YWJ.Player.Progress
{
    [Serializable]
    public class PlayerProgressData
    {
        public int DataVersion = 1;
        public PlayerProgress CurrentProgress = PlayerProgress.OpeningCutScene;
    }
}
