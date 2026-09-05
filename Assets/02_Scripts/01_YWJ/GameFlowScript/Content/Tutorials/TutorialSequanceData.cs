using System.Collections.Generic;
using KKH.ChartData;
using UnityEngine;

namespace YWJ.GameFlow.Content
{
    [CreateAssetMenu(
        fileName = "TutorialSequence",
        menuName = "YWJ/Tutorial/Tutorial Sequence")]
    public class TutorialSequenceData : ScriptableObject
    {
        [Header("Music")]
        [SerializeField]
        private SongChartData _tutorialChartData;

        [Header("Steps")]
        [SerializeField]
        private List<TutorialStep> _steps = new();

        public SongChartData TutorialChartData =>
            _tutorialChartData;

        public IReadOnlyList<TutorialStep> Steps =>
            _steps;
    }
}