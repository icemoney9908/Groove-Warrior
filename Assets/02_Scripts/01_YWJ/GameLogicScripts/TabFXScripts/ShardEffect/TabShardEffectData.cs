using System;
using UnityEngine;

namespace YWJ.GameLogic.Effect
{
    [Serializable]
    public sealed class TabShardEffectData
    {
        [Header("Effect Type")]
        [SerializeField]
        private TabEffectType _effectType;

        [Header("Particle Effect")]
        [Tooltip(
            "ParticleEffectPoolManager에 등록한 Effect ID"
        )]
        [SerializeField]
        private string _effectId = "Tab_FX";

        [Tooltip(
            "이벤트 한 번에 파티클 프리팹을 재생할 횟수입니다."
        )]
        [Min(0)]
        [SerializeField]
        private int _playCount = 1;

        public TabEffectType EffectType =>
            _effectType;

        public string EffectId =>
            _effectId;

        public int PlayCount =>
            Mathf.Max(
                0,
                _playCount
            );
    }
}