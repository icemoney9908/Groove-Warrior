using System;
using UnityEngine;

namespace YWJ.GameLogic.Judgement
{
    [Serializable]
    public class TabJudgementSetting
    {
        [Tooltip("Tab 입력의 최대 허용 시간 오차입니다. 초 단위입니다.")]
        [Min(0f)]
        [SerializeField] private double _perfectWindow = 0.1d;

        [Tooltip("Tab 입력의 실패 시간 오차입니다. 초 단위입니다.")]
        [Min(0f)]
        [SerializeField] private double _missWindow = 0.1d;

        public double PerfectWindow => _perfectWindow;
        public double MissWindow => _missWindow;

        public bool Validate(double timingDifference)
        {
            return Math.Abs(timingDifference) <= _perfectWindow;
        }

        public bool Validate(double inputTime, double targetTime)
        {
            return Validate(inputTime - targetTime);
        }
    }
}