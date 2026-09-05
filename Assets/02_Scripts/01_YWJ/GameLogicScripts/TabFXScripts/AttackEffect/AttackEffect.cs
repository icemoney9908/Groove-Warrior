using System.Collections;
using UnityEngine;

namespace YWJ.Effect
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AttackEffect :
        MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField]
        private Animator _animator;

        [Header("Playback")]
        [Tooltip(
            "Animator를 사용하지 않을 때 표시되는 시간입니다."
        )]
        [Min(0.01f)]
        [SerializeField]
        private float _defaultDuration = 0.25f;

        [Tooltip(
            "Animator를 사용하더라도 이 시간이 지나면 " +
            "강제로 풀에 반환합니다."
        )]
        [Min(0.01f)]
        [SerializeField]
        private float _maximumDuration = 2f;

        private AttackEffectPoolManager _owner;
        private Coroutine _returnRoutine;
        private bool _isPlaying;

        public string EffectId { get; private set; }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer =
                    GetComponent<SpriteRenderer>();
            }

            if (_animator == null)
            {
                _animator =
                    GetComponent<Animator>();
            }
        }

        public void Initialize(
            string effectId,
            AttackEffectPoolManager owner)
        {
            EffectId = effectId;
            _owner = owner;
        }

        public void Play(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Transform parent = null,
            bool flipX = false,
            float duration = -1f)
        {
            StopReturnRoutine();

            transform.SetParent(
                parent,
                false
            );

            if (parent == null)
            {
                transform.SetPositionAndRotation(
                    position,
                    rotation
                );
            }
            else
            {
                transform.localPosition =
                    position;

                transform.localRotation =
                    rotation;
            }

            transform.localScale = scale;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX =
                    flipX;

                _spriteRenderer.enabled =
                    true;
            }

            gameObject.SetActive(true);

            RestartAnimator();

            _isPlaying = true;

            float finalDuration =
                duration > 0f
                    ? duration
                    : GetPlaybackDuration();

            _returnRoutine =
                StartCoroutine(
                    ReturnAfterDelayRoutine(
                        finalDuration
                    )
                );
        }

        /// <summary>
        /// 애니메이션 이벤트에서도 직접 호출할 수 있습니다.
        /// </summary>
        public void ReturnToPool()
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;

            StopReturnRoutine();

            _owner?.Return(this);
        }

        private void RestartAnimator()
        {
            if (_animator == null)
                return;

            _animator.enabled = true;

            _animator.Rebind();
            _animator.Update(0f);
        }

        private float GetPlaybackDuration()
        {
            if (_animator == null ||
                _animator.runtimeAnimatorController == null)
            {
                return _defaultDuration;
            }

            RuntimeAnimatorController controller =
                _animator.runtimeAnimatorController;

            AnimationClip[] clips =
                controller.animationClips;

            if (clips == null ||
                clips.Length == 0)
            {
                return _defaultDuration;
            }

            float longestDuration = 0f;

            for (int i = 0;
                 i < clips.Length;
                 i++)
            {
                AnimationClip clip =
                    clips[i];

                if (clip == null)
                    continue;

                longestDuration =
                    Mathf.Max(
                        longestDuration,
                        clip.length
                    );
            }

            if (longestDuration <= 0f)
            {
                longestDuration =
                    _defaultDuration;
            }

            return Mathf.Min(
                longestDuration,
                _maximumDuration
            );
        }

        private IEnumerator
            ReturnAfterDelayRoutine(
                float duration)
        {
            duration =
                Mathf.Clamp(
                    duration,
                    0.01f,
                    _maximumDuration
                );

            yield return new WaitForSeconds(
                duration
            );

            _returnRoutine = null;

            ReturnToPool();
        }

        private void StopReturnRoutine()
        {
            if (_returnRoutine == null)
                return;

            StopCoroutine(
                _returnRoutine
            );

            _returnRoutine = null;
        }

        public void OnReturnedToPool(
            Transform poolRoot)
        {
            StopReturnRoutine();

            _isPlaying = false;

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            transform.SetParent(
                poolRoot,
                false
            );

            transform.localPosition =
                Vector3.zero;

            transform.localRotation =
                Quaternion.identity;

            transform.localScale =
                Vector3.one;

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _isPlaying = false;
        }
    }
}