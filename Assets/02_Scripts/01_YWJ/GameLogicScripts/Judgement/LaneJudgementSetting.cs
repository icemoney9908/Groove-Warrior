using System;
using UnityEngine;

namespace YWJ.GameLogic.Judgement
{
    [Serializable]
    public class LaneJudgementSetting
    {
        [Tooltip("유효 Lane 유지 성공 판정 간격입니다.")]
        [Min(0.01f)]
        [SerializeField]
        private double _tickInterval = 0.25d;

        public double TickInterval => _tickInterval;

        /// <summary>
        /// 플레이어가 현재 또는 다음 유효 Lane에 있는지 판정합니다.
        /// </summary>
        public bool ValidateLane(
            int playerLaneIndex,
            int currentAllowedLaneIndex,
            int nextAllowedLaneIndex)
        {
            if (playerLaneIndex < 0)
                return false;

            return playerLaneIndex == currentAllowedLaneIndex
                   || playerLaneIndex == nextAllowedLaneIndex;
        }

        /// <summary>
        /// Tick 시점에 성공 판정을 발생시킬 수 있는지 검사합니다.
        /// </summary>
        public bool ValidateTick(
            int playerLaneIndex,
            int currentAllowedLaneIndex,
            int nextAllowedLaneIndex,
            double currentTime,
            double nextTickTime)
        {
            if (currentTime < nextTickTime)
                return false;

            return ValidateLane(
                playerLaneIndex,
                currentAllowedLaneIndex,
                nextAllowedLaneIndex
            );
        }

        public void ClampValues()
        {
            _tickInterval =
                Math.Max(0.01d, _tickInterval);
        }
    }
}