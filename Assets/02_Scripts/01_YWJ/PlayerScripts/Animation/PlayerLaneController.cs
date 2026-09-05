using UnityEngine;
using YWJ.GameLogic;

namespace YWJ.Player
{
    public class PlayerLaneController : MonoBehaviour
    {
        [Header("Lane Follow Checker")]
        [SerializeField]
        private LaneFallowChecker _laneFallowChecker = null;

        [Header("Lane Settings")]
        [Tooltip("전체 레인 개수입니다.")]
        [Min(1)]
        [SerializeField]
        private int _laneCount = 4;

        [Tooltip("월드 좌표 기준 가장 아래 레인의 중심 Y 좌표입니다.")]
        [SerializeField]
        private float _bottomLaneCenterY = -3f;

        [Tooltip("각 레인 중심 사이의 Y 간격입니다.")]
        [Min(0f)]
        [SerializeField]
        private float _laneSpacing = 2f;

        [Header("Movement")]
        [Tooltip("레인 변경 시 즉시 이동할지 여부입니다.")]
        [SerializeField]
        private bool _moveImmediately = true;

        [Tooltip("부드럽게 이동할 때의 이동 속도입니다.")]
        [Min(0f)]
        [SerializeField]
        private float _moveSpeed = 12f;

        [Header("Current State")]
        [SerializeField]
        private int _currentPlayerLaneIndex = -1;

        private Vector3 _targetPosition;

        public int CurrentPlayerLaneIndex =>
            _currentPlayerLaneIndex;

        private void Awake()
        {
            if (_laneFallowChecker == null)
            {
                _laneFallowChecker =
                    FindFirstObjectByType<LaneFallowChecker>();
            }

            _targetPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_laneFallowChecker == null)
            {
                Debug.LogError(
                    "LaneFallowChecker is not assigned " +
                    "in PlayerLaneController.",
                    this
                );

                return;
            }

            _laneFallowChecker.PlayerLaneChanged +=
                OnPlayerLaneChanged;
        }

        private void OnDisable()
        {
            if (_laneFallowChecker == null)
                return;

            _laneFallowChecker.PlayerLaneChanged -=
                OnPlayerLaneChanged;
        }

        private void Update()
        {
            if (_moveImmediately)
                return;

            UpdatePlayerPosition();
        }

        private void OnPlayerLaneChanged(
            int laneIndex)
        {
            if (!IsValidLaneIndex(laneIndex))
                return;

            _currentPlayerLaneIndex =
                laneIndex;

            float targetY =
                CalculateLaneCenterY(laneIndex);

            _targetPosition =
                transform.position;

            _targetPosition.y =
                targetY;

            if (_moveImmediately)
            {
                InitializePlayerPosition();
            }
        }

        private void InitializePlayerPosition()
        {
            transform.position =
                _targetPosition;
        }

        private void UpdatePlayerPosition()
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    _targetPosition,
                    _moveSpeed * Time.deltaTime
                );
        }

        private float CalculateLaneCenterY(
            int laneIndex)
        {
            return _bottomLaneCenterY
                   + laneIndex * _laneSpacing;
        }

        private bool IsValidLaneIndex(
            int laneIndex)
        {
            return laneIndex >= 0
                   && laneIndex < _laneCount;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _laneCount =
                Mathf.Max(1, _laneCount);

            _laneSpacing =
                Mathf.Max(0f, _laneSpacing);

            _moveSpeed =
                Mathf.Max(0f, _moveSpeed);
        }
#endif
    }
}