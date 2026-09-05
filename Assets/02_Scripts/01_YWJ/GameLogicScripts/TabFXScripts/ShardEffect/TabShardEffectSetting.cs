using System.Collections.Generic;
using UnityEngine;

namespace YWJ.GameLogic.Effect
{
    [CreateAssetMenu(
        fileName = "TabParticleEffectSetting",
        menuName =
            "YWJ/Effect/Tab Particle Effect Setting"
    )]
    public sealed class TabShardEffectSetting :
        ScriptableObject
    {
        [Header("Effect Type Settings")]
        [SerializeField]
        private List<TabShardEffectData>
            _effectSettings = new();

        [Header("Tier Settings")]
        [SerializeField]
        private List<TabShardTierEffectData>
            _tierSettings = new();

        [Header("Direction")]
        [Tooltip(
            "파티클 프리팹의 기준 회전 각도입니다. " +
            "0도는 오른쪽 방향입니다."
        )]
        [SerializeField]
        private float _baseAngle;

        [Tooltip(
            "기준 각도를 중심으로 각 파티클 프리팹의 " +
            "회전을 무작위로 분산할 전체 각도입니다."
        )]
        [Range(0f, 360f)]
        [SerializeField]
        private float _spreadAngle = 30f;

        public TabShardPlayData GetPlayData(
            TabEffectType effectType,
            int tier)
        {
            TabShardEffectData effectData =
                FindEffectSetting(
                    effectType
                );

            if (effectData == null)
            {
                return CreateEmptyPlayData();
            }

            TabShardTierEffectData tierData =
                FindTierSetting(
                    tier
                );

            float countMultiplier =
                tierData?.PlayCountMultiplier
                ?? 1f;

            int finalPlayCount =
                Mathf.RoundToInt(
                    effectData.PlayCount *
                    countMultiplier
                );

            return new TabShardPlayData(
                effectData.EffectId,
                finalPlayCount,
                _baseAngle,
                _spreadAngle
            );
        }

        public bool IsSuccessEffect(
            TabEffectType effectType)
        {
            switch (effectType)
            {
                case TabEffectType.TapSuccess:
                case TabEffectType.HoldStart:
                case TabEffectType.HoldTick:
                case TabEffectType.HoldComplete:
                    return true;

                case TabEffectType.TapFail:
                case TabEffectType.HoldFail:
                default:
                    return false;
            }
        }

        private TabShardEffectData FindEffectSetting(
            TabEffectType effectType)
        {
            if (_effectSettings == null)
                return null;

            for (int i = 0;
                 i < _effectSettings.Count;
                 i++)
            {
                TabShardEffectData setting =
                    _effectSettings[i];

                if (setting == null)
                    continue;

                if (setting.EffectType ==
                    effectType)
                {
                    return setting;
                }
            }

            return null;
        }

        private TabShardTierEffectData FindTierSetting(
            int tier)
        {
            if (_tierSettings == null)
                return null;

            tier = Mathf.Clamp(
                tier,
                1,
                6
            );

            for (int i = 0;
                 i < _tierSettings.Count;
                 i++)
            {
                TabShardTierEffectData setting =
                    _tierSettings[i];

                if (setting == null)
                    continue;

                if (setting.Tier == tier)
                {
                    return setting;
                }
            }

            return null;
        }

        private TabShardPlayData CreateEmptyPlayData()
        {
            return new TabShardPlayData(
                string.Empty,
                0,
                _baseAngle,
                _spreadAngle
            );
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _spreadAngle =
                Mathf.Clamp(
                    _spreadAngle,
                    0f,
                    360f
                );
        }
#endif
    }
}