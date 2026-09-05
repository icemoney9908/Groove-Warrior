using System.Collections.Generic;
using UnityEngine;

namespace YWJ.CutScene
{
    [CreateAssetMenu(
        fileName = "CutSceneData",
        menuName = "YWJ/CutScene/CutScene Data"
    )]
    public class CutSceneData : ScriptableObject
    {
        [SerializeField] private string _cutSceneID;
        [SerializeField] private string _displayName;
        [TextArea]
        [SerializeField] private string _description;

        [SerializeField]
        [Tooltip("컷신이 정상 종료되면 Actor들을 컷신 시작 위치로 복귀시킵니다.")]
        private bool _restoreActorPositionsOnFinish;

        [SerializeField] private List<CutSceneStep> _steps = new();


        public string CutSceneID => _cutSceneID;
        public string DisplayName => _displayName;
        public string Description => _description;
        public IReadOnlyList<CutSceneStep> Steps => _steps;
        public bool RestoreActorPositionsOnFinish => _restoreActorPositionsOnFinish;

    }
}
