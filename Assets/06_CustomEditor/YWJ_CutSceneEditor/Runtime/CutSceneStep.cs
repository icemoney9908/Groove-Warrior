using System;
using System.Collections.Generic;
using UnityEngine;

namespace YWJ.CutScene
{
    [Serializable]
    public class CutSceneStep
    {
        [SerializeField] private string _stepName = "New Step";

        [Tooltip("이 목록의 모든 Action은 같은 프레임에 시작됩니다.")]
        [SerializeField] private List<CutSceneAction> _actions = new();

        public string StepName => _stepName;
        public IReadOnlyList<CutSceneAction> Actions => _actions;
    }
}
