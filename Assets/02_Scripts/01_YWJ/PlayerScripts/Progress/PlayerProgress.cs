using System;

namespace YWJ.Player.Progress
{
    public enum PlayerProgress
    {
        OpeningCutScene = 0,
        BasicTutorial = 10,

        Stage01 = 20,
        CutScene01 = 25,
        BossStage01 = 30,

        Stage02 = 40,
        CutScene02 = 45,
        BossStage02 = 50,

        EndingCutScene = 70,
        GameCompleted = 80
    }
}