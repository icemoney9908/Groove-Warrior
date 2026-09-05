using UnityEngine;

namespace YWJ.GameLogic.Effect
{
    public readonly struct TabShardPlayData
    {
        public string EffectId { get; }

        public int PlayCount { get; }

        public float BaseAngle { get; }

        public float SpreadAngle { get; }

        public TabShardPlayData(
            string effectId,
            int playCount,
            float baseAngle,
            float spreadAngle)
        {
            EffectId = effectId;

            PlayCount =
                Mathf.Max(
                    0,
                    playCount
                );

            BaseAngle = baseAngle;

            SpreadAngle =
                Mathf.Clamp(
                    spreadAngle,
                    0f,
                    360f
                );
        }
    }
}