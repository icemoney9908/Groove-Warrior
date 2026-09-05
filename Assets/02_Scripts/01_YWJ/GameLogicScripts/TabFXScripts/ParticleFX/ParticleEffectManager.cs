using UnityEngine;
using YWJ.GameStateMachine;
using YWJ.GameLogic;

namespace YWJ.Effect
{
    /// <summary>
    /// 탭 성공 시 일회성 FX를 재생하고,
    /// 전달받은 콤보에 따라 플레이어 Aura를 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleEffectManager : MonoBehaviour
    {
        private enum AuraTier
        {
            None,
            Tier1,
            Tier2
        }

        [Header("Dependencies")]

        [SerializeField]
        private ParticleEffectPoolManager
            _particleEffectPoolManager;

        [Header("Effect Points")]
        [Tooltip("Aura가 플레이어 뒤에서 따라다닐 위치")]
        [SerializeField]
        private Transform _auraEffectPoint;

        [Header("Effect IDs")]
        [SerializeField]
        private string _tier1AuraEffectId =
            "Tier1_Aura";

        [SerializeField]
        private string _tier2AuraEffectId =
            "Tier2_Aura";

        [Header("Aura Combo Threshold")]
        [Min(1)]
        [SerializeField]
        private int _tier1RequiredCombo = 20;

        [Min(1)]
        [SerializeField]
        private int _tier2RequiredCombo = 60;

        [Header("Runtime")]
        [SerializeField]
        private int _currentCombo;

        [SerializeField]
        private AuraTier _currentAuraTier =
            AuraTier.None;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private PooledParticleEffect
            _activeAuraEffect;

        private void Awake()
        {
            InitializeReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();

            GameStateEventBus.OnMusicModeStateEntered +=
                ResetEffects;

            GameStateEventBus.OnMusicModeStateExited +=
                ResetEffects;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            GameStateEventBus.OnMusicModeStateEntered -=
                ResetEffects;

            GameStateEventBus.OnMusicModeStateExited -=
                ResetEffects;

            StopCurrentAura(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _tier1RequiredCombo =
                Mathf.Max(
                    1,
                    _tier1RequiredCombo
                );

            _tier2RequiredCombo =
                Mathf.Max(
                    _tier1RequiredCombo + 1,
                    _tier2RequiredCombo
                );
        }
#endif

        // ========================================
        // Initialization
        // ========================================

        private void InitializeReferences()
        {
            if (_particleEffectPoolManager == null)
            {
                _particleEffectPoolManager =
                    FindFirstObjectByType<
                        ParticleEffectPoolManager>();
            }

            if (_particleEffectPoolManager == null)
            {
                Debug.LogError(
                    "[ParticleEffectManager] " +
                    "ParticleEffectPoolManager가 연결되지 않았습니다.",
                    this
                );
            }
        }

        // ========================================
        // Event
        // ========================================

        private void SubscribeEvents()
        {

        }

        private void UnsubscribeEvents()
        {

        }

        // ========================================
        // Combo Aura
        // ========================================

        /// <summary>
        /// GameScoreCalculator에서 현재 콤보를 전달합니다.
        /// 콤보 단계가 바뀌었을 때만 Aura를 교체합니다.
        /// </summary>
        public void SetCombo(int combo)
        {
            _currentCombo =
                Mathf.Max(
                    0,
                    combo
                );

            AuraTier nextTier =
                CalculateAuraTier(
                    _currentCombo
                );

            if (_currentAuraTier == nextTier)
                return;

            ChangeAura(nextTier);

            if (_showDebugLog)
            {
                Debug.Log(
                    "[ParticleEffectManager] " +
                    $"Combo={_currentCombo}, " +
                    $"Aura={_currentAuraTier}",
                    this
                );
            }
        }

        private AuraTier CalculateAuraTier(
            int combo)
        {
            // 높은 단계부터 검사합니다.
            if (combo >= _tier2RequiredCombo)
            {
                return AuraTier.Tier2;
            }

            if (combo >= _tier1RequiredCombo)
            {
                return AuraTier.Tier1;
            }

            return AuraTier.None;
        }

        private void ChangeAura(
            AuraTier nextTier)
        {
            StopCurrentAura(true);

            _currentAuraTier = nextTier;

            switch (nextTier)
            {
                case AuraTier.None:
                    return;

                case AuraTier.Tier1:
                    PlayAura(
                        _tier1AuraEffectId
                    );
                    break;

                case AuraTier.Tier2:
                    PlayAura(
                        _tier2AuraEffectId
                    );
                    break;
            }
        }

        private void PlayAura(
            string effectId)
        {
            if (_particleEffectPoolManager == null)
                return;

            if (string.IsNullOrWhiteSpace(effectId))
                return;

            Transform effectPoint =
                _auraEffectPoint != null
                    ? _auraEffectPoint
                    : transform;

            /*
             * Aura는 플레이어를 따라다니는 지속형 FX입니다.
             *
             * effectPoint를 추적 대상으로 넘기고,
             * 자동 반환은 사용하지 않습니다.
             */
            _activeAuraEffect =
                _particleEffectPoolManager.Play(
                    effectId,
                    effectPoint.position,
                    effectPoint.rotation,
                    effectPoint,
                    false
                );

            if (_activeAuraEffect == null)
            {
                Debug.LogWarning(
                    "[ParticleEffectManager] " +
                    $"Aura 재생 실패: {effectId}",
                    this
                );
            }
        }

        private void StopCurrentAura(
            bool immediate)
        {
            if (_activeAuraEffect == null)
                return;

            _activeAuraEffect.Stop(immediate);
            _activeAuraEffect = null;
        }

        // ========================================
        // Reset
        // ========================================

        public void ResetEffects()
        {
            _currentCombo = 0;
            _currentAuraTier = AuraTier.None;

            StopCurrentAura(true);
        }
    }
}