using System;
using UnityEngine;

namespace YWJ.GameLogic.Effect
{
    [Serializable]
    public sealed class TabShardTierEffectData
    {
        [Header("Tier")]
        [Range(1, 6)]
        [SerializeField]
        private int _tier = 1;

        [Header("Play Count")]
        [Tooltip(
            "기본 파티클 재생 횟수에 적용할 배수입니다."
        )]
        [Min(0f)]
        [SerializeField]
        private float _playCountMultiplier = 1f;

        public int Tier =>
            Mathf.Clamp(
                _tier,
                1,
                6
            );

        public float PlayCountMultiplier =>
            Mathf.Max(
                0f,
                _playCountMultiplier
            );
    }
}