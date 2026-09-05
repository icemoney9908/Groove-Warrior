using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace YWJ.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        [Header("Cinemachine")]
        [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [SerializeField]
        private CinemachineFollow _cinemachineFollow;

        [SerializeField]
        private CinemachineImpulseSource _impulseSource;

        [Header("Post Processing")]
        [SerializeField]
        private Volume _globalVolume;

        [Header("Initial Fixed Camera")]
        [SerializeField]
        private Transform _initialFixedPoint;

        [SerializeField]
        private Vector3 _initialFixedPosition =
            new Vector3(0f, 0f, -10f);

        [Header("Default Camera")]
        [SerializeField]
        private Vector3 _defaultFollowOffset =
            new Vector3(0f, 0f, -10f);

        [Min(0.01f)]
        [SerializeField]
        private float _defaultOrthographicSize = 5f;

        [Header("Camera Shake")]

        [Min(0f)]
        [SerializeField]
        private float _defaultShakeStrength = 1f;

        [Min(0.01f)]
        [SerializeField]
        private float _defaultShakeInterval = 0.08f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _shakeRandomness = 0.35f;

        [SerializeField]
        private Vector3 _defaultShakeDirection = Vector3.down;

        [SerializeField]
        private bool _fadeShakeStrength = true;

        [Header("Time")]
        [SerializeField]
        private bool _useUnscaledTime = true;

        private ColorAdjustments _colorAdjustments;

        private CameraControlState _controlState =
            CameraControlState.Fixed;

        private Transform _currentFollowTarget;

        private Coroutine _positionCoroutine;
        private Coroutine _offsetCoroutine;
        private Coroutine _zoomCoroutine;
        private Coroutine _monochromeCoroutine;
        private Coroutine _shakeCoroutine;

        public CameraControlState ControlState =>
            _controlState;

        public Transform CurrentFollowTarget =>
            _currentFollowTarget;

        public bool IsFollowing =>
            _controlState == CameraControlState.Following;

        private void Awake()
        {
            InitializeReferences();
            InitializeVolume();
        }

        private void Start()
        {
            InitializeCameraState();
        }

        private void InitializeReferences()
        {
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera =
                    GetComponent<CinemachineCamera>();
            }

            if (_cinemachineFollow == null)
            {
                _cinemachineFollow =
                    GetComponent<CinemachineFollow>();
            }

            if (_impulseSource == null)
            {
                _impulseSource =
                    GetComponent<CinemachineImpulseSource>();
            }
        }

        private void InitializeVolume()
        {
            if (_globalVolume == null)
            {
                Debug.LogWarning(
                    "[CameraController] Global Volume이 없습니다.",
                    this
                );

                return;
            }

            VolumeProfile profile = _globalVolume.profile;

            if (profile == null)
            {
                Debug.LogWarning(
                    "[CameraController] Volume Profile이 없습니다.",
                    this
                );

                return;
            }

            if (!profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments =
                    profile.Add<ColorAdjustments>(true);
            }

            _colorAdjustments.saturation.overrideState = true;
        }

        private void InitializeCameraState()
        {
            SetOrthographicSizeImmediate(
                _defaultOrthographicSize
            );

            if (_cinemachineFollow != null)
            {
                _cinemachineFollow.FollowOffset =
                    _defaultFollowOffset;
            }

            Vector3 initialPosition =
                _initialFixedPoint != null
                    ? _initialFixedPoint.position
                    : _initialFixedPosition;

            FixAtPositionImmediate(initialPosition);
        }

        // =========================================================
        // Follow
        // =========================================================

        public void FollowTarget(
            Transform target)
        {
            FollowTarget(
                target,
                _defaultFollowOffset
            );
        }

        public void FollowTarget(
            Transform target,
            Vector3 followOffset)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    "[CameraController] 추적 대상이 없습니다.",
                    this
                );

                return;
            }

            if (_cinemachineCamera == null)
            {
                return;
            }

            StopPositionTransition();

            if (_cinemachineFollow != null)
            {
                _cinemachineFollow.FollowOffset =
                    followOffset;
            }

            _currentFollowTarget = target;
            _controlState = CameraControlState.Following;

            _cinemachineCamera.Target.TrackingTarget =
                target;

            _cinemachineCamera.PreviousStateIsValid =
                false;
        }

        // =========================================================
        // Fixed camera
        // =========================================================

        /// <summary>
        /// 현재 실제 화면이 보고 있는 위치에서 고정한다.
        /// 추적 중인 화면이 튀지 않고 그대로 멈춘다.
        /// </summary>
        public void FixCurrentPosition()
        {
            StopPositionTransition();

            Vector3 currentCameraPosition =
                GetCurrentRenderedCameraPosition();

            ClearFollowTargetInternal();

            transform.position = currentCameraPosition;

            InvalidateCameraState();
        }

        /// <summary>
        /// 지정 위치에 즉시 고정한다.
        /// </summary>
        public void FixAtPositionImmediate(
            Vector3 position)
        {
            StopPositionTransition();
            ClearFollowTargetInternal();

            transform.position = position;

            InvalidateCameraState();
        }

        /// <summary>
        /// 지정 위치로 이동한 뒤 고정한다.
        /// </summary>
        public void FixAtPosition(
            Vector3 position,
            float duration)
        {
            FixAtPosition(
                position,
                duration,
                AnimationCurve.EaseInOut(
                    0f,
                    0f,
                    1f,
                    1f
                )
            );
        }

        public void FixAtPosition(
            Vector3 position,
            float duration,
            AnimationCurve curve)
        {
            StopPositionTransition();

            /*
             * 먼저 현재 실제 화면 위치를 보관해야
             * 추적을 끊을 때 화면이 튀지 않는다.
             */
            Vector3 startPosition =
                GetCurrentRenderedCameraPosition();

            ClearFollowTargetInternal();

            transform.position = startPosition;

            _positionCoroutine = StartCoroutine(
                FixPositionRoutine(
                    startPosition,
                    position,
                    duration,
                    curve
                )
            );
        }

        /// <summary>
        /// 지정된 Transform 위치로 이동한 뒤 고정한다.
        /// </summary>
        public void FixAtPoint(
            Transform fixedPoint,
            float duration)
        {
            if (fixedPoint == null)
            {
                return;
            }

            FixAtPosition(
                fixedPoint.position,
                duration
            );
        }

        private IEnumerator FixPositionRoutine(
            Vector3 startPosition,
            Vector3 targetPosition,
            float duration,
            AnimationCurve curve)
        {
            if (duration <= 0f)
            {
                transform.position = targetPosition;
                _positionCoroutine = null;

                InvalidateCameraState();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                float normalizedTime =
                    Mathf.Clamp01(elapsed / duration);

                float evaluatedTime = curve != null
                    ? curve.Evaluate(normalizedTime)
                    : normalizedTime;

                transform.position =
                    Vector3.LerpUnclamped(
                        startPosition,
                        targetPosition,
                        evaluatedTime
                    );

                yield return null;
            }

            transform.position = targetPosition;
            _positionCoroutine = null;

            InvalidateCameraState();
        }

        private void ClearFollowTargetInternal()
        {
            _currentFollowTarget = null;
            _controlState = CameraControlState.Fixed;

            if (_cinemachineCamera == null)
            {
                return;
            }

            _cinemachineCamera.Target.TrackingTarget =
                null;
        }

        private Vector3 GetCurrentRenderedCameraPosition()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                return mainCamera.transform.position;
            }

            return transform.position;
        }

        private void InvalidateCameraState()
        {
            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.PreviousStateIsValid =
                    false;
            }
        }

        // =========================================================
        // Follow offset
        // =========================================================

        public void SetFollowOffset(
            Vector3 targetOffset,
            float duration)
        {
            StopOffsetTransition();

            _offsetCoroutine = StartCoroutine(
                FollowOffsetRoutine(
                    targetOffset,
                    duration
                )
            );
        }

        private IEnumerator FollowOffsetRoutine(
            Vector3 targetOffset,
            float duration)
        {
            if (_cinemachineFollow == null)
            {
                _offsetCoroutine = null;
                yield break;
            }

            Vector3 startOffset =
                _cinemachineFollow.FollowOffset;

            if (duration <= 0f)
            {
                _cinemachineFollow.FollowOffset =
                    targetOffset;

                _offsetCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                float t =
                    Mathf.Clamp01(elapsed / duration);

                _cinemachineFollow.FollowOffset =
                    Vector3.Lerp(
                        startOffset,
                        targetOffset,
                        Mathf.SmoothStep(0f, 1f, t)
                    );

                yield return null;
            }

            _cinemachineFollow.FollowOffset =
                targetOffset;

            _offsetCoroutine = null;
        }

        // =========================================================
        // Shake
        // =========================================================

        /// <summary>
        /// 지정 시간 동안 화면을 흔든다.
        /// </summary>
        public void Shake(float duration)
        {
            Shake(duration, _defaultShakeStrength);
        }

        /// <summary>
        /// interval 간격마다 Impulse를 발생시켜
        /// duration 동안 지속적으로 화면을 흔든다.
        /// </summary>
        public void Shake(
            float duration,
            float strength)
        {
            StopShake();

            if (_impulseSource == null)
            {
                Debug.LogWarning(
                    "[CameraController] CinemachineImpulseSource가 없습니다.",
                    this
                );

                return;
            }

            if (duration <= 0f || strength <= 0f)
            {
                return;
            }

            _shakeCoroutine = StartCoroutine(
                ShakeRoutine(duration, strength)
            );
        }

        /// <summary>
        /// 한 번만 흔들림을 발생시킨다.
        /// </summary>
        public void ShakeOnce(
            float strength,
            Vector3 direction)
        {
            if (_impulseSource == null ||
                strength <= 0f)
            {
                return;
            }

            GenerateImpulse(strength);
        }

        private IEnumerator ShakeRoutine(
            float duration,
            float strength)
        {
            float elapsed = 0f;
            float nextImpulseTime = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                if (elapsed >= nextImpulseTime)
                {
                    float currentStrength = strength;

                    if (_fadeShakeStrength)
                    {
                        float remainingRatio =
                            1f - Mathf.Clamp01(
                                elapsed / duration
                            );

                        currentStrength *= remainingRatio;
                    }

                    GenerateImpulse(currentStrength);

                    nextImpulseTime += _defaultShakeInterval;
                }

                yield return null;
            }

            _shakeCoroutine = null;
        }

        private void GenerateImpulse(float strength)
        {
            Vector3 baseDirection =
                _defaultShakeDirection.sqrMagnitude > 0f
                    ? _defaultShakeDirection.normalized
                    : Vector3.down;

            Vector2 randomOffset =
                Random.insideUnitCircle *
                _shakeRandomness;

            Vector3 direction =
                (
                    baseDirection +
                    new Vector3(
                        randomOffset.x,
                        randomOffset.y,
                        0f
                    )
                ).normalized;

            _impulseSource.GenerateImpulseWithVelocity(
                direction * strength
            );
        }

        public void StopShake()
        {
            if (_shakeCoroutine == null)
            {
                return;
            }

            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        // =========================================================
        // Zoom
        // =========================================================

        public void Zoom(
            float targetOrthographicSize,
            float duration)
        {
            StopZoomTransition();

            targetOrthographicSize =
                Mathf.Max(
                    0.01f,
                    targetOrthographicSize
                );

            _zoomCoroutine = StartCoroutine(
                ZoomRoutine(
                    targetOrthographicSize,
                    duration
                )
            );
        }

        public void RestoreDefaultZoom(
            float duration)
        {
            Zoom(
                _defaultOrthographicSize,
                duration
            );
        }

        private IEnumerator ZoomRoutine(
            float targetSize,
            float duration)
        {
            if (_cinemachineCamera == null)
            {
                _zoomCoroutine = null;
                yield break;
            }

            float startSize =
                _cinemachineCamera
                    .Lens
                    .OrthographicSize;

            if (duration <= 0f)
            {
                SetOrthographicSizeImmediate(
                    targetSize
                );

                _zoomCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                float t =
                    Mathf.Clamp01(elapsed / duration);

                float size = Mathf.Lerp(
                    startSize,
                    targetSize,
                    Mathf.SmoothStep(0f, 1f, t)
                );

                SetOrthographicSize(size);

                yield return null;
            }

            SetOrthographicSize(targetSize);
            _zoomCoroutine = null;
        }

        public void SetOrthographicSizeImmediate(
            float size)
        {
            StopZoomTransition();
            SetOrthographicSize(size);
        }

        private void SetOrthographicSize(float size)
        {
            if (_cinemachineCamera == null)
            {
                return;
            }

            LensSettings lens =
                _cinemachineCamera.Lens;

            lens.OrthographicSize =
                Mathf.Max(0.01f, size);

            _cinemachineCamera.Lens = lens;
        }

        // =========================================================
        // Monochrome
        // =========================================================

        public void SetMonochrome(
            bool enabled,
            float duration)
        {
            StopMonochromeTransition();

            float targetSaturation =
                enabled ? -100f : 0f;

            _monochromeCoroutine = StartCoroutine(
                MonochromeRoutine(
                    targetSaturation,
                    duration
                )
            );
        }

        private IEnumerator MonochromeRoutine(
            float targetSaturation,
            float duration)
        {
            if (_colorAdjustments == null)
            {
                _monochromeCoroutine = null;
                yield break;
            }

            float startSaturation =
                _colorAdjustments.saturation.value;

            if (duration <= 0f)
            {
                SetSaturation(targetSaturation);

                _monochromeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();

                float t =
                    Mathf.Clamp01(elapsed / duration);

                float saturation = Mathf.Lerp(
                    startSaturation,
                    targetSaturation,
                    Mathf.SmoothStep(0f, 1f, t)
                );

                SetSaturation(saturation);

                yield return null;
            }

            SetSaturation(targetSaturation);
            _monochromeCoroutine = null;
        }

        private void SetSaturation(float value)
        {
            if (_colorAdjustments == null)
            {
                return;
            }

            _colorAdjustments
                .saturation
                .overrideState = true;

            _colorAdjustments
                .saturation
                .value = Mathf.Clamp(
                    value,
                    -100f,
                    100f
                );
        }

        // =========================================================
        // Coroutine control
        // =========================================================

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        private void StopPositionTransition()
        {
            if (_positionCoroutine == null)
            {
                return;
            }

            StopCoroutine(_positionCoroutine);
            _positionCoroutine = null;
        }

        private void StopOffsetTransition()
        {
            if (_offsetCoroutine == null)
            {
                return;
            }

            StopCoroutine(_offsetCoroutine);
            _offsetCoroutine = null;
        }

        private void StopZoomTransition()
        {
            if (_zoomCoroutine == null)
            {
                return;
            }

            StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = null;
        }

        private void StopMonochromeTransition()
        {
            if (_monochromeCoroutine == null)
            {
                return;
            }

            StopCoroutine(_monochromeCoroutine);
            _monochromeCoroutine = null;
        }

        private void OnDisable()
        {
            StopPositionTransition();
            StopOffsetTransition();
            StopZoomTransition();
            StopMonochromeTransition();
            StopShake();
        }
    }
}