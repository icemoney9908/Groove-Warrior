using System;
using UnityEngine;

namespace YWJ.GameLogic.Judgement
{
    [Serializable]
    public class HoldTabJudgementSetting
    {
        [Tooltip("HoldTab 시작 입력의 최대 허용 시간 차이입니다.")]
        [Min(0f)]
        [SerializeField]
        private double _startWindow = 0.1d;

        [Tooltip("HoldTab 입력의 실패 시간 오차입니다. 초 단위입니다.")]
        [Min(0f)]
        [SerializeField]
        private double _missWindow = 0.1d;

        [Tooltip("유지 중 성공 판정을 발생시키는 시간 간격입니다.")]
        [Min(0.01f)]
        [SerializeField]
        private double _tickInterval = 0.25d;

        [Tooltip("노트 종료 시점보다 이만큼 일찍 놓아도 완료로 인정합니다.")]
        [Min(0f)]
        [SerializeField]
        private double _releaseWindow = 0.1d;

        public double StartWindow => _startWindow;
        public double MissWindow => _missWindow;
        public double TickInterval => _tickInterval;
        public double ReleaseWindow => _releaseWindow;

        public bool ValidateStart(
            double inputTime,
            double targetStartTime)
        {
            return Math.Abs(inputTime - targetStartTime)
                   <= _startWindow;
        }

        public bool ValidateTick(
            bool isHolding,
            bool successHolding,
            double currentTime,
            double targetEndTime)
        {
            return isHolding
                && successHolding
                && targetEndTime - currentTime >= _tickInterval;
        }

        public bool ValidateRelease(
            double releaseTime,
            double targetEndTime)
        {
            return releaseTime >=
                   targetEndTime - _releaseWindow;
        }
    }
}