using System;
using UnityEngine;
using UnityEngine.InputSystem;
using KKH.Gameplay;
using YWJ.GameLogic.Judgement;
using YWJ.GameStateMachine;

namespace YWJ.GameLogic
{
    public class LaneFallowChecker : MonoBehaviour
    {
        [Header("Music Conductor")]
        [SerializeField]
        private MusicConductor _musicConductor = null;

        [Header("Judgement Setting")]
        [SerializeField]
        private JudgementSetting _judgementSetting = null;

        [Header("Lane Area")]
        [Tooltip("전체 Lane 개수입니다.")]
        [Min(1)]
        [SerializeField]
        private int _laneCount = 4;

        [Tooltip("Lane 영역의 화면 하단 Y 비율입니다.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _laneAreaMinYRatio = 0.2f;

        [Tooltip("Lane 영역의 화면 상단 Y 비율입니다.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _laneAreaMaxYRatio = 0.95f;

        [Header("Player Lane")]
        [Tooltip("현재 플레이어가 위치한 Lane입니다. 아래부터 0번입니다.")]
        [SerializeField]
        private int _currentPlayerLaneIndex = -1;

        [Header("Allowed Lanes")]
        [Tooltip("현재 유효 Lane입니다.")]
        [SerializeField]
        private int _currentAllowedLaneIndex = -1;

        [Tooltip("다음 유효 Lane입니다.")]
        [SerializeField]
        private int _nextAllowedLaneIndex = -1;

        [Header("Debug")]
        [SerializeField]
        private bool _showDebugLog = true;

        // 플레이어 레인이 변경되었을 때 발생합니다.
        // 플레이어 위치 변경용 이벤트입니다.
        public event Action<int> PlayerLaneChanged;

        // 유효 레인 구성이 변경되었을 때 발생합니다.
        public event Action<int, int> AllowedLanesChanged;

        // 유효 레인 안에서 Tick 성공 시 발생합니다.
        public event Action LaneFollowSuccess;

        // 유효 레인 변경 순간 플레이어가 새 유효 레인에 없으면 발생합니다.
        public event Action LaneFollowFail;

        private LaneJudgementSetting _laneJudgement;

        private Vector2 _currentMousePosition =
            Vector2.zero;

        private bool _isCheckingLane = false;

        // 최초 유효 레인 설정인지 구분합니다.
        // 최초 설정에서는 실패 판정을 발생시키지 않습니다.
        private bool _hasAllowedLaneInitialized = false;

        private double _nextTickTime = 0.0d;

        public int CurrentPlayerLaneIndex =>
            _currentPlayerLaneIndex;

        public int CurrentAllowedLaneIndex =>
            _currentAllowedLaneIndex;

        public int NextAllowedLaneIndex =>
            _nextAllowedLaneIndex;

        public bool IsPlayerInsideAllowedLane
        {
            get
            {
                if (_laneJudgement == null)
                    return false;

                return _laneJudgement.ValidateLane(
                    _currentPlayerLaneIndex,
                    _currentAllowedLaneIndex,
                    _nextAllowedLaneIndex
                );
            }
        }

        private void Awake()
        {
            if (_musicConductor == null)
            {
                _musicConductor =
                    FindFirstObjectByType<MusicConductor>();
            }

            if (_judgementSetting == null)
            {
                Debug.LogError(
                    "JudgementSetting is not assigned " +
                    "in LaneFallowChecker.",
                    this
                );

                return;
            }

            _laneJudgement =
                _judgementSetting.Lane;

            if (_laneJudgement == null)
            {
                Debug.LogError(
                    "LaneJudgementSetting is null " +
                    "in JudgementSetting.",
                    this
                );
            }
        }

        private void OnEnable()
        {
            GameStateEventBus.OnMusicModeStateEntered +=
                StartLaneChecking;

            GameStateEventBus.OnMusicModeStateExited +=
                StopLaneChecking;
        }

        private void OnDisable()
        {
            GameStateEventBus.OnMusicModeStateEntered -=
                StartLaneChecking;

            GameStateEventBus.OnMusicModeStateExited -=
                StopLaneChecking;
        }

        private void Update()
        {
            if (!_isCheckingLane)
                return;

            if (_laneJudgement == null)
                return;

            UpdateMousePosition();
            UpdateCurrentPlayerLane();
            UpdateLaneSuccessByInterval();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _laneCount =
                Mathf.Max(1, _laneCount);

            _laneAreaMinYRatio =
                Mathf.Clamp01(_laneAreaMinYRatio);

            _laneAreaMaxYRatio =
                Mathf.Clamp01(_laneAreaMaxYRatio);

            if (_laneAreaMaxYRatio <
                _laneAreaMinYRatio)
            {
                _laneAreaMaxYRatio =
                    _laneAreaMinYRatio;
            }
        }
#endif

        // ========================================
        // Lane Checking Start / Stop
        // ========================================

        private void StartLaneChecking()
        {
            _isCheckingLane = true;
            _hasAllowedLaneInitialized = false;

            UpdateMousePosition();

            _currentPlayerLaneIndex =
                CalculateLaneIndex(
                    _currentMousePosition.y
                );

            if (_currentPlayerLaneIndex >= 0)
            {
                PlayerLaneChanged?.Invoke(
                    _currentPlayerLaneIndex
                );
            }

            _nextTickTime =
                GetCurrentTime()
                + _laneJudgement.TickInterval;

            /*
             * 임시 허용 레인 설정입니다.
             * 추후 다른 파트에서 SetAllowedLanes()를 호출한다면
             * 이 호출은 제거하면 됩니다.
             */
            RefreshAllowedLanes();
        }

        private void StopLaneChecking()
        {
            _isCheckingLane = false;
            _hasAllowedLaneInitialized = false;

            _currentMousePosition =
                Vector2.zero;

            _currentPlayerLaneIndex = -1;
            _currentAllowedLaneIndex = -1;
            _nextAllowedLaneIndex = -1;

            _nextTickTime = 0.0d;
        }

        // ========================================
        // Player Lane
        // ========================================

        private void UpdateMousePosition()
        {
            if (Mouse.current == null)
                return;

            _currentMousePosition =
                Mouse.current.position.ReadValue();
        }

        private void UpdateCurrentPlayerLane()
        {
            int newLaneIndex =
                CalculateLaneIndex(
                    _currentMousePosition.y
                );

            if (newLaneIndex ==
                _currentPlayerLaneIndex)
            {
                return;
            }

            int previousLaneIndex =
                _currentPlayerLaneIndex;

            _currentPlayerLaneIndex =
                newLaneIndex;

            if (_showDebugLog)
            {
                Debug.Log(
                    $"Player Lane Changed: " +
                    $"{previousLaneIndex} -> " +
                    $"{_currentPlayerLaneIndex}",
                    this
                );
            }

            /*
             * 레인이 변경되었다는 사실만 알립니다.
             * 여기서는 성공/실패 판정을 하지 않습니다.
             */
            PlayerLaneChanged?.Invoke(
                _currentPlayerLaneIndex
            );
        }

        private int CalculateLaneIndex(
            float mouseScreenY)
        {
            if (_laneCount <= 0)
                return -1;

            float screenHeight =
                Screen.height;

            float minimumY =
                screenHeight * _laneAreaMinYRatio;

            float maximumY =
                screenHeight * _laneAreaMaxYRatio;

            if (mouseScreenY < minimumY)
            {
                return 0;
            }

            if (mouseScreenY > maximumY)
            {
                return _laneCount - 1;
            }

            float laneAreaHeight =
                maximumY - minimumY;

            if (laneAreaHeight <= 0f)
                return -1;

            float normalizedY =
                (mouseScreenY - minimumY)
                / laneAreaHeight;

            int laneIndex =
                Mathf.FloorToInt(
                    normalizedY * _laneCount
                );

            return Mathf.Clamp(
                laneIndex,
                0,
                _laneCount - 1
            );
        }

        // ========================================
        // Lane Tick Success
        // ========================================

        private void UpdateLaneSuccessByInterval()
        {
            if (!_hasAllowedLaneInitialized)
                return;

            if (!HasValidAllowedLanes())
                return;

            double currentTime =
                GetCurrentTime();

            while (currentTime >= _nextTickTime)
            {
                CheckLaneTickSuccess();

                _nextTickTime +=
                    _laneJudgement.TickInterval;
            }
        }

        private void CheckLaneTickSuccess()
        {
            bool isSuccess =
                _laneJudgement.ValidateLane(
                    _currentPlayerLaneIndex,
                    _currentAllowedLaneIndex,
                    _nextAllowedLaneIndex
                );

            /*
             * Tick 시점에 유효 Lane에 있으면 성공합니다.
             * 유효 Lane 밖이어도 여기서는 실패를 발생시키지 않습니다.
             */
            if (!isSuccess)
            {
                if (_showDebugLog)
                {
                    Debug.Log(
                        $"Lane Follow Fail / " +
                        $"Player={_currentPlayerLaneIndex}, " +
                        $"Current={_currentAllowedLaneIndex}, " +
                        $"Next={_nextAllowedLaneIndex}",
                        this
                    );
                }

                LaneFollowFail?.Invoke();
                return;
            }


            if (_showDebugLog)
            {
                Debug.Log(
                    $"Lane Follow Success / " +
                    $"Player={_currentPlayerLaneIndex}, " +
                    $"Current={_currentAllowedLaneIndex}, " +
                    $"Next={_nextAllowedLaneIndex}",
                    this
                );
            }

            LaneFollowSuccess?.Invoke();
        }

        // ========================================
        // Allowed Lane
        // ========================================

        /// <summary>
        /// 현재 유효 Lane과 다음 유효 Lane을 설정합니다.
        ///
        /// 최초 설정에서는 실패 판정을 하지 않습니다.
        /// 최초 설정 이후 유효 Lane이 변경되면,
        /// 플레이어가 변경된 유효 Lane에 있는지 즉시 판정합니다.
        /// </summary>
        public void SetAllowedLanes(
            int currentLaneIndex,
            int nextLaneIndex)
        {
            if (!IsValidLaneIndex(
                    currentLaneIndex))
            {
                Debug.LogWarning(
                    $"Invalid current allowed Lane: " +
                    $"{currentLaneIndex}",
                    this
                );

                return;
            }

            if (!IsValidLaneIndex(
                    nextLaneIndex))
            {
                Debug.LogWarning(
                    $"Invalid next allowed Lane: " +
                    $"{nextLaneIndex}",
                    this
                );

                return;
            }

            bool isChanged =
                _currentAllowedLaneIndex != currentLaneIndex
                || _nextAllowedLaneIndex != nextLaneIndex;

            if (!isChanged)
                return;

            bool wasInitialized =
                _hasAllowedLaneInitialized;

            _currentAllowedLaneIndex =
                currentLaneIndex;

            _nextAllowedLaneIndex =
                nextLaneIndex;

            _hasAllowedLaneInitialized = true;

            // 유효 레인이 변경될 때 Tick 시각도 새로 시작합니다.
            _nextTickTime =
                GetCurrentTime()
                + _laneJudgement.TickInterval;

            if (_showDebugLog)
            {
                Debug.Log(
                    $"Allowed Lanes Changed / " +
                    $"Current={_currentAllowedLaneIndex}, " +
                    $"Next={_nextAllowedLaneIndex}",
                    this
                );
            }

            AllowedLanesChanged?.Invoke(
                _currentAllowedLaneIndex,
                _nextAllowedLaneIndex
            );

            /*
             * 최초 유효 레인 설정에서는 실패시키지 않습니다.
             * 그 이후 실제 변경에서만 현재 플레이어 위치를 판정합니다.
             */
            if (wasInitialized)
            {
                ValidatePlayerAfterAllowedLaneChanged();
            }
        }

        private void ValidatePlayerAfterAllowedLaneChanged()
        {
            bool isValid =
                _laneJudgement.ValidateLane(
                    _currentPlayerLaneIndex,
                    _currentAllowedLaneIndex,
                    _nextAllowedLaneIndex
                );

            if (isValid)
            {
                if (_showDebugLog)
                {
                    Debug.Log(
                        $"Allowed Lane Change Valid / " +
                        $"Player={_currentPlayerLaneIndex}",
                        this
                    );
                }

                return;
            }

            if (_showDebugLog)
            {
                Debug.Log(
                    $"Allowed Lane Change Fail / " +
                    $"Player={_currentPlayerLaneIndex}, " +
                    $"Current={_currentAllowedLaneIndex}, " +
                    $"Next={_nextAllowedLaneIndex}",
                    this
                );
            }

            LaneFollowFail?.Invoke();
        }

        /// <summary>
        /// 다음 유효 Lane으로 진행합니다.
        ///
        /// 기존 Next가 새로운 Current가 되고,
        /// 전달받은 Lane이 새로운 Next가 됩니다.
        /// </summary>
        public void AdvanceAllowedLane(
            int newNextLaneIndex)
        {
            if (!IsValidLaneIndex(
                    newNextLaneIndex))
            {
                Debug.LogWarning(
                    $"Invalid new next Lane: " +
                    $"{newNextLaneIndex}",
                    this
                );

                return;
            }

            if (!IsValidLaneIndex(
                    _nextAllowedLaneIndex))
            {
                Debug.LogWarning(
                    "Next allowed Lane is not initialized.",
                    this
                );

                return;
            }

            SetAllowedLanes(
                _nextAllowedLaneIndex,
                newNextLaneIndex
            );
        }

        /// <summary>
        /// 테스트용으로 임의의 Current/Next Lane을 설정합니다.
        /// 추후 외부 파트가 SetAllowedLanes()를 호출하면 됩니다.
        /// </summary>
        public void RefreshAllowedLanes()
        {
            int currentLaneIndex =
                RequestCurrentAllowedLane();

            int nextLaneIndex =
                RequestNextAllowedLane(
                    currentLaneIndex
                );

            SetAllowedLanes(
                currentLaneIndex,
                nextLaneIndex
            );
        }

        /// <summary>
        /// 추후 외부 데이터로 대체할 임시 함수입니다.
        /// </summary>
        private int RequestCurrentAllowedLane()
        {
            if (_laneCount <= 0)
                return -1;

            return UnityEngine.Random.Range(
                0,
                _laneCount
            );
        }

        /// <summary>
        /// 추후 외부 데이터로 대체할 임시 함수입니다.
        /// 현재는 Current와 인접한 Lane을 반환합니다.
        /// </summary>
        private int RequestNextAllowedLane(
            int currentLaneIndex)
        {
            if (!IsValidLaneIndex(
                    currentLaneIndex))
            {
                return -1;
            }

            if (_laneCount <= 1)
                return currentLaneIndex;

            bool canMoveDown =
                currentLaneIndex > 0;

            bool canMoveUp =
                currentLaneIndex
                < _laneCount - 1;

            if (canMoveDown && canMoveUp)
            {
                return UnityEngine.Random.value < 0.5f
                    ? currentLaneIndex - 1
                    : currentLaneIndex + 1;
            }

            if (canMoveDown)
                return currentLaneIndex - 1;

            return currentLaneIndex + 1;
        }

        // ========================================
        // Utility
        // ========================================

        private bool HasValidAllowedLanes()
        {
            return IsValidLaneIndex(
                       _currentAllowedLaneIndex)
                   && IsValidLaneIndex(
                       _nextAllowedLaneIndex);
        }

        private bool IsValidLaneIndex(
            int laneIndex)
        {
            return laneIndex >= 0
                   && laneIndex < _laneCount;
        }

        private double GetCurrentTime()
        {
            if (_musicConductor != null)
            {
                return _musicConductor
                    .CurrentSongTimeSecondsExact;
            }

            return Time.unscaledTimeAsDouble;
        }
    }
}