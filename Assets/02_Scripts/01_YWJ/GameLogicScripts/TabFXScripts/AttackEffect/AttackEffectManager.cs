using UnityEngine;
using YWJ.GameLogic;
using YWJ.GameLogic.Effect;

namespace YWJ.Effect
{
    public sealed class AttackEffectController :
        MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private TabSuccessChecker _tabSuccessChecker;

        [SerializeField]
        private AttackEffectPoolManager
            _effectPoolManager;

        [Tooltip(
            "공격 이펙트를 표시할 위치입니다."
        )]
        [SerializeField]
        private Transform _attackEffectPoint;

        [Header("Effect")]
        [Tooltip(
            "AttackEffectPoolManager에 등록된 " +
            "공격 이펙트 ID 목록입니다."
        )]
        [SerializeField]
        private string[] _attackEffectIds =
        {
            "Player_Attack_FX_01",
            "Player_Attack_FX_02",
            "Player_Attack_FX_03",
            "Player_Attack_FX_04"
        };

        [Tooltip(
            "직전에 재생한 이펙트가 연속으로 " +
            "다시 나오지 않도록 합니다."
        )]
        [SerializeField]
        private bool _preventConsecutiveDuplicate = true;

        [SerializeField]
        private Vector3 _effectScale =
            Vector3.one;

        [SerializeField]
        private bool _flipX;

        [Tooltip(
            "-1이면 프리팹 설정이나 " +
            "애니메이션 길이를 사용합니다."
        )]
        [SerializeField]
        private float _duration = -1f;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog;

        private int _previousEffectIndex = -1;

        private void Awake()
        {
            InitializeReferences();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Start()
        {
            if (_tabSuccessChecker == null ||
                _effectPoolManager == null)
            {
                InitializeReferences();
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

            if (_effectPoolManager == null)
            {
                _effectPoolManager =
                    FindFirstObjectByType<
                        AttackEffectPoolManager>();
            }

            if (_tabSuccessChecker == null)
            {
                Debug.LogError(
                    "[AttackEffectController] " +
                    "TabSuccessChecker가 없습니다.",
                    this
                );
            }

            if (_effectPoolManager == null)
            {
                Debug.LogError(
                    "[AttackEffectController] " +
                    "AttackEffectPoolManager가 없습니다.",
                    this
                );
            }

            if (_attackEffectPoint == null)
            {
                Debug.LogError(
                    "[AttackEffectController] " +
                    "AttackEffectPoint가 없습니다.",
                    this
                );
            }
        }

        // ========================================
        // Event
        // ========================================

        private void SubscribeEvents()
        {
            if (_tabSuccessChecker == null)
                return;

            _tabSuccessChecker
                .TabEffectRequested -=
                HandleTabEffectRequested;

            _tabSuccessChecker
                .TabEffectRequested +=
                HandleTabEffectRequested;
        }

        private void UnsubscribeEvents()
        {
            if (_tabSuccessChecker == null)
                return;

            _tabSuccessChecker
                .TabEffectRequested -=
                HandleTabEffectRequested;
        }

        private void HandleTabEffectRequested(
            TabEffectType effectType)
        {
            switch (effectType)
            {
                case TabEffectType.TapSuccess:
                case TabEffectType.HoldStart:
                case TabEffectType.HoldComplete:
                    PlayAttackEffect();
                    break;
            }
        }

        // ========================================
        // Play
        // ========================================

        public void PlayAttackEffect()
        {
            if (_effectPoolManager == null ||
                _attackEffectPoint == null)
            {
                return;
            }

            string effectId =
                GetRandomEffectId();

            if (string.IsNullOrWhiteSpace(
                    effectId))
            {
                Debug.LogWarning(
                    "[AttackEffectController] " +
                    "재생 가능한 공격 이펙트 ID가 없습니다.",
                    this
                );

                return;
            }

            AttackEffect effect =
                _effectPoolManager.Play(
                    effectId,
                    _attackEffectPoint.position,
                    _attackEffectPoint.rotation,
                    _effectScale,
                    parent: null,
                    flipX: _flipX,
                    duration: _duration
                );

            if (_showDebugLog)
            {
                Debug.Log(
                    "[AttackEffectController] " +
                    $"공격 이펙트 재생: {effectId}, " +
                    $"Success={effect != null}",
                    this
                );
            }
        }

        private string GetRandomEffectId()
        {
            if (_attackEffectIds == null ||
                _attackEffectIds.Length == 0)
            {
                return string.Empty;
            }

            if (_attackEffectIds.Length == 1)
            {
                _previousEffectIndex = 0;

                return _attackEffectIds[0];
            }

            int selectedIndex;

            if (_preventConsecutiveDuplicate &&
                _previousEffectIndex >= 0)
            {
                /*
                 * 이전 인덱스를 제외한 범위에서 뽑습니다.
                 *
                 * 4개 중 하나를 제외하면
                 * Random.Range(0, 3)으로 먼저 선택하고,
                 * 이전 인덱스 이상이면 1을 더합니다.
                 */
                selectedIndex =
                    Random.Range(
                        0,
                        _attackEffectIds.Length - 1
                    );

                if (selectedIndex >=
                    _previousEffectIndex)
                {
                    selectedIndex++;
                }
            }
            else
            {
                selectedIndex =
                    Random.Range(
                        0,
                        _attackEffectIds.Length
                    );
            }

            _previousEffectIndex =
                selectedIndex;

            return _attackEffectIds[
                selectedIndex
            ];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_attackEffectIds == null)
                return;

            for (int i = 0;
                 i < _attackEffectIds.Length;
                 i++)
            {
                if (string.IsNullOrWhiteSpace(
                        _attackEffectIds[i]))
                {
                    Debug.LogWarning(
                        "[AttackEffectController] " +
                        $"Attack Effect IDs의 " +
                        $"Element {i}가 비어 있습니다.",
                        this
                    );
                }
            }
        }
#endif
    }
}