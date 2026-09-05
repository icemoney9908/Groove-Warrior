using UnityEngine;
using YWJ.Effect;

namespace YWJ.GameLogic.Effect
{
    /// <summary>
    /// TabEffectRequested 이벤트를 받아
    /// 설정된 파티클 이펙트를 풀에서 재생합니다.
    /// </summary>
    public sealed class TabShardEffectController :
        MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [SerializeField]
        private GameScoreCalculator _scoreCalculator;

        [SerializeField]
        private ParticleEffectPoolManager
            _particleEffectPoolManager;

        [Tooltip("파티클이 분사되는 시작 위치")]
        [SerializeField]
        private Transform _spawnPoint;

        [Header("Effect Setting")]
        [SerializeField]
        private TabShardEffectSetting _effectSetting;

        [Header("Runtime")]
        [Range(1, 6)]
        [SerializeField]
        private int _currentTier = 1;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private bool _isSubscribed;

        private void Awake()
        {
            InitializeReferences();
            InitializeCurrentTier();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            /*
             * 다른 오브젝트의 Awake 순서 때문에
             * 참조를 찾지 못한 경우 한 번 더 시도합니다.
             */
            if (_tabSuccessChecker == null ||
                _scoreCalculator == null ||
                _particleEffectPoolManager == null)
            {
                InitializeReferences();
                InitializeCurrentTier();
                SubscribeEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ========================================
        // Initialization
        // ========================================

        private void InitializeReferences()
        {
            if (_tabSuccessChecker == null)
            {
                _tabSuccessChecker =
                    FindFirstObjectByType<
                        TabSuccessChecker>();
            }

            if (_scoreCalculator == null)
            {
                _scoreCalculator =
                    FindFirstObjectByType<
                        GameScoreCalculator>();
            }

            if (_particleEffectPoolManager == null)
            {
                _particleEffectPoolManager =
                    FindFirstObjectByType<
                        ParticleEffectPoolManager>();
            }

            if (_tabSuccessChecker == null)
            {
                Debug.LogError(
                    "[TabShardEffectController] " +
                    "TabSuccessChecker가 연결되지 않았습니다.",
                    this
                );
            }

            if (_scoreCalculator == null)
            {
                Debug.LogError(
                    "[TabShardEffectController] " +
                    "GameScoreCalculator가 연결되지 않았습니다.",
                    this
                );
            }

            if (_particleEffectPoolManager == null)
            {
                Debug.LogError(
                    "[TabShardEffectController] " +
                    "ParticleEffectPoolManager가 연결되지 않았습니다.",
                    this
                );
            }

            if (_spawnPoint == null)
            {
                Debug.LogError(
                    "[TabShardEffectController] " +
                    "Spawn Point가 연결되지 않았습니다.",
                    this
                );
            }

            if (_effectSetting == null)
            {
                Debug.LogError(
                    "[TabShardEffectController] " +
                    "TabShardEffectSetting이 연결되지 않았습니다.",
                    this
                );
            }
        }

        private void InitializeCurrentTier()
        {
            _currentTier =
                _scoreCalculator != null
                    ? Mathf.Clamp(
                        _scoreCalculator.MusicTier,
                        1,
                        6
                    )
                    : 1;
        }

        // ========================================
        // Event Subscription
        // ========================================

        private void SubscribeEvents()
        {
            if (_isSubscribed)
                return;

            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker
                    .TabEffectRequested +=
                    HandleTabEffectRequested;
            }

            if (_scoreCalculator != null)
            {
                _scoreCalculator
                    .MusicTierChanged +=
                    HandleMusicTierChanged;
            }

            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed)
                return;

            if (_tabSuccessChecker != null)
            {
                _tabSuccessChecker
                    .TabEffectRequested -=
                    HandleTabEffectRequested;
            }

            if (_scoreCalculator != null)
            {
                _scoreCalculator
                    .MusicTierChanged -=
                    HandleMusicTierChanged;
            }

            _isSubscribed = false;
        }

        // ========================================
        // Event Handling
        // ========================================

        private void HandleMusicTierChanged(
            int newTier)
        {
            _currentTier =
                Mathf.Clamp(
                    newTier,
                    1,
                    6
                );
        }

        private void HandleTabEffectRequested(
            TabEffectType effectType)
        {
            if (_effectSetting == null)
                return;

            if (!_effectSetting.IsSuccessEffect(
                    effectType))
            {
                return;
            }

            TabShardPlayData playData =
                _effectSetting.GetPlayData(
                    effectType,
                    _currentTier
                );

            if (string.IsNullOrWhiteSpace(
                    playData.EffectId))
            {
                return;
            }

            if (playData.PlayCount <= 0)
                return;

            PlayParticleEffects(
                playData
            );
        }

        // ========================================
        // Particle Play
        // ========================================

        private void PlayParticleEffects(
            TabShardPlayData playData)
        {
            if (_particleEffectPoolManager == null ||
                _spawnPoint == null)
            {
                return;
            }

            for (int i = 0;
                 i < playData.PlayCount;
                 i++)
            {
                PlaySingleParticleEffect(
                    playData
                );
            }
        }

        private void PlaySingleParticleEffect(
            TabShardPlayData playData)
        {
            float halfSpread =
                playData.SpreadAngle * 0.5f;

            float randomAngle =
                playData.BaseAngle +
                Random.Range(
                    -halfSpread,
                    halfSpread
                );

            Quaternion rotation =
                _spawnPoint.rotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    randomAngle
                );

            PooledParticleEffect effect =
                _particleEffectPoolManager.Play(
                    playData.EffectId,
                    _spawnPoint.position,
                    rotation,
                    parent: null,
                    autoReturn: true
                );

            if (_showDebugLog)
            {
                Debug.Log(
                    "[TabShardEffectController] " +
                    $"Particle 재생: " +
                    $"ID={playData.EffectId}, " +
                    $"Tier={_currentTier}, " +
                    $"Angle={randomAngle:F1}, " +
                    $"Success={effect != null}",
                    this
                );
            }
        }
    }
}