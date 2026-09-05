using System;
using UnityEngine;

namespace YWJ.GameLogic.Effect
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TabShardEffect : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [Header("Movement Curve")]
        [Tooltip(
            "시간에 따른 이동 속도 배수입니다. " +
            "X축은 진행도, Y축은 속도 배수입니다."
        )]
        [SerializeField]
        private AnimationCurve _speedCurve =
            new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.7f, 0.65f),
                new Keyframe(1f, 0f)
            );

        [Header("Direction Slerp")]
        [Tooltip(
            "활성화하면 시작 방향에서 목표 방향으로 " +
            "Slerp하여 살짝 휘어집니다."
        )]
        [SerializeField]
        private bool _useDirectionSlerp = true;

        [Tooltip(
            "최종 방향이 시작 방향에서 " +
            "회전할 수 있는 각도 범위입니다."
        )]
        [SerializeField]
        private Vector2 _directionCurveAngleRange =
            new Vector2(-15f, 15f);

        [Tooltip(
            "방향이 목표 방향으로 전환되는 정도입니다."
        )]
        [SerializeField]
        private AnimationCurve _directionSlerpCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Fade")]
        [SerializeField]
        private AnimationCurve _alphaCurve =
            new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.75f, 1f),
                new Keyframe(1f, 0f)
            );

        private Vector2 _startDirection;
        private Vector2 _targetDirection;

        private float _speed;
        private float _angularSpeed;
        private float _lifeTime;
        private float _elapsedTime;

        private Color _originalColor;

        private Action<TabShardEffect>
            _returnCallback;

        private bool _isPlaying;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            UpdateEffect();
        }

        public void Play(
            Vector3 position,
            Vector2 velocity,
            float angularSpeed,
            float lifeTime,
            Vector3 scale,
            Color color,
            Action<TabShardEffect> returnCallback)
        {
            transform.position = position;
            transform.localScale = scale;
            transform.rotation = Quaternion.identity;

            _speed = velocity.magnitude;

            _startDirection =
                velocity.sqrMagnitude > 0f
                    ? velocity.normalized
                    : Vector2.right;

            _targetDirection =
                CreateTargetDirection(
                    _startDirection
                );

            _angularSpeed = angularSpeed;
            _lifeTime = Mathf.Max(0.01f, lifeTime);
            _elapsedTime = 0f;

            _originalColor = color;
            _returnCallback = returnCallback;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color =
                    _originalColor;
            }

            _isPlaying = true;

            gameObject.SetActive(true);
        }

        private void UpdateEffect()
        {
            _elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    _elapsedTime / _lifeTime
                );

            UpdateMovement(normalizedTime);
            UpdateRotation();
            UpdateAlpha(normalizedTime);

            if (_elapsedTime >= _lifeTime)
            {
                StopEffect();
            }
        }

        private void UpdateMovement(
            float normalizedTime)
        {
            float currentSpeedMultiplier =
                _speedCurve != null
                    ? _speedCurve.Evaluate(
                        normalizedTime
                    )
                    : 1f;

            Vector2 currentDirection =
                GetCurrentDirection(
                    normalizedTime
                );

            Vector2 movement =
                currentDirection *
                _speed *
                currentSpeedMultiplier *
                Time.deltaTime;

            transform.position +=
                (Vector3)movement;
        }

        private Vector2 GetCurrentDirection(
            float normalizedTime)
        {
            if (!_useDirectionSlerp)
            {
                return _startDirection;
            }

            float slerpTime =
                _directionSlerpCurve != null
                    ? _directionSlerpCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;

            Vector3 currentDirection =
                Vector3.Slerp(
                    _startDirection,
                    _targetDirection,
                    Mathf.Clamp01(slerpTime)
                );

            return new Vector2(
                currentDirection.x,
                currentDirection.y
            ).normalized;
        }

        private Vector2 CreateTargetDirection(
            Vector2 startDirection)
        {
            if (!_useDirectionSlerp)
            {
                return startDirection;
            }

            float minimumAngle =
                Mathf.Min(
                    _directionCurveAngleRange.x,
                    _directionCurveAngleRange.y
                );

            float maximumAngle =
                Mathf.Max(
                    _directionCurveAngleRange.x,
                    _directionCurveAngleRange.y
                );

            float randomAngle =
                UnityEngine.Random.Range(
                    minimumAngle,
                    maximumAngle
                );

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    randomAngle
                );

            Vector3 rotatedDirection =
                rotation *
                new Vector3(
                    startDirection.x,
                    startDirection.y,
                    0f
                );

            return new Vector2(
                rotatedDirection.x,
                rotatedDirection.y
            ).normalized;
        }

        private void UpdateRotation()
        {
            transform.Rotate(
                0f,
                0f,
                _angularSpeed *
                Time.deltaTime
            );
        }

        private void UpdateAlpha(
            float normalizedTime)
        {
            if (_spriteRenderer == null)
                return;

            float alphaMultiplier =
                _alphaCurve != null
                    ? _alphaCurve.Evaluate(
                        normalizedTime
                    )
                    : 1f;

            Color currentColor =
                _originalColor;

            currentColor.a *=
                Mathf.Clamp01(
                    alphaMultiplier
                );

            _spriteRenderer.color =
                currentColor;
        }

        private void StopEffect()
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;

            gameObject.SetActive(false);

            Action<TabShardEffect> callback =
                _returnCallback;

            _returnCallback = null;

            callback?.Invoke(this);
        }

        private void OnDisable()
        {
            /*
             * 풀 반환 과정에서 비활성화될 때
             * 이전 상태가 남지 않도록 합니다.
             */
            _isPlaying = false;
            _elapsedTime = 0f;
        }
    }
}