using UnityEngine;

namespace YWJ.GameLogic.Judgement
{
    [CreateAssetMenu(
        fileName = "JudgementSetting",
        menuName = "YWJ/Rhythm Game/Judgement Setting"
    )]
    public class JudgementSetting : ScriptableObject
    {
        [Header("Tab")]
        [SerializeField]
        private TabJudgementSetting _tab = new();


        [Header("Hold Tab")]
        [SerializeField]
        private HoldTabJudgementSetting _holdTab = new();

        [Header("Lane")]
        [SerializeField]
        private LaneJudgementSetting _lane = new();

        public TabJudgementSetting Tab => _tab;
        public HoldTabJudgementSetting HoldTab => _holdTab;
        public LaneJudgementSetting Lane => _lane;
    }
}